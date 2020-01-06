namespace Gameboy.VM
{
    internal class ControlRegisters
    {
        /// <summary>
        /// Undocumented byte at 0xFF50 which is set to 0/1. 0 To enable the boot ROM
        /// and 1 to disable it.
        /// </summary>
        internal byte RomDisabledRegister { get; set; }

        // Serial Cable Registers
        internal byte SerialTransferData { get; set; }
        internal byte SerialTransferControl { get; set; }

        #region Timer Registers
        internal byte Divider { get; set; }
        internal byte TimerCounter { get; set; }
        internal byte TimerModulo { get; set; }
        internal byte TimerController { get; set; }
        #endregion

        public void Clear()
        {
            SerialTransferData = 0x0;
            SerialTransferControl = 0x0; // TODO - Is this right? Manual says bit 1 always 1 and cannot be changed
            RomDisabledRegister = 0x0;
        }

        public override string ToString()
        {
            return $"SB={SerialTransferData:X1},SC={SerialTransferControl:X1},RomDisabled={RomDisabledRegister:X1}";
        }
    }
}
