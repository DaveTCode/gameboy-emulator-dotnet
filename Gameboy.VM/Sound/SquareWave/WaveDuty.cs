using System;

namespace Gameboy.VM.Sound.SquareWave
{
    internal enum WaveDuty
    {
        HalfQuarter = 0x0,
        Quarter = 0x1,
        Half = 0x2,
        ThreeQuarters = 0x3
    }

    internal static class WaveDutyExtensions
    {
        internal static byte DutyByte(this WaveDuty waveDuty) => waveDuty switch
        {
            WaveDuty.HalfQuarter => 0b0000_0001,
            WaveDuty.Quarter => 0b1000_0001,
            WaveDuty.Half => 0b1000_0111,
            WaveDuty.ThreeQuarters => 0b0111_1110,
            _ => throw new ArgumentOutOfRangeException(nameof(waveDuty), waveDuty, null)
        };
    }
}