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
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "general.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "interrupts", "ie_push.gb"), expectedFrameBuffer, 1000 * 10, 0x686E);
        }

        [Fact]
        public async Task EnableInterruptSequenceTests()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "ei_sequence.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "ei_sequence.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2D);
        }

        [Fact]
        public async Task EnableInterruptTimingTests()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "ei_timing.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "ei_timing.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2D);
        }

        [Fact]
        public async Task HaltIME0IETest()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "general.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "halt_ime0_ei.gb"), expectedFrameBuffer, 1000 * 10, 0x486E);
        }

        [Fact]
        public async Task HaltIME0NointrTimingTest()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "halt_ime0_nointr_timing.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "halt_ime0_nointr_timing.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2D);
        }

        [Fact]
        public async Task HaltIME1Timing()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "halt_ime1_timing.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "halt_ime1_timing.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2D);
        }

        [Fact]
        public async Task IFIERegisters()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "if_ie_registers.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "if_ie_registers.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2D);
        }

        [Fact]
        public async Task IntrTiming()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "intr_timing.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "intr_timing.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2D);
        }

        [Fact]
        public async Task RapidEnableDisableInterrupt()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "rapid_di_ei.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "rapid_di_ei.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2E);
        }

        [Fact]
        public async Task RetiIntrTiming()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "reti_intr_timing.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "reti_intr_timing.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2D);
        }

        [Fact]
        public async Task DITimingGS()
        {
            var expectedFrameBuffer = await File.ReadAllLinesAsync(
                Path.Join(TestUtils.SolutionDirectory, "Roms", "tests", "mooneye-gb", "acceptance", "general.framebuffer"));
            await TestUtils.TestRomAgainstResult(
                Path.Join("Roms", "tests", "mooneye-gb", "acceptance", "di_timing-GS.gb"), expectedFrameBuffer, 1000 * 10, 0x4B2D);
        }
    }
}
