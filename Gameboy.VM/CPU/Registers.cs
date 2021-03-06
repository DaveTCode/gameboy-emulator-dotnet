﻿using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Gameboy.VM.Cpu.Tests")]
namespace Gameboy.VM.CPU
{

    /// <summary>
    /// Holds the CPU register state and provides utility functions to access
    /// flag information.
    /// </summary>
    internal class Registers
    {
        internal byte A;
        internal byte B;
        internal byte C;
        internal byte D;
        internal byte E;
        internal byte F;
        internal byte H;
        internal byte L;

        internal ushort AF
        {
            get => (ushort)((A << 8) | F);
            set
            {
                A = (byte)(value >> 8);
                F = (byte)(value & 0xF0); // Note that the lowest 4 bits of the F register are always 0
            }
        }

        internal ushort BC
        {
            get => (ushort)((B << 8) | C);
            set
            {
                B = (byte)(value >> 8);
                C = (byte)(value & 0xFF);
            }
        }

        internal ushort DE
        {
            get => (ushort)((D << 8) | E);
            set
            {
                D = (byte)(value >> 8);
                E = (byte)(value & 0xFF);
            }
        }
        internal ushort HL
        {
            get => (ushort)((H << 8) | L);
            set
            {
                H = (byte)(value >> 8);
                L = (byte)(value & 0xFF);
            }
        }

        internal ushort HLI()
        {
            HL = (ushort)((HL + 1) & 0xFFFF);
            return (ushort)((HL - 1) & 0xFFFF);
        }

        internal ushort HLD()
        {
            HL = (ushort)((HL - 1) & 0xFFFF);
            return (ushort)((HL + 1) & 0xFFFF);
        }

        internal void SetFlag(CpuFlags flag, bool set)
        {
            switch (set)
            {
                case true:
                    F |= (byte)flag;
                    break;
                case false:
                    F &= (byte)~flag;
                    break;
            }

            F &= 0x00F0;
        }

        internal bool GetFlag(CpuFlags flag)
        {
            return (F & (byte)flag) == (byte)flag;
        }


        internal ushort ProgramCounter;
        internal ushort StackPointer;

        public void Clear()
        {
            AF = 0x0000;
            BC = 0x0000;
            DE = 0x0000;
            HL = 0x0000;
            ProgramCounter = 0x0000;
            StackPointer = 0x0000;
        }

        public override string ToString()
        {
            return $"A:{A:X2}, BC:{BC:X4}, DE:{DE:X4}, HL:{HL:X4}, SP:{StackPointer:X4}, PC:{ProgramCounter:X4}, Z:{GetFlag(CpuFlags.ZeroFlag)}, C:{GetFlag(CpuFlags.CarryFlag)}, H:{GetFlag(CpuFlags.HalfCarryFlag)}, N:{GetFlag(CpuFlags.SubtractFlag)}";
        }
    }

    internal enum Register16Bit
    {
        AF,
        BC,
        DE,
        HL,
        SP
    }
}
