using System;
using System.Linq;
using Gameboy.VM.Cartridge;
using Xunit;

namespace Gameboy.VM.Tests.Cartridge
{
    // TODO - Lots more tests around special case of setting ROM/RAM bank mode
    public class MBC1Tests
    {
        private readonly byte[] _cartridgeBaseContents = new byte[0x8000]; // 64KB blank cartridge

        public MBC1Tests()
        {
            var cartridgeHeader = new byte[0x100].Concat(new byte[]
            {
                0x00, 0xC3, 0x37, 0x06, 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, // 0x100-0x10F 
                0x00, 0x0C, 0x00, 0x0D, 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, // 0x110-0x11F
                0xDD, 0xDD, 0xD9, 0x99, 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, // 0x120-0x12F 
                0xBB, 0xB9, 0x33, 0x3E, 0x43, 0x50, 0x55, 0x5F, 0x49, 0x4E, 0x53, 0x54, 0x52, 0x53, 0x00, 0x00, // 0x130-0x13F
                0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x3B, 0xF5, 0x30  // 0x140-0x14F
            }).ToArray();
            Array.Copy(cartridgeHeader, _cartridgeBaseContents, cartridgeHeader.Length);
        }

        /// <summary>
        /// Test that if no RAM is on the cartridge according to the header then all addresses behave as expected
        /// </summary>
        [Fact]
        public void TestRamAccessNoBanks()
        {
            var device = new Device(CartridgeFactory.CreateCartridge(_cartridgeBaseContents), DeviceMode.DMG);
            device.SkipBootRom();

            for (ushort ii = 0x0; ii < 0x2000; ii++)
            {
                device.MMU.WriteByte(ii, 0x0A); // Enable RAM
                device.MMU.WriteByte(0xA000, 0x1);
                device.MMU.WriteByte(0xBFFF, 0x2);
                Assert.Equal(0xFF, device.MMU.ReadByte(0xA000));
                Assert.Equal(0xFF, device.MMU.ReadByte(0xBFFF)); // Don't test whole range, just start and end
            }
        }

        /// <summary>
        /// Test that enable/disable RAM works as expected and that writing/reading from RAM works
        /// </summary>
        [Fact]
        public void TestRamAccessOneBank()
        {
            var cartridgeContents = new byte[0x8000];
            Array.Copy(_cartridgeBaseContents, cartridgeContents, _cartridgeBaseContents.Length);
            cartridgeContents[0x149] = (byte) CartridgeRAMSize.A2KB;
            var device = new Device(CartridgeFactory.CreateCartridge(cartridgeContents), DeviceMode.DMG);
            device.SkipBootRom();

            for (ushort ii = 0x0; ii < 0x2000; ii++)
            {
                device.MMU.WriteByte(ii, 0x0A); // Enable RAM
                device.MMU.WriteByte(0xA000, 0x1);
                device.MMU.WriteByte(0xBFFF, 0x2);
                Assert.Equal(0x1, device.MMU.ReadByte(0xA000));
                Assert.Equal(0x2, device.MMU.ReadByte(0xBFFF)); // Don't test whole range, just start and end

                device.MMU.WriteByte(ii, 0x00); // Disable RAM
                Assert.Equal(0xFF, device.MMU.ReadByte(0xA000));
                Assert.Equal(0xFF, device.MMU.ReadByte(0xBFFF)); // Don't test whole range, just start and end
            }
        }

        /// <summary>
        /// Test that RAM banking works as expected and that values can be written/read from the different banks
        /// </summary>
        [Fact]
        public void TestRamBanking()
        {
            var cartridgeContents = new byte[0x8000];
            Array.Copy(_cartridgeBaseContents, cartridgeContents, _cartridgeBaseContents.Length);
            cartridgeContents[0x149] = (byte)CartridgeRAMSize.A32KB;
            var device = new Device(CartridgeFactory.CreateCartridge(cartridgeContents), DeviceMode.DMG);
            device.SkipBootRom();

            device.MMU.WriteByte(0x0, 0x0A); // Enable RAM
            device.MMU.WriteByte(0x6000, 0x1); // Set RAM banking mode ON

            for (byte bank = 0; bank < device.Cartridge.RAMSize.NumberBanks(); bank++)
            {
                device.MMU.WriteByte(0x4000, bank); // Select RAM bank
                device.MMU.WriteByte(0xA000, (byte) (bank + 1));
            }

            for (byte bank = 0; bank < device.Cartridge.RAMSize.NumberBanks(); bank++)
            {
                device.MMU.WriteByte(0x4000, bank); // Select RAM bank
                Assert.Equal(bank + 1, device.MMU.ReadByte(0xA000));
            }
        }

        [Fact]
        public void TestRomBanking()
        {
            var cartridgeContents = new byte[0x8000];
            Array.Copy(_cartridgeBaseContents, cartridgeContents, _cartridgeBaseContents.Length);
            cartridgeContents[0x7FFF] = 0x9; // Set a random value in ROM bank 1
            var device = new Device(CartridgeFactory.CreateCartridge(cartridgeContents), DeviceMode.DMG);

            Assert.Equal(CartridgeROMSize.A64KB,device.Cartridge.ROMSize);
            Assert.Equal(CartridgeRAMSize.None, device.Cartridge.RAMSize);

            device.SkipBootRom();
            Assert.Equal(0xC3, device.MMU.ReadByte(0x0101)); // Check one fixed value from header to make sure bank 0 readable
            device.MMU.WriteByte(0x2000, 0x0); // Note that this sets rom bank to 1 not 0
            Assert.Equal(9, device.MMU.ReadByte(0x7FFF));
        }
    }
}
