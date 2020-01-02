using System;

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

        // Timer Registers
        internal byte TimerCounter { get; set; }
        internal byte TimerModulo { get; set; }
        internal byte TimerController { get; set; }

        // Interrupt Registers
        internal byte InterruptRequest { get; set; }
        internal byte InterruptEnable { get; set; }

        // LCD Registers
        internal byte ScrollX { get; set; }
        internal byte ScrollY { get; set; }
        internal byte WindowX { get; set; }
        internal byte WindowY { get; set; }
        internal byte BackgroundPaletteData { get; set; }
        internal byte ObjectPaletteData0 { get; set; }
        internal byte ObjectPaletteData1 { get; set; }
        internal byte LineDataBeingProcessed { get; set; }
        internal byte LCDControlRegister { get; set; }
        internal byte StatRegister { get; set; }

        #region STAT Register

        internal StatMode StatMode => (StatMode)(StatRegister & 0x03);

        internal void SetStatMode(StatMode mode)
        {
            // ReSharper disable once RedundantCast - Actually necessary or CSC error
            StatRegister &= (byte)(mode switch
            {
                StatMode.AllAccessEnabled => 0b11111100,
                StatMode.VBlankPeriod => 0b11111101,
                StatMode.OAMRAMPeriod => 0b11111110,
                StatMode.TransferringDataToDriver => 0b11111111,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            });
        }

        #endregion

        public void Clear()
        {
            SerialTransferData = 0x0;
            SerialTransferControl = 0x0; // TODO - Is this right? Manual says bit 1 always 1 and cannot be changed
            RomDisabledRegister = 0x0;
            ScrollX = 0x0;
            ScrollY = 0x0;
            WindowX = 0x0;
            WindowY = 0x0;
            BackgroundPaletteData = 0x0;
            ObjectPaletteData0 = 0x0;
            ObjectPaletteData1 = 0x0;
            LineDataBeingProcessed = 0x0;
            LCDControlRegister = 0x0;
            StatRegister = 0x0;
        }

        public override string ToString()
        {
            return $"SB={SerialTransferData:X1},SC={SerialTransferControl:X1}";
        }
    }
    
    internal enum StatMode
    {
        AllAccessEnabled,
        VBlankPeriod,
        OAMRAMPeriod,
        TransferringDataToDriver
    }
}
