using System;

namespace Gameboy.VM.LCD
{
    [Flags]
    internal enum StatMode
    {
        HBlankPeriod = 0x00,
        VBlankPeriod = 0x01,
        OAMRAMPeriod = 0x02,
        TransferringDataToDriver = 0x03
    }
}