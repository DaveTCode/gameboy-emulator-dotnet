namespace Gameboy.VM.LCD
{
    internal class Sprite
    {
        internal SpriteToBgPriority SpriteToBgPriority { get; set; }

        internal int Y { get; set; } = -16;

        internal int X { get; set; } = -8;

        internal int TileNumber { get; set; } = 0;

        internal bool YFlip { get; set; } = false;

        internal bool XFlip { get; set; } = false;

        internal bool UsePalette1 { get; set; } = false;
    }
}
