﻿using Gameboy.VM.LCD;
using Gameboy.VM.Interrupts;
using Gameboy.VM.Sound;

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
        public const int ClockCyclesPerSecond = 4194304;

        /// <summary>
        /// Original ROM from a DMG, used to set initial values of registers
        /// </summary>
        internal static readonly byte[] DmgRomContents =
        {
            0x31, 0xfe, 0xff, 0xaf, 0x21, 0xff, 0x9f, 0x32, 0xcb, 0x7c, 0x20, 0xfb, 0x21, 0x26, 0xff, 0x0e,
            0x11, 0x3e, 0x80, 0x32, 0xe2, 0x0c, 0x3e, 0xf3, 0xe2, 0x32, 0x3e, 0x77, 0x77, 0x3e, 0xfc, 0xe0,
            0x47, 0x11, 0x04, 0x01, 0x21, 0x10, 0x80, 0x1a, 0xcd, 0x95, 0x20, 0xcd, 0x96, 0x20, 0x13, 0x7b,
            0xfe, 0x34, 0x20, 0xf3, 0x11, 0xd8, 0x20, 0x06, 0x08, 0x1a, 0x13, 0x22, 0x23, 0x05, 0x20, 0xf9,
            0x3e, 0x19, 0xea, 0x10, 0x99, 0x21, 0x2f, 0x99, 0x0e, 0x0c, 0x3d, 0x28, 0x08, 0x32, 0x0d, 0x20,
            0xf9, 0x2e, 0x0f, 0x18, 0xf3, 0x67, 0x3e, 0x64, 0x57, 0xe0, 0x42, 0x3e, 0x91, 0xe0, 0x40, 0x04,
            0x1e, 0x02, 0x0e, 0x0c, 0xf0, 0x44, 0xfe, 0x90, 0x20, 0xfa, 0x0d, 0x20, 0xf7, 0x1d, 0x20, 0xf2,
            0x0e, 0x13, 0x24, 0x7c, 0x1e, 0x83, 0xfe, 0x62, 0x28, 0x06, 0x1e, 0xc1, 0xfe, 0x64, 0x20, 0x06,
            0x7b, 0xe2, 0x0c, 0x3e, 0x87, 0xe2, 0xf0, 0x42, 0x90, 0xe0, 0x42, 0x15, 0x20, 0xd2, 0x05, 0x20,
            0x4f, 0x16, 0x20, 0x18, 0xcb, 0x4f, 0x06, 0x04, 0xc5, 0xcb, 0x11, 0x17, 0xc1, 0xcb, 0x11, 0x17,
            0x05, 0x20, 0xf5, 0x22, 0x23, 0x22, 0x23, 0xc9, 0xce, 0xed, 0x66, 0x66, 0xcc, 0x0d, 0x20, 0x0b,
            0x03, 0x73, 0x20, 0x83, 0x20, 0x0c, 0x20, 0x0d, 0x20, 0x08, 0x11, 0x1f, 0x88, 0x89, 0x20, 0x0e,
            0xdc, 0xcc, 0x6e, 0xe6, 0xdd, 0xdd, 0xd9, 0x99, 0xbb, 0xbb, 0x67, 0x63, 0x6e, 0x0e, 0xec, 0xcc,
            0xdd, 0xdc, 0x99, 0x9f, 0xbb, 0xb9, 0x33, 0x3e, 0x3c, 0x42, 0xb9, 0xa5, 0xb9, 0xa5, 0x42, 0x3c,
            0x21, 0x04, 0x01, 0x11, 0xa8, 0x20, 0x1a, 0x13, 0xbe, 0x20, 0xfe, 0x23, 0x7d, 0xfe, 0x34, 0x20,
            0xf5, 0x06, 0x19, 0x78, 0x86, 0x23, 0x05, 0x20, 0xfb, 0x86, 0x20, 0xfe, 0x3e, 0x01, 0xe0, 0x50
        };

        private readonly MMU _mmu;
        private readonly CPU.CPU _cpu;
        private readonly ControlRegisters _controlRegisters;
        private readonly SoundRegisters _soundRegisters;
        private readonly LCDRegisters _lcdRegisters;
        private readonly InterruptRegisters _interruptRegisters;
        private readonly Cartridge.Cartridge _cartridge;
        private readonly LCDDriver _lcdDriver;

        public Device(Cartridge.Cartridge cartridge)
        {
            _interruptRegisters = new InterruptRegisters();
            _controlRegisters = new ControlRegisters();
            _soundRegisters = new SoundRegisters();
            _lcdRegisters = new LCDRegisters();
            _cartridge = cartridge;
            _mmu = new MMU(DmgRomContents, _controlRegisters, _soundRegisters, _lcdRegisters, _interruptRegisters, _cartridge);
            _cpu = new CPU.CPU(_mmu, _interruptRegisters);
            _lcdDriver = new LCDDriver(_mmu, _lcdRegisters, _interruptRegisters);
        }

        /// <summary>
        /// Rendering the screen should only really happen during VBlank so
        /// we expose that to the calling code here.
        /// </summary>
        /// <returns>
        /// True if the LCD is performing VBlank and false otherwise
        /// </returns>
        public bool SafeToDrawScreen() => _lcdRegisters.LCDCurrentScanline >= ScreenHeight;

        /// <summary>
        /// We need to expose information on whether the LCD is actually on to
        /// determine whether to actually display anything.
        /// </summary>
        /// <returns></returns>
        public bool IsScreenOn() => _lcdRegisters.IsLcdOn;

        public Grayscale[] GetCurrentFrame()
        {
            return _lcdDriver.GetCurrentFrame();
        }

        /// <summary>
        /// Sets registers/memory to initial values as if we've run the boot 
        /// rom successfully.
        /// </summary>
        public void SkipBootRom()
        {
            // Set up registers
            _cpu.Registers.AF = 0x01B0;
            _cpu.Registers.BC = 0x0013;
            _cpu.Registers.DE = 0x00D8;
            _cpu.Registers.HL = 0x014D;
            _cpu.Registers.ProgramCounter = 0x0100;
            _cpu.Registers.StackPointer = 0xFFFE;

            // Set up memory
            _mmu.WriteByte(0xFF05, 0);
            _mmu.WriteByte(0xFF06, 0);
            _mmu.WriteByte(0xFF07, 0);
            _mmu.WriteByte(0xFF10, 0x80);
            _mmu.WriteByte(0xFF11, 0xBF);
            _mmu.WriteByte(0xFF12, 0xF3);
            _mmu.WriteByte(0xFF14, 0xBF);
            _mmu.WriteByte(0xFF16, 0x3F);
            _mmu.WriteByte(0xFF16, 0x3F);
            _mmu.WriteByte(0xFF17, 0);
            _mmu.WriteByte(0xFF19, 0xBF);
            _mmu.WriteByte(0xFF1A, 0x7F);
            _mmu.WriteByte(0xFF1B, 0xFF);
            _mmu.WriteByte(0xFF1C, 0x9F);
            _mmu.WriteByte(0xFF1E, 0xFF);
            _mmu.WriteByte(0xFF20, 0xFF);
            _mmu.WriteByte(0xFF21, 0);
            _mmu.WriteByte(0xFF22, 0);
            _mmu.WriteByte(0xFF23, 0xBF);
            _mmu.WriteByte(0xFF24, 0x77);
            _mmu.WriteByte(0xFF25, 0xF3);
            _mmu.WriteByte(0xFF26, 0xF1);
            _mmu.WriteByte(0xFF40, 0x91);
            _mmu.WriteByte(0xFF42, 0);
            _mmu.WriteByte(0xFF43, 0);
            _mmu.WriteByte(0xFF45, 0);
            _mmu.WriteByte(0xFF47, 0xFC);
            _mmu.WriteByte(0xFF48, 0xFF);
            _mmu.WriteByte(0xFF49, 0xFF);
            _mmu.WriteByte(0xFF4A, 0);
            _mmu.WriteByte(0xFF4B, 0);
        }

        /// <summary>
        /// Performs a update to all subsystems returning the number of CPU
        /// cycles taken.
        /// </summary>
        /// <returns>
        /// The total number of CPU cycles taken by the step.
        /// </returns>
        public int Step()
        {
            // Step 1: Check for interrupts
            var cycles = _cpu.CheckForInterrupts();

            if (cycles > 0)
            {
                // Step 1.5: Update the LCD subsystem to sync with the new number of cycles if an interrupt occurred
                _lcdDriver.Step(cycles);
            }

            // Step 2: Atomically run the next operation
            cycles += _cpu.Step();

            // Step 3: Update the LCD subsystem to sync with the new number of cycles
            _lcdDriver.Step(cycles);

            // Print out debug information after each cycle
            //Trace.TraceInformation("{0} {1} {2}", _controlRegisters, _cpu.Registers, _lcdRegisters);

            return cycles;
        }
    }
}
