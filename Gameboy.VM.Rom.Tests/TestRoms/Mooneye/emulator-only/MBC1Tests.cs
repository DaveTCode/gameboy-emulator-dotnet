using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Mooneye
{
    public class MBC1Tests
    {
        [Theory]
        [InlineData("bits_bank1.gb", 10 * 1000, 0x486E)]
        [InlineData("bits_bank2.gb", 10 * 1000, 0x486E)]
        [InlineData("bits_mode.gb", 10 * 1000, 0x486E)]
        [InlineData("bits_ramg.gb", 10 * 1000, 0x486E)]
        //[InlineData("multicart_rom_8Mb.gb", 10 * 1000, 0x486E)] - Known unimplemented multicart
        [InlineData("ram_64Kb.gb", 10 * 1000, 0x486E)]
        [InlineData("ram_256Kb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_1Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_2Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_4Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_8Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_16Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_512Kb.gb", 10 * 1000, 0x486E)]
        public async Task Mbc1MooneyeTest(string romName, int timeout, ushort finalAddress)
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "emulator-only", "mbc1", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "emulator-only", "mbc1", romName), expectedFrameBuffer, timeout, finalAddress);
        }
    }
}
