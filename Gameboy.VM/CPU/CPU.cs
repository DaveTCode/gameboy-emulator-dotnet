using Gameboy.VM.Interrupts;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Gameboy.VM.CPU
{
    internal class CPU : IEnumerable<int>
    {
        private readonly ALU _alu;
        private readonly Device _device;

        private bool _isHalted;
        private bool _isHaltBugState;
        internal bool IsStopped;
        private int _enableInterruptsCountdown;
        internal bool IsProcessingInstruction;

        internal Registers Registers { get; }

        internal CPU(Device device)
        {
            Registers = new Registers();
            _alu = new ALU(this);
            _device = device;
            Reset();
        }

        /// <summary>
        /// Emulates a single step of the CPU and returns the number of cycles
        /// that the step took.
        /// </summary>
        /// 
        /// <returns>
        /// Total TCycles this step would have taken on a real gameboy
        /// </returns>
        public IEnumerator<int> GetEnumerator()
        {
            while (true)
            {
                // EI instruction doesn't enable interrupts until after the _next_ instruction, quirk of hardware
                if (_enableInterruptsCountdown > 0)
                {
                    _enableInterruptsCountdown--;

                    if (_enableInterruptsCountdown == 0)
                    {
                        _device.InterruptRegisters.AreInterruptsEnabledGlobally = true;
                    }
                }

                // Before processing the next opcode check if there are any interrupts to fire
                if (!_device.DMAController.BlockInterrupts())
                {
                    // Note that the priority ordering is the same as the bit ordering so this works
                    for (var bit = 0; bit < 6; bit++)
                    {
                        var mask = 1 << bit;
                        if ((_device.InterruptRegisters.InterruptEnable & _device.InterruptRegisters.InterruptFlags & mask) == mask)
                        {
                            if (_isHalted)
                            {
                                _isHalted = false;
                                //yield return 1;
                            }

                            // TODO - Not really sure what STOP mode actually means, presumably this is correct
                            if (_device.CPU.IsStopped)
                            {
                                IsStopped = false;
                                yield return 1;
                            }

                            if (_device.InterruptRegisters.AreInterruptsEnabledGlobally)
                            {
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
                                yield return 1;
                                yield return 1;

                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(_device.CPU.Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(_device.CPU.Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = interrupt.StartingAddress();
                                yield return 1;
                            }
                        }
                    }
                }

                if (_isHalted || IsStopped || _device.DMAController.HaltCpu())
                {
                    yield return 0;
                }
                else
                {
                    var opcode = FetchByte();
                    IsProcessingInstruction = true;

                    if (_isHaltBugState)
                    {
                        Registers.ProgramCounter--;
                        _isHaltBugState = false;
                    }

                    switch (opcode)
                    {
                        case 0x00:
                            break;
                        case 0x01:
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;
                                Registers.BC = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0x02:
                            _device.MMU.WriteByte(Registers.BC, Registers.A);
                            yield return 1; // TODO - Which cycle does the write happen in? What happens on the other cycle?
                            break;
                        case 0x03:
                            Registers.BC++;
                            yield return 1; // TODO - Which cycles does the increment happen in? What happens on the other cycle?
                            break;
                        case 0x04:
                            _alu.Increment(ref Registers.B);
                            break;
                        case 0x05:
                            _alu.Decrement(ref Registers.B);
                            break;
                        case 0x06:
                            var d8 = FetchByte();
                            yield return 1;
                            Registers.B = d8;
                            break;
                        case 0x07:
                            _alu.RotateLeftWithCarryA();
                            break;
                        case 0x08:
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;
                                var address = (ushort)(b1 | (b2 << 8));
                                _device.MMU.WriteByte(address, (byte)(Registers.StackPointer & 0xFF));
                                yield return 1;
                                address++;
                                _device.MMU.WriteByte(address, (byte)(Registers.StackPointer >> 8));
                                yield return 1; // TODO - What happens in the final two cycles? Does it take a cycle to read SP in the middle?
                                break;
                            }
                        case 0x09:
                            _alu.AddHL(Registers.BC);
                            yield return 1;
                            break;
                        case 0x0A:
                            {
                                var b = _device.MMU.ReadByte(Registers.BC);
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x0B:
                            Registers.BC = (ushort)(Registers.BC - 1);
                            yield return 1;
                            break;
                        case 0x0C:
                            _alu.Increment(ref Registers.C);
                            break;
                        case 0x0D:
                            _alu.Decrement(ref Registers.C);
                            break;
                        case 0x0E:
                            {
                                var b = FetchByte();
                                yield return 1;
                                Registers.C = b;
                                break;
                            }
                        case 0x0F:
                            _alu.RotateRightWithCarryA();
                            break;
                        case 0x10: // STOP
                            {
                                var b = FetchByte();
                                yield return 1;
                                if (b != 0x0)
                                {
                                    // TODO - Should turn the LCD on I think
                                }

                                // Are we waiting to switch speed in GBC mode?
                                if (_device.ControlRegisters.SpeedSwitchRequested)
                                {
                                    _device.ControlRegisters.SpeedSwitchRequested = false;
                                    _device.DoubleSpeed = true;
                                }
                                else
                                {
                                    IsStopped = true;
                                }
                                break;
                            }
                        case 0x11:
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;
                                Registers.DE = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0x12:
                            _device.MMU.WriteByte(Registers.DE, Registers.A);
                            yield return 1; // TODO - Which cycle does the write happen in? What happens on the other cycle?
                            break;
                        case 0x13:
                            Registers.DE++;
                            yield return 1;
                            break;
                        case 0x14:
                            _alu.Increment(ref Registers.D);
                            break;
                        case 0x15:
                            _alu.Decrement(ref Registers.D);
                            break;
                        case 0x16:
                            {
                                var b = FetchByte();
                                yield return 1;
                                Registers.D = b;
                                break;
                            }
                        case 0x17:
                            _alu.RotateLeftNoCarryA();
                            break;
                        case 0x18:
                            {
                                var r8 = (sbyte)FetchByte();
                                yield return 1;
                                Registers.ProgramCounter = (ushort)((Registers.ProgramCounter + r8) & 0xFFFF);
                                yield return 1;
                                break;
                            }
                        case 0x19:
                            _alu.AddHL(Registers.DE);
                            yield return 1;
                            break;
                        case 0x1A:
                            {
                                var b = _device.MMU.ReadByte(Registers.DE);
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x1B:
                            Registers.DE--;
                            yield return 1;
                            break;
                        case 0x1C:
                            _alu.Increment(ref Registers.E);
                            break;
                        case 0x1D:
                            _alu.Decrement(ref Registers.E);
                            break;
                        case 0x1E:
                            {
                                var b = FetchByte();
                                yield return 1;
                                Registers.E = b;
                                break;
                            }
                        case 0x1F:
                            _alu.RotateRightNoCarryA();
                            break;
                        case 0x20:
                            {
                                var distance = (sbyte)FetchByte();
                                yield return 1;
                                if (Registers.GetFlag(CpuFlags.ZeroFlag))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.ProgramCounter = (ushort)((Registers.ProgramCounter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x21:
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;
                                Registers.HL = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0x22:
                            {
                                var address = Registers.HLI();
                                _device.MMU.WriteByte(address, Registers.A);
                                yield return 1;
                                break;
                            }
                        case 0x23:
                            yield return 1;
                            Registers.HL++;
                            break;
                        case 0x24:
                            _alu.Increment(ref Registers.H);
                            break;
                        case 0x25:
                            _alu.Decrement(ref Registers.H);
                            break;
                        case 0x26:
                            {
                                var b = FetchByte();
                                yield return 1;
                                Registers.H = b;
                                break;
                            }
                        case 0x27:
                            _alu.DecimalAdjustRegister(ref Registers.A);
                            break;
                        case 0x28:
                            {
                                var distance = (sbyte)FetchByte();
                                yield return 1;
                                if (!Registers.GetFlag(CpuFlags.ZeroFlag))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.ProgramCounter = (ushort)((Registers.ProgramCounter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x29:
                            yield return 1;
                            _alu.AddHL(Registers.HL);
                            break;
                        case 0x2A:
                            {
                                var b = _device.MMU.ReadByte(Registers.HLI());
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x2B:
                            yield return 1;
                            Registers.HL--;
                            break;
                        case 0x2C:
                            _alu.Increment(ref Registers.L);
                            break;
                        case 0x2D:
                            _alu.Decrement(ref Registers.L);
                            break;
                        case 0x2E:
                            {
                                var b = FetchByte();
                                yield return 1;
                                Registers.L = b;
                                break;
                            }
                        case 0x2F:
                            _alu.CPL();
                            break;
                        case 0x30:
                            {
                                var distance = (sbyte)FetchByte();
                                yield return 1;
                                if (Registers.GetFlag(CpuFlags.CarryFlag))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.ProgramCounter = (ushort)((Registers.ProgramCounter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x31:
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;
                                Registers.StackPointer = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0x32:
                            {
                                var address = Registers.HLD();
                                _device.MMU.WriteByte(address, Registers.A);
                                yield return 1;
                                break;
                            }
                        case 0x33:
                            yield return 1;
                            Registers.StackPointer++;
                            break;
                        case 0x34:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.Increment(ref b);
                                _device.MMU.WriteByte(Registers.HL, b);
                                yield return 1;
                                break;
                            }
                        case 0x35:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.Decrement(ref b);
                                _device.MMU.WriteByte(Registers.HL, b);
                                yield return 1;
                                break;
                            }
                        case 0x36:
                            {
                                var b = FetchByte();
                                yield return 1;
                                _device.MMU.WriteByte(Registers.HL, b);
                                yield return 1;
                                break;
                            }
                        case 0x37:
                            _alu.SCF();
                            break;
                        case 0x38:
                            {
                                var distance = (sbyte)FetchByte();
                                yield return 1;
                                if (!Registers.GetFlag(CpuFlags.CarryFlag))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.ProgramCounter = (ushort)((Registers.ProgramCounter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x39:
                            yield return 1;
                            _alu.AddHL(Registers.StackPointer);
                            break;
                        case 0x3A:
                            {
                                var b = _device.MMU.ReadByte(Registers.HLD());
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x3B:
                            yield return 1;
                            --Registers.StackPointer;
                            break;
                        case 0x3C:
                            _alu.Increment(ref Registers.A);
                            break;
                        case 0x3D:
                            _alu.Decrement(ref Registers.A);
                            break;
                        case 0x3E:
                            {
                                var b = FetchByte();
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x3F:
                            _alu.CCF();
                            break;
                        case 0x40:
                            break;
                        case 0x41:
                            Registers.B = Registers.C;
                            break;
                        case 0x42:
                            Registers.B = Registers.D;
                            break;
                        case 0x43:
                            Registers.B = Registers.E;
                            break;
                        case 0x44:
                            Registers.B = Registers.H;
                            break;
                        case 0x45:
                            Registers.B = Registers.L;
                            break;
                        case 0x46:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.B = b;
                                break;
                            }
                        case 0x47:
                            Registers.B = Registers.A;
                            break;
                        case 0x48:
                            Registers.C = Registers.B;
                            break;
                        case 0x49:
                            break;
                        case 0x4A:
                            Registers.C = Registers.D;
                            break;
                        case 0x4B:
                            Registers.C = Registers.E;
                            break;
                        case 0x4C:
                            Registers.C = Registers.H;
                            break;
                        case 0x4D:
                            Registers.C = Registers.L;
                            break;
                        case 0x4E:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.C = b;
                                break;
                            }
                        case 0x4F:
                            Registers.C = Registers.A;
                            break;
                        case 0x50:
                            Registers.D = Registers.B;
                            break;
                        case 0x51:
                            Registers.D = Registers.C;
                            break;
                        case 0x52:
                            break;
                        case 0x53:
                            Registers.D = Registers.E;
                            break;
                        case 0x54:
                            Registers.D = Registers.H;
                            break;
                        case 0x55:
                            Registers.D = Registers.L;
                            break;
                        case 0x56:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.D = b;
                                break;
                            }
                        case 0x57:
                            Registers.D = Registers.A;
                            break;
                        case 0x58:
                            Registers.E = Registers.B;
                            break;
                        case 0x59:
                            Registers.E = Registers.C;
                            break;
                        case 0x5A:
                            Registers.E = Registers.D;
                            break;
                        case 0x5B:
                            break;
                        case 0x5C:
                            Registers.E = Registers.H;
                            break;
                        case 0x5D:
                            Registers.E = Registers.L;
                            break;
                        case 0x5E:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.E = b;
                                break;
                            }
                        case 0x5F:
                            Registers.E = Registers.A;
                            break;
                        case 0x60:
                            Registers.H = Registers.B;
                            break;
                        case 0x61:
                            Registers.H = Registers.C;
                            break;
                        case 0x62:
                            Registers.H = Registers.D;
                            break;
                        case 0x63:
                            Registers.H = Registers.E;
                            break;
                        case 0x64:
                            break;
                        case 0x65:
                            Registers.H = Registers.L;
                            break;
                        case 0x66:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.H = b;
                                break;
                            }
                        case 0x67:
                            Registers.H = Registers.A;
                            break;
                        case 0x68:
                            Registers.L = Registers.B;
                            break;
                        case 0x69:
                            Registers.L = Registers.C;
                            break;
                        case 0x6A:
                            Registers.L = Registers.D;
                            break;
                        case 0x6B:
                            Registers.L = Registers.E;
                            break;
                        case 0x6C:
                            Registers.L = Registers.H;
                            break;
                        case 0x6D:
                            break;
                        case 0x6E:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.L = b;
                                break;
                            }
                        case 0x6F:
                            Registers.L = Registers.A;
                            break;
                        case 0x70:
                            _device.MMU.WriteByte(Registers.HL, Registers.B);
                            yield return 1;
                            break;
                        case 0x71:
                            _device.MMU.WriteByte(Registers.HL, Registers.C);
                            yield return 1;
                            break;
                        case 0x72:
                            _device.MMU.WriteByte(Registers.HL, Registers.D);
                            yield return 1;
                            break;
                        case 0x73:
                            _device.MMU.WriteByte(Registers.HL, Registers.E);
                            yield return 1;
                            break;
                        case 0x74:
                            _device.MMU.WriteByte(Registers.HL, Registers.H);
                            yield return 1;
                            break;
                        case 0x75:
                            _device.MMU.WriteByte(Registers.HL, Registers.L);
                            yield return 1;
                            break;
                        case 0x76: // HALT
                            _isHalted = true;
                            _isHaltBugState = false;

                            // Odd HALT behaviour (don't increment PC on next read) happens
                            if (!_device.InterruptRegisters.AreInterruptsEnabledGlobally &&
                                (_device.InterruptRegisters.InterruptEnable & _device.InterruptRegisters.InterruptFlags & 0x1F) != 0)
                            {
                                _isHaltBugState = true;
                            }
                            break;
                        case 0x77:
                            _device.MMU.WriteByte(Registers.HL, Registers.A);
                            yield return 1;
                            break;
                        case 0x78:
                            Registers.A = Registers.B;
                            break;
                        case 0x79:
                            Registers.A = Registers.C;
                            break;
                        case 0x7A:
                            Registers.A = Registers.D;
                            break;
                        case 0x7B:
                            Registers.A = Registers.E;
                            break;
                        case 0x7C:
                            Registers.A = Registers.H;
                            break;
                        case 0x7D:
                            Registers.A = Registers.L;
                            break;
                        case 0x7E:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x7F:
                            break;
                        case 0x80:
                            _alu.Add(ref Registers.A, Registers.B, false);
                            break;
                        case 0x81:
                            _alu.Add(ref Registers.A, Registers.C, false);
                            break;
                        case 0x82:
                            _alu.Add(ref Registers.A, Registers.D, false);
                            break;
                        case 0x83:
                            _alu.Add(ref Registers.A, Registers.E, false);
                            break;
                        case 0x84:
                            _alu.Add(ref Registers.A, Registers.H, false);
                            break;
                        case 0x85:
                            _alu.Add(ref Registers.A, Registers.L, false);
                            break;
                        case 0x86:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.Add(ref Registers.A, b, false);
                                break;
                            }
                        case 0x87:
                            _alu.Add(ref Registers.A, Registers.A, false);
                            break;
                        case 0x88:
                            _alu.Add(ref Registers.A, Registers.B, true);
                            break;
                        case 0x89:
                            _alu.Add(ref Registers.A, Registers.C, true);
                            break;
                        case 0x8A:
                            _alu.Add(ref Registers.A, Registers.D, true);
                            break;
                        case 0x8B:
                            _alu.Add(ref Registers.A, Registers.E, true);
                            break;
                        case 0x8C:
                            _alu.Add(ref Registers.A, Registers.H, true);
                            break;
                        case 0x8D:
                            _alu.Add(ref Registers.A, Registers.L, true);
                            break;
                        case 0x8E:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.Add(ref Registers.A, b, true);
                                break;
                            }
                        case 0x8F:
                            _alu.Add(ref Registers.A, Registers.A, true);
                            break;
                        case 0x90:
                            _alu.Sub(ref Registers.A, Registers.B, false);
                            break;
                        case 0x91:
                            _alu.Sub(ref Registers.A, Registers.C, false);
                            break;
                        case 0x92:
                            _alu.Sub(ref Registers.A, Registers.D, false);
                            break;
                        case 0x93:
                            _alu.Sub(ref Registers.A, Registers.E, false);
                            break;
                        case 0x94:
                            _alu.Sub(ref Registers.A, Registers.H, false);
                            break;
                        case 0x95:
                            _alu.Sub(ref Registers.A, Registers.L, false);
                            break;
                        case 0x96:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.Sub(ref Registers.A, b, false);
                                break;
                            }
                        case 0x97:
                            _alu.Sub(ref Registers.A, Registers.A, false);
                            break;
                        case 0x98:
                            _alu.Sub(ref Registers.A, Registers.B, true);
                            break;
                        case 0x99:
                            _alu.Sub(ref Registers.A, Registers.C, true);
                            break;
                        case 0x9A:
                            _alu.Sub(ref Registers.A, Registers.D, true);
                            break;
                        case 0x9B:
                            _alu.Sub(ref Registers.A, Registers.E, true);
                            break;
                        case 0x9C:
                            _alu.Sub(ref Registers.A, Registers.H, true);
                            break;
                        case 0x9D:
                            _alu.Sub(ref Registers.A, Registers.L, true);
                            break;
                        case 0x9E:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.Sub(ref Registers.A, b, true);
                                break;
                            }
                        case 0x9F:
                            _alu.Sub(ref Registers.A, Registers.A, true);
                            break;
                        case 0xA0:
                            _alu.And(ref Registers.A, Registers.B);
                            break;
                        case 0xA1:
                            _alu.And(ref Registers.A, Registers.C);
                            break;
                        case 0xA2:
                            _alu.And(ref Registers.A, Registers.D);
                            break;
                        case 0xA3:
                            _alu.And(ref Registers.A, Registers.E);
                            break;
                        case 0xA4:
                            _alu.And(ref Registers.A, Registers.H);
                            break;
                        case 0xA5:
                            _alu.And(ref Registers.A, Registers.L);
                            break;
                        case 0xA6:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.And(ref Registers.A, b);
                                break;
                            }
                        case 0xA7:
                            _alu.And(ref Registers.A, Registers.A);
                            break;
                        case 0xA8:
                            _alu.Xor(ref Registers.A, Registers.B);
                            break;
                        case 0xA9:
                            _alu.Xor(ref Registers.A, Registers.C);
                            break;
                        case 0xAA:
                            _alu.Xor(ref Registers.A, Registers.D);
                            break;
                        case 0xAB:
                            _alu.Xor(ref Registers.A, Registers.E);
                            break;
                        case 0xAC:
                            _alu.Xor(ref Registers.A, Registers.H);
                            break;
                        case 0xAD:
                            _alu.Xor(ref Registers.A, Registers.L);
                            break;
                        case 0xAE:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.Xor(ref Registers.A, b);
                                break;
                            }
                        case 0xAF:
                            _alu.Xor(ref Registers.A, Registers.A);
                            break;
                        case 0xB0:
                            _alu.Or(ref Registers.A, Registers.B);
                            break;
                        case 0xB1:
                            _alu.Or(ref Registers.A, Registers.C);
                            break;
                        case 0xB2:
                            _alu.Or(ref Registers.A, Registers.D);
                            break;
                        case 0xB3:
                            _alu.Or(ref Registers.A, Registers.E);
                            break;
                        case 0xB4:
                            _alu.Or(ref Registers.A, Registers.H);
                            break;
                        case 0xB5:
                            _alu.Or(ref Registers.A, Registers.L);
                            break;
                        case 0xB6:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.Or(ref Registers.A, b);
                                break;
                            }
                        case 0xB7:
                            _alu.Or(ref Registers.A, Registers.A);
                            break;
                        case 0xB8:
                            _alu.Cp(Registers.A, Registers.B);
                            break;
                        case 0xB9:
                            _alu.Cp(Registers.A, Registers.C);
                            break;
                        case 0xBA:
                            _alu.Cp(Registers.A, Registers.D);
                            break;
                        case 0xBB:
                            _alu.Cp(Registers.A, Registers.E);
                            break;
                        case 0xBC:
                            _alu.Cp(Registers.A, Registers.H);
                            break;
                        case 0xBD:
                            _alu.Cp(Registers.A, Registers.L);
                            break;
                        case 0xBE:
                            {
                                var b = _device.MMU.ReadByte(Registers.HL);
                                yield return 1;
                                _alu.Cp(Registers.A, b);
                                break;
                            }
                        case 0xBF:
                            _alu.Cp(Registers.A, Registers.A);
                            break;
                        case 0xC0: // RET NZ
                            {
                                yield return 1;
                                if (Registers.GetFlag(CpuFlags.ZeroFlag))
                                {
                                    break;
                                }

                                // Get word from stack in 2 cycles
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8)); // TODO - What happens during the two cycles to set PC?
                                yield return 1;
                                break;
                            }
                        case 0xC1: // POP BC
                            {
                                // Get word from stack in 2 cycles
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                // Set word in BC in one cycle
                                Registers.BC = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xC2: // JP NZ, a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                if (Registers.GetFlag(CpuFlags.ZeroFlag))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xC3: // JP a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;
                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xC4: // CALL NZ, a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                if (Registers.GetFlag(CpuFlags.ZeroFlag))
                                {
                                    break;
                                }

                                yield return 1;// Internal delay

                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xC5: // PUSH BC
                            {
                                yield return 1; // Internal delay
                                _device.MMU.WriteByte(--Registers.StackPointer, Registers.B);
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, Registers.C);
                                yield return 1;
                                break;
                            }
                        case 0xC6:
                            {
                                var b = FetchByte();
                                yield return 1;
                                _alu.Add(ref Registers.A, b, false);
                                break;
                            }
                        case 0xC7: // RST 00
                            {
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;
                                Registers.ProgramCounter = 0x00;
                                break;
                            }
                        case 0xC8: // RET Z
                            {
                                yield return 1;
                                if (!Registers.GetFlag(CpuFlags.ZeroFlag))
                                {
                                    break;
                                }

                                // Get word from stack in 2 cycles
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8)); // TODO - What happens during the two cycles to set PC?
                                break;
                            }
                        case 0xC9: // RET
                            {
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8)); // TODO - What happens during the two cycles to set PC?
                                break;
                            }
                        case 0xCA: // JP Z, a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                if (!Registers.GetFlag(CpuFlags.ZeroFlag))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xCB:
                            {
                                var subcode = FetchByte();
                                yield return 1;

                                switch (subcode)
                                {
                                    case 0x00:
                                        _alu.RotateLeftWithCarry(ref Registers.B);
                                        break;
                                    case 0x01:
                                        _alu.RotateLeftWithCarry(ref Registers.C);
                                        break;
                                    case 0x02:
                                        _alu.RotateLeftWithCarry(ref Registers.D);
                                        break;
                                    case 0x03:
                                        _alu.RotateLeftWithCarry(ref Registers.E);
                                        break;
                                    case 0x04:
                                        _alu.RotateLeftWithCarry(ref Registers.H);
                                        break;
                                    case 0x05:
                                        _alu.RotateLeftWithCarry(ref Registers.L);
                                        break;
                                    case 0x06:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.RotateLeftWithCarry(ref b);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x07:
                                        _alu.RotateLeftWithCarry(ref Registers.A);
                                        break;
                                    case 0x08:
                                        _alu.RotateRightWithCarry(ref Registers.B);
                                        break;
                                    case 0x09:
                                        _alu.RotateRightWithCarry(ref Registers.C);
                                        break;
                                    case 0x0A:
                                        _alu.RotateRightWithCarry(ref Registers.D);
                                        break;
                                    case 0x0B:
                                        _alu.RotateRightWithCarry(ref Registers.E);
                                        break;
                                    case 0x0C:
                                        _alu.RotateRightWithCarry(ref Registers.H);
                                        break;
                                    case 0x0D:
                                        _alu.RotateRightWithCarry(ref Registers.L);
                                        break;
                                    case 0x0E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.RotateRightWithCarry(ref b);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x0F:
                                        _alu.RotateRightWithCarry(ref Registers.A);
                                        break;
                                    case 0x10:
                                        _alu.RotateLeftNoCarry(ref Registers.B);
                                        break;
                                    case 0x11:
                                        _alu.RotateLeftNoCarry(ref Registers.C);
                                        break;
                                    case 0x12:
                                        _alu.RotateLeftNoCarry(ref Registers.D);
                                        break;
                                    case 0x13:
                                        _alu.RotateLeftNoCarry(ref Registers.E);
                                        break;
                                    case 0x14:
                                        _alu.RotateLeftNoCarry(ref Registers.H);
                                        break;
                                    case 0x15:
                                        _alu.RotateLeftNoCarry(ref Registers.L);
                                        break;
                                    case 0x16:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.RotateLeftNoCarry(ref b);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x17:
                                        _alu.RotateLeftNoCarry(ref Registers.A);
                                        break;
                                    case 0x18:
                                        _alu.RotateRightNoCarry(ref Registers.B);
                                        break;
                                    case 0x19:
                                        _alu.RotateRightNoCarry(ref Registers.C);
                                        break;
                                    case 0x1A:
                                        _alu.RotateRightNoCarry(ref Registers.D);
                                        break;
                                    case 0x1B:
                                        _alu.RotateRightNoCarry(ref Registers.E);
                                        break;
                                    case 0x1C:
                                        _alu.RotateRightNoCarry(ref Registers.H);
                                        break;
                                    case 0x1D:
                                        _alu.RotateRightNoCarry(ref Registers.L);
                                        break;
                                    case 0x1E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.RotateRightNoCarry(ref b);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x1F:
                                        _alu.RotateRightNoCarry(ref Registers.A);
                                        break;
                                    case 0x20:
                                        _alu.ShiftLeft(ref Registers.B);
                                        break;
                                    case 0x21:
                                        _alu.ShiftLeft(ref Registers.C);
                                        break;
                                    case 0x22:
                                        _alu.ShiftLeft(ref Registers.D);
                                        break;
                                    case 0x23:
                                        _alu.ShiftLeft(ref Registers.E);
                                        break;
                                    case 0x24:
                                        _alu.ShiftLeft(ref Registers.H);
                                        break;
                                    case 0x25:
                                        _alu.ShiftLeft(ref Registers.L);
                                        break;
                                    case 0x26:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.ShiftLeft(ref b);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x27:
                                        _alu.ShiftLeft(ref Registers.A);
                                        break;
                                    case 0x28:
                                        _alu.ShiftRightAdjust(ref Registers.B);
                                        break;
                                    case 0x29:
                                        _alu.ShiftRightAdjust(ref Registers.C);
                                        break;
                                    case 0x2A:
                                        _alu.ShiftRightAdjust(ref Registers.D);
                                        break;
                                    case 0x2B:
                                        _alu.ShiftRightAdjust(ref Registers.E);
                                        break;
                                    case 0x2C:
                                        _alu.ShiftRightAdjust(ref Registers.H);
                                        break;
                                    case 0x2D:
                                        _alu.ShiftRightAdjust(ref Registers.L);
                                        break;
                                    case 0x2E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.ShiftRightAdjust(ref b);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x2F:
                                        _alu.ShiftRightAdjust(ref Registers.A);
                                        break;
                                    case 0x30:
                                        _alu.Swap(ref Registers.B);
                                        break;
                                    case 0x31:
                                        _alu.Swap(ref Registers.C);
                                        break;
                                    case 0x32:
                                        _alu.Swap(ref Registers.D);
                                        break;
                                    case 0x33:
                                        _alu.Swap(ref Registers.E);
                                        break;
                                    case 0x34:
                                        _alu.Swap(ref Registers.H);
                                        break;
                                    case 0x35:
                                        _alu.Swap(ref Registers.L);
                                        break;
                                    case 0x36:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Swap(ref b);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x37:
                                        _alu.Swap(ref Registers.A);
                                        break;
                                    case 0x38:
                                        _alu.ShiftRightLeave(ref Registers.B);
                                        break;
                                    case 0x39:
                                        _alu.ShiftRightLeave(ref Registers.C);
                                        break;
                                    case 0x3A:
                                        _alu.ShiftRightLeave(ref Registers.D);
                                        break;
                                    case 0x3B:
                                        _alu.ShiftRightLeave(ref Registers.E);
                                        break;
                                    case 0x3C:
                                        _alu.ShiftRightLeave(ref Registers.H);
                                        break;
                                    case 0x3D:
                                        _alu.ShiftRightLeave(ref Registers.L);
                                        break;
                                    case 0x3E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.ShiftRightLeave(ref b);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x3F:
                                        _alu.ShiftRightLeave(ref Registers.A);
                                        break;
                                    case 0x40:
                                        _alu.Bit(Registers.B, 0);
                                        break;
                                    case 0x41:
                                        _alu.Bit(Registers.C, 0);
                                        break;
                                    case 0x42:
                                        _alu.Bit(Registers.D, 0);
                                        break;
                                    case 0x43:
                                        _alu.Bit(Registers.E, 0);
                                        break;
                                    case 0x44:
                                        _alu.Bit(Registers.H, 0);
                                        break;
                                    case 0x45:
                                        _alu.Bit(Registers.L, 0);
                                        break;
                                    case 0x46:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Bit(b, 0);
                                            break;
                                        }
                                    case 0x47:
                                        _alu.Bit(Registers.A, 0);
                                        break;
                                    case 0x48:
                                        _alu.Bit(Registers.B, 1);
                                        break;
                                    case 0x49:
                                        _alu.Bit(Registers.C, 1);
                                        break;
                                    case 0x4A:
                                        _alu.Bit(Registers.D, 1);
                                        break;
                                    case 0x4B:
                                        _alu.Bit(Registers.E, 1);
                                        break;
                                    case 0x4C:
                                        _alu.Bit(Registers.H, 1);
                                        break;
                                    case 0x4D:
                                        _alu.Bit(Registers.L, 1);
                                        break;
                                    case 0x4E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Bit(b, 1);
                                            break;
                                        }
                                    case 0x4F:
                                        _alu.Bit(Registers.A, 1);
                                        break;
                                    case 0x50:
                                        _alu.Bit(Registers.B, 2);
                                        break;
                                    case 0x51:
                                        _alu.Bit(Registers.C, 2);
                                        break;
                                    case 0x52:
                                        _alu.Bit(Registers.D, 2);
                                        break;
                                    case 0x53:
                                        _alu.Bit(Registers.E, 2);
                                        break;
                                    case 0x54:
                                        _alu.Bit(Registers.H, 2);
                                        break;
                                    case 0x55:
                                        _alu.Bit(Registers.L, 2);
                                        break;
                                    case 0x56:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Bit(b, 2);
                                            break;
                                        }
                                    case 0x57:
                                        _alu.Bit(Registers.A, 2);
                                        break;
                                    case 0x58:
                                        _alu.Bit(Registers.B, 3);
                                        break;
                                    case 0x59:
                                        _alu.Bit(Registers.C, 3);
                                        break;
                                    case 0x5A:
                                        _alu.Bit(Registers.D, 3);
                                        break;
                                    case 0x5B:
                                        _alu.Bit(Registers.E, 3);
                                        break;
                                    case 0x5C:
                                        _alu.Bit(Registers.H, 3);
                                        break;
                                    case 0x5D:
                                        _alu.Bit(Registers.L, 3);
                                        break;
                                    case 0x5E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Bit(b, 3);
                                            break;
                                        }
                                    case 0x5F:
                                        _alu.Bit(Registers.A, 3);
                                        break;
                                    case 0x60:
                                        _alu.Bit(Registers.B, 4);
                                        break;
                                    case 0x61:
                                        _alu.Bit(Registers.C, 4);
                                        break;
                                    case 0x62:
                                        _alu.Bit(Registers.D, 4);
                                        break;
                                    case 0x63:
                                        _alu.Bit(Registers.E, 4);
                                        break;
                                    case 0x64:
                                        _alu.Bit(Registers.H, 4);
                                        break;
                                    case 0x65:
                                        _alu.Bit(Registers.L, 4);
                                        break;
                                    case 0x66:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Bit(b, 4);
                                            break;
                                        }
                                    case 0x67:
                                        _alu.Bit(Registers.A, 4);
                                        break;
                                    case 0x68:
                                        _alu.Bit(Registers.B, 5);
                                        break;
                                    case 0x69:
                                        _alu.Bit(Registers.C, 5);
                                        break;
                                    case 0x6A:
                                        _alu.Bit(Registers.D, 5);
                                        break;
                                    case 0x6B:
                                        _alu.Bit(Registers.E, 5);
                                        break;
                                    case 0x6C:
                                        _alu.Bit(Registers.H, 5);
                                        break;
                                    case 0x6D:
                                        _alu.Bit(Registers.L, 5);
                                        break;
                                    case 0x6E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Bit(b, 5);
                                            break;
                                        }
                                    case 0x6F:
                                        _alu.Bit(Registers.A, 5);
                                        break;
                                    case 0x70:
                                        _alu.Bit(Registers.B, 6);
                                        break;
                                    case 0x71:
                                        _alu.Bit(Registers.C, 6);
                                        break;
                                    case 0x72:
                                        _alu.Bit(Registers.D, 6);
                                        break;
                                    case 0x73:
                                        _alu.Bit(Registers.E, 6);
                                        break;
                                    case 0x74:
                                        _alu.Bit(Registers.H, 6);
                                        break;
                                    case 0x75:
                                        _alu.Bit(Registers.L, 6);
                                        break;
                                    case 0x76:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Bit(b, 6);
                                            break;
                                        }
                                    case 0x77:
                                        _alu.Bit(Registers.A, 6);
                                        break;
                                    case 0x78:
                                        _alu.Bit(Registers.B, 7);
                                        break;
                                    case 0x79:
                                        _alu.Bit(Registers.C, 7);
                                        break;
                                    case 0x7A:
                                        _alu.Bit(Registers.D, 7);
                                        break;
                                    case 0x7B:
                                        _alu.Bit(Registers.E, 7);
                                        break;
                                    case 0x7C:
                                        _alu.Bit(Registers.H, 7);
                                        break;
                                    case 0x7D:
                                        _alu.Bit(Registers.L, 7);
                                        break;
                                    case 0x7E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Bit(b, 7);
                                            break;
                                        }
                                    case 0x7F:
                                        _alu.Bit(Registers.A, 7);
                                        break;
                                    case 0x80:
                                        _alu.Res(ref Registers.B, 0);
                                        break;
                                    case 0x81:
                                        _alu.Res(ref Registers.C, 0);
                                        break;
                                    case 0x82:
                                        _alu.Res(ref Registers.D, 0);
                                        break;
                                    case 0x83:
                                        _alu.Res(ref Registers.E, 0);
                                        break;
                                    case 0x84:
                                        _alu.Res(ref Registers.H, 0);
                                        break;
                                    case 0x85:
                                        _alu.Res(ref Registers.L, 0);
                                        break;
                                    case 0x86:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Res(ref b, 0);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x87:
                                        _alu.Res(ref Registers.A, 0);
                                        break;
                                    case 0x88:
                                        _alu.Res(ref Registers.B, 1);
                                        break;
                                    case 0x89:
                                        _alu.Res(ref Registers.C, 1);
                                        break;
                                    case 0x8A:
                                        _alu.Res(ref Registers.D, 1);
                                        break;
                                    case 0x8B:
                                        _alu.Res(ref Registers.E, 1);
                                        break;
                                    case 0x8C:
                                        _alu.Res(ref Registers.H, 1);
                                        break;
                                    case 0x8D:
                                        _alu.Res(ref Registers.L, 1);
                                        break;
                                    case 0x8E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Res(ref b, 1);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x8F:
                                        _alu.Res(ref Registers.A, 1);
                                        break;
                                    case 0x90:
                                        _alu.Res(ref Registers.B, 2);
                                        break;
                                    case 0x91:
                                        _alu.Res(ref Registers.C, 2);
                                        break;
                                    case 0x92:
                                        _alu.Res(ref Registers.D, 2);
                                        break;
                                    case 0x93:
                                        _alu.Res(ref Registers.E, 2);
                                        break;
                                    case 0x94:
                                        _alu.Res(ref Registers.H, 2);
                                        break;
                                    case 0x95:
                                        _alu.Res(ref Registers.L, 2);
                                        break;
                                    case 0x96:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Res(ref b, 2);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x97:
                                        _alu.Res(ref Registers.A, 2);
                                        break;
                                    case 0x98:
                                        _alu.Res(ref Registers.B, 3);
                                        break;
                                    case 0x99:
                                        _alu.Res(ref Registers.C, 3);
                                        break;
                                    case 0x9A:
                                        _alu.Res(ref Registers.D, 3);
                                        break;
                                    case 0x9B:
                                        _alu.Res(ref Registers.E, 3);
                                        break;
                                    case 0x9C:
                                        _alu.Res(ref Registers.H, 3);
                                        break;
                                    case 0x9D:
                                        _alu.Res(ref Registers.L, 3);
                                        break;
                                    case 0x9E:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Res(ref b, 3);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x9F:
                                        _alu.Res(ref Registers.A, 3);
                                        break;
                                    case 0xA0:
                                        _alu.Res(ref Registers.B, 4);
                                        break;
                                    case 0xA1:
                                        _alu.Res(ref Registers.C, 4);
                                        break;
                                    case 0xA2:
                                        _alu.Res(ref Registers.D, 4);
                                        break;
                                    case 0xA3:
                                        _alu.Res(ref Registers.E, 4);
                                        break;
                                    case 0xA4:
                                        _alu.Res(ref Registers.H, 4);
                                        break;
                                    case 0xA5:
                                        _alu.Res(ref Registers.L, 4);
                                        break;
                                    case 0xA6:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Res(ref b, 4);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xA7:
                                        _alu.Res(ref Registers.A, 4);
                                        break;
                                    case 0xA8:
                                        _alu.Res(ref Registers.B, 5);
                                        break;
                                    case 0xA9:
                                        _alu.Res(ref Registers.C, 5);
                                        break;
                                    case 0xAA:
                                        _alu.Res(ref Registers.D, 5);
                                        break;
                                    case 0xAB:
                                        _alu.Res(ref Registers.E, 5);
                                        break;
                                    case 0xAC:
                                        _alu.Res(ref Registers.H, 5);
                                        break;
                                    case 0xAD:
                                        _alu.Res(ref Registers.L, 5);
                                        break;
                                    case 0xAE:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Res(ref b, 5);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xAF:
                                        _alu.Res(ref Registers.A, 5);
                                        break;
                                    case 0xB0:
                                        _alu.Res(ref Registers.B, 6);
                                        break;
                                    case 0xB1:
                                        _alu.Res(ref Registers.C, 6);
                                        break;
                                    case 0xB2:
                                        _alu.Res(ref Registers.D, 6);
                                        break;
                                    case 0xB3:
                                        _alu.Res(ref Registers.E, 6);
                                        break;
                                    case 0xB4:
                                        _alu.Res(ref Registers.H, 6);
                                        break;
                                    case 0xB5:
                                        _alu.Res(ref Registers.L, 6);
                                        break;
                                    case 0xB6:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Res(ref b, 6);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xB7:
                                        _alu.Res(ref Registers.A, 6);
                                        break;
                                    case 0xB8:
                                        _alu.Res(ref Registers.B, 7);
                                        break;
                                    case 0xB9:
                                        _alu.Res(ref Registers.C, 7);
                                        break;
                                    case 0xBA:
                                        _alu.Res(ref Registers.D, 7);
                                        break;
                                    case 0xBB:
                                        _alu.Res(ref Registers.E, 7);
                                        break;
                                    case 0xBC:
                                        _alu.Res(ref Registers.H, 7);
                                        break;
                                    case 0xBD:
                                        _alu.Res(ref Registers.L, 7);
                                        break;
                                    case 0xBE:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Res(ref b, 7);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xBF:
                                        _alu.Res(ref Registers.A, 7);
                                        break;
                                    case 0xC0:
                                        _alu.Set(ref Registers.B, 0);
                                        break;
                                    case 0xC1:
                                        _alu.Set(ref Registers.C, 0);
                                        break;
                                    case 0xC2:
                                        _alu.Set(ref Registers.D, 0);
                                        break;
                                    case 0xC3:
                                        _alu.Set(ref Registers.E, 0);
                                        break;
                                    case 0xC4:
                                        _alu.Set(ref Registers.H, 0);
                                        break;
                                    case 0xC5:
                                        _alu.Set(ref Registers.L, 0);
                                        break;
                                    case 0xC6:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Set(ref b, 0);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xC7:
                                        _alu.Set(ref Registers.A, 0);
                                        break;
                                    case 0xC8:
                                        _alu.Set(ref Registers.B, 1);
                                        break;
                                    case 0xC9:
                                        _alu.Set(ref Registers.C, 1);
                                        break;
                                    case 0xCA:
                                        _alu.Set(ref Registers.D, 1);
                                        break;
                                    case 0xCB:
                                        _alu.Set(ref Registers.E, 1);
                                        break;
                                    case 0xCC:
                                        _alu.Set(ref Registers.H, 1);
                                        break;
                                    case 0xCD:
                                        _alu.Set(ref Registers.L, 1);
                                        break;
                                    case 0xCE:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Set(ref b, 1);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xCF:
                                        _alu.Set(ref Registers.A, 1);
                                        break;
                                    case 0xD0:
                                        _alu.Set(ref Registers.B, 2);
                                        break;
                                    case 0xD1:
                                        _alu.Set(ref Registers.C, 2);
                                        break;
                                    case 0xD2:
                                        _alu.Set(ref Registers.D, 2);
                                        break;
                                    case 0xD3:
                                        _alu.Set(ref Registers.E, 2);
                                        break;
                                    case 0xD4:
                                        _alu.Set(ref Registers.H, 2);
                                        break;
                                    case 0xD5:
                                        _alu.Set(ref Registers.L, 2);
                                        break;
                                    case 0xD6:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Set(ref b, 2);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xD7:
                                        _alu.Set(ref Registers.A, 2);
                                        break;
                                    case 0xD8:
                                        _alu.Set(ref Registers.B, 3);
                                        break;
                                    case 0xD9:
                                        _alu.Set(ref Registers.C, 3);
                                        break;
                                    case 0xDA:
                                        _alu.Set(ref Registers.D, 3);
                                        break;
                                    case 0xDB:
                                        _alu.Set(ref Registers.E, 3);
                                        break;
                                    case 0xDC:
                                        _alu.Set(ref Registers.H, 3);
                                        break;
                                    case 0xDD:
                                        _alu.Set(ref Registers.L, 3);
                                        break;
                                    case 0xDE:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Set(ref b, 3);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xDF:
                                        _alu.Set(ref Registers.A, 3);
                                        break;
                                    case 0xE0:
                                        _alu.Set(ref Registers.B, 4);
                                        break;
                                    case 0xE1:
                                        _alu.Set(ref Registers.C, 4);
                                        break;
                                    case 0xE2:
                                        _alu.Set(ref Registers.D, 4);
                                        break;
                                    case 0xE3:
                                        _alu.Set(ref Registers.E, 4);
                                        break;
                                    case 0xE4:
                                        _alu.Set(ref Registers.H, 4);
                                        break;
                                    case 0xE5:
                                        _alu.Set(ref Registers.L, 4);
                                        break;
                                    case 0xE6:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Set(ref b, 4);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xE7:
                                        _alu.Set(ref Registers.A, 4);
                                        break;
                                    case 0xE8:
                                        _alu.Set(ref Registers.B, 5);
                                        break;
                                    case 0xE9:
                                        _alu.Set(ref Registers.C, 5);
                                        break;
                                    case 0xEA:
                                        _alu.Set(ref Registers.D, 5);
                                        break;
                                    case 0xEB:
                                        _alu.Set(ref Registers.E, 5);
                                        break;
                                    case 0xEC:
                                        _alu.Set(ref Registers.H, 5);
                                        break;
                                    case 0xED:
                                        _alu.Set(ref Registers.L, 5);
                                        break;
                                    case 0xEE:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Set(ref b, 5);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xEF:
                                        _alu.Set(ref Registers.A, 5);
                                        break;
                                    case 0xF0:
                                        _alu.Set(ref Registers.B, 6);
                                        break;
                                    case 0xF1:
                                        _alu.Set(ref Registers.C, 6);
                                        break;
                                    case 0xF2:
                                        _alu.Set(ref Registers.D, 6);
                                        break;
                                    case 0xF3:
                                        _alu.Set(ref Registers.E, 6);
                                        break;
                                    case 0xF4:
                                        _alu.Set(ref Registers.H, 6);
                                        break;
                                    case 0xF5:
                                        _alu.Set(ref Registers.L, 6);
                                        break;
                                    case 0xF6:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Set(ref b, 6);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xF7:
                                        _alu.Set(ref Registers.A, 6);
                                        break;
                                    case 0xF8:
                                        _alu.Set(ref Registers.B, 7);
                                        break;
                                    case 0xF9:
                                        _alu.Set(ref Registers.C, 7);
                                        break;
                                    case 0xFA:
                                        _alu.Set(ref Registers.D, 7);
                                        break;
                                    case 0xFB:
                                        _alu.Set(ref Registers.E, 7);
                                        break;
                                    case 0xFC:
                                        _alu.Set(ref Registers.H, 7);
                                        break;
                                    case 0xFD:
                                        _alu.Set(ref Registers.L, 7);
                                        break;
                                    case 0xFE:
                                        {
                                            var b = _device.MMU.ReadByte(Registers.HL);
                                            yield return 1;
                                            _alu.Set(ref b, 7);
                                            _device.MMU.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xFF:
                                        _alu.Set(ref Registers.A, 7);
                                        break;
                                    default:
                                        throw new ArgumentException($"CB sub opcode {subcode} not implemented", nameof(subcode));
                                }
                                break;
                            }
                        case 0xCC: // CALL Z, a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                if (!Registers.GetFlag(CpuFlags.ZeroFlag))
                                {
                                    break;
                                }

                                yield return 1;// Internal delay

                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xCD: // CALL a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                yield return 1;// Internal delay

                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xCE:
                            {
                                var b = FetchByte();
                                yield return 1;
                                _alu.Add(ref Registers.A, b, true);
                                break;
                            }
                        case 0xCF: // RST 08
                            {
                                yield return 1;

                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = 0x08;
                                break;
                            }
                        case 0xD0: // RET NC
                            {
                                yield return 1;
                                if (Registers.GetFlag(CpuFlags.CarryFlag))
                                {
                                    break;
                                }

                                // Get word from stack in 2 cycles
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8)); // TODO - What happens during the two cycles to set PC?
                                break;
                            }
                        case 0xD1: // POP DE
                            {
                                // Get word from stack in 2 cycles
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                // Set word in DE in one cycle
                                Registers.DE = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xD2: // JP NC, a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                if (Registers.GetFlag(CpuFlags.CarryFlag))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xD3:
                            _device.Log.Error("Invalid instruction {0} executed", opcode);
                            break;
                        case 0xD4: // CALL NC, a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                if (Registers.GetFlag(CpuFlags.CarryFlag))
                                {
                                    break;
                                }

                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xD5: // PUSH DE
                            {
                                yield return 1; // Internal delay
                                _device.MMU.WriteByte(--Registers.StackPointer, Registers.D);
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, Registers.E);
                                yield return 1;
                                break;
                            }
                        case 0xD6:
                            {
                                var b = FetchByte();
                                yield return 1;
                                _alu.Sub(ref Registers.A, b, false);
                                break;
                            }
                        case 0xD7: // RST 10
                            {
                                yield return 1;

                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = 0x10;
                                break;
                            }
                        case 0xD8: // RET C
                            {
                                yield return 1;
                                if (!Registers.GetFlag(CpuFlags.CarryFlag))
                                {
                                    break;
                                }

                                // Get word from stack in 2 cycles
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8)); // TODO - What happens during the two cycles to set PC?
                                break;
                            }
                        case 0xD9: // RETI
                            {
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8)); // TODO - What happens during the two cycles to set PC?

                                _device.InterruptRegisters.AreInterruptsEnabledGlobally = true;
                                break;
                            }
                        case 0xDA: // JP C, a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                if (!Registers.GetFlag(CpuFlags.CarryFlag))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xDB:
                            _device.Log.Error("Invalid instruction {0} executed", opcode);
                            break;
                        case 0xDC: // CALL C, a16
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                if (!Registers.GetFlag(CpuFlags.CarryFlag))
                                {
                                    break;
                                }

                                yield return 1;// Internal delay

                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xDD:
                            _device.Log.Error("Invalid instruction {0} executed", opcode);
                            break;
                        case 0xDE:
                            {
                                var b = FetchByte();
                                yield return 1;
                                _alu.Sub(ref Registers.A, b, true);
                                break;
                            }
                        case 0xDF: // RST 18
                            {
                                yield return 1;

                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = 0x18;
                                break;
                            }
                        case 0xE0: // LDH (a8),A
                            {
                                var b = FetchByte();
                                yield return 1;
                                _device.MMU.WriteByte((ushort)(0xFF00 + b), Registers.A);
                                yield return 1;
                                break;
                            }
                        case 0xE1: // POP HL
                            {
                                // Get word from stack in 2 cycles
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                // Set word in HL in one cycle
                                Registers.HL = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xE2: // LD (C), A
                            _device.MMU.WriteByte((ushort)(0xFF00 + Registers.C), Registers.A);
                            yield return 1;
                            break;
                        case 0xE3:
                        case 0xE4:
                            _device.Log.Error("Invalid instruction {0} executed", opcode);
                            break;
                        case 0xE5: // PUSH HL
                            {
                                yield return 1; // Internal delay
                                _device.MMU.WriteByte(--Registers.StackPointer, Registers.H);
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, Registers.L);
                                yield return 1;
                                break;
                            }
                        case 0xE6:
                            {
                                var b = FetchByte();
                                yield return 1;
                                _alu.And(ref Registers.A, b);
                                break;
                            }
                        case 0xE7: // RST 20
                            {
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = 0x20;
                                break;
                            }
                        case 0xE8: // ADD SP,r8
                            {
                                var b = (sbyte)FetchByte();
                                yield return 1;
                                yield return 1;
                                yield return 1;
                                _alu.AddSP(b);
                                break;
                            }
                        case 0xE9:
                            Registers.ProgramCounter = Registers.HL;
                            break;
                        case 0xEA: // LD (a16), A
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;

                                _device.MMU.WriteByte((ushort)(b1 | (b2 << 8)), Registers.A);
                                yield return 1;
                                break;
                            }
                        case 0xEB:
                        case 0xEC:
                        case 0xED:
                            _device.Log.Error("Invalid instruction {0} executed", opcode);
                            break;
                        case 0xEE:
                            {
                                var b = FetchByte();
                                yield return 1;
                                _alu.Xor(ref Registers.A, b);
                                break;
                            }
                        case 0xEF: // RST 28
                            {
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = 0x28;
                                break;
                            }
                        case 0xF0: // LDH A, (a8)
                            {
                                var address = (ushort)(0xFF00 + FetchByte());
                                yield return 1;
                                var b = _device.MMU.ReadByte(address);
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0xF1: // POP AF
                            {
                                // Get word from stack in 2 cycles
                                var b1 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;
                                var b2 = _device.MMU.ReadByte(Registers.StackPointer++);
                                yield return 1;

                                // Set word in AF in one cycle
                                Registers.AF = (ushort)(b1 | (b2 << 8));
                                break;
                            }
                        case 0xF2: // LD A, (C)
                            {
                                var b = _device.MMU.ReadByte((ushort)(0xFF00 + Registers.C));
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0xF3: // DI
                            _device.InterruptRegisters.AreInterruptsEnabledGlobally = false;
                            _enableInterruptsCountdown = 0; // Stop counting down to enable interrupts
                            break;
                        case 0xF4:
                            _device.Log.Error("Invalid instruction {0} executed", opcode);
                            break;
                        case 0xF5: // PUSH AF
                            {
                                yield return 1; // Internal delay
                                _device.MMU.WriteByte(--Registers.StackPointer, Registers.A);
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, Registers.F);
                                yield return 1;
                                break;
                            }
                        case 0xF6: // OR d8
                            {
                                var b = FetchByte();
                                yield return 1;
                                _alu.Or(ref Registers.A, b);
                                break;
                            }
                        case 0xF7: // RST 30
                            {
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = 0x30;
                                break;
                            }
                        case 0xF8: // LD HL,SP+r8
                            {
                                var distance = (sbyte)FetchByte();
                                yield return 1;
                                yield return 1;
                                _alu.LoadHLSpPlusR8(distance);
                                break;
                            }
                        case 0xF9: // LD SP, HL
                            yield return 1;
                            Registers.StackPointer = Registers.HL;
                            break;
                        case 0xFA: // LD A,(a16)
                            {
                                var b1 = FetchByte();
                                yield return 1;
                                var b2 = FetchByte();
                                yield return 1;
                                var b = _device.MMU.ReadByte((ushort)(b1 | (b2 << 8)));
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0xFB: // EI
                            if (_enableInterruptsCountdown == 0) // Don't restart waiting one cycle for EI
                            {
                                _enableInterruptsCountdown = 2;
                            }
                            break;
                        case 0xFC:
                        case 0xFD:
                            _device.Log.Error("Invalid instruction {0} executed", opcode);
                            break;
                        case 0xFE: // CP d8
                            {
                                var b = FetchByte();
                                yield return 1;
                                _alu.Cp(Registers.A, b);
                                break;
                            }
                        case 0xFF: // RST 38
                            {
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter >> 8));
                                yield return 1;
                                _device.MMU.WriteByte(--Registers.StackPointer, (byte)(Registers.ProgramCounter & 0xFF));
                                yield return 1;

                                Registers.ProgramCounter = 0x38;
                                break;
                            }
                        default:
                            throw new ArgumentException($"Opcode {opcode} not implemented", nameof(opcode));
                    }

                    // Every opcode takes at least 1 m-cycle
                    IsProcessingInstruction = false;
                    yield return 1;
                }
            }
        }

        /// <summary>
        /// Reset the VM to it's initial state
        /// </summary>
        internal void Reset()
        {
            Registers.Clear();
            _isHalted = false;
            _enableInterruptsCountdown = 0;
        }

        internal byte FetchByte()
        {
            var b = _device.MMU.ReadByte(Registers.ProgramCounter);
            Registers.ProgramCounter = (ushort)(Registers.ProgramCounter + 1);
            return b;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
