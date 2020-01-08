using System;

namespace Gameboy.VM.LCD
{
    internal class LCDRegisters
    {
        internal byte ScrollX { get; set; }

        internal byte ScrollY { get; set; }

        internal byte WindowX { get; set; } // TODO - Must be > 7

        internal byte WindowY { get; set; }

        internal byte BackgroundPaletteData { get; set; }

        internal byte ObjectPaletteData0 { get; set; }

        internal byte ObjectPaletteData1 { get; set; }

        // Note - Only settable from LCDDriver, not from memory address
        internal byte LCDCurrentScanline { get; set; }

        internal byte IncrementLineBeingProcessed()
        {
            LCDCurrentScanline = (byte)((LCDCurrentScanline + 1) & 0xFF);
            return LCDCurrentScanline;
        }

        internal byte LCDControlRegister { get; set; }

        // TODO - Should trigger interrupt if LYC = LC and LYC/LC check enabled in stat register? When does that happen?
        internal byte LYCompare { get; set; }

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
                s &= (byte)(LYCompare == LCDCurrentScanline ? 0xFF : 0xFB); // Bit 2 is set based on whether LYC = LY

                return s;
            }
            set
            {
                var s = value | 0x80; // Bit 7 is always set
                s &= 0xF8; // Unset bits 0-2 as we shouldn't touch those
                _statRegister = (byte)(value | s);
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

        internal bool IsLcdOn => (LCDControlRegister & 0x80) == 0x80; // Bit 7 set on LCDC register determines whether the LCD is on or not
        internal int WindowTileMapOffset => (LCDControlRegister & 0x40) == 0x40 ? 0x9C00 : 0x9800; // Bit 6 on LCDC register controls which memory location contains the window tile map
        internal bool IsWindowEnabled => (LCDControlRegister & 0x20) == 0x20; // Bit 5 set on LCDC register controls whether the window overlay is displayed
        internal int BackgroundAndWindowTilesetOffset => (LCDControlRegister & 0x10) == 0x10 ? 0x8000 : 0x8800; // Bit 4 on LCDC register controls which memory location contains the background and window tileset
        internal bool UsingSignedByteForTileData => BackgroundAndWindowTilesetOffset == 0x8800; // Bit 4 also determines whether the tile relative address is signed or unsigned
        internal int BackgroundTilemapOffset => (LCDControlRegister & 0x8) == 0x8 ? 0x9C00 : 0x9800; // Bit 3 on LCDC register controls which memory location contains the background tilemap
        internal int SpriteHeight => (LCDControlRegister & 0x4) == 0x4 ? 16 : 8; // Bit 2 on LCDC register controls which memory location contains the window tile map
        internal bool IsSpritesEnabled => (LCDControlRegister & 0x2) == 0x2; // Bit 1 on LCDC register controls whether to display sprites
        internal bool IsBackgroundEnabled => (LCDControlRegister & 0x1) == 0x1; // Bit 0 on LCDC register controls whether to display the background

        internal bool IsLYLCCheckEnabled => (StatRegister & 0x40) == 0x40; // Is bit 6 of STAT register on?
        internal bool Mode2OAMCheckEnabled => (StatRegister & 0x20) == 0x20; // Is bit 5 of STAT register on?
        internal bool Mode1VBlankCheckEnabled => (StatRegister & 0x10) == 0x10; // Is bit 4 of STAT register on?
        internal bool Mode0HBlankCheckEnabled => (StatRegister & 0x8) == 0x8; // Is bit 3 of STAT register on?

        internal StatMode StatMode
        {
            get => (StatMode)((StatRegister & 0x03) | 0xFC);
            set => StatRegister &= (byte)value;
        }

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

        public override string ToString()
        {
            return $"LCDC:{LCDControlRegister:X1}, STAT{StatRegister:X1}, SCX:{ScrollX:X1}, SCY:{ScrollY:X1}, LY:{LCDCurrentScanline:X1}, LYC:{LYCompare:X1}, BGP:{BackgroundPaletteData:X1}, OBP0:{ObjectPaletteData0:X1}, OBP1:{ObjectPaletteData1:X1}, WY:{WindowY:X1}, WX:{WindowX:X1}";
        }
    }
}
