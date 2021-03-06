﻿using System;

namespace Gameboy.VM.LCD
{
    internal class LCDRegisters
    {
        private readonly Device _device;

        internal LCDRegisters(Device device)
        {
            _device = device;

            // Because it isn't possible to turn the background off entirely on a CGB we enable it at startup
            if (_device.Type == DeviceType.CGB)
            {
                IsBackgroundEnabled = true;
            }
        }

        internal byte ScrollX { get; set; }

        internal byte ScrollY { get; set; }

        internal byte WindowX { get; set; } // TODO - Must be > 7

        internal byte WindowY { get; set; }

        internal byte BackgroundPaletteData { get; set; }

        internal byte ObjectPaletteData0 { get; set; }

        internal byte ObjectPaletteData1 { get; set; }

        #region CGB Registers

        internal CGBPalette CGBBackgroundPalette { get; } = new CGBPalette();

        internal CGBPalette CGBSpritePalette { get; } = new CGBPalette();
        #endregion

        internal byte LYRegister { get; set; }

        private byte _lyCompare;
        internal byte LYCompare
        {
            get => _lyCompare;
            set
            {
                _lyCompare = value;

                UpdateStatIRQSignal();
            }
        }

        #region PaletteData Utilities

        internal Grayscale GetColorFromNumberPalette(int colorNumber, byte paletteData) =>
            colorNumber switch
            {
                0 => (Grayscale)(paletteData & 0x3),
                1 => (Grayscale)((paletteData >> 2) & 0x3),
                2 => (Grayscale)((paletteData >> 4) & 0x3),
                3 => (Grayscale)((paletteData >> 6) & 0x3),
                _ => throw new ArgumentOutOfRangeException(nameof(colorNumber), colorNumber,
                    "Color number out of range (0-3)")
            };

        #endregion

        #region LCD Control Register
        internal bool IsLcdOn { get; private set; }
        internal int WindowTileMapOffset { get; private set; }
        internal bool IsWindowEnabled { get; private set; }
        internal int BackgroundAndWindowTilesetOffset { get; private set; }
        internal bool UsingSignedByteForTileData { get; private set; }
        internal int BackgroundTileMapOffset { get; private set; }
        internal bool LargeSprites { get; private set; }
        internal bool AreSpritesEnabled { get; private set; }
        internal bool IsBackgroundEnabled { get; private set; }
        internal bool IsCgbSpriteMasterPriorityOn { get; private set; }

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
                LargeSprites = (value & 0x4) == 0x4; // Bit 2 on LCDC register controls how large sprites are
                AreSpritesEnabled = (value & 0x2) == 0x2; // Bit 1 on LCDC register controls whether to display sprites

                switch (_device.Type)
                {
                    // Bit 0 acts differently on DMG/CGB
                    case DeviceType.DMG:
                        // Bit 0 on LCDC register controls whether to display the background in DMG devices
                        IsBackgroundEnabled = (value & 0x1) == 0x1;
                        break;
                    case DeviceType.CGB when _device.Mode == DeviceType.CGB:
                        IsCgbSpriteMasterPriorityOn = (value & 0x1) == 0x0;
                        break;
                    case DeviceType.CGB when _device.Mode == DeviceType.DMG:
                        // Bit 0 on LCDC register controls whether to display the background and window on CGB devices in DMG mode
                        IsBackgroundEnabled = (value & 0x1) == 0x1;
                        IsWindowEnabled = IsBackgroundEnabled;
                        break;
                }

                if (!IsLcdOn)
                {
                    _device.LCDDriver.TurnLCDOff();

                    LYRegister = 0x0;
                    StatMode = StatMode.HBlankPeriod;
                    _statIRQSignal = false;
                }
                else
                {
                    UpdateStatIRQSignal();
                }
            }
        }
        #endregion

        #region STAT Register
        private byte _statRegister = 0x80; // Default top bit to set
        private bool _statIRQSignal;

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

                UpdateStatIRQSignal();
            }
        }

        private void UpdateStatIRQSignal()
        {
            if (!IsLcdOn) return;
            var oldStatIRQSignal = _statIRQSignal;

            CoincidenceFlag = _lyCompare == LYRegister;

            _statIRQSignal = (IsLYLCCheckEnabled && LYRegister == _lyCompare) ||
                             (StatMode == StatMode.HBlankPeriod && Mode0HBlankCheckEnabled) ||
                             (StatMode == StatMode.OAMRAMPeriod && Mode2OAMCheckEnabled) ||
                             (StatMode == StatMode.VBlankPeriod && (Mode1VBlankCheckEnabled || Mode2OAMCheckEnabled));

            if (!oldStatIRQSignal && _statIRQSignal)
            {
                _device.InterruptRegisters.RequestInterrupt(Interrupts.Interrupt.LCDSTAT);
            }
        }

        internal bool IsLYLCCheckEnabled { get; private set; }

        internal bool Mode2OAMCheckEnabled { get; private set; }

        internal bool Mode1VBlankCheckEnabled { get; private set; }

        internal bool Mode0HBlankCheckEnabled { get; private set; }

        private bool _coincidenceFlag;
        internal bool CoincidenceFlag  // Bit 2 of the STAT register refers to the Coincidence flag which is readonly and reflects LY = LYC
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

                UpdateStatIRQSignal();
            }
        }
        #endregion

        public override string ToString()
        {
            return $"LCDC:{LCDControlRegister:X1}, STAT:{StatRegister:X1}, SCX:{ScrollX:X1}, SCY:{ScrollY:X1}, LY:{LYRegister:X1}, LYC:{LYCompare:X1}, BGP:{BackgroundPaletteData:X1}, OBP0:{ObjectPaletteData0:X1}, OBP1:{ObjectPaletteData1:X1}, WY:{WindowY:X1}, WX:{WindowX:X1}";
        }
    }
}
