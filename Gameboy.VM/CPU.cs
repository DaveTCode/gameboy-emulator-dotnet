using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Gameboy.VM.Cpu.Tests")]
namespace Gameboy.VM
{
    internal class CPU
    {
        private bool _inStopMode = false;
        private readonly MMU _mmu = new MMU();
        private Registers _registers;

        internal CPU()
        {
            Reset();
        }

        /// <summary>
        /// Emulates a single step of the CPU and returns the number of cycles
        /// that the step took.
        /// </summary>
        /// 
        /// <returns>
        /// Total CPU cycles this step would have taken on a real gameboy
        /// </returns>
        internal byte Step()
        {
            var opcode = FetchByte();

            switch(opcode)
            {
                case 0x00:
                    return 1;
                case 0x01:
                    _registers.BC = FetchWord();
                    return 3;
                case 0x02:
                    _mmu.WriteByte(_registers.DE, _registers.A);
                    return 2;
                case 0x03: // Increment BC - Done on 16 bit inc/dec/ld unit, no flag updates
                    _registers.BC = Increment(_registers.BC);
                    return 2;
                case 0x04: // Increment B
                    _registers.B = Increment(_registers.B);
                    return 1;
                case 0x05: // Decrement B
                    _registers.B = Decrement(_registers.B);
                    return 1;
                case 0x06: // LD B, d8
                    _registers.B = FetchByte();
                    return 2;
                case 0x07: // RLCA
                    _registers.A = RotateLeftWithCarry(_registers.A);
                    return 1;
                case 0x08: // LD (a16), SP
                    var address = FetchWord();
                    _mmu.WriteByte(address, (byte)(_registers.StackPointer & 0xFF));
                    _mmu.WriteByte((ushort)((address + 1) & 0xFFFF), (byte)(_registers.StackPointer >> 8));
                    _registers.StackPointer = FetchWord();
                    return 5;
                case 0x09: // ADD HL, BC
                    _registers.HL = Add(_registers.HL, _registers.BC);
                    return 2;
                case 0x0A: // LD A, (BC)
                    _registers.A = _mmu.ReadByte(_registers.BC);
                    return 2;
                case 0x0B: // DEC BC - Done on 16 bit inc/dec/ld unit, no flag updates
                    _registers.BC = Decrement(_registers.BC);
                    return 2;
                case 0x0C: // INC C
                    _registers.C = Increment(_registers.C);
                    return 1;
                case 0x0D: // DEC C
                    _registers.C = Decrement(_registers.C);
                    return 1;
                case 0x0E: // LD C, d8
                    _registers.C = FetchByte();
                    return 2;
                case 0x0F: // RRCA
                    _registers.A = RotateRightWithCarry(_registers.A);
                    return 1;
                case 0x10: // STOP 0
                    // TODO - programming manual suggests what this does depends on lots of things!
                    var _ = FetchByte();
                    _inStopMode = true;
                    return 2;
                case 0x11: // LD DE, d16
                    _registers.DE = FetchWord();
                    return 3;
                case 0x12: // LD (DE), A
                    _mmu.WriteByte(_registers.DE, _registers.A);
                    return 2;
                case 0x13: // INC DE
                    _registers.DE = Increment(_registers.DE);
                    return 2;
                case 0x14: // INC D
                    _registers.D = Increment(_registers.D);
                    return 1;
                case 0x15: // DEC D
                    _registers.D = Decrement(_registers.D);
                    return 1;
                case 0x16: // LD D, d8
                    _registers.D = FetchByte();
                    return 2;
                case 0x17: // RLA
                    _registers.A = RotateLeftNoCarry(_registers.A);
                    return 1;
                case 0x18: // JR r8
                    _registers.ProgramCounter = (ushort)((_registers.ProgramCounter + FetchByte()) & 0xFFFF);
                    return 3;
                case 0x19: // ADD HL, DE
                    _registers.HL = Add(_registers.HL, _registers.DE);
                    return 2;
                case 0x1A: // LD A, (DE)
                    _registers.A = _mmu.ReadByte(_registers.DE);
                    return 2;
                case 0x1B: // DEC DE
                    _registers.DE = Decrement(_registers.DE);
                    return 2;
                case 0x1C: // INC E
                    _registers.E = Increment(_registers.E);
                    return 1;
                case 0x1D: // DEC E
                    _registers.E = Decrement(_registers.E);
                    return 1;
                case 0x1E: // LD E, d8
                    _registers.E = FetchByte();
                    return 2;
                case 0x1F: // RRA
                    _registers.A = RotateRightNoCarry(_registers.A);
                    return 1;
                case 0x20: // JR NZ, r8
                    return JumpOnFlag(FRegisterFlags.ZeroFlag, false);
                case 0x21: // LD HL, d16
                    _registers.HL = FetchWord();
                    return 3;
                case 0x22: // LD (HL+), A
                    _mmu.WriteByte(_registers.HL, _registers.A);
                    _registers.HL = Increment(_registers.HL);
                    return 2;
                case 0x23: // INC HL
                    _registers.HL = Increment(_registers.HL);
                    return 1;
                case 0x24: // INC H
                    _registers.H = Increment(_registers.H);
                    return 1;
                case 0x25: // DEC H
                    _registers.H = Decrement(_registers.H);
                    return 1;
                case 0x26: // LD H, d8
                    _registers.H = FetchByte();
                    return 2;
                case 0x27: // DAA
                    _registers.A = DecimalAdjustRegister(_registers.A);
                    return 1;
                case 0x28: // JR Z, r8
                    return JumpOnFlag(FRegisterFlags.ZeroFlag, true);
                case 0x29: // ADD HL, HL
                    _registers.HL = Add(_registers.HL, _registers.HL);
                    return 2;
                case 0x2A: // LD A, (HL+)
                    _registers.A = _mmu.ReadByte(_registers.HL);
                    _registers.HL = Increment(_registers.HL);
                    return 2;
                case 0x2B: // DEC HL
                    _registers.HL = Decrement(_registers.HL);
                    return 1;
                case 0x2C: // INC L
                    _registers.L = Increment(_registers.L);
                    return 1;
                case 0x2D: // DEC L
                    _registers.L = Decrement(_registers.L);
                    return 1;
                case 0x2E: // LD L, d8
                    _registers.L = FetchByte();
                    return 2;
                case 0x2F: // CPL
                    _registers.A ^= _registers.A;
                    _registers.SetFlag(FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, true);
                    return 1;
                case 0x30: // JR NC, d8
                    return JumpOnFlag(FRegisterFlags.CarryFlag, false);
                case 0x31: // LD SP, d16
                    _registers.StackPointer = FetchWord();
                    return 3;
                case 0x32: // LD (HL-), A
                    _mmu.WriteByte(_registers.HL, _registers.A);
                    _registers.HL = Decrement(_registers.HL);
                    return 2;
                case 0x33: // INC SP
                    _registers.StackPointer = Increment(_registers.StackPointer);
                    return 2;
                case 0x34: // INC (HL)
                    _mmu.WriteByte(_registers.HL, Increment(_mmu.ReadByte(_registers.HL)));
                    return 3;
                case 0x35: // DEC (HL)
                    _mmu.WriteByte(_registers.HL, Decrement(_mmu.ReadByte(_registers.HL)));
                    return 3;
                case 0x36: // LD (HL), d8
                    _mmu.WriteByte(_registers.HL, FetchByte());
                    return 3;
                case 0x37: // SCF
                    _registers.SetFlag(FRegisterFlags.CarryFlag, true);
                    _registers.SetFlag(FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
                    return 1;
                case 0x38: // JR C, d8
                    return JumpOnFlag(FRegisterFlags.CarryFlag, true);
                case 0x39: // ADD HL, SP
                    _registers.HL = Add(_registers.HL, _registers.StackPointer);
                    return 2;
                case 0x3A: // LD A, (HL-)
                    _registers.A = _mmu.ReadByte(_registers.HL);
                    _registers.HL = Decrement(_registers.HL);
                    return 2;
                case 0x3B: // DEC SP
                    _registers.StackPointer = Decrement(_registers.StackPointer);
                    return 2;
                case 0x3C: // INC A
                    _registers.A = Increment(_registers.A);
                    return 1;
                case 0x3D: // DEC A
                    _registers.A = Decrement(_registers.A);
                    return 1;
                case 0x3E: // LD A, d8
                    _registers.A = FetchByte();
                    return 2;
                case 0x3F: // CCF
                    _registers.SetFlag(FRegisterFlags.CarryFlag, !_registers.GetFlag(FRegisterFlags.CarryFlag));
                    _registers.SetFlag(FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
                    return 1;
                case 0x40: // LD B, B
                    return 1;
                case 0x41: // LD B, C
                    _registers.B = _registers.C;
                    return 1;
                case 0x42: // LD B, D
                    _registers.B = _registers.D;
                    return 1;
                case 0x43: // LD B, E
                    _registers.B = _registers.E;
                    return 1;
                case 0x44: // LD B, H
                    _registers.B = _registers.H;
                    return 1;
                case 0x45: // LD B, L
                    _registers.B = _registers.L;
                    return 1;
                case 0x46: // LD B, (HL)
                    _registers.B = _mmu.ReadByte(_registers.HL);
                    return 2;
                case 0x47: // LD B, A
                    _registers.B = _registers.A;
                    return 1;
                case 0x48: // LD C, B
                    _registers.C = _registers.B;
                    return 1;
                case 0x49: // LD C, C
                    return 1;
                case 0x4A: // LD C, D
                    _registers.C = _registers.D;
                    return 1;
                case 0x4B: // LD C, E
                    _registers.C = _registers.E;
                    return 1;
                case 0x4C: // LD C, H
                    _registers.C = _registers.H;
                    return 1;
                case 0x4D: // LD C, L
                    _registers.C = _registers.L;
                    return 1;
                case 0x4E: // LD C, (HL)
                    _registers.C = _mmu.ReadByte(_registers.HL);
                    return 2;
                case 0x4F: // LD C, A
                    _registers.C = _registers.A;
                    return 1;
                case 0x50: // LD D, B
                    _registers.D = _registers.B;
                    return 1;
                case 0x51: // LD D, C
                    _registers.D = _registers.C;
                    return 1;
                case 0x52: // LD D, D
                    return 1;
                case 0x53: // LD D, E
                    _registers.D = _registers.E;
                    return 1;
                case 0x54: // LD D, H
                    _registers.D = _registers.H;
                    return 1;
                case 0x55: // LD D, L
                    _registers.D = _registers.L;
                    return 1;
                case 0x56: // LD D, (HL)
                    _registers.D = _mmu.ReadByte(_registers.HL);
                    return 2;
                case 0x57: // LD D, A
                    _registers.D = _registers.A;
                    return 1;
                case 0x58: // LD E, B
                    _registers.E = _registers.B;
                    return 1;
                case 0x59: // LD E, C
                    _registers.E = _registers.C;
                    return 1;
                case 0x5A: // LD E, D
                    _registers.E = _registers.D;
                    return 1;
                case 0x5B: // LD E, E
                    return 1;
                case 0x5C: // LD E, H
                    _registers.E = _registers.H;
                    return 1;
                case 0x5D: // LD E, L
                    _registers.E = _registers.L;
                    return 1;
                case 0x5E: // LD E, (HL)
                    _registers.E = _mmu.ReadByte(_registers.HL);
                    return 2;
                case 0x5F: // LD E, A
                    _registers.E = _registers.A;
                    return 1;
                case 0x60: // LD H, B
                    _registers.H = _registers.B;
                    return 1;
                case 0x61: // LD H, C
                    _registers.H = _registers.C;
                    return 1;
                case 0x62: // LD H, D
                    _registers.H = _registers.D;
                    return 1;
                case 0x63: // LD H, E
                    _registers.H = _registers.E;
                    return 1;
                case 0x64: // LD H, H
                    return 1;
                case 0x65: // LD H, L
                    _registers.H = _registers.L;
                    return 1;
                case 0x66: // LD H, (HL)
                    _registers.H = _mmu.ReadByte(_registers.HL);
                    return 2;
                case 0x67: // LD H, A
                    _registers.H = _registers.A;
                    return 1;
                case 0x68: // LD L, B
                    _registers.L = _registers.B;
                    return 1;
                case 0x69: // LD L, C
                    _registers.L = _registers.C;
                    return 1;
                case 0x6A: // LD L, D
                    _registers.L = _registers.D;
                    return 1;
                case 0x6B: // LD L, E
                    _registers.L = _registers.E;
                    return 1;
                case 0x6C: // LD L, H
                    _registers.L = _registers.H;
                    return 1;
                case 0x6D: // LD L, L
                    return 1;
                case 0x6E: // LD L, (HL)
                    _registers.L = _mmu.ReadByte(_registers.HL);
                    return 2;
                case 0x6F: // LD L, A
                    _registers.L = _registers.A;
                    return 1;
                case 0x70: // LD (HL), B
                    _mmu.WriteByte(_registers.HL, _registers.B);
                    return 2;
                case 0x71: // LD (HL), C
                    _mmu.WriteByte(_registers.HL, _registers.C);
                    return 2;
                case 0x72: // LD (HL), D
                    _mmu.WriteByte(_registers.HL, _registers.D);
                    return 2;
                case 0x73: // LD (HL), E
                    _mmu.WriteByte(_registers.HL, _registers.E);
                    return 2;
                case 0x74: // LD (HL), H
                    _mmu.WriteByte(_registers.HL, _registers.H);
                    return 2;
                case 0x75: // LD (HL), L
                    _mmu.WriteByte(_registers.HL, _registers.L);
                    return 2;
                case 0x76: // HALT
                    // TODO - What does HALT do?
                    return 1;
                case 0x77: // LD (HL), A
                    _mmu.WriteByte(_registers.HL, _registers.A);
                    return 2;
                case 0x78: // LD A, B
                    _registers.A = _registers.B;
                    return 1;
                case 0x79: // LD A, C
                    _registers.A = _registers.C;
                    return 1;
                case 0x7A: // LD A, D
                    _registers.A = _registers.D;
                    return 1;
                case 0x7B: // LD A, E
                    _registers.A = _registers.E;
                    return 1;
                case 0x7C: // LD A, H
                    _registers.A = _registers.H;
                    return 1;
                case 0x7D: // LD A, L
                    _registers.A = _registers.L;
                    return 1;
                case 0x7E: // LD A, (HL)
                    _registers.A = _mmu.ReadByte(_registers.HL);
                    return 2;
                case 0x7F: // LD A, A
                    return 1;
                case 0x80: // ADD A, B
                    _registers.A = Add(_registers.A, _registers.B, false);
                    return 1;
                case 0x81: // ADD A, C
                    _registers.A = Add(_registers.A, _registers.C, false);
                    return 1;
                case 0x82: // ADD A, D
                    _registers.A = Add(_registers.A, _registers.D, false);
                    return 1;
                case 0x83: // ADD A, E
                    _registers.A = Add(_registers.A, _registers.E, false);
                    return 1;
                case 0x84: // ADD A, H
                    _registers.A = Add(_registers.A, _registers.H, false);
                    return 1;
                case 0x85: // ADD A, L
                    _registers.A = Add(_registers.A, _registers.L, false);
                    return 1;
                case 0x86: // ADD A, (HL)
                    _registers.A = Add(_registers.A, _mmu.ReadByte(_registers.HL), false);
                    return 2;
                case 0x87: // ADD A, A
                    _registers.A = Add(_registers.A, _registers.A, false);
                    return 1;
                case 0x88: // ADC A, B
                    _registers.A = Add(_registers.A, _registers.B, true);
                    return 1;
                case 0x89: // ADC A, C
                    _registers.A = Add(_registers.A, _registers.C, true);
                    return 1;
                case 0x8A: // ADC A, D
                    _registers.A = Add(_registers.A, _registers.D, true);
                    return 1;
                case 0x8B: // ADC A, E
                    _registers.A = Add(_registers.A, _registers.E, true);
                    return 1;
                case 0x8C: // ADC A, H
                    _registers.A = Add(_registers.A, _registers.H, true);
                    return 1;
                case 0x8D: // ADC A, L
                    _registers.A = Add(_registers.A, _registers.L, true);
                    return 1;
                case 0x8E: // ADC A, (HL)
                    _registers.A = Add(_registers.A, _mmu.ReadByte(_registers.HL), true);
                    return 1;
                case 0x8F: // ADC A, A
                    _registers.A = Add(_registers.A, _registers.A, true);
                    return 1;
                case 0x90: // SUB B
                    _registers.A = Sub(_registers.A, _registers.B, false);
                    return 1;
                case 0x91: // SUB C
                    _registers.A = Sub(_registers.A, _registers.C, false);
                    return 1;
                case 0x92: // SUB D
                    _registers.A = Sub(_registers.A, _registers.D, false);
                    return 1;
                case 0x93: // SUB E
                    _registers.A = Sub(_registers.A, _registers.E, false);
                    return 1;
                case 0x94: // SUB H
                    _registers.A = Sub(_registers.A, _registers.H, false);
                    return 1;
                case 0x95: // SUB L
                    _registers.A = Sub(_registers.A, _registers.L, false);
                    return 1;
                case 0x96: // SUB (HL)
                    _registers.A = Sub(_registers.A, _mmu.ReadByte(_registers.HL), false);
                    return 1;
                case 0x97: // SUB A
                    _registers.A = Sub(_registers.A, _registers.A, false);
                    return 1;
                case 0x98: // SBC B
                    _registers.A = Sub(_registers.A, _registers.B, true);
                    return 1;
                case 0x99: // SBC C
                    _registers.A = Sub(_registers.A, _registers.C, true);
                    return 1;
                case 0x9A: // SBC D
                    _registers.A = Sub(_registers.A, _registers.D, true);
                    return 1;
                case 0x9B: // SBC E
                    _registers.A = Sub(_registers.A, _registers.E, true);
                    return 1;
                case 0x9C: // SBC H
                    _registers.A = Sub(_registers.A, _registers.H, true);
                    return 1;
                case 0x9D: // SBC A, L
                    _registers.A = Sub(_registers.A, _registers.L, true);
                    return 1;
                case 0x9E: // SBC A, (HL)
                    _registers.A = Sub(_registers.A, _mmu.ReadByte(_registers.HL), true);
                    return 1;
                case 0x9F: // SBC A, A
                    _registers.A = Sub(_registers.A, _registers.A, true);
                    return 1;
                default:
                    throw new NotImplementedException($"Opcode {opcode} not implemented");
            }
        }

        /// <summary>
        /// Reset the VM to it's initial state
        /// </summary>
        internal void Reset()
        {
            _mmu.Clear();
            _registers = new Registers();
        }

        private byte FetchByte()
        {
            var b = _mmu.ReadByte(_registers.ProgramCounter);
            _registers.ProgramCounter = (ushort)((_registers.ProgramCounter + 1) & 0xFFFF);
            return b;
        }

        private ushort FetchWord()
        {
            var w = _mmu.ReadWord(_registers.ProgramCounter);
            _registers.ProgramCounter = (ushort)((_registers.ProgramCounter + 2) & 0xFFFF);
            return w;
        }

        #region 8bit Arithemetic functions
        private byte Increment(byte a)
        {
            var result = (byte)((a + 1) & 0xFF);
            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (a & 0x0F) + 1 > 0x0F);
            return result;
        }

        private byte Decrement(byte a)
        {
            var result = (byte)((a - 1) & 0xFF);
            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, true);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (a & 0xF) == 0xF);
            return result;
        }

