using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Gameboy.VM.LCD
{
    internal class LCDDriver
    {
        public const int ClockCyclesForScanline = 114; // Note - this is 456/4
        public const int DisplayWidth = 160;
        public const int DisplayHeight = 144;
        public const int MaxSpritesPerScanline = 10;
        public const int MaxSpritesPerFrame = 40;

        private readonly MMU _mmu;
        private readonly LCDRegisters _lcdRegisters;

        private readonly byte[] _frameBuffer = new byte[256 * 256];

        // Current state of LCD driver
        private int _currentCycle;

        internal LCDDriver(MMU mmu, LCDRegisters lcdRegisters)
        {
            _mmu = mmu;
            _lcdRegisters = lcdRegisters;
        }
        
        internal void Clear()
        {
            _currentCycle = 0;
            Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
        }
        
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
            if (currentScanLine < 144)
            {
                // TODO - Actually render the scanline
            }
        }

        private void SetLCDOffValues()
        {
            // TODO - What needs to happen if the LCD isn't on?
            _lcdRegisters.LCDCurrentScanline = 0x0;
        }

        private void SetLCDStatus(byte currentScanLine)
        {
            var oldMode = _lcdRegisters.StatMode;

            if (currentScanLine >= 144)
            {
                _lcdRegisters.StatMode = StatMode.VBlankPeriod;
            }
            else
            {
                _lcdRegisters.StatMode = _currentCycle switch
                {
                    _ when _currentCycle < 80 => StatMode.OAMRAMPeriod,
                    _ when _currentCycle < 252 => StatMode.TransferringDataToDriver,
                    _ => StatMode.HBlankPeriod
                };

                if (oldMode != _lcdRegisters.StatMode)
                {
                    Trace.TraceInformation("LCD Mode changed from {0} to {1}, checking interrupts", oldMode, _lcdRegisters.StatMode);
                    if (_lcdRegisters.Mode0HBlankCheckEnabled && _lcdRegisters.StatMode == StatMode.HBlankPeriod)
                    {
                        Trace.TraceInformation("Triggered HBlank interrupt");
                        // TODO - Trigger HBlank Interrupt
                    }
                    else if (_lcdRegisters.Mode1VBlankCheckEnabled && _lcdRegisters.StatMode == StatMode.VBlankPeriod)
                    {
                        Trace.TraceInformation("Triggered VBlank interrupt");
                        // TODO - Trigger VBlank Interrupt
                    }
                    else if (_lcdRegisters.Mode2OAMCheckEnabled && _lcdRegisters.StatMode == StatMode.OAMRAMPeriod)
                    {
                        Trace.TraceInformation("Triggered OAM Read interrupt");
                        // TODO - Trigger OAM Read Interrupt
                    }
                }
            }
        }
    }
}
