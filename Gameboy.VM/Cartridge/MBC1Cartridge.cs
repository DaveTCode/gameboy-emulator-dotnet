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

        internal override byte ReadRom(in ushort address)
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

            return 0x0;
        }

        internal override byte ReadRam(in ushort address)
        {
            if (!_isRamEnabled)
            {
                return 0x0;
            }

            if (address < 0xA000 || address >= 0xC000)
            {
                return 0x0;
            }

            return _ramBanks[address - 0xA0000 + _ramBank * RAMSize.BankSizeBytes()];
        }

        internal override void WriteRom(in ushort address, in byte value)
        {
            if (address <= 0x1FFF)
            {
                _isRamEnabled = (value & 0x0F) == 0x0A;
            }
            else if (address >= 0x2000 && address <= 0x3FFF)
            {
                SetRomBank((_romBank & 0b11100000) | (value & 0x1F));
            }
            else if (address >= 0x4000 && address <= 0x5FFF)
            {
                var highBits = value & 0x3;
                if (_mode == MBC1Mode.RAM)
                {
                    _ramBank = highBits % RAMSize.NumberBanks();
                }
                else
                {
                    SetRomBank((_romBank & 0x31) | (value & 0xE0));
                }
            }
            else if (address >= 0x6000 && address <= 0x7FFF)
            {
                if (value == 0x0)
                {
                    _mode = MBC1Mode.ROM;
                }
                else if (value == 0x1)
                {
                    _mode = MBC1Mode.RAM;
                }
            }
        }

        internal override void WriteRam(in ushort address, in byte value)
        {
            var bankedAddress = address - 0xA000 + _ramBank * RAMSize.BankSizeBytes();

            if (bankedAddress > _ramBanks.Length) return; // TODO - Should we do this or does it wrap?

            Contents[bankedAddress] = value;
        }

        private void SetRomBank(in int romBank)
        {
            // Setting ROM bank which isn't present on the cartridge causes it to wrap.
            var bank = romBank % ROMSize.NumberBanks();

            _romBank = bank switch
            {
                0 => 1,
                0x20 => 0x21,
                0x40 => 0x41,
                0x60 => 0x61,
                _ => bank
            };
        }

        private enum MBC1Mode
        {
            ROM,
            RAM
        }
    }
}