        private byte Add(byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && _registers.GetFlag(FRegisterFlags.CarryFlag) ? 1 : 0;
            var result = a + b + c;
            _registers.SetFlag(FRegisterFlags.ZeroFlag, (result & 0xFF) == 0x0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (((a & 0xF) + (b & 0xF) + (c & 0xF)) & 0x10) == 0x10);
            _registers.SetFlag(FRegisterFlags.CarryFlag, result > 0xFF);
            return (byte)(result & 0xFF);
        }

        private byte Sub(byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && _registers.GetFlag(FRegisterFlags.CarryFlag) ? 1 : 0;
            var result = a - b - c;
            _registers.SetFlag(FRegisterFlags.ZeroFlag, (result & 0xFF) == 0x0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, true);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (a & 0x0F) < (b & 0x0F) + c);
            _registers.SetFlag(FRegisterFlags.CarryFlag, result < 0);
            return (byte)(result & 0xFF);
        }
        #endregion

        private ushort Increment(ushort a) => (ushort)((a + 1) & 0xFFFF);

        private ushort Decrement(ushort a) => (ushort)((a - 1) & 0xFFFF);

        private ushort Add(ushort a, ushort b)
        {
            var result = a + b;
            _registers.SetFlag(FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (a & 0xFFF) > (result & 0xFFF));
            _registers.SetFlag(FRegisterFlags.CarryFlag, result > 0xFFFF);
            return (ushort)(result & 0xFFFF);
        }

        private byte RotateLeftWithCarry(byte a)
        {
            var result = (byte)(((a << 1) | (a >> 7)) & 0xFF);
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, a >= 0x7F);
            return result;
        }

        private byte RotateLeftNoCarry(byte a)
        {
            var result = (byte)((a << 1) | (_registers.GetFlag(FRegisterFlags.CarryFlag) ? 0x1 : 0x0));
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, a >= 0x7F);
            return result;
        }

        private byte RotateRightWithCarry(byte a)
        {
            var result = (byte)((a >> 1) | ((a & 1) << 7));
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, (a & 0x1) == 0x1);
            return result;
        }

        private byte RotateRightNoCarry(byte a)
        {
            var result = (byte)((a >> 1) | (_registers.GetFlag(FRegisterFlags.CarryFlag) ? 0x80 : 0x0));
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, (a & 0x1) == 0x1);
            return result;
        }

        private byte JumpOnFlag(FRegisterFlags flag, bool isSet)
        {
            var jumpSteps = FetchByte();
            if (_registers.GetFlag(flag) == isSet)
            {
                return 2;
            }
            _registers.ProgramCounter = (ushort)((_registers.ProgramCounter + jumpSteps) & 0xFFFF);
            return 3;
        }

        private byte DecimalAdjustRegister(byte a)
        {
            var adjust = 0;

            if (_registers.GetFlag(FRegisterFlags.CarryFlag) ||
                (_registers.GetFlag(FRegisterFlags.SubtractFlag) && a > 0x99)) adjust |= 0x60;
            if (_registers.GetFlag(FRegisterFlags.HalfCarryFlag) ||
                (_registers.GetFlag(FRegisterFlags.SubtractFlag) && (a & 0x0F) > 0x09)) adjust |= 0x06;

            var result = (byte)((a + adjust) & 0xFF);

            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0x0);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, adjust > 0x60);
            return result;
        }
    }
}
