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

        internal LCDDriver(MMU mmu, ControlRegisters controlRegisters)
        {
            _mmu = mmu;
            _controlRegisters = controlRegisters;
        }
        
        internal void Clear()
        {
            Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
        }
        
        /// <summary>
        /// Proceed by <see cref="cycles"/> number of cycles.
        /// </summary>
        /// <param name="cycles">The number of cycles since the last step was called.</param>
        internal void Step(int cycles)
        {

        }

        private int BgCharacterDataOffset => ((_controlRegisters.LCDControlRegister >> 4) & 0x1) == 0x0 ? 0x8800 : 0x8000;

        private int BgCodeAreaOffset => ((_controlRegisters.LCDControlRegister >> 3) & 0x1) == 0x0 ? 0x9800 : 0x9C00;

        private int WindowCodeAreaOffset => ((_controlRegisters.LCDControlRegister >> 3) & 0x1) == 0x0 ? 0x9800 : 0x9C00;
    }
}
