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
        private readonly byte[] _ramBanks;

        public MBC1Cartridge(in byte[] contents) : base(in contents)
        {
            _isRamEnabled = false;
            _romBank = 1;
            _ramBank = 0;
            _mode = MBC1Mode.ROM;
            _ramBanks = new byte[RAMSize.NumberBanks() * RAMSize.BankSizeBytes()];
        }

        internal override byte ReadRom(ushort address)
        {
            if (address < RomBankSizeBytes) // Fixed bank 0
            {
                // TODO - What's the correct behaviour if the ROM is smaller than the addressable space? 0x0, wrap or panic?
                return address >= Contents.Length ? (byte)0x0 : Contents[address];
            }

            if (address < RomBankSizeBytes * 2) // Switchable ROM banks
            {
                var bankAddress = address + (_romBank - 1) * RomBankSizeBytes;
                // TODO - What's the correct behaviour if the ROM is smaller than the addressable space? 0x0, wrap or panic?
                return bankAddress >= Contents.Length ? (byte)0x0 : Contents[bankAddress];
            }

            Trace.TraceWarning("Attempt to read address {0} from MBC ROM which isn't mapped", address);
            return 0x0;
        }

        internal override byte ReadRam(ushort address)
        {
            if (!_isRamEnabled)
            {
                Trace.TraceWarning("Attempt to access RAM at address {0} whilst it was disabled", address);
                return 0x0;
            }

            if (address < 0xA000 || address >= 0xC000)
            {
                Trace.TraceWarning("Attempt to access MBC RAM at address {0} which doesn't map to MBC RAM", address);
                return 0x0;
            }

            return _ramBanks[address - 0xA0000 + _ramBank * RAMSize.BankSizeBytes()];
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1FFF)
            {
                _isRamEnabled = (value & 0x0F) == 0x0A;
                //Trace.TraceInformation("RAM enabled is now {0} by setting {1} to {2}", _isRamEnabled, value, address);
            }
            else if (address >= 0x2000 && address <= 0x3FFF)
            {
                SetRomBank((_romBank & 0b11100000) | (value & 0x1F));
                //Trace.TraceInformation("Selecting ROM Bank {0} by setting {1} to {2}", _romBank, value, address);
            }
            else if (address >= 0x4000 && address <= 0x5FFF)
            {
                var highBits = value & 0x3;
                if (_mode == MBC1Mode.RAM)
                {
                    _ramBank = highBits % RAMSize.NumberBanks();
                    //Trace.TraceInformation("Switched RAM bank to {0} by setting {1} to {2}", _ramBank, value, address);
                }
                else
                {
                    SetRomBank((_romBank & 0x31) | (value & 0xE0));
                    //Trace.TraceInformation("Switched ROM bank to {0} by setting {1} to {2}", _ramBank, value, address);
                }
            }
            else if (address >= 0x6000 && address <= 0x7FFF)
            {
                if (value == 0x0)
                {
                    _mode = MBC1Mode.ROM;
                    //Trace.TraceInformation("Switching MBC1 bank mode to RAM by write of {0} to {1}", value, address);
                }
                else if (value == 0x1)
                {
                    _mode = MBC1Mode.RAM;
                    //Trace.TraceInformation("Switching MBC1 bank mode to RAM by write of {0} to {1}", value, address);
                }
                else
                {
                    //Trace.TraceWarning("Value {0} at address {1} written to select MBC1 bank mode but only 0,1 are accepted", value, address);
                }
            }
        }

        internal override void WriteRam(ushort address, byte value)
        {
            var bankedAddress = address - 0xA000 + _ramBank * RAMSize.BankSizeBytes();

            if (bankedAddress > _ramBanks.Length) return; // TODO - Should we do this or does it wrap?

            Contents[bankedAddress] = value;
        }

        private void SetRomBank(int romBank)
        {
            // Setting ROM bank which isn't present on the cartridge causes it to wrap.
            romBank %= ROMSize.NumberBanks();
            
            _romBank = romBank switch
            {
                0 => 1,
                0x20 => 0x21,
                0x40 => 0x41,
                0x60 => 0x61,
                _ => romBank
            };
        }

        private enum MBC1Mode
        {
            ROM,
            RAM
        }
    }
}
