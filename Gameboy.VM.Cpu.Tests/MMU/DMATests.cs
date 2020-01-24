using Gameboy.VM.LCD;
using Xunit;

namespace Gameboy.VM.Tests.MMU
{
    public class DMATests
    {
        [Fact]
        public void TestOAMDMA()
        {
            var device = TestUtils.CreateTestDevice(new byte[0x1000]);
            device.LCDRegisters.LCDControlRegister = 0x0; // Turn LCD off
            device.LCDRegisters.StatMode = StatMode.VBlankPeriod; // Need to be in vblank for OAM reads to work

            for (var ii = 0; ii < 2; ii++) device.Step();

            // Set up WRAM
            for (byte ii = 0; ii < 160; ii++)
            {
                device.MMU.WriteByte((ushort)(0xC000 + ii), (byte) (ii+2));
            }

            // Execute OAM DMA procedure
            device.MMU.WriteByte(0xFF46, 0xC0);

            // Check that first step doesn't do anything (requesting DMA by LD into FF46)
            device.Step();
            Assert.Equal(0x0, device.MMU.ReadByte(0xFE00));

            var cycles = 0;
            while (device.MMU.ReadByte(0xFE9F) != 161 && cycles <= 160 * 4)
            {
                cycles += device.Step();
            }

            // DMA should take exactly 160 * 4 + 4 t-cycles
            Assert.Equal(160 * 4 + 4, cycles);

            // And the bytes should be copied across correctly
            for (byte ii = 0; ii < 160; ii++)
            {
                Assert.Equal(ii + 2, device.MMU.ReadByte((ushort) (0xFE00 + ii)));
            }
        }

        [Fact]
        public void TestRestartOAMDMA()
        {
            var device = TestUtils.CreateTestDevice(new byte[0x1000]);
            device.LCDRegisters.LCDControlRegister = 0x0; // Turn LCD off
            device.LCDRegisters.StatMode = StatMode.VBlankPeriod; // Need to be in vblank for OAM reads to work

            for (var ii = 0; ii < 2; ii++) device.Step();

            // Set up WRAM
            for (byte ii = 0; ii < 160; ii++)
            {
                device.MMU.WriteByte((ushort)(0xC000 + ii), (byte)(ii + 2));
            }

            // Execute OAM DMA procedure
            device.MMU.WriteByte(0xFF46, 0xC0);

            // Check that first step doesn't do anything (requesting DMA by LD into FF46)
            device.Step();
            Assert.Equal(0x0, device.MMU.ReadByte(0xFE00));

            // Perform 100 steps
            for (var ii = 0; ii < 100; ii++) device.Step();
            
            // 99 values should have been written so far (1 m-cycle to set up)
            for (var ii = 0; ii < 99; ii++)
            {
                // Need to go direct to OAM RAM here because DMA ongoing
                Assert.Equal(ii + 2, device.LCDDriver.GetOAMByte((ushort)(0xFE00 + ii)));
            }
            // Check that we have still not written byte 99
            Assert.Equal(0x0, device.LCDDriver.GetOAMByte(0xFE63));

            // Execute OAM DMA procedure again, restarting the current one
            device.MMU.WriteByte(0xFF46, 0xC0);
            device.Step();

            // Check that we HAVE now written byte 99 since the restart only stops the following write
            Assert.Equal(0x65, device.LCDDriver.GetOAMByte(0xFE63));
            device.Step();

            // But now we should have restarted DMA and the next byte should still be 0
            Assert.Equal(0x00, device.LCDDriver.GetOAMByte(0xFE64));
        }
    }
}
