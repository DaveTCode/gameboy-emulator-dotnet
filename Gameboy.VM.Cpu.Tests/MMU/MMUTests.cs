using System.Linq;
using Gameboy.VM.Interrupts;
using Gameboy.VM.LCD;
using Gameboy.VM.Sound;
using Xunit;

namespace Gameboy.VM.Cpu.Tests.MMU
{
    public class MMUTests
    {
        [Theory(DisplayName = "Test all addresses can be accessed whilst ROM is mapped/unmapped")]
        [InlineData(1)]
        [InlineData(0)]
        public void TestReadAllAddresses(byte isRomMapped)
        {
            var cr = new ControlRegisters { RomDisabledRegister = isRomMapped };
            var mmu = new VM.MMU(Device.DmgRomContents, cr, new SoundRegisters(), new LCDRegisters(), new InterruptRegisters(), TestUtils.CreateMBC0ROMCartridge());

            foreach (var address in Enumerable.Range(ushort.MinValue, ushort.MaxValue + 1))
            {
                // A failed read here will throw an exception causing test failure
                mmu.ReadByte((ushort)address);
            }
        }

        [Fact(DisplayName = "Test all addresses are mapped for writes through MMU")]
        public void TestWriteAllAddresses()
        {
            var cr = new ControlRegisters { RomDisabledRegister = 0x1 };
            var mmu = new VM.MMU(Device.DmgRomContents, cr, new SoundRegisters(), new LCDRegisters(), new InterruptRegisters(), TestUtils.CreateMBC0ROMCartridge());

            foreach (var address in Enumerable.Range(ushort.MinValue, ushort.MaxValue + 1))
            {
                // A failed write here will throw an exception causing test failure, this doesn't include writes to unused addresses which just act as NOOP
                mmu.WriteByte((ushort)address, 0x1);
            }
        }
    }
}
