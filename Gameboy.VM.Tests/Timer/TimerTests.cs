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
            var cartridgeContents = new byte[300];
            cartridgeContents[0x0] = 0xFB;
            cartridgeContents[0x1] = 0x0E;
            cartridgeContents[0x2] = 0x07;
            cartridgeContents[0x3] = 0x3E;
            cartridgeContents[0x4] = 0x04;
            cartridgeContents[0x5] = 0xE2;
            var device = TestUtils.CreateTestDevice(cartridgeContents);

            for (var ii = 0; ii < 6; ii++) device.Step(); // Turn the timer on but perform no other operations

            Assert.Equal(0, device.Timer.TimerCounter);

            // At 4096 there are 1024 cycles per TAC increment, (1024 - 8) / 4 = 254 NOPs before increment
            for (var ii = 0; ii < 253; ii++) device.Step();
            Assert.Equal(0, device.Timer.TimerCounter);

            // 1 More NOP to take total to 1024 cycles so timer should now increment
            device.Step(); 
            Assert.Equal(1, device.Timer.TimerCounter);
        }

    }
}
