using System;
using System.Collections.Generic;
using System.Text;

namespace Gameboy.VM.LCD
{
    internal class Sprite
    {
        internal SpriteToBgPriority SpriteToBgPriority { get; set; }

        internal int Y { get; set; }

        internal int X { get; set; }

        internal int TileNumber { get; set; }

        internal bool YFlip { get; set; }

        internal bool XFlip { get; set; }

        internal bool UsePalette1 { get; set; }
    }
}
