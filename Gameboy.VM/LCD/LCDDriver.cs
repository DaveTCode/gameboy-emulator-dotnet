using System;
using System.Diagnostics;
using Gameboy.VM.Interrupts;

namespace Gameboy.VM.LCD
{
    internal class LCDDriver
    {
        public const int ClockCyclesForScanline = 114; // Note - this is 456/4
        public const int MaxSpritesPerScanline = 10;
        public const int MaxSpritesPerFrame = 40;

        private readonly MMU _mmu;
        private readonly LCDRegisters _lcdRegisters;
        private readonly InterruptRegisters _interruptRegisters;

        private readonly Grayscale[] _frameBuffer = new Grayscale[256 * 256];

        // Current state of LCD driver
        private int _currentCycle;

        internal LCDDriver(in MMU mmu, in LCDRegisters lcdRegisters, in InterruptRegisters interruptRegisters)
        {
            _mmu = mmu;
            _lcdRegisters = lcdRegisters;
            _interruptRegisters = interruptRegisters;
        }

        internal void Clear()
        {
            _currentCycle = 0;
            Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
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
            if (!_lcdRegisters.IsLcdOn)
            {
                SetLCDOffValues();
                return;
            }

            _currentCycle += cycles;

            var currentScanLine = _lcdRegisters.LCDCurrentScanline;

            if (_currentCycle >= ClockCyclesForScanline)
            {
                _currentCycle -= ClockCyclesForScanline;

                // Update the LY register - TODO might trigger an LY==LYC interrupt
                currentScanLine = _lcdRegisters.IncrementLineBeingProcessed();
            }

            SetLCDStatus(currentScanLine);

            // Don't render on invisible scanlines
            if (currentScanLine < Device.ScreenHeight)
            {
                if (_lcdRegisters.IsBackgroundEnabled)
                {
                    DrawBackground();
                }

                if (_lcdRegisters.IsSpritesEnabled)
                {
                    // TODO - Draw sprites
                }
            }
        }

        private void DrawBackground()
        {
            // First figure out which bit of memory we need for the tilemap (pixel X -> tile Y),
            // which bit of memory for the tileset itself (tile Y -> data Z),
            // and which y coordinate (both in tiles and raw pixels) we're talking about
            var tileMapAddress = UsingWindowForScanline ? _lcdRegisters.WindowTileMapOffset : _lcdRegisters.BackgroundTilemapOffset;
            var yPosition = UsingWindowForScanline ? _lcdRegisters.LCDCurrentScanline - _lcdRegisters.WindowY : _lcdRegisters.LCDCurrentScanline + _lcdRegisters.ScrollY;
            var tileRow = (yPosition / 8) * 32;
            var tileLine = (yPosition % 8) * 2;

            for (var pixel = 0; pixel < Device.ScreenWidth; pixel++)
            {
                // Determine the x position relative to whether we're in the window or the background
                // taking into account scrolling.
                var xPos = (UsingWindowForScanline && pixel >= _lcdRegisters.WindowX) ?
                    pixel - _lcdRegisters.WindowX :
                    pixel + _lcdRegisters.ScrollX;

                var tileCol = xPos / 8;

                var tileNumberAddress = (ushort)((tileMapAddress + tileRow + tileCol) & 0xFFFF);

                var tileNumber = _mmu.ReadByte(tileNumberAddress);

                var tileDataAddress = GetTileDataAddress(tileNumber);
                var byte1 = _mmu.ReadByte((ushort)((tileDataAddress + tileLine) & 0xFFFF));
                var byte2 = _mmu.ReadByte((ushort)((tileDataAddress + tileLine + 1) & 0xFFFF));

                // Convert the tile data spread over two bytes into the
                // specific color value for this pixel.
                var colorBit = (((xPos % 8) - 7) * -1);
                var colorBitMask = 1 << (colorBit - 1);
                var colorNumber =
                    (byte2 & colorBitMask) == colorBitMask ? 2 : 0 +
                    (byte1 & colorBitMask) == colorBitMask ? 1 : 0;

                // Retrieve the actual color to be used from the palette
                var color = _lcdRegisters.GetColorFromNumber(colorNumber);

                // Finally set the pixel to the appropriate color
                _frameBuffer[_lcdRegisters.LCDCurrentScanline * Device.ScreenWidth + pixel] = color;
            }
        }

        private void SetLCDOffValues()
        {
            // TODO - What needs to happen if the LCD isn't on?
            _lcdRegisters.LCDCurrentScanline = 0x0;
        }

        private void SetLCDStatus(in byte currentScanLine)
        {
            var oldMode = _lcdRegisters.StatMode;

            if (currentScanLine >= Device.ScreenHeight)
            {
                _lcdRegisters.StatMode = StatMode.VBlankPeriod;
            }
            else
            {
                _lcdRegisters.StatMode = _currentCycle switch
                {
                    _ when _currentCycle < 80 => StatMode.OAMRAMPeriod,
                    _ when _currentCycle < 252 => StatMode.TransferringDataToDriver, // TODO - Not strictly true, depends on #sprites
                    _ => StatMode.HBlankPeriod
                };

                if (oldMode != _lcdRegisters.StatMode)
                {
                    //Trace.TraceInformation("LCD Mode changed from {0} to {1}, checking interrupts", oldMode, _lcdRegisters.StatMode);
                    if (_lcdRegisters.Mode0HBlankCheckEnabled && _lcdRegisters.StatMode == StatMode.HBlankPeriod)
                    {
                        //Trace.TraceInformation("Triggered HBlank interrupt");
                        _interruptRegisters.RequestInterrupt(Interrupt.LCDSTAT); // TODO - Is this the right interrupt?
                    }
                    else if (_lcdRegisters.Mode1VBlankCheckEnabled && _lcdRegisters.StatMode == StatMode.VBlankPeriod)
                    {
                        //Trace.TraceInformation("Triggered VBlank interrupt");
                        _interruptRegisters.RequestInterrupt(Interrupt.VerticalBlank);
                    }
                    else if (_lcdRegisters.Mode2OAMCheckEnabled && _lcdRegisters.StatMode == StatMode.OAMRAMPeriod)
                    {
                        //Trace.TraceInformation("Triggered OAM Read interrupt");
                        _interruptRegisters.RequestInterrupt(Interrupt.LCDSTAT); // TODO - Is this the right interrupt?
                    }
                }
            }
        }

        #region Utility functions on registers

        private bool UsingWindowForScanline => _lcdRegisters.IsWindowEnabled && _lcdRegisters.LCDCurrentScanline <= _lcdRegisters.WindowY;

        private ushort GetTileDataAddress(in byte tileNumber)
        {
            var tilesetAddress = _lcdRegisters.BackgroundAndWindowTilesetOffset;
            ushort tileDataAddress;
            if (_lcdRegisters.UsingSignedByteForTileData)
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
