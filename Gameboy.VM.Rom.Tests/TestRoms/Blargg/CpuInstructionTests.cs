using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Blargg
{
    public class CpuInstructionTests
    {
        [Fact(DisplayName = "Tests all of the CPU instructions thoroughly")]
        public async Task TestCpuInstructions()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "blargg", "cpu_instrs", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "blargg", "cpu_instrs", "cpu_instrs.gb"), expectedFrameBuffer, 1000 * 120, 0x6F1);
        }

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
