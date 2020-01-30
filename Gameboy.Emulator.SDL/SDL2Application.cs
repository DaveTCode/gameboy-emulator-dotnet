using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gameboy.VM;
using Gameboy.VM.Joypad;
using Gameboy.VM.LCD;
using NAudio.Wave;

namespace Gameboy.Emulator.SDL
{
    internal class SDL2Application : IDisposable
    {
        private const int AudioFrequency = 44100;
        private const int AudioSamples = 4096;
        private const int DownSampleCount = Device.CyclesPerSecondHz / AudioFrequency / 4;

        private readonly Dictionary<(byte, byte, byte), (byte, byte, byte)> _grayscaleColorMap = new Dictionary<(byte, byte, byte), (byte, byte, byte)>
        {
            { GrayscaleExtensions.GrayscaleWhite, (236, 237, 176) },
            { GrayscaleExtensions.GrayscaleLightGray, (187, 187, 24) },
            { GrayscaleExtensions.GrayscaleDarkGray, (107, 110, 0) },
            { GrayscaleExtensions.GrayscaleBlack, (16, 55, 0) },
        };

        private readonly Dictionary<SDL2.SDL_Keycode, DeviceKey> _keyMap = new Dictionary<SDL2.SDL_Keycode, DeviceKey>
        {
            {SDL2.SDL_Keycode.SDLK_RIGHT, DeviceKey.Right},
            {SDL2.SDL_Keycode.SDLK_LEFT, DeviceKey.Left},
            {SDL2.SDL_Keycode.SDLK_UP, DeviceKey.Up},
            {SDL2.SDL_Keycode.SDLK_DOWN, DeviceKey.Down},
            {SDL2.SDL_Keycode.SDLK_z, DeviceKey.A},
            {SDL2.SDL_Keycode.SDLK_x, DeviceKey.B},
            {SDL2.SDL_Keycode.SDLK_RETURN, DeviceKey.Start},
            {SDL2.SDL_Keycode.SDLK_RSHIFT, DeviceKey.Select}
        };

        private readonly IntPtr _window;
        private readonly IntPtr _renderer;
        private readonly int _pixelSize;
        private readonly Device _device;
        private bool _quit;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly int _msPerFrame;

        private readonly BufferedWaveProvider _waveProvider;
        private readonly IWavePlayer _wavePlayer;

        internal SDL2Application(Device device, int pixelSize, int framesPerSecond)
        {
            _device = device;
            _device.VBlankHandler = HandleVBlankEvent;
            _device.SoundHandler = PlaySoundByte;
            _pixelSize = pixelSize;
            var screenWidth = Device.ScreenWidth * pixelSize;
            var screenHeight = Device.ScreenHeight * pixelSize;

            SDL2.SDL_Init(SDL2.SDL_INIT_VIDEO | SDL2.SDL_INIT_AUDIO);

            SDL2.SDL_CreateWindowAndRenderer(
                screenWidth,
                screenHeight,
                0,
                out _window,
                out _renderer);
            SDL2.SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            SDL2.SDL_RenderClear(_renderer);
            SDL2.SDL_SetWindowTitle(_window, $"{device.GetCartridgeTitle()} 59.7fps");

            _waveProvider = new BufferedWaveProvider(new WaveFormat(AudioFrequency, 2));
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.Init(_waveProvider);
            _wavePlayer.Play();

            _msPerFrame = (int)((1.0 / framesPerSecond) * 1000);
        }

        private void CheckForInput()
        {
            // TODO - Checking for input once per frame seems a bit sketchy
            while (SDL2.SDL_PollEvent(out var e) != 0)
            {
                switch (e.type)
                {
                    case SDL2.SDL_EventType.SDL_QUIT:
                        _quit = true;
                        break;
                    case SDL2.SDL_EventType.SDL_KEYUP:
                        if (e.key.keysym.sym == SDL2.SDL_Keycode.SDLK_F2)
                        {
                            var (vramBank0, vramBank1, oamRam, cgbBgPalette, cgbSpritePalette, frameBuffer) = _device.DumpLcdDebugInformation();
                            using var fbFile = System.IO.File.OpenWrite("framebuffer");
                            using var vramBank0File = System.IO.File.OpenWrite("VRAMBank0.csv");
                            using var vramBank1File = System.IO.File.OpenWrite("VRAMBank1.csv");
                            using var oamFile = System.IO.File.OpenWrite("OAMRAM.csv");
                            using var cgbBgPaletteFile = System.IO.File.OpenWrite("CGB_BG_PALETTE.csv");
                            using var cgbSpritePaletteFile = System.IO.File.OpenWrite("CGB_SPRITE_PALETTE.csv");
                            vramBank0File.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", vramBank0)));
                            vramBank1File.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", vramBank1)));
                            oamFile.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", oamRam)));
                            cgbBgPaletteFile.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", cgbBgPalette)));
                            cgbSpritePaletteFile.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", cgbSpritePalette)));
                            fbFile.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", frameBuffer.Select(color => color.Item1 + "," + color.Item2 + "," + color.Item3))));
                            Console.WriteLine(_device.ToString());
                        }
                        else if (e.key.keysym.sym == SDL2.SDL_Keycode.SDLK_F3)
                        {
                            _device.SetDebugMode();
                        }
                        else if (_keyMap.ContainsKey(e.key.keysym.sym))
                        {
                            _device.HandleKeyUp(_keyMap[e.key.keysym.sym]);
                        }
                        break;
                    case SDL2.SDL_EventType.SDL_KEYDOWN:
                        if (_keyMap.ContainsKey(e.key.keysym.sym))
                        {
                            _device.HandleKeyDown(_keyMap[e.key.keysym.sym]);
                        }
                        break;
                }
            }
        }

