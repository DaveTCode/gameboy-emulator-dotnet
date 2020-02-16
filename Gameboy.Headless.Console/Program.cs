using System.IO;
using CommandLine;
using Gameboy.VM;
using Gameboy.VM.Cartridge;
using Gameboy.VM.LCD;
using Gameboy.VM.Sound;


namespace Gameboy.Headless.Console
{
    public class CommandLineOptions
    {
        [Option('f', "romFilePath", Required = true, HelpText = "The full file path to a binary rom dump")]
        public string RomFilePath { get; }

        [Option("skipBootRom", Default = false, HelpText = "Set to true if you want to skip the boot rom check (e.g. if the rom is not a valid gameboy cartridge)")]
        public bool SkipBootRom { get; }

        [Option("mode", Default = DeviceType.DMG, HelpText = "Use to select the device mode from CGB/DMG")]
        public DeviceType Mode { get; }

        public CommandLineOptions(string romFilePath, bool skipBootRom, DeviceType mode)
        {
            RomFilePath = romFilePath;
            SkipBootRom = skipBootRom;
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
            var device = new Device(CartridgeFactory.CreateCartridge(File.ReadAllBytes(options.RomFilePath)), options.Mode, new NullRenderer(options.Mode), new NullSoundOutput(), null);

            if (options.SkipBootRom) device.SkipBootRom();

            for (var i = 0; i < 10000; i++)
            {
                device.Step();
            }

            return 0;
        }
    }
}
