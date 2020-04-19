using System;
using Gameboy.VM.Interrupts;

namespace Gameboy.VM.LCD
{
    internal class LCDDriver
    {
        private const int VRAMSize = 0x2000;
        private const int OAMRAMSize = 0xA0;

        public const int ClockCyclesForScanline = 456;
        public const int MaxSpritesPerScanline = 10;
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

        /// <summary>
        /// The framebuffer contains the current state of the screen in
        /// internal resolution.
        ///
        /// Note that byte ordering here is R,G,B so 3 bytes per pixel. This is
        /// a leaky abstraction as that's the format of the SDL Surface we'll
        /// write it onto.
        /// </summary>
        private readonly byte[] _frameBuffer = new byte[Device.ScreenHeight * Device.ScreenWidth * 4];

        /// <summary>
        /// Used for debugging, stores off which tile each pixel corresponds to
        /// </summary>
        private readonly byte[] _tileBuffer = new byte[Device.ScreenHeight * Device.ScreenWidth];

        // Current state of LCD driver
        private int _currentTCyclesInScanline;
        private int _currentScanline;
        private int _windowLinesSkipped;
        private bool _frameUsesWindow;
        private bool _scanlineUsedWindow;

        internal LCDDriver(Device device)
        {
            _device = device;
        }

        internal byte GetVRAMBankRegister()
        {
            return _vRamBank == 1 ? (byte)0xFF : (byte)0xFE;
        }

        internal void SetVRAMBankRegister(byte value)
        {
            if (_device.Mode == DeviceType.DMG) return;

            _vRamBank = (byte)(value & 0x1); // Only bottom bit is important
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

        internal byte[] GetCurrentFrame()
        {
            return _frameBuffer;
        }

        /// <summary>
        /// Internal state to avoid allocation during scanlines, used by sprites to tell whether to draw over bg
        /// </summary>
        private readonly byte[] _scanline = new byte[Device.ScreenWidth * 4];
        private readonly ScanlineBgPriority[] _scanlineBgPriority = new ScanlineBgPriority[Device.ScreenWidth];

        internal void Step()
        {
            if (!_device.LCDRegisters.IsLcdOn) return;

            _currentTCyclesInScanline = (_currentTCyclesInScanline + 4) % ClockCyclesForScanline;

            // Possibly increment the current scanline of the LCD driver
            if (_currentTCyclesInScanline == 0)
            {
                _currentScanline = (_currentScanline + 1) % 154;
            }

            var redrawScanline = SetLCDStatus(_currentScanline, _currentTCyclesInScanline);

            // Don't render on invisible scanlines
            if (_currentScanline < Device.ScreenHeight && redrawScanline)
            {
                // Clear scanline
                for (var ii = 0; ii < Device.ScreenWidth * 4; ii += 4)
                {
                    _scanlineBgPriority[ii / 4] = ScanlineBgPriority.Normal;
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
                    _frameBuffer, _currentScanline * Device.ScreenWidth * 4,
                    _scanline.Length);
            }
        }

        internal (byte[], byte[], byte[], byte[]) DumpVRAM()
        {
            return (_vRamBank0, _vRamBank1, _oamRam, _tileBuffer);
        }

        private readonly Sprite[] _spritesOnLine = new Sprite[10];
        private readonly DMGSpriteComparer _dmgSpriteComparer = new DMGSpriteComparer();
        private void DrawSprites()
        {
            var spriteSize = _device.LCDRegisters.LargeSprites ? 16 : 8;
            var spritesFoundOnLine = 0;

            // First find the first (up to) 10 sprites (counting forwards in memory)
            Array.Clear(_spritesOnLine, 0, _spritesOnLine.Length);
            for (var spriteIndex = 0; spriteIndex < MaxSpritesPerFrame; spriteIndex++)
            {
                if (spritesFoundOnLine == MaxSpritesPerScanline) break;

                var sprite = _sprites[spriteIndex];

                // Ensure that a portion of the sprite lies on the line
                if (_currentScanline < sprite.Y || _currentScanline >= sprite.Y + spriteSize) continue;

                _spritesOnLine[spritesFoundOnLine] = sprite;

                // A sprite is declared on the line even if it turns out later to be transparent
                spritesFoundOnLine++;
            }

            // In DMG mode the priority of sprites is that the least X goes on top of the > X so resort if DMG mode
            if (_device.Mode == DeviceType.DMG)
            {
                Array.Sort(_spritesOnLine, _dmgSpriteComparer);
            }

            // Loop through all sprites
            for (var spriteIndex = _spritesOnLine.Length - 1; spriteIndex >= 0; spriteIndex--)
            {
                var sprite = _spritesOnLine[spriteIndex];
                if (sprite == null) continue;

                var tileNumber = spriteSize == 8 ? sprite.TileNumber : sprite.TileNumber & 0xFE;
                var palette = sprite.UsePalette1
                    ? _device.LCDRegisters.ObjectPaletteData1
                    : _device.LCDRegisters.ObjectPaletteData0;

                var tileAddress = sprite.YFlip ?
                    tileNumber * 16 + (spriteSize - 1 - (_currentScanline - sprite.Y)) * 2 :
                    tileNumber * 16 + (_currentScanline - sprite.Y) * 2;
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
                    var (r, g, b) = _device.Mode == DeviceType.CGB
                        ? _device.LCDRegisters.CGBSpritePalette.Palette[sprite.CGBPaletteNumber * 4 + colorNumber]
                        : _device.LCDRegisters.GetColorFromNumberPalette(colorNumber, palette).BaseRgb();
                    (r, g, b) = _device.Renderer.ColorAdjust(r, g, b);

                    // Don't draw low priority sprites over background unless background is transparent or CGB master override is on
                    if (!_device.LCDRegisters.IsCgbSpriteMasterPriorityOn &&
                        sprite.SpriteToBgPriority == SpriteToBgPriority.BehindColors123 &&
                        _scanlineBgPriority[pixel] == ScanlineBgPriority.Normal) continue;

                    _scanline[pixel * 4 + 3] = 0xFF; // Alpha channel
                    _scanline[pixel * 4 + 2] = r;
                    _scanline[pixel * 4 + 1] = g;
                    _scanline[pixel * 4 + 0] = b;
                }
            }
        }

