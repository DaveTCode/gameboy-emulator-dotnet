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

        private byte Increment(byte register)
        {
            var result = (byte)((register + 1) & 0xFF);
            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (register & 0x0F) + 1 > 0x0F);
            return result;
        }

        private byte Decrement(byte register)
        {
            var result = (byte)((register - 1) & 0xFF);
            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, true);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (register & 0xF) == 0xF);
            return result;
        }

        private ushort Increment(ushort register) => (ushort)((register + 1) & 0xFFFF);

        private ushort Decrement(ushort register) => (ushort)((register - 1) & 0xFFFF);

        private ushort Add(ushort register1, ushort register2)
        {
            var result = (register1 + register2) & 0xFFFF;
            _registers.SetFlag(FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (register1 & 0xFFF) > (result & 0xFFF));
            _registers.SetFlag(FRegisterFlags.CarryFlag, result > 0xFFFF);
            return (ushort)(result & 0xFFFF);
        }

        private byte RotateLeftWithCarry(byte register)
        {
            var result = (byte)(((register << 1) | (register >> 7)) & 0xFF);
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, register >= 0x7F);
            return result;
        }

        private byte RotateLeftNoCarry(byte register)
        {
            var result = (byte)((register << 1) | (_registers.GetFlag(FRegisterFlags.CarryFlag) ? 0x1 : 0x0));
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, register >= 0x7F);
            return result;
        }

        private byte RotateRightWithCarry(byte register)
        {
            var result = (byte)((register >> 1) | ((register & 1) << 7));
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, (register & 0x1) == 0x1);
            return result;
        }

        private byte RotateRightNoCarry(byte register)
        {
            var result = (byte)((register >> 1) | (_registers.GetFlag(FRegisterFlags.CarryFlag) ? 0x80 : 0x0));
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, (register & 0x1) == 0x1);
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

        private byte DecimalAdjustRegister(byte registerValue)
        {
            var adjust = 0;

            if (_registers.GetFlag(FRegisterFlags.CarryFlag) ||
                (_registers.GetFlag(FRegisterFlags.SubtractFlag) && registerValue > 0x99)) adjust |= 0x60;
            if (_registers.GetFlag(FRegisterFlags.HalfCarryFlag) ||
                (_registers.GetFlag(FRegisterFlags.SubtractFlag) && (registerValue & 0x0F) > 0x09)) adjust |= 0x06;

            var result = (byte)((registerValue + adjust) & 0xFF);

            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0x0);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, adjust > 0x60);
            return result;
        }
    }
}
