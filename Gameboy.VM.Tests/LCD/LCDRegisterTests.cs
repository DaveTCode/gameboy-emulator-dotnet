using Gameboy.VM.LCD;
using Xunit;

namespace Gameboy.VM.Tests.LCD
{
    /// <summary>
    /// Low level tests of setting register values in the LCD unit
    /// </summary>
    public class LCDRegisterTests
    {
        [Theory]
        [InlineData(0x0, 0x84, false, false, false, false, (int)StatMode.HBlankPeriod)]
        [InlineData(0x1, 0x84, false, false, false, false, (int)StatMode.HBlankPeriod)] // Can't set mode through stat register writes
        [InlineData(0xFF, 0xFC, true, true, true, true, (int)StatMode.HBlankPeriod)] // Can't set mode or LY=LYC through stat register writes
        public void TestSTATRegisterChanges(byte statRegisterSet, byte statRegisterActual, bool hblankEnabled, bool vblankEnabled, bool oamEnabled, bool lylcEnabled, int statModeValue)
        {
            var statMode = (StatMode) statModeValue;
            var device = TestUtils.CreateTestDevice();
            device.LCDRegisters.StatRegister = statRegisterSet;

            Assert.Equal(statRegisterActual, device.LCDRegisters.StatRegister);

            Assert.Equal(statMode, device.LCDRegisters.StatMode);
            Assert.Equal(hblankEnabled, device.LCDRegisters.Mode0HBlankCheckEnabled);
            Assert.Equal(vblankEnabled, device.LCDRegisters.Mode1VBlankCheckEnabled);
            Assert.Equal(oamEnabled, device.LCDRegisters.Mode2OAMCheckEnabled);
            Assert.Equal(lylcEnabled, device.LCDRegisters.IsLYLCCheckEnabled);
        }

        [Theory]
        [InlineData((int)StatMode.HBlankPeriod, 0x84)]
        [InlineData((int)StatMode.VBlankPeriod, 0x85)]
        [InlineData((int)StatMode.OAMRAMPeriod, 0x86)]
        [InlineData((int)StatMode.TransferringDataToDriver, 0x87)]
        public void TestSTATRegisterAfterModeChanges(int statModeValue, byte expectedStatRegisterValue)
        {
            var statMode = (StatMode)statModeValue;
            var device = TestUtils.CreateTestDevice();
            device.LCDRegisters.StatMode = statMode;
            Assert.Equal(statMode, device.LCDRegisters.StatMode);
            Assert.Equal(expectedStatRegisterValue | 0b10000000, device.LCDRegisters.StatRegister);
        }

        [Theory]
        [InlineData(0xFF, true, 0x9C00, true, 0x8000, 0x9C00, true, true, true)]
        [InlineData(0x00, false, 0x9800, false, 0x8800, 0x9800, false, false, false)]
        [InlineData(0x01, false, 0x9800, false, 0x8800, 0x9800, false, false, true)]
        [InlineData(0x02, false, 0x9800, false, 0x8800, 0x9800, false, true, false)]
        [InlineData(0x04, false, 0x9800, false, 0x8800, 0x9800, true, false, false)]
        [InlineData(0x08, false, 0x9800, false, 0x8800, 0x9C00, false, false, false)]
        [InlineData(0x10, false, 0x9800, false, 0x8000, 0x9800, false, false, false)]
        [InlineData(0x20, false, 0x9800, true, 0x8800, 0x9800, false, false, false)]
        [InlineData(0x40, false, 0x9c00, false, 0x8800, 0x9800, false, false, false)]
        [InlineData(0x80, true, 0x9800, false, 0x8800, 0x9800, false, false, false)]
        public void TestLCDCRegisterChanges(byte lcdcValue, bool isLcdOn, ushort windowTileMap, bool isWindowEnabled,
            ushort bgWindowTileData, ushort bgTileMap, bool largeSprites, bool spritesEnabled, bool bgEnabled)
        {
            var device = TestUtils.CreateTestDevice();
            device.LCDRegisters.LCDControlRegister = lcdcValue;

            Assert.Equal(isLcdOn, device.LCDRegisters.IsLcdOn); // Bit 7
            Assert.Equal(windowTileMap, device.LCDRegisters.WindowTileMapOffset); // Bit 6
            Assert.Equal(isWindowEnabled, device.LCDRegisters.IsWindowEnabled); // Bit 5
            Assert.Equal(bgWindowTileData, device.LCDRegisters.BackgroundAndWindowTilesetOffset); // Bit 4
            Assert.Equal(bgTileMap, device.LCDRegisters.BackgroundTileMapOffset); // Bit 3
            Assert.Equal(largeSprites, device.LCDRegisters.LargeSprites); // Bit 2
            Assert.Equal(spritesEnabled, device.LCDRegisters.AreSpritesEnabled); // Bit 1
            Assert.Equal(bgEnabled, device.LCDRegisters.IsBackgroundEnabled); // Bit 0
        }
    }
}
