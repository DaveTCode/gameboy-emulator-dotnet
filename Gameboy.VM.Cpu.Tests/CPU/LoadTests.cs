using Gameboy.VM.CPU;
using System;
using Xunit;

namespace Gameboy.VM.Tests.CPU
{
    /// <summary>
    /// Test all the opcodes that look like LD x,y or LD x,d16
    /// 
    /// Opcodes covered:
    /// - 40 - 7F (all standard LD operations - missed HALT obvs) - register loads
    /// - 22/32 LD (HL+-), A & 2A/3A LD A, (HL+-) - load and modify HL
    /// - 06,0E,16,1E,26,2E,36,3E - LD A-L&(HL),d8 - direct loads
    /// </summary>
    public class LoadTests
    {
        private byte OpcodeLoadd8FromOpcodeLoadRegDest(byte opcode) => opcode switch
        {
            _ when opcode >= 0x40 && opcode <= 0x47 => 0x06, // LD B, d8
            _ when opcode >= 0x48 && opcode <= 0x4F => 0x0E, // LD C, d8
            _ when opcode >= 0x50 && opcode <= 0x57 => 0x16, // LD D, d8
            _ when opcode >= 0x58 && opcode <= 0x5F => 0x1E, // LD E, d8
            _ when opcode >= 0x60 && opcode <= 0x67 => 0x26, // LD H, d8
            _ when opcode >= 0x68 && opcode <= 0x6F => 0x2E, // LD L, d8
            _ when opcode >= 0x78 && opcode <= 0x7F => 0x3E, // LD A, d8
            _ => throw new ArgumentOutOfRangeException(nameof(opcode), opcode, "Opcode not mapped")
        };

        private byte OpcodeLoadd8FromOpcodeLoadRegSrc(byte opcode) => opcode switch
        {
            _ when opcode == 0x40 || opcode == 0x48 || opcode == 0x50 || opcode == 0x58 || opcode == 0x60 || opcode == 0x68 || opcode == 0x70 || opcode == 0x78 => 0x06, // LD B, d8
            _ when opcode == 0x41 || opcode == 0x49 || opcode == 0x51 || opcode == 0x59 || opcode == 0x61 || opcode == 0x69 || opcode == 0x71 || opcode == 0x79 => 0x0E, // LD C, d8
            _ when opcode == 0x42 || opcode == 0x4A || opcode == 0x52 || opcode == 0x5A || opcode == 0x62 || opcode == 0x6A || opcode == 0x72 || opcode == 0x7A => 0x16, // LD D, d8
            _ when opcode == 0x43 || opcode == 0x4B || opcode == 0x53 || opcode == 0x5B || opcode == 0x63 || opcode == 0x6B || opcode == 0x73 || opcode == 0x7B => 0x1E, // LD E, d8
            _ when opcode == 0x44 || opcode == 0x4C || opcode == 0x54 || opcode == 0x5C || opcode == 0x64 || opcode == 0x6C || opcode == 0x74 || opcode == 0x7C => 0x26, // LD H, d8
            _ when opcode == 0x45 || opcode == 0x4D || opcode == 0x55 || opcode == 0x5D || opcode == 0x65 || opcode == 0x6D || opcode == 0x75 || opcode == 0x7D => 0x2E, // LD L, d8
            _ when opcode == 0x47 || opcode == 0x4F || opcode == 0x57 || opcode == 0x5F || opcode == 0x67 || opcode == 0x6F || opcode == 0x77 || opcode == 0x7F => 0x3E, // LD A, d8
            _ => throw new ArgumentOutOfRangeException(nameof(opcode), opcode, "Opcode not mapped")
        };

        private byte GetRegisterFromOpcode(Registers register, in byte opcode) => opcode switch
        {
            _ when opcode >= 0x40 && opcode <= 0x47 => register.B, // LD B, d8
            _ when opcode >= 0x48 && opcode <= 0x4F => register.C, // LD C, d8
            _ when opcode >= 0x50 && opcode <= 0x57 => register.D, // LD D, d8
            _ when opcode >= 0x58 && opcode <= 0x5F => register.E, // LD E, d8
            _ when opcode >= 0x60 && opcode <= 0x67 => register.H, // LD H, d8
            _ when opcode >= 0x68 && opcode <= 0x6F => register.L, // LD L, d8
            _ when opcode >= 0x78 && opcode <= 0x7F => register.A, // LD A, d8
            _ => throw new ArgumentOutOfRangeException(nameof(opcode), opcode, "Opcode not mapped")
        };