        private int _sampleCount;
        private readonly byte[] _soundBuffer = new byte[AudioSamples * 4]; // 2 bytes per channel
        private int _soundBufferIndex;
        public void PlaySoundByte(int left, int right)
        {
            _sampleCount++;
            if (_sampleCount < DownSampleCount) return;
            _sampleCount = 0;

            // Apply gain
            left *= 5000;
            right *= 5000;

            _soundBuffer[_soundBufferIndex] = (byte)left;
            _soundBuffer[_soundBufferIndex + 1] = (byte)(left >> 8);
            _soundBuffer[_soundBufferIndex + 2] = (byte)right;
            _soundBuffer[_soundBufferIndex + 3] = (byte)(right >> 8);
            _soundBufferIndex += 4;

            if (_soundBufferIndex == _soundBuffer.Length)
            {
                _waveProvider.AddSamples(_soundBuffer, 0, _soundBufferIndex);
                _soundBufferIndex = 0;
                Array.Clear(_soundBuffer, 0, _soundBuffer.Length);
            }
        }

        private long _prevFrameTCycles;

        public void HandleVBlankEvent((byte, byte, byte)[] frameBuffer)
        {
            // TODO - Should do this more than once per VBlank (particularly since VBlank not fired during HALT/STOP!
            CheckForInput();

            for (var pixel = 0; pixel < frameBuffer.Length; pixel++)
            {
                var (red, green, blue) = frameBuffer[pixel];
                (red, green, blue) = ColorAdjust(red, green, blue);

                SDL2.SDL_SetRenderDrawColor(_renderer, red, green, blue, 255);

                var x = pixel % Device.ScreenWidth;
                var y = pixel / Device.ScreenWidth;
                var rect = new SDL2.SDL_Rect
                {
                    x = x * _pixelSize,
                    y = y * _pixelSize,
                    h = _pixelSize,
                    w = _pixelSize,
                };
                SDL2.SDL_RenderFillRect(_renderer, ref rect);
            }

            SDL2.SDL_RenderPresent(_renderer);

            var msToSleep = _msPerFrame - (_stopwatch.ElapsedTicks / (double)Stopwatch.Frequency) * 1000;
            Console.WriteLine("Frame took {0}ms and {1} t-cycles", (_stopwatch.ElapsedTicks / (double)Stopwatch.Frequency) * 1000, _device.TCycles - _prevFrameTCycles);
            _prevFrameTCycles = _device.TCycles;
            if (msToSleep > 0)
            {
                //SDL2.SDL_Delay((uint)msToSleep);
            }
            _stopwatch.Restart();
        }

        /// <summary>
        /// Adjust colors for modern LCD screens from original device palettes
        /// </summary>
        private (byte, byte, byte) ColorAdjust(byte r, byte g, byte b)
        {
            if (_device.Type == DeviceType.DMG)
            {
                return _grayscaleColorMap[(r, g, b)];
            }

            return ((byte, byte, byte))(
                (r * 13 + g * 2 + b) >> 1,
                (g * 3 + b) << 1,
                (r * 3 + g * 2 + b * 11) >> 1
            );
        }

        public void ExecuteProgram()
        {
            while (!_quit)
            {
                _device.Step();
            }
        }

        public void Dispose()
        {
            _wavePlayer.Dispose();
            SDL2.SDL_DestroyRenderer(_renderer);
            SDL2.SDL_DestroyWindow(_window);
            SDL2.SDL_Quit();
        }
    }
}
