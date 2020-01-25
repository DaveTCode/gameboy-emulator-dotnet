using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gameboy.VM;
using Gameboy.VM.Joypad;
using Gameboy.VM.LCD;

namespace Gameboy.Emulator.SDL
{
    internal class SDL2Application : IDisposable
    {
        private readonly Dictionary<Grayscale, (byte, byte, byte)> _grayscaleColorMap = new Dictionary<Grayscale, (byte, byte, byte)>
        {
            { Grayscale.White, (236, 237, 176) },
            { Grayscale.LightGray, (187, 187, 24) },
            { Grayscale.DarkGray, (107, 110, 0) },
            { Grayscale.Black, (16, 55, 0) },
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

        internal SDL2Application(Device device, int pixelSize, int framesPerSecond)
        {
            _device = device;
            _device.VBlankHandler = HandleVBlankEvent;
            _pixelSize = pixelSize;
            var screenWidth = Device.ScreenWidth * pixelSize;
            var screenHeight = Device.ScreenHeight * pixelSize;

            SDL2.SDL_Init(SDL2.SDL_INIT_VIDEO);

            SDL2.SDL_CreateWindowAndRenderer(
                screenWidth,
                screenHeight,
                0,
                out _window,
                out _renderer);
            SDL2.SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            SDL2.SDL_RenderClear(_renderer);
            SDL2.SDL_SetWindowTitle(_window, $"{device.GetCartridgeTitle()} 59.7fps");

            _msPerFrame = (int) ((1.0 / framesPerSecond) * 1000);
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

        public void HandleVBlankEvent((byte, byte, byte)[] frameBuffer)
        {
            // TODO - Should do this more than once per VBlank (particularly since VBlank not fired during HALT/STOP!
            CheckForInput();

            for (var pixel = 0; pixel < frameBuffer.Length; pixel++)
            {
                var (red, green, blue) = frameBuffer[pixel];
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
            if (msToSleep > 0)
            {
                SDL2.SDL_Delay((uint)msToSleep);
            }
            _stopwatch.Restart();
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
            SDL2.SDL_DestroyRenderer(_renderer);
            SDL2.SDL_DestroyWindow(_window);
            SDL2.SDL_Quit();
        }
    }
}
