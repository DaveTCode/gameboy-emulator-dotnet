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
        }

        [Theory]
        [InlineData(0x0, 0x0, 0x10, 0x06, 0x78, false, false, true)] // Test zero flag set on RL B
        [InlineData(0x0, 0x0, 0x11, 0x0E, 0x79, false, false, true)] // Test zero flag set on RL C
        [InlineData(0x0, 0x0, 0x12, 0x16, 0x7A, false, false, true)] // Test zero flag set on RL D
        [InlineData(0x0, 0x0, 0x13, 0x1E, 0x7B, false, false, true)] // Test zero flag set on RL E
        [InlineData(0x0, 0x0, 0x14, 0x26, 0x7C, false, false, true)] // Test zero flag set on RL H
        [InlineData(0x0, 0x0, 0x15, 0x2E, 0x7D, false, false, true)] // Test zero flag set on RL L
        [InlineData(0x0, 0x0, 0x16, 0x36, 0x7E, false, false, true)] // Test zero flag set on RL (HL)
        [InlineData(0x0, 0x0, 0x17, 0x3E, 0x7F, false, false, true)] // Test zero flag set on RL A
        [InlineData(0x0, 0x1, 0x10, 0x06, 0x78, true, false, false)] // Test carry flag used on RL B
        [InlineData(0x0, 0x1, 0x11, 0x0E, 0x79, true, false, false)] // Test carry flag used on RL C
        [InlineData(0x0, 0x1, 0x12, 0x16, 0x7A, true, false, false)] // Test carry flag used on RL D
        [InlineData(0x0, 0x1, 0x13, 0x1E, 0x7B, true, false, false)] // Test carry flag used on RL E
        [InlineData(0x0, 0x1, 0x14, 0x26, 0x7C, true, false, false)] // Test carry flag used on RL H
        [InlineData(0x0, 0x1, 0x15, 0x2E, 0x7D, true, false, false)] // Test carry flag used on RL L
        [InlineData(0x0, 0x1, 0x16, 0x36, 0x7E, true, false, false)] // Test carry flag used on RL (HL)
        [InlineData(0x0, 0x1, 0x17, 0x3E, 0x7F, true, false, false)] // Test carry flag used on RL A
        [InlineData(0x80, 0x0, 0x10, 0x06, 0x78, false, true, true)] // Test bit not carried on RL B
        [InlineData(0x80, 0x0, 0x11, 0x0E, 0x79, false, true, true)] // Test bit not carried on RL C
        [InlineData(0x80, 0x0, 0x12, 0x16, 0x7A, false, true, true)] // Test bit not carried on RL D
        [InlineData(0x80, 0x0, 0x13, 0x1E, 0x7B, false, true, true)] // Test bit not carried on RL E
        [InlineData(0x80, 0x0, 0x14, 0x26, 0x7C, false, true, true)] // Test bit not carried on RL H
        [InlineData(0x80, 0x0, 0x15, 0x2E, 0x7D, false, true, true)] // Test bit not carried on RL L
        [InlineData(0x80, 0x0, 0x16, 0x36, 0x7E, false, true, true)] // Test bit not carried on RL (HL)
        [InlineData(0x80, 0x0, 0x17, 0x3E, 0x7F, false, true, true)] // Test bit not carried on RL A
        [InlineData(0x80, 0x1, 0x10, 0x06, 0x78, true, true, false)] // Test bit not carried on RL B
        [InlineData(0x80, 0x1, 0x11, 0x0E, 0x79, true, true, false)] // Test bit not carried on RL C
        [InlineData(0x80, 0x1, 0x12, 0x16, 0x7A, true, true, false)] // Test bit not carried on RL D
        [InlineData(0x80, 0x1, 0x13, 0x1E, 0x7B, true, true, false)] // Test bit not carried on RL E
        [InlineData(0x80, 0x1, 0x14, 0x26, 0x7C, true, true, false)] // Test bit not carried on RL H
        [InlineData(0x80, 0x1, 0x15, 0x2E, 0x7D, true, true, false)] // Test bit not carried on RL L
        [InlineData(0x80, 0x1, 0x16, 0x36, 0x7E, true, true, false)] // Test bit not carried on RL (HL)
        [InlineData(0x80, 0x1, 0x17, 0x3E, 0x7F, true, true, false)] // Test bit not carried on RL A
        public void TestRL(byte initialValue, byte expectedValue, byte rotateOpcode, byte loadOpcode, byte loadRegIntoA, bool preC, bool c, bool z)
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
        }

        [Theory]
        [InlineData(0x0, 0x0, 0x8, 0x06, 0x78, false, false, true)] // Test zero flag set on RRC B
        [InlineData(0x0, 0x0, 0x9, 0x0E, 0x79, false, false, true)] // Test zero flag set on RRC C
        [InlineData(0x0, 0x0, 0xA, 0x16, 0x7A, false, false, true)] // Test zero flag set on RRC D
        [InlineData(0x0, 0x0, 0xB, 0x1E, 0x7B, false, false, true)] // Test zero flag set on RRC E
        [InlineData(0x0, 0x0, 0xC, 0x26, 0x7C, false, false, true)] // Test zero flag set on RRC H
        [InlineData(0x0, 0x0, 0xD, 0x2E, 0x7D, false, false, true)] // Test zero flag set on RRC L
        [InlineData(0x0, 0x0, 0xE, 0x36, 0x7E, false, false, true)] // Test zero flag set on RRC (HL)
        [InlineData(0x0, 0x0, 0xF, 0x3E, 0x7F, false, false, true)] // Test zero flag set on RRC A
        [InlineData(0x0, 0x0, 0x8, 0x06, 0x78, true, false, true)] // Test carry flag not doing anything on RRC B
        [InlineData(0x0, 0x0, 0x9, 0x0E, 0x79, true, false, true)] // Test carry flag not doing anything on RRC C
        [InlineData(0x0, 0x0, 0xA, 0x16, 0x7A, true, false, true)] // Test carry flag not doing anything on RRC D
        [InlineData(0x0, 0x0, 0xB, 0x1E, 0x7B, true, false, true)] // Test carry flag not doing anything on RRC E
        [InlineData(0x0, 0x0, 0xC, 0x26, 0x7C, true, false, true)] // Test carry flag not doing anything on RRC H
        [InlineData(0x0, 0x0, 0xD, 0x2E, 0x7D, true, false, true)] // Test carry flag not doing anything on RRC L
        [InlineData(0x0, 0x0, 0xE, 0x36, 0x7E, true, false, true)] // Test carry flag not doing anything on RRC (HL)
        [InlineData(0x0, 0x0, 0xF, 0x3E, 0x7F, true, false, true)] // Test carry flag not doing anything on RRC A
        [InlineData(0x1, 0x80, 0x8, 0x06, 0x78, false, true, false)] // Test bit carried on RRC B
        [InlineData(0x1, 0x80, 0x9, 0x0E, 0x79, false, true, false)] // Test bit carried on RRC C
        [InlineData(0x1, 0x80, 0xA, 0x16, 0x7A, false, true, false)] // Test bit carried on RRC D
        [InlineData(0x1, 0x80, 0xB, 0x1E, 0x7B, false, true, false)] // Test bit carried on RRC E
        [InlineData(0x1, 0x80, 0xC, 0x26, 0x7C, false, true, false)] // Test bit carried on RRC H
        [InlineData(0x1, 0x80, 0xD, 0x2E, 0x7D, false, true, false)] // Test bit carried on RRC L
        [InlineData(0x1, 0x80, 0xE, 0x36, 0x7E, false, true, false)] // Test bit carried on RRC (HL)
        [InlineData(0x1, 0x80, 0xF, 0x3E, 0x7F, false, true, false)] // Test bit carried on RRC A
        public void TestRRC(byte initialValue, byte expectedValue, byte rotateOpcode, byte loadOpcode, byte loadRegIntoA, bool preC, bool c, bool z)
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
        }

        [Theory]
        [InlineData(0x0, 0x0, 0x18, 0x06, 0x78, false, false, true)] // Test zero flag set on RR B
        [InlineData(0x0, 0x0, 0x19, 0x0E, 0x79, false, false, true)] // Test zero flag set on RR C
        [InlineData(0x0, 0x0, 0x1A, 0x16, 0x7A, false, false, true)] // Test zero flag set on RR D
        [InlineData(0x0, 0x0, 0x1B, 0x1E, 0x7B, false, false, true)] // Test zero flag set on RR E
        [InlineData(0x0, 0x0, 0x1C, 0x26, 0x7C, false, false, true)] // Test zero flag set on RR H
        [InlineData(0x0, 0x0, 0x1D, 0x2E, 0x7D, false, false, true)] // Test zero flag set on RR L
        [InlineData(0x0, 0x0, 0x1E, 0x36, 0x7E, false, false, true)] // Test zero flag set on RR (HL)
        [InlineData(0x0, 0x0, 0x1F, 0x3E, 0x7F, false, false, true)] // Test zero flag set on RR A
        [InlineData(0x0, 0x80, 0x18, 0x06, 0x78, true, false, false)] // Test carry flag used on RR B
        [InlineData(0x0, 0x80, 0x19, 0x0E, 0x79, true, false, false)] // Test carry flag used on RR C
        [InlineData(0x0, 0x80, 0x1A, 0x16, 0x7A, true, false, false)] // Test carry flag used on RR D
        [InlineData(0x0, 0x80, 0x1B, 0x1E, 0x7B, true, false, false)] // Test carry flag used on RR E
        [InlineData(0x0, 0x80, 0x1C, 0x26, 0x7C, true, false, false)] // Test carry flag used on RR H
        [InlineData(0x0, 0x80, 0x1D, 0x2E, 0x7D, true, false, false)] // Test carry flag used on RR L
        [InlineData(0x0, 0x80, 0x1E, 0x36, 0x7E, true, false, false)] // Test carry flag used on RR (HL)
        [InlineData(0x0, 0x80, 0x1F, 0x3E, 0x7F, true, false, false)] // Test carry flag used on RR A
        [InlineData(0x1, 0x0, 0x18, 0x06, 0x78, false, true, true)] // Test bit not carried on RR B
        [InlineData(0x1, 0x0, 0x19, 0x0E, 0x79, false, true, true)] // Test bit not carried on RR C
        [InlineData(0x1, 0x0, 0x1A, 0x16, 0x7A, false, true, true)] // Test bit not carried on RR D
        [InlineData(0x1, 0x0, 0x1B, 0x1E, 0x7B, false, true, true)] // Test bit not carried on RR E
        [InlineData(0x1, 0x0, 0x1C, 0x26, 0x7C, false, true, true)] // Test bit not carried on RR H
        [InlineData(0x1, 0x0, 0x1D, 0x2E, 0x7D, false, true, true)] // Test bit not carried on RR L
        [InlineData(0x1, 0x0, 0x1E, 0x36, 0x7E, false, true, true)] // Test bit not carried on RR (HL)
        [InlineData(0x1, 0x0, 0x1F, 0x3E, 0x7F, false, true, true)] // Test bit not carried on RR A
        [InlineData(0x1, 0x80, 0x18, 0x06, 0x78, true, true, false)] // Test bit not carried on RR B
        [InlineData(0x1, 0x80, 0x19, 0x0E, 0x79, true, true, false)] // Test bit not carried on RR C
        [InlineData(0x1, 0x80, 0x1A, 0x16, 0x7A, true, true, false)] // Test bit not carried on RR D
        [InlineData(0x1, 0x80, 0x1B, 0x1E, 0x7B, true, true, false)] // Test bit not carried on RR E
        [InlineData(0x1, 0x80, 0x1C, 0x26, 0x7C, true, true, false)] // Test bit not carried on RR H
        [InlineData(0x1, 0x80, 0x1D, 0x2E, 0x7D, true, true, false)] // Test bit not carried on RR L
        [InlineData(0x1, 0x80, 0x1E, 0x36, 0x7E, true, true, false)] // Test bit not carried on RR (HL)
        [InlineData(0x1, 0x80, 0x1F, 0x3E, 0x7F, true, true, false)] // Test bit not carried on RR A
        public void TestRR(byte initialValue, byte expectedValue, byte rotateOpcode, byte loadOpcode, byte loadRegIntoA, bool preC, bool c, bool z)
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
        [InlineData(0x0, 0x0, 0x20, 0x06, 0x78, false, true)] // SLA B - check zero flag
        [InlineData(0x0, 0x0, 0x21, 0x0E, 0x79, false, true)] // SLA C - check zero flag
        [InlineData(0x0, 0x0, 0x22, 0x16, 0x7A, false, true)] // SLA D - check zero flag
        [InlineData(0x0, 0x0, 0x23, 0x1E, 0x7B, false, true)] // SLA E - check zero flag
        [InlineData(0x0, 0x0, 0x24, 0x26, 0x7C, false, true)] // SLA H - check zero flag
        [InlineData(0x0, 0x0, 0x25, 0x2E, 0x7D, false, true)] // SLA L - check zero flag
        [InlineData(0x0, 0x0, 0x26, 0x36, 0x7E, false, true)] // SLA (HL) - check zero flag
        [InlineData(0x0, 0x0, 0x27, 0x3E, 0x7F, false, true)] // SLA A - check zero flag
        [InlineData(0x80, 0x0, 0x20, 0x06, 0x78, true, true)] // SLA B - check carry & zero flag
        [InlineData(0x80, 0x0, 0x21, 0x0E, 0x79, true, true)] // SLA C - check zero flag & zero flag
        [InlineData(0x80, 0x0, 0x22, 0x16, 0x7A, true, true)] // SLA D - check zero flag & zero flag
        [InlineData(0x80, 0x0, 0x23, 0x1E, 0x7B, true, true)] // SLA E - check zero flag & zero flag
        [InlineData(0x80, 0x0, 0x24, 0x26, 0x7C, true, true)] // SLA H - check zero flag & zero flag
        [InlineData(0x80, 0x0, 0x25, 0x2E, 0x7D, true, true)] // SLA L - check zero flag & zero flag
        [InlineData(0x80, 0x0, 0x26, 0x36, 0x7E, true, true)] // SLA (HL) - check zero flag & zero flag
        [InlineData(0x80, 0x0, 0x27, 0x3E, 0x7F, true, true)] // SLA A - check zero flag & zero flag
        [InlineData(0xFF, 0xFE, 0x20, 0x06, 0x78, true, false)] // SLA B - check not zero but carry
        [InlineData(0xFF, 0xFE, 0x21, 0x0E, 0x79, true, false)] // SLA C - check zero flag & zero flag
        [InlineData(0xFF, 0xFE, 0x22, 0x16, 0x7A, true, false)] // SLA D - check zero flag & zero flag
        [InlineData(0xFF, 0xFE, 0x23, 0x1E, 0x7B, true, false)] // SLA E - check zero flag & zero flag
        [InlineData(0xFF, 0xFE, 0x24, 0x26, 0x7C, true, false)] // SLA H - check zero flag & zero flag
        [InlineData(0xFF, 0xFE, 0x25, 0x2E, 0x7D, true, false)] // SLA L - check zero flag & zero flag
        [InlineData(0xFF, 0xFE, 0x26, 0x36, 0x7E, true, false)] // SLA (HL) - check zero flag & zero flag
        [InlineData(0xFF, 0xFE, 0x27, 0x3E, 0x7F, true, false)] // SLA A - check zero flag & zero flag
        public void TestSLA(byte initialValue, byte expectedValue, byte shiftOpcode, byte loadOpcode, byte loadRegIntoA, bool c, bool z)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x00, 0xC0, // Load 0xC000 into HL
                loadOpcode, initialValue, // Load into reg
                0x37, // Set carry flag to 1
                0x3F, // CCF to unset carry flag so we know if it's been set properly
                0xCB, shiftOpcode, // Shift reg (opcode under test)
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
        }

        [Theory]
        [InlineData(0x0, 0x0, 0x28, 0x06, 0x78, false, true)] // SRA B - check zero flag
        [InlineData(0x0, 0x0, 0x29, 0x0E, 0x79, false, true)] // SRA C - check zero flag
        [InlineData(0x0, 0x0, 0x2A, 0x16, 0x7A, false, true)] // SRA D - check zero flag
        [InlineData(0x0, 0x0, 0x2B, 0x1E, 0x7B, false, true)] // SRA E - check zero flag
        [InlineData(0x0, 0x0, 0x2C, 0x26, 0x7C, false, true)] // SRA H - check zero flag
        [InlineData(0x0, 0x0, 0x2D, 0x2E, 0x7D, false, true)] // SRA L - check zero flag
        [InlineData(0x0, 0x0, 0x2E, 0x36, 0x7E, false, true)] // SRA (HL) - check zero flag
        [InlineData(0x0, 0x0, 0x2F, 0x3E, 0x7F, false, true)] // SRA A - check zero flag
        [InlineData(0x1, 0x0, 0x28, 0x06, 0x78, true, true)] // SLA B - check carry & zero flag
        [InlineData(0x1, 0x0, 0x29, 0x0E, 0x79, true, true)] // SLA C - check zero flag & zero flag
        [InlineData(0x1, 0x0, 0x2A, 0x16, 0x7A, true, true)] // SLA D - check zero flag & zero flag
        [InlineData(0x1, 0x0, 0x2B, 0x1E, 0x7B, true, true)] // SLA E - check zero flag & zero flag
        [InlineData(0x1, 0x0, 0x2C, 0x26, 0x7C, true, true)] // SLA H - check zero flag & zero flag
        [InlineData(0x1, 0x0, 0x2D, 0x2E, 0x7D, true, true)] // SLA L - check zero flag & zero flag
        [InlineData(0x1, 0x0, 0x2E, 0x36, 0x7E, true, true)] // SLA (HL) - check zero flag & zero flag
        [InlineData(0x1, 0x0, 0x2F, 0x3E, 0x7F, true, true)] // SLA A - check zero flag & zero flag
        [InlineData(0xFF, 0xFF, 0x28, 0x06, 0x78, true, false)] // SLA B - check not zero but carry
        [InlineData(0xFF, 0xFF, 0x29, 0x0E, 0x79, true, false)] // SLA C - check not zero but carry
        [InlineData(0xFF, 0xFF, 0x2A, 0x16, 0x7A, true, false)] // SLA D - check not zero but carry
        [InlineData(0xFF, 0xFF, 0x2B, 0x1E, 0x7B, true, false)] // SLA E - check not zero but carry
        [InlineData(0xFF, 0xFF, 0x2C, 0x26, 0x7C, true, false)] // SLA H - check not zero but carry
        [InlineData(0xFF, 0xFF, 0x2D, 0x2E, 0x7D, true, false)] // SLA L - check not zero but carry
        [InlineData(0xFF, 0xFF, 0x2E, 0x36, 0x7E, true, false)] // SLA (HL) - check not zero but carry
        [InlineData(0xFF, 0xFF, 0x2F, 0x3E, 0x7F, true, false)] // SLA A - check not zero but carry
        public void TestSRA(byte initialValue, byte expectedValue, byte shiftOpcode, byte loadOpcode, byte loadRegIntoA, bool c, bool z)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x00, 0xC0, // Load 0xC000 into HL
                loadOpcode, initialValue, // Load into reg
                0x37, // Set carry flag to 1
                0x3F, // CCF to unset carry flag so we know if it's been set properly
                0xCB, shiftOpcode, // Shift reg (opcode under test)
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
