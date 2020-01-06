using System.IO;
using Gameboy.VM.Cartridge;
using Gameboy.VM.Interrupts;
using Gameboy.VM.LCD;
using Gameboy.VM.Sound;

namespace Gameboy.VM.Cpu.Tests
{
    internal static class TestUtils
    {
        internal static Cartridge.Cartridge CreateMBC0ROMCartridge()
        {
            return CartridgeFactory.CreateCartridge(File.ReadAllBytes(Path.Join("ROMs", "base.gb")));
        }

        internal static VM.CPU.CPU CreateCPU()
        {
            var interruptRegisters = new InterruptRegisters();
            
            return new VM.CPU.CPU(CreateMMU(interruptRegisters), interruptRegisters);
        }

        internal static VM.MMU CreateMMU(InterruptRegisters interruptRegisters)
        {
            return new VM.MMU(Device.DmgRomContents, new ControlRegisters(), new SoundRegisters(), new LCDRegisters(), interruptRegisters, CreateMBC0ROMCartridge());
        }
    }
}
