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
                loadReg1Opcode, 0x05, // LD reg1, 0x05
                loadReg2Opcode, 0x11, // LD reg2, 0x11
                pushOpcode, // PUSH
                loadReg1Opcode, 0x00, // LD reg1, 0x00
                loadReg2Opcode, 0x00, // LD reg2, 0x00
                popOpcode, // POP
            });

            for (var ii = 0; ii < 8; ii++) // 6 ops + 2 to move to 0x150
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
                0x3E, 0x05, // LD A, 0x05
                0x37, // SCF to force F = Carry
                0xF5, // PUSH AF
                0x3E, 0x00, // LD reg1, 0x00
                0xF1, // POP AF
            });

            for (var ii = 0; ii < 8; ii++) // 5 ops + 2 to move to 0x150
            {
                device.Step();
            }

            Assert.Equal(0x0610, device.CPU.Registers.AF);
        }
    }
}
