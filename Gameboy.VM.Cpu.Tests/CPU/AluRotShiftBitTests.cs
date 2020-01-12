using Gameboy.VM.CPU;
using Xunit;

namespace Gameboy.VM.Tests.CPU
{
    public class AluRotShiftBitTests
    {
        [Theory]
        [InlineData(0x0, 0x0, 0x0, 0x06, 0x78, false, false, true)] // Test zero flag set on RLC B
        [InlineData(0x0, 0x0, 0x1, 0x0E, 0x79, false, false, true)] // Test zero flag set on RLC C
        [InlineData(0x0, 0x0, 0x2, 0x16, 0x7A, false, false, true)] // Test zero flag set on RLC D
        [InlineData(0x0, 0x0, 0x3, 0x1E, 0x7B, false, false, true)] // Test zero flag set on RLC E
        [InlineData(0x0, 0x0, 0x4, 0x26, 0x7C, false, false, true)] // Test zero flag set on RLC H
        [InlineData(0x0, 0x0, 0x5, 0x2E, 0x7D, false, false, true)] // Test zero flag set on RLC L
        [InlineData(0x0, 0x0, 0x6, 0x36, 0x7E, false, false, true)] // Test zero flag set on RLC (HL)
        [InlineData(0x0, 0x0, 0x7, 0x3E, 0x7F, false, false, true)] // Test zero flag set on RLC A
        [InlineData(0x0, 0x0, 0x0, 0x06, 0x78, true, false, true)] // Test carry flag not doing anything on RLC B
        [InlineData(0x0, 0x0, 0x1, 0x0E, 0x79, true, false, true)] // Test carry flag not doing anything on RLC C
        [InlineData(0x0, 0x0, 0x2, 0x16, 0x7A, true, false, true)] // Test carry flag not doing anything on RLC D
        [InlineData(0x0, 0x0, 0x3, 0x1E, 0x7B, true, false, true)] // Test carry flag not doing anything on RLC E
        [InlineData(0x0, 0x0, 0x4, 0x26, 0x7C, true, false, true)] // Test carry flag not doing anything on RLC H
        [InlineData(0x0, 0x0, 0x5, 0x2E, 0x7D, true, false, true)] // Test carry flag not doing anything on RLC L
        [InlineData(0x0, 0x0, 0x6, 0x36, 0x7E, true, false, true)] // Test carry flag not doing anything on RLC (HL)
        [InlineData(0x0, 0x0, 0x7, 0x3E, 0x7F, true, false, true)] // Test carry flag not doing anything on RLC A
        [InlineData(0x80, 0x1, 0x0, 0x06, 0x78, false, true, false)] // Test bit carried on RLC B
        [InlineData(0x80, 0x1, 0x1, 0x0E, 0x79, false, true, false)] // Test bit carried on RLC C
        [InlineData(0x80, 0x1, 0x2, 0x16, 0x7A, false, true, false)] // Test bit carried on RLC D
        [InlineData(0x80, 0x1, 0x3, 0x1E, 0x7B, false, true, false)] // Test bit carried on RLC E
        [InlineData(0x80, 0x1, 0x4, 0x26, 0x7C, false, true, false)] // Test bit carried on RLC H
        [InlineData(0x80, 0x1, 0x5, 0x2E, 0x7D, false, true, false)] // Test bit carried on RLC L
        [InlineData(0x80, 0x1, 0x6, 0x36, 0x7E, false, true, false)] // Test bit carried on RLC (HL)
        [InlineData(0x80, 0x1, 0x7, 0x3E, 0x7F, false, true, false)] // Test bit carried on RLC A
        public void TestRLC(byte initialValue, byte expectedValue, byte rotateOpcode, byte loadOpcode, byte loadRegIntoA, bool preC, bool c, bool z)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x00, 0xC0, // Load 0xC000 into HL
                loadOpcode, initialValue, // Load into reg
                0x37, // Set carry flag to 1
                (byte)((preC) ? 0x00 : 0x3F), // CCF to unset carry flag if starting from unset
                0xCB, rotateOpcode, // Rotate reg (opcode under test)
                loadRegIntoA, // Move result to A for evaluation
            });

            for (var ii = 0; ii < 8; ii++) // 2 to get to PC 0x150 then 6 to setup and act
            {
                device.Step();
            }

            Assert.Equal(expectedValue, device.CPU.Registers.A);
            Assert.Equal(c, device.CPU.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, device.CPU.Registers.GetFlag(CpuFlags.ZeroFlag));

            device.Step();
        }

        [Theory]
        [InlineData(0x85, 0x0B, false, true, false, false, false)] // Note that the example in the official manual says 0x0A but think that's incorrect
        public void TestRLCA(byte a, byte result, bool cBefore, bool c, bool h, bool z, bool n)
        {
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
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
            var device = TestUtils.CreateTestDevice();
            var cpu = device.CPU;
            var alu = new ALU(cpu, device.MMU);
            var cycles = alu.Res(ref a, bit);
            Assert.Equal(1, cycles);
            Assert.Equal(res, a);
        }
    }
}
