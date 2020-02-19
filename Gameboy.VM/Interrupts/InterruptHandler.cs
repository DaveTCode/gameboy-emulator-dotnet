using System.Collections;
using System.Collections.Generic;

namespace Gameboy.VM.Interrupts
{
    internal class InterruptHandler : IEnumerable<int>
    {
        private readonly Device _device;

        internal InterruptHandler(Device device)
        {
            _device = device;
        }

        public IEnumerator<int> GetEnumerator()
        {
            while (true)
            {
                if (_device.DMAController.BlockInterrupts()) yield return 0;

                // Note that the priority ordering is the same as the bit ordering so this works
                for (var bit = 0; bit < 6; bit++)
                {
                    var mask = 1 << bit;
                    if ((_device.InterruptRegisters.InterruptEnable & _device.InterruptRegisters.InterruptFlags & mask) == mask)
                    {
                        if (_device.CPU.IsHalted)
                        {
                            _device.CPU.IsHalted = false;
                            yield return 1;
                        }

                        // TODO - Not really sure what STOP mode actually means, presumably this is correct
                        if (_device.CPU.IsStopped)
                        {
                            _device.CPU.IsStopped = false;
                            yield return 1;
                        }

                        if (_device.InterruptRegisters.AreInterruptsEnabledGlobally)
                        {
                            // Set this flag to true so that the CPU is paused whilst we look for interrupts to handle
                            _device.CPU.IsProcessingInterrupt = true;

                            var interrupt = (Interrupt)bit;

                            _device.Log.Information("Handling interrupt {0}", interrupt);

                            // First disable the master interrupt flag
                            _device.InterruptRegisters.AreInterruptsEnabledGlobally = false;

                            // Then reset the interrupt request
                            _device.InterruptRegisters.ResetInterrupt(interrupt);

                            // Finally push the PC to the stack and call the interrupt address
                            // Note that we only handle one interrupt at a time, the
                            // next won't be handled until the previous one completes
                            // and that's done through normal opcode cycles.
                            var b1 = _device.CPU.FetchByte();
                            yield return 1;
                            var b2 = _device.CPU.FetchByte();
                            yield return 1;

                            _device.MMU.WriteByte(_device.CPU.Registers.StackPointer--, (byte)(_device.CPU.Registers.ProgramCounter & 0xFF));
                            yield return 1;
                            _device.MMU.WriteByte(_device.CPU.Registers.StackPointer--, (byte)(_device.CPU.Registers.ProgramCounter >> 8));
                            yield return 1;

                            yield return 1;
                            _device.CPU.Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                        }
                    }
                }

                yield return 0;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
