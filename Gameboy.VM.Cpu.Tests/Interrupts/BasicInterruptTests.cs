using System;
using System.Linq;
using Gameboy.VM.Interrupts;
using Xunit;

namespace Gameboy.VM.Tests.Interrupts
{
    public class BasicInterruptTests
    {
        [Theory]
        [InlineData((int)Interrupt.VerticalBlank, 0b11100001)]
        [InlineData((int)Interrupt.LCDSTAT, 0b11100010)]
        [InlineData((int)Interrupt.Timer, 0b11100100)]
        [InlineData((int)Interrupt.Serial, 0b11101000)]
        [InlineData((int)Interrupt.Joypad, 0b11110000)]
        public void TestAllInterruptsCanBeEnabled(int interrupt, byte expectedValue)
        {
            var interruptRegisters = new InterruptRegisters();
            interruptRegisters.RequestInterrupt((Interrupt)interrupt);

            Assert.Equal(expectedValue, interruptRegisters.InterruptFlags);
        }

        [Fact]
        public void TestInterruptsCanOverlap()
        {
            var interruptRegisters = new InterruptRegisters();
            interruptRegisters.RequestInterrupt(Interrupt.VerticalBlank);
            interruptRegisters.RequestInterrupt(Interrupt.LCDSTAT);
            Assert.Equal(0b11100011, interruptRegisters.InterruptFlags);

            interruptRegisters.RequestInterrupt(Interrupt.Timer);
            interruptRegisters.RequestInterrupt(Interrupt.Serial);
            Assert.Equal(0b11101111, interruptRegisters.InterruptFlags);

            interruptRegisters.RequestInterrupt(Interrupt.Joypad);
            Assert.Equal(0b11111111, interruptRegisters.InterruptFlags);
        }

        [Fact]
        public void TestInterruptResetWorks()
        {
            var interruptRegisters = new InterruptRegisters();
            foreach (var interrupt in Enum.GetValues(typeof(Interrupt)).Cast<Interrupt>())
            {
                interruptRegisters.RequestInterrupt(interrupt);
                Assert.Equal(interrupt.Mask() | 0b11100000, interruptRegisters.InterruptFlags);
                interruptRegisters.ResetInterrupt(interrupt);
                Assert.Equal(0b11100000, interruptRegisters.InterruptFlags);
            }
        }
    }
}
