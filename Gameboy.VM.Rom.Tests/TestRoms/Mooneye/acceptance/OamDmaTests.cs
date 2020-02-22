using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Mooneye.acceptance
{
    public class OamDmaTests
    {
        [Fact]
        public async Task MooneyeBasicOamDmaTest()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "general.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "oam_dma", "basic.gb"), expectedFrameBuffer, 1000 * 10, 0x486E);
        }

        [Fact]
        public async Task MooneyeBasicRegReadTests()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "general.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "oam_dma", "reg_read.gb"), expectedFrameBuffer, 1000 * 10, 0x486E);
        }

        [Fact(Skip = "TODO - Known failure - unclear how important as fails on CGB device")]
        public async Task MooneyeBasicOamDmaSourcesGs()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "general.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "oam_dma", "sources-GS.gb"), expectedFrameBuffer, 1000 * 10, 0x490E);
        }

        [Fact]
        public async Task MooneyeOamDmaRestart()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "oam_dma_restart.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "oam_dma_restart.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2E);
        }

        [Fact(Skip = "TODO - Known failure, unknown reason")]
        public async Task MooneyeOamDmaStart()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "oam_dma_start.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "oam_dma_start.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2E);
        }

        [Fact]
        public async Task MooneyeOamDmaTiming()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "oam_dma_timing.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "oam_dma_timing.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2E);
        }
    }
}
