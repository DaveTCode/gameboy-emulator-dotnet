﻿using System;
using System.Collections.Generic;
using Gameboy.VM;
using Gameboy.VM.Cartridge;
using Gameboy.VM.Joypad;
using NAudio.Wave;

namespace Gameboy.Emulator.SDL
{
    internal class SDL2Application : IDisposable
    {
        private const int AudioFrequency = 44100;
        private const int AudioSamples = 2048;
        private const int DownSampleCount = Device.CyclesPerSecondHz / AudioFrequency / 32;

        private readonly IntPtr _window;
        private readonly IntPtr _renderer;
        private readonly Device _device;

        private readonly BufferedWaveProvider _waveProvider;
        private readonly IWavePlayer _wavePlayer;

        internal SDL2Application(Cartridge cartridge, DeviceType mode, int pixelSize, bool skipBootRom, int framesPerSecond)
        {
            SDL2.SDL_Init(SDL2.SDL_INIT_VIDEO | SDL2.SDL_INIT_AUDIO);

            SDL2.SDL_CreateWindowAndRenderer(
                Device.ScreenWidth * pixelSize,
                Device.ScreenHeight * pixelSize,
                0,
                out _window,
                out _renderer);
            SDL2.SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            SDL2.SDL_RenderClear(_renderer);
            SDL2.SDL_SetWindowTitle(_window, $"{cartridge.GameTitle} 59.7fps");
            SDL2.SDL_SetHint(SDL2.SDL_HINT_RENDER_SCALE_QUALITY, "0");

            _waveProvider = new BufferedWaveProvider(new WaveFormat(AudioFrequency, 16, 2));
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.Init(_waveProvider);
            _wavePlayer.Play();

            var sdl2Renderer = new SDL2Renderer(_renderer, mode, (int)((1.0 / framesPerSecond) * 1000));
            _device = new Device(cartridge, mode, sdl2Renderer) { SoundHandler = PlaySoundByte };

            if (skipBootRom) _device.SkipBootRom();
        }

        private int _sampleCount;
        private readonly byte[] _soundBuffer = new byte[AudioSamples * 4]; // 2 bytes per channel
        private int _soundBufferIndex;
        private bool _quit;

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
                //Console.WriteLine(_waveProvider.BufferedBytes);
                _waveProvider.ClearBuffer();
                _waveProvider.AddSamples(_soundBuffer, 0, _soundBufferIndex);
                _soundBufferIndex = 0;
                //Console.WriteLine(string.Join(",", _soundBuffer));
                Array.Clear(_soundBuffer, 0, _soundBuffer.Length);
            }
        }

        private const int ClocksPerInputCheck = 35000;
        private int _inputCountdown = ClocksPerInputCheck;
        public void ExecuteProgram()
        {
            while (!_quit)
            {
                var clocks = _device.Step();

                _inputCountdown -= clocks;
                if (_inputCountdown < 0)
                {
                    _inputCountdown = ClocksPerInputCheck;
                    CheckForInput();
                }
            }
        }

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
                            fbFile.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", frameBuffer)));
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

        public void Dispose()
        {
            _wavePlayer.Dispose();
            SDL2.SDL_DestroyRenderer(_renderer);
            SDL2.SDL_DestroyWindow(_window);
            SDL2.SDL_Quit();
        }
    }
}
