using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Blargg
{
    public class InterruptTiming
    {
        [Fact(DisplayName = "Test OAM bug behavior", Skip = "TODO - Double speed not yet implemented")]
        public async Task TestOAMBugBehavior()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "blargg", "interrupt_time", "framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "blargg", "interrupt_time", "interrupt_time.gb"), expectedFrameBuffer, 1000 * 120, 0xC9C9);
        }
    }
}
