using System;
using Gameboy.VM.Interrupts;

namespace Gameboy.VM.CPU
{
    internal class CPU
    {
        private readonly ALU _alu;
        private readonly Device _device;

        private bool _isHalted;
        private bool _isHaltBugState;
        private bool _isStopped;
        private bool _enableInterruptsAfterNextCpuInstruction;

        internal Registers Registers { get; }

        internal CPU(Device device)
        {
            Registers = new Registers();
            _alu = new ALU(this, device.MMU);
            _device = device;
            Reset();
        }

        /// <summary>
        /// Called before each opcode is processed, checks for interrupt
        /// requests and transfer the program into the interrupt handler.
        /// </summary>
        /// <returns>
        /// The total number of cycles taken to check for interrupts.
        /// 0 if no interrupts to handle, 5 (one CALL) if there _are_
        /// interrupts to handle regardless of the number.
        ///
        /// TODO - This is guesswork not evidenced 
        /// </returns>
        internal int CheckForInterrupts()
        {
            if (_device.DMAController.BlockInterrupts()) return 0;

            var cycles = 0;

            // Note that the priority ordering is the same as the bit ordering so this works
            for (var bit = 0; bit < 6; bit++)
            {
                var mask = 1 << bit;
                if ((_device.InterruptRegisters.InterruptEnable & _device.InterruptRegisters.InterruptFlags & mask) == mask)
                {
                    if (_isHalted)
                    {
                        _isHalted = false;
                        cycles += 4;
                    }

                    // TODO - Not really sure what STOP mode actually means, presumably this is correct
                    if (_isStopped)
                    {
                        _isStopped = false;
                        cycles += 4;
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
                        _alu.Call(interrupt.StartingAddress());

                        cycles += 20;
                    }
                }
            }

            return cycles;
        }

        /// <summary>
        /// Emulates a single step of the CPU and returns the number of cycles
        /// that the step took.
        /// </summary>
        /// 
        /// <returns>
        /// Total TCycles this step would have taken on a real gameboy
        /// </returns>
        internal int Step()
        {
            // TODO - All the below cases indicate that the CPU is paused but that the clock is still going, better m-cycle emulation would not need this as the clock would be controlled outside of the CPU
            if (_isHalted || _isStopped || _device.DMAController.HaltCpu()) return 4;

            // EI instruction doesn't enable interrupts until after the _next_ instruction, quirk of hardware
            if (_enableInterruptsAfterNextCpuInstruction)
            {
                _device.InterruptRegisters.AreInterruptsEnabledGlobally = true;
                _enableInterruptsAfterNextCpuInstruction = false;
            }

            var opcode = FetchByte();

            if (_isHaltBugState)
            {
                Registers.ProgramCounter--;
                _isHaltBugState = false;
            }

            return opcode switch
            {
                0x00 => 4, // NOOP
                0x01 => _alu.Load(Register16Bit.BC, FetchWord()) + 4, // LD BC, d16
                0x02 => _device.MMU.WriteByte(Registers.BC, Registers.A), // LD (BC), A
                0x03 => _alu.Increment(Register16Bit.BC), // Increment BC
                0x04 => _alu.Increment(ref Registers.B), // Increment B
                0x05 => _alu.Decrement(ref Registers.B), // Decrement B
                0x06 => _alu.Load(ref Registers.B, FetchByte()) + 4, // LD B, d8
                0x07 => _alu.RotateLeftWithCarryA(), // RLCA
                0x08 => _device.MMU.WriteWord(FetchWord(), Registers.StackPointer) + 4, // LD (a16), SP
                0x09 => _alu.AddHL(Registers.BC), // ADD HL, BC
                0x0A => _alu.Load(ref Registers.A, _device.MMU.ReadByte(Registers.BC)) + 4, // LD A, (BC)
                0x0B => _alu.Decrement(Register16Bit.BC), // DEC BC - Done on 16 bit inc/dec/ld unit, no flag updates
                0x0C => _alu.Increment(ref Registers.C), // INC C
                0x0D => _alu.Decrement(ref Registers.C), // DEC C
                0x0E => _alu.Load(ref Registers.C, FetchByte()) + 4, // LD C, d8
                0x0F => _alu.RotateRightWithCarryA(), // RRCA
                0x10 => Stop(), // STOP 0
                0x11 => _alu.Load(Register16Bit.DE, FetchWord()) + 4, // LD DE, d16
                0x12 => _device.MMU.WriteByte(Registers.DE, Registers.A), // LD (DE), A
                0x13 => _alu.Increment(Register16Bit.DE), // INC DE
                0x14 => _alu.Increment(ref Registers.D), // INC D
                0x15 => _alu.Decrement(ref Registers.D), // DEC D
                0x16 => _alu.Load(ref Registers.D, FetchByte()) + 4, // LD D, d8
                0x17 => _alu.RotateLeftNoCarryA(), // RLA
                0x18 => _alu.JumpRight((sbyte)FetchByte()), // JR r8
                0x19 => _alu.AddHL(Registers.DE), // ADD HL, DE
                0x1A => _alu.Load(ref Registers.A, _device.MMU.ReadByte(Registers.DE)) + 4, // LD A, (DE)
                0x1B => _alu.Decrement(Register16Bit.DE), // DEC DE
                0x1C => _alu.Increment(ref Registers.E), // INC E
                0x1D => _alu.Decrement(ref Registers.E), // DEC E
                0x1E => _alu.Load(ref Registers.E, FetchByte()) + 4, // LD E, d8
                0x1F => _alu.RotateRightNoCarryA(), // RRA
                0x20 => _alu.JumpRightOnFlag(CpuFlags.ZeroFlag, (sbyte)FetchByte(), false), // JR NZ, r8
                0x21 => _alu.Load(Register16Bit.HL, FetchWord()) + 4, // LD HL, d16
                0x22 => _device.MMU.WriteByte(Registers.HLI(), Registers.A), // LD (HL+), A
                0x23 => _alu.Increment(Register16Bit.HL), // INC HL
                0x24 => _alu.Increment(ref Registers.H), // INC H
                0x25 => _alu.Decrement(ref Registers.H), // DEC H
                0x26 => _alu.Load(ref Registers.H, FetchByte()) + 4, // LD H, d8
                0x27 => _alu.DecimalAdjustRegister(ref Registers.A), // DAA
                0x28 => _alu.JumpRightOnFlag(CpuFlags.ZeroFlag, (sbyte)FetchByte(), true), // JR Z, r8
                0x29 => _alu.AddHL(Registers.HL), // ADD HL, HL
                0x2A => _alu.Load(ref Registers.A, _device.MMU.ReadByte(Registers.HLI())) + 4, // LD A, (HL+)
                0x2B => _alu.Decrement(Register16Bit.HL), // DEC HL
                0x2C => _alu.Increment(ref Registers.L), // INC L
                0x2D => _alu.Decrement(ref Registers.L), // DEC L
                0x2E => _alu.Load(ref Registers.L, FetchByte()) + 4, // LD L, d8
                0x2F => _alu.CPL(), // CPL
                0x30 => _alu.JumpRightOnFlag(CpuFlags.CarryFlag, (sbyte)FetchByte(), false), // JR NC, d8
                0x31 => _alu.Load(Register16Bit.SP, FetchWord()) + 4, // LD SP, d16
                0x32 => _device.MMU.WriteByte(Registers.HLD(), Registers.A), // LD (HL-), A
                0x33 => _alu.Increment(Register16Bit.SP), // INC SP
                0x34 => _alu.ActOnMemoryAddress(Registers.HL, _alu.Increment), // INC (HL)
                0x35 => _alu.ActOnMemoryAddress(Registers.HL, _alu.Decrement), // DEC (HL)
                0x36 => _device.MMU.WriteByte(Registers.HL, FetchByte()) + 4, // LD (HL), d8
                0x37 => _alu.SCF(), // SCF
                0x38 => _alu.JumpRightOnFlag(CpuFlags.CarryFlag, (sbyte)FetchByte(), true), // JR C, d8
                0x39 => _alu.AddHL(Registers.StackPointer), // ADD HL, SP
                0x3A => _alu.Load(ref Registers.A, _device.MMU.ReadByte(Registers.HLD())) + 4, // LD A, (HL-)
                0x3B => _alu.Decrement(Register16Bit.SP), // DEC SP
                0x3C => _alu.Increment(ref Registers.A), // INC A
                0x3D => _alu.Decrement(ref Registers.A), // DEC A
                0x3E => _alu.Load(ref Registers.A, FetchByte()) + 4, // LD A, d8
                0x3F => _alu.CCF(), // CCF
                0x40 => 4, // LD B, B
                0x41 => _alu.Load(ref Registers.B, Registers.C), // LD B, C
                0x42 => _alu.Load(ref Registers.B, Registers.D), // LD B, D
                0x43 => _alu.Load(ref Registers.B, Registers.E), // LD B, E
                0x44 => _alu.Load(ref Registers.B, Registers.H), // LD B, H
                0x45 => _alu.Load(ref Registers.B, Registers.L), // LD B, L
                0x46 => _alu.Load(ref Registers.B, _device.MMU.ReadByte(Registers.HL)) + 4, // LD B, (HL)
                0x47 => _alu.Load(ref Registers.B, Registers.A), // LD B, A
                0x48 => _alu.Load(ref Registers.C, Registers.B), // LD C, B
                0x49 => 4, // LD C, C
                0x4A => _alu.Load(ref Registers.C, Registers.D), // LD C, D
                0x4B => _alu.Load(ref Registers.C, Registers.E), // LD C, E
                0x4C => _alu.Load(ref Registers.C, Registers.H), // LD C, H
                0x4D => _alu.Load(ref Registers.C, Registers.L), // LD C, L
                0x4E => _alu.Load(ref Registers.C, _device.MMU.ReadByte(Registers.HL)) + 4, // LD C, (HL)
                0x4F => _alu.Load(ref Registers.C, Registers.A), // LD C, A
                0x50 => _alu.Load(ref Registers.D, Registers.B), // LD D, B
                0x51 => _alu.Load(ref Registers.D, Registers.C), // LD D, C
                0x52 => 4, // LD D, D
                0x53 => _alu.Load(ref Registers.D, Registers.E), // LD D, E
                0x54 => _alu.Load(ref Registers.D, Registers.H), // LD D, H
                0x55 => _alu.Load(ref Registers.D, Registers.L), // LD D, L
                0x56 => _alu.Load(ref Registers.D, _device.MMU.ReadByte(Registers.HL)) + 4, // LD D, (HL)
                0x57 => _alu.Load(ref Registers.D, Registers.A), // LD D, A
                0x58 => _alu.Load(ref Registers.E, Registers.B), // LD E, B
                0x59 => _alu.Load(ref Registers.E, Registers.C), // LD E, C
                0x5A => _alu.Load(ref Registers.E, Registers.D), // LD E, D
                0x5B => 4, // LD E, E
                0x5C => _alu.Load(ref Registers.E, Registers.H), // LD E, H
                0x5D => _alu.Load(ref Registers.E, Registers.L), // LD E, L
                0x5E => _alu.Load(ref Registers.E, _device.MMU.ReadByte(Registers.HL)) + 4, // LD E, (HL)
                0x5F => _alu.Load(ref Registers.E, Registers.A), // LD E, A
                0x60 => _alu.Load(ref Registers.H, Registers.B), // LD H, B
                0x61 => _alu.Load(ref Registers.H, Registers.C), // LD H, C
                0x62 => _alu.Load(ref Registers.H, Registers.D), // LD H, D
                0x63 => _alu.Load(ref Registers.H, Registers.E), // LD H, E
                0x64 => 4, // LD H, H
                0x65 => _alu.Load(ref Registers.H, Registers.L), // LD H, L
                0x66 => _alu.Load(ref Registers.H, _device.MMU.ReadByte(Registers.HL)) + 4, // LD H, (HL)
                0x67 => _alu.Load(ref Registers.H, Registers.A), // LD H, A
                0x68 => _alu.Load(ref Registers.L, Registers.B), // LD L, B
                0x69 => _alu.Load(ref Registers.L, Registers.C), // LD L, C
                0x6A => _alu.Load(ref Registers.L, Registers.D), // LD L, D
                0x6B => _alu.Load(ref Registers.L, Registers.E), // LD L, E
                0x6C => _alu.Load(ref Registers.L, Registers.H), // LD L, H
                0x6D => 4, // LD L, L
                0x6E => _alu.Load(ref Registers.L, _device.MMU.ReadByte(Registers.HL)) + 4, // LD L, (HL)
                0x6F => _alu.Load(ref Registers.L, Registers.A), // LD L, A
                0x70 => _device.MMU.WriteByte(Registers.HL, Registers.B), // LD (HL), B
                0x71 => _device.MMU.WriteByte(Registers.HL, Registers.C), // LD (HL), C
                0x72 => _device.MMU.WriteByte(Registers.HL, Registers.D), // LD (HL), D
                0x73 => _device.MMU.WriteByte(Registers.HL, Registers.E), // LD (HL), E
                0x74 => _device.MMU.WriteByte(Registers.HL, Registers.H), // LD (HL), H
                0x75 => _device.MMU.WriteByte(Registers.HL, Registers.L), // LD (HL), L
                0x76 => Halt(), // HALT
                0x77 => _device.MMU.WriteByte(Registers.HL, Registers.A), // LD (HL), A
                0x78 => _alu.Load(ref Registers.A, Registers.B), // LD A, B
                0x79 => _alu.Load(ref Registers.A, Registers.C), // LD A, C
                0x7A => _alu.Load(ref Registers.A, Registers.D), // LD A, D
                0x7B => _alu.Load(ref Registers.A, Registers.E), // LD A, E
                0x7C => _alu.Load(ref Registers.A, Registers.H), // LD A, H
                0x7D => _alu.Load(ref Registers.A, Registers.L), // LD A, L
                0x7E => _alu.Load(ref Registers.A, _device.MMU.ReadByte(Registers.HL)) + 4, // LD A, (HL)
                0x7F => 4, // LD A, A
                0x80 => _alu.Add(ref Registers.A, Registers.B, false), // ADD A, B
                0x81 => _alu.Add(ref Registers.A, Registers.C, false), // ADD A, C
                0x82 => _alu.Add(ref Registers.A, Registers.D, false), // ADD A, D
                0x83 => _alu.Add(ref Registers.A, Registers.E, false), // ADD A, E
                0x84 => _alu.Add(ref Registers.A, Registers.H, false), // ADD A, H
                0x85 => _alu.Add(ref Registers.A, Registers.L, false), // ADD A, L
                0x86 => _alu.Add(ref Registers.A, _device.MMU.ReadByte(Registers.HL), false) + 4, // ADD A, (HL)
                0x87 => _alu.Add(ref Registers.A, Registers.A, false), // ADD A, A
                0x88 => _alu.Add(ref Registers.A, Registers.B, true), // ADC A, B
                0x89 => _alu.Add(ref Registers.A, Registers.C, true), // ADC A, C
                0x8A => _alu.Add(ref Registers.A, Registers.D, true), // ADC A, D
                0x8B => _alu.Add(ref Registers.A, Registers.E, true), // ADC A, E
                0x8C => _alu.Add(ref Registers.A, Registers.H, true), // ADC A, H
                0x8D => _alu.Add(ref Registers.A, Registers.L, true), // ADC A, L
                0x8E => _alu.Add(ref Registers.A, _device.MMU.ReadByte(Registers.HL), true) + 4, // ADC A, (HL)
                0x8F => _alu.Add(ref Registers.A, Registers.A, true), // ADC A, A
                0x90 => _alu.Sub(ref Registers.A, Registers.B, false), // SUB B
                0x91 => _alu.Sub(ref Registers.A, Registers.C, false), // SUB C
                0x92 => _alu.Sub(ref Registers.A, Registers.D, false), // SUB D
                0x93 => _alu.Sub(ref Registers.A, Registers.E, false), // SUB E
                0x94 => _alu.Sub(ref Registers.A, Registers.H, false), // SUB H
                0x95 => _alu.Sub(ref Registers.A, Registers.L, false), // SUB L
                0x96 => _alu.Sub(ref Registers.A, _device.MMU.ReadByte(Registers.HL), false) + 4, // SUB (HL)
                0x97 => _alu.Sub(ref Registers.A, Registers.A, false), // SUB A
                0x98 => _alu.Sub(ref Registers.A, Registers.B, true), // SBC B
                0x99 => _alu.Sub(ref Registers.A, Registers.C, true), // SBC C
                0x9A => _alu.Sub(ref Registers.A, Registers.D, true), // SBC D
                0x9B => _alu.Sub(ref Registers.A, Registers.E, true), // SBC E
                0x9C => _alu.Sub(ref Registers.A, Registers.H, true), // SBC H
                0x9D => _alu.Sub(ref Registers.A, Registers.L, true), // SBC A, L
                0x9E => _alu.Sub(ref Registers.A, _device.MMU.ReadByte(Registers.HL), true) + 4, // SBC A, (HL)
                0x9F => _alu.Sub(ref Registers.A, Registers.A, true), // SBC A, A
                0xA0 => _alu.And(ref Registers.A, Registers.B), // AND B
                0xA1 => _alu.And(ref Registers.A, Registers.C), // AND C
                0xA2 => _alu.And(ref Registers.A, Registers.D), // AND D
                0xA3 => _alu.And(ref Registers.A, Registers.E), // AND E
                0xA4 => _alu.And(ref Registers.A, Registers.H), // AND H
                0xA5 => _alu.And(ref Registers.A, Registers.L), // AND L
                0xA6 => _alu.And(ref Registers.A, _device.MMU.ReadByte(Registers.HL)) + 4, // AND (HL)
                0xA7 => _alu.And(ref Registers.A, Registers.A), // AND A
                0xA8 => _alu.Xor(ref Registers.A, Registers.B), // XOR B
                0xA9 => _alu.Xor(ref Registers.A, Registers.C), // XOR C
                0xAA => _alu.Xor(ref Registers.A, Registers.D), // XOR D
                0xAB => _alu.Xor(ref Registers.A, Registers.E), // XOR E
                0xAC => _alu.Xor(ref Registers.A, Registers.H), // XOR H
                0xAD => _alu.Xor(ref Registers.A, Registers.L), // XOR L
                0xAE => _alu.Xor(ref Registers.A, _device.MMU.ReadByte(Registers.HL)) + 4, // XOR (HL)
                0xAF => _alu.Xor(ref Registers.A, Registers.A), // XOR A
                0xB0 => _alu.Or(ref Registers.A, Registers.B), // OR B
                0xB1 => _alu.Or(ref Registers.A, Registers.C), // OR C
                0xB2 => _alu.Or(ref Registers.A, Registers.D), // OR D
                0xB3 => _alu.Or(ref Registers.A, Registers.E), // OR E
                0xB4 => _alu.Or(ref Registers.A, Registers.H), // OR H
                0xB5 => _alu.Or(ref Registers.A, Registers.L), // OR L
                0xB6 => _alu.Or(ref Registers.A, _device.MMU.ReadByte(Registers.HL)) + 4, // OR (HL)
                0xB7 => _alu.Or(ref Registers.A, Registers.A), // OR A
                0xB8 => _alu.Cp(Registers.A, Registers.B), // CP B
                0xB9 => _alu.Cp(Registers.A, Registers.C), // CP C
                0xBA => _alu.Cp(Registers.A, Registers.D), // CP D
                0xBB => _alu.Cp(Registers.A, Registers.E), // CP E
                0xBC => _alu.Cp(Registers.A, Registers.H), // CP H
                0xBD => _alu.Cp(Registers.A, Registers.L), // CP L
                0xBE => _alu.Cp(Registers.A, _device.MMU.ReadByte(Registers.HL)) + 4, // CP (HL)
                0xBF => _alu.Cp(Registers.A, Registers.A), // CP A
                0xC0 => _alu.ReturnOnFlag(CpuFlags.ZeroFlag, false), // RET NZ
                0xC1 => _alu.PopFromStackIntoRegister(Register16Bit.BC), // POP BC
                0xC2 => _alu.JumpOnFlag(CpuFlags.ZeroFlag, FetchWord(), false), // JP NZ, a16
                0xC3 => _alu.Jump(FetchWord()), // JP a16
                0xC4 => _alu.CallOnFlag(CpuFlags.ZeroFlag, FetchWord(), false), // CALL NZ, a16
                0xC5 => _alu.PushToStack(Registers.BC), // PUSH BC
                0xC6 => _alu.Add(ref Registers.A, FetchByte(), false) + 4, // ADD A, d8
                0xC7 => _alu.Rst(0), // RST 0
                0xC8 => _alu.ReturnOnFlag(CpuFlags.ZeroFlag, true), // RET Z
                0xC9 => _alu.Return(), // RET
                0xCA => _alu.JumpOnFlag(CpuFlags.ZeroFlag, FetchWord(), true), // JP Z, a16
                0xCB => ProcessOpcodeCB() + 4, // CB prefixed opcodes (add one for retrieving CB opcode)
                0xCC => _alu.CallOnFlag(CpuFlags.ZeroFlag, FetchWord(), true), // CALL Z, a16
                0xCD => _alu.Call(FetchWord()), // CALL a16
                0xCE => _alu.Add(ref Registers.A, FetchByte(), true) + 4, // ADC A, d8
                0xCF => _alu.Rst(0x08), // RST 08h
                0xD0 => _alu.ReturnOnFlag(CpuFlags.CarryFlag, false), // RET NC
                0xD1 => _alu.PopFromStackIntoRegister(Register16Bit.DE), // POP DE
                0xD2 => _alu.JumpOnFlag(CpuFlags.CarryFlag, FetchWord(), false), // JP NC, a16
                0xD3 => 0, // Unused opcode
                0xD4 => _alu.CallOnFlag(CpuFlags.CarryFlag, FetchWord(), false), // CALL NC, a16
                0xD5 => _alu.PushToStack(Registers.DE), // PUSH DE
                0xD6 => _alu.Sub(ref Registers.A, FetchByte(), false) + 4, // SUB d8
                0xD7 => _alu.Rst(0x10), // RST 10
                0xD8 => _alu.ReturnOnFlag(CpuFlags.CarryFlag, true), // RET C
                0xD9 => _alu.ReturnAndEnableInterrupts(_device.InterruptRegisters), // RETI
                0xDA => _alu.JumpOnFlag(CpuFlags.CarryFlag, FetchWord(), true), // JP C, a16
                0xDB => 0, // Unused opcode
                0xDC => _alu.CallOnFlag(CpuFlags.CarryFlag, FetchWord(), true), // CALL C, a16
                0xDD => 0, // Unused opcode - TODO - what actually happens on an unused opcode?
                0xDE => _alu.Sub(ref Registers.A, FetchByte(), true) + 4, // SBC A, d8
                0xDF => _alu.Rst(0x18), // RST 18
                0xE0 => _device.MMU.WriteByte((ushort)(0xFF00 + FetchByte()), Registers.A) + 4, // LDH (a8),A
                0xE1 => _alu.PopFromStackIntoRegister(Register16Bit.HL), // POP HL
                0xE2 => _device.MMU.WriteByte((ushort)(0xFF00 + Registers.C), Registers.A), // LD (C), A
                0xE3 => 0, // Unused opcode
                0xE4 => 0, // Unused opcode
                0xE5 => _alu.PushToStack(Registers.HL), // PUSH HL
                0xE6 => _alu.And(ref Registers.A, FetchByte()) + 4, // AND d8
                0xE7 => _alu.Rst(0x20), // RST 20
                0xE8 => _alu.AddSP((sbyte)FetchByte()), // ADD SP, r8
                0xE9 => _alu.Jump(Registers.HL) - 12, // JP HL
                0xEA => _device.MMU.WriteByte(FetchWord(), Registers.A) + 8, // LD (a16), A
                0xEB => 0, // Unused opcode
                0xEC => 0, // Unused opcode
                0xED => 0, // Unused opcode
                0xEE => _alu.Xor(ref Registers.A, FetchByte()) + 4, // XOR d8
                0xEF => _alu.Rst(0x28), // RST 28
                0xF0 => _alu.Load(ref Registers.A, _device.MMU.ReadByte((ushort)(0xFF00 + FetchByte()))) + 8, // LDH A, (a8)
                0xF1 => _alu.PopFromStackIntoRegister(Register16Bit.AF), // POP AF
                0xF2 => _alu.Load(ref Registers.A, _device.MMU.ReadByte((ushort)(0xFF00 + Registers.C))) + 4, // LD A, (C)
                0xF3 => DisableInterrupts(), // DI
                0xF4 => 0, // Unused opcode
                0xF5 => _alu.PushToStack(Registers.AF), // PUSH AF
                0xF6 => _alu.Or(ref Registers.A, FetchByte()) + 4, // OR d8
                0xF7 => _alu.Rst(0x30), // RST 30
                0xF8 => _alu.LoadHLSpPlusR8((sbyte)FetchByte()), // LD HL, SP+r8
                0xF9 => _alu.Load(Register16Bit.SP, Registers.HL), // LD SP, HL
                0xFA => _alu.Load(ref Registers.A, _device.MMU.ReadByte(FetchWord())) + 12, // LD A, (a16)
                0xFB => EnableInterrupts(), // EI
                0xFC => 0, // Unused opcode
                0xFD => 0, // Unused opcode
                0xFE => _alu.Cp(Registers.A, FetchByte()) + 4, // CP d8
                0xFF => _alu.Rst(0x38), // RST 38
                _ => throw new ArgumentException($"Opcode {opcode} not implemented", nameof(opcode))
            };
        }

