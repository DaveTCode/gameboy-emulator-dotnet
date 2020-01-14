﻿using System;
using Gameboy.VM.Interrupts;
using Serilog;

namespace Gameboy.VM.CPU
{
    internal class ALU
    {
        private readonly ILogger _log;
        private readonly CPU _cpu;
        private readonly MMU _mmu;

        internal ALU(in ILogger log, in CPU cpu, in MMU mmu)
        {
            _log = log;
            _cpu = cpu;
            _mmu = mmu;
        }


        internal delegate int ActOnByteReference(ref byte a);
        internal delegate int ActOnByteReferenceOneParam(ref byte a, int param);

        internal int ActOnMemoryAddress(ushort address, ActOnByteReference func)
        {
            var a = _mmu.ReadByte(address);
            var result = func(ref a);
            result += _mmu.WriteByte(address, a);
            return result;
        }

        internal int ActOnMemoryAddressOneParam(ushort address, ActOnByteReferenceOneParam func, int param)
        {
            var a = _mmu.ReadByte(address);
            var result = func(ref a, param);
            result += _mmu.WriteByte(address, a);
            return result;
        }

        #region 8 bit Load/Store/Move

        // ReSharper disable once RedundantAssignment
        internal int Load(ref byte a, byte b)
        {
            a = b;
            return 1;
        }

        #endregion

        #region 8 bit Rotate/Shift/Bit

        internal int RotateLeftWithCarryA()
        {
            RotateLeftWithCarry(ref _cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, false);
            return 1;
        }

        internal int RotateLeftWithCarry(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, a > 0x7F);
            a = (byte)(((a << 1) | (a >> 7)) & 0xFF);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
            return 1;
        }

        internal int RotateLeftNoCarryA()
        {
            RotateLeftNoCarry(ref _cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, false);
            return 1;
        }

