using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Blargg
{
    public class MemoryTimingTests
    {
        // Test skipped because it's known to fail until we implement cycle accuracy
        [Fact(DisplayName = "Test memory read/writes happen on the right m-cycle during an instruction", Skip = "true")]
        public async Task TestMemoryAccessTiming()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "blargg", "mem_timing", "framebuffer")); // TODO - Don't know what the actual framebuffer would look like yet as this fails
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "blargg", "mem_timing", "mem_timing.gb"), expectedFrameBuffer, 1000 * 120, 0x06F1);
        }

        // Test skipped because it's known to fail until we implement cycle accuracy
        [Fact(DisplayName = "Test memory read/writes happen on the right m-cycle during an instruction (2)", Skip = "true")]
        public async Task TestMemoryAccessTiming2()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "blargg", "mem_timing-2", "framebuffer")); // TODO - Don't know what the actual framebuffer would look like yet as this fails
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "blargg", "mem_timing-2", "mem_timing-2.gb"), expectedFrameBuffer, 1000 * 120, 0x2BDD);
        }
    }
}
