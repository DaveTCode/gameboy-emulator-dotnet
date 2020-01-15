using System;
using System.Diagnostics.CodeAnalysis;

namespace Gameboy.VM.Cartridge
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum CartridgeROMSize
    {
        A32KB = 0x00,
        A64KB = 0x01,
        A128KB = 0x02,
        A256KB = 0x03,
        A512KB = 0x04,
        A1MB = 0x05,
        A2MB = 0x06,
        A4MB = 0x07,
        A8MB = 0x08,
        A1p1MB = 0x52,
        A1p2MB = 0x53,
        A1p3MB = 0x54,
    }

    public static class CartridgeROMSizeExtensions
    {
        public static int NumberBanks(this CartridgeROMSize romSize) =>
            romSize switch
            {
                CartridgeROMSize.A32KB => 0,
                CartridgeROMSize.A64KB => 4,
                CartridgeROMSize.A128KB => 8,
                CartridgeROMSize.A256KB => 16,
                CartridgeROMSize.A512KB => 32,
                CartridgeROMSize.A1MB => 64,
                CartridgeROMSize.A2MB => 128,
                CartridgeROMSize.A4MB => 256,
                CartridgeROMSize.A8MB => 512,
                CartridgeROMSize.A1p1MB => 72,
                CartridgeROMSize.A1p2MB => 80,
                CartridgeROMSize.A1p3MB => 96,
                _ => throw new ArgumentOutOfRangeException(nameof(romSize), romSize, null)
            };
    }
}