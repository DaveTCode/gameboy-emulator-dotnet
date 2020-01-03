using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Gameboy.VM.Cartridge
{
    public class CartridgeFactory
    {
        public Cartridge CreateCartridge(byte[] contents)
        {
            if (contents.Length < 0x150)
            {
                throw new ArgumentOutOfRangeException(nameof(contents), contents, "Cartridge contents must be more than 0x150 bytes to include the whole header");
            }

            return contents[0x147] switch
            {
                0x00 => new RomOnlyCartridge(contents),  // ROM Only
                0x01 => new MBC1Cartridge(contents), // MBC1
                0x02 => new MBC1Cartridge(contents), // MBC1 + RAM
                0x03 => new MBC1Cartridge(contents), // MBC1 + RAM + Battery
                _ => throw new ArgumentException($"Unmapped/invalid cartridge type {contents[0x147]}", nameof(contents))
            };
        }
    }
}
