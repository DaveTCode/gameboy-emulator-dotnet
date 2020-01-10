using System;
using System.Diagnostics;
using Gameboy.VM.Interrupts;
using Gameboy.VM.LCD;
using Gameboy.VM.Sound;

namespace Gameboy.VM
{
    internal class MMU
    {
        private const int WRAMSize = 0x2000;
        private const int HRAMSize = 0x7F;
        private const int WaveRAMSize = 0x10;

        private readonly Device _device;

        private readonly byte[] _rom = new byte[0x100];

        private readonly byte[] _workingRam = new byte[WRAMSize];
        private readonly byte[] _hRam = new byte[HRAMSize];
        private readonly byte[] _waveRam = new byte[WaveRAMSize];

        public MMU(in byte[] rom, in Device device)
        {
            _rom = rom;
            _device = device;
        }

        internal void Clear()
        {
            Array.Clear(_workingRam, 0, _workingRam.Length);
            Array.Clear(_hRam, 0, _hRam.Length);
            Array.Clear(_waveRam, 0, _waveRam.Length);
        }

        internal byte ReadByte(ushort address)
        {
            _device.Log.Debug("Reading address {0}", address);

            if (address <= 0xFF)
                return _device.ControlRegisters.RomDisabledRegister == 0
                    ? _rom[address]                     // Read from device ROM if in that state
                    : _device.Cartridge.ReadRom(address);      // Read from the 8kB ROM on the cartridge
            if (address >= 0x0100 && address <= 0x7FFF) // Read from the 8kB ROM on the cartridge
                return _device.Cartridge.ReadRom(address);
            if (address >= 0x8000 && address <= 0x9FFF) // Read from the 8kB Video RAM
            {
                // Video RAM is unreadable by the CPU during STAT mode 3
                if (_device.LCDRegisters.StatMode == StatMode.TransferringDataToDriver)
                {
                    _device.Log.Information("CPU attempted to read VRAM ({0:X2}) during stat mode 3", address);
                    return 0xFF; // May not be 100% correct, based on speculative information on the internet
                }

                return _device.LCDDriver.GetVRAMByte(address);
            }
            if (address >= 0xA000 && address <= 0xBFFF) // Read from MBC RAM on the cartridge
                return _device.Cartridge.ReadRam(address);
            if (address >= 0xC000 && address <= 0xDFFF) // Read from 8kB internal RAM
                return _workingRam[address - 0xC000];
            if (address >= 0xE000 && address <= 0xFDFF) // Read from echo of internal RAM
                return _workingRam[address - 0xE000];
            if (address >= 0xFE00 && address <= 0xFE9F) // Read from sprite attribute table
            {
                // OAM RAM is unreadable by the CPU during STAT mode 2 & 3                                              
                if (_device.LCDRegisters.StatMode == StatMode.OAMRAMPeriod || _device.LCDRegisters.StatMode == StatMode.TransferringDataToDriver)
                {
                    _device.Log.Information("CPU attempted to read OAM RAM ({0:X2}) during stat mode {1}", address, _device.LCDRegisters.StatMode);
                    return 0xFF; // May not be 100% correct, based on speculative information on the internet
                }

                return _device.LCDDriver.GetOAMByte(address);
            }
            if (address >= 0xFEA0 && address <= 0xFEFF) // Unusable addresses
                return ReadUnusedAddress(address);
            if (address == 0xFF00) // P1 Register - TODO
            {
                _device.Log.Error("Port (P1) register not yet implemented");
                return 0x0;
            }
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
                return _device.InterruptRegisters.InterruptRequest;
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
            {
                return 0x0; // TODO - Is this right? Can one read from DMA register?
            }
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
            if (address >= 0xFF4C && address <= 0xFF4F) // Unused addresses (TODO 0xFF4D used in CGB)
                return ReadUnusedAddress(address);
            if (address == 0xFF50) // Is device ROM enabled?
                return _device.ControlRegisters.RomDisabledRegister;
            if (address >= 0xFF51 && address <= 0xFF7F) // Unused addresses (TODO some used in CGB)
                return ReadUnusedAddress(address);
            if (address >= 0xFF80 && address <= 0xFFFE) // Read from HRAM
                return _hRam[address - 0xFF80];
            if (address == 0xFFFF) // Read from the interrupt enable register
                return _device.InterruptRegisters.InterruptEnable;

            throw new ArgumentOutOfRangeException(nameof(address), address, $"Memory address {address:X4} doesn't map to anything");
        }

