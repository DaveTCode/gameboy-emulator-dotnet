using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Mooneye.acceptance
{
    public class InterruptTests
    {
        [Fact(Skip = "Odd behavior test and fails on bgb in round 3 so not terribly important")]
        public async Task MooneyeIEPushTest()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "interrupts", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "interrupts", "ie_push.gb"), expectedFrameBuffer, 1000 * 10, 0x686E);
        }
    }
}
