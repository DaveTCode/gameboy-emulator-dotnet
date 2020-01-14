using Gameboy.VM.CPU;
using Xunit;

namespace Gameboy.VM.Tests.CPU
{
    public class Alu16BitArithmeticTests
    {
        [Theory]
        [InlineData(0x01, 0x00, 0x00, 0x00, 0x01, 0x03, 0x78, 0x79)] // INC BC[0]
        [InlineData(0x01, 0xFF, 0xFF, 0x00, 0x00, 0x03, 0x78, 0x79)] // INC BC[FFFF]
        [InlineData(0x01, 0x23, 0x5F, 0x23, 0x60, 0x03, 0x78, 0x79)] // INC BC[235F]
        [InlineData(0x11, 0x00, 0x00, 0x00, 0x01, 0x13, 0x7A, 0x7B)] // INC DE[0]
        [InlineData(0x11, 0xFF, 0xFF, 0x00, 0x00, 0x13, 0x7A, 0x7B)] // INC DE[FFFF]
        [InlineData(0x11, 0x23, 0x5F, 0x23, 0x60, 0x13, 0x7A, 0x7B)] // INC DE[235F]
        [InlineData(0x21, 0x00, 0x00, 0x00, 0x01, 0x23, 0x7C, 0x7D)] // INC HL[0]
        [InlineData(0x21, 0xFF, 0xFF, 0x00, 0x00, 0x23, 0x7C, 0x7D)] // INC HL[FFFF]
        [InlineData(0x21, 0x23, 0x5F, 0x23, 0x60, 0x23, 0x7C, 0x7D)] // INC HL[235F]
        [InlineData(0x31, 0x00, 0x00, 0x00, 0x01, 0x33, 0x78, 0x79)] // INC SP[0]
        [InlineData(0x31, 0xFF, 0xFF, 0x00, 0x00, 0x33, 0x78, 0x79)] // INC SP[FFFF]
        [InlineData(0x31, 0x23, 0x5F, 0x23, 0x60, 0x33, 0x78, 0x79)] // INC SP[235F]
        public void Test16BitIncrement(byte regPairLoadOpcode, byte inHighByte, byte inLowByte, byte outHighByte,
            byte outLowByte, byte incOpcode, byte loadHighByteIntoA, byte loadLowByteIntoA)
        {
            var device = TestUtils.CreateTestDevice(new[]
            {
                regPairLoadOpcode, inLowByte, inHighByte, // LD reg pair from d16
                incOpcode, // INC regpair
                loadHighByteIntoA, // LD high byte of pair into A for checking
                loadLowByteIntoA, // LD low byte of pair into A for checking
            });

            for (var ii = 0; ii < 5; ii++) device.Step();

            if (regPairLoadOpcode == 0x31) // Can't move SP to A so checking a different way
            {
                Assert.Equal(outHighByte << 8 | outLowByte, device.CPU.Registers.StackPointer);
            }
            else
            {
                Assert.Equal(outHighByte, device.CPU.Registers.A);
                device.Step();
                Assert.Equal(outLowByte, device.CPU.Registers.A);   
            }
        }

        [Theory]
        [InlineData(0x01, 0x00, 0x00, 0xFF, 0xFF, 0x0B, 0x78, 0x79)] // DEC BC[0]
        [InlineData(0x01, 0xFF, 0xFF, 0xFF, 0xFE, 0x0B, 0x78, 0x79)] // DEC BC[FFFF]
        [InlineData(0x01, 0x23, 0x5F, 0x23, 0x5E, 0x0B, 0x78, 0x79)] // DEC BC[235F]
        [InlineData(0x11, 0x00, 0x00, 0xFF, 0xFF, 0x1B, 0x7A, 0x7B)] // DEC DE[0]
        [InlineData(0x11, 0xFF, 0xFF, 0xFF, 0xFE, 0x1B, 0x7A, 0x7B)] // DEC DE[FFFF]
        [InlineData(0x11, 0x23, 0x5F, 0x23, 0x5E, 0x1B, 0x7A, 0x7B)] // DEC DE[235F]
        [InlineData(0x21, 0x00, 0x00, 0xFF, 0xFF, 0x2B, 0x7C, 0x7D)] // DEC HL[0]
        [InlineData(0x21, 0xFF, 0xFF, 0xFF, 0xFE, 0x2B, 0x7C, 0x7D)] // DEC HL[FFFF]
        [InlineData(0x21, 0x23, 0x5F, 0x23, 0x5E, 0x2B, 0x7C, 0x7D)] // DEC HL[235F]
        [InlineData(0x31, 0x00, 0x00, 0xFF, 0xFF, 0x3B, 0x78, 0x79)] // DEC SP[0]
        [InlineData(0x31, 0xFF, 0xFF, 0xFF, 0xFE, 0x3B, 0x78, 0x79)] // DEC SP[FFFF]
        [InlineData(0x31, 0x23, 0x5F, 0x23, 0x5E, 0x3B, 0x78, 0x79)] // DEC SP[235F]
        public void Test16BitDecrement(byte regPairLoadOpcode, byte inHighByte, byte inLowByte, byte outHighByte,
            byte outLowByte, byte decOpcode, byte loadHighByteIntoA, byte loadLowByteIntoA)
        {
            var device = TestUtils.CreateTestDevice(new[]
            {
                regPairLoadOpcode, inLowByte, inHighByte, // LD reg pair from d16
                decOpcode, // DEC regpair
                loadHighByteIntoA, // LD high byte of pair into A for checking
                loadLowByteIntoA, // LD low byte of pair into A for checking
            });

            for (var ii = 0; ii < 5; ii++) device.Step();

            if (regPairLoadOpcode == 0x31) // Can't move SP to A so checking a different way
            {
                Assert.Equal(outHighByte << 8 | outLowByte, device.CPU.Registers.StackPointer);
            }
            else
            {
                Assert.Equal(outHighByte, device.CPU.Registers.A);
                device.Step();
                Assert.Equal(outLowByte, device.CPU.Registers.A);   
            }
        }