        private void DrawBackground()
        {
            for (var pixel = 0; pixel < Device.ScreenWidth; pixel++)
            {
                // First figure out which bit of memory we need for the tilemap (pixel X -> tile Y),
                // which bit of memory for the tileset itself (tile Y -> data Z),
                // and which y coordinate (both in tiles and raw pixels) we're talking about.
                //
                // Then determine the x position relative to whether we're in the window or the background
                // taking into account scrolling.
                int xPos, yPos, tileMapAddress;
                if (UsingWindowForScanline && pixel >= _device.LCDRegisters.WindowX - 7)
                {
                    yPos = (_currentScanline - _device.LCDRegisters.WindowY - _windowLinesSkipped) & 0xFF;
                    xPos = (pixel - _device.LCDRegisters.WindowX + 7) & 0xFF;
                    tileMapAddress = _device.LCDRegisters.WindowTileMapOffset;
                    _scanlineUsedWindow = true;
                    _frameUsesWindow = true;
                }
                else
                {
                    yPos = (_currentScanline + _device.LCDRegisters.ScrollY) & 0xFF;
                    xPos = (pixel + _device.LCDRegisters.ScrollX) & 0xFF;
                    tileMapAddress = _device.LCDRegisters.BackgroundTileMapOffset;
                }

                var tileRow = yPos / 8 * 32;
                var tileLine = yPos % 8 * 2;
                var tileCol = xPos / 8;
                var tileNumberAddress = (ushort)((tileMapAddress + tileRow + tileCol) & 0xFFFF);

                var tileNumber = _vRamBank0[tileNumberAddress - 0x8000];

                _tileBuffer[_currentScanline * Device.ScreenWidth + pixel] = tileNumber;

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
                var (r, g, b) = _device.Mode == DeviceType.CGB
                    ? _device.LCDRegisters.CGBBackgroundPalette.Palette[paletteNumber * 4 + colorNumber]
                    : _device.LCDRegisters.GetColorFromNumberPalette(colorNumber, _device.LCDRegisters.BackgroundPaletteData).BaseRgb();
                (r, g, b) = _device.Renderer.ColorAdjust(r, g, b);

                // Finally set the pixel to the appropriate color and flag whether this color can be overwritten by sprites
                _scanline[pixel * 4 + 3] = 0xFF; // Alpha channel
                _scanline[pixel * 4 + 2] = r;
                _scanline[pixel * 4 + 1] = g;
                _scanline[pixel * 4 + 0] = b;

                if (colorNumber == 0) _scanlineBgPriority[pixel] = ScanlineBgPriority.Color0;
                else if (bgToOamPriority == 1) _scanlineBgPriority[pixel] = ScanlineBgPriority.Priority;
                else _scanlineBgPriority[pixel] = ScanlineBgPriority.Normal;
            }

            // Pause the window renderer to cope with skipping window lines
            if (_frameUsesWindow && !_scanlineUsedWindow)
            {
                _windowLinesSkipped++;
            }
        }

