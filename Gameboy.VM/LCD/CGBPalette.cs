namespace Gameboy.VM.LCD
{
    internal class CGBPalette
    {
        internal (int, int, int)[] Palette { get; } = new (int, int, int)[32]; // 8 palettes * 4 colors per palette

        private bool _autoIncrement;

        private byte _paletteIndex;
        internal byte PaletteIndex
        {
            get
            {
                if (_autoIncrement)
                {
                    return (byte) (_paletteIndex | 0x80 | 0x40);
                }

                return (byte) (_paletteIndex | 0x40);
            }
            set
            {
                _autoIncrement = (value & 0x80) == 0x80;
                _paletteIndex = (byte) (value & 0b111111); // Only bottom 6 bits reflect the actual palette index
            }
        }

        internal byte ReadPaletteMemory()
        {
            var colorIndex = _paletteIndex / 2;
            if (_paletteIndex % 2 == 0) // Low byte in pair
            {
                return (byte) (Palette[colorIndex].Item1 | (Palette[colorIndex].Item2 << 5)); // Red value + bottom 3 bits of green value
            }

            return (byte) ((Palette[colorIndex].Item2 >> 3) | (Palette[colorIndex].Item3 << 2)); // top 2 bits of green + blue
        }

        internal void WritePaletteMemory(byte value)
        {
            var colorIndex = _paletteIndex / 2;
            if (_paletteIndex % 2 == 0) // Low byte in pair
            {
                Palette[colorIndex].Item1 = value & 0b11111; // Red is the bottom 5 bits
                Palette[colorIndex].Item2 = (Palette[colorIndex].Item2 & 0b11000) | ((value & 0b11100000) >> 5); // Green has some bits in this byte
            }

            Palette[colorIndex].Item2 = (Palette[colorIndex].Item2 & 0b111) | ((value & 0b11) << 2); // Green has the rest of the bits in this byte
            Palette[colorIndex].Item3 = (value & 0b1111100) >> 2; // Blue is the bits 2-6 of the byte

            // Increment the index register if so configured
            if (_autoIncrement) PaletteIndex += 1;
        }
    }
}