        [Theory]
        [InlineData(0x01, 0x06, 0x05, 0x8A, 0x23, 0x90, 0x28, 0x09, false, true)] // ADD HL[8a23], BC[0605]
        [InlineData(0x01, 0x8A, 0x23, 0x8A, 0x23, 0x14, 0x46, 0x09, true, true)] // ADD HL[8a23], BC[8A23]
        [InlineData(0x01, 0x00, 0x02, 0xFF, 0xF8, 0xFF, 0xFA, 0x09, false, false)] // ADD HL[FFF8], BC[0002]
        [InlineData(0x11, 0x06, 0x05, 0x8A, 0x23, 0x90, 0x28, 0x19, false, true)] // ADD HL[8a23], DE[0605]
        [InlineData(0x11, 0x8A, 0x23, 0x8A, 0x23, 0x14, 0x46, 0x19, true, true)] // ADD HL[8a23], DE[8A23]
        [InlineData(0x11, 0x00, 0x02, 0xFF, 0xF8, 0xFF, 0xFA, 0x19, false, false)] // ADD HL[FFF8], DE[0002]
        [InlineData(0x31, 0x06, 0x05, 0x8A, 0x23, 0x90, 0x28, 0x39, false, true)] // ADD HL[8a23], SP[0605]
        [InlineData(0x31, 0x8A, 0x23, 0x8A, 0x23, 0x14, 0x46, 0x39, true, true)] // ADD HL[8a23], SP[8A23]
        [InlineData(0x31, 0x00, 0x02, 0xFF, 0xF8, 0xFF, 0xFA, 0x39, false, false)] // ADD HL[8a23], SP[8A23]
        public void Test16BitAdd(byte regPairLoadOpcode, byte inHighByte, byte inLowByte, byte hlHighByte, byte hlLowByte, byte outHighByte,
            byte outLowByte, byte addOpcode, bool c, bool h)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, hlLowByte, hlHighByte, // Set up HL as accumulator
                regPairLoadOpcode, inLowByte, inHighByte, // LD reg pair from d16
                addOpcode, // ADD HL,regpair
                0x7C, // LD A,H check high byte
                0x7D, // LD A,L check low byte
            });

            for (var ii = 0; ii < 6; ii++) device.Step();

            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(c, device.CPU.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, device.CPU.Registers.GetFlag(CpuFlags.HalfCarryFlag));

            Assert.Equal(outHighByte, device.CPU.Registers.A);
            device.Step();
            Assert.Equal(outLowByte, device.CPU.Registers.A);
        }

        [Theory]
        [InlineData(0x8A, 0x23, 0x14, 0x46, true, true)]
        public void Test16BitAddHLHL(byte hlHighByte, byte hlLowByte, byte outHighByte, byte outLowByte, bool c, bool h)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, hlLowByte, hlHighByte, // Set up HL
                0x29, // ADD HL,HL
            });

            for (var ii = 0; ii < 4; ii++) device.Step();

            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.Equal(c, device.CPU.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, device.CPU.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.Equal(device.CPU.Registers.H, outHighByte);
            Assert.Equal(device.CPU.Registers.L, outLowByte);
        }

        [Theory]
        [InlineData(0xFF, 0xF8, 0x2, 0xFFFA, false, false)]
        [InlineData(0xFF, 0xF8, 0xFF, 0xFFF7, true, true)]
        public void TestLDHLSPr8(byte spHighByte, byte spLowByte, byte relative, ushort expected, bool c, bool h)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x31, spLowByte, spHighByte, // Set up SP
                0xF8, relative, // LD HL, SP + r8
            });

            for (var ii = 0; ii < 4; ii++) device.Step(); // 4 steps to set up and run test

            Assert.Equal(expected, device.CPU.Registers.HL);
            Assert.Equal(c, device.CPU.Registers.GetFlag(CpuFlags.CarryFlag));
            Assert.Equal(h, device.CPU.Registers.GetFlag(CpuFlags.HalfCarryFlag));
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.SubtractFlag));
            Assert.False(device.CPU.Registers.GetFlag(CpuFlags.ZeroFlag));
        }
    }
}
