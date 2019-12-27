using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Gameboy.VM.Cpu.Tests")]
namespace Gameboy.VM
{
    internal class CPU
    {
        private bool _inStopMode;
        private readonly MMU _mmu;
        private readonly ALU _alu;
        private readonly Registers _registers = new Registers();

        internal CPU()
        {
            _mmu = new MMU();
            _alu = new ALU(_mmu, _registers);
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
        internal int Step()
        {
            var opcode = FetchByte();

            return opcode switch
            {
                0x00 => 1, // NOOP
                0x01 => _alu.Load(Register16Bit.BC, FetchWord()), // LD BC, d16
                0x02 => _mmu.WriteByte(_registers.BC, _registers.A), // LD (BC), A
                0x03 => _alu.Increment(Register16Bit.BC), // Increment BC
                0x04 => _alu.Increment(ref _registers.B), // Increment B
                0x05 => _alu.Decrement(ref _registers.B), // Decrement B
                0x06 => (_alu.Load(ref _registers.B, FetchByte()) + 1), // LD B, d8
                0x07 => _alu.RotateLeftWithCarry(ref _registers.A), // RLCA
                0x08 => (_mmu.WriteWord(FetchWord(), _registers.StackPointer) + 1), // LD (a16), SP
                0x09 => _alu.Add(Register16Bit.HL, _registers.HL, _registers.BC), // ADD HL, BC
                0x0A => (_alu.Load(ref _registers.A, _mmu.ReadByte(_registers.BC)) + 1), // LD A, (BC)
                0x0B => _alu.Decrement(Register16Bit.BC), // DEC BC - Done on 16 bit inc/dec/ld unit, no flag updates
                0x0C => _alu.Increment(ref _registers.C), // INC C
                0x0D => _alu.Decrement(ref _registers.C), // DEC C
                0x0E => (_alu.Load(ref _registers.C, FetchByte()) + 1), // LD C, d8
                0x0F => _alu.RotateRightWithCarry(ref _registers.A), // RRCA
                0x10 => Stop(), // STOP 0
                0x11 => _alu.Load(Register16Bit.DE, FetchWord()), // LD DE, d16
                0x12 => _mmu.WriteByte(_registers.DE, _registers.A), // LD (DE), A
                0x13 => _alu.Increment(Register16Bit.DE), // INC DE
                0x14 => _alu.Increment(ref _registers.D), // INC D
                0x15 => _alu.Decrement(ref _registers.D), // DEC D
                0x16 => (_alu.Load(ref _registers.D, FetchByte()) + 1), // LD D, d8
                0x17 => _alu.RotateLeftNoCarry(ref _registers.A), // RLA
                0x18 => _alu.JumpRight((sbyte)FetchByte()), // JR r8
                0x19 => _alu.Add(Register16Bit.HL, _registers.HL, _registers.DE), // ADD HL, DE
                0x1A => _alu.Load(ref _registers.A, _mmu.ReadByte(_registers.DE)), // LD A, (DE)
                0x1B => _alu.Decrement(Register16Bit.DE), // DEC DE
                0x1C => _alu.Increment(ref _registers.E), // INC E
                0x1D => _alu.Decrement(ref _registers.E), // DEC E
                0x1E => (_alu.Load(ref _registers.E, FetchByte()) + 1), // LD E, d8
                0x1F => _alu.RotateRightNoCarry(ref _registers.A), // RRA
                0x20 => _alu.JumpRightOnFlag(FRegisterFlags.ZeroFlag, (sbyte)FetchByte(), false), // JR NZ, r8
                0x21 => (_alu.Load(Register16Bit.HL, FetchWord()) + 1), // LD HL, d16
                0x22 => _mmu.WriteByte(_registers.HLI(), _registers.A), // LD (HL+), A
                0x23 => _alu.Increment(Register16Bit.HL), // INC HL
                0x24 => _alu.Increment(ref _registers.H), // INC H
                0x25 => _alu.Decrement(ref _registers.H), // DEC H
                0x26 => (_alu.Load(ref _registers.H, FetchByte()) + 1), // LD H, d8
                0x27 => _alu.DecimalAdjustRegister(ref _registers.A), // DAA
                0x28 => _alu.JumpRightOnFlag(FRegisterFlags.ZeroFlag, (sbyte)FetchByte(), true), // JR Z, r8
                0x29 => _alu.Add(Register16Bit.HL, _registers.HL, _registers.HL), // ADD HL, HL
                0x2A => _alu.Load(ref _registers.A, _mmu.ReadByte(_registers.HLI())), // LD A, (HL+)
                0x2B => _alu.Decrement(Register16Bit.HL), // DEC HL
                0x2C => _alu.Increment(ref _registers.L), // INC L
                0x2D => _alu.Decrement(ref _registers.L), // DEC L
                0x2E => (_alu.Load(ref _registers.L, FetchByte()) + 1), // LD L, d8
                0x2F => _alu.CPL(), // CPL
                0x30 => _alu.JumpRightOnFlag(FRegisterFlags.CarryFlag, (sbyte)FetchByte(), false), // JR NC, d8
                0x31 => (_alu.Load(Register16Bit.SP, FetchWord()) + 1), // LD SP, d16
                0x32 => _mmu.WriteByte(_registers.HLD(), _registers.A), // LD (HL-), A
                0x33 => _alu.Increment(Register16Bit.SP), // INC SP
                0x34 => _alu.IncrementMemoryValue(_registers.HL), // INC (HL)
                0x35 => _alu.DecrementMemoryValue(_registers.HL), // DEC (HL)
                0x36 => (_mmu.WriteByte(_registers.HL, FetchByte()) + 1), // LD (HL), d8
                0x37 => _alu.SCF(), // SCF
                0x38 => _alu.JumpRightOnFlag(FRegisterFlags.CarryFlag, (sbyte)FetchByte(), true), // JR C, d8
                0x39 => _alu.Add(Register16Bit.HL, _registers.HL, _registers.StackPointer), // ADD HL, SP
                0x3A => _alu.Load(ref _registers.A, _mmu.ReadByte(_registers.HLD())), // LD A, (HL-)
                0x3B => _alu.Decrement(Register16Bit.SP), // DEC SP
                0x3C => _alu.Increment(ref _registers.A), // INC A
                0x3D => _alu.Decrement(ref _registers.A), // DEC A
                0x3E => (_alu.Load(ref _registers.A, FetchByte()) + 1), // LD A, d8
                0x3F => _alu.CCF(), // CCF
                0x40 => 1, // LD B, B
                0x41 => _alu.Load(ref _registers.B, _registers.C), // LD B, C
                0x42 => _alu.Load(ref _registers.B, _registers.D), // LD B, D
                0x43 => _alu.Load(ref _registers.B, _registers.E), // LD B, E
                0x44 => _alu.Load(ref _registers.B, _registers.H), // LD B, H
                0x45 => _alu.Load(ref _registers.B, _registers.L), // LD B, L
                0x46 => (_alu.Load(ref _registers.B, _mmu.ReadByte(_registers.HL)) + 1), // LD B, (HL)
                0x47 => _alu.Load(ref _registers.B, _registers.A), // LD B, A
                0x48 => _alu.Load(ref _registers.C, _registers.B), // LD C, B
                0x49 => 1, // LD C, C
                0x4A => _alu.Load(ref _registers.C, _registers.D), // LD C, D
                0x4B => _alu.Load(ref _registers.C, _registers.E), // LD C, E
                0x4C => _alu.Load(ref _registers.C, _registers.H), // LD C, H
                0x4D => _alu.Load(ref _registers.C, _registers.L), // LD C, L
                0x4E => (_alu.Load(ref _registers.C, _mmu.ReadByte(_registers.HL)) + 1), // LD C, (HL)
                0x4F => _alu.Load(ref _registers.C, _registers.A), // LD C, A
                0x50 => _alu.Load(ref _registers.D, _registers.B), // LD D, B
                0x51 => _alu.Load(ref _registers.D, _registers.C), // LD D, C
                0x52 => 1, // LD D, D
                0x53 => _alu.Load(ref _registers.D, _registers.E), // LD D, E
                0x54 => _alu.Load(ref _registers.D, _registers.H), // LD D, H
                0x55 => _alu.Load(ref _registers.D, _registers.L), // LD D, L
                0x56 => (_alu.Load(ref _registers.D, _mmu.ReadByte(_registers.HL)) + 1), // LD D, (HL)
                0x57 => _alu.Load(ref _registers.D, _registers.A), // LD D, A
                0x58 => _alu.Load(ref _registers.E, _registers.B), // LD E, B
                0x59 => _alu.Load(ref _registers.E, _registers.C), // LD E, C
                0x5A => _alu.Load(ref _registers.E, _registers.D), // LD E, D
                0x5B => 1, // LD E, E
                0x5C => _alu.Load(ref _registers.E, _registers.H), // LD E, H
                0x5D => _alu.Load(ref _registers.E, _registers.L), // LD E, L
                0x5E => (_alu.Load(ref _registers.E, _mmu.ReadByte(_registers.HL)) + 1), // LD E, (HL)
                0x5F => _alu.Load(ref _registers.E, _registers.A), // LD E, A
                0x60 => _alu.Load(ref _registers.H, _registers.B), // LD H, B
                0x61 => _alu.Load(ref _registers.H, _registers.C), // LD H, C
                0x62 => _alu.Load(ref _registers.H, _registers.D), // LD H, D
                0x63 => _alu.Load(ref _registers.H, _registers.E), // LD H, E
                0x64 => 1, // LD H, H
                0x65 => _alu.Load(ref _registers.H, _registers.L), // LD H, L
                0x66 => (_alu.Load(ref _registers.H, _mmu.ReadByte(_registers.HL)) + 1), // LD H, (HL)
                0x67 => _alu.Load(ref _registers.H, _registers.A), // LD H, A
                0x68 => _alu.Load(ref _registers.L, _registers.B), // LD L, B
                0x69 => _alu.Load(ref _registers.L, _registers.C), // LD L, C
                0x6A => _alu.Load(ref _registers.L, _registers.D), // LD L, D
                0x6B => _alu.Load(ref _registers.L, _registers.E), // LD L, E
                0x6C => _alu.Load(ref _registers.L, _registers.H), // LD L, H
                0x6D => 1, // LD L, L
                0x6E => (_alu.Load(ref _registers.L, _mmu.ReadByte(_registers.HL)) + 1), // LD L, (HL)
                0x6F => _alu.Load(ref _registers.L, _registers.A), // LD L, A
                0x70 => _mmu.WriteByte(_registers.HL, _registers.B), // LD (HL), B
                0x71 => _mmu.WriteByte(_registers.HL, _registers.C), // LD (HL), C
                0x72 => _mmu.WriteByte(_registers.HL, _registers.D), // LD (HL), D
                0x73 => _mmu.WriteByte(_registers.HL, _registers.E), // LD (HL), E
                0x74 => _mmu.WriteByte(_registers.HL, _registers.H), // LD (HL), H
                0x75 => _mmu.WriteByte(_registers.HL, _registers.L), // LD (HL), L
                0x76 => Halt(), // HALT
                0x77 => _mmu.WriteByte(_registers.HL, _registers.A), // LD (HL), A
                0x78 => _alu.Load(ref _registers.A, _registers.B), // LD A, B
                0x79 => _alu.Load(ref _registers.A, _registers.C), // LD A, C
                0x7A => _alu.Load(ref _registers.A, _registers.D), // LD A, D
                0x7B => _alu.Load(ref _registers.A, _registers.E), // LD A, E
                0x7C => _alu.Load(ref _registers.A, _registers.H), // LD A, H
                0x7D => _alu.Load(ref _registers.A, _registers.L), // LD A, L
                0x7E => (_alu.Load(ref _registers.A, _mmu.ReadByte(_registers.HL)) + 1), // LD A, (HL)
                0x7F => 1, // LD A, A
                0x80 => _alu.Add(ref _registers.A, _registers.B, false), // ADD A, B
                0x81 => _alu.Add(ref _registers.A, _registers.C, false), // ADD A, C
                0x82 => _alu.Add(ref _registers.A, _registers.D, false), // ADD A, D
                0x83 => _alu.Add(ref _registers.A, _registers.E, false), // ADD A, E
                0x84 => _alu.Add(ref _registers.A, _registers.H, false), // ADD A, H
                0x85 => _alu.Add(ref _registers.A, _registers.L, false), // ADD A, L
                0x86 => _alu.Add(ref _registers.A, _mmu.ReadByte(_registers.HL), false), // ADD A, (HL)
                0x87 => _alu.Add(ref _registers.A, _registers.A, false), // ADD A, A
                0x88 => _alu.Add(ref _registers.A, _registers.B, true), // ADC A, B
                0x89 => _alu.Add(ref _registers.A, _registers.C, true), // ADC A, C
                0x8A => _alu.Add(ref _registers.A, _registers.D, true), // ADC A, D
                0x8B => _alu.Add(ref _registers.A, _registers.E, true), // ADC A, E
                0x8C => _alu.Add(ref _registers.A, _registers.H, true), // ADC A, H
                0x8D => _alu.Add(ref _registers.A, _registers.L, true), // ADC A, L
                0x8E => _alu.Add(ref _registers.A, _mmu.ReadByte(_registers.HL), true), // ADC A, (HL)
                0x8F => _alu.Add(ref _registers.A, _registers.A, true), // ADC A, A
                0x90 => _alu.Sub(ref _registers.A, _registers.B, false), // SUB B
                0x91 => _alu.Sub(ref _registers.A, _registers.C, false), // SUB C
                0x92 => _alu.Sub(ref _registers.A, _registers.D, false), // SUB D
                0x93 => _alu.Sub(ref _registers.A, _registers.E, false), // SUB E
                0x94 => _alu.Sub(ref _registers.A, _registers.H, false), // SUB H
                0x95 => _alu.Sub(ref _registers.A, _registers.L, false), // SUB L
                0x96 => _alu.Sub(ref _registers.A, _mmu.ReadByte(_registers.HL), false), // SUB (HL)
                0x97 => _alu.Sub(ref _registers.A, _registers.A, false), // SUB A
                0x98 => _alu.Sub(ref _registers.A, _registers.B, true), // SBC B
                0x99 => _alu.Sub(ref _registers.A, _registers.C, true), // SBC C
                0x9A => _alu.Sub(ref _registers.A, _registers.D, true), // SBC D
                0x9B => _alu.Sub(ref _registers.A, _registers.E, true), // SBC E
                0x9C => _alu.Sub(ref _registers.A, _registers.H, true), // SBC H
                0x9D => _alu.Sub(ref _registers.A, _registers.L, true), // SBC A, L
                0x9E => _alu.Sub(ref _registers.A, _mmu.ReadByte(_registers.HL), true), // SBC A, (HL)
                0x9F => _alu.Sub(ref _registers.A, _registers.A, true), // SBC A, A
                0xA0 => _alu.And(ref _registers.A, _registers.B), // AND B
                0xA1 => _alu.And(ref _registers.A, _registers.C), // AND C
                0xA2 => _alu.And(ref _registers.A, _registers.D), // AND D
                0xA3 => _alu.And(ref _registers.A, _registers.E), // AND E
                0xA4 => _alu.And(ref _registers.A, _registers.H), // AND H
                0xA5 => _alu.And(ref _registers.A, _registers.L), // AND L
                0xA6 => _alu.And(ref _registers.A, _mmu.ReadByte(_registers.HL)), // AND (HL)
                0xA7 => _alu.And(ref _registers.A, _registers.B), // AND A
                0xA8 => _alu.Xor(ref _registers.A, _registers.B), // XOR B
                0xA9 => _alu.Xor(ref _registers.A, _registers.C), // XOR C
                0xAA => _alu.Xor(ref _registers.A, _registers.D), // XOR D
                0xAB => _alu.Xor(ref _registers.A, _registers.E), // XOR E
                0xAC => _alu.Xor(ref _registers.A, _registers.H), // XOR H
                0xAD => _alu.Xor(ref _registers.A, _registers.L), // XOR L
                0xAE => _alu.Xor(ref _registers.A, _mmu.ReadByte(_registers.HL)), // XOR (HL)
                0xAF => _alu.Xor(ref _registers.A, _registers.B), // XOR A
                0xB0 => _alu.Or(ref _registers.A, _registers.B), // OR B
                0xB1 => _alu.Or(ref _registers.A, _registers.C), // OR C
                0xB2 => _alu.Or(ref _registers.A, _registers.D), // OR D
                0xB3 => _alu.Or(ref _registers.A, _registers.E), // OR E
                0xB4 => _alu.Or(ref _registers.A, _registers.H), // OR H
                0xB5 => _alu.Or(ref _registers.A, _registers.L), // OR L
                0xB6 => _alu.Or(ref _registers.A, _mmu.ReadByte(_registers.HL)), // OR (HL)
                0xB7 => _alu.Or(ref _registers.A, _registers.B), // OR A
                0xB8 => _alu.Cp(_registers.A, _registers.B), // CP B
                0xB9 => _alu.Cp(_registers.A, _registers.C), // CP C
                0xBA => _alu.Cp(_registers.A, _registers.D), // CP D
                0xBB => _alu.Cp(_registers.A, _registers.E), // CP E
                0xBC => _alu.Cp(_registers.A, _registers.H), // CP H
                0xBD => _alu.Cp(_registers.A, _registers.L), // CP L
                0xBE => _alu.Cp(_registers.A, _mmu.ReadByte(_registers.HL)), // CP (HL)
                0xBF => _alu.Cp(_registers.A, _registers.A), // CP A
                0xC0 => !_registers.GetFlag(FRegisterFlags.ZeroFlag) ? _alu.Jump(PopFromStack()) + 1 : 2, // RET NZ
                0xC1 => (_alu.Load(Register16Bit.BC, PopFromStack()) + 1), // POP BC
                0xC2 => _alu.JumpOnFlag(FRegisterFlags.ZeroFlag, FetchWord(), false), // JP NZ, a16
                0xC3 => _alu.Jump(FetchWord()), // JP a16
                0xC4 => !_registers.GetFlag(FRegisterFlags.ZeroFlag) ? _alu.Jump(FetchWord()) + PushToStack(_registers.ProgramCounter) - 2: 3, // CALL NZ, a16
                0xC5 => PushToStack(_registers.BC), // PUSH BC
                0xC6 => _alu.Add(ref _registers.A, FetchByte(), false) + 1, // ADD A, d8
                0xC7 => Rst(0), // RST 0
                0xC8 => _registers.GetFlag(FRegisterFlags.ZeroFlag) ? _alu.Jump(PopFromStack()) + 1 : 2, // RET Z
                0xC9 => _alu.Jump(PopFromStack()), // RET
                0xCA => _alu.JumpOnFlag(FRegisterFlags.ZeroFlag, FetchWord(), true), // JP Z, a16
                0xCB => 1, // TODO - Prefix CB
                0xCC => _registers.GetFlag(FRegisterFlags.ZeroFlag) ? _alu.Jump(FetchWord()) + PushToStack(_registers.ProgramCounter) - 2 : 3, // CALL Z, a16
                0xCD => _alu.Jump(FetchWord()) + PushToStack(_registers.ProgramCounter) - 2, // CALL a16
                0xCE => _alu.Add(ref _registers.A, FetchByte(), true) + 1, // ADC A, d8
                0xCF => Rst(0x08), // RST 08h
                0xD0 => !_registers.GetFlag(FRegisterFlags.CarryFlag) ? _alu.Jump(PopFromStack()) + 1 : 2, // RET NC
                0xD1 => (_alu.Load(Register16Bit.DE, PopFromStack()) + 1), // POP DE
                0xD2 => _alu.JumpOnFlag(FRegisterFlags.CarryFlag, FetchWord(), false), // JP NC, a16
                0xD3 => 0, // Unused opcode
                0xD4 => !_registers.GetFlag(FRegisterFlags.CarryFlag) ? _alu.Jump(FetchWord()) + PushToStack(_registers.ProgramCounter) - 2 : 3, // CALL NC, a16
                0xD5 => PushToStack(_registers.DE), // PUSH DE
                0xD6 => _alu.Sub(ref _registers.A, FetchByte(), false) + 1, // SUB d8
                0xD7 => Rst(0x10), // RST 10
                0xD8 => _registers.GetFlag(FRegisterFlags.CarryFlag) ? _alu.Jump(PopFromStack()) + 1 : 2, // RET C
                0xD9 => _alu.ReturnAndEnableInterrupts(PopFromStack()), // RETI
                0xDA => _alu.JumpOnFlag(FRegisterFlags.CarryFlag, FetchWord(), true), // JP C, a16
                0xDB => 0, // Unused opcode
                0xDC => _registers.GetFlag(FRegisterFlags.CarryFlag) ? _alu.Jump(FetchWord()) + PushToStack(_registers.ProgramCounter) - 2 : 3, // CALL C, a16
                0xDD => 0, // Unused opcode - TODO - what actually happens on an unused opcode?
                0xDE => _alu.Sub(ref _registers.A, FetchByte(), true) + 1, // SBC A, d8
                0xDF => Rst(0x18), // RST 18
                0xE0 => _mmu.WriteByte((ushort)(0xFF00 + FetchByte()), _registers.A) + 1, // LDH (a8),A
                0xE1 => (_alu.Load(Register16Bit.HL, PopFromStack()) + 1), // POP HL
                0xE2 => _mmu.WriteByte((ushort)(0xFF00 + _registers.C), _registers.A), // LD (C), A
                0xE3 => 0, // Unused opcode
                0xE4 => 0, // Unused opcode
                0xE5 => PushToStack(_registers.HL), // PUSH HL
                0xE6 => _alu.And(ref _registers.A, FetchByte()) + 1, // AND d8
                0xE7 => Rst(0x20), // RST 20
                0xE8 => _alu.Add(Register16Bit.SP, _registers.StackPointer, (sbyte)FetchByte()), // ADD SP, r8
                0xE9 => _alu.Jump(_mmu.ReadWord(_registers.HL)), // JP (HL)
                0xEA => _mmu.WriteByte(FetchWord(), _registers.A) + 2, // LD (a16), A
                0xEB => 0, // Unused opcode
                0xEC => 0, // Unused opcode
                0xED => 0, // Unused opcode
                0xEE => _alu.Xor(ref _registers.A, FetchByte()) + 1, // XOR d8
                0xEF => Rst(0x28), // RST 28
                0xF0 => _alu.Load(ref _registers.A, _mmu.ReadByte((ushort)(0xFF00 + FetchByte()))) + 2, // LDH A, (a8)
                0xF1 => (_alu.Load(Register16Bit.AF, PopFromStack()) + 1), // POP AF
                0xF2 => _alu.Load(ref _registers.A, _mmu.ReadByte((ushort)(0xFF00 + _registers.C))) + 1, // LD A, (C)
                0xF3 => DisableInterrupts(), // DI
                0xF4 => 0, // Unused opcode
                0xF5 => PushToStack(_registers.AF), // PUSH AF
                0xF6 => _alu.And(ref _registers.A, FetchByte()) + 1, // OR d8
                0xF7 => Rst(0x30), // RST 30
                0xF8 => _alu.Load(Register16Bit.HL, _mmu.ReadWord((ushort)((_registers.StackPointer + (sbyte)FetchByte()) & 0xFFFF))) + 1, // LD HL, SP+r8
                0xF9 => _alu.Load(Register16Bit.SP, _registers.HL), // LD SP, HL
                0xFA => _alu.Load(ref _registers.A, _mmu.ReadByte(FetchWord())) + 2, // LD A, (a16)
                0xFB => EnableInterrupts(), // EI
                0xFC => 0, // Unused opcode
                0xFD => 0, // Unused opcode
                0xFE => _alu.Cp(_registers.A, FetchByte()) + 1, // CP d8
                0xFF => Rst(0x28), // RST 38
                _ => throw new NotImplementedException($"Opcode {opcode} not implemented")
            };
        }

        private int EnableInterrupts()
        {
            throw new NotImplementedException();
        }

        private int DisableInterrupts()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reset the VM to it's initial state
        /// </summary>
        internal void Reset()
        {
            _mmu.Clear();
            _registers.Clear();
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

        private ushort PopFromStack()
        {
            var w = _mmu.ReadWord(_registers.StackPointer);
            _registers.StackPointer = (ushort)((_registers.StackPointer + 2) & 0xFFFF);
            return w;
        }

        private int PushToStack(ushort value)
        {
            _registers.StackPointer = (ushort)((_registers.StackPointer - 2) & 0xFFFF);
            return _mmu.WriteWord(_registers.StackPointer, value);
        }

        private int Rst(byte page)
        {
            PushToStack(_registers.ProgramCounter);
            _registers.ProgramCounter = page;
            return 4;
        }

        private int Halt()
        {
            // TODO
            return 1;
        }

        private int Stop()
        {
            // TODO
            var _ = FetchByte();
            _inStopMode = true;
            return 2;
        }
    }
}
