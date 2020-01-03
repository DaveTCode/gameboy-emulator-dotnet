namespace Gameboy.VM.Cartridge
{
    public enum CGBSupportCode
    {
        CGBIncompatible = 0x00,
        CGBCompatible = 0x80,
        CGBExclusive = 0xC0
    }
}