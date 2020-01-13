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
            { Grayscale.White, (0xFF, 0xFF, 0xFF) },
            { Grayscale.LightGray, (0xCC, 0xCC, 0xCC) },
            { Grayscale.DarkGray, (0x77, 0x77, 0x77) },
            { Grayscale.Black, (0x00, 0x00, 0x00) },
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

        internal SDL2Application(in Device device, in int pixelSize)
        {
            _device = device;
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
        }

        public void ExecuteProgram(int framesPerSecond)
        {
            var stopwatch = Stopwatch.StartNew();
            var msPerFrame = (1.0 / framesPerSecond) * 1000;
            var clockCyclesPerFrame = Device.ClockCyclesPerSecond / framesPerSecond;

            var quit = false;
            while (!quit)
            {
                // TODO - Checking for input once per frame seems a bit sketchy
                while (SDL2.SDL_PollEvent(out var e) != 0)
                {
                    switch (e.type)
                    {
                        case SDL2.SDL_EventType.SDL_QUIT:
                            quit = true;
                            break;
                        // TODO - Handle input here when joypad implementation complete
                        case SDL2.SDL_EventType.SDL_KEYUP:
                            if (e.key.keysym.sym == SDL2.SDL_Keycode.SDLK_F2)
                            {
                                var frameBuffer = _device.GetCurrentFrame();
                                var (vram, oamram) = _device.DumpVRAM();
                                using var fbFile = System.IO.File.OpenWrite("framebuffer");
                                using var vramFile = System.IO.File.OpenWrite("VRAM.csv");
                                using var oamFile = System.IO.File.OpenWrite("OAMRAM.csv");
                                vramFile.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", vram)));
                                oamFile.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", vram)));
                                fbFile.Write(System.Text.Encoding.ASCII.GetBytes(string.Join("\r\n", frameBuffer.Select(f => (int)f))));
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

                var cyclesThisUpdate = 0;
                while (cyclesThisUpdate < clockCyclesPerFrame)
                {
                    cyclesThisUpdate += _device.Step();
                }

                if (_device.IsScreenOn()) // TODO - What if after n cycles the screen isn't in VBlank?
                {
                    var frameBuffer = _device.GetCurrentFrame();

                    for (var pixel = 0; pixel < frameBuffer.Length; pixel++)
                    {
                        var (red, green, blue) = _grayscaleColorMap[frameBuffer[pixel]];
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
                }

                var msToSleep = msPerFrame - (stopwatch.ElapsedTicks / Stopwatch.Frequency) * 1000;
                //Console.WriteLine($"Frame took {stopwatch.ElapsedTicks * 1000 / Stopwatch.Frequency}ms, ms per frame should be {msPerFrame}");
                if (msToSleep > 0)
                {
                    SDL2.SDL_Delay((uint)msToSleep);
                }
                stopwatch.Restart();
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
