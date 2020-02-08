namespace Gameboy.VM.LCD
{
    public class NullRenderer : IRenderer
    {
        public (byte, byte, byte) ColorAdjust(byte r, byte g, byte b)
        {
            return (r, g, b);
        }

        public void HandleVBlankEvent(byte[] frameBuffer, long tCycles)
        {
        }
    }
}
