using Gameboy.VM.CPU;
using Xunit;

namespace Gameboy.VM.Cpu.Tests.Opcodes
{
    public class Alu8BitArithmeticTests
    {
        [Theory]
        [InlineData(0xFF, 0x00, true, true)]
        [InlineData(0x50, 0x51, false, false)]
        public void TestIncrement(byte a, byte result, bool h, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            var cycles = alu.Increment(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x01, 0x00, false, true)]
        [InlineData(0x00, 0xFF, true, false)]
        public void TestDecrement(byte a, byte result, bool h, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            var cycles = alu.Decrement(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.True(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x3A, 0xC6, 0x0, true, true, true)]
        [InlineData(0x3C, 0xFF, 0x3B, true, true, false)]
        [InlineData(0x3C, 0x12, 0x4E, false, false, false)]
        public void TestAdd(byte a, byte b, byte result, bool c, bool h, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            var cycles = alu.Add(ref a, b, false);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0xE1, 0x0F, 0xF1, true, false, true, false)]
        [InlineData(0xE1, 0x3B, 0x1D, true, true, false, false)]
        [InlineData(0xE1, 0x1E, 0x00, true, true, true, true)]
        public void TestAdc(byte a, byte b, byte result, bool currentCarry, bool c, bool h, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            cpu.Registers.SetFlag(CpuFlags.CarryFlag, currentCarry);
            var alu = new ALU(cpu);
            var cycles = alu.Add(ref a, b, true);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x3E, 0x3E, 0x00, false, false, true)]
        [InlineData(0x3E, 0x0F, 0x2F, false, true, false)]
        [InlineData(0x3E, 0x40, 0xFE, true, false, false)]
        [InlineData(0x00, 0x01, 0xFF, true, true, false)]
        public void TestSub(byte a, byte b, byte result, bool c, bool h, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            var cycles = alu.Sub(ref a, b, false);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.True(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x3B, 0x2A, 0x10, true, false, false, false)]
        [InlineData(0x3B, 0x3A, 0x00, true, false, false, true)]
        [InlineData(0x3B, 0x4F, 0xEB, true, true, true, false)]
        public void TestSbc(byte a, byte b, byte result, bool currentCarry, bool c, bool h, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            cpu.Registers.SetFlag(CpuFlags.CarryFlag, currentCarry);
            var alu = new ALU(cpu);
            var cycles = alu.Sub(ref a, b, true);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.True(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x5A, 0x3F, 0x1A, false)]
        [InlineData(0x5A, 0x38, 0x18, false)]
        [InlineData(0x5A, 0x00, 0x00, true)]
        public void TestAnd(byte a, byte b, byte result, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            var cycles = alu.And(ref a, b);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.False(cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.True(cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x5A, 0x5A, 0x5A, false)]
        [InlineData(0x5A, 0x03, 0x5B, false)]
        [InlineData(0x00, 0x00, 0x00, true)]
        public void TestOr(byte a, byte b, byte result, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            var cycles = alu.Or(ref a, b);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.False(cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.False(cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0xFF, 0xFF, 0x00, true)]
        [InlineData(0xFF, 0x0F, 0xF0, false)]
        [InlineData(0xFF, 0x8A, 0x75, false)]
        public void TestXor(byte a, byte b, byte result, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            var cycles = alu.Xor(ref a, b);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.False(cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.False(cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x3C, 0x2F, false, true, false)]
        [InlineData(0x3C, 0x3C, false, false, true)]
        [InlineData(0x3C, 0x40, true, false, false)]
        public void TestCp(byte a, byte b, bool c, bool h, bool z)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            var cycles = alu.Cp(a, b);
            Assert.Equal(1, cycles);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.True(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Fact]
        public void TestDecimalAdjustRegister()
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            cpu.Registers.A = 0x45;
            cpu.Registers.B = 0x38;
            alu.Add(ref cpu.Registers.A, cpu.Registers.B, false);
            var cycles = alu.DecimalAdjustRegister(ref cpu.Registers.A);
            Assert.Equal(0x83, cpu.Registers.A);
            Assert.Equal(1, cycles);
            Assert.Equal(0x0, cpu.Registers.F); // All flags zero 

            cpu.Registers.A = 0x83;
            alu.Sub(ref cpu.Registers.A, cpu.Registers.B, false);
            cycles = alu.DecimalAdjustRegister(ref cpu.Registers.A);
            Assert.Equal(0x45, cpu.Registers.A);
            Assert.Equal(1, cycles);
            Assert.Equal((byte)CpuFlags.SubtractFlag, cpu.Registers.F); // All flags zero 
        }

        [Fact]
        public void TestCCF()
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            alu.CCF();
            Assert.True(cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            alu.CCF();
            Assert.False(cpu.Registers.GetFlag(CpuFlags.CarryFlag));
        }

        [Fact]
        public void TestSCF()
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            alu.SCF();
            Assert.True(cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            alu.SCF();
            Assert.True(cpu.Registers.GetFlag(CpuFlags.CarryFlag));
        }

        [Theory]
        [InlineData(0x35, 0xCA)]
        public void TestCPL(byte a, byte result)
        {
            var cpu = new CPU.CPU(new MMU(null, null, null));
            var alu = new ALU(cpu);
            cpu.Registers.A = a;
            alu.CPL();
            Assert.Equal(result, cpu.Registers.A);
            Assert.True(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.True(cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
        }
    }
}
