using Gameboy.VM.LCD;
using Xunit;

namespace Gameboy.VM.Tests.LCD
{
    public class CGBPaletteTests
    {
        [Theory]
        [InlineData(0xFF, 0xFF, 0x1F, 0x1F, 0x1F)]
        [InlineData(0xFF, 0x00, 0x1F, 0x7, 0x0)]
        [InlineData(0x00, 0xFF, 0x00, 0x18, 0x1F)]
        [InlineData(0x4A, 0x29, 0x0A, 0x0A, 0x0A)]
        public void TestSetRGBValues(byte low, byte high, byte r, byte g, byte b)
        {
            var p = new CGBPalette {PaletteIndex = 0};

            p.WritePaletteMemory(low);
            p.PaletteIndex = 1;
            p.WritePaletteMemory(high);

            Assert.Equal(r, p.Palette[0].Item1);
            Assert.Equal(g, p.Palette[0].Item2);
            Assert.Equal(b, p.Palette[0].Item3);
        }
    }
}
