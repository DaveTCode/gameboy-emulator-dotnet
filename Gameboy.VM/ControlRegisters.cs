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

        #region Timer Registers
        internal byte Divider { get; set; }
        internal byte TimerCounter { get; set; }
        internal byte TimerModulo { get; set; }
        internal byte TimerController { get; set; }
        #endregion

        #region Interrupt Registers
        internal byte InterruptRequest { get; set; }
        internal byte InterruptEnable { get; set; }
        #endregion

        #region LCD Registers
        internal byte ScrollX { get; set; }
        internal byte ScrollY { get; set; }
        internal byte WindowX { get; set; }
        internal byte WindowY { get; set; }
        internal byte BackgroundPaletteData { get; set; }
        internal byte ObjectPaletteData0 { get; set; }
        internal byte ObjectPaletteData1 { get; set; }
        internal byte LCDCurrentScanline { get; private set; }
        private byte _lyCompare;
        internal byte LYCompare
        {
            get
            {
                return _lyCompare;
            }
            set
            {
                _lyCompare = value;
                // TODO - Should trigger interrupt if LYC = LC and LYC/LC check enabled in stat register? When does that happen?
            }
        }
        internal byte LCDControlRegister { get; set; }
        private byte _statRegister;
        internal byte StatRegister
        {
            get
            {
                var s = _statRegister;
                if (!IsLcdOn)
                {
                    s &= 0xFC; // Bit 0 & 1 are always 0 if the LCD is turned off
                }
                s &= (byte)((LYCompare == LCDCurrentScanline) ? 0xFF : 0xFB); // Bit 2 is set based on whether LYC = LY

                return s;
            }
            set
            {
                var s = value | 0x80; // Bit 7 is always set
                s &= 0xF8; // Unset bits 0-2 as we shouldn't touch those
                _statRegister = (byte)(value | s);
            }
        }

        private bool IsLcdOn => (LCDControlRegister & 0x80) == 0x80; // Bit 7 set on LCDC register determines whether the LCD is on or not
        private int WindowTileMapOffset => ((LCDControlRegister & 0x40) == 0x40) ? 0x9C00 : 0x9800; // Bit 6 on LCDC register controls which memory location contains the window tile map
        private bool IsWindowEnabled => (LCDControlRegister & 0x20) == 0x20; // Bit 5 set on LCDC register controls whether the window overlay is displayed
        private int BackgroundAndWindowTilesetOffset => ((LCDControlRegister & 0x10) == 0x10) ? 0x8000 : 0x8800; // Bit 4 on LCDC register controls which memory location contains the background and window tileset
        private int BackgroundTilemapOffset => ((LCDControlRegister & 0x8) == 0x8) ? 0x9C00 : 0x9800; // Bit 3 on LCDC register controls which memory location contains the backgroudn tilemap
        private int SpriteHeight => ((LCDControlRegister & 0x4) == 0x4) ? 16 : 8; // Bit 2 on LCDC register controls which memory location contains the window tile map
        private bool IsSpritesEnabled => (LCDControlRegister & 0x2) == 0x2; // Bit 1 on LCDC register controls whether to display sprites
        private bool IsBackgroundEnabled => (LCDControlRegister & 0x1) == 0x1; // Bit 0 on LCDC register controls whether to display the background

        internal bool IncrementLineBeingProcessed()
        {
            var overflow = LCDCurrentScanline == byte.MaxValue;
            LCDCurrentScanline = (byte)((LCDCurrentScanline + 1) & 0xFF);
            // TODO - Should trigger an interrupt if it matches LYCompare
            return overflow;
        }
        #endregion

        #region STAT Register

        private bool IsLYLCCheckEnabled => (StatRegister & 0x40) == 0x40; // Is bit 6 of STAT register on?
        private bool Mode2OAMCheckEnabled => (StatRegister & 0x20) == 0x20; // Is bit 5 of STAT register on?
        private bool Mode1VBlankCheckEnabled => (StatRegister & 0x10) == 0x10; // Is bit 4 of STAT register on?
        private bool Mode0HBlankCheckEnabled => (StatRegister & 0x8) == 0x8; // Is bit 3 of STAT register on?

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
            LCDCurrentScanline = 0x0;
            LYCompare = 0x0;
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
