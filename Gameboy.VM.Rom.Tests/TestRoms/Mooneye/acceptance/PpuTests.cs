using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Mooneye.acceptance
{
    public class PpuTests
    {
        // TODO - Fix broken & commented out tests
        [Theory]
        //[InlineData("hblank_ly_scx_timing-GS.gb", 10 * 1000, 0x48F3, "framebuffer")]
        //[InlineData("intr_1_2_timing-GS.gb", 10 * 1000, 0x4B2E, "framebuffer")]
        [InlineData("intr_2_0_timing.gb", 10 * 1000, 0x4B2E, "intr_2_0_timing.framebuffer")]
        //[InlineData("intr_2_mode0_timing.gb", 10 * 1000, 0x4B2E, "intr_2_mode0_timing.framebuffer")]
        //[InlineData("intr_2_mode0_timing_sprites.gb", 10 * 1000, 0x4B2E, "framebuffer")]
        //[InlineData("intr_2_mode3_timing.gb", 10 * 1000, 0x4B2E, "intr_2_mode3_timing.framebuffer")]
        //[InlineData("intr_2_oam_ok_timing.gb", 10 * 1000, 0x4B2E, "intr_2_oam_ok_timing.framebuffer")]
        //[InlineData("lcdon_timing-GS.gb", 10 * 1000, 0x4CB6, "framebuffer")]
        //[InlineData("lcdon_write_timing-GS.gb", 10 * 1000, 0x496B, "framebuffer")]
        //[InlineData("stat_irq_blocking.gb", 10 * 1000, 0x48F3, "framebuffer")]
        //[InlineData("stat_lyc_onoff.gb", 10 * 1000, 0x486E, "framebuffer")]
        //[InlineData("vblank_stat_intr-GS.gb", 10 * 1000, 0x4B2E, "framebuffer")]
        public async Task MooneyePpuTests(string romName, int timeout, ushort finalAddress, string framebufferFile)
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "ppu", framebufferFile));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "ppu", romName), expectedFrameBuffer, timeout, finalAddress);
        }
    }
}
