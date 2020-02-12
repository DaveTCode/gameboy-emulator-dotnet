using System;
using System.Collections.Generic;
using Gameboy.VM;
using Gameboy.VM.LCD;

namespace Gameboy.Emulator.SDL
{
    public class SDL2Renderer : IRenderer, IDisposable
    {
        private readonly DeviceType _deviceType;
        private readonly IntPtr _renderer;
        private readonly IntPtr _texture;

        private readonly Dictionary<(byte, byte, byte), (byte, byte, byte)> _grayscaleColorMap = new Dictionary<(byte, byte, byte), (byte, byte, byte)>
        {
            { GrayscaleExtensions.GrayscaleWhite, (236, 237, 176) },
            { GrayscaleExtensions.GrayscaleLightGray, (187, 187, 24) },
            { GrayscaleExtensions.GrayscaleDarkGray, (107, 110, 0) },
            { GrayscaleExtensions.GrayscaleBlack, (16, 55, 0) },
        };

        public SDL2Renderer(IntPtr renderer, DeviceType deviceType)
        {
            _renderer = renderer;
            _deviceType = deviceType;

            _texture = SDL2.SDL_CreateTexture(
                renderer: _renderer,
                format: SDL2.SDL_PIXELFORMAT_ARGB8888,
                access: (int)SDL2.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                w: Device.ScreenWidth,
                h: Device.ScreenHeight);
        }

        /// <summary>
        /// Adjust colors for modern LCD screens from original device palettes
        /// </summary>
        public (byte, byte, byte) ColorAdjust(byte r, byte g, byte b)
        {
            if (_deviceType == DeviceType.DMG)
            {
                return (r, g, b);
                return _grayscaleColorMap[(r, g, b)];
            }

            return ((byte, byte, byte))(
                (r * 13 + g * 2 + b) >> 1,
                (g * 3 + b) << 1,
                (r * 3 + g * 2 + b * 11) >> 1
            );
        }

        public void HandleVBlankEvent(byte[] frameBuffer, long tCycles)
        {
            unsafe
            {
                fixed (byte* p = frameBuffer)
                {
                    SDL2.SDL_UpdateTexture(_texture, IntPtr.Zero, (IntPtr)p, Device.ScreenWidth * 4);
                }
            }

            SDL2.SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, IntPtr.Zero);
            SDL2.SDL_RenderPresent(_renderer);
        }

        public void Dispose()
        {
            SDL2.SDL_DestroyTexture(_texture);
        }
    }
}
