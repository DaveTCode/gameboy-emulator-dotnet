namespace Gameboy.VM.Cartridge
{
    internal class MBC2Cartridge : Cartridge
    {
        private const int MBC2RamSize = 0x200;

        private readonly byte[] _ram = new byte[MBC2RamSize];

        public MBC2Cartridge(byte[] contents) : base(contents)
        {
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address < 0x4000)
            {
                if ((address & 0x100) == 0x100) // LSB of upper byte = 1 to select ROM bank
                {
                    RomBank = value & 0b1111; // Lower 4 bits select ROM bank
                    RomBank = RomBank == 0 ? 1 : RomBank; // But it can't be 0
                }
                else
                {
                    IsRamEnabled = (value & 0x0F) == 0x0A;
                }
            }
        }

        internal override void WriteRam(ushort address, byte value)
        {
            if (!IsRamEnabled) return;

            // Only lower 4 bits are stored and max size of RAM space is 
            _ram[(address - RamAddressStart) % MBC2RamSize] = (byte) ((value & 0b1111) | 0b11110000);
        }

        internal override byte ReadRam(ushort address)
        {
            if (!IsRamEnabled) return 0xFF;

            return _ram[(address - RamAddressStart) % MBC2RamSize];
        }
    }
}
