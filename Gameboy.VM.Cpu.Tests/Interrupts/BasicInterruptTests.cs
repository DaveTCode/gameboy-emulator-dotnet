using System;
using System.Linq;
using Gameboy.VM.Interrupts;
using Xunit;

namespace Gameboy.VM.Tests.Interrupts
{
    public class BasicInterruptTests
    {
        [Theory]
        [InlineData((int)Interrupt.VerticalBlank, 0b00000001)]
        [InlineData((int)Interrupt.LCDSTAT, 0b00000010)]
        [InlineData((int)Interrupt.Timer, 0b00000100)]
        [InlineData((int)Interrupt.Serial, 0b00001000)]
        [InlineData((int)Interrupt.Joypad, 0b00010000)]
        public void TestAllInterruptsCanBeEnabled(in int interrupt, byte expectedValue)
        {
            var interruptRegisters = new InterruptRegisters();
            interruptRegisters.RequestInterrupt((Interrupt)interrupt);

            Assert.Equal(expectedValue, interruptRegisters.InterruptRequest);
        }

        [Fact]
        public void TestInterruptsCanOverlap()
        {
            var interruptRegisters = new InterruptRegisters();
            interruptRegisters.RequestInterrupt(Interrupt.VerticalBlank);
            interruptRegisters.RequestInterrupt(Interrupt.LCDSTAT);
            Assert.Equal(0b00000011, interruptRegisters.InterruptRequest);

            interruptRegisters.RequestInterrupt(Interrupt.Timer);
            interruptRegisters.RequestInterrupt(Interrupt.Serial);
            Assert.Equal(0b00001111, interruptRegisters.InterruptRequest);

            interruptRegisters.RequestInterrupt(Interrupt.Joypad);
            Assert.Equal(0b00011111, interruptRegisters.InterruptRequest);
        }

        [Fact]
        public void TestInterruptResetWorks()
        {
            var interruptRegisters = new InterruptRegisters();
            foreach (var interrupt in Enum.GetValues(typeof(Interrupt)).Cast<Interrupt>())
            {
                interruptRegisters.RequestInterrupt(interrupt);
                Assert.Equal(interrupt.Mask(), interruptRegisters.InterruptRequest);
                interruptRegisters.ResetInterrupt(interrupt);
                Assert.Equal(0x0, interruptRegisters.InterruptRequest);
            }
        }
    }
}
