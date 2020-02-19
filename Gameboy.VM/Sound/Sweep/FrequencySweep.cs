using Gameboy.VM.Sound.Channels;

namespace Gameboy.VM.Sound.Sweep
{
    internal class FrequencySweep
    {
        private readonly SquareChannel1 _sound;

        internal FrequencySweep(SquareChannel1 sound)
        {
            _sound = sound;
        }

        private const byte RegisterMask = 0b1000_0000;

        private bool _isEnabled;

        private bool _isSweepDecrease;
        private int _sweepPeriod;
        private int _sweepShiftNumber;
        private int _shadowRegister;

        private int _currentPeriod;

        internal byte Register
        {
            get =>
                (byte)(RegisterMask |
                        (_isSweepDecrease ? 0x8 : 0x0) |
                        (_sweepPeriod << 4) |
                        _sweepShiftNumber);
            set
            {
                _sweepShiftNumber = value & 0x7;
                _isSweepDecrease = (value & 0x8) == 0x8;
                _sweepPeriod = (value >> 4) & 0x7;
            }
        }

        internal void Trigger(int squareWaveFrequency)
        {
            _shadowRegister = squareWaveFrequency;
            _currentPeriod = _sweepPeriod;

            _isEnabled = (_currentPeriod != 0 || _sweepShiftNumber != 0);

            if (_sweepShiftNumber != 0)
            {
                SweepCalculation();
            }
        }

        internal void Reset()
        {
            _sweepShiftNumber = 0x0;
            _isSweepDecrease = false;
            _sweepPeriod = 0;
        }

        internal void Step()
        {
            _currentPeriod--;
            if (_currentPeriod == 0)
            {
                _currentPeriod = _sweepPeriod;
                if (_currentPeriod == 0) _currentPeriod = 8;

                if (_isEnabled && _sweepPeriod > 0)
                {
                    SweepCalculation();
                }
            }
        }

        private void SweepCalculation()
        {
            var workingValue = _shadowRegister >> _sweepShiftNumber;
            if (_isSweepDecrease)
            {
                workingValue = _shadowRegister - workingValue;
            }
            else
            {
                workingValue = _shadowRegister + workingValue;
            }

            if (workingValue > 2047)
            {
                _sound.IsEnabled = false;
            }

            _shadowRegister = workingValue;
            _sound.FrequencyData = workingValue;
        }
    }
}
