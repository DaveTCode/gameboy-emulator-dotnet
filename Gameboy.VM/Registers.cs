using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Gameboy.VM.Cpu.Tests")]
namespace Gameboy.VM
{
    /// <summary>
    /// TODO - This assumes initialising values to 0x0, which is fine if we 
    /// load from bootrom but not find if we load from cartridge address 0x100.
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
                F = (byte)(value & 0x00F0); // Note that the lowest 4 bits of the F register are always 0
            }
        }

        internal ushort BC
        {
            get => (ushort)((B << 8) | C);
            set
            {
                B = (byte)(value >> 8);
                C = (byte)(value & 0x00FF);
            }
        }

        internal ushort DE
        {
            get => (ushort)((D << 8) | E);
            set
            {
                D = (byte)(value >> 8);
                E = (byte)(value & 0x00FF);
            }
        }
        internal ushort HL
        {
            get => (ushort)((H << 8) | L);
            set
            {
                H = (byte)(value >> 8);
                L = (byte)(value & 0x00FF);
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

        internal void SetFlag(FRegisterFlags flag, bool set)
        {
            F |= set switch
            {
                true => (byte) flag,
                false => (byte) ~flag
            };

            F &= 0x00F0;
        }

        internal bool GetFlag(FRegisterFlags flag)
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
            return $"AF: {AF:X4}, BC: {BC:X4}, DE: {DE:X4}, HL: {HL:X4}";
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
