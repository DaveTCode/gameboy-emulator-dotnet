using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Mooneye.acceptance
{
    public class MooneyeBitsTests
    {
        [Fact]
        public async Task MooneyeMemOamTest()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "bits", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "bits", "mem_oam.gb"), expectedFrameBuffer, 1000 * 10, 0x486E);
        }

        [Fact]
        public async Task MooneyeRegFTests()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "bits", "reg_f_framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "bits", "reg_f.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2E);
        }

        [Fact(Skip = "TODO - Failing because HDMA is broken")]
        public async Task MooneyeUnusedHwioGsTests()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "bits", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "bits", "unused_hwio-GS.gb"), expectedFrameBuffer, 1000 * 10, 0x486E);
        }
    }
}
