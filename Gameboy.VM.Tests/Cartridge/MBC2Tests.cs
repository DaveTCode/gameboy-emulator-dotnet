using System;
using System.Linq;
using Gameboy.VM.Cartridge;
using Gameboy.VM.LCD;
using Gameboy.VM.Sound;
using Xunit;

namespace Gameboy.VM.Tests.Cartridge
{
    public class MBC2Tests
    {
        private readonly byte[] _cartridgeBaseContents = new byte[0x40000]; // 256KB blank cartridge

        public MBC2Tests()
        {
            var cartridgeHeader = new byte[0x100].Concat(new byte[]
            {
                0x00, 0xC3, 0x50, 0x01, 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83,
                0x00, 0x0C, 0x00, 0x0D, 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6,
                0xDD, 0xDD, 0xD9, 0x99, 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F,
                0xBB, 0xB9, 0x33, 0x3E, 0x6D, 0x6F, 0x6F, 0x6E, 0x65, 0x79, 0x65, 0x2D, 0x67, 0x62, 0x20, 0x74,
                0x65, 0x73, 0x74, 0x00, 0x5A, 0x5A, 0x00, 0x06, 0x03, 0x00, 0x01, 0x33, 0x00, 0x27, 0x71, 0x73
            }).ToArray();
            Array.Copy(cartridgeHeader, _cartridgeBaseContents, cartridgeHeader.Length);
        }

        /// <summary>
        /// Tests a few things:
        /// 1. RAM top 4 bits are always 1
        /// 2. RAM enable/disable only works on correct addresses
        /// </summary>
        [Fact]
        public void TestRamAccess()
        {
            var device = new Device(CartridgeFactory.CreateCartridge(_cartridgeBaseContents), DeviceType.DMG, new NullRenderer(), new NullSoundOutput());
            device.SkipBootRom();

            Assert.IsType<MBC2Cartridge>(device.Cartridge);

            for (ushort ii = 0x0; ii < 0x3FFF; ii++)
            {
                device.MMU.WriteByte(0, 0); // Disable RAM
                device.MMU.WriteByte(ii, 0x0A); // Maybe enable RAM
                device.MMU.WriteByte(0xA000, 0x1);
                if ((ii & 0x100) == 0)
                {
                    Assert.Equal(0x1 | 0b11110000, device.MMU.ReadByte(0xA000));
                }
                else
                {
                    Assert.Equal(0xFF, device.MMU.ReadByte(0xA000));
                }
            }
        }

        [Fact]
        public void TestRomBanking()
        {
            var cartridgeContents = new byte[0x40000];
            Array.Copy(_cartridgeBaseContents, cartridgeContents, _cartridgeBaseContents.Length);
            cartridgeContents[0x7FFF] = 0x9; // Set a random value in ROM bank 1
            var device = new Device(CartridgeFactory.CreateCartridge(cartridgeContents), DeviceType.DMG, new NullRenderer(), new NullSoundOutput());

            Assert.Equal(CartridgeROMSize.A256KB, device.Cartridge.ROMSize);
            Assert.Equal(CartridgeRAMSize.None, device.Cartridge.RAMSize);

            device.SkipBootRom();
            Assert.Equal(0xC3, device.MMU.ReadByte(0x0101)); // Check one fixed value from header to make sure bank 0 readable
            device.MMU.WriteByte(0x2100, 0x0); // Note that this sets rom bank to 1 not 0
            Assert.Equal(9, device.MMU.ReadByte(0x7FFF));
        }
    }
}
