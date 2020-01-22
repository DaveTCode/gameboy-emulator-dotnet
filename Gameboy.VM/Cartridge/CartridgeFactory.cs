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
        public static Cartridge CreateCartridge(byte[] contents)
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
                0x05 => new MBC2Cartridge(contents), // MBC2
                0x06 => new MBC2Cartridge(contents), // MBC2 + Battery
                // TODO - MBC0 with RAM 0x08 =>
                // TODO - MBC0 with RAM 0x09 =>
                0x0A => throw new ArgumentException($"Unmapped/invalid cartridge type {contents[0x147]}", nameof(contents)), // 0x0A unused
                // TODO 0x0B-0x0D MMM01 cartridge
                0x0E => throw new ArgumentException($"Unmapped/invalid cartridge type {contents[0x147]}", nameof(contents)), // 0x0E unused
                0x0F => new MBC3Cartridge(contents), // MBC3 + Timer + Battery
                0x10 => new MBC3Cartridge(contents), // MBC3 + RAM + Timer + Battery
                0x11 => new MBC3Cartridge(contents), // MBC3
                0x12 => new MBC3Cartridge(contents), // MBC3 + RAM
                0x13 => new MBC3Cartridge(contents), // MBC3 + RAM + Battery
                _ when contents[0x147] >= 0x14 && contents[0x147] <= 0x18 => throw new ArgumentException($"Unmapped/invalid cartridge type {contents[0x147]}", nameof(contents)), // 0x14-18 unused
                0x19 => new MBC5Cartridge(contents), // MBC5
                0x1A => new MBC5Cartridge(contents), // MBC5+RAM
                0x1B => new MBC5Cartridge(contents), // MBC5+RAM+BATTERY
                0x1C => new MBC5Cartridge(contents), // MBC5+RUMBLE
                0x1D => new MBC5Cartridge(contents), // MBC5+RUMBLE+RAM
                0x1E => new MBC5Cartridge(contents), // MBC5+RUMBLE+RAM+BATTERY
                0x1F => throw new ArgumentException($"Unmapped/invalid cartridge type {contents[0x147]}", nameof(contents)), // 0x1F unused
                // TODO - MBC 6,7,PocketCamera,etc
                _ => throw new ArgumentException($"Unmapped/invalid cartridge type {contents[0x147]}", nameof(contents))
            };
        }
    }
}
