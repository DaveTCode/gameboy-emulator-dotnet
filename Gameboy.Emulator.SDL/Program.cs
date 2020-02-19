using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CommandLine;
using Gameboy.VM;
using Gameboy.VM.Cartridge;

namespace Gameboy.Emulator.SDL
{
    public class CommandLineOptions
    {
        [Value(0, HelpText = "The full file path to a binary rom dump", MetaName = "RomFilePath")]
        public string RomFilePath { get; }

        [Option("bootRomFilePath", Required = false, HelpText = "Set to the file path of a boot rom, this should match the device mode selected")]
        public string BootRomFilePath { get; }

        [Option('s', "framesPerSecondCap", Default = 60, HelpText = "The number of frames per second, 60 is typical")]
        public int FramesPerSecond { get; }

        [Option('p', "pixelSize", Default = 2, HelpText = "The size of the square that we use to represent a single pixel")]
        public int PixelSize { get; }

        [Option("mode", Default = DeviceType.DMG, HelpText = "Use to select the device mode from CGB/DMG")]
        public DeviceType Mode { get; }

        public CommandLineOptions(string romFilePath, string bootRomFilePath, int framesPerSecond, int pixelSize, DeviceType mode)
        {
            RomFilePath = romFilePath;
            BootRomFilePath = bootRomFilePath;
            FramesPerSecond = framesPerSecond;
            PixelSize = pixelSize;
            Mode = mode;
        }
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult(RunProgram, _ => -1);
        }

        private static int RunProgram(CommandLineOptions options)
        {
            byte[] romBytes;
            if (options.RomFilePath.EndsWith(".zip"))
            {
                using var file = File.OpenRead(options.RomFilePath);
                using var zip = new ZipArchive(file, ZipArchiveMode.Read);

                var romFile = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".gb") || e.Name.EndsWith(".gbc"));
                if (romFile == null)
                {
                    Console.WriteLine("More than one game in archive {0}, can't load it", options.RomFilePath);
                    return -1;
                }

                using var stream = romFile.Open();
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                romBytes = memoryStream.ToArray();
            }
            else
            {
                romBytes = File.ReadAllBytes(options.RomFilePath);
            }


            using var sdlApplication = new SDL2Application(
                CartridgeFactory.CreateCartridge(romBytes),
                options.Mode,
                options.PixelSize,
                (options.BootRomFilePath == null) ? null : File.ReadAllBytes(options.BootRomFilePath),
                options.FramesPerSecond);
            sdlApplication.ExecuteProgram();

            return 0;
        }
    }
}
