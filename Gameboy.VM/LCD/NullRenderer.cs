namespace Gameboy.VM.LCD
{
    public class NullRenderer : IRenderer
    {
        private readonly DeviceType _deviceType;

        public NullRenderer(DeviceType deviceType)
        {
            _deviceType = deviceType;
        }

        public (byte, byte, byte) ColorAdjust(byte r, byte g, byte b)
        {
            if (_deviceType == DeviceType.DMG)
            {
                return (r, g, b);
            }

            return ((byte, byte, byte))(
                (r * 13 + g * 2 + b) >> 1,
                (g * 3 + b) << 1,
                (r * 3 + g * 2 + b * 11) >> 1
            );
        }

        public void HandleVBlankEvent(byte[] frameBuffer, long tCycles)
        {

        }
    }
}
