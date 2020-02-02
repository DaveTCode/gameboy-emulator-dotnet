using Gameboy.VM.Sound.Channels;

namespace Gameboy.VM.Sound.SquareWave
{
    /// <summary>
    /// Both sound 1 & 2 are square waves, this provides the common code to
    /// represent the square wave in both cases.
    /// </summary>
    internal abstract class SquareWave : BaseChannel
    {
        private const byte ControlByteMask = 0b0011_1111;
        private const byte HighByteMask = 0b1011_1111;

        internal int FrequencyData { get; set; }

        protected int ActualFrequencyHz => 131072 / (2048 - FrequencyData);

        protected int FrequencyPeriod => 4 * (2048 - FrequencyData);

        protected WaveDuty DutyCycle { get; private set; }

        private int _dutyCycleBit;

        internal byte ControlByte
        {
            get => (byte)(
                ControlByteMask |
                ((int)DutyCycle << 6));
            set
            {
                DutyCycle = (WaveDuty)(value >> 6);
                SoundLength = 64 - (value & ControlByteMask);
            }
        }

        internal byte LowByte
        {
            get => (byte)FrequencyData;
            set => FrequencyData = (FrequencyData & 0x700) | value;
        }

        internal byte HighByte
        {
            get => (byte)(HighByteMask | ((FrequencyData & 0x700) >> 8));
            set
            {
                FrequencyData = (FrequencyData & 0xFF) | ((value & 0x7) << 8); // Set upper 3 bits of frequencyData
                UseSoundLength = (value & 0x40) == 0x40;
                if ((value & 0x80) == 0x80)
                {
                    Trigger();
                }
            }
        }

        internal virtual void Trigger()
        {
            IsEnabled = true;
            if (SoundLength == 0)
            {
                SoundLength = 0x3F;
            }

            _dutyCycleBit = 0;
        }

        internal override void Reset()
        {
            IsEnabled = false;
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