        private int ProcessOpcodeCB()
        {
            var subcode = FetchByte();

            return subcode switch
            {
                0x00 => _alu.RotateLeftWithCarry(ref Registers.B), // RLC B
                0x01 => _alu.RotateLeftWithCarry(ref Registers.C), // RLC C
                0x02 => _alu.RotateLeftWithCarry(ref Registers.D), // RLC D
                0x03 => _alu.RotateLeftWithCarry(ref Registers.E), // RLC E
                0x04 => _alu.RotateLeftWithCarry(ref Registers.H), // RLC H
                0x05 => _alu.RotateLeftWithCarry(ref Registers.L), // RLC L
                0x06 => _alu.ActOnMemoryAddress(address: Registers.HL, _alu.RotateLeftWithCarry), // RLC (HL)
                0x07 => _alu.RotateLeftWithCarry(ref Registers.A), // RLC A
                0x08 => _alu.RotateRightWithCarry(ref Registers.B), // RRC B
                0x09 => _alu.RotateRightWithCarry(ref Registers.C), // RRC C
                0x0A => _alu.RotateRightWithCarry(ref Registers.D), // RRC D
                0x0B => _alu.RotateRightWithCarry(ref Registers.E), // RRC E
                0x0C => _alu.RotateRightWithCarry(ref Registers.H), // RRC H
                0x0D => _alu.RotateRightWithCarry(ref Registers.L), // RRC L
                0x0E => _alu.ActOnMemoryAddress(address: Registers.HL, _alu.RotateRightWithCarry), // RLC (HL)
                0x0F => _alu.RotateRightWithCarry(ref Registers.A), // RRC A
                0x10 => _alu.RotateLeftNoCarry(ref Registers.B), // RL B
                0x11 => _alu.RotateLeftNoCarry(ref Registers.C), // RL C
                0x12 => _alu.RotateLeftNoCarry(ref Registers.D), // RL D
                0x13 => _alu.RotateLeftNoCarry(ref Registers.E), // RL E
                0x14 => _alu.RotateLeftNoCarry(ref Registers.H), // RL H
                0x15 => _alu.RotateLeftNoCarry(ref Registers.L), // RL L
                0x16 => _alu.ActOnMemoryAddress(address: Registers.HL, _alu.RotateLeftNoCarry), // RL (HL)
                0x17 => _alu.RotateLeftNoCarry(ref Registers.A), // RL A
                0x18 => _alu.RotateRightNoCarry(ref Registers.B), // RR B
                0x19 => _alu.RotateRightNoCarry(ref Registers.C), // RR C
                0x1A => _alu.RotateRightNoCarry(ref Registers.D), // RR D
                0x1B => _alu.RotateRightNoCarry(ref Registers.E), // RR E
                0x1C => _alu.RotateRightNoCarry(ref Registers.H), // RR H
                0x1D => _alu.RotateRightNoCarry(ref Registers.L), // RR L
                0x1E => _alu.ActOnMemoryAddress(address: Registers.HL, _alu.RotateRightNoCarry), // RR (HL)
                0x1F => _alu.RotateRightNoCarry(ref Registers.A), // RR A
                0x20 => _alu.ShiftLeft(ref Registers.B), // SLA B
                0x21 => _alu.ShiftLeft(ref Registers.C), // SLA C
                0x22 => _alu.ShiftLeft(ref Registers.D), // SLA D
                0x23 => _alu.ShiftLeft(ref Registers.E), // SLA E
                0x24 => _alu.ShiftLeft(ref Registers.H), // SLA H
                0x25 => _alu.ShiftLeft(ref Registers.L), // SLA L
                0x26 => _alu.ActOnMemoryAddress(address: Registers.HL, _alu.ShiftLeft), // SLA (HL)
                0x27 => _alu.ShiftLeft(ref Registers.A), // SLA A
                0x28 => _alu.ShiftRightAdjust(ref Registers.B), // SRA B
                0x29 => _alu.ShiftRightAdjust(ref Registers.C), // SRA C
                0x2A => _alu.ShiftRightAdjust(ref Registers.D), // SRA D
                0x2B => _alu.ShiftRightAdjust(ref Registers.E), // SRA E
                0x2C => _alu.ShiftRightAdjust(ref Registers.H), // SRA H
                0x2D => _alu.ShiftRightAdjust(ref Registers.L), // SRA L
                0x2E => _alu.ActOnMemoryAddress(address: Registers.HL, _alu.ShiftRightAdjust), // SRA (HL)
                0x2F => _alu.ShiftRightAdjust(ref Registers.A), // SRA A
                0x30 => _alu.Swap(ref Registers.B), // SWAP B
                0x31 => _alu.Swap(ref Registers.C), // SWAP C
                0x32 => _alu.Swap(ref Registers.D), // SWAP D
                0x33 => _alu.Swap(ref Registers.E), // SWAP E
                0x34 => _alu.Swap(ref Registers.H), // SWAP H
                0x35 => _alu.Swap(ref Registers.L), // SWAP L
                0x36 => _alu.ActOnMemoryAddress(Registers.HL, _alu.Swap), // SWAP (HL)
                0x37 => _alu.Swap(ref Registers.A), // SWAP A
                0x38 => _alu.ShiftRightLeave(ref Registers.B), // SRL B
                0x39 => _alu.ShiftRightLeave(ref Registers.C), // SRL C
                0x3A => _alu.ShiftRightLeave(ref Registers.D), // SRL D
                0x3B => _alu.ShiftRightLeave(ref Registers.E), // SRL E
                0x3C => _alu.ShiftRightLeave(ref Registers.H), // SRL H
                0x3D => _alu.ShiftRightLeave(ref Registers.L), // SRL L
                0x3E => _alu.ActOnMemoryAddress(address: Registers.HL, _alu.ShiftRightLeave), // SRL (HL)
                0x3F => _alu.ShiftRightLeave(ref Registers.A), // SRL A
                0x40 => _alu.Bit(Registers.B, 0), // BIT 0, B
                0x41 => _alu.Bit(Registers.C, 0), // BIT 0, C
                0x42 => _alu.Bit(Registers.D, 0), // BIT 0, D
                0x43 => _alu.Bit(Registers.E, 0), // BIT 0, E
                0x44 => _alu.Bit(Registers.H, 0), // BIT 0, H
                0x45 => _alu.Bit(Registers.L, 0), // BIT 0, L
                0x46 => _alu.Bit(_device.MMU.ReadByte(Registers.HL), 0) + 4, // BIT 0, (HL)
                0x47 => _alu.Bit(Registers.A, 0), // BIT 0, A
                0x48 => _alu.Bit(Registers.B, 1), // BIT 1, B
                0x49 => _alu.Bit(Registers.C, 1), // BIT 1, C
                0x4A => _alu.Bit(Registers.D, 1), // BIT 1, D
                0x4B => _alu.Bit(Registers.E, 1), // BIT 1, E
                0x4C => _alu.Bit(Registers.H, 1), // BIT 1, H
                0x4D => _alu.Bit(Registers.L, 1), // BIT 1, L
                0x4E => _alu.Bit(_device.MMU.ReadByte(Registers.HL), 1) + 4, // BIT 1, (HL)
                0x4F => _alu.Bit(Registers.A, 1), // BIT 1, A
                0x50 => _alu.Bit(Registers.B, 2), // BIT 2, B
                0x51 => _alu.Bit(Registers.C, 2), // BIT 2, C
                0x52 => _alu.Bit(Registers.D, 2), // BIT 2, D
                0x53 => _alu.Bit(Registers.E, 2), // BIT 2, E
                0x54 => _alu.Bit(Registers.H, 2), // BIT 2, H
                0x55 => _alu.Bit(Registers.L, 2), // BIT 2, L
                0x56 => _alu.Bit(_device.MMU.ReadByte(Registers.HL), 2) + 4, // BIT 2, (HL)
                0x57 => _alu.Bit(Registers.A, 2), // BIT 2, A
                0x58 => _alu.Bit(Registers.B, 3), // BIT 3, B
                0x59 => _alu.Bit(Registers.C, 3), // BIT 3, C
                0x5A => _alu.Bit(Registers.D, 3), // BIT 3, D
                0x5B => _alu.Bit(Registers.E, 3), // BIT 3, E
                0x5C => _alu.Bit(Registers.H, 3), // BIT 3, H
                0x5D => _alu.Bit(Registers.L, 3), // BIT 3, L
                0x5E => _alu.Bit(_device.MMU.ReadByte(Registers.HL), 3) + 4, // BIT 3, (HL)
                0x5F => _alu.Bit(Registers.A, 3), // BIT 3, A
                0x60 => _alu.Bit(Registers.B, 4), // BIT 4, B
                0x61 => _alu.Bit(Registers.C, 4), // BIT 4, C
                0x62 => _alu.Bit(Registers.D, 4), // BIT 4, D
                0x63 => _alu.Bit(Registers.E, 4), // BIT 4, E
                0x64 => _alu.Bit(Registers.H, 4), // BIT 4, H
                0x65 => _alu.Bit(Registers.L, 4), // BIT 4, L
                0x66 => _alu.Bit(_device.MMU.ReadByte(Registers.HL), 4) + 4, // BIT 4, (HL)
                0x67 => _alu.Bit(Registers.A, 4), // BIT 4, A
                0x68 => _alu.Bit(Registers.B, 5), // BIT 5, B
                0x69 => _alu.Bit(Registers.C, 5), // BIT 5, C
                0x6A => _alu.Bit(Registers.D, 5), // BIT 5, D
                0x6B => _alu.Bit(Registers.E, 5), // BIT 5, E
                0x6C => _alu.Bit(Registers.H, 5), // BIT 5, H
                0x6D => _alu.Bit(Registers.L, 5), // BIT 5, L
                0x6E => _alu.Bit(_device.MMU.ReadByte(Registers.HL), 5) + 4, // BIT 5, (HL)
                0x6F => _alu.Bit(Registers.A, 5), // BIT 5, A
                0x70 => _alu.Bit(Registers.B, 6), // BIT 6, B
                0x71 => _alu.Bit(Registers.C, 6), // BIT 6, C
                0x72 => _alu.Bit(Registers.D, 6), // BIT 6, D
                0x73 => _alu.Bit(Registers.E, 6), // BIT 6, E
                0x74 => _alu.Bit(Registers.H, 6), // BIT 6, H
                0x75 => _alu.Bit(Registers.L, 6), // BIT 6, L
                0x76 => _alu.Bit(_device.MMU.ReadByte(Registers.HL), 6) + 4, // BIT 6, (HL)
                0x77 => _alu.Bit(Registers.A, 6), // BIT 6, A
                0x78 => _alu.Bit(Registers.B, 7), // BIT 7, B
                0x79 => _alu.Bit(Registers.C, 7), // BIT 7, C
                0x7A => _alu.Bit(Registers.D, 7), // BIT 7, D
                0x7B => _alu.Bit(Registers.E, 7), // BIT 7, E
                0x7C => _alu.Bit(Registers.H, 7), // BIT 7, H
                0x7D => _alu.Bit(Registers.L, 7), // BIT 7, L
                0x7E => _alu.Bit(_device.MMU.ReadByte(Registers.HL), 7) + 4, // BIT 7, (HL)
                0x7F => _alu.Bit(Registers.A, 7), // BIT 7, A
                0x80 => _alu.Res(ref Registers.B, 0), // RES B, 0
                0x81 => _alu.Res(ref Registers.C, 0), // RES C, 0
                0x82 => _alu.Res(ref Registers.D, 0), // RES D, 0
                0x83 => _alu.Res(ref Registers.E, 0), // RES E, 0
                0x84 => _alu.Res(ref Registers.H, 0), // RES H, 0
                0x85 => _alu.Res(ref Registers.L, 0), // RES L, 0
                0x86 => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Res, 0), // RES (HL), 0
                0x87 => _alu.Res(ref Registers.A, 0), // RES A, 0
                0x88 => _alu.Res(ref Registers.B, 1), // RES B, 1
                0x89 => _alu.Res(ref Registers.C, 1), // RES C, 1
                0x8A => _alu.Res(ref Registers.D, 1), // RES D, 1
                0x8B => _alu.Res(ref Registers.E, 1), // RES E, 1
                0x8C => _alu.Res(ref Registers.H, 1), // RES H, 1
                0x8D => _alu.Res(ref Registers.L, 1), // RES L, 1
                0x8E => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Res, 1), // RES (HL), 1
                0x8F => _alu.Res(ref Registers.A, 1), // RES A, 1
                0x90 => _alu.Res(ref Registers.B, 2), // RES B, 2
                0x91 => _alu.Res(ref Registers.C, 2), // RES C, 2
                0x92 => _alu.Res(ref Registers.D, 2), // RES D, 2
                0x93 => _alu.Res(ref Registers.E, 2), // RES E, 2
                0x94 => _alu.Res(ref Registers.H, 2), // RES H, 2
                0x95 => _alu.Res(ref Registers.L, 2), // RES L, 2
                0x96 => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Res, 2), // RES (HL), 2
                0x97 => _alu.Res(ref Registers.A, 2), // RES A, 2
                0x98 => _alu.Res(ref Registers.B, 3), // RES B, 3
                0x99 => _alu.Res(ref Registers.C, 3), // RES C, 3
                0x9A => _alu.Res(ref Registers.D, 3), // RES D, 3
                0x9B => _alu.Res(ref Registers.E, 3), // RES E, 3
                0x9C => _alu.Res(ref Registers.H, 3), // RES H, 3
                0x9D => _alu.Res(ref Registers.L, 3), // RES L, 3
                0x9E => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Res, 3), // RES (HL), 3
                0x9F => _alu.Res(ref Registers.A, 3), // RES A, 3
                0xA0 => _alu.Res(ref Registers.B, 4), // RES B, 4
                0xA1 => _alu.Res(ref Registers.C, 4), // RES C, 4
                0xA2 => _alu.Res(ref Registers.D, 4), // RES D, 4
                0xA3 => _alu.Res(ref Registers.E, 4), // RES E, 4
                0xA4 => _alu.Res(ref Registers.H, 4), // RES H, 4
                0xA5 => _alu.Res(ref Registers.L, 4), // RES L, 4
                0xA6 => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Res, 4), // RES (HL), 4
                0xA7 => _alu.Res(ref Registers.A, 4), // RES A, 4
                0xA8 => _alu.Res(ref Registers.B, 5), // RES B, 5
                0xA9 => _alu.Res(ref Registers.C, 5), // RES C, 5
                0xAA => _alu.Res(ref Registers.D, 5), // RES D, 5
                0xAB => _alu.Res(ref Registers.E, 5), // RES E, 5
                0xAC => _alu.Res(ref Registers.H, 5), // RES H, 5
                0xAD => _alu.Res(ref Registers.L, 5), // RES L, 5
                0xAE => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Res, 5), // RES (HL), 5
                0xAF => _alu.Res(ref Registers.A, 5), // RES A, 5
                0xB0 => _alu.Res(ref Registers.B, 6), // RES B, 6
                0xB1 => _alu.Res(ref Registers.C, 6), // RES C, 6
                0xB2 => _alu.Res(ref Registers.D, 6), // RES D, 6
                0xB3 => _alu.Res(ref Registers.E, 6), // RES E, 6
                0xB4 => _alu.Res(ref Registers.H, 6), // RES H, 6
                0xB5 => _alu.Res(ref Registers.L, 6), // RES L, 6
                0xB6 => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Res, 6), // RES (HL), 6
                0xB7 => _alu.Res(ref Registers.A, 6), // RES A, 6
                0xB8 => _alu.Res(ref Registers.B, 7), // RES B, 7
                0xB9 => _alu.Res(ref Registers.C, 7), // RES C, 7
                0xBA => _alu.Res(ref Registers.D, 7), // RES D, 7
                0xBB => _alu.Res(ref Registers.E, 7), // RES E, 7
                0xBC => _alu.Res(ref Registers.H, 7), // RES H, 7
                0xBD => _alu.Res(ref Registers.L, 7), // RES L, 7
                0xBE => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Res, 7), // RES (HL), 7
                0xBF => _alu.Res(ref Registers.A, 7), // RES A, 7
                0xC0 => _alu.Set(ref Registers.B, 0), // SET B, 0
                0xC1 => _alu.Set(ref Registers.C, 0), // SET C, 0
                0xC2 => _alu.Set(ref Registers.D, 0), // SET D, 0
                0xC3 => _alu.Set(ref Registers.E, 0), // SET E, 0
                0xC4 => _alu.Set(ref Registers.H, 0), // SET H, 0
                0xC5 => _alu.Set(ref Registers.L, 0), // SET L, 0
                0xC6 => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Set, 0), // SET (HL), 0
                0xC7 => _alu.Set(ref Registers.A, 0), // SET A, 0
                0xC8 => _alu.Set(ref Registers.B, 1), // SET B, 1
                0xC9 => _alu.Set(ref Registers.C, 1), // SET C, 1
                0xCA => _alu.Set(ref Registers.D, 1), // SET D, 1
                0xCB => _alu.Set(ref Registers.E, 1), // SET E, 1
                0xCC => _alu.Set(ref Registers.H, 1), // SET H, 1
                0xCD => _alu.Set(ref Registers.L, 1), // SET L, 1
                0xCE => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Set, 1), // SET (HL), 1
                0xCF => _alu.Set(ref Registers.A, 1), // SET A, 1
                0xD0 => _alu.Set(ref Registers.B, 2), // SET B, 2
                0xD1 => _alu.Set(ref Registers.C, 2), // SET C, 2
                0xD2 => _alu.Set(ref Registers.D, 2), // SET D, 2
                0xD3 => _alu.Set(ref Registers.E, 2), // SET E, 2
                0xD4 => _alu.Set(ref Registers.H, 2), // SET H, 2
                0xD5 => _alu.Set(ref Registers.L, 2), // SET L, 2
                0xD6 => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Set, 2), // SET (HL), 2
                0xD7 => _alu.Set(ref Registers.A, 2), // SET A, 2
                0xD8 => _alu.Set(ref Registers.B, 3), // SET B, 3
                0xD9 => _alu.Set(ref Registers.C, 3), // SET C, 3
                0xDA => _alu.Set(ref Registers.D, 3), // SET D, 3
                0xDB => _alu.Set(ref Registers.E, 3), // SET E, 3
                0xDC => _alu.Set(ref Registers.H, 3), // SET H, 3
                0xDD => _alu.Set(ref Registers.L, 3), // SET L, 3
                0xDE => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Set, 3), // SET (HL), 3
                0xDF => _alu.Set(ref Registers.A, 3), // SET A, 3
                0xE0 => _alu.Set(ref Registers.B, 4), // SET B, 4
                0xE1 => _alu.Set(ref Registers.C, 4), // SET C, 4
                0xE2 => _alu.Set(ref Registers.D, 4), // SET D, 4
                0xE3 => _alu.Set(ref Registers.E, 4), // SET E, 4
                0xE4 => _alu.Set(ref Registers.H, 4), // SET H, 4
                0xE5 => _alu.Set(ref Registers.L, 4), // SET L, 4
                0xE6 => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Set, 4), // SET (HL), 4
                0xE7 => _alu.Set(ref Registers.A, 4), // SET A, 4
                0xE8 => _alu.Set(ref Registers.B, 5), // SET B, 5
                0xE9 => _alu.Set(ref Registers.C, 5), // SET C, 5
                0xEA => _alu.Set(ref Registers.D, 5), // SET D, 5
                0xEB => _alu.Set(ref Registers.E, 5), // SET E, 5
                0xEC => _alu.Set(ref Registers.H, 5), // SET H, 5
                0xED => _alu.Set(ref Registers.L, 5), // SET L, 5
                0xEE => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Set, 5), // SET (HL), 5
                0xEF => _alu.Set(ref Registers.A, 5), // SET A, 5
                0xF0 => _alu.Set(ref Registers.B, 6), // SET B, 6
                0xF1 => _alu.Set(ref Registers.C, 6), // SET C, 6
                0xF2 => _alu.Set(ref Registers.D, 6), // SET D, 6
                0xF3 => _alu.Set(ref Registers.E, 6), // SET E, 6
                0xF4 => _alu.Set(ref Registers.H, 6), // SET H, 6
                0xF5 => _alu.Set(ref Registers.L, 6), // SET L, 6
                0xF6 => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Set, 6), // SET (HL), 6
                0xF7 => _alu.Set(ref Registers.A, 6), // SET A, 6
                0xF8 => _alu.Set(ref Registers.B, 7), // SET B, 7
                0xF9 => _alu.Set(ref Registers.C, 7), // SET C, 7
                0xFA => _alu.Set(ref Registers.D, 7), // SET D, 7
                0xFB => _alu.Set(ref Registers.E, 7), // SET E, 7
                0xFC => _alu.Set(ref Registers.H, 7), // SET H, 7
                0xFD => _alu.Set(ref Registers.L, 7), // SET L, 7
                0xFE => _alu.ActOnMemoryAddressOneParam(Registers.HL, _alu.Set, 7), // SET (HL), 7
                0xFF => _alu.Set(ref Registers.A, 7), // SET A, 7
                _ => throw new ArgumentException($"CB sub opcode {subcode} not implemented", nameof(subcode))
            };
        }

        private int EnableInterrupts()
        {
            _enableInterruptsAfterNextCpuInstruction = true;

            return 4;
        }

        private int DisableInterrupts()
        {
            _device.InterruptRegisters.AreInterruptsEnabledGlobally = false;
            return 4;
        }

        /// <summary>
        /// Reset the VM to it's initial state
        /// </summary>
        internal void Reset()
        {
            Registers.Clear();
            _isHalted = false;
            _enableInterruptsAfterNextCpuInstruction = false;
        }

        private byte FetchByte()
        {
            var b = _device.MMU.ReadByte(Registers.ProgramCounter);
            Registers.ProgramCounter = (ushort)(Registers.ProgramCounter + 1);
            return b;
        }

        private ushort FetchWord()
        {
            var w = _device.MMU.ReadWord(Registers.ProgramCounter);
            Registers.ProgramCounter = (ushort)(Registers.ProgramCounter + 2);
            return w;
        }

        private int Halt()
        {
            _isHalted = true;
            _isHaltBugState = false;

            // HALT bug behaviour (don't increment PC on next read) happens
            if (!_device.InterruptRegisters.AreInterruptsEnabledGlobally &&
                (_device.InterruptRegisters.InterruptEnable & _device.InterruptRegisters.InterruptFlags & 0x1F) != 0)
            {
                _isHaltBugState = true;
            }
            return 4;
        }

        private int Stop()
        {
            var extraByte = FetchByte();

            if (extraByte != 0x0)
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
                _isStopped = true;
            }

            return 8;
        }
    }
}
