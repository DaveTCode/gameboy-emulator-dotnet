using Xunit;

namespace Gameboy.VM.Cpu.Tests.Opcodes
{
    public class Alu16BitArithmeticTests
    {
        [Theory]
        [InlineData((ushort)0xFFFF, (ushort)0x0000, false, false, false, false)]
        [InlineData((ushort)0xFFFF, (ushort)0x0000, true, true, true, true)]
        [InlineData((ushort)0x235F, (ushort)0x2360, false, false, false, false)]
        [InlineData((ushort)0x235F, (ushort)0x2360, true, true, true, true)]
        public void Test16BitIncrement(ushort bc, ushort result, bool c, bool h, bool n, bool z)
        {
            var reg = new Registers();
            var alu = new ALU(new MMU(), reg);
            reg.BC = bc;
            reg.SetFlag(FRegisterFlags.CarryFlag, c);
            reg.SetFlag(FRegisterFlags.HalfCarryFlag, h);
            reg.SetFlag(FRegisterFlags.SubtractFlag, n);
            reg.SetFlag(FRegisterFlags.ZeroFlag, z);
            Assert.Equal(2, alu.Increment(Register16Bit.BC));
            Assert.Equal(result, reg.BC);
            Assert.Equal(c, reg.GetFlag(FRegisterFlags.CarryFlag));
            Assert.Equal(h, reg.GetFlag(FRegisterFlags.HalfCarryFlag));
            Assert.Equal(n, reg.GetFlag(FRegisterFlags.SubtractFlag));
            Assert.Equal(z, reg.GetFlag(FRegisterFlags.ZeroFlag));
        }

        [Theory]
        [InlineData((ushort)0x0000, (ushort)0xFFFF, false, false, false, false)]
        [InlineData((ushort)0x0000, (ushort)0xFFFF, true, true, true, true)]
        [InlineData((ushort)0x235F, (ushort)0x235E, false, false, false, false)]
        [InlineData((ushort)0x235F, (ushort)0x235E, true, true, true, true)]
        public void Test16BitDecrement(ushort bc, ushort result, bool c, bool h, bool n, bool z)
        {
            var reg = new Registers();
            var alu = new ALU(new MMU(), reg);
            reg.BC = bc;
            reg.SetFlag(FRegisterFlags.CarryFlag, c);
            reg.SetFlag(FRegisterFlags.HalfCarryFlag, h);
            reg.SetFlag(FRegisterFlags.SubtractFlag, n);
            reg.SetFlag(FRegisterFlags.ZeroFlag, z);
            Assert.Equal(2, alu.Decrement(Register16Bit.BC));
            Assert.Equal(result, reg.BC);
            Assert.Equal(c, reg.GetFlag(FRegisterFlags.CarryFlag));
            Assert.Equal(h, reg.GetFlag(FRegisterFlags.HalfCarryFlag));
            Assert.Equal(n, reg.GetFlag(FRegisterFlags.SubtractFlag));
            Assert.Equal(z, reg.GetFlag(FRegisterFlags.ZeroFlag));
        }

        [Theory]
        [InlineData(0x8A23, 0x0605, 0x9028, false, true, false, false)]
        [InlineData(0x8A23, 0x8A23, 0x1446, true, true, false, false)]
        [InlineData(0xFFF8, 0x0002, 0xFFFA, false, false, false, false)]
        public void Test16BitAdd(ushort a, ushort b, ushort result, bool c, bool h, bool n, bool z)
        {
            var reg = new Registers();
            var alu = new ALU(new MMU(), reg);
            reg.BC = a;
            reg.SetFlag(FRegisterFlags.CarryFlag, c);
            reg.SetFlag(FRegisterFlags.HalfCarryFlag, h);
            reg.SetFlag(FRegisterFlags.SubtractFlag, n);
            reg.SetFlag(FRegisterFlags.ZeroFlag, z);
            Assert.Equal(4, alu.Add(Register16Bit.BC, a, b));
            Assert.Equal(result, reg.BC);
            Assert.Equal(c, reg.GetFlag(FRegisterFlags.CarryFlag));
            Assert.Equal(h, reg.GetFlag(FRegisterFlags.HalfCarryFlag));
            Assert.Equal(n, reg.GetFlag(FRegisterFlags.SubtractFlag));
            Assert.Equal(z, reg.GetFlag(FRegisterFlags.ZeroFlag));
        }
    }
}
