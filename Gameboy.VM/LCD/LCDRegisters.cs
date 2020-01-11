using System;

namespace Gameboy.VM.LCD
{
    internal class LCDRegisters
    {
        private readonly Device _device;

        internal LCDRegisters(in Device device)
        {
            _device = device;
        }

        internal byte ScrollX { get; set; }

        internal byte ScrollY { get; set; }

        internal byte WindowX { get; set; } // TODO - Must be > 7

        internal byte WindowY { get; set; }

        internal byte BackgroundPaletteData { get; set; }

        internal byte ObjectPaletteData0 { get; set; }

        internal byte ObjectPaletteData1 { get; set; }

        // TODO - This can be set by the program during normal operation when the LCD is off
        private byte _lcdCurrentScanline;
        internal byte LCDCurrentScanline
        {
            get => _lcdCurrentScanline;
            set
            {
                _lcdCurrentScanline = value;
                _coincidenceFlag = value == LYCompare;

                CheckLYLCInterrupt();
            }
        }

        internal byte IncrementLineBeingProcessed()
        {
            LCDCurrentScanline = (byte)((LCDCurrentScanline + 1) & 0xFF);
            return LCDCurrentScanline;
        }

        internal byte ResetCurrentScanline()
        {
            LCDCurrentScanline = 0x0;
            return LCDCurrentScanline;
        }

        private byte _lyCompare;
        internal byte LYCompare
        {
            get => _lyCompare;
            set
            {
                _lyCompare = value;
                _coincidenceFlag = _lyCompare == _lcdCurrentScanline;

                CheckLYLCInterrupt();
            }
        }

        #region BackgroundPaletteData Utilities

        internal Grayscale GetColorFromNumber(int colorNumber) => colorNumber switch
        {
            0 => (Grayscale)(BackgroundPaletteData & 0x3),
            1 => (Grayscale)((BackgroundPaletteData >> 2) & 0x3),
            2 => (Grayscale)((BackgroundPaletteData >> 4) & 0x3),
            3 => (Grayscale)((BackgroundPaletteData >> 6) & 0x3),
            _ => throw new ArgumentOutOfRangeException(nameof(colorNumber), colorNumber, "Color number out of range (0-3)")
        };

        #endregion

        #region LCD Control Register
        internal bool IsLcdOn { get; private set; }
        internal int WindowTileMapOffset { get; private set; }
        internal bool IsWindowEnabled { get; private set; }
        internal int BackgroundAndWindowTilesetOffset { get; private set; }
        internal bool UsingSignedByteForTileData { get; private set; }
        internal int BackgroundTileMapOffset { get; private set; }
        internal int SpriteHeight { get; private set; }
        internal bool AreSpritesEnabled { get; private set; }
        internal bool IsBackgroundEnabled { get; private set; }

        private byte _lcdControlRegister;
        internal byte LCDControlRegister
        {
            get => _lcdControlRegister;
            set
            {
                _lcdControlRegister = value;
                IsLcdOn = (value & 0x80) == 0x80; // Bit 7 set on LCDC register determines whether the LCD is on or not
                WindowTileMapOffset = (value & 0x40) == 0x40 ? 0x9C00 : 0x9800; // Bit 6 on LCDC register controls which memory location contains the window tile map
                IsWindowEnabled = (value & 0x20) == 0x20; // Bit 5 set on LCDC register controls whether the window overlay is displayed
                BackgroundAndWindowTilesetOffset = (value & 0x10) == 0x10 ? 0x8000 : 0x8800; // Bit 4 on LCDC register controls which memory location contains the background and window tileset
                UsingSignedByteForTileData = BackgroundAndWindowTilesetOffset == 0x8800; // Bit 4 also determines whether the tile relative address is signed or unsigned
                BackgroundTileMapOffset = (value & 0x8) == 0x8 ? 0x9C00 : 0x9800; // Bit 3 on LCDC register controls which memory location contains the background tilemap
                SpriteHeight = (value & 0x4) == 0x4 ? 16 : 8; // Bit 2 on LCDC register controls how large sprites are
                AreSpritesEnabled = (value & 0x2) == 0x2; // Bit 1 on LCDC register controls whether to display sprites
                IsBackgroundEnabled = (value & 0x1) == 0x1; // Bit 0 on LCDC register controls whether to display the background
            }
        }
        #endregion

        #region STAT Register
        private byte _statRegister = 0x80; // Default top bit to set

        internal byte StatRegister
        {
            get => _statRegister;
            set
            {
                var s = value | 0x80; // Bit 7 is always set
                s &= 0xF8; // Unset bits 0-2 as we shouldn't touch those
                _statRegister = (byte)s;
                IsLYLCCheckEnabled = (value & 0x40) == 0x40; // Is bit 6 of STAT register on?
                Mode2OAMCheckEnabled = (value & 0x20) == 0x20; // Is bit 5 of STAT register on?
                Mode1VBlankCheckEnabled = (value & 0x10) == 0x10; // Is bit 4 of STAT register on?
                Mode0HBlankCheckEnabled = (value & 0x8) == 0x8; // Is bit 3 of STAT register on?

                CheckLYLCInterrupt();
            }
        }

        internal bool IsLYLCCheckEnabled { get; private set; }

        internal bool Mode2OAMCheckEnabled { get; private set; }

        internal bool Mode1VBlankCheckEnabled { get; private set; }

        internal bool Mode0HBlankCheckEnabled { get; private set; }

        private bool _coincidenceFlag;
        internal bool CoincidenceFlag  // Bit 2 of the STAT register refers to the Coincidence flag which is readonly and reflect LY = LYC
        {
            get => _coincidenceFlag;
            private set
            {
                _coincidenceFlag = value;
                if (value)
                {
                    _statRegister |= 0x4;
                }
                else
                {
                    _statRegister &= 0xFB;
                }
            }
        }

        private StatMode _statMode;
        internal StatMode StatMode // Bit 0,1 of the STAT register refer to the STAT mode
        {
            get => _statMode;
            set
            {
                _statMode = value;
                _statRegister = (byte)((_statRegister & 0xFC) | (int)value);
            }
        }
        #endregion

        internal void Clear()
        {
            LCDControlRegister = 0x0;
            StatRegister = 0x0;
            ScrollY = 0x0;
            ScrollX = 0x0;
            LCDCurrentScanline = 0x0;
            LYCompare = 0x0;
            BackgroundPaletteData = 0x0;
            ObjectPaletteData0 = 0x0;
            ObjectPaletteData1 = 0x0;
            WindowY = 0x0;
            WindowX = 0x7; // Values 0-6 should never be set for WindowX - TODO, what happens if they are?
        }

        private void CheckLYLCInterrupt()
        {
            if (IsLYLCCheckEnabled && _lcdCurrentScanline == _lyCompare)
            {
                _device.InterruptRegisters.RequestInterrupt(Interrupts.Interrupt.LCDSTAT);
            }
        }

        public override string ToString()
        {
            return $"LCDC:{LCDControlRegister:X1}, STAT:{StatRegister:X1}, SCX:{ScrollX:X1}, SCY:{ScrollY:X1}, LY:{LCDCurrentScanline:X1}, LYC:{LYCompare:X1}, BGP:{BackgroundPaletteData:X1}, OBP0:{ObjectPaletteData0:X1}, OBP1:{ObjectPaletteData1:X1}, WY:{WindowY:X1}, WX:{WindowX:X1}";
        }
    }
}
