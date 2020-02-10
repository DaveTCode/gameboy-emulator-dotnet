using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Mooneye
{
    public class MBC2Tests
    {
        [Theory]
        [InlineData("bits_ramg.gb", 10 * 1000, 0x486E)]
        [InlineData("bits_romb.gb", 10 * 1000, 0x486E)]
        [InlineData("bits_unused.gb", 10 * 1000, 0x486E)]
        [InlineData("ram.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_1Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_2Mb.gb", 10 * 1000, 0x486E)]
        [InlineData("rom_512Kb.gb", 10 * 1000, 0x486E)]
        public async Task Mbc2MooneyeTest(string romName, int timeout, ushort finalAddress)
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "emulator-only", "mbc2", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "emulator-only", "mbc2", romName), expectedFrameBuffer, timeout, finalAddress);
        }
    }
}
