using System;
using System.Linq;
using Gameboy.VM.Cartridge;
using Gameboy.VM.LCD;
using Gameboy.VM.Sound;
using Xunit;

namespace Gameboy.VM.Tests.Cartridge
{
    public class MBC5Tests
    {
        private readonly byte[] _cartridgeBaseContents = new byte[512 * 0x4000]; // 64Mb

        public MBC5Tests()
        {
            var cartridgeHeader = new byte[0x100].Concat(new byte[]
            {
                0x00, 0xC3, 0x50, 0x01, 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83,
                0x00, 0x0C, 0x00, 0x0D, 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6,
                0xDD, 0xDD, 0xD9, 0x99, 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F,
                0xBB, 0xB9, 0x33, 0x3E, 0x43, 0x47, 0x42, 0x20, 0x44, 0x45, 0x4D, 0x4F, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x80, 0x30, 0x31, 0x00, 0x19, 0x08, 0x04, 0x01, 0x33, 0x00, 0xA4, 0x30, 0xE3
            }).ToArray();
            Array.Copy(cartridgeHeader, _cartridgeBaseContents, cartridgeHeader.Length);
        }

        [Fact]
        public void TestRomBanking()
        {
            var cartridgeContents = new byte[_cartridgeBaseContents.Length];
            Array.Copy(_cartridgeBaseContents, cartridgeContents, _cartridgeBaseContents.Length);

            // Set a value in each of the 512 rom banks
            for (var ii = 0; ii < 512; ii++)
            {
                cartridgeContents[0x4000 * ii + 0x1000] = (byte)ii;
            }

            var device = new Device(CartridgeFactory.CreateCartridge(cartridgeContents), DeviceType.CGB, new NullRenderer(DeviceType.DMG), new NullSoundOutput(), null);
            device.SkipBootRom();

            Assert.Equal(CartridgeROMSize.A8MB, device.Cartridge.ROMSize);
            Assert.Equal(CartridgeRAMSize.A128KB, device.Cartridge.RAMSize);
            Assert.Equal(0xC3, device.MMU.ReadByte(0x0101)); // Check one fixed value from header to make sure bank 0 readable

            for (var ii = 0; ii < 512; ii++)
            {
                // Set the ROM bank by setting both upper and lower parts of rom bank
                device.MMU.WriteByte(0x2000, (byte)ii);
                device.MMU.WriteByte(0x3000, (byte)(ii >> 8));

                // Check that the byte in 0x1000 in the ROM bank is what we set earlier
                Assert.Equal((byte)ii, device.MMU.ReadByte(0x5000));
            }
        }

        // TODO - Test RAM Banking on MBC5 chip
    }
}
