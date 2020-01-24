using System;
using Gameboy.VM.LCD;

namespace Gameboy.VM
{
    internal class MMU
    {
        private const int WRAMSizeDmg = 0x2000;
        private const int WRAMSizeCgb = 0x8000;
        private const int HRAMSize = 0x7F;
        private const int WaveRAMSize = 0x10;

        private readonly Device _device;

        private readonly byte[] _rom;

        private readonly byte[] _workingRam;
        private readonly byte[] _hRam = new byte[HRAMSize];
        private readonly byte[] _waveRam = new byte[WaveRAMSize];
        
        private byte _wramBank = 1;

        public MMU(byte[] rom, Device device)
        {
            _rom = rom;
            _device = device;

            _workingRam = device.Mode switch
            {
                DeviceType.DMG => new byte[WRAMSizeDmg],
                DeviceType.CGB => new byte[WRAMSizeCgb],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        internal byte ReadByte(ushort address)
        {
            if (address <= 0xFF)
                return _device.ControlRegisters.RomDisabledRegister == 0
                    ? _rom[address]                     // Read from device ROM if in that state
                    : _device.Cartridge.ReadRom(address);      // Read from the 8kB ROM on the cartridge
            if (address <= 0x7FFF) // Read from the 8kB ROM on the cartridge
                return _device.Cartridge.ReadRom(address);
            if (address <= 0x9FFF) // Read from the 8kB Video RAM
            {
                // Video RAM is unreadable by the CPU during STAT mode 3
                if (_device.LCDRegisters.StatMode == StatMode.TransferringDataToDriver)
                {
                    _device.Log.Information("CPU attempted to read VRAM ({0:X2}) during stat mode 3", address);
                    return 0xFF; // May not be 100% correct, based on speculative information on the internet
                }

                return _device.LCDDriver.GetVRAMByte(address);
            }
            if (address <= 0xBFFF) // Read from MBC RAM on the cartridge
                return _device.Cartridge.ReadRam(address);
            if (address <= 0xDFFF) // Read from WRAM
                return ReadFromRam(address);
            if (address <= 0xFDFF) // Read from echo of internal RAM
                return ReadFromRam((ushort) (address - 0x2000));
            if (address <= 0xFE9F) // Read from sprite attribute table
            {
                // OAM RAM is unreadable by the CPU during STAT mode 2 & 3 and during OAM DMA
                if (_device.LCDRegisters.StatMode == StatMode.OAMRAMPeriod || _device.LCDRegisters.StatMode == StatMode.TransferringDataToDriver || _device.DMAController.BlocksOAMRAM())
                {
                    return 0xFF; // May not be 100% correct, based on speculative information on the internet
                }

                return _device.LCDDriver.GetOAMByte(address);
            }
            if (address <= 0xFEFF) // Unusable addresses
                return ReadUnusedAddress(address);
            if (address == 0xFF00) // P1 Register - Joypad input
                return _device.JoypadHandler.P1Register;
            if (address == 0xFF01) // SB register
                return _device.ControlRegisters.SerialTransferData;
            if (address == 0xFF02) // SC register
                return _device.ControlRegisters.SerialTransferControl;
            if (address == 0xFF03) // Unused address - all reads return 0
                return ReadUnusedAddress(address);
            if (address == 0xFF04) // Divider
                return _device.Timer.Divider;
            if (address == 0xFF05) // Timer Counter
                return _device.Timer.TimerCounter;
            if (address == 0xFF06) // Timer Modulo
                return _device.Timer.TimerModulo;
            if (address == 0xFF07) // TAC Register
                return _device.Timer.TimerController;
            if (address >= 0xFF08 && address <= 0xFF0E) // Unused addresses - all reads return 0
                return ReadUnusedAddress(address);
            if (address == 0xFF0F) // IF Register
                return _device.InterruptRegisters.InterruptFlags;
            if (address >= 0xFF10 && address <= 0xFF26) // Sound registers
                return _device.SoundRegisters.ReadFromRegister(address);
            if (address >= 0xFF27 && address <= 0xFF2F) // Unused addresses - all reads return 0
                return ReadUnusedAddress(address);
            if (address >= 0xFF30 && address <= 0xFF3F) // Wave Pattern RAM
                return _waveRam[address - 0xFF30];
            if (address == 0xFF40) // LCDC Register
                return _device.LCDRegisters.LCDControlRegister;
            if (address == 0xFF41) // STAT Register
                return _device.LCDRegisters.StatRegister;
            if (address == 0xFF42) // SCY Register
                return _device.LCDRegisters.ScrollY;
            if (address == 0xFF43) // SCX Register
                return _device.LCDRegisters.ScrollX;
            if (address == 0xFF44) // LY Register
                return _device.LCDRegisters.LCDCurrentScanline;
            if (address == 0xFF45) // LYC Register
                return _device.LCDRegisters.LYCompare;
            if (address == 0xFF46) // DMA Register
                return _device.DMAController.DMA;
            if (address == 0xFF47) // Background Palette Register
                return _device.LCDRegisters.BackgroundPaletteData;
            if (address == 0xFF48) // Object 0 Palette Register
                return _device.LCDRegisters.ObjectPaletteData0;
            if (address == 0xFF49) // Object 1 Palette Register
                return _device.LCDRegisters.ObjectPaletteData1;
            if (address == 0xFF4A) // WY Register
                return _device.LCDRegisters.WindowY;
            if (address == 0xFF4B) // WX Register
                return _device.LCDRegisters.WindowX;
            if (address >= 0xFF4C && address <= 0xFF4E) // Unused addresses (TODO 0xFF4D used in CGB)
                return ReadUnusedAddress(address);
            if (address == 0xFF4F)
                return _device.LCDDriver.GetVRAMBankRegister();
            if (address == 0xFF50) // Is device ROM enabled?
                return _device.ControlRegisters.RomDisabledRegister;
            if (address == 0xFF68) // BCPS register
                return _device.LCDRegisters.CGBBackgroundPalette.PaletteIndex;
            if (address == 0xFF69) // BCPD register
                return _device.LCDRegisters.CGBBackgroundPalette.ReadPaletteMemory();
            if (address == 0xFF6A) // OCPS register
                return _device.LCDRegisters.CGBSpritePalette.PaletteIndex;
            if (address == 0xFF6B) // OCPD register
                return _device.LCDRegisters.CGBSpritePalette.ReadPaletteMemory();
            if (address == 0xFF70) // RAM Bank register
                return _wramBank;
            if (address >= 0xFF51 && address <= 0xFF7F) // Unused addresses (TODO some used in CGB)
                return ReadUnusedAddress(address);
            if (address >= 0xFF80 && address <= 0xFFFE) // Read from HRAM
                return _hRam[address - 0xFF80];
            if (address == 0xFFFF) // Read from the interrupt enable register
                return _device.InterruptRegisters.InterruptEnable;

            throw new ArgumentOutOfRangeException(nameof(address), address, $"Memory address {address:X4} doesn't map to anything");
        }

        private byte ReadUnusedAddress(ushort address)
        {
            _device.Log.Warning("Attempt to read from unused memory location {0:X4}", address);
            return 0xFF;
        }

        internal ushort ReadWord(ushort address) =>
            (ushort)(ReadByte(address) | (ReadByte((ushort)((address + 1) & 0xFFFF)) << 8));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        /// <returns>The number of cpu cycles taken to write</returns>
        internal int WriteByte(ushort address, byte value)
        {
            //_device.Log.Information("Writing {0:X2} to {1:X4}", value, address);

            if (address <= 0x7FFF) // Write to the 8kB ROM on the cartridge
                _device.Cartridge.WriteRom(address, value);
            else if (address >= 0x8000 && address <= 0x9FFF) // Write to the 8kB Video RAM
            {
                if (_device.LCDRegisters.StatMode != StatMode.TransferringDataToDriver)
                {
                    _device.LCDDriver.WriteVRAMByte(address, value);
                }
            }
            else if (address >= 0xA000 && address <= 0xBFFF) // Write to the MBC RAM on the cartridge - TODO
                _device.Cartridge.WriteRam(address, value);
            else if (address >= 0xC000 && address <= 0xDFFF) // Write to the 8kB internal RAM
                WriteToRam(address, value);
            else if (address >= 0xE000 && address <= 0xFDFF) // Write to the 8kB internal RAM
                WriteToRam((ushort)(address - 0x2000), value);
            else if (address >= 0xFE00 && address <= 0xFE9F) // Write to the sprite attribute table
            {
                if (_device.LCDRegisters.StatMode != StatMode.OAMRAMPeriod && _device.LCDRegisters.StatMode != StatMode.TransferringDataToDriver && !_device.DMAController.BlocksOAMRAM())
                {
                    _device.LCDDriver.WriteOAMByte(address, value);
                }
            }
            else if (address >= 0xFEA0 && address <= 0xFEFF) // Unusable addresses - writes explicitly ignored
                _device.Log.Information("Unusable address {0:X4} for write", address);
            else if (address == 0xFF00) // IO Ports Register
                _device.JoypadHandler.P1Register = value;
            else if (address == 0xFF01)
            {
                // TODO - Replace with proper implementation of serial port
                if (_device.ControlRegisters.SerialTransferControl == 0x81)
                {
                    Console.Write(Convert.ToChar(value));
                    _device.ControlRegisters.SerialTransferData = value;
                    _device.Log.Information("Wrote character `{0}` to serial port", Convert.ToChar(value));
                }
            }
            else if (address == 0xFF02)
                _device.ControlRegisters.SerialTransferControl = value;
            else if (address == 0xFF03)
                _device.Log.Information("Unusable address {0:X4} for write", address);
            else if (address == 0xFF04)
                _device.Timer.Divider = value;
            else if (address == 0xFF05)
                _device.Timer.TimerCounter = value;
            else if (address == 0xFF06)
                _device.Timer.TimerModulo = value;
            else if (address == 0xFF07)
                _device.Timer.TimerController = value;
            else if (address >= 0xFF08 && address <= 0xFF0E) // Unused addresses
                _device.Log.Information("Unusable address {0:X4} for write", address);
            else if (address == 0xFF0F)
                _device.InterruptRegisters.InterruptFlags = value;
            else if (address >= 0xFF10 && address <= 0xFF26)
                _device.SoundRegisters.WriteToRegister(address, value);
            else if (address >= 0xFF27 && address <= 0xFF2F) // Unused addresses
                _device.Log.Information("Unusable address {0:X4} for write", address);
            else if (address >= 0xFF30 && address <= 0xFF3F) // Waveform RAM
                _waveRam[address - 0xFF30] = value;
            else if (address == 0xFF40)
                _device.LCDRegisters.LCDControlRegister = value;
            else if (address == 0xFF41)
                _device.LCDRegisters.StatRegister = value;
            else if (address == 0xFF42)
                _device.LCDRegisters.ScrollY = value;
            else if (address == 0xFF43)
                _device.LCDRegisters.ScrollX = value;
            else if (address == 0xFF44)
                _device.LCDRegisters.LCDCurrentScanline = value;
            else if (address == 0xFF45)
                _device.LCDRegisters.LYCompare = value;
            else if (address == 0xFF46) // DMA register
                _device.DMAController.DMA = value;
            else if (address == 0xFF47)
                _device.LCDRegisters.BackgroundPaletteData = value;
            else if (address == 0xFF48)
                _device.LCDRegisters.ObjectPaletteData0 = value;
            else if (address == 0xFF49)
                _device.LCDRegisters.ObjectPaletteData1 = value;
            else if (address == 0xFF4A)
                _device.LCDRegisters.WindowY = value;
            else if (address == 0xFF4B)
                _device.LCDRegisters.WindowX = value;
            else if (address >= 0xFF4C && address <= 0xFF4E) // Unused addresses (TODO 0xFF4D used in CGB)
                _device.Log.Information("Write to unused address {0:X4}", address);
            else if (address == 0xFF4F)
                _device.LCDDriver.SetVRAMBankRegister(value);
            else if (address == 0xFF50) // Undocumented register to unmap ROM and map cartridge
                _device.ControlRegisters.RomDisabledRegister = value;
            else if (address == 0xFF51) // HDMA1
                _device.DMAController.HDMA1 = value;
            else if (address == 0xFF52) // HDMA2
                _device.DMAController.HDMA2 = value;
            else if (address == 0xFF53) // HDMA3
                _device.DMAController.HDMA3 = value;
            else if (address == 0xFF54) // HDMA4
                _device.DMAController.HDMA4 = value;
            else if (address == 0xFF55) // HDMA5
                _device.DMAController.HDMA5 = value;
            else if (address == 0xFF68)
                _device.LCDRegisters.CGBBackgroundPalette.PaletteIndex = value;
            else if (address == 0xFF69)
                _device.LCDRegisters.CGBBackgroundPalette.WritePaletteMemory(value);
            else if (address == 0xFF6A)
                _device.LCDRegisters.CGBSpritePalette.PaletteIndex = value;
            else if (address == 0xFF6B)
                _device.LCDRegisters.CGBSpritePalette.WritePaletteMemory(value);
            else if (address == 0xFF70) // RAM Bank register - only bits 0-2 valid
                _wramBank = (byte)(value & 0x7);
            else if (address >= 0xFF51 && address <= 0xFF7F) // Unused addresses (TODO - some used in CGB)
                _device.Log.Information("Write to unused address {0:X4}", address);
            else if (address >= 0xFF80 && address <= 0xFFFE)  // Write to HRAM
                _hRam[address - 0xFF80] = value;
            else if (address == 0xFFFF) // Write to interrupt enable register
                _device.InterruptRegisters.InterruptEnable = value;
            else
                // Happy to throw an exception and crash here as we should map all addresses
                throw new ArgumentOutOfRangeException(nameof(address), address, $"Address {address:X4} is not mapped");

            return 8;
        }

        /// <summary>
        /// Write a 2 byte value into the specified memory address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        /// <returns>The corresponding number of CPU cycles (4).</returns>
        internal int WriteWord(ushort address, ushort value)
        {
            return
                WriteByte(address, (byte)(value & 0xFF)) +
                WriteByte((ushort)((address + 1) & 0xFFFF), (byte)(value >> 8));
        }

        private void WriteToRam(ushort address, byte value)
        {
            if (_device.Mode == DeviceType.DMG || address < 0xD000)
            {
                _workingRam[address - 0xC000] = value;
                return;
            }

            _workingRam[address - 0xD000 + _wramBank * 0x1000] = value;
        }

        private byte ReadFromRam(ushort address)
        {
            if (_device.Mode == DeviceType.DMG || address < 0xD000)
            {
                return _workingRam[address - 0xC000];
            }

            return _workingRam[address - 0xD000 + _wramBank * 0x1000];
        }
    }
}
