using Gameboy.VM.CPU;
using Xunit;

namespace Gameboy.VM.Cpu.Tests.CPU
{
    public class FunctionTests
    {
        [Fact]
        public void TestCallFunctionAndReturn()
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);

            cpu.Registers.StackPointer = 0xFFFE;
            cpu.Registers.ProgramCounter = 0x0100;
            alu.Call(0x0200);
            Assert.Equal(0x0200, cpu.Registers.ProgramCounter);
            Assert.Equal(0xFFFC, cpu.Registers.StackPointer);

            alu.Return();

            Assert.Equal(0x0100, cpu.Registers.ProgramCounter);
            Assert.Equal(0xFFFE, cpu.Registers.StackPointer);
        }
    }
}
