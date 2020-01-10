using System.IO;
using CommandLine;
using Gameboy.VM;
using Gameboy.VM.Cartridge;

namespace Gameboy.Emulator.SDL
{
    public class CommandLineOptions
    {
        [Option('f', "romFilePath", Required = true, HelpText = "The full file path to a binary rom dump")]
        public string RomFilePath { get; }

        [Option("skipBootRom", Default = false, HelpText = "Set to true if you want to skip the boot rom check (e.g. if the rom is not a valid gameboy cartridge)")]
        public bool SkipBootRom { get; }

        [Option('s', "framesPerSecondCap", Default = 60, HelpText = "The number of frames per second, 60 is typical")]
        public int FramesPerSecond { get; }

        [Option('p', "pixelSize", Default = 2, HelpText = "The size of the square that we use to represent a single pixel")]
        public int PixelSize { get; }

        public CommandLineOptions(string romFilePath, bool skipBootRom, int framesPerSecond, int pixelSize)
        {
            RomFilePath = romFilePath;
            SkipBootRom = skipBootRom;
            FramesPerSecond = framesPerSecond;
            PixelSize = pixelSize;
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
            var device = new Device(CartridgeFactory.CreateCartridge(File.ReadAllBytes(options.RomFilePath)));

            if (options.SkipBootRom) device.SkipBootRom();

            using var sdlApplication = new SDL2Application(device, options.PixelSize);
            sdlApplication.ExecuteProgram(options.FramesPerSecond);

            return 0;
        }
    }
}
