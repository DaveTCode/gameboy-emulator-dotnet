using System.Linq;
using Xunit;

namespace Gameboy.VM.Tests.MMU
{
    public class MMUTests
    {
        [Theory(DisplayName = "Test all addresses can be accessed whilst ROM is mapped/unmapped")]
        [InlineData(1)]
        [InlineData(0)]
        public void TestReadAllAddresses(byte isRomMapped)
        {
            var device = TestUtils.CreateTestDevice();
            device.ControlRegisters.RomDisabledRegister = isRomMapped;

            foreach (var address in Enumerable.Range(ushort.MinValue, ushort.MaxValue + 1))
            {
                // A failed read here will throw an exception causing test failure
                device.MMU.ReadByte((ushort)address);
            }
        }

        [Fact(DisplayName = "Test all addresses are mapped for writes through MMU")]
        public void TestWriteAllAddresses()
        {
            var device = TestUtils.CreateTestDevice();
            device.ControlRegisters.RomDisabledRegister = 0x1;

            foreach (var address in Enumerable.Range(ushort.MinValue, ushort.MaxValue + 1))
            {
                // A failed write here will throw an exception causing test failure, this doesn't include writes to unused addresses which just act as NOOP
                device.MMU.WriteByte((ushort)address, 0x1);
            }
        }

        [Fact]
        public void TestAllHRAMAddressesWriteable()
        {
            var device = TestUtils.CreateTestDevice();

            foreach (var address in Enumerable.Range(0xFF80, 0x7F))
            {
                device.MMU.WriteByte((ushort)address, 0x5);
                Assert.Equal(0x5, device.MMU.ReadByte((ushort)address));
            }
        }

        [Fact]
        public void TestAllWRAMAddressesWriteable()
        {
            var device = TestUtils.CreateTestDevice();

            foreach (var address in Enumerable.Range(0xC000, 0x2000))
            {
                device.MMU.WriteByte((ushort)address, 0x5);
                Assert.Equal(0x5, device.MMU.ReadByte((ushort)address));
            }
        }

        [Fact]
        public void TestAllWRAMMirrorAddressesWriteable()
        {
            var device = TestUtils.CreateTestDevice();

            foreach (var address in Enumerable.Range(0xE000, 0x1E00))
            {
                device.MMU.WriteByte((ushort)address, 0x5);
                Assert.Equal(0x5, device.MMU.ReadByte((ushort)(address)));
                Assert.Equal(0x5, device.MMU.ReadByte((ushort)(address - 0x2000))); // Should get the same value in the non-mirrored portion of WRAM
            }
        }

        [Fact]
        public void TestCGBRamBank()
        {
            var device = TestUtils.CreateTestDevice(mode: DeviceType.CGB);

            device.MMU.WriteByte(0xC000, 0x1); // Bank 0 value
            device.MMU.WriteByte(0xE001, 0xA); // Bank 0 value in mirror ram

            for (byte bank = 1; bank < 8; bank++)
            {
                device.MMU.WriteByte(0xFF70, bank);
                device.MMU.WriteByte(0xD000, bank);
                device.MMU.WriteByte(0xF001, (byte)(bank + 1));
            }

            for (byte bank = 1; bank < 8; bank++)
            {
                device.MMU.WriteByte(0xFF70, bank);
                Assert.Equal(0x1, device.MMU.ReadByte(0xC000)); // Bank 0 should never change
                Assert.Equal(0x1, device.MMU.ReadByte(0xE000)); // Bank 0 should never change in mirror either
                Assert.Equal(0xA, device.MMU.ReadByte(0xC001)); // Bank 0 should never change
                Assert.Equal(0xA, device.MMU.ReadByte(0xE001)); // Bank 0 should never change in mirror either
                Assert.Equal(bank, device.MMU.ReadByte(0xD000)); // Banked value should be correct
                Assert.Equal(bank, device.MMU.ReadByte(0xF000)); // Banked value should be correct in mirror
                Assert.Equal(bank + 1, device.MMU.ReadByte(0xD001)); // Banked value should be correct
                Assert.Equal(bank + 1, device.MMU.ReadByte(0xF001)); // Banked value should be correct in mirror
            }
        }
    }
}
