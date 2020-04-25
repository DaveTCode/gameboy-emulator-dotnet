using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.MattCurrie
{
    public class GraphicAcidTests
    {
        [Fact(DisplayName = "Run Acid test for DMG and check the result against expected image")]
        public async Task DmgAcidTest()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mattcurrie", "dmg-acid2.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mattcurrie", "dmg-acid2.gb"), expectedFrameBuffer, 1000 * 2, null);
        }
    }
}
