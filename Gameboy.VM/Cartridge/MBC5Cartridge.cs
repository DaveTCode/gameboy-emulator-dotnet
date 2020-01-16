using System;

namespace Gameboy.VM.Cartridge
{
    internal class MBC5Cartridge : Cartridge
    {
        private bool _isRamEnabled; // Is write/read enabled to external RAM?

        private byte _ROMB0; // Lower ROM Bank register
        private byte _ROMB1; // Upper ROM Bank register
        private int _romBank; // Combined ROM bank from above;

        private int _ramBank;
        private readonly byte[] _ramBanks;

        public MBC5Cartridge(byte[] contents) : base(contents)
        {
            _isRamEnabled = false;
            _romBank = 1;
            _ramBank = 0;
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

            return 0x0;
        }

        internal override byte ReadRam(ushort address)
        {
            // Is RAM enabled
            if (!_isRamEnabled) return 0xFF;

            // Is the address mappable
            if (address < 0xA000 || address >= 0xC000) throw new ArgumentOutOfRangeException(nameof(address), address, $"Can't access RAM at address {address}");

            return _ramBanks[address - 0xA000 + _ramBank * RAMSize.BankSizeBytes()];
        }

        internal override void WriteRom(ushort address, in byte value)
        {
            if (address <= 0x1FFF)
            {
                _isRamEnabled = (value & 0x0F) == 0x0A;
            }
            else if (address >= 0x2000 && address <= 0x2FFF)
            {
                _ROMB0 = value;
                _romBank = ((_ROMB1 << 8) | _ROMB0) % ROMSize.NumberBanks();
            }
            else if (address >= 0x3000 && address <= 0x3FFF)
            {
                _ROMB1 = (byte)(value & 0b00000001); // Only bottom 1 bit is used AFAICT
                _romBank = ((_ROMB1 << 8) | _ROMB0) % ROMSize.NumberBanks();
            }
            else if (address >= 0x4000 && address <= 0x5FFF)
            {
                _ramBank = (value & 8) % RAMSize.NumberBanks();
            }
        }

        internal override void WriteRam(ushort address, in byte value)
        {
            if (!_isRamEnabled) return; // Don't accept writes if RAM disabled

            var bankedAddress = address - 0xA000 + _ramBank * RAMSize.BankSizeBytes();

            if (bankedAddress > _ramBanks.Length) return; // TODO - Should we do this or does it wrap?

            _ramBanks[bankedAddress] = value;
        }
    }
}
