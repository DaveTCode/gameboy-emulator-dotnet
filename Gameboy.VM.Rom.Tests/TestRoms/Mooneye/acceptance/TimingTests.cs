using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Mooneye.acceptance
{
    /// <summary>
    /// Contains mooneye tests which relate to individual instruction timing
    /// </summary>
    public class TimingTests
    {
        [Theory]
        [InlineData("add_sp_e_timing.gb", 0x4B2D, "add_sp_e_timing.framebuffer")]
        [InlineData("call_timing.gb", 0x486D, "general.framebuffer")]
        [InlineData("call_timing2.gb", 0x4B2D, "call_timing2.framebuffer")]
        [InlineData("call_cc_timing.gb", 0x486D, "general.framebuffer")]
        [InlineData("call_cc_timing2.gb", 0x4B2D, "call_cc_timing2.framebuffer")]
        //[InlineData("di_timing-GS.gb", 0x486D, "general.framebuffer")] - TODO failing
        [InlineData("div_timing.gb", 0x4B2D, "div_timing.framebuffer")]
        [InlineData("jp_cc_timing.gb", 0x486D, "general.framebuffer")]
        [InlineData("jp_timing.gb", 0x486D, "general.framebuffer")]
        [InlineData("ld_hl_sp_e_timing.gb", 0x4B2D, "ld_hl_sp_e_timing.framebuffer")]
        [InlineData("pop_timing.gb", 0x4B2D, "pop_timing.framebuffer")]
        [InlineData("push_timing.gb", 0x4B2D, "push_timing.framebuffer")]
        [InlineData("ret_cc_timing.gb", 0x486D, "general.framebuffer")]
        [InlineData("ret_timing.gb", 0x486D, "general.framebuffer")]
        [InlineData("reti_timing.gb", 0x486D, "general.framebuffer")]
        [InlineData("rst_timing.gb", 0x4B2D, "rst_timing.framebuffer")]
        public async Task TestInstructionTiming(string romFileName, ushort finalAddress, string framebufferFileName)
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", framebufferFileName));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", romFileName), expectedFrameBuffer, 1000 * 10, finalAddress);
        }
    }
}
