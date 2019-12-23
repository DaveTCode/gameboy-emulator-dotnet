using System;

namespace Gameboy.VM
{
    internal class ALU
    {
        private readonly MMU _mmu;
        private readonly Registers _registers;

        internal ALU(MMU mmu, Registers registers)
        {
            _mmu = mmu;
            _registers = registers;
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

        internal int RotateLeftWithCarry(ref byte a)
        {
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, a >= 0x7F);
            a = (byte)(((a << 1) | (a >> 7)) & 0xFF);
            return 1;
        }

        internal int RotateLeftNoCarry(ref byte a)
        {
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, a >= 0x7F);
            a = (byte)((a << 1) | (_registers.GetFlag(FRegisterFlags.CarryFlag) ? 0x1 : 0x0));
            return 1;
        }

        internal int RotateRightWithCarry(ref byte a)
        {
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, (a & 0x1) == 0x1);
            a = (byte)((a >> 1) | ((a & 1) << 7));
            return 1;
        }

        internal int RotateRightNoCarry(ref byte a)
        {
            _registers.SetFlag(FRegisterFlags.ZeroFlag | FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, (a & 0x1) == 0x1);
            a = (byte)((a >> 1) | (_registers.GetFlag(FRegisterFlags.CarryFlag) ? 0x80 : 0x0));
            return 1;
        }

        #endregion

        #region 8 bit arithemetic

        internal int Increment(ref byte a)
        {
            var result = (byte)((a + 1) & 0xFF);
            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (a & 0x0F) + 1 > 0x0F);
            a = result;
            return 1;
        }

        internal int Decrement(ref byte a)
        {
            var result = (byte)((a - 1) & 0xFF);
            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, true);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (a & 0xF) == 0xF);
            a = result;
            return 1;
        }

        internal int IncrementMemoryValue(ushort address)
        {
            var b = _mmu.ReadByte(address);
            Increment(ref b);
            _mmu.WriteByte(address, b);
            return 3;
        }

        internal int DecrementMemoryValue(ushort address)
        {
            var b = _mmu.ReadByte(address);
            Decrement(ref b);
            _mmu.WriteByte(address, b);
            return 3;
        }

        internal int Add(ref byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && _registers.GetFlag(FRegisterFlags.CarryFlag) ? 1 : 0;
            var result = a + b + c;
            _registers.SetFlag(FRegisterFlags.ZeroFlag, (result & 0xFF) == 0x0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (((a & 0xF) + (b & 0xF) + (c & 0xF)) & 0x10) == 0x10);
            _registers.SetFlag(FRegisterFlags.CarryFlag, result > 0xFF);
            a = (byte)(result & 0xFF);
            return 1;
        }

        internal int Sub(ref byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && _registers.GetFlag(FRegisterFlags.CarryFlag) ? 1 : 0;
            var result = a - b - c;
            _registers.SetFlag(FRegisterFlags.ZeroFlag, (result & 0xFF) == 0x0);
            _registers.SetFlag(FRegisterFlags.SubtractFlag, true);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (a & 0x0F) < (b & 0x0F) + c);
            _registers.SetFlag(FRegisterFlags.CarryFlag, result < 0);
            a = (byte)(result & 0xFF);
            return 1;
        }

        internal int And(ref byte a, byte b)
        {
            var result = a & b;
            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0);
            _registers.SetFlag(FRegisterFlags.CarryFlag | FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, true);
            a = (byte)result;
            return 1;
        }

        internal int Xor(ref byte a, byte b)
        {
            var result = a ^ b;
            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0);
            _registers.SetFlag(FRegisterFlags.CarryFlag | FRegisterFlags.SubtractFlag | FRegisterFlags.HalfCarryFlag, false);
            a = (byte)result;
            return 1;
        }

        internal int Or(ref byte a, byte b)
        {
            var result = a | b;
            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0);
            _registers.SetFlag(FRegisterFlags.CarryFlag | FRegisterFlags.SubtractFlag | FRegisterFlags.HalfCarryFlag, false);
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
            var adjust = 0;

            if (_registers.GetFlag(FRegisterFlags.CarryFlag) ||
                (_registers.GetFlag(FRegisterFlags.SubtractFlag) && a > 0x99)) adjust |= 0x60;
            if (_registers.GetFlag(FRegisterFlags.HalfCarryFlag) ||
                (_registers.GetFlag(FRegisterFlags.SubtractFlag) && (a & 0x0F) > 0x09)) adjust |= 0x06;

            var result = (byte)((a + adjust) & 0xFF);

            _registers.SetFlag(FRegisterFlags.ZeroFlag, result == 0x0);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, false);
            _registers.SetFlag(FRegisterFlags.CarryFlag, adjust > 0x60);

            a = result;
            return 1;
        }

        internal int CCF()
        {
            _registers.SetFlag(FRegisterFlags.CarryFlag, !_registers.GetFlag(FRegisterFlags.CarryFlag));
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            return 1;
        }

        internal int SCF()
        {
            _registers.SetFlag(FRegisterFlags.CarryFlag, true);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, false);
            return 1;
        }

        internal int CPL()
        {
            _registers.A ^= _registers.A;
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag | FRegisterFlags.SubtractFlag, true);
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
                    _registers.AF = (ushort)((_registers.AF + 1) & 0xFFFF);
                    break;
                case Register16Bit.BC:
                    _registers.BC = (ushort)((_registers.BC + 1) & 0xFFFF);
                    break;
                case Register16Bit.DE:
                    _registers.DE = (ushort)((_registers.DE + 1) & 0xFFFF);
                    break;
                case Register16Bit.HL:
                    _registers.HL = (ushort)((_registers.HL + 1) & 0xFFFF);
                    break;
                case Register16Bit.SP:
                    _registers.StackPointer = (ushort)((_registers.StackPointer + 1) & 0xFFFF);
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
                    _registers.AF = (ushort)((_registers.AF - 1) & 0xFFFF);
                    break;
                case Register16Bit.BC:
                    _registers.BC = (ushort)((_registers.BC - 1) & 0xFFFF);
                    break;
                case Register16Bit.DE:
                    _registers.DE = (ushort)((_registers.DE - 1) & 0xFFFF);
                    break;
                case Register16Bit.HL:
                    _registers.HL = (ushort)((_registers.HL - 1) & 0xFFFF);
                    break;
                case Register16Bit.SP:
                    _registers.StackPointer = (ushort)((_registers.StackPointer - 1) & 0xFFFF);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(register), register, null);
            }

            return 2;
        }

        internal int Add(Register16Bit outputRegister, ushort a, ushort b)
        {
            var result = a + b;
            _registers.SetFlag(FRegisterFlags.SubtractFlag, false);
            _registers.SetFlag(FRegisterFlags.HalfCarryFlag, (a & 0xFFF) > (result & 0xFFF));
            _registers.SetFlag(FRegisterFlags.CarryFlag, result > 0xFFFF);

            switch (outputRegister)
            {
                case Register16Bit.AF:
                    _registers.AF = (ushort)(result & 0xFFFF);
                    break;
                case Register16Bit.BC:
                    _registers.BC = (ushort)(result & 0xFFFF);
                    break;
                case Register16Bit.DE:
                    _registers.DE = (ushort)(result & 0xFFFF);
                    break;
                case Register16Bit.HL:
                    _registers.HL = (ushort)(result & 0xFFFF);
                    break;
                case Register16Bit.SP:
                    _registers.StackPointer = (ushort)(result & 0xFFFF);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outputRegister), outputRegister, null);
            }

            return 2;
        }

        #endregion

        #region 16 bit Load/Store/Move

        internal int Load(Register16Bit register, ushort value)
        {
            switch (register)
            {
                case Register16Bit.AF:
                    _registers.AF = value;
                    break;
                case Register16Bit.BC:
                    _registers.BC = value;
                    break;
                case Register16Bit.DE:
                    _registers.DE = value;
                    break;
                case Register16Bit.HL:
                    _registers.HL = value;
                    break;
                case Register16Bit.SP:
                    _registers.StackPointer = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(register), register, null);
            }

            return 2;
        }

        #endregion

        #region Jumps/Calls

        internal int Jump(ushort address)
        {
            _registers.ProgramCounter = address;
            return 4;
        }

        internal int JumpRight(sbyte distance)
        {
            _registers.ProgramCounter = (ushort)((_registers.ProgramCounter + distance) & 0xFFFF);
            return 3;
        }

        internal int JumpRightOnFlag(FRegisterFlags flag, sbyte distance, bool isSet)
        {
            return _registers.GetFlag(flag) == isSet ? JumpRight(distance) : 2;
        }

        internal int JumpOnFlag(FRegisterFlags flag, ushort address, bool isSet)
        {
            return _registers.GetFlag(flag) != isSet ? 3 : Jump(address);
        }

        #endregion
    }
}
