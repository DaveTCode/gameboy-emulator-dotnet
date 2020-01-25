namespace Gameboy.VM.Cartridge
{
    internal class MBC5Cartridge : Cartridge
    {
        private byte _romb0; // Lower ROM Bank register
        private byte _romb1; // Upper ROM Bank register

        public MBC5Cartridge(byte[] contents) : base(contents)
        {
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1FFF)
            {
                IsRamEnabled = (value & 0x0F) == 0x0A;
            }
            else if (address >= 0x2000 && address <= 0x2FFF)
            {
                _romb0 = value;
                RomBank = ((_romb1 << 8) | _romb0) % ROMSize.NumberBanks();
            }
            else if (address >= 0x3000 && address <= 0x3FFF)
            {
                _romb1 = (byte)(value & 0b00000001); // Only bottom 1 bit is used AFAICT
                RomBank = ((_romb1 << 8) | _romb0) % ROMSize.NumberBanks();
            }
            else if (address >= 0x4000 && address <= 0x5FFF)
            {
                if (RAMSize == CartridgeRAMSize.None) RamBank = 0;
                else RamBank = (value & 8) % RAMSize.NumberBanks();
            }
        }
    }
}
