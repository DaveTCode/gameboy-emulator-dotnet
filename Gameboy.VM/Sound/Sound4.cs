using Gameboy.VM.Sound.Envelope;

namespace Gameboy.VM.Sound
{
    /// <summary>
    /// SOUND 4
    /// Generates white noise with an envelope function
    /// </summary>
    internal class Sound4 : BaseSound
    {
        private const int NR41Mask = 0b1100_0000;
        private const int NR44Mask = 0b0011_1111;

        internal int FrequencyDivisionRatio { get; private set; }

        internal int PolynomialCounterSteps { get; private set; }

        internal int PolynomialCounterShiftClockFrequency { get; private set; }

        private byte _nr41 = NR41Mask;
        internal byte NR41
        {
            get => _nr41;
            set
            {
                _nr41 = (byte)(value | NR41Mask);
                SoundLength = 64 - (value & 0b0011_1111);
            }
        }

        internal SoundEnvelope Envelope { get; } = new SoundEnvelope();

        private byte _nr43;

        internal byte NR43
        {
            get => _nr43;
            set
            {
                _nr43 = value;
                FrequencyDivisionRatio = value & 0x7;
                PolynomialCounterSteps = (value & 0x8) == 0x8 ? 7 : 15;
                PolynomialCounterShiftClockFrequency = (value & 0xF0) >> 4;
            }
        }

        private byte _nr44 = NR44Mask;
        internal byte NR44
        {
            get => _nr44;
            set
            {
                _nr44 = (byte) (value | NR44Mask);
                UseSoundLength = (value & 0x40) == 0x40;
                IsEnabled = (value & 0x80) == 0x80;
                if (IsEnabled)
                {
                    Trigger();
                }
            }
        }

        internal override void Trigger()
        {
            IsEnabled = true;
            if (SoundLength == 0)
            {
                SoundLength = 64;
            }

            // TODO - More to do here
        }

        internal override void Reset()
        {
            _nr41 = NR41Mask;
            Envelope.Reset();
            _nr43 = 0x0;
            _nr44 = NR44Mask;
            IsEnabled = false;
            SoundLength = 0x0;
            UseSoundLength = false;
            FrequencyDivisionRatio = 0x0;
            PolynomialCounterSteps = 0x0;
            PolynomialCounterShiftClockFrequency = 0x0;
        }

        internal override void Step()
        {
            // TODO - Implement sound 4 properly
        }

        internal override int GetOutputVolume()
        {
            return 0x0; // TODO - Implement sound 4 properly
        }
    }
}
