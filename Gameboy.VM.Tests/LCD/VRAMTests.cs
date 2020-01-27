using Xunit;

namespace Gameboy.VM.Tests.LCD
{
    public class VRAMTests
    {
        [Fact]
        public void TestCGBVRAMBanking()
        {
            var device = TestUtils.CreateTestDevice(mode: DeviceType.CGB);
            // VRAM Bank 0
            device.MMU.WriteByte(0x8000, 0x1);
            // Change VRAM Bank
            device.MMU.WriteByte(0xFF4F, 0xFF); // Only bottom bit interesting
            // VRAM Bank 1
            device.MMU.WriteByte(0x8000, 0x2);

            // Check the vram bank number
            Assert.Equal(0xFF, device.MMU.ReadByte(0xFF4F));

            device.MMU.WriteByte(0xFF4F, 0xFE);
            Assert.Equal(0x1, device.MMU.ReadByte(0x8000));
            device.MMU.WriteByte(0xFF4F, 0x1);
            Assert.Equal(0x2, device.MMU.ReadByte(0x8000));
        }
    }
}