        private byte ReadUnusedAddress(in ushort address)
        {
            _device.Log.Information("Attempt to read from unused memory location {0:X4}", address);
            return 0x0;
        }

        internal ushort ReadWord(in ushort address) =>
            (ushort)(ReadByte(address) | (ReadByte((ushort)((address + 1) & 0xFFFF)) << 8));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        /// <returns>The number of cpu cycles taken to write</returns>
        internal int WriteByte(in ushort address, in byte value)
        {
            _device.Log.Information("Writing {0:X2} to {1:X4}", value, address);

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
                _workingRam[address - 0xC000] = value;
            else if (address >= 0xE000 && address <= 0xFDFF) // Write to the 8kB internal RAM
                _workingRam[address - 0xE000] = value;
            else if (address >= 0xFE00 && address <= 0xFE9F) // Write to the sprite attribute table
            {
                if (_device.LCDRegisters.StatMode != StatMode.OAMRAMPeriod && _device.LCDRegisters.StatMode != StatMode.TransferringDataToDriver)
                {
                    _device.LCDDriver.WriteOAMByte(address, value);
                }
            }
            else if (address >= 0xFEA0 && address <= 0xFEFF) // Unusable addresses - writes explicitly ignored
                _device.Log.Information("Unusable address {0:X4} for write", address);
            else if (address == 0xFF00) // IO Ports Register - TODO
                _device.Log.Error("IO Ports register not implemented", address);
            else if (address == 0xFF01)
            {
                // TODO - Replace with proper implementation of serial port
                if (_device.ControlRegisters.SerialTransferControl == 0x81)
                {
                    Console.Write(Convert.ToChar(value));
                }
            }
            else if (address == 0xFF02)
                _device.ControlRegisters.SerialTransferControl = value;
            else if (address == 0xFF03)
                _device.Log.Information("Unusable address {0:X4} for write", address);
            else if (address == 0xFF04)
                _device.Timer.Divider = 0x0; // Always reset divider to 0 on write
            else if (address == 0xFF05)
                _device.Timer.TimerCounter = value;
            else if (address == 0xFF06)
                _device.Timer.TimerModulo = value;
            else if (address == 0xFF07)
                _device.Timer.TimerController = value;
            else if (address >= 0xFF08 && address <= 0xFF0E) // Unused addresses
                _device.Log.Information("Unusable address {0:X4} for write", address);
            else if (address == 0xFF0F)
                _device.InterruptRegisters.InterruptRequest = value;
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
                _device.Log.Information("Can't write directly to LY register from MMU");
            else if (address == 0xFF45)
                _device.LCDRegisters.LYCompare = value;
            else if (address == 0xFF46) // DMA register
            {
                _device.DMAController.InitiateDMATransfer(value);
            }
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
            else if (address >= 0xFF4C && address <= 0xFF4F) // Unused addresses (TODO 0xFF4D used in CGB)
                _device.Log.Information("Write to unused address {0:X4}", address);
            else if (address == 0xFF50) // Undocumented register to unmap ROM and map cartridge
                _device.ControlRegisters.RomDisabledRegister = value;
            else if (address >= 0xFF51 && address <= 0xFF7F) // Unused addresses (TODO - some used in CGB)
                _device.Log.Information("Write to unused address {0:X4}", address);
            else if (address >= 0xFF80 && address <= 0xFFFE)  // Write to HRAM
                _hRam[address - 0xFF80] = value;
            else if (address == 0xFFFF) // Write to interrupt enable register
                _device.InterruptRegisters.InterruptEnable = value;
            else
                // Happy to throw an exception and crash here as we should map all addresses
                throw new ArgumentOutOfRangeException(nameof(address), address, $"Address {address:X4} is not mapped");

            return 2;
        }

        /// <summary>
        /// Write a 2 byte value into the specified memory address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        /// <returns>The corresponding number of CPU cycles (4).</returns>
        internal int WriteWord(ushort address, in ushort value)
        {
            return
                WriteByte(address, (byte)(value & 0xFF)) +
                WriteByte((ushort)((address + 1) & 0xFFFF), (byte)(value >> 8));
        }
    }
}
