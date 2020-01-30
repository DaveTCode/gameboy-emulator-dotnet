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

        private readonly (byte, byte, byte)[] _frameBuffer = new (byte, byte, byte)[Device.ScreenHeight * Device.ScreenWidth];

        // Current state of LCD driver
        private int _currentCycle;

        internal LCDDriver(Device device)
        {
            _device = device;
        }

        internal byte GetVRAMBankRegister()
        {
            return _vRamBank == 1 ? (byte) 0xFF : (byte) 0xFE;
        }

        internal void SetVRAMBankRegister(byte value)
        {
            if (_device.Mode == DeviceType.DMG) return;

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
                    _sprites[spriteNumber].VRAMBankNumber = (value & 0x8) >> 3;
                    _sprites[spriteNumber].CGBPaletteNumber = value & 0x7;
                    break;
            }
        }

        internal (byte, byte, byte)[] GetCurrentFrame()
        {
            return _frameBuffer;
        }

        /// <summary>
        /// Internal state to avoid allocation during scanlines, used by sprites to tell whether to draw over bg
        /// </summary>
        private readonly (byte, byte, byte)[] _scanline = new (byte, byte, byte)[Device.ScreenWidth];
        private readonly ScanlineBgPriority[] _scanlineBgPriority = new ScanlineBgPriority[Device.ScreenWidth];

        /// <summary>
        /// Proceed by <see cref="tCycles"/> number of cycles.
        ///
        /// Note that this is all a bit sketchy IMO. It's only drawing whole
        /// scanlines at a time instead of doing pixel based timings. But I
        /// don't know enough to achieve that yet
        /// </summary>
        /// <param name="tCycles">
        /// The number of cycles since the last step was called.
        /// </param>
        internal void Step(int tCycles)
        {
            if (!_device.LCDRegisters.IsLcdOn)
            {
                SetLCDOffValues();
                return;
            }

            _currentCycle += tCycles;

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
                    _scanline[ii] = Grayscale.White.BaseRgb();
                    _scanlineBgPriority[ii] = ScanlineBgPriority.Normal;
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

        internal (byte[], byte[], byte[]) DumpVRAM()
        {
            return (_vRamBank0, _vRamBank1, _oamRam);
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
                var b1 = sprite.VRAMBankNumber == 0
                    ? _vRamBank0[tileAddress]
                    : _vRamBank1[tileAddress];
                var b2 = sprite.VRAMBankNumber == 0
                    ? _vRamBank0[tileAddress + 1]
                    : _vRamBank1[tileAddress + 1];

                for (var x = 0; x < 8; x++)
                {
                    var pixel = sprite.X + x;
                    if (pixel < 0 || pixel >= Device.ScreenWidth) continue;
                    if (_scanlineBgPriority[pixel] == ScanlineBgPriority.Priority) continue; // Don't overwrite high priority background tiles

                    // Convert the tile data spread over two bytes into the
                    // specific color value for this pixel.
                    var colorBit = sprite.XFlip ? x : 7 - x;
                    var colorBitMask = 1 << colorBit;
                    var colorNumber =
                        ((b2 & colorBitMask) == colorBitMask ? 2 : 0) +
                        ((b1 & colorBitMask) == colorBitMask ? 1 : 0);

                    if (colorNumber == 0) continue; // Don't draw pixels if they're color 0 (transparent)

                    // Retrieve the actual color to be used from the palette
                    var color = _device.Mode == DeviceType.CGB
                        ? _device.LCDRegisters.CGBSpritePalette.Palette[sprite.CGBPaletteNumber * 4 + colorNumber]
                        : _device.LCDRegisters.GetColorFromNumberPalette(colorNumber, palette).BaseRgb();

                    // Don't draw low priority sprites over background unless background is transparent
                    if (sprite.SpriteToBgPriority == SpriteToBgPriority.BehindColors123 &&
                        _scanlineBgPriority[pixel] == ScanlineBgPriority.Normal) continue;

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
                ? (_device.LCDRegisters.LCDCurrentScanline - _device.LCDRegisters.WindowY) & 0xFF
                : (_device.LCDRegisters.LCDCurrentScanline + _device.LCDRegisters.ScrollY) & 0xFF;
            var tileRow = yPosition / 8 * 32;
            var tileLine = yPosition % 8 * 2;

            for (var pixel = 0; pixel < Device.ScreenWidth; pixel++)
            {
                // Determine the x position relative to whether we're in the window or the background
                // taking into account scrolling.
                var xPos = UsingWindowForScanline && pixel >= _device.LCDRegisters.WindowX ?
                    (pixel - _device.LCDRegisters.WindowX + 7) & 0xFF :
                    (pixel + _device.LCDRegisters.ScrollX) & 0xFF;

                var tileCol = xPos / 8;

                var tileNumberAddress = (ushort)((tileMapAddress + tileRow + tileCol) & 0xFFFF);

                var tileNumber = _vRamBank0[tileNumberAddress - 0x8000];
                var flagsByte = _device.Mode == DeviceType.CGB
                    ? _vRamBank1[tileNumberAddress - 0x8000]
                    : 0x0;
                var paletteNumber = flagsByte & 0x7;
                var vramBankNumber = (flagsByte & 0x8) >> 3;
                var xFlip = (flagsByte & 0x20) >> 5 != 0;
                var yFlip = (flagsByte & 0x40) >> 6 != 0;
                var bgToOamPriority = (flagsByte & 0x80) >> 7;

                var tileDataAddress = yFlip
                    ? GetTileDataAddress(tileNumber) + 14 - tileLine
                    : GetTileDataAddress(tileNumber) + tileLine;

                var byte1 = vramBankNumber == 0
                    ? _vRamBank0[tileDataAddress & 0xFFFF - 0x8000]
                    : _vRamBank1[tileDataAddress & 0xFFFF - 0x8000];
                var byte2 = vramBankNumber == 0
                    ? _vRamBank0[(tileDataAddress + 1) & 0xFFFF - 0x8000]
                    : _vRamBank1[(tileDataAddress + 1) & 0xFFFF - 0x8000];

                // Convert the tile data spread over two bytes into the
                // specific color value for this pixel.
                var colorBit = xFlip ? xPos % 8 : 7 - xPos % 8;
                var colorBitMask = 1 << colorBit;
                var colorNumber =
                    ((byte2 & colorBitMask) == colorBitMask ? 2 : 0) +
                    ((byte1 & colorBitMask) == colorBitMask ? 1 : 0);

                // Retrieve the actual color to be used from the palette
                var color = _device.Mode == DeviceType.CGB
                    ? _device.LCDRegisters.CGBBackgroundPalette.Palette[paletteNumber * 4 + colorNumber]
                    : _device.LCDRegisters.GetColorFromNumberPalette(colorNumber, _device.LCDRegisters.BackgroundPaletteData).BaseRgb();

                // Finally set the pixel to the appropriate color and flag whether this color can be overwritten by sprites
                _scanline[pixel] = color;

                if (bgToOamPriority == 1) _scanlineBgPriority[pixel] = ScanlineBgPriority.Priority;
                else if (colorNumber == 0) _scanlineBgPriority[pixel] = ScanlineBgPriority.Color0;
                else _scanlineBgPriority[pixel] = ScanlineBgPriority.Normal;
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
                    case StatMode.VBlankPeriod: // Entering VBlank so draw whole screen
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

        private enum ScanlineBgPriority
        {
            Color0, // Sprites always go on top
            Priority, // Background always goes over
            Normal // Sprites go on top unless they say not to
        }
    }
}
