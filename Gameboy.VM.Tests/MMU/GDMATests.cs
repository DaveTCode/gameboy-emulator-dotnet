using Xunit;

namespace Gameboy.VM.Tests.MMU
{
    public class GDMATests
    {
        [Fact(DisplayName = "Test basic GDMA copies correct source into correct destination")]
        public void TestGDMABasic()
        {
            var program = new byte[]
            {
                0x3E, 0xC0, // LD A, C0
                0xE0, 0x51, // LDH FF51, A - High byte of GDMA source
                0x3E, 0x00, // LD A, 00
                0xE0, 0x52, // LDH FF52, A - Low byte of GDMA source
                0x3E, 0x80, // LD A, 80
                0xE0, 0x53, // LDH FF53, A - High byte of GDMA dest
                0x3E, 0x00, // LD A, 00
                0xE0, 0x54, // LDH FF54, A - Low byte of GDMA dest
                0x3E, 0x00, // LD A, 00
                0xE0, 0x55, // LDH FF55, A - Trigger GDMA Transfer
                0x00, 0x00,
                0x00, 0x00, 
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00  // Extra NOPs for ease of use
            };
            var device = TestUtils.CreateTestDevice(program, DeviceType.CGB);
            device.LCDRegisters.LCDControlRegister = 0x0; // Turn LCD off
            // Set up 16 bytes of working RAM (minimum transfer amount)
            for (byte ii = 0; ii < 0x10; ii++)
            {
                device.MMU.WriteByte((ushort)(0xC000 + ii), ii);
            }

            for (var ii = 0; ii < 12; ii++) device.Step(); // Set up GDMA Transfer

            // All HDMA registers return 0xFF regardless of what they were set to
            Assert.Equal(0xFF, device.MMU.ReadByte(0xFF51));
            Assert.Equal(0xFF, device.MMU.ReadByte(0xFF52));
            Assert.Equal(0xFF, device.MMU.ReadByte(0xFF53));
            Assert.Equal(0xFF, device.MMU.ReadByte(0xFF54));

            // HDMA5 should still return 0x0, not 0xFF to indicate success
            Assert.Equal(0x00, device.MMU.ReadByte(0xFF55));

            // The HDMA is one m-cycle underway at this point although in reality this is a bug caused by opcode atomicity
            var currentPC = device.CPU.Registers.ProgramCounter;

            // 16 bytes at 2 bytes per cycle = 8 cycles to complete GDMA (but one happened above)
            for (var ii = 0; ii < 7; ii++) device.Step();

            // Check that the CPU is halted during GDMA
            Assert.Equal(currentPC, device.CPU.Registers.ProgramCounter);

            // GDMA should be complete now (one block done)
            Assert.Equal(0xFF, device.MMU.ReadByte(0xFF55));

            for (byte ii = 0; ii < 0x10; ii++)
            {
                Assert.Equal(ii, device.MMU.ReadByte((ushort) (0x8000 + ii)));
            }
        }
    }
}
