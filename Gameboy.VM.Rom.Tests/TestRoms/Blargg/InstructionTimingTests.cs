using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Blargg
{
    public class InstructionTimingTests
    {
        [Fact(DisplayName = "Test each instruction takes the correct number of t-cycles")]
        public async Task TestCpuInstructionTiming()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "blargg", "instr_timing", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "blargg", "instr_timing", "instr_timing.gb"), expectedFrameBuffer, 1000 * 120, 0xC8B0);
        }
    }
}
