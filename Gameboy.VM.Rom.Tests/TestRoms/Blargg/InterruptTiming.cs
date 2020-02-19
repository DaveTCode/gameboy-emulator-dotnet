using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gameboy.VM.Rom.Tests.TestRoms.Blargg
{
    public class InterruptTiming
    {
        [Fact(DisplayName = "Test Interrupt Timing")]
        public async Task TestInterruptTiming()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "blargg", "interrupt_time", "framebuffer"));
            // Don't seem to be able to use the final opcode here as the video hasn't yet caught up at that point
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "blargg", "interrupt_time", "interrupt_time.gb"), expectedFrameBuffer, 1000 * 10, deviceType: DeviceType.CGB);
        }
    }
}
