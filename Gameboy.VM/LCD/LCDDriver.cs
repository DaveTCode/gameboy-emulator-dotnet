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

        private readonly byte[] _vRam = new byte[VRAMSize]; // TODO - Implement CGB vram banks
        private readonly byte[] _oamRam = new byte[OAMRAMSize];

        private readonly Grayscale[] _frameBuffer = new Grayscale[Device.ScreenHeight * Device.ScreenWidth];

        // Current state of LCD driver
        private int _currentCycle;

        internal LCDDriver(in Device device)
        {
            _device = device;
        }

        internal void Clear()
        {
            _currentCycle = 0;
            Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
            Array.Clear(_vRam, 0, _vRam.Length);
            Array.Clear(_oamRam, 0, _oamRam.Length);
        }

        internal byte GetVRAMByte(in ushort address)
        {
            if (address < 0x8000 || address > 0x9FFF) throw new ArgumentOutOfRangeException(nameof(address), address, "VRAM read with invalid address");

            return _vRam[address - 0x8000];
        }

        internal void WriteVRAMByte(in ushort address, in byte value)
        {
            if (address < 0x8000 || address > 0x9FFF) throw new ArgumentOutOfRangeException(nameof(address), address, "VRAM write with invalid address");

            _vRam[address - 0x8000] = value;
        }

        internal byte GetOAMByte(in ushort address)
        {
            if (address < 0xFE00 || address > 0xFE9F) throw new ArgumentOutOfRangeException(nameof(address), address, "OAM read with invalid address");

            return _oamRam[address - 0xFE00];
        }

        internal void WriteOAMByte(in ushort address, in byte value)
        {
            if (address < 0xFE00 || address > 0xFE9F) throw new ArgumentOutOfRangeException(nameof(address), address, "OAM read with invalid address");

            _oamRam[address - 0xFE00] = value;
        }

        internal Grayscale[] GetCurrentFrame()
        {
            return _frameBuffer;
        }

        /// <summary>
        /// Proceed by <see cref="cycles"/> number of cycles.
        ///
        /// Note that this is all a bit sketchy IMO. It's only drawing whole
        /// scanlines at a time instead of doing pixel based timings. But I
        /// don't know enough to achieve that yet
        /// </summary>
        /// <param name="cycles">The number of cycles since the last step was called.</param>
        internal void Step(in int cycles)
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
                if (_device.LCDRegisters.IsBackgroundEnabled)
                {
                    DrawBackground();
                }

                if (_device.LCDRegisters.AreSpritesEnabled)
                {
                    // TODO - Draw sprites
                }
            }
        }

        internal byte[] DumpVRAM()
        {
            return _vRam;
        }

        private void DrawBackground()
        {
            // First figure out which bit of memory we need for the tilemap (pixel X -> tile Y),
            // which bit of memory for the tileset itself (tile Y -> data Z),
            // and which y coordinate (both in tiles and raw pixels) we're talking about
            var tileMapAddress = UsingWindowForScanline ? _device.LCDRegisters.WindowTileMapOffset : _device.LCDRegisters.BackgroundTileMapOffset;
            var yPosition = UsingWindowForScanline ? _device.LCDRegisters.LCDCurrentScanline - _device.LCDRegisters.WindowY : _device.LCDRegisters.LCDCurrentScanline + _device.LCDRegisters.ScrollY;
            var tileRow = (yPosition / 8) * 32;
            var tileLine = (yPosition % 8) * 2;

            for (var pixel = 0; pixel < Device.ScreenWidth; pixel++)
            {
                // Determine the x position relative to whether we're in the window or the background
                // taking into account scrolling.
                var xPos = (UsingWindowForScanline && pixel >= _device.LCDRegisters.WindowX) ?
                    pixel - _device.LCDRegisters.WindowX :
                    pixel + _device.LCDRegisters.ScrollX;

                var tileCol = xPos / 8;

                var tileNumberAddress = (ushort)((tileMapAddress + tileRow + tileCol) & 0xFFFF);

                var tileNumber = _vRam[tileNumberAddress - 0x8000];

                var tileDataAddress = GetTileDataAddress(tileNumber);
                var byte1 = _vRam[(tileDataAddress + tileLine) & 0xFFFF - 0x8000];
                var byte2 = _vRam[(tileDataAddress + tileLine + 1) & 0xFFFF - 0x8000];

                // Convert the tile data spread over two bytes into the
                // specific color value for this pixel.
                var colorBit = ((xPos % 8) - 7) * -1;
                var colorBitMask = 1 << colorBit;
                var colorNumber =
                    (byte2 & colorBitMask) == colorBitMask ? 2 : 0 +
                    (byte1 & colorBitMask) == colorBitMask ? 1 : 0;

                // Retrieve the actual color to be used from the palette
                var color = _device.LCDRegisters.GetColorFromNumber(colorNumber);

                // Finally set the pixel to the appropriate color
                _frameBuffer[_device.LCDRegisters.LCDCurrentScanline * Device.ScreenWidth + pixel] = color;
            }
        }

        private void SetLCDOffValues()
        {
            // TODO - What needs to happen if the LCD isn't on?
            _device.LCDRegisters.ResetCurrentScanline();
        }

        private bool SetLCDStatus(in byte currentScanLine)
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
                            _device.InterruptRegisters.RequestInterrupt(Interrupt.LCDSTAT); // TODO - Is this the right interrupt?
                        }
                        return true; // Entering HBlank so redraw scanline
                    case StatMode.VBlankPeriod:
                        if (_device.LCDRegisters.Mode1VBlankCheckEnabled)
                        {
                            _device.InterruptRegisters.RequestInterrupt(Interrupt.VerticalBlank);
                        }
                        return false;
                    case StatMode.OAMRAMPeriod:
                        if (_device.LCDRegisters.Mode2OAMCheckEnabled)
                        {
                            _device.InterruptRegisters.RequestInterrupt(Interrupt.LCDSTAT); // TODO - Is this the right interrupt?
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

        private bool UsingWindowForScanline => _device.LCDRegisters.IsWindowEnabled && _device.LCDRegisters.LCDCurrentScanline <= _device.LCDRegisters.WindowY;

        private ushort GetTileDataAddress(in byte tileNumber)
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
