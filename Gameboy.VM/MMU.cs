using System;
using System.Diagnostics;

namespace Gameboy.VM
{
    internal class MMU
    {
        private const int WRAMSize = 0x1FFF;
        private const int HRAMSize = 0x7E;

        private readonly byte[] _workingRam = new byte[WRAMSize];
        private readonly byte[] _hRam = new byte[HRAMSize];

        internal void Clear()
        {
            Array.Clear(_workingRam, 0, _workingRam.Length);
        }

        internal byte ReadByte(ushort address)
        {
            Trace.WriteLine($"Reading from {address}");

            return address switch
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                _ when (address >= 0x0000) && (address <= 0x7FFF) => 0x0, // Read from the 8kB ROM on the cartridge - TODO
                _ when (address >= 0x8000) && (address <= 0x9FFF) => 0x0, // Read from the 8kB Video RAM - TODO
                _ when (address >= 0xA000) && (address <= 0xBFFF) => 0x0, // Read from MBC RAM on the cartridge - TODO
                _ when (address >= 0xC000) && (address <= 0xDFFF) => _workingRam[address - 0xC000], // Read from 8kB internal RAM
                _ when (address >= 0xE000) && (address <= 0xFDFF) => _workingRam[address - 0xE000], // Read from echo of internal RAM
                _ when (address >= 0xFE00) && (address <= 0xFE9F) => 0x0, // Read from sprite attribute table - TODO
                _ when (address >= 0xFEA0) && (address <= 0xFEFF) => 0x0, // Unusable addresses
                _ when (address >= 0xFF00) && (address <= 0xFF7F) => 0x0, // I/O Ports - TODO
                _ when (address >= 0xFF80) && (address <= 0xFFFE) => _hRam[address - 0xFF80], // Read from HRAM
                _ when (address == 0xFFFF) => 0x0, // Read from the interrupt enable register - TODO
                _ => throw new NotImplementedException($"Memory address {address} doesn't map to anything"),
            };
        }

        internal ushort ReadWord(ushort address) => (ushort)((ReadByte(address) << 8) | (ReadByte(address)));

        internal void WriteByte(ushort address, byte value)
        {
            Trace.WriteLine($"Writing {value} to {address}");
            switch(address)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                case var a when (address >= 0x0000) && (address <= 0x7FFF):
                    // Write to the 8kB ROM on the cartridge - TODO
                    break;
                case var a when (address >= 0x8000) && (address <= 0x9FFF):
                    // Write to the 8kB Video RAM - TODO
                    break;
                case var a when (address >= 0xA000) && (address <= 0xBFFF):
                    // Write to the MBC RAM on the cartridge - TODO
                    break;
                case var a when (address >= 0xC000) && (address <= 0xDFFF):
                    // Write to the 8kB internal RAM
                    _workingRam[address - 0xC000] = value;
                    break;
                case var a when (address >= 0xE000) && (address <= 0xFDFF):
                    // Write to the 8kB internal RAM
                    _workingRam[address - 0xE000] = value;
                    break;
                case var a when (address >= 0xFE00) && (address <= 0xFE9F):
                    // Write to the sprite attribute table - TODO
                    break;
                case var a when (address >= 0xFEA0) && (address <= 0xFEFF):
                    // Unusable addresses
                    break;
                case var a when (address >= 0xFF00) && (address <= 0xFF7F):
                    // I/O Ports - TODO
                    break;
                case var a when (address >= 0xFF80) && (address <= 0xFFFE):
                    // Write to HRAM
                    _hRam[address - 0xFF80] = value;
                    break;
                case 0xFFFF:
                    // Write to interrupt enable register - TODO
                    break;
            };
        }
    }
}
