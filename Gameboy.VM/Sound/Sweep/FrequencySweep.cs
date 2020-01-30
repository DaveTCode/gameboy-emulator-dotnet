namespace Gameboy.VM.Sound.Sweep
{
    internal class FrequencySweep
    {
        private const byte RegisterMask = 0b1000_0000;
        private const int Divider = Device.CyclesPerSecondHz / 128;

        #region Internal state tracking
        private int _internalCounter;
        #endregion

        private byte _registerValue = RegisterMask;
        internal byte Register
        {
            get => _registerValue;
            set
            {
                _registerValue = (byte)(RegisterMask | value);
                SweepShiftNumber = value & 0x7;
                SweepIncreaseDecrease = (value & 0x8) == 0x8 ? SweepIncreaseDecrease.Subtraction : SweepIncreaseDecrease.Addition;
                SweepTime = (SweepTime)((value >> 4) & 0x8);
            }
        }

        internal int SweepShiftNumber { get; private set; }

        internal SweepIncreaseDecrease SweepIncreaseDecrease { get; private set; }

        internal SweepTime SweepTime { get; private set; }

        internal void Reset()
        {
            _registerValue = RegisterMask;
            SweepShiftNumber = 0x0;
            SweepIncreaseDecrease = SweepIncreaseDecrease.Addition;
            SweepTime = SweepTime.Off;
        }

        internal void Step()
        {
            _internalCounter++;

            if (_internalCounter == Divider)
            {
                _internalCounter = 0;

                // TODO - More implementation to do here
            }
        }
    }
}
