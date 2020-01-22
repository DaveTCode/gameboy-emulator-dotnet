using System;
using Gameboy.VM.Interrupts;

namespace Gameboy.VM.LCD
{
    internal class LCDDriver
    {
        private const int VRAMSize = 0x2000;
        private const int OAMRAMSize = 0xA0;

        public const int ClockCyclesForScanline = 456;
        public const int MaxSpritesPerScanline = 10; // TODO - Not actually using this
        public const int MaxSpritesPerFrame = 40;

        private readonly Device _device;

        private readonly byte[] _vRamBank0 = new byte[VRAMSize];
        private readonly byte[] _vRamBank1 = new byte[VRAMSize];
        private readonly byte[] _oamRam = new byte[OAMRAMSize];

        private byte _vRamBank;

        /// <summary>
        /// Pre-allocated array of sprite objects which get filled on each
        /// iteration through the DrawSprites routine
        /// </summary>
        private readonly Sprite[] _sprites = {
            new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(),
            new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(),
            new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(),
            new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite()
        };

        private readonly Grayscale[] _frameBuffer = new Grayscale[Device.ScreenHeight * Device.ScreenWidth];

        // Current state of LCD driver
        private int _currentCycle;

        internal LCDDriver(Device device)
        {
            _device = device;
        }

        internal void Clear()
        {
            _currentCycle = 0;
            _vRamBank = 0;
            Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
            Array.Clear(_vRamBank0, 0, _vRamBank0.Length);
            Array.Clear(_vRamBank1, 0, _vRamBank1.Length);
            Array.Clear(_oamRam, 0, _oamRam.Length);
        }

        internal byte GetVRAMBankRegister()
        {
            return _vRamBank == 1 ? (byte) 0xFF : (byte) 0xFE;
        }

        internal void SetVRAMBankRegister(byte value)
        {
            if (_device.Mode == DeviceMode.DMG) return;

            _vRamBank = (byte) (value & 0x1); // Only bottom bit is important
        }

        internal byte GetVRAMByte(ushort address)
        {
            return _vRamBank == 0 ? _vRamBank0[address - 0x8000] : _vRamBank1[address - 0x8000];
        }

        internal void WriteVRAMByte(ushort address, byte value)
        {
            if (_vRamBank == 0) _vRamBank0[address - 0x8000] = value;
            else _vRamBank1[address - 0x8000] = value;
        }

        internal byte GetOAMByte(ushort address)
        {
            return _oamRam[address - 0xFE00];
        }

        internal void WriteOAMByte(ushort address, byte value)
        {
            var modAddress = address - 0xFE00;
            var spriteNumber = modAddress >> 2;
            _oamRam[modAddress] = value;

            // Also update the relevant sprite
            switch (modAddress & 0x3)
            {
                case 0:
                    _sprites[spriteNumber].Y = value - 16;
                    break;
                case 1:
                    _sprites[spriteNumber].X = value - 8;
                    break;
                case 2:
                    _sprites[spriteNumber].TileNumber = value;
                    break;
                case 3:
                    _sprites[spriteNumber].SpriteToBgPriority = (value & 0x80) == 0x80 ? SpriteToBgPriority.BehindColors123 : SpriteToBgPriority.Above;
                    _sprites[spriteNumber].YFlip = (value & 0x40) == 0x40;
                    _sprites[spriteNumber].XFlip = (value & 0x20) == 0x20;
                    _sprites[spriteNumber].UsePalette1 = (value & 0x10) == 0x10;
                    break;
            }
        }

        internal Grayscale[] GetCurrentFrame()
        {
            return _frameBuffer;
        }

        /// <summary>
        /// Internal state to avoid allocation during scanlines, used by sprites to tell whether to draw over bg
        /// </summary>
        private readonly Grayscale[] _scanline = new Grayscale[Device.ScreenWidth];

