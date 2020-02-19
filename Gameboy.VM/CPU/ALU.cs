namespace Gameboy.VM.CPU
{
    internal class ALU
    {
        private readonly CPU _cpu;

        internal ALU(CPU cpu)
        {
            _cpu = cpu;
        }

        #region 8 bit Rotate/Shift/Bit

        internal void RotateLeftWithCarryA()
        {
            RotateLeftWithCarry(ref _cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, false);
        }

        internal void RotateLeftWithCarry(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, a > 0x7F);
            a = (byte)((a << 1) | (a >> 7));
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
        }

        internal void RotateLeftNoCarryA()
        {
            RotateLeftNoCarry(ref _cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, false);
        }

        internal void RotateLeftNoCarry(ref byte a)
        {
            var setCarry = (a & 0x80) == 0x80;
            a = (byte)((a << 1) | (_cpu.Registers.GetFlag(CpuFlags.CarryFlag) ? 0x1 : 0x0));
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, setCarry);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
        }

        internal void RotateRightWithCarryA()
        {
            RotateRightWithCarry(ref _cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, false);
        }

        internal void RotateRightWithCarry(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, (a & 0x1) == 0x1);
            a = (byte)((a >> 1) | ((a & 1) << 7));
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
        }

        internal void RotateRightNoCarryA()
        {
            RotateRightNoCarry(ref _cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, false);
        }

        internal void RotateRightNoCarry(ref byte a)
        {
            var setCarry = (a & 0x1) == 0x1;
            a = (byte)((a >> 1) | (_cpu.Registers.GetFlag(CpuFlags.CarryFlag) ? 0x80 : 0x0));
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, setCarry);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
        }

        internal void ShiftLeft(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, (a & 0x80) == 0x80);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            a = (byte)(a << 1);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0);
        }

        internal void ShiftRightAdjust(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, (a & 0x01) == 0x01);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            a = (byte)((a >> 1) | (a & 0x80));
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0);
        }

        internal void ShiftRightLeave(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, (a & 0x01) == 0x01);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
            a = (byte)(a >> 1);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0);
        }

        internal void Swap(ref byte a)
        {
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag | CpuFlags.CarryFlag, false);
            a = (byte)((a >> 4) | (a << 4));
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0);
        }

        internal void Bit(byte a, int bit)
        {
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, true);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, (a & (1 << bit)) == 0);
        }

        internal void Res(ref byte a, int bit)
        {
            a = (byte)(a & ~(1 << bit));
        }

        internal void Set(ref byte a, int bit)
        {
            a = (byte)(a | (1 << bit));
        }

        #endregion

        #region 8 bit arithemetic

        internal void Increment(ref byte a)
        {
            var result = (byte)(a + 1);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (a & 0x0F) + 1 > 0x0F);
            a = result;
        }

        internal void Decrement(ref byte a)
        {
            var result = (byte)(a - 1);
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, true);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (a & 0x0F) < 0x01);
            a = result;
        }

        internal void Add(ref byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && _cpu.Registers.GetFlag(CpuFlags.CarryFlag) ? 1 : 0;
            var result = a + b + c;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, (result & 0xFF) == 0x0);
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (((a & 0xF) + (b & 0xF) + (c & 0xF)) & 0x10) == 0x10);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, result > 0xFF);
            a = (byte)result;
        }

        internal void Sub(ref byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && _cpu.Registers.GetFlag(CpuFlags.CarryFlag) ? 1 : 0;
            var result = a - b - c;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, (result & 0xFF) == 0x0);
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, true);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (a & 0x0F) < (b & 0x0F) + c);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, result < 0);
            a = (byte)result;
        }

        internal void And(ref byte a, byte b)
        {
            var result = a & b;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag | CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, true);
            a = (byte)result;
        }

        internal void Xor(ref byte a, byte b)
        {
            var result = a ^ b;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag | CpuFlags.SubtractFlag | CpuFlags.HalfCarryFlag, false);
            a = (byte)result;
        }

        internal void Or(ref byte a, byte b)
        {
            var result = a | b;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, result == 0);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag | CpuFlags.SubtractFlag | CpuFlags.HalfCarryFlag, false);
            a = (byte)result;
        }

        internal void Cp(byte a, byte b)
        {
            Sub(ref a, b, false);
        }

        internal void DecimalAdjustRegister(ref byte a)
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

            a = (byte)tmp;
            _cpu.Registers.SetFlag(CpuFlags.ZeroFlag, a == 0x0);
            if (tmp > 0xFF) _cpu.Registers.SetFlag(CpuFlags.CarryFlag, true); // Note that we don't unset Carry, only ever set it
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, false);
        }

        internal void CCF()
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, !_cpu.Registers.GetFlag(CpuFlags.CarryFlag));
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
        }

        internal void SCF()
        {
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, true);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, false);
        }

        internal void CPL()
        {
            _cpu.Registers.A = (byte)(~_cpu.Registers.A);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag | CpuFlags.SubtractFlag, true);
        }

        #endregion

        #region 16 bit arithemetic

        internal void AddHL(ushort b)
        {
            var result = _cpu.Registers.HL + b;
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, (_cpu.Registers.HL & 0xFFF) + (b & 0xFFF) > 0xFFF);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, result > 0xFFFF);
            _cpu.Registers.HL = (ushort)result;
        }

        internal void AddSP(sbyte operand)
        {
            var result = _cpu.Registers.StackPointer + operand;
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag | CpuFlags.ZeroFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, ((_cpu.Registers.StackPointer ^ operand ^ (result & 0xFFFF)) & 0x10) == 0x10);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, ((_cpu.Registers.StackPointer ^ operand ^ (result & 0xFFFF)) & 0x100) == 0x100);
            _cpu.Registers.StackPointer = (ushort)result;
        }

        #endregion

        #region 16 bit Load/Store/Move

        internal void LoadHLSpPlusR8(sbyte operand)
        {
            var result = _cpu.Registers.StackPointer + operand;
            _cpu.Registers.SetFlag(CpuFlags.SubtractFlag | CpuFlags.ZeroFlag, false);
            _cpu.Registers.SetFlag(CpuFlags.HalfCarryFlag, ((_cpu.Registers.StackPointer ^ operand ^ (result & 0xFFFF)) & 0x10) == 0x10);
            _cpu.Registers.SetFlag(CpuFlags.CarryFlag, ((_cpu.Registers.StackPointer ^ operand ^ (result & 0xFFFF)) & 0x100) == 0x100);
            _cpu.Registers.HL = (ushort)result;
        }

        #endregion
    }
}
