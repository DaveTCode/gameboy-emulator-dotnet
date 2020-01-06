using System;
using Gameboy.VM.CPU;
using Gameboy.VM.LCD;
using Gameboy.VM.Sound;
using Xunit;

namespace Gameboy.VM.Cpu.Tests.CPU
{
    public class AluRotShiftBitTests
    {
        [Theory]
        [InlineData(0x85, 0x0B, false, true, false, false, false)] // Note that the example in the official manual says 0x0A but think that's incorrect
        public void TestRLCA(byte a, byte result, bool cBefore, bool c, bool h, bool z, bool n)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            cpu.Registers.SetFlag(CpuFlags.CarryFlag, cBefore);
            var cycles = alu.RotateLeftWithCarry(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(n, cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x95, 0x2B, true, true, false, false, false)]
        public void TestRLA(byte a, byte result, bool cBefore, bool c, bool h, bool z, bool n)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            cpu.Registers.SetFlag(CpuFlags.CarryFlag, cBefore);
            var cycles = alu.RotateLeftNoCarry(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(n, cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x3B, 0x9D, false, true, false, false, false)]
        public void TestRRCA(byte a, byte result, bool cBefore, bool c, bool h, bool z, bool n)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            cpu.Registers.SetFlag(CpuFlags.CarryFlag, cBefore);
            var cycles = alu.RotateRightWithCarry(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(n, cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x81, 0x40, false, true, false, false, false)]
        public void TestRRA(byte a, byte result, bool cBefore, bool c, bool h, bool z, bool n)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            cpu.Registers.SetFlag(CpuFlags.CarryFlag, cBefore);
            var cycles = alu.RotateRightNoCarry(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(n, cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x80, 0x00, false, true, false, true, false)]
        [InlineData(0xFF, 0xFE, false, true, false, false, false)]
        public void TestSLA(byte a, byte result, bool cBefore, bool c, bool h, bool z, bool n)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            cpu.Registers.SetFlag(CpuFlags.CarryFlag, cBefore);
            var cycles = alu.ShiftLeft(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(n, cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x8A, 0xC5, false, false, false, false, false)]
        [InlineData(0x01, 0x00, false, true, false, true, false)]
        public void TestSRA(byte a, byte result, bool cBefore, bool c, bool h, bool z, bool n)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            cpu.Registers.SetFlag(CpuFlags.CarryFlag, cBefore);
            var cycles = alu.ShiftRightAdjust(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(n, cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0xFF, 0x7F, false, true, false, false, false)]
        [InlineData(0x01, 0x00, false, true, false, true, false)]
        public void TestSRL(byte a, byte result, bool cBefore, bool c, bool h, bool z, bool n)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            cpu.Registers.SetFlag(CpuFlags.CarryFlag, cBefore);
            var cycles = alu.ShiftRightLeave(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(n, cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x00, 0x00, false, false, true, false)]
        [InlineData(0xF0, 0x0F, false, false, false, false)]
        public void TestSwap(byte a, byte result, bool c, bool h, bool z, bool n)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            var cycles = alu.Swap(ref a);
            Assert.Equal(1, cycles);
            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(n, cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x80, 7, false)]
        [InlineData(0xEF, 4, true)]
        [InlineData(0xFE, 0, true)]
        [InlineData(0xFE, 1, false)]
        public void TestBit(byte a, int bit, bool z)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            var cycles = alu.Bit(a, bit);
            Assert.Equal(1, cycles);
            Assert.True(cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x80, 3, 0x88)] // Wrong in original manual, used 84 as result
        [InlineData(0x3B, 7, 0xBB)]
        [InlineData(0x00, 2, 0x04)] // Wrong in original manual, used 3 instead of 2 for bit
        public void TestSet(byte a, int bit, byte res)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            var cycles = alu.Set(ref a, bit);
            Assert.Equal(1, cycles);
            Assert.Equal(res, a);
        }

        [Theory]
        [InlineData(0x80, 7, 0x00)]
        [InlineData(0x3B, 1, 0x39)]
        [InlineData(0xFF, 3, 0xF7)]
        public void TestRes(byte a, int bit, byte res)
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);
            var cycles = alu.Res(ref a, bit);
            Assert.Equal(1, cycles);
            Assert.Equal(res, a);
        }
    }
}
