using System.Diagnostics;
using System.IO;
using CommandLine;
using Gameboy.VM;
using Gameboy.VM.Cartridge;


namespace Gameboy.Headless.Console
{
    public class CommandLineOptions
    {
        [Option('f', "romFilePath", Required = true, HelpText = "The full file path to a binary rom dump")]
        public string RomFilePath { get; }

        [Option("skipBootRom", Default = false, HelpText = "Set to true if you want to skip the boot rom check (e.g. if the rom is not a valid gameboy cartridge)")]
        public bool SkipBootRom { get; }

        public CommandLineOptions(string romFilePath, bool skipBootRom)
        {
            RomFilePath = romFilePath;
            SkipBootRom = skipBootRom;
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

            for (var i = 0; i < 10000; i++)
            {
                device.Step();
            }

            return 0;
        }
    }
}
