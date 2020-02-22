using System;
using Xunit;

namespace Gameboy.VM.Tests.CPU
{
    /// <summary>
    /// Tests all PUSH/POP opcodes
    /// </summary>
    public class StackTests
    {
        [Theory]
        [InlineData(0x06, 0x0E, 0xC5, 0xC1)] // BC
        [InlineData(0x16, 0x1E, 0xD5, 0xD1)] // DE
        [InlineData(0x26, 0x2E, 0xE5, 0xE1)] // HL
        public void TestStackPushPop(byte loadReg1Opcode, byte loadReg2Opcode, byte pushOpcode, byte popOpcode)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                loadReg1Opcode, 0x05, // LD reg1, 0x05 - 2 m-cycles
                loadReg2Opcode, 0x11, // LD reg2, 0x11 - 2 m-cycles
                pushOpcode, // PUSH - 4 m-cycles
                loadReg1Opcode, 0x00, // LD reg1, 0x00 - 2 m-cycles
                loadReg2Opcode, 0x00, // LD reg2, 0x00 - 2 m-cycles
                popOpcode, // POP - 4 m-cycles
            });

            for (var ii = 0; ii < 21; ii++) // 16 ops + 5 to move to 0x150
            {
                device.Step();
            }

            Assert.Equal(0x0511, actual: pushOpcode switch
            {
                0xC5 => device.CPU.Registers.BC,
                0xD5 => device.CPU.Registers.DE,
                0xE5 => device.CPU.Registers.HL,
                _ => throw new ArgumentOutOfRangeException(nameof(pushOpcode), pushOpcode, "Push opcode not mapped to register")
            });
        }

        [Fact]
        public void TestStackPushPopAF()
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x3E, 0x05, // LD A, 0x05 - 2 m-cycles
                0x37, // SCF to force F = Carry - 1 m-cycle
                0xF5, // PUSH AF - 4 m-cycles
                0x3E, 0x00, // LD A, 0x00 - 2 m-cycles
                0xF1, // POP AF - 3 m-cycles
            });

            for (var ii = 0; ii < 17; ii++) // 12 ops + 5 to move to 0x150
            {
                device.Step();
            }

            Assert.Equal(0x0590, device.CPU.Registers.AF);
        }
    }
}
