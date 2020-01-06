using Gameboy.VM.CPU;
using Xunit;

namespace Gameboy.VM.Cpu.Tests.CPU
{
    public class RegisterTests
    {
        [Theory]
        [InlineData(0xFFFF, 0xFF, 0xF0)]
        [InlineData(0xFF00, 0xFF, 0x00)]
        [InlineData(0xF0FF, 0xF0, 0xF0)]
        public void TestAFRegisters(ushort af, byte a, byte f)
        {
            var registers = new Registers
            {
                AF = af
            };

            Assert.Equal(a, registers.A);
            Assert.Equal(0x00, registers.B);
            Assert.Equal(0x00, registers.C);
            Assert.Equal(0x00, registers.D);
            Assert.Equal(0x00, registers.E);
            Assert.Equal(f, registers.F);
            Assert.Equal(0x00, registers.H);
            Assert.Equal(0x00, registers.L);
            Assert.Equal(0x00, registers.ProgramCounter);
            Assert.Equal(0x00, registers.StackPointer);
        }

        [Theory]
        [InlineData(0xFFFF, 0xFF, 0xFF)]
        [InlineData(0xFF00, 0xFF, 0x00)]
        [InlineData(0xF0F0, 0xF0, 0xF0)]
        public void TestBCRegisters(ushort bc, byte b, byte c)
        {
            var registers = new Registers
            {
                BC = bc
            };

            Assert.Equal(0x00, registers.A);
            Assert.Equal(b, registers.B);
            Assert.Equal(c, registers.C);
            Assert.Equal(0x00, registers.D);
            Assert.Equal(0x00, registers.E);
            Assert.Equal(0x00, registers.F);
            Assert.Equal(0x00, registers.H);
            Assert.Equal(0x00, registers.L);
            Assert.Equal(0x00, registers.ProgramCounter);
            Assert.Equal(0x00, registers.StackPointer);
        }

        [Theory]
        [InlineData(0xFFFF, 0xFF, 0xFF)]
        [InlineData(0xFF00, 0xFF, 0x00)]
        [InlineData(0xF0F0, 0xF0, 0xF0)]
        public void TestDERegisters(ushort de, byte d, byte e)
        {
            var registers = new Registers
            {
                DE = de
            };

            Assert.Equal(0x00, registers.A);
            Assert.Equal(0x00, registers.B);
            Assert.Equal(0x00, registers.C);
            Assert.Equal(d, registers.D);
            Assert.Equal(e, registers.E);
            Assert.Equal(0x00, registers.F);
            Assert.Equal(0x00, registers.H);
            Assert.Equal(0x00, registers.L);
            Assert.Equal(0x00, registers.ProgramCounter);
            Assert.Equal(0x00, registers.StackPointer);
        }

        [Theory]
        [InlineData(0xFFFF, 0xFF, 0xFF)]
        [InlineData(0xFF00, 0xFF, 0x00)]
        [InlineData(0xF0F0, 0xF0, 0xF0)]
        public void TestHLRegisters(ushort hl, byte h, byte l)
        {
            var registers = new Registers
            {
                HL = hl
            };

            Assert.Equal(0x00, registers.A);
            Assert.Equal(0x00, registers.B);
            Assert.Equal(0x00, registers.C);
            Assert.Equal(0x00, registers.D);
            Assert.Equal(0x00, registers.E);
            Assert.Equal(0x00, registers.F);
            Assert.Equal(h, registers.H);
            Assert.Equal(l, registers.L);
            Assert.Equal(0x00, registers.ProgramCounter);
            Assert.Equal(0x00, registers.StackPointer);
        }

        [Fact]
        public void TestToString()
        {
            var registers = new Registers
            {
                A = 0x01,
                F = 0xB0,
                B = 0x00,
                C = 0x13,
                D = 0x00,
                E = 0xD8,
                H = 0x01,
                L = 0x4D,
                ProgramCounter = 0x0100,
                StackPointer = 0xFFFE,
            };

            Assert.Equal("AF: 01B0, BC: 0013, DE: 00D8, HL: 014D, SP: FFFE, PC: 0100", registers.ToString());
        }
    }
}

