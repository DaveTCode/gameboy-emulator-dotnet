using System;
using System.Diagnostics;

namespace Gameboy.VM.Cartridge
{
    internal class MBC1Cartridge : Cartridge
    {
        private bool _isRamEnabled;
        private int _romBank;
        private int _ramBank;
        private MBC1Mode _mode;

        public MBC1Cartridge(in byte[] contents) : base(in contents)
        {
            _isRamEnabled = false;
            _romBank = 1;
            _ramBank = 0;
            _mode = MBC1Mode.ROM;
        }

        internal override byte ReadRom(ushort address)
        {
            if (address <= 0x3FFF) // Fixed bank 0
            {
                return Contents[address];
            }
            else if (address <= 0x7FFF) // Switchable ROM banks
            {

            }
        }

        internal override byte ReadRam(ushort address)
        {
            throw new NotImplementedException();
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1FFF)
            {
                _isRamEnabled = (value & 0x0F) == 0x0A;
                Trace.TraceInformation("RAM enabled is now {0} by setting {1} to {2}", _isRamEnabled, value, address);
            }
            else if (address >= 0x2000 && address <= 0x3FFF)
            {
                _romBank = ((_romBank & 0b11100000) | (value & 0x1F)) switch
                {
                    0 => 1,
                    0x20 => 0x21,
                    0x40 => 0x41,
                    0x60 => 0x61,
                    _ => value
                };
                Trace.TraceInformation("Selecting ROM Bank {0} by setting {1} to {2}", _romBank, value, address);
            }
            else if (address >= 0x4000 && address <= 0x5FFF)
            {
                var highBits = value >> 6;
                // TODO - Allow selection of high bits of the rom bank
            }
            else if (address >= 0x6000 && address <= 0x7FFF)
            {
                if (value == 0x0)
                {
                    _mode = MBC1Mode.ROM;
                    Trace.TraceInformation("Switching MBC1 bank mode to RAM by write of {0} to {1}", value, address);
                }
                else if (value == 0x1)
                {
                    _mode = MBC1Mode.RAM;
                    Trace.TraceInformation("Switching MBC1 bank mode to RAM by write of {0} to {1}", value, address);
                }
                else
                {
                    Trace.TraceWarning("Value {0} at address {1} written to select MBC1 bank mode but only 0,1 are accepted", value, address);
                }
            }
        }

        internal override void WriteRam(ushort address, byte value)
        {
            throw new NotImplementedException();
        }

        private enum MBC1Mode
        {
            ROM,
            RAM
        }
    }
}
