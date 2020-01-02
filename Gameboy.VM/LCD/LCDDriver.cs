using System;

namespace Gameboy.VM.LCD
{
    internal class LCDDriver
    {
        public const int DisplayWidth = 160;
        public const int DisplayHeight = 144;
        public const int MaxSpritesPerScanline = 10;

        private readonly MMU _mmu;
        private readonly ControlRegisters _controlRegisters;

        private readonly byte[] _frameBuffer = new byte[256 * 256];

        // Current state of LCD driver
        private int _line;
        private StatMode _mode = StatMode.OAMRAMPeriod;

        internal LCDDriver(MMU mmu, ControlRegisters controlRegisters)
        {
            _mmu = mmu;
            _controlRegisters = controlRegisters;
        }
        
        internal void Clear()
        {
            _line = 0;
            Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
        }
        
        /// <summary>
        /// Proceed by <see cref="cycles"/> number of cycles.
        /// </summary>
        /// <param name="cycles">The number of cycles since the last step was called.</param>
        internal void Step(int cycles)
        {
            for (var cycle = 0; cycle < cycles; cycle++)
            {
                DoSingleCycle();
            }
        }

        private void DoSingleCycle()
        {

        }
    }
}
