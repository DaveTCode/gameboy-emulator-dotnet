namespace Gameboy.VM.Cartridge
{
    internal class RomOnlyCartridge : Cartridge
    {
        public RomOnlyCartridge(in byte[] contents) : base(in contents)
        {
        }

        internal override byte ReadRam(in ushort address)
        {
            return 0x0; // TODO - Not necessarily true, not clear what this would actually do in practice
        }

        internal override void WriteRom(in ushort address, in byte value)
        {
            // NOOP - TODO, is this correct or does it lock up?
        }

        internal override void WriteRam(in ushort address, in byte value)
        {
            // NOOP - TODO, is this correct or does it lock up?
        }
    }
}
