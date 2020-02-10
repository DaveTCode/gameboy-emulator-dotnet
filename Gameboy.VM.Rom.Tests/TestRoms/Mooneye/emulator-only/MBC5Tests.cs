using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Mooneye
{
    public class MBC5Tests
    {
        [Theory]
        [InlineData("rom_1Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_2Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_4Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_8Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_16Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_32Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_64Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_512Kb.gb", 10 * 1000, 0x486E)]
        public async Task Mbc5MooneyeTest(string romName, int timeout, ushort finalAddress)
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "emulator-only", "mbc5", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "emulator-only", "mbc5", romName), expectedFrameBuffer, timeout, finalAddress);
        }
    }
}
