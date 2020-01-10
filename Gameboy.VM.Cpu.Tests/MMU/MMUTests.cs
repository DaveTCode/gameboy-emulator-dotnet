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
    }
}
