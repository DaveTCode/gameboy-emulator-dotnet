namespace Gameboy.VM.Cartridge
{
    internal class RomOnlyCartridge : Cartridge
    {
        public RomOnlyCartridge(byte[] contents) : base(contents)
        {
        }

        // TODO - Some documentation suggests that it's possible for a Rom only cartridge to have a single RAM bank?
        internal override byte ReadRam(ushort address)
        {
            return 0xFF;
        }

        internal override void WriteRom(ushort address, byte value)
        {
        }

        internal override void WriteRam(ushort address, byte value)
        {
        }
    }
}
