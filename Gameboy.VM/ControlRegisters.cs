namespace Gameboy.VM
{
    internal class ControlRegisters
    {
        /// <summary>
        /// Undocumented byte at 0xFF50 which is set to 0/1. 0 To enable the boot ROM
        /// and 1 to disable it.
        /// </summary>
        private bool _isRomDisabled;

        internal byte RomDisabledRegister
        {
            get => (byte) (_isRomDisabled ? 0xFF : 0x0);
            set
            {
                if (_isRomDisabled) return;
                _isRomDisabled = value == 0x1;
            }
        }

        // ReSharper disable once InconsistentNaming
        private byte _ff6c = 0xFE;
        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Unused register FF6C, in CGB mode bit 0 is read/write.
        /// </summary>
        internal byte FF6C
        {
            get => _ff6c;
            set => _ff6c = (byte)(value | 0xFE);
        }

        /// <summary>
        /// FF72 - Unused but read/write register
        /// </summary>
        internal byte FF72 { get; set; }

        /// <summary>
        /// FF72 - Unused but read/write register
        /// </summary>
        internal byte FF73 { get; set; }

        /// <summary>
        /// FF72 - Unused but read/write register
        /// </summary>
        internal byte FF74 { get; set; }

        private byte _ff75 = 0b1000_1111;
        /// <summary>
        /// Unused register with bits 4-6 writeable
        /// </summary>
        internal byte FF75
        {
            get => _ff75;
            set => _ff75 = (byte)(value | 0b1000_1111);
        }

        internal bool SpeedSwitchRequested { get; set; }

        // Serial Cable Registers
        internal byte SerialTransferData { get; set; }

        private byte _serialTransferControl = 0b01111110;
        internal byte SerialTransferControl { get => _serialTransferControl; set => _serialTransferControl = (byte) (0b01111110 | value); }

        public override string ToString()
        {
            return $"SB={SerialTransferData:X1},SC={SerialTransferControl:X1},RomDisabled={RomDisabledRegister:X1}";
        }
    }
}
