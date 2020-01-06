using Gameboy.VM.CPU;
using Xunit;

namespace Gameboy.VM.Cpu.Tests.CPU
{
    public class StackTests
    {
        [Fact]
        public void TestStackPushPop()
        {
            var cpu = TestUtils.CreateCPU();
            var alu = new ALU(cpu);

            cpu.Registers.StackPointer = 0xFFFE;
            cpu.Registers.B = 0x1;
            cpu.Registers.C = 0x2;
            alu.PushToStack(cpu.Registers.BC);
            Assert.Equal(0xFFFC, cpu.Registers.StackPointer);

            cpu.Registers.BC = 0x0;

            alu.PopFromStackIntoRegister(Register16Bit.BC);

            Assert.Equal(0x1, cpu.Registers.B);
            Assert.Equal(0x2, cpu.Registers.C);
            Assert.Equal(0xFFFE, cpu.Registers.StackPointer);
        }
    }
}