        [Theory]
        [InlineData(0x40)] // LD B, B
        [InlineData(0x41)] // LD B, C
        [InlineData(0x42)] // LD B, D
        [InlineData(0x43)] // LD B, E
        [InlineData(0x44)] // LD B, H
        [InlineData(0x45)] // LD B, L
        [InlineData(0x47)] // LD B, A
        [InlineData(0x48)] // LD C, B
        [InlineData(0x49)] // LD C, C
        [InlineData(0x4A)] // LD C, D
        [InlineData(0x4B)] // LD C, E
        [InlineData(0x4C)] // LD C, H
        [InlineData(0x4D)] // LD C, L
        [InlineData(0x4F)] // LD C, A
        [InlineData(0x50)] // LD D, B
        [InlineData(0x51)] // LD D, C
        [InlineData(0x52)] // LD D, D
        [InlineData(0x53)] // LD D, E
        [InlineData(0x54)] // LD D, H
        [InlineData(0x55)] // LD D, L
        [InlineData(0x57)] // LD D, A
        [InlineData(0x58)] // LD E, B
        [InlineData(0x59)] // LD E, C
        [InlineData(0x5A)] // LD E, D
        [InlineData(0x5B)] // LD E, E
        [InlineData(0x5C)] // LD E, H
        [InlineData(0x5D)] // LD E, L
        [InlineData(0x5F)] // LD E, A
        [InlineData(0x60)] // LD H, B
        [InlineData(0x61)] // LD H, C
        [InlineData(0x62)] // LD H, D
        [InlineData(0x63)] // LD H, E
        [InlineData(0x64)] // LD H, H
        [InlineData(0x65)] // LD H, L
        [InlineData(0x67)] // LD H, A
        [InlineData(0x68)] // LD L, B
        [InlineData(0x69)] // LD L, C
        [InlineData(0x6A)] // LD L, D
        [InlineData(0x6B)] // LD L, E
        [InlineData(0x6C)] // LD L, H
        [InlineData(0x6D)] // LD L, L
        [InlineData(0x6F)] // LD L, A
        [InlineData(0x78)] // LD A, B
        [InlineData(0x79)] // LD A, C
        [InlineData(0x7A)] // LD A, D
        [InlineData(0x7B)] // LD A, E
        [InlineData(0x7C)] // LD A, H
        [InlineData(0x7D)] // LD A, L
        [InlineData(0x7F)] // LD A, A
        public void TestLoadRegisterIntoRegister(byte opcode)
        {
            var opcodeLoadReg1 = OpcodeLoadd8FromOpcodeLoadRegDest(opcode);
            var opcodeLoadReg2 = OpcodeLoadd8FromOpcodeLoadRegSrc(opcode);

            var device = TestUtils.CreateTestDevice(new byte[]
            {
                opcodeLoadReg1, 0x05, // LD reg1, 0x05
                opcodeLoadReg2, 0x0A, // LD reg2, 0x0A
                opcode, // LD reg1, reg2
            });

            for (var ii = 0; ii < 4; ii++) // 2 to move to 0x150 + 2 to set up
            {
                device.Step();
            }

            Assert.Equal(opcodeLoadReg1 == opcodeLoadReg2 ? 0xA : 0x5, GetRegisterFromOpcode(device.CPU.Registers, opcode));

            device.Step();
            Assert.Equal(0xA, GetRegisterFromOpcode(device.CPU.Registers, opcode));
        }

