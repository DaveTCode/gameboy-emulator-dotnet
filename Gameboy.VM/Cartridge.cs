using System;

namespace Gameboy.VM
{
    internal class Cartridge
    {
        internal const int CartridgeSize = 0x8000;
        private readonly byte[] _contents = new byte[CartridgeSize];

        public Cartridge(in byte[] contents)
        {
            // If the cartridge is smaller than the available address space all remaining entries are zeroed
            Array.Clear(_contents, 0, _contents.Length);
            Array.Copy(contents, 0, _contents, 0, contents.Length);
        }

        public byte ReadByte(in ushort address) => _contents[address];
    }
}
