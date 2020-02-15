using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Blargg
{
    public class BugTests
    {
        [Fact(DisplayName = "Test HALT bug behaviour")]
        public async Task TestHaltBugBehavior()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "blargg", "halt_bug_framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "blargg", "halt_bug.gb"), expectedFrameBuffer, 1000 * 120, 0xC818);
        }

        [Fact(DisplayName = "Test OAM bug behavior", Skip = "OAM bug not implemented at the moment")]
        public async Task TestOAMBugBehavior()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "blargg", "oam_bug", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "blargg", "oam_bug", "oam_bug.gb"), expectedFrameBuffer, 1000 * 120, 0x2BD2);
        }
    }
}