        internal int RotateLeftNoCarry(ref byte a)
        {
            var setCarry = (a & 0x80) == 0x80;
            a = (byte)((a << 1) | (_cpu.Registers.GetFlag(CpuFlags.CarryFlag) ? 0x1 : 0x0));
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, setCarry);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
            return 1;
        }

        internal int RotateRightWithCarryA()
        {
            RotateRightWithCarry(ref _cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, false);
            return 1;
        }

        internal int RotateRightWithCarry(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, (a & 0x1) == 0x1);
            a = (byte)((a >> 1) | ((a & 1) << 7));
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
            return 1;
        }

        internal int RotateRightNoCarryA()
        {
            RotateRightNoCarry(ref _cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, false);
            return 1;
        }

        internal int RotateRightNoCarry(ref byte a)
        {
            var setCarry = (a & 0x1) == 0x1;
            a = (byte)((a >> 1) | (_cpu.Registers.GetFlag(CpuFlags.CarryFlag) ? 0x80 : 0x0));
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, setCarry);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
            return 1;
        }

        internal int ShiftLeft(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, (a & 0x80) == 0x80);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            a = (byte)(a << 1);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0);
            return 1;
        }

        internal int ShiftRightAdjust(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, (a & 0x01) == 0x01);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            a = (byte)((a >> 1) | (a & 0x80));
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0);
            return 1;
        }

        internal int ShiftRightLeave(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, (a & 0x01) == 0x01);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            a = (byte)(a >> 1);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0);
            return 1;
        }

        internal int Swap(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag | CpuFlags.CarryFlag, false);
            a = (byte)((a >> 4) | (a << 4));
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0);
            return 1;
        }

        internal int Bit(byte a, int bit)
        {
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, true);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, (a & (1 << bit)) == 0);
            return 1;
        }

        internal int Res(ref byte a, int bit)
        {
            a = (byte)(a & ~(1 << bit));
            return 1;
        }

        internal int Set(ref byte a, int bit)
        {
            a = (byte)(a | (1 << bit));
            return 1;
        }

        #endregion

        #region 8 bit arithemetic

        internal int Increment(ref byte a)
        {
            var result = (byte)((a + 1) & 0xFF);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (a & 0x0F) + 1 > 0x0F);
            a = result;
            return 1;
        }

        internal int Decrement(ref byte a)
        {
            var result = (byte)((a - 1) & 0xFF);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, true);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (a & 0x0F) < 0x01);
            a = result;
            return 1;
        }

        internal int Add(ref byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && _cpu.Registers.GetFlag(CpuFlags.CarryFlag) ? 1 : 0;
            var result = a + b + c;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, (result & 0xFF) == 0x0);
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (((a & 0xF) + (b & 0xF) + (c & 0xF)) & 0x10) == 0x10);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, result > 0xFF);
            a = (byte)(result & 0xFF);
            return 1;
        }

        internal int Sub(ref byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && _cpu.Registers.GetFlag(CpuFlags.CarryFlag) ? 1 : 0;
            var result = a - b - c;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, (result & 0xFF) == 0x0);
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, true);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (a & 0x0F) < (b & 0x0F) + c);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, result < 0);
            a = (byte)(result & 0xFF);
            return 1;
        }

        internal int And(ref byte a, byte b)
        {
            var result = a & b;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, true);
            a = (byte)result;
            return 1;
        }

        internal int Xor(ref byte a, byte b)
        {
            var result = a ^ b;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag | CpuFlags.SubtractFlag | CpuFlags.HalfCarryFlag, false);
            a = (byte)result;
            return 1;
        }

        internal int Or(ref byte a, byte b)
        {
            var result = a | b;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag | CpuFlags.SubtractFlag | CpuFlags.HalfCarryFlag, false);
            a = (byte)result;
            return 1;
        }

        internal int Cp(byte a, byte b)
        {
            Sub(ref a, b, false);
            return 1;
        }

        internal int DecimalAdjustRegister(ref byte a)
        {
            int tmp = a;

            if (!_cpu.Registers.GetFlag(CpuFlags.SubtractFlag))
            {
                if (_cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag) || (tmp & 0x0F) > 9)
                    tmp += 0x06;
                if (_cpu.Registers.GetFlag(CpuFlags.CarryFlag) || tmp > 0x9F)
                    tmp += 0x60;
            }
            else
            {
                if (_cpu.Registers.GetFlag(CpuFlags.HalfCarryFlag))
                {
                    tmp -= 0x06;
                    if (!_cpu.Registers.GetFlag(CpuFlags.CarryFlag))
                        tmp &= 0xFF;
                }
                if (_cpu.Registers.GetFlag(CpuFlags.CarryFlag))
                    tmp -= 0x60;
            }

            a = (byte)(tmp & 0xFF);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
            if (tmp > 0xFF) _cpu.Registers.SetFlag(CpuFlags.CarryFlag, true); // Note that we don't unset Carry, only ever set it
                _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, false);

            return 1;
        }

        internal int CCF()
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, !_cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            return 1;
        }

        internal int SCF()
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, true);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            return 1;
        }

        internal int CPL()
        {
            _cpu.Registers.A = (byte)(~_cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, true);
            return 1;
        }

        #endregion

        #region 16 bit arithemetic

        /// <summary>
        /// Increment a 16 bit register.
        /// Note: Done on 16 bit inc/dec/ld unit, no flag updates
        /// </summary>
        /// <remarks>
        /// This is horrific because we can't pass a ref to a property (obvs).
        /// </remarks>
        internal int Increment(Register16Bit register)
        {
            switch (register)
            {
                case Register16Bit.AF:
                    _cpu.Registers.AF = (ushort)((_cpu.Registers.AF + 1) & 0xFFFF);
                    break;
                case Register16Bit.BC:
                    _cpu.Registers.BC = (ushort)((_cpu.Registers.BC + 1) & 0xFFFF);
                    break;
                case Register16Bit.DE:
                    _cpu.Registers.DE = (ushort)((_cpu.Registers.DE + 1) & 0xFFFF);
                    break;
                case Register16Bit.HL:
                    _cpu.Registers.HL = (ushort)((_cpu.Registers.HL + 1) & 0xFFFF);
                    break;
                case Register16Bit.SP:
                    _cpu.Registers.StackPointer = (ushort)((_cpu.Registers.StackPointer + 1) & 0xFFFF);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(register), register, null);
            }

            return 2;
        }

        /// <summary>
        /// Decrement a 16 bit register.
        /// Note: Done on 16 bit inc/dec/ld unit, no flag updates
        /// </summary>
        /// <remarks>
        /// This is horrific because we can't pass a ref to a property (obvs).
        /// </remarks>
        internal int Decrement(Register16Bit register)
        {
            switch (register)
            {
                case Register16Bit.AF:
                    _cpu.Registers.AF = (ushort)((_cpu.Registers.AF - 1) & 0xFFFF);
                    break;
                case Register16Bit.BC:
                    _cpu.Registers.BC = (ushort)((_cpu.Registers.BC - 1) & 0xFFFF);
                    break;
                case Register16Bit.DE:
                    _cpu.Registers.DE = (ushort)((_cpu.Registers.DE - 1) & 0xFFFF);
                    break;
                case Register16Bit.HL:
                    _cpu.Registers.HL = (ushort)((_cpu.Registers.HL - 1) & 0xFFFF);
                    break;
                case Register16Bit.SP:
                    _cpu.Registers.StackPointer = (ushort)((_cpu.Registers.StackPointer - 1) & 0xFFFF);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(register), register, null);
            }

            return 2;
        }

        internal int AddHL(int b)
        {
            var result = _cpu.Registers.HL + b;
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (_cpu.Registers.HL & 0xFFF) > (result & 0xFFF));
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, result > 0xFFFF);
            _cpu.Registers.HL = (ushort)(result & 0xFFFF);

            return 4;
        }

        internal int AddSP(sbyte relative)
        {
            var result = _cpu.Registers.StackPointer + relative;
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag | CpuFlags.ZeroFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (_cpu.Registers.HL & 0xFFF) > (result & 0xFFF));
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, result > 0xFFFF);
            _cpu.Registers.StackPointer = (ushort)(result & 0xFFFF);

            return 4;
        }

        #endregion

        #region 16 bit Load/Store/Move

        internal int Load(Register16Bit register, ushort value)
        {
            switch (register)
            {
                case Register16Bit.AF:
                    _cpu.Registers.AF = value;
                    break;
                case Register16Bit.BC:
                    _cpu.Registers.BC = value;
                    break;
                case Register16Bit.DE:
                    _cpu.Registers.DE = value;
                    break;
                case Register16Bit.HL:
                    _cpu.Registers.HL = value;
                    break;
                case Register16Bit.SP:
                    _cpu.Registers.StackPointer = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(register), register, null);
            }

            return 2;
        }

        internal int LoadHLSpPlusR8(in sbyte operand)
        {
            var result = _cpu.Registers.StackPointer + operand;
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag | CpuFlags.ZeroFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (operand & 0xFFF) > (result & 0xFFF));
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, result > 0xFFFF);
            _cpu.Registers.HL = (ushort)(result & 0xFFFF);

            return 3;
        }

        #endregion

        #region Jumps/Calls

        internal int Call(in ushort address)
        {
            PushToStack(_cpu.Registers.ProgramCounter);
            Jump(address);
            return 6;
        }

        internal int CallOnFlag(in CpuFlags flag, in ushort address, in bool isSet)
        {
            return _cpu.Registers.GetFlag(flag) == isSet ? Call(address) : 3;
        }

        internal int Return()
        {
            Jump(PopFromStack());

            return 4;
        }

        internal int ReturnOnFlag(in CpuFlags flag, in bool isSet)
        {
            if (_cpu.Registers.GetFlag(flag) == isSet)
            {
                return Return() + 1; // Return on flag takes 5 cycles according to official gameboy programming manuals
            }

            return 2;
        }

        internal int Jump(in ushort address)
        {
            _cpu.Registers.ProgramCounter = address;
            return 4;
        }

        internal int JumpRight(in sbyte distance)
        {
            _cpu.Registers.ProgramCounter = (ushort)((_cpu.Registers.ProgramCounter + distance) & 0xFFFF);
            return 3;
        }

        internal int JumpRightOnFlag(in CpuFlags flag, in sbyte distance, in bool isSet)
        {
            return _cpu.Registers.GetFlag(flag) == isSet ? JumpRight(distance) : 2;
        }

        internal int JumpOnFlag(CpuFlags flag, ushort address, bool isSet)
        {
            return _cpu.Registers.GetFlag(flag) != isSet ? 3 : Jump(address);
        }

        internal int ReturnAndEnableInterrupts(in InterruptRegisters interruptRegisters)
        {
            Return();

            interruptRegisters.AreInterruptsEnabledGlobally = true;

            return 4;
        }

        internal int Rst(byte page)
        {
            PushToStack(_cpu.Registers.ProgramCounter);
            _cpu.Registers.ProgramCounter = page;
            return 4;
        }

        #endregion

        #region Stack Functions

        internal int PushToStack(ushort value)
        {
            _cpu.Registers.StackPointer = (ushort)((_cpu.Registers.StackPointer - 2) & 0xFFFF);
            _mmu.WriteWord(_cpu.Registers.StackPointer, value);
            return 4;
        }

        private ushort PopFromStack()
        {
            var w = _mmu.ReadWord(_cpu.Registers.StackPointer);
            _cpu.Registers.StackPointer = (ushort)((_cpu.Registers.StackPointer + 2) & 0xFFFF);
            return w;
        }

        internal int PopFromStackIntoRegister(Register16Bit register)
        {
            Load(register, PopFromStack());
            return 3;
        }

        #endregion
    }
}
