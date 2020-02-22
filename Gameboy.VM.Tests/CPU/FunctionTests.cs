using Xunit;

namespace Gameboy.VM.Tests.CPU
{
    /// <summary>
    /// Tests cover opcodes:
    ///  - C4 - CALL NZ,a16
    ///  - CC - CALL Z,a16
    ///  - D4 - CALL NC,a16
    ///  - DC - CALL C,a16
    ///  - CD - CALL a16
    /// </summary>
    public class FunctionTests
    {
        [Fact]
        public void TestCallFunctionAndReturn()
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x31, 0xFE, 0xFF, // Setup the SP to 0xFFFE - 3 m-cycles
                0xCD, 0x57, 0x01, // 0x153 - Call 0x157 - 6 m-cycles
                0x00, // 0x154 - NOP - 1 m-cycles
                0xC9, // 0x155 - RET - 4 m-cycles
            });

            for (var ii = 0; ii < 5; ii++) // 5 to move to 0x150
            {
                device.Step();
            }

            device.Step(); device.Step(); device.Step();

            Assert.Equal(0xFFFE, device.CPU.Registers.StackPointer);

            device.Step(); device.Step(); device.Step(); device.Step(); device.Step(); device.Step();
            Assert.Equal(0x0157, device.CPU.Registers.ProgramCounter);
            Assert.Equal(0xFFFC, device.CPU.Registers.StackPointer);

            device.Step(); device.Step(); device.Step(); device.Step();

            Assert.Equal(0x0156, device.CPU.Registers.ProgramCounter);
            Assert.Equal(0xFFFE, device.CPU.Registers.StackPointer);
        }

        [Theory]
        [InlineData(0x00, 0xD4, 0x158, 0xFFFE, 3)] // CALL NC (no)
        [InlineData(0x3F, 0xD4, 0x159, 0xFFFC, 6)] // CALL NC (yes)
        [InlineData(0x00, 0xDC, 0x159, 0xFFFC, 6)] // CALL C (yes)
        [InlineData(0x3F, 0xDC, 0x158, 0xFFFE, 3)] // CALL C (no)
        public void TestCallFunctionOnCarry(byte flipCarryOrNoop, byte callOpcode, ushort programCounter, ushort stackPointer, int cyclesForJump)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x31, 0xFE, 0xFF, // Setup the SP to 0xFFFE - 3 m-cycles
                0x37, // Make sure that CARRY flag is true with SCF - 1 m-cycle
                flipCarryOrNoop, // Either CCF to flip the flag or NOP to leave it set - 1 m-cycles
                callOpcode, 0x59, 0x01, // 0x155 - CALL 0x159 if NC/C - 6 m-cycles if jump or 3 if not
                0x00, // 0x158 - NOP - 1 m-cycle
                0xC9, // 0x159 - RET - 4 m-cycles
            });

            for (var ii = 0; ii < 10; ii++) // 5 to move to 0x150, 5 to set SP and set C
            {
                device.Step();
            }

            Assert.Equal(0xFFFE, device.CPU.Registers.StackPointer);
            Assert.Equal(flipCarryOrNoop == 0x00, device.CPU.Registers.GetFlag(VM.CPU.CpuFlags.CarryFlag));

            for (var ii = 0; ii < cyclesForJump; ii++) device.Step();
            Assert.Equal(programCounter, device.CPU.Registers.ProgramCounter);
            Assert.Equal(stackPointer, device.CPU.Registers.StackPointer);
        }

        [Theory]
        [InlineData(0xFF, 0xC4, 0x159, 0xFFFE, 3)] // CALL NZ (no)
        [InlineData(0xFE, 0xC4, 0x15A, 0xFFFC, 6)] // CALL NZ (yes)
        [InlineData(0xFF, 0xCC, 0x15A, 0xFFFC, 6)] // CALL Z (yes)
        [InlineData(0xFE, 0xCC, 0x159, 0xFFFE, 3)] // CALL Z (no)
        public void TestCallFunctionOnZero(byte startingA, byte callOpcode, ushort programCounter, ushort stackPointer, int cyclesForJump)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x31, 0xFE, 0xFF, // Setup the SP to 0xFFFE - 3 m-cycles
                0x3E, startingA, // LD A, [FF,FE] - 2 m-cycles
                0x3C, // INC A (set Z flag if A was FF otherwise unset it) - 1 m-cycle
                callOpcode, 0x5A, 0x01, // 0x156 - CALL 0x15A if NZ/Z
                0x00, // 0x159 - NOP
                0xC9, // 0x15A - RET
            });

            for (var ii = 0; ii < 11; ii++) // 5 to move to 0x150, 6 to set SP and set Z
            {
                device.Step();
            }

            Assert.Equal(0xFFFE, device.CPU.Registers.StackPointer);
            Assert.Equal(startingA == 0xFF, device.CPU.Registers.GetFlag(VM.CPU.CpuFlags.ZeroFlag));

            for (var ii = 0; ii < cyclesForJump; ii++) device.Step();
            Assert.Equal(programCounter, device.CPU.Registers.ProgramCounter);
            Assert.Equal(stackPointer, device.CPU.Registers.StackPointer);
        }
    }
}
