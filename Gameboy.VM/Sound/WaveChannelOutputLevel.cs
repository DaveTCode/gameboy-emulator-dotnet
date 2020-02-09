using System;

namespace Gameboy.VM.Sound
{
    internal enum WaveChannelOutputLevel
    {
        Mute = 0b00,
        Unmodified = 0b01,
        Half = 0b10,
        Quarter = 0b11
    }

    internal static class WaveChannelOutputLevelExtensions
    {
        internal static int RightShiftValue(this WaveChannelOutputLevel outputLevel) => outputLevel switch
        {
            WaveChannelOutputLevel.Mute => 4,
            WaveChannelOutputLevel.Unmodified => 0,
            WaveChannelOutputLevel.Half => 1,
            WaveChannelOutputLevel.Quarter => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(outputLevel), outputLevel, "Unmapped wave channel output level")
        };
    }
}