using System;

namespace Gameboy.VM.LCD
{

    public enum Grayscale
    {
        White = 0x0,
        LightGray = 0x1,
        DarkGray = 0x2,
        Black = 0x3
    }

    public static class GrayscaleExtensions
    {
        public static (byte, byte, byte) GrayscaleWhite = (255, 255, 255);
        public static (byte, byte, byte) GrayscaleLightGray = (192, 192, 192);
        public static (byte, byte, byte) GrayscaleDarkGray = (96, 96, 96);
        public static (byte, byte, byte) GrayscaleBlack = (0, 0, 0);

        public static (byte, byte, byte) BaseRgb(this Grayscale grayscale) => grayscale switch
        {
            Grayscale.White => GrayscaleWhite,
            Grayscale.LightGray => GrayscaleLightGray,
            Grayscale.DarkGray => GrayscaleDarkGray,
            Grayscale.Black => GrayscaleBlack,
            _ => throw new ArgumentOutOfRangeException(nameof(grayscale), grayscale, null)
        };
    }
}