using System;

namespace Gameboy.VM.Cartridge
{
    public static class CartridgeFactory
    {
        /// <summary>
        /// Given a gameboy cartridge binary dump this function will return a 
        /// Cartridge object by determining which type of cartridge it is, how much
        /// RAM it has, how many ROM banks it has etc.
        /// </summary>
        /// 
        /// <param name="contents">The full binary dump of a ROM</param>
        /// 
        /// <returns>A cartridge ready to inject into a <see cref="Device"/></returns>
        public static Cartridge CreateCartridge(in byte[] contents)
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
                0x04 => throw new ArgumentException($"Unmapped/invalid cartridge type {contents[0x147]}", nameof(contents)), // 0x04 unused
                // TODO - MBC2,3,5,6,7,PocketCamera,etc
                _ => throw new ArgumentException($"Unmapped/invalid cartridge type {contents[0x147]}", nameof(contents))
            };
        }
    }
}
