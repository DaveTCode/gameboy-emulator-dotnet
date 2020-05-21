using System;
using System.Collections.Generic;
using Gameboy.VM.LCD;
using Gameboy.VM.Interrupts;
using Gameboy.VM.Sound;
using System.Runtime.CompilerServices;
using Gameboy.VM.Cartridge;
using Gameboy.VM.Timers;
using Serilog;
using Serilog.Core;
using Gameboy.VM.Joypad;

[assembly: InternalsVisibleTo("Gameboy.VM.Tests")]
[assembly: InternalsVisibleTo("Gameboy.VM.Rom.Tests")]
namespace Gameboy.VM
{
    /// <summary>
    /// A device here refers to the entire of an emulated gameboy and provides
    /// the public interface into this library.
    /// </summary>
    public class Device
    {
        public const int ScreenWidth = 160;
        public const int ScreenHeight = 144;
        public const int CyclesPerSecondHz = 4_194_304; // 4.194304 MHz

        public long TCycles;

        public readonly DeviceType Mode;
        public readonly DeviceType Type;
        internal readonly IRenderer Renderer;
        internal readonly ISoundOutput SoundOutput;
        internal readonly MMU MMU;
        internal readonly CPU.CPU CPU;
        private readonly IEnumerator<int> _cpuGenerator;
        internal readonly ControlRegisters ControlRegisters;
        internal readonly APU APU;
        internal readonly LCDRegisters LCDRegisters;
        internal readonly InterruptRegisters InterruptRegisters;
        internal readonly Cartridge.Cartridge Cartridge;
        internal readonly LCDDriver LCDDriver;
        internal readonly Timer Timer;
        internal readonly DMAController DMAController;
        internal readonly JoypadHandler JoypadHandler;

        internal bool DoubleSpeed = false;
        private bool _stepPPUOnNextDoubleSpeedCycle = true;

        internal Logger Log;

        public void SetDebugMode()
        {
            Log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("log-debug.txt", outputTemplate: "{Message}{NewLine}", rollOnFileSizeLimit: true, buffered: true)
                .CreateLogger();
            Log.Information("Debug mode ON");
        }

        public Device(Cartridge.Cartridge cartridge, DeviceType type, IRenderer renderer, ISoundOutput soundOutput, byte[] bootRom)
        {
            Log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .CreateLogger();

            // A bit of double checking that we're loading a valid cartridge for the device type
            if (cartridge.CGBSupportCode == CGBSupportCode.CGBExclusive && type == DeviceType.DMG)
            {
                Log.Error("Cartridge can't be loaded because it's CGB only and this device was created as DMG");
                throw new ApplicationException("Cartridge can't be loaded because it's CGB only and this device was created as DMG");
            }

            Type = type;
            Mode = cartridge.CGBSupportCode switch
            {
                CGBSupportCode.CGBExclusive => DeviceType.CGB,
                CGBSupportCode.CGBCompatible => type,
                CGBSupportCode.CGBIncompatible => DeviceType.DMG,
                _ => throw new ArgumentOutOfRangeException()
            };

            Renderer = renderer;
            SoundOutput = soundOutput;

            InterruptRegisters = new InterruptRegisters();
            ControlRegisters = new ControlRegisters();
            APU = new APU(this);
            LCDRegisters = new LCDRegisters(this);
            Cartridge = cartridge;
            MMU = new MMU(bootRom, this);
            CPU = new CPU.CPU(this);
            _cpuGenerator = CPU.GetEnumerator();
            LCDDriver = new LCDDriver(this);
            Timer = new Timer(this);
            DMAController = new DMAController(this);
            JoypadHandler = new JoypadHandler(this);

            // Set default values if there was no passed in boot rom
            if (bootRom == null) SkipBootRom();
        }

        /// <summary>
        /// Debug information about the LCD driver & registers
        /// </summary>
        /// 
        /// <returns>
        /// 1. The contents of VRAM bank 0
        /// 2. The contents of VRAM bank 1
        /// 3. The contents of OAM RAM
        /// 4. The tile buffer (i.e. which tile each pixel comes from)
        /// 5. The contents of the CGB BG Palette
        /// 6. The contents of the CGB Sprite Palette
        /// 7. The contents of the framebuffer
        /// </returns>
        public (byte[], byte[], byte[], byte[], (byte, byte, byte)[], (byte, byte, byte)[], byte[]) DumpLcdDebugInformation()
        {
            var (bank0, bank1, oam, tileBuffer) = LCDDriver.DumpVRAM();
            return (
                bank0,
                bank1,
                oam,
                tileBuffer,
                LCDRegisters.CGBBackgroundPalette.Palette,
                LCDRegisters.CGBSpritePalette.Palette,
                LCDDriver.GetCurrentFrame()
            );
        }

