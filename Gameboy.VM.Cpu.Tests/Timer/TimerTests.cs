using Gameboy.VM.Timers;
using Xunit;

namespace Gameboy.VM.Tests.Timer
{
    /// <summary>
    /// Low level tests of setting register values in the LCD unit
    /// </summary>
    public class TimerTests
    {
        [Fact]
        public void TestTimerAt4096Frequency()
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0xFB, // Enable interrupts
                0x0E, 0x07, // LD C, 0x7
                0x3E, 0x04, // LD A, 0x4
                0xE2, // LD (C), A - i.e. turn on the timer with frequency 4096
            });

            for (var ii = 0; ii < 6; ii++) device.Step(); // Turn the timer on but perform no other operations

            // TODO - Timer will already be 8 internal cycles because of ordering of CPU -> Timer when a CPU operation can turn the timer on and then immediately count those instruction cycles. likely wrong behavior
            Assert.Equal(0, device.Timer.TimerCounter);
            device.Step(); // 1 NOPs takes 12 cycles so timer should still not increment
            Assert.Equal(0, device.Timer.TimerCounter);
            device.Step(); // 1 More NOP to take 16 cycles so timer should now increment
            Assert.Equal(1, device.Timer.TimerCounter);
        }

    }
}
