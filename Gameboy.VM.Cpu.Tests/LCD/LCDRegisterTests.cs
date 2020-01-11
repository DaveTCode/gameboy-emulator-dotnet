using Gameboy.VM.LCD;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Gameboy.VM.Tests.LCD
{
    /// <summary>
    /// Low level tests of setting register values in the LCD unit
    /// </summary>
    public class LCDRegisterTests
    {
        [Theory]
        [InlineData(0x0, 0x80, false, false, false, false, 0x0)]
        [InlineData(0x1, 0x80, false, false, false, false, 0x0)] // Can't set mode through stat register writes
        [InlineData(0xFF, 0xF8, true, true, true, true, 0x0)] // Can't set mode or LY=LYC through stat register writes
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
        [InlineData(0x0, 0x80)]
        [InlineData(0x1, 0x81)]
        [InlineData(0x2, 0x82)]
        [InlineData(0x3, 0x83)]
        public void TestSTATRegisterAfterModeChanges(int statModeValue, byte expectedStatRegisterValue)
        {
            var statMode = (StatMode)statModeValue;
            var device = TestUtils.CreateTestDevice();
            device.LCDRegisters.StatMode = statMode;
            Assert.Equal(statMode, device.LCDRegisters.StatMode);
            Assert.Equal(expectedStatRegisterValue, device.LCDRegisters.StatRegister);
        }
    }
}