        internal void TurnLCDOff()
        {
            _currentTCyclesInScanline = 0x0;
        }

        private int Mode3CyclesOnCurrentLine()
        {
            var cycles = 172 + (_device.LCDRegisters.ScrollX & 0x7);
            var spriteSize = _device.LCDRegisters.LargeSprites ? 16 : 8;
            var spritesFoundOnLine = 0;
            foreach (var sprite in _sprites)
            {
                if (spritesFoundOnLine == MaxSpritesPerScanline - 1) break;
                if (_currentScanline < sprite.Y || _currentScanline >= sprite.Y + spriteSize) continue;

                cycles += 6 + Math.Min(0, 5 - (sprite.X % 8));
                spritesFoundOnLine++;
            }

            return cycles;
        }

        private bool SetLCDStatus(int currentScanLine, int currentTCyclesInScanline)
        {
            // Set the STAT mode correctly
            var oldMode = _device.LCDRegisters.StatMode;

            if (currentScanLine >= Device.ScreenHeight)
            {
                _device.LCDRegisters.StatMode = StatMode.VBlankPeriod;
            }
            else
            {
                _device.LCDRegisters.StatMode = _currentTCyclesInScanline switch
                {
                    _ when _currentTCyclesInScanline < 76 => StatMode.OAMRAMPeriod,
                    _ when _currentTCyclesInScanline < 76 + Mode3CyclesOnCurrentLine() => StatMode.TransferringDataToDriver,
                    _ => StatMode.HBlankPeriod
                };
            }

            if (oldMode != _device.LCDRegisters.StatMode)
            {
                switch (_device.LCDRegisters.StatMode)
                {
                    case StatMode.HBlankPeriod:
                        return true; // Entering HBlank so redraw scanline
                    case StatMode.VBlankPeriod: // Entering VBlank so draw whole screen
                        _device.Renderer.HandleVBlankEvent(_frameBuffer, _device.TCycles);
                        _device.InterruptRegisters.RequestInterrupt(Interrupt.VerticalBlank);

                        // Reset the window to tell it to draw from the top of the screen again
                        _windowLinesSkipped = 0;
                        _frameUsesWindow = false;
                        return false;
                    case StatMode.OAMRAMPeriod:
                        _scanlineUsedWindow = false; // Reset window status as we're moving to a new scanline
                        return false;
                    case StatMode.TransferringDataToDriver:
                        return false;
                    default:
                        throw new ArgumentException($"StatMode {_device.LCDRegisters.StatMode} out of range");
                }
            }

            // Set the LY register to the correct value
            // TODO - Are these quite right in double speed mode?
            if (_device.Type == DeviceType.DMG)
            {
                _device.LCDRegisters.LYRegister = PPUTimingDetails.LYByLineAndClockDMG[currentScanLine][currentTCyclesInScanline / 4];
            }
            else if (_device.Type == DeviceType.CGB && _device.Mode == DeviceType.DMG)
            {
                _device.LCDRegisters.LYRegister = PPUTimingDetails.LYByLineAndClockCGBDMGMode[currentScanLine][currentTCyclesInScanline / 4];
            }
            else if (_device.Type == DeviceType.CGB && _device.Mode == DeviceType.CGB)
            {
                _device.LCDRegisters.LYRegister = PPUTimingDetails.LYByLineAndClockCGBMode[currentScanLine][currentTCyclesInScanline / 4];
            }

            return false;
        }

        #region Utility functions on registers

        private bool UsingWindowForScanline => _device.LCDRegisters.IsWindowEnabled && _currentScanline >= _device.LCDRegisters.WindowY;

        private ushort GetTileDataAddress(byte tileNumber)
        {
            var tilesetAddress = _device.LCDRegisters.BackgroundAndWindowTilesetOffset;
            ushort tileDataAddress;
            if (_device.LCDRegisters.UsingSignedByteForTileData)
            {
                tileDataAddress = (ushort)(tilesetAddress + ((sbyte)tileNumber + 128) * 16);
            }
            else
            {
                tileDataAddress = (ushort)(tilesetAddress + tileNumber * 16);
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
