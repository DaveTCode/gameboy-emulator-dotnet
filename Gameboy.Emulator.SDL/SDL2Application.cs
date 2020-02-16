using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gameboy.VM;
using Gameboy.VM.Cartridge;
using Gameboy.VM.Joypad;

namespace Gameboy.Emulator.SDL
{
    internal class SDL2Application : IDisposable
    {
        private readonly IntPtr _window;
        private readonly IntPtr _renderer;
        private readonly Device _device;
        private readonly NAudioSoundOutput _soundOutput;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly int _msPerFrame;

        internal SDL2Application(Cartridge cartridge, DeviceType mode, int pixelSize, byte[] bootRom, int framesPerSecond)
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

            // TODO - This being an int means our frames aren't CPU isn't quite clocked properly
            _msPerFrame = (int)((1.0 / framesPerSecond) * 1000);

            var sdl2Renderer = new SDL2Renderer(_renderer, mode);
            _soundOutput = new NAudioSoundOutput();
            _device = new Device(cartridge, mode, sdl2Renderer, _soundOutput, bootRom);
        }

        private bool _quit;

        private const int ClocksPerFrame = 70256;
        private const int ClocksPerInputCheck = 35000;
        private int _inputCountdown = ClocksPerInputCheck;
        private int _delayCountdown = ClocksPerFrame;
        public void ExecuteProgram()
        {
            _stopwatch.Start();

            while (!_quit)
            {
                var clocks = _device.Step();

                _inputCountdown -= clocks;
                if (_inputCountdown < 0)
                {
                    _inputCountdown += ClocksPerInputCheck;
                    CheckForInput();
                }

                _delayCountdown -= clocks;
                if (_delayCountdown < 0)
                {
                    _delayCountdown += ClocksPerFrame;
                    var msToSleep = _msPerFrame - (_stopwatch.ElapsedTicks / (double)Stopwatch.Frequency) * 1000;
                    if (msToSleep > 0)
                    {
                        SDL2.SDL_Delay((uint)msToSleep);
                    }
                    _stopwatch.Restart();
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
            _soundOutput?.Dispose();
            SDL2.SDL_DestroyRenderer(_renderer);
            SDL2.SDL_DestroyWindow(_window);
            SDL2.SDL_Quit();
        }
    }
}
