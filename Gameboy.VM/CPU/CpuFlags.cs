using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Gameboy.VM.Cpu.Tests")]
namespace Gameboy.VM.CPU
{
    [Flags]
    internal enum CpuFlags : byte
    {
        ZeroFlag      = 0b10000000,
        SubtractFlag  = 0b01000000,
        HalfCarryFlag = 0b00100000,
        CarryFlag     = 0b00010000,
    }
}