        /// <summary>
        /// Proceed by <see cref="cycles"/> number of cycles.
        ///
        /// Note that this is all a bit sketchy IMO. It's only drawing whole
        /// scanlines at a time instead of doing pixel based timings. But I
        /// don't know enough to achieve that yet
        /// </summary>
        /// <param name="cycles">The number of cycles since the last step was called.</param>
        internal void Step(int cycles)
        {
            if (!_device.LCDRegisters.IsLcdOn)
            {
                SetLCDOffValues();
                return;
            }

            _currentCycle += cycles;

            var currentScanLine = _device.LCDRegisters.LCDCurrentScanline;

            if (_currentCycle >= ClockCyclesForScanline)
            {
                _currentCycle -= ClockCyclesForScanline;

                // Update the LY register
                currentScanLine = _device.LCDRegisters.IncrementLineBeingProcessed();
            }

            var redrawScanline = SetLCDStatus(currentScanLine);

            // Don't render on invisible scanlines
            if (currentScanLine < Device.ScreenHeight && redrawScanline)
            {
                // Clear scanline
                for (var ii = 0; ii < Device.ScreenWidth; ii++)
                {
                    _scanline[ii] = Grayscale.White;
                }

                if (_device.LCDRegisters.IsBackgroundEnabled)
                {
                    DrawBackground();
                }

                if (_device.LCDRegisters.AreSpritesEnabled)
                {
                    DrawSprites();
                }

                // Copy the scanline into the framebuffer
                Array.Copy(_scanline, 0, 
                    _frameBuffer, _device.LCDRegisters.LCDCurrentScanline * Device.ScreenWidth,
                    Device.ScreenWidth);
            }
        }

        internal (byte[], byte[]) DumpVRAM()
        {
            return (_vRamBank0, _oamRam);
        }

        private void DrawSprites()
        {
            var line = _device.LCDRegisters.LCDCurrentScanline;
            var spriteSize = _device.LCDRegisters.LargeSprites ? 16 : 8;

            // Loop through all sprites
            for (var spriteIndex = 0; spriteIndex < MaxSpritesPerFrame; spriteIndex++)
            {
                var sprite = _sprites[spriteIndex];

                // Ensure that a portion of the sprite lies on the line
                if (line < sprite.Y || line >= sprite.Y + spriteSize) continue;

                var tileNumber = spriteSize == 8 ? sprite.TileNumber : sprite.TileNumber & 0xFE;
                var palette = sprite.UsePalette1
                    ? _device.LCDRegisters.ObjectPaletteData1
                    : _device.LCDRegisters.ObjectPaletteData0;

                var tileAddress = sprite.YFlip ? 
                    tileNumber * 16 + (spriteSize - 1 - (line - sprite.Y)) * 2 :
                    tileNumber * 16 + (line - sprite.Y) * 2;
                var b1 = _vRamBank0[tileAddress];
                var b2 = _vRamBank0[tileAddress + 1];

                for (var x = 0; x < 8; x++)
                {
                    var pixel = sprite.X + x;
                    if (pixel < 0 || pixel >= Device.ScreenWidth) continue;
                    
                    // Convert the tile data spread over two bytes into the
                    // specific color value for this pixel.
                    var colorBit = sprite.XFlip ? x : 7 - x;
                    var colorBitMask = 1 << colorBit;
                    var colorNumber =
                        ((b2 & colorBitMask) == colorBitMask ? 2 : 0) +
                        ((b1 & colorBitMask) == colorBitMask ? 1 : 0);

                    if (colorNumber == 0) continue; // Don't draw pixels if they're color 0 (transparent)
                    var color = _device.LCDRegisters.GetColorFromNumberPalette(colorNumber, palette);

                    if (sprite.SpriteToBgPriority == SpriteToBgPriority.BehindColors123 &&
                        _scanline[pixel] != Grayscale.White) continue; // Don't draw low priority sprites over background

                    _scanline[pixel] = color;
                }
            }
        }

