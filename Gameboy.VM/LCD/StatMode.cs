using System;

namespace Gameboy.VM.LCD
{
    [Flags]
    internal enum StatMode
    {
        HBlankPeriod = 0xFC,
        VBlankPeriod = 0xFD,
        OAMRAMPeriod = 0xFE,
        TransferringDataToDriver = 0xFF
    }
}