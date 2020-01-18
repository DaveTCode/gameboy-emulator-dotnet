using System;

namespace Gameboy.VM.Cartridge
{
    internal class MBC5Cartridge : Cartridge
    {
        private byte _ROMB0; // Lower ROM Bank register
        private byte _ROMB1; // Upper ROM Bank register
        private int _romBank; // Combined ROM bank from above;

        public MBC5Cartridge(byte[] contents) : base(contents)
        {
            _romBank = 1;
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

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1FFF)
            {
                IsRamEnabled = (value & 0x0F) == 0x0A;
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
                RamBank = (value & 8) % RAMSize.NumberBanks();
            }
        }
    }
}