        [Theory]
        [InlineData(0x06, 0x70)] // LD (HL), B
        [InlineData(0x0E, 0x71)] // LD (HL), C
        [InlineData(0x16, 0x72)] // LD (HL), D
        [InlineData(0x1E, 0x73)] // LD (HL), E
        [InlineData(0x3E, 0x77)] // LD (HL), A
        public void TestLoadRegisterIntoMemory(byte regLoadOpcode, byte memLoadOpcode)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                regLoadOpcode, 0x05, // LD B 0x05
                0x21, 0x00, 0xC0, // LD HL, 0xC000
                memLoadOpcode // LD (HL), B
            });

            for (var ii = 0; ii < 5; ii++) // 2 to move to 0x150 + 3 to act
            {
                device.Step();
            }

            Assert.Equal(0x05, device.MMU.ReadByte(0xC000));
        }

        [Fact]
        public void TestLoadHAndLIntoHL()
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x04, 0xC0, // LD HL, 0xC004
                0x74, // LD (HL), H
                0x21, 0x05, 0xC0, // LD HL, 0xC005
                0x75 // LD (HL), L
            });

            for (var ii = 0; ii < 6; ii++) // 2 to move to 0x150 + 4 to act
            {
                device.Step();
            }

            Assert.Equal(0xC0, device.MMU.ReadByte(0xC004));
            Assert.Equal(0x05, device.MMU.ReadByte(0xC005));
        }

        [Theory]
        [InlineData(0x46)] // LD B, (HL)
        [InlineData(0x4E)] // LD C, (HL)
        [InlineData(0x56)] // LD D, (HL)
        [InlineData(0x5E)] // LD E, (HL)
        [InlineData(0x66)] // LD H, (HL)
        [InlineData(0x6E)] // LD L, (HL)
        [InlineData(0x7E)] // LD A, (HL)
        public void TestLoadMemoryIntoRegister(byte memLoadOpcode)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x00, 0xC0, // LD HL, 0xC000
                0x36, 0x08, // LD (HL), 8
                memLoadOpcode // LD reg, (HL)
            });

            for (var ii = 0; ii < 5; ii++) // 2 to move to 0x150 + 3 to act
            {
                device.Step();
            }

            Assert.Equal(0x08, device.MMU.ReadByte(0xC000)); // Check byte written to memory
            Assert.Equal(0x08, memLoadOpcode switch
            {
                0x46 => device.CPU.Registers.B,
                0x4E => device.CPU.Registers.C,
                0x56 => device.CPU.Registers.D,
                0x5E => device.CPU.Registers.E,
                0x66 => device.CPU.Registers.H,
                0x6E => device.CPU.Registers.L,
                0x7E => device.CPU.Registers.A,
                _ => throw new ArgumentOutOfRangeException(nameof(memLoadOpcode), memLoadOpcode, "Opcode not mapped")
            });
        }

        [Fact]
        public void TestLoadAndChangeHL()
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x05, 0xC0, // LD HL, 0xC005
                0x3E, 0x08, // LD A, 8
                0x32, // LD (HL-), A
                0x3E, 0x07, // LD A, 7
                0x22, // LD (HL+), A
            });

            for (var ii = 0; ii < 5; ii++) // 2 to move to 0x150 + 3 to act
            {
                device.Step();
            }

            Assert.Equal(0x08, device.MMU.ReadByte(0xC005)); // Check byte written to memory
            Assert.Equal(0xC004, device.CPU.Registers.HL);

            device.Step(); device.Step();

            Assert.Equal(0x07, device.MMU.ReadByte(0xC004)); // Check byte written to memory
            Assert.Equal(0xC005, device.CPU.Registers.HL);
        }

        [Fact]
        public void TestLoadIntoAAndChangeHL()
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x21, 0x05, 0xC0, // LD HL, 0xC005
                0x36, 0x08, // LD (HL), d8
                0x3A, // LD A, (HL-)
                0x36, 0x07, // LD (HL), d8
                0x2A, // LD A, (HL+)
            });

            for (var ii = 0; ii < 5; ii++) // 2 to move to 0x150 + 3 to act
            {
                device.Step();
            }

            Assert.Equal(0xC004, device.CPU.Registers.HL);
            Assert.Equal(0x08, device.CPU.Registers.A);

            device.Step(); device.Step();

            Assert.Equal(0xC005, device.CPU.Registers.HL);
            Assert.Equal(0x07, device.CPU.Registers.A);
        }

        [Theory]
        [InlineData(0xFFF8, -2, 0xFFF6, 0x20)]
        [InlineData(0xFFF8, 2, 0xFFFA, 0x0)]
        public void TestLoadHlSpPlusR8(ushort stackPointer, sbyte r8, ushort result, byte flagValues)
        {
            var device = TestUtils.CreateTestDevice(new byte[]
            {
                0x31, (byte)(stackPointer & 0xFF), (byte)(stackPointer >> 8), // Set SP
                0x21, 0x05, 0xC0, // LD HL, 0xC005
                0xF8, (byte)r8, // LD HL, SP+r8
            });

            for (var ii = 0; ii < 4; ii++) // 2 to move to 0x150 and 2 to set up
            {
                device.Step();
            }

            Assert.Equal(stackPointer, device.CPU.Registers.StackPointer);
            Assert.Equal(0xC005, device.CPU.Registers.HL);

            device.Step();

            Assert.Equal(stackPointer, device.CPU.Registers.StackPointer); // Doesn't modify SP
            Assert.Equal(result, device.CPU.Registers.HL);
            Assert.Equal(flagValues, device.CPU.Registers.F); // All flags unset
        }
    }
}
