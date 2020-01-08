﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gameboy.VM;
using Gameboy.VM.LCD;

namespace Gameboy.Emulator.SDL
{
    internal class SDL2Application : IDisposable
    {
        private readonly Dictionary<Grayscale, (byte, byte, byte)> GrayscaleColorMap = new Dictionary<Grayscale, (byte, byte, byte)>
        {
            { Grayscale.White, (0xFF, 0xFF, 0xFF) },
            { Grayscale.LightGray, (0xCC, 0xCC, 0xCC) },
            { Grayscale.DarkGray, (0x77, 0x77, 0x77) },
            { Grayscale.Black, (0x00, 0x00, 0x00) },
        };

        private readonly IntPtr _window;
        private readonly IntPtr _renderer;
        private readonly int _pixelSize;
        private readonly int _screenWidth;
        private readonly int _screenHeight;
        private readonly Device _device;

        internal SDL2Application(in Device device, in int pixelSize)
        {
            _device = device;
            _pixelSize = pixelSize;
            _screenWidth = Device.ScreenWidth * pixelSize;
            _screenHeight = Device.ScreenHeight * pixelSize;

            SDL2.SDL_Init(SDL2.SDL_INIT_VIDEO);

            SDL2.SDL_CreateWindowAndRenderer(
                _screenWidth,
                _screenHeight,
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
                while (SDL2.SDL_PollEvent(out var e) != 0)
                {
                    switch (e.type)
                    {
                        case SDL2.SDL_EventType.SDL_QUIT:
                            quit = true;
                            break;
                            // TODO - Handle input here when joypad implementation complete
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
                        var (red, green, blue) = GrayscaleColorMap[frameBuffer[pixel]];
                        SDL2.SDL_SetRenderDrawColor(_renderer, red, green, blue, 255);

                        var x = pixel % Device.ScreenWidth;
                        var y = pixel / Device.ScreenHeight;
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
                if (msToSleep > 0)
                {
                    SDL2.SDL_Delay((uint)msToSleep);
                }
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
