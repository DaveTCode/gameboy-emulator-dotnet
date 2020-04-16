using System.Collections.Generic;

namespace Gameboy.VM.LCD
{
    internal class DMGSpriteComparer : IComparer<Sprite>
    {
        public int Compare(Sprite x, Sprite y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            if (x.X == y.X) return 0;
            if (x.X < y.X) return -1;
            
            return 1;
        }
    }
}
