namespace Gameboy.VM.Cartridge
{
    internal class RomOnlyCartridge : Cartridge
    {
        public RomOnlyCartridge(in byte[] contents) : base(in contents)
        {
        }

        internal override byte ReadRam(ushort address)
        {
            return 0x0; // TODO - Not necessarily true, not clear what this would actually do in practice
        }

        internal override void WriteRom(ushort address, byte value)
        {
            // NOOP - TODO, is this correct or does it lock up?
        }

        internal override void WriteRam(ushort address, byte value)
        {
            // NOOP - TODO, is this correct or does it lock up?
        }
    }
}
