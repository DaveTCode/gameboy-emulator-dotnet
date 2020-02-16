﻿using System.IO;
using CommandLine;
using Gameboy.VM;
using Gameboy.VM.Cartridge;

namespace Gameboy.Emulator.SDL
{
    public class CommandLineOptions
    {
        [Value(0, HelpText = "The full file path to a binary rom dump", MetaName = "RomFilePath")]
        public string RomFilePath { get; }

        [Option("skipBootRom", Default = false, HelpText = "Set to true if you want to skip the boot rom check (e.g. if the rom is not a valid gameboy cartridge)")]
        public bool SkipBootRom { get; }

        [Option('s', "framesPerSecondCap", Default = 60, HelpText = "The number of frames per second, 60 is typical")]
        public int FramesPerSecond { get; }

        [Option('p', "pixelSize", Default = 2, HelpText = "The size of the square that we use to represent a single pixel")]
        public int PixelSize { get; }

        [Option("mode", Default = DeviceType.DMG, HelpText = "Use to select the device mode from CGB/DMG")]
        public DeviceType Mode { get; }

        public CommandLineOptions(string romFilePath, bool skipBootRom, int framesPerSecond, int pixelSize, DeviceType mode)
        {
            RomFilePath = romFilePath;
            SkipBootRom = skipBootRom;
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
            using var sdlApplication = new SDL2Application(
                CartridgeFactory.CreateCartridge(File.ReadAllBytes(options.RomFilePath)),
                options.Mode,
                options.PixelSize,
                options.SkipBootRom,
                options.FramesPerSecond);
            sdlApplication.ExecuteProgram();

            return 0;
        }
    }
}