        /// <summary>
        /// Sets registers/memory to initial values as if we've run the boot 
        /// rom successfully.
        /// </summary>
        public void SkipBootRom()
        {
            // Set up registers
            switch (Type, Mode)
            {
                case (DeviceType.DMG, DeviceType.DMG): // DMG device running a DMG cartridge
                    CPU.Registers.AF = 0x01B0;
                    CPU.Registers.BC = 0x0013;
                    CPU.Registers.DE = 0x00D8;
                    CPU.Registers.HL = 0x014D;
                    break;
                case (DeviceType.CGB, DeviceType.DMG): // CGB device running a DMG cartridge
                    CPU.Registers.AF = 0x1180;
                    CPU.Registers.BC = 0x0000;
                    CPU.Registers.DE = 0x0008;
                    CPU.Registers.HL = 0x007C;
                    break;
                case (DeviceType.CGB, DeviceType.CGB): // CGB device running a CGB cartridge
                    CPU.Registers.AF = 0x1180;
                    CPU.Registers.BC = 0x0000;
                    CPU.Registers.DE = 0xFF56;
                    CPU.Registers.HL = 0x000D;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Same across all device types to the best available knowledge
            CPU.Registers.ProgramCounter = 0x0100;
            CPU.Registers.StackPointer = 0xFFFE;

            // Set up memory
            MMU.WriteByte(0xFF05, 0);
            MMU.WriteByte(0xFF06, 0);
            MMU.WriteByte(0xFF07, 0);
            MMU.WriteByte(0xFF40, 0x91);
            MMU.WriteByte(0xFF42, 0);
            MMU.WriteByte(0xFF43, 0);
            MMU.WriteByte(0xFF45, 0);
            MMU.WriteByte(0xFF47, 0xFC);
            MMU.WriteByte(0xFF48, 0xFF);
            MMU.WriteByte(0xFF49, 0xFF);
            MMU.WriteByte(0xFF4A, 0);
            MMU.WriteByte(0xFF4B, 0);
            MMU.WriteByte(0xFF50, 0x1); // Turn off boot ROM

            APU.SkipBootRom();

            Timer.Reset(true); // Set up system counter (aka DIV register)
        }

        /// <summary>
        /// Performs a update to all subsystems returning the number of t-cycles
        /// taken.
        /// </summary>
        /// <returns>
        /// The total number of t-cycles taken by the step.
        /// </returns>
        public int Step()
        {
            //Log.Information("{0}", CPU.Registers);

            // Step the CPU by 1 m-cycle
            _cpuGenerator.MoveNext();

            var nonCpuCycles = DoubleSpeed ? 2 : 4;

            // Step 3: Run the DMA controller to move bytes directly into VRAM/OAM
            DMAController.Step(4);

            // Step 4: Step the LCD subsystem, only once per CPU cycle pair in double speed mode
            if (!DoubleSpeed || _stepPPUOnNextDoubleSpeedCycle)
            {
                LCDDriver.Step();
                _stepPPUOnNextDoubleSpeedCycle = false;
            }
            else
            {
                _stepPPUOnNextDoubleSpeedCycle = true;
            }

            // Step 5: Update the timer controller by a single m-cycle
            Timer.Step();

            // Step 6: Step audio subsystem
            APU.Step(nonCpuCycles);

            TCycles += 4;

            return 4;
        }

        /// <summary>
        /// Called whenever a key down event is handled by the calling code
        /// </summary>
        /// <param name="key">The key pressed</param>
        public void HandleKeyDown(DeviceKey key)
        {
            JoypadHandler.Keydown(key);
        }

        /// <summary>
        /// Called whenever a key up event is handled by the calling code
        /// </summary>
        /// <param name="key">The key lifted</param>
        public void HandleKeyUp(DeviceKey key)
        {
            JoypadHandler.Keyup(key);
        }

        public override string ToString()
        {
            return $"{ControlRegisters} {CPU.Registers} {LCDRegisters} {Timer} {InterruptRegisters} Cyc:{Timer.SystemCounter}";
        }
    }
}
