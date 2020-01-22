using System.Linq;
using System.Text;
using Gameboy.VM.Cartridge;
using Xunit;

namespace Gameboy.VM.Tests.Cartridge
{
    public class RomOnlyCartridgeTests
    {
        private readonly byte[] _cartridgeHeader = new byte[0x100].Concat(new byte[]
        {
            0x00, 0xC3, 0x50, 0x01, 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, // 0x100-0x10F 
            0x00, 0x0C, 0x00, 0x0D, 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, // 0x110-0x11F
            0xDD, 0xDD, 0xD9, 0x99, 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, // 0x120-0x12F 
            0xBB, 0xB9, 0x33, 0x3E, 0x54, 0x45, 0x53, 0x54, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 0x130-0x13F
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0xAA, 0x01, 0xFD  // 0x140-0x14F
        }).ToArray();

        [Fact]
        public void TestBasicProperties()
        {
            var cartridge = CartridgeFactory.CreateCartridge(_cartridgeHeader);

            Assert.Equal("TEST\0\0\0\0\0\0\0", cartridge.GameTitle);
            Assert.Equal("\0\0\0\0", cartridge.ManufacturerCode);
            Assert.Equal(CGBSupportCode.CGBIncompatible, cartridge.CGBSupportCode);
            Assert.Equal(Encoding.ASCII.GetString(new byte[]{ 0x01 }), cartridge.MakerCode);
            Assert.Equal(SGBSupportCode.GameboyCompatible, cartridge.SGBSupportCode);
            Assert.IsType<RomOnlyCartridge>(cartridge);
            Assert.Equal(CartridgeROMSize.A32KB, cartridge.ROMSize);
            Assert.Equal(CartridgeRAMSize.None, cartridge.RAMSize);
            Assert.Equal(CartridgeDestinationCode.NonJapanese, cartridge.DestinationCode);
            Assert.Equal(0x0, cartridge.RomVersion);
        }

        [Fact]
        public void TestRamDoesNotExist()
        {
            var cartridge = CartridgeFactory.CreateCartridge(_cartridgeHeader);
            var device = new Device(cartridge, DeviceMode.DMG);
            device.SkipBootRom();

            // Test that all writes to RAM are ignored leaving 0xFF as default value
            for (ushort ii = 0xA000; ii < 0xC000; ii++)
            {
                device.MMU.WriteByte(ii, 0x1);
                Assert.Equal(0xFF, device.MMU.ReadByte(ii));
            }
        }

        [Fact]
        public void TestRomAddressSpace()
        {
            var cartridge = CartridgeFactory.CreateCartridge(_cartridgeHeader);
            var device = new Device(cartridge, DeviceMode.DMG);
            device.SkipBootRom();

            // Test that all writes to ROM are ignored and original values retained
            for (ushort ii = 0; ii < 0x7FFF; ii++)
            {
                var original = device.MMU.ReadByte(ii);
                device.MMU.WriteByte(ii, 0x50);
                Assert.Equal(original, device.MMU.ReadByte(ii));
            }
        }
    }
}
