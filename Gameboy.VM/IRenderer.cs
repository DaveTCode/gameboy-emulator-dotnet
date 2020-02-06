namespace Gameboy.VM
{
    public interface IRenderer
    {
        /// <summary>
        /// Provides a means to map from Gameboy RGB to appropriate RGB values
        /// for the destination screen.
        /// </summary>
        public (byte, byte, byte) ColorAdjust(byte r, byte g, byte b);

        /// <summary>
        /// Handle a VBlank event and (normally) render the framebuffer to screen
        /// </summary>
        public void HandleVBlankEvent(byte[] frameBuffer, long tCycles);
    }
}
