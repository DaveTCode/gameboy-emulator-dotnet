using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Mooneye.acceptance
{
    public class TimerTests
    {
        // TODO - Fix broken & commented out tests
        [Theory]
        [InlineData("div_write.gb", 10 * 1000, 0x486E, "general.framebuffer")]
        //[InlineData("rapid_toggle.gb", 10 * 1000, 0x4B2E, "rapid_toggle_framebuffer")]
        [InlineData("tim00.gb", 10 * 1000, 0x4B2E, "tim00.framebuffer")]
        [InlineData("tim01.gb", 10 * 1000, 0x4B2E, "tim01.framebuffer")]
        [InlineData("tim10.gb", 10 * 1000, 0x4B2E, "tim10.framebuffer")]
        [InlineData("tim11.gb", 10 * 1000, 0x4B2E, "tim11.framebuffer")]
        // TODO - These div trigger tests miss the trigger by 4 cycles, likely because 4 cycles have happened before the write completes to the div register - decided not to hack that in
        //[InlineData("tim00_div_trigger.gb", 10 * 1000, 0x4B2E, "tim00_framebuffer")]
        //[InlineData("tim01_div_trigger.gb", 10 * 1000, 0x4B2E, "tim01_framebuffer")]
        //[InlineData("tim10_div_trigger.gb", 10 * 1000, 0x4B2E, "tim10_framebuffer")]
        //[InlineData("tim11_div_trigger.gb", 10 * 1000, 0x4B2E, "tim11_framebuffer")]
        [InlineData("tima_reload.gb", 10 * 1000, 0x4B2E, "tima_reload.framebuffer")]
        // TODO - The following behaviour can't really be replicated without a cycle accurate CPU so tests need to be left commented out
        //[InlineData("tima_write_reloading.gb", 10 * 1000, 0x4B2E, "tima_write_reloading_framebuffer")]
        //[InlineData("tma_write_reloading.gb", 10 * 1000, 0x4B2E, "tma_write_reloading_framebuffer")]
        public async Task MooneyeTimerTests(string romName, int timeout, ushort finalAddress, string framebufferFile)
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "timer", framebufferFile));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "timer", romName), expectedFrameBuffer, timeout, finalAddress);
        }
    }
}
