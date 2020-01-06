using System;

namespace Gameboy.VM.Interrupts
{
    internal enum Interrupt
    {
        VerticalBlank = 0,
        LCDSTAT = 1,
        Timer = 2,
        Serial = 3,
        Joypad = 4
    }

    internal static class InterruptExtensions
    {
        internal static int Priority(this Interrupt interrupt) => interrupt switch
        {
            Interrupt.VerticalBlank => 1,
            Interrupt.LCDSTAT => 2,
            Interrupt.Timer => 3,
            Interrupt.Serial => 4,
            Interrupt.Joypad => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(interrupt), interrupt, null)
        };

        internal static ushort StartingAddress(this Interrupt interrupt) => interrupt switch
        {
            Interrupt.VerticalBlank => 0x40,
            Interrupt.LCDSTAT => 0x48,
            Interrupt.Timer => 0x50,
            Interrupt.Serial => 0x58,
            Interrupt.Joypad => 0x60,
            _ => throw new ArgumentOutOfRangeException(nameof(interrupt), interrupt, null)
        };

        internal static byte Mask(this Interrupt interrupt) => interrupt switch
        {
            Interrupt.VerticalBlank => 0b00000001,
            Interrupt.LCDSTAT => 0b00000010,
            Interrupt.Timer => 0b00000100,
            Interrupt.Serial => 0b00001000,
            Interrupt.Joypad => 0b00010000,
            _ => throw new ArgumentOutOfRangeException(nameof(interrupt), interrupt, null)
        };
    }
}