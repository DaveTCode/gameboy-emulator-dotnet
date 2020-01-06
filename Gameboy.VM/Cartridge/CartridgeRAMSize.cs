using System;

namespace Gameboy.VM.Cartridge
{
    public enum CartridgeRAMSize
    {
        None = 0x00,
        A2KB = 0x01,
        A8KB = 0x02,
        A32KB = 0x03,
        A128KB = 0x04,
        A64KB = 0x05
    }

    public static class CartridgeRAMSizeExtensions
    {
        public static int NumberBanks(this CartridgeRAMSize ramSize) => ramSize switch
        {
            CartridgeRAMSize.None => 0,
            CartridgeRAMSize.A2KB => 1,
            CartridgeRAMSize.A8KB => 1,
            CartridgeRAMSize.A32KB => 4,
            CartridgeRAMSize.A128KB => 16,
            CartridgeRAMSize.A64KB => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(ramSize), ramSize, null)
        };

        public static int BankSizeBytes(this CartridgeRAMSize ramSize) => ramSize switch
        {
            CartridgeRAMSize.None => 0x0,
            CartridgeRAMSize.A2KB => 0x800,
            CartridgeRAMSize.A8KB => 0x2000,
            CartridgeRAMSize.A32KB => 0x2000,
            CartridgeRAMSize.A128KB => 0x2000,
            CartridgeRAMSize.A64KB => 0x2000,
            _ => throw new ArgumentOutOfRangeException(nameof(ramSize), ramSize, null)
        };
    }
}