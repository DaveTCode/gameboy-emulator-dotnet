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

namespace Gameboy.VM.Rom.Tests.TestRoms.Blargg
{
    public class CpuInstructions
    {
        [Fact(DisplayName = "Tests all of the CPU instructions thoroughly")]
        public async Task TestCpuInstructionsRom()
        {
            var solutionDirectory = Directory.GetParent(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Parent?.Parent?.Parent?.Parent?.ToString();
            var imageOutputDirectory = Path.Join(solutionDirectory, "Roms", "output_images");
            Directory.CreateDirectory(imageOutputDirectory);
            var cartridge = CartridgeFactory.CreateCartridge(await File.ReadAllBytesAsync(Path.Join(solutionDirectory, "Roms", "tests", "blargg", "cpu_instrs", "cpu_instrs.gb")));
            var device = new Device(cartridge, DeviceType.DMG, new NullRenderer(), new NullSoundOutput());

            var sw = new Stopwatch();
            sw.Start();

            // Timeout after 60s or when we hit the end of the program
            while (device.CPU.Registers.ProgramCounter != 0x6F1)
            {
                device.Step();

                if (sw.ElapsedMilliseconds > 1000 * 120)
                {
                    Assert.False(true, "Test timed out");
                }
            }

            var (_, _, _, _, _, frameBuffer) = device.DumpLcdDebugInformation();

            using (var image = new Image<Rgba32>(Device.ScreenWidth, Device.ScreenHeight))
            {
                for (var x = 0; x < Device.ScreenWidth; x++)
                {
                    for (var y = 0; y < Device.ScreenHeight; y++)
                    {
                        var pixel = (x + Device.ScreenWidth * y) * 4;
                        var a = frameBuffer[pixel + 3];
                        var r = frameBuffer[pixel + 2];
                        var g = frameBuffer[pixel + 1];
                        var b = frameBuffer[pixel + 0];

                        image[x, y] = new Rgba32(r, g, b, a);
                    }
                }

                image.Save(Path.Join(imageOutputDirectory, "cpu_instrs.png"), new PngEncoder());
            }

            var expectedFrameBuffer = await File.ReadAllLinesAsync(Path.Join(solutionDirectory, "Roms", "tests", "blargg", "cpu_instrs", "framebuffer"));
            Assert.Equal(expectedFrameBuffer, frameBuffer.Select(x => x.ToString("D")));
        }
    }
}
