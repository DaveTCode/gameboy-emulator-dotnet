namespace Gameboy.VM.Sound.SquareWave
{
    /// <summary>
    /// Both sound 1 & 2 are square waves, this provides the common code to
    /// represent the square wave in both cases.
    /// </summary>
    internal abstract class SquareWave : BaseSound
    {
        private const byte ControlByteMask = 0b0011_1111;
        private const byte HighByteMask = 0b1011_1111;

        private byte _controlByte = ControlByteMask;
        private byte _lowByte;
        private byte _highByte = HighByteMask;

        internal int FrequencyData { get; private set; }

        internal WaveDuty DutyCycle { get; private set; }

        private int _dutyCycleBit;

        internal byte ControlByte
        {
            get => _controlByte;
            set
            {
                _controlByte = (byte)(value | ControlByteMask);
                DutyCycle = (WaveDuty)(value >> 6);
                SoundLength = 64 - (value & ControlByteMask);
                if ((value & 0x80) == 0x80)
                {
                    Trigger();
                }
            }
        }

        internal byte LowByte
        {
            get => _lowByte;
            set
            {
                _lowByte = value;
                FrequencyData = (FrequencyData & 0x700) | value;
            }
        }

        internal byte HighByte
        {
            get => _highByte;
            set
            {
                _highByte = (byte)(value | HighByteMask);
                FrequencyData = (FrequencyData & 0xFF) | ((value & 0x7) << 8); // Set upper 3 bits of frequencyData
                UseSoundLength = (value & 0x40) == 0x40;
            }
        }

        internal override void Trigger()
        {
            IsEnabled = true;
            if (SoundLength == 0)
            {
                SoundLength = 256;
            }

            _dutyCycleBit = 0;
        }

        internal override void Reset()
        {
            IsEnabled = false;
            _controlByte = ControlByteMask;
            _lowByte = 0x0;
            _highByte = 0x0;
            FrequencyData = 0x0;
            DutyCycle = WaveDuty.HalfQuarter;
            _dutyCycleBit = 0;
        }

        protected int NextDutyCycleValue()
        {
            var output = (DutyCycle.DutyByte() & (1 << _dutyCycleBit)) >> _dutyCycleBit;
            _dutyCycleBit = (_dutyCycleBit + 1) % 8;
            return output;
        }
    }
}
