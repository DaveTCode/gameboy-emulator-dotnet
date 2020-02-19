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
            alu.RotateLeftWithCarry(ref a);

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
            alu.RotateLeftNoCarry(ref a);

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
            alu.RotateRightWithCarry(ref a);

            Assert.Equal(result, a);
            Assert.Equal(c, cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(n, cpu.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, cpu.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x0, 0x0, false, false)] // RRA 0 is still 0 
        [InlineData(0x1, 0x0, false, true)] // RRA 1 is 0 with carry set
        [InlineData(0x0, 0x80, true, false)] // RRA carry bit applied
        [InlineData(0x1, 0x80, true, true)] // RRA carry bit applied and also stored
        [InlineData(0x81, 0x40, false, true)]
        public void TestRRA(byte a, byte result, bool cBefore, bool c)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x3E, a, // LD A, a
                0x37, // SCF to set carry flag
                (byte)((cBefore) ? 0x00 : 0x3F), // Unset carry flag with CCF as required by test
                0x1F, // RRA
            });

            for (var ii = 0; ii < 6; ii++) // 2 to get into PC 150 then 4 to act
            {
                device.Step();
            }

            Assert.Equal(result, device.CPU.Registers.A);
            Assert.Equal(c, device.CPU.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.ZeroFlag));
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
        [InlineData(0x0, 0x0, 0x30, 0x06, 0x78, true)] // SRA B - check zero flag
        [InlineData(0x0, 0x0, 0x31, 0x0E, 0x79, true)] // SRA C - check zero flag
        [InlineData(0x0, 0x0, 0x32, 0x16, 0x7A, true)] // SRA D - check zero flag
        [InlineData(0x0, 0x0, 0x33, 0x1E, 0x7B, true)] // SRA E - check zero flag
        [InlineData(0x0, 0x0, 0x34, 0x26, 0x7C, true)] // SRA H - check zero flag
        [InlineData(0x0, 0x0, 0x35, 0x2E, 0x7D, true)] // SRA L - check zero flag
        [InlineData(0x0, 0x0, 0x36, 0x36, 0x7E, true)] // SRA (HL) - check zero flag
        [InlineData(0x0, 0x0, 0x37, 0x3E, 0x7F, true)] // SRA A - check zero flag
        [InlineData(0xF0, 0x0F, 0x30, 0x06, 0x78, false)] // SRA B - check expected value
        [InlineData(0xF0, 0x0F, 0x31, 0x0E, 0x79, false)] // SRA C - check expected value
        [InlineData(0xF0, 0x0F, 0x32, 0x16, 0x7A, false)] // SRA D - check expected value
        [InlineData(0xF0, 0x0F, 0x33, 0x1E, 0x7B, false)] // SRA E - check expected value
        [InlineData(0xF0, 0x0F, 0x34, 0x26, 0x7C, false)] // SRA H - check expected value
        [InlineData(0xF0, 0x0F, 0x35, 0x2E, 0x7D, false)] // SRA L - check expected value
        [InlineData(0xF0, 0x0F, 0x36, 0x36, 0x7E, false)] // SRA (HL) - check expected value
        [InlineData(0xF0, 0x0F, 0x37, 0x3E, 0x7F, false)] // SRA A - check expected value

        public void TestSWAP(byte initialValue, byte expectedValue, byte swapOpcode, byte loadOpcode, byte loadRegIntoA, bool z)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x00, 0xC0, // Load 0xC000 into HL
                loadOpcode, initialValue, // Load into reg
                0x37, // Set carry flag to 1
                0x3F, // CCF to unset carry flag so we know if it's been set properly
                0xCB, swapOpcode, // SWAP reg (opcode under test)
                loadRegIntoA, // Move result to A for evaluation
            });

            for (var ii = 0; ii < 8; ii++) // 2 to get to PC 0x150 then 6 to setup and act
            {
                device.Step();
            }

            Assert.Equal(expectedValue, device.CPU.Registers.A);
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(z, device.CPU.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x0, 0x0, 0x38, 0x06, 0x78, false, true)] // SRL B - check zero flag
        [InlineData(0x0, 0x0, 0x39, 0x0E, 0x79, false, true)] // SRL C - check zero flag
        [InlineData(0x0, 0x0, 0x3A, 0x16, 0x7A, false, true)] // SRL D - check zero flag
        [InlineData(0x0, 0x0, 0x3B, 0x1E, 0x7B, false, true)] // SRL E - check zero flag
        [InlineData(0x0, 0x0, 0x3C, 0x26, 0x7C, false, true)] // SRL H - check zero flag
        [InlineData(0x0, 0x0, 0x3D, 0x2E, 0x7D, false, true)] // SRL L - check zero flag
        [InlineData(0x0, 0x0, 0x3E, 0x36, 0x7E, false, true)] // SRL (HL) - check zero flag
        [InlineData(0x0, 0x0, 0x3F, 0x3E, 0x7F, false, true)] // SRL A - check zero flag
        [InlineData(0x1, 0x0, 0x38, 0x06, 0x78, true, true)] // SRL B - check carry & zero flag
        [InlineData(0x1, 0x0, 0x39, 0x0E, 0x79, true, true)] // SRL C - check carry & zero flag
        [InlineData(0x1, 0x0, 0x3A, 0x16, 0x7A, true, true)] // SRL D - check carry & zero flag
        [InlineData(0x1, 0x0, 0x3B, 0x1E, 0x7B, true, true)] // SRL E - check carry & zero flag
        [InlineData(0x1, 0x0, 0x3C, 0x26, 0x7C, true, true)] // SRL H - check carry & zero flag
        [InlineData(0x1, 0x0, 0x3D, 0x2E, 0x7D, true, true)] // SRL L - check carry & zero flag
        [InlineData(0x1, 0x0, 0x3E, 0x36, 0x7E, true, true)] // SRL (HL) - check carry & zero flag
        [InlineData(0x1, 0x0, 0x3F, 0x3E, 0x7F, true, true)] // SRL A - check carry & zero flag
        [InlineData(0xFF, 0x7F, 0x38, 0x06, 0x78, true, false)] // SLA B - check not zero but carry
        [InlineData(0xFF, 0x7F, 0x39, 0x0E, 0x79, true, false)] // SLA C - check not zero but carry
        [InlineData(0xFF, 0x7F, 0x3A, 0x16, 0x7A, true, false)] // SLA D - check not zero but carry
        [InlineData(0xFF, 0x7F, 0x3B, 0x1E, 0x7B, true, false)] // SLA E - check not zero but carry
        [InlineData(0xFF, 0x7F, 0x3C, 0x26, 0x7C, true, false)] // SLA H - check not zero but carry
        [InlineData(0xFF, 0x7F, 0x3D, 0x2E, 0x7D, true, false)] // SLA L - check not zero but carry
        [InlineData(0xFF, 0x7F, 0x3E, 0x36, 0x7E, true, false)] // SLA (HL) - check not zero but carry
        [InlineData(0xFF, 0x7F, 0x3F, 0x3E, 0x7F, true, false)] // SLA A - check not zero but carry
        public void TestSRL(byte initialValue, byte expectedValue, byte shiftOpcode, byte loadOpcode, byte loadRegIntoA, bool c, bool z)
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
        [InlineData(0x0, 0x40, 0x06, true)] // BIT 0,B - Set
        [InlineData(0x0, 0x41, 0x0E, true)] // BIT 0,C - Set
        [InlineData(0x0, 0x42, 0x16, true)] // BIT 0,D - Set
        [InlineData(0x0, 0x43, 0x1E, true)] // BIT 0,E - Set
        [InlineData(0x0, 0x44, 0x26, true)] // BIT 0,H - Set
        [InlineData(0x0, 0x45, 0x2E, true)] // BIT 0,L - Set
        [InlineData(0x0, 0x46, 0x36, true)] // BIT 0,(HL) - Set
        [InlineData(0x0, 0x47, 0x3E, true)] // BIT 0,A - Set
        [InlineData(0x1, 0x40, 0x06, false)] // BIT 0,B - Unset
        [InlineData(0x1, 0x41, 0x0E, false)] // BIT 0,C - Unset
        [InlineData(0x1, 0x42, 0x16, false)] // BIT 0,D - Unset
        [InlineData(0x1, 0x43, 0x1E, false)] // BIT 0,E - Unset
        [InlineData(0x1, 0x44, 0x26, false)] // BIT 0,H - Unset
        [InlineData(0x1, 0x45, 0x2E, false)] // BIT 0,L - Unset
        [InlineData(0x1, 0x46, 0x36, false)] // BIT 0,(HL) - Unset
        [InlineData(0x1, 0x47, 0x3E, false)] // BIT 0,A - Unset

        [InlineData(0x0, 0x48, 0x06, true)] // BIT 1,B - Set
        [InlineData(0x0, 0x49, 0x0E, true)] // BIT 1,C - Set
        [InlineData(0x0, 0x4A, 0x16, true)] // BIT 1,D - Set
        [InlineData(0x0, 0x4B, 0x1E, true)] // BIT 1,E - Set
        [InlineData(0x0, 0x4C, 0x26, true)] // BIT 1,H - Set
        [InlineData(0x0, 0x4D, 0x2E, true)] // BIT 1,L - Set
        [InlineData(0x0, 0x4E, 0x36, true)] // BIT 1,(HL) - Set
        [InlineData(0x0, 0x4F, 0x3E, true)] // BIT 1,A - Set
        [InlineData(0x2, 0x48, 0x06, false)] // BIT 1,B - Unset
        [InlineData(0x2, 0x49, 0x0E, false)] // BIT 1,C - Unset
        [InlineData(0x2, 0x4A, 0x16, false)] // BIT 1,D - Unset
        [InlineData(0x2, 0x4B, 0x1E, false)] // BIT 1,E - Unset
        [InlineData(0x2, 0x4C, 0x26, false)] // BIT 1,H - Unset
        [InlineData(0x2, 0x4D, 0x2E, false)] // BIT 1,L - Unset
        [InlineData(0x2, 0x4E, 0x36, false)] // BIT 1,(HL) - Unset
        [InlineData(0x2, 0x4F, 0x3E, false)] // BIT 1,A - Unset

        [InlineData(0x0, 0x50, 0x06, true)] // BIT 2,B - Set
        [InlineData(0x0, 0x51, 0x0E, true)] // BIT 2,C - Set
        [InlineData(0x0, 0x52, 0x16, true)] // BIT 2,D - Set
        [InlineData(0x0, 0x53, 0x1E, true)] // BIT 2,E - Set
        [InlineData(0x0, 0x54, 0x26, true)] // BIT 2,H - Set
        [InlineData(0x0, 0x55, 0x2E, true)] // BIT 2,L - Set
        [InlineData(0x0, 0x56, 0x36, true)] // BIT 2,(HL) - Set
        [InlineData(0x0, 0x57, 0x3E, true)] // BIT 2,A - Set
        [InlineData(0x4, 0x50, 0x06, false)] // BIT 2,B - Unset
        [InlineData(0x4, 0x51, 0x0E, false)] // BIT 2,C - Unset
        [InlineData(0x4, 0x52, 0x16, false)] // BIT 2,D - Unset
        [InlineData(0x4, 0x53, 0x1E, false)] // BIT 2,E - Unset
        [InlineData(0x4, 0x54, 0x26, false)] // BIT 2,H - Unset
        [InlineData(0x4, 0x55, 0x2E, false)] // BIT 2,L - Unset
        [InlineData(0x4, 0x56, 0x36, false)] // BIT 2,(HL) - Unset
        [InlineData(0x4, 0x57, 0x3E, false)] // BIT 2,A - Unset

        [InlineData(0x0, 0x58, 0x06, true)] // BIT 3,B - Set
        [InlineData(0x0, 0x59, 0x0E, true)] // BIT 3,C - Set
        [InlineData(0x0, 0x5A, 0x16, true)] // BIT 3,D - Set
        [InlineData(0x0, 0x5B, 0x1E, true)] // BIT 3,E - Set
        [InlineData(0x0, 0x5C, 0x26, true)] // BIT 3,H - Set
        [InlineData(0x0, 0x5D, 0x2E, true)] // BIT 3,L - Set
        [InlineData(0x0, 0x5E, 0x36, true)] // BIT 3,(HL) - Set
        [InlineData(0x0, 0x5F, 0x3E, true)] // BIT 3,A - Set
        [InlineData(0x8, 0x58, 0x06, false)] // BIT 3,B - Unset
        [InlineData(0x8, 0x59, 0x0E, false)] // BIT 3,C - Unset
        [InlineData(0x8, 0x5A, 0x16, false)] // BIT 3,D - Unset
        [InlineData(0x8, 0x5B, 0x1E, false)] // BIT 3,E - Unset
        [InlineData(0x8, 0x5C, 0x26, false)] // BIT 3,H - Unset
        [InlineData(0x8, 0x5D, 0x2E, false)] // BIT 3,L - Unset
        [InlineData(0x8, 0x5E, 0x36, false)] // BIT 3,(HL) - Unset
        [InlineData(0x8, 0x5F, 0x3E, false)] // BIT 3,A - Unset

        [InlineData(0x0, 0x60, 0x06, true)] // BIT 4,B - Set
        [InlineData(0x0, 0x61, 0x0E, true)] // BIT 4,C - Set
        [InlineData(0x0, 0x62, 0x16, true)] // BIT 4,D - Set
        [InlineData(0x0, 0x63, 0x1E, true)] // BIT 4,E - Set
        [InlineData(0x0, 0x64, 0x26, true)] // BIT 4,H - Set
        [InlineData(0x0, 0x65, 0x2E, true)] // BIT 4,L - Set
        [InlineData(0x0, 0x66, 0x36, true)] // BIT 4,(HL) - Set
        [InlineData(0x0, 0x67, 0x3E, true)] // BIT 4,A - Set
        [InlineData(0x10, 0x60, 0x06, false)] // BIT 4,B - Unset
        [InlineData(0x10, 0x61, 0x0E, false)] // BIT 4,C - Unset
        [InlineData(0x10, 0x62, 0x16, false)] // BIT 4,D - Unset
        [InlineData(0x10, 0x63, 0x1E, false)] // BIT 4,E - Unset
        [InlineData(0x10, 0x64, 0x26, false)] // BIT 4,H - Unset
        [InlineData(0x10, 0x65, 0x2E, false)] // BIT 4,L - Unset
        [InlineData(0x10, 0x66, 0x36, false)] // BIT 4,(HL) - Unset
        [InlineData(0x10, 0x67, 0x3E, false)] // BIT 4,A - Unset

        [InlineData(0x0, 0x68, 0x06, true)] // BIT 5,B - Set
        [InlineData(0x0, 0x69, 0x0E, true)] // BIT 5,C - Set
        [InlineData(0x0, 0x6A, 0x16, true)] // BIT 5,D - Set
        [InlineData(0x0, 0x6B, 0x1E, true)] // BIT 5,E - Set
        [InlineData(0x0, 0x6C, 0x26, true)] // BIT 5,H - Set
        [InlineData(0x0, 0x6D, 0x2E, true)] // BIT 5,L - Set
        [InlineData(0x0, 0x6E, 0x36, true)] // BIT 5,(HL) - Set
        [InlineData(0x0, 0x6F, 0x3E, true)] // BIT 5,A - Set
        [InlineData(0x20, 0x68, 0x06, false)] // BIT 5,B - Unset
        [InlineData(0x20, 0x69, 0x0E, false)] // BIT 5,C - Unset
        [InlineData(0x20, 0x6A, 0x16, false)] // BIT 5,D - Unset
        [InlineData(0x20, 0x6B, 0x1E, false)] // BIT 5,E - Unset
        [InlineData(0x20, 0x6C, 0x26, false)] // BIT 5,H - Unset
        [InlineData(0x20, 0x6D, 0x2E, false)] // BIT 5,L - Unset
        [InlineData(0x20, 0x6E, 0x36, false)] // BIT 5,(HL) - Unset
        [InlineData(0x20, 0x6F, 0x3E, false)] // BIT 5,A - Unset

        [InlineData(0x0, 0x70, 0x06, true)] // BIT 6,B - Set
        [InlineData(0x0, 0x71, 0x0E, true)] // BIT 6,C - Set
        [InlineData(0x0, 0x72, 0x16, true)] // BIT 6,D - Set
        [InlineData(0x0, 0x73, 0x1E, true)] // BIT 6,E - Set
        [InlineData(0x0, 0x74, 0x26, true)] // BIT 6,H - Set
        [InlineData(0x0, 0x75, 0x2E, true)] // BIT 6,L - Set
        [InlineData(0x0, 0x77, 0x36, true)] // BIT 6,(HL) - Set
        [InlineData(0x0, 0x77, 0x3E, true)] // BIT 6,A - Set
        [InlineData(0x40, 0x70, 0x06, false)] // BIT 6,B - Unset
        [InlineData(0x40, 0x71, 0x0E, false)] // BIT 6,C - Unset
        [InlineData(0x40, 0x72, 0x16, false)] // BIT 6,D - Unset
        [InlineData(0x40, 0x73, 0x1E, false)] // BIT 6,E - Unset
        [InlineData(0x40, 0x74, 0x26, false)] // BIT 6,H - Unset
        [InlineData(0x40, 0x75, 0x2E, false)] // BIT 6,L - Unset
        [InlineData(0x40, 0x76, 0x36, false)] // BIT 6,(HL) - Unset
        [InlineData(0x40, 0x77, 0x3E, false)] // BIT 6,A - Unset

        [InlineData(0x0, 0x78, 0x06, true)] // BIT 7,B - Set
        [InlineData(0x0, 0x79, 0x0E, true)] // BIT 7,C - Set
        [InlineData(0x0, 0x7A, 0x16, true)] // BIT 7,D - Set
        [InlineData(0x0, 0x7B, 0x1E, true)] // BIT 7,E - Set
        [InlineData(0x0, 0x7C, 0x26, true)] // BIT 7,H - Set
        [InlineData(0x0, 0x7D, 0x2E, true)] // BIT 7,L - Set
        [InlineData(0x0, 0x7E, 0x36, true)] // BIT 7,(HL) - Set
        [InlineData(0x0, 0x7F, 0x3E, true)] // BIT 7,A - Set
        [InlineData(0x80, 0x78, 0x06, false)] // BIT 7,B - Unset
        [InlineData(0x80, 0x79, 0x0E, false)] // BIT 7,C - Unset
        [InlineData(0x80, 0x7A, 0x16, false)] // BIT 7,D - Unset
        [InlineData(0x80, 0x7B, 0x1E, false)] // BIT 7,E - Unset
        [InlineData(0x80, 0x7C, 0x26, false)] // BIT 7,H - Unset
        [InlineData(0x80, 0x7D, 0x2E, false)] // BIT 7,L - Unset
        [InlineData(0x80, 0x7E, 0x36, false)] // BIT 7,(HL) - Unset
        [InlineData(0x80, 0x7F, 0x3E, false)] // BIT 7,A - Unset
        public void TestBIT(byte initialValue, byte bitOpcode, byte loadOpcode, bool z)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x00, 0xC0, // Load 0xC000 into HL
                loadOpcode, initialValue, // Load into reg
                0x37, // Set carry flag to 1
                0xCB, bitOpcode, // BIT reg (opcode under test)
            });

            for (var ii = 0; ii < 6; ii++) // 2 to get to PC 0x150 then 4 to setup and act
            {
                device.Step();
            }

            Assert.True(device.CPU.Registers.GetFlag(CpuFlags.CarryFlag)); // Test must not touch it
            Assert.True(device.CPU.Registers.GetFlag(CpuFlags.HalfCarryFlag)); // Half carry always set to true by BIT operation
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.SubtractFlag)); // Subtract flag always set to false by BIT operation
            Assert.Equal(z, device.CPU.Registers.GetFlag(CpuFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x0, 0x1, 0xC0, 0x06, 0x78)] // SET 0,B 
        [InlineData(0x0, 0x1, 0xC1, 0x0E, 0x79)] // SET 0,C 
        [InlineData(0x0, 0x1, 0xC2, 0x16, 0x7A)] // SET 0,D 
        [InlineData(0x0, 0x1, 0xC3, 0x1E, 0x7B)] // SET 0,E 
        [InlineData(0x0, 0x1, 0xC4, 0x26, 0x7C)] // SET 0,H 
        [InlineData(0x0, 0x1, 0xC5, 0x2E, 0x7D)] // SET 0,L 
        [InlineData(0x0, 0x1, 0xC6, 0x36, 0x7E)] // SET 0,(HL) 
        [InlineData(0x0, 0x1, 0xC7, 0x3E, 0x7F)] // SET 0,A 

        [InlineData(0x0, 0x2, 0xC8, 0x06, 0x78)] // SET 1,B 
        [InlineData(0x0, 0x2, 0xC9, 0x0E, 0x79)] // SET 1,C 
        [InlineData(0x0, 0x2, 0xcA, 0x16, 0x7A)] // SET 1,D 
        [InlineData(0x0, 0x2, 0xCB, 0x1E, 0x7B)] // SET 1,E 
        [InlineData(0x0, 0x2, 0xcC, 0x26, 0x7C)] // SET 1,H 
        [InlineData(0x0, 0x2, 0xCD, 0x2E, 0x7D)] // SET 1,L 
        [InlineData(0x0, 0x2, 0xCE, 0x36, 0x7E)] // SET 1,(HL) 
        [InlineData(0x0, 0x2, 0xCF, 0x3E, 0x7F)] // SET 1,A 

        [InlineData(0x0, 0x4, 0xD0, 0x06, 0x78)] // SET 2,B 
        [InlineData(0x0, 0x4, 0xD1, 0x0E, 0x79)] // SET 2,C 
        [InlineData(0x0, 0x4, 0xD2, 0x16, 0x7A)] // SET 2,D 
        [InlineData(0x0, 0x4, 0xD3, 0x1E, 0x7B)] // SET 2,E 
        [InlineData(0x0, 0x4, 0xD4, 0x26, 0x7C)] // SET 2,H 
        [InlineData(0x0, 0x4, 0xD5, 0x2E, 0x7D)] // SET 2,L 
        [InlineData(0x0, 0x4, 0xD6, 0x36, 0x7E)] // SET 2,(HL) 
        [InlineData(0x0, 0x4, 0xD7, 0x3E, 0x7F)] // SET 2,A 

        [InlineData(0x0, 0x8, 0xD8, 0x06, 0x78)] // SET 3,B 
        [InlineData(0x0, 0x8, 0xD9, 0x0E, 0x79)] // SET 3,C 
        [InlineData(0x0, 0x8, 0xDA, 0x16, 0x7A)] // SET 3,D 
        [InlineData(0x0, 0x8, 0xDB, 0x1E, 0x7B)] // SET 3,E 
        [InlineData(0x0, 0x8, 0xDC, 0x26, 0x7C)] // SET 3,H 
        [InlineData(0x0, 0x8, 0xDD, 0x2E, 0x7D)] // SET 3,L 
        [InlineData(0x0, 0x8, 0xDE, 0x36, 0x7E)] // SET 3,(HL) 
        [InlineData(0x0, 0x8, 0xDF, 0x3E, 0x7F)] // SET 3,A 

        [InlineData(0x0, 0x10, 0xE0, 0x06, 0x78)] // SET 4,B 
        [InlineData(0x0, 0x10, 0xE1, 0x0E, 0x79)] // SET 4,C 
        [InlineData(0x0, 0x10, 0xE2, 0x16, 0x7A)] // SET 4,D 
        [InlineData(0x0, 0x10, 0xE3, 0x1E, 0x7B)] // SET 4,E 
        [InlineData(0x0, 0x10, 0xE4, 0x26, 0x7C)] // SET 4,H 
        [InlineData(0x0, 0x10, 0xE5, 0x2E, 0x7D)] // SET 4,L 
        [InlineData(0x0, 0x10, 0xE6, 0x36, 0x7E)] // SET 4,(HL) 
        [InlineData(0x0, 0x10, 0xE7, 0x3E, 0x7F)] // SET 4,A 

        [InlineData(0x0, 0x20, 0xE8, 0x06, 0x78)] // SET 5,B 
        [InlineData(0x0, 0x20, 0xE9, 0x0E, 0x79)] // SET 5,C 
        [InlineData(0x0, 0x20, 0xEA, 0x16, 0x7A)] // SET 5,D 
        [InlineData(0x0, 0x20, 0xEB, 0x1E, 0x7B)] // SET 5,E 
        [InlineData(0x0, 0x20, 0xEC, 0x26, 0x7C)] // SET 5,H 
        [InlineData(0x0, 0x20, 0xED, 0x2E, 0x7D)] // SET 5,L 
        [InlineData(0x0, 0x20, 0xEE, 0x36, 0x7E)] // SET 5,(HL) 
        [InlineData(0x0, 0x20, 0xEF, 0x3E, 0x7F)] // SET 5,A 

        [InlineData(0x0, 0x40, 0xF0, 0x06, 0x78)] // SET 6,B 
        [InlineData(0x0, 0x40, 0xF1, 0x0E, 0x79)] // SET 6,C 
        [InlineData(0x0, 0x40, 0xF2, 0x16, 0x7A)] // SET 6,D 
        [InlineData(0x0, 0x40, 0xF3, 0x1E, 0x7B)] // SET 6,E 
        [InlineData(0x0, 0x40, 0xF4, 0x26, 0x7C)] // SET 6,H 
        [InlineData(0x0, 0x40, 0xF5, 0x2E, 0x7D)] // SET 6,L 
        [InlineData(0x0, 0x40, 0xF6, 0x36, 0x7E)] // SET 6,(HL) 
        [InlineData(0x0, 0x40, 0xF7, 0x3E, 0x7F)] // SET 6,A 

        [InlineData(0x0, 0x80, 0xF8, 0x06, 0x78)] // SET 7,B 
        [InlineData(0x0, 0x80, 0xF9, 0x0E, 0x79)] // SET 7,C 
        [InlineData(0x0, 0x80, 0xFA, 0x16, 0x7A)] // SET 7,D 
        [InlineData(0x0, 0x80, 0xFB, 0x1E, 0x7B)] // SET 7,E 
        [InlineData(0x0, 0x80, 0xFC, 0x26, 0x7C)] // SET 7,H 
        [InlineData(0x0, 0x80, 0xFD, 0x2E, 0x7D)] // SET 7,L 
        [InlineData(0x0, 0x80, 0xFE, 0x36, 0x7E)] // SET 7,(HL) 
        [InlineData(0x0, 0x80, 0xFF, 0x3E, 0x7F)] // SET 7,A 
        public void TestSet(byte initialValue, byte expectedValue, byte setOpcode, byte loadOpcode, byte loadRegIntoA)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x00, 0xC0, // Load 0xC000 into HL
                loadOpcode, initialValue, // Load into reg
                0xCB, setOpcode, // SET reg (opcode under test)
                loadRegIntoA // Move reg into A for testing
            });

            for (var ii = 0; ii < 6; ii++) // 2 to get to PC 0x150 then 4 to setup and act
            {
                device.Step();
            }

            Assert.Equal(expectedValue, device.CPU.Registers.A);
        }

        [Theory]
        [InlineData(0xFF, 0xFE, 0x80, 0x06, 0x78)] // RES 0,B 
        [InlineData(0xFF, 0xFE, 0x81, 0x0E, 0x79)] // RES 0,C 
        [InlineData(0xFF, 0xFE, 0x82, 0x16, 0x7A)] // RES 0,D 
        [InlineData(0xFF, 0xFE, 0x83, 0x1E, 0x7B)] // RES 0,E 
        [InlineData(0xFF, 0xFE, 0x84, 0x26, 0x7C)] // RES 0,H 
        [InlineData(0xFF, 0xFE, 0x85, 0x2E, 0x7D)] // RES 0,L 
        [InlineData(0xFF, 0xFE, 0x86, 0x36, 0x7E)] // RES 0,(HL) 
        [InlineData(0xFF, 0xFE, 0x87, 0x3E, 0x7F)] // RES 0,A 

        [InlineData(0xFF, 0xFD, 0x88, 0x06, 0x78)] // RES 1,B 
        [InlineData(0xFF, 0xFD, 0x89, 0x0E, 0x79)] // RES 1,C 
        [InlineData(0xFF, 0xFD, 0x8A, 0x16, 0x7A)] // RES 1,D 
        [InlineData(0xFF, 0xFD, 0x8B, 0x1E, 0x7B)] // RES 1,E 
        [InlineData(0xFF, 0xFD, 0x8C, 0x26, 0x7C)] // RES 1,H 
        [InlineData(0xFF, 0xFD, 0x8D, 0x2E, 0x7D)] // RES 1,L 
        [InlineData(0xFF, 0xFD, 0x8E, 0x36, 0x7E)] // RES 1,(HL) 
        [InlineData(0xFF, 0xFD, 0x8F, 0x3E, 0x7F)] // RES 1,A 

        [InlineData(0xFF, 0xFB, 0x90, 0x06, 0x78)] // RES 2,B 
        [InlineData(0xFF, 0xFB, 0x91, 0x0E, 0x79)] // RES 2,C 
        [InlineData(0xFF, 0xFB, 0x92, 0x16, 0x7A)] // RES 2,D 
        [InlineData(0xFF, 0xFB, 0x93, 0x1E, 0x7B)] // RES 2,E 
        [InlineData(0xFF, 0xFB, 0x94, 0x26, 0x7C)] // RES 2,H 
        [InlineData(0xFF, 0xFB, 0x95, 0x2E, 0x7D)] // RES 2,L 
        [InlineData(0xFF, 0xFB, 0x96, 0x36, 0x7E)] // RES 2,(HL) 
        [InlineData(0xFF, 0xFB, 0x97, 0x3E, 0x7F)] // RES 2,A 

        [InlineData(0xFF, 0xF7, 0x98, 0x06, 0x78)] // RES 3,B 
        [InlineData(0xFF, 0xF7, 0x99, 0x0E, 0x79)] // RES 3,C 
        [InlineData(0xFF, 0xF7, 0x9A, 0x16, 0x7A)] // RES 3,D 
        [InlineData(0xFF, 0xF7, 0x9B, 0x1E, 0x7B)] // RES 3,E 
        [InlineData(0xFF, 0xF7, 0x9C, 0x26, 0x7C)] // RES 3,H 
        [InlineData(0xFF, 0xF7, 0x9D, 0x2E, 0x7D)] // RES 3,L 
        [InlineData(0xFF, 0xF7, 0x9E, 0x36, 0x7E)] // RES 3,(HL) 
        [InlineData(0xFF, 0xF7, 0x9F, 0x3E, 0x7F)] // RES 3,A 

        [InlineData(0xFF, 0xEF, 0xA0, 0x06, 0x78)] // RES 4,B 
        [InlineData(0xFF, 0xEF, 0xA1, 0x0E, 0x79)] // RES 4,C 
        [InlineData(0xFF, 0xEF, 0xA2, 0x16, 0x7A)] // RES 4,D 
        [InlineData(0xFF, 0xEF, 0xA3, 0x1E, 0x7B)] // RES 4,E 
        [InlineData(0xFF, 0xEF, 0xA4, 0x26, 0x7C)] // RES 4,H 
        [InlineData(0xFF, 0xEF, 0xA5, 0x2E, 0x7D)] // RES 4,L 
        [InlineData(0xFF, 0xEF, 0xA6, 0x36, 0x7E)] // RES 4,(HL) 
        [InlineData(0xFF, 0xEF, 0xA7, 0x3E, 0x7F)] // RES 4,A 

        [InlineData(0xFF, 0xDF, 0xA8, 0x06, 0x78)] // RES 5,B 
        [InlineData(0xFF, 0xDF, 0xA9, 0x0E, 0x79)] // RES 5,C 
        [InlineData(0xFF, 0xDF, 0xAA, 0x16, 0x7A)] // RES 5,D 
        [InlineData(0xFF, 0xDF, 0xAB, 0x1E, 0x7B)] // RES 5,E 
        [InlineData(0xFF, 0xDF, 0xAC, 0x26, 0x7C)] // RES 5,H 
        [InlineData(0xFF, 0xDF, 0xAD, 0x2E, 0x7D)] // RES 5,L 
        [InlineData(0xFF, 0xDF, 0xAE, 0x36, 0x7E)] // RES 5,(HL) 
        [InlineData(0xFF, 0xDF, 0xAF, 0x3E, 0x7F)] // RES 5,A 

        [InlineData(0xFF, 0xBF, 0xB0, 0x06, 0x78)] // RES 6,B 
        [InlineData(0xFF, 0xBF, 0xB1, 0x0E, 0x79)] // RES 6,C 
        [InlineData(0xFF, 0xBF, 0xB2, 0x16, 0x7A)] // RES 6,D 
        [InlineData(0xFF, 0xBF, 0xB3, 0x1E, 0x7B)] // RES 6,E 
        [InlineData(0xFF, 0xBF, 0xB4, 0x26, 0x7C)] // RES 6,H 
        [InlineData(0xFF, 0xBF, 0xB5, 0x2E, 0x7D)] // RES 6,L 
        [InlineData(0xFF, 0xBF, 0xB6, 0x36, 0x7E)] // RES 6,(HL) 
        [InlineData(0xFF, 0xBF, 0xB7, 0x3E, 0x7F)] // RES 6,A 

        [InlineData(0xFF, 0x7F, 0xB8, 0x06, 0x78)] // RES 7,B 
        [InlineData(0xFF, 0x7F, 0xB9, 0x0E, 0x79)] // RES 7,C 
        [InlineData(0xFF, 0x7F, 0xBA, 0x16, 0x7A)] // RES 7,D 
        [InlineData(0xFF, 0x7F, 0xBB, 0x1E, 0x7B)] // RES 7,E 
        [InlineData(0xFF, 0x7F, 0xBC, 0x26, 0x7C)] // RES 7,H 
        [InlineData(0xFF, 0x7F, 0xBD, 0x2E, 0x7D)] // RES 7,L 
        [InlineData(0xFF, 0x7F, 0xbE, 0x36, 0x7E)] // RES 7,(HL) 
        [InlineData(0xFF, 0x7F, 0xBF, 0x3E, 0x7F)] // RES 7,A 
        public void TestRes(byte initialValue, byte expectedValue, byte setOpcode, byte loadOpcode, byte loadRegIntoA)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x00, 0xC0, // Load 0xC000 into HL
                loadOpcode, initialValue, // Load into reg
                0xCB, setOpcode, // RES reg (opcode under test)
                loadRegIntoA // Move reg into A for testing
            });

            for (var ii = 0; ii < 6; ii++) // 2 to get to PC 0x150 then 4 to setup and act
            {
                device.Step();
            }

            Assert.Equal(expectedValue, device.CPU.Registers.A);
        }
    }
}
