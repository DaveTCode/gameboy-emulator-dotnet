using Gameboy.VM.Joypad;
using Xunit;

namespace Gameboy.VM.Tests.Joypad
{
    public class JoypadTests
    {
        [Fact]
        public void TestJoypadDefaultState()
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x0E, 0x00, // LD C, 00
                0xF2, // LD A, (C) (get joypad state into A)
            });
            
            for (var ii = 0; ii < 4; ii++) device.Step(); // Step 4 times to set up tests and execute

            Assert.Equal(0xFF, device.CPU.Registers.A);
        }

        [Theory]
        [InlineData(0xEF, DeviceKey.Right, 0xEE)] // Direction button checks
        [InlineData(0xEF, DeviceKey.Left, 0xED)]
        [InlineData(0xEF, DeviceKey.Up, 0xEB)]
        [InlineData(0xEF, DeviceKey.Down, 0xE7)]
        [InlineData(0xDF, DeviceKey.Right, 0xDF)] // Don't register button presses if wrong output check selected
        [InlineData(0xDF, DeviceKey.Left, 0xDF)]
        [InlineData(0xDF, DeviceKey.Up, 0xDF)]
        [InlineData(0xDF, DeviceKey.Down, 0xDF)]
        [InlineData(0xDF, DeviceKey.A, 0xDE)] // Normal button checks
        [InlineData(0xDF, DeviceKey.B, 0xDD)]
        [InlineData(0xDF, DeviceKey.Select, 0xDB)]
        [InlineData(0xDF, DeviceKey.Start, 0xD7)]
        [InlineData(0xEF, DeviceKey.A, 0xEF)] // Don't register button presses if wrong output check selected
        [InlineData(0xEF, DeviceKey.B, 0xEF)]
        [InlineData(0xEF, DeviceKey.Select, 0xEF)]
        [InlineData(0xEF, DeviceKey.Start, 0xEF)]
        public void TestJoypadSingleButtonPress(byte joypadMode, DeviceKey key, byte expectedValue)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x0E, 0x00, // LD C, 00
                0x3E, joypadMode, // LD A, 0xDF
                0xE2, // LD (C), A (set joypad to look at normal keys)
                0xF2, // LD A, (C) (get joypad state into A)
                0xF2, // LD A, (C) (get joypad state into A)
            });
            
            for (var ii = 0; ii < 6; ii++) device.Step(); // Step 6 times to set up tests and execute up to first joypad query

            Assert.Equal(joypadMode, device.CPU.Registers.A); // Double check joypad register is correct

            // Check key press is registered
            device.HandleKeyDown(key);
            device.Step();
            Assert.Equal(expectedValue, device.CPU.Registers.A);
            device.HandleKeyUp(key);
        }
    }
}