        private void DrawBackground()
        {
            // First figure out which bit of memory we need for the tilemap (pixel X -> tile Y),
            // which bit of memory for the tileset itself (tile Y -> data Z),
            // and which y coordinate (both in tiles and raw pixels) we're talking about
            var tileMapAddress = UsingWindowForScanline
                ? _device.LCDRegisters.WindowTileMapOffset
                : _device.LCDRegisters.BackgroundTileMapOffset;
            var yPosition = UsingWindowForScanline
                ? ((_device.LCDRegisters.LCDCurrentScanline - _device.LCDRegisters.WindowY) & 0xFF)
                : ((_device.LCDRegisters.LCDCurrentScanline + _device.LCDRegisters.ScrollY) & 0xFF);
            var tileRow = (yPosition / 8) * 32;
            var tileLine = (yPosition % 8) * 2;

            for (var pixel = 0; pixel < Device.ScreenWidth; pixel++)
            {
                // Determine the x position relative to whether we're in the window or the background
                // taking into account scrolling.
                var xPos = (UsingWindowForScanline && pixel >= _device.LCDRegisters.WindowX) ?
                    ((pixel - _device.LCDRegisters.WindowX + 7) & 0xFF) :
                    ((pixel + _device.LCDRegisters.ScrollX) & 0xFF);

                var tileCol = xPos / 8;

                var tileNumberAddress = (ushort)((tileMapAddress + tileRow + tileCol) & 0xFFFF);

                var tileNumber = _vRamBank0[tileNumberAddress - 0x8000];

                var tileDataAddress = GetTileDataAddress(tileNumber);
                var byte1 = _vRamBank0[(tileDataAddress + tileLine) & 0xFFFF - 0x8000];
                var byte2 = _vRamBank0[(tileDataAddress + tileLine + 1) & 0xFFFF - 0x8000];

                // Convert the tile data spread over two bytes into the
                // specific color value for this pixel.
                var colorBit = ((xPos % 8) - 7) * -1;
                var colorBitMask = 1 << colorBit;
                var colorNumber =
                    ((byte2 & colorBitMask) == colorBitMask ? 2 : 0) +
                    ((byte1 & colorBitMask) == colorBitMask ? 1 : 0);

                // Retrieve the actual color to be used from the palette
                var color = _device.LCDRegisters.GetColorFromNumberPalette(colorNumber, _device.LCDRegisters.BackgroundPaletteData);

                // Finally set the pixel to the appropriate color
                _scanline[pixel] = color;
            }
        }

        private void SetLCDOffValues()
        {
            _currentCycle = 0x0;
            _device.LCDRegisters.ResetCurrentScanline();
            _device.LCDRegisters.StatMode = StatMode.HBlankPeriod;
        }

        private bool SetLCDStatus(byte currentScanLine)
        {
            var oldMode = _device.LCDRegisters.StatMode;

            if (currentScanLine >= Device.ScreenHeight)
            {
                _device.LCDRegisters.StatMode = StatMode.VBlankPeriod;
            }
            else
            {
                _device.LCDRegisters.StatMode = _currentCycle switch
                {
                    _ when _currentCycle < 80 => StatMode.OAMRAMPeriod,
                    _ when _currentCycle < 252 => StatMode.TransferringDataToDriver, // TODO - Not strictly true, depends on #sprites
                    _ => StatMode.HBlankPeriod
                };
            }

            if (oldMode != _device.LCDRegisters.StatMode)
            {
                switch (_device.LCDRegisters.StatMode)
                {
                    case StatMode.HBlankPeriod:
                        if (_device.LCDRegisters.Mode0HBlankCheckEnabled)
                        {
                            _device.InterruptRegisters.RequestInterrupt(Interrupt.LCDSTAT);
                        }
                        return true; // Entering HBlank so redraw scanline
                    case StatMode.VBlankPeriod:
                        _device.VBlankHandler?.Invoke(_frameBuffer);
                        _device.InterruptRegisters.RequestInterrupt(Interrupt.VerticalBlank); // TODO - VBlank interrupt but do we need an LCDSTAT interrupt as well?
                        return false;
                    case StatMode.OAMRAMPeriod:
                        if (_device.LCDRegisters.Mode2OAMCheckEnabled)
                        {
                            _device.InterruptRegisters.RequestInterrupt(Interrupt.LCDSTAT);
                        }
                        return false;
                    case StatMode.TransferringDataToDriver:
                        return false;
                    default:
                        throw new ArgumentException($"StatMode {_device.LCDRegisters.StatMode} out of range");
                }
            }

            return false;
        }

        #region Utility functions on registers

        private bool UsingWindowForScanline => _device.LCDRegisters.IsWindowEnabled && _device.LCDRegisters.LCDCurrentScanline >= _device.LCDRegisters.WindowY;

        private ushort GetTileDataAddress(byte tileNumber)
        {
            var tilesetAddress = _device.LCDRegisters.BackgroundAndWindowTilesetOffset;
            ushort tileDataAddress;
            if (_device.LCDRegisters.UsingSignedByteForTileData)
            {
                tileDataAddress = (ushort)((tilesetAddress + ((sbyte)tileNumber + 128) * 16) & 0xFFFF);
            }
            else
            {
                tileDataAddress = (ushort)((tilesetAddress + tileNumber * 16) & 0xFFFF);
            }
            return tileDataAddress;
        }

        #endregion
    }
}
