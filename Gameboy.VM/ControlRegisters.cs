namespace Gameboy.VM
{
    internal class ControlRegisters
    {
        /// <summary>
        /// Undocumented byte at 0xFF50 which is set to 0/1. 0 To enable the boot ROM
        /// and 1 to disable it.
        /// </summary>
        internal byte RomDisabledRegister;

        public void Clear()
        {
            RomDisabledRegister = 0x0;
        }
    }
}
