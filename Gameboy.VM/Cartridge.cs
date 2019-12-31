using System;
using System.Collections.Generic;
using System.Text;

namespace Gameboy.VM
{
    internal class Cartridge
    {
        private readonly byte[] _contents;

        public Cartridge(in byte[] contents)
        {
            _contents = contents;
        }

        public byte ReadByte(in ushort address) => _contents[address];
    }
}
