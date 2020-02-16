using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Gameboy.VM.Cartridge;
using Gameboy.VM.LCD;
using Gameboy.VM.Sound;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Gameboy.VM.Rom.Tests
{
    internal static class TestUtils
    {
        internal static string SolutionDirectory = Directory.GetParent(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Parent?.Parent?.Parent?.Parent?.ToString();
        internal static string ImageDirectory = Path.Join(SolutionDirectory, "Roms", "output_images");

        /// <summary>
        /// Given a specific ROM stored within the solution directory, this
        /// function will execute the ROM until either the timeout or the
        /// final opcode is reached, it will then compare the resulting
        /// framebuffer with the expected buffer and fail the test if they
        /// are not equal.
        /// </summary>
        /// <param name="relativePathToRomFromSolution"></param>
        /// <param name="expectedFramebuffer">
        /// The framebuffer with one byte per line encoded as b\ng\nr\na
        /// </param>
        /// <param name="finalOpcode"></param>
        /// <param name="timeoutMs"></param>
        internal static async Task TestRomAgainstResult(string relativePathToRomFromSolution, string[] expectedFramebuffer, int timeoutMs, ushort? finalOpcode = null, DeviceType deviceType = DeviceType.DMG)
        {
            var romFile = Path.Join(SolutionDirectory, relativePathToRomFromSolution);
            var imageFile = Path.Join(new DirectoryInfo(romFile).Parent?.FullName, Path.GetFileNameWithoutExtension(romFile) + ".png");
            Directory.CreateDirectory(ImageDirectory);
            var cartridge = CartridgeFactory.CreateCartridge(await File.ReadAllBytesAsync(Path.Join(SolutionDirectory, relativePathToRomFromSolution)));
            var device = new Device(cartridge, deviceType, new NullRenderer(deviceType), new NullSoundOutput());
            device.SkipBootRom();

            var sw = new Stopwatch();
            sw.Start();

            // Timeout or when we hit the end of the program
            while (device.CPU.Registers.ProgramCounter != finalOpcode && sw.ElapsedMilliseconds < timeoutMs)
            {
                device.Step();
            }

            // Timing out is only an error if we passed a final opcode to check for
            if (sw.ElapsedMilliseconds > timeoutMs && finalOpcode.HasValue)
            {
                Assert.False(true, $"Test timed out with device state {device}");
            }

            var (_, _, _, _, _, frameBuffer) = device.DumpLcdDebugInformation();

            Image.LoadPixelData<Bgra32>(frameBuffer, Device.ScreenWidth, Device.ScreenHeight).Save(imageFile, new PngEncoder());

            Assert.Equal(expectedFramebuffer, frameBuffer.Select(x => x.ToString("D")));
        }
    }
}
