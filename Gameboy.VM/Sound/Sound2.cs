using Gameboy.VM.Sound.Envelope;

namespace Gameboy.VM.Sound
{
    /// <summary>
    /// SOUND 2
    /// Rectangular waveform with envelope but no sweep function
    /// </summary>
    internal class Sound2 : SquareWave.SquareWave
    {
        // NR22 Register
        internal SoundEnvelope Envelope { get; } = new SoundEnvelope();

        private int _frequencyPeriod;
        private int _lastOutput;

        private int SoundFrequency => 131072 / (2048 - FrequencyData);

        internal override void Reset()
        {
            base.Reset();
            Envelope.Reset();
        }

        internal override void Step()
        {
            _frequencyPeriod--;
            if (_frequencyPeriod == 0)
            {
                _frequencyPeriod = SoundFrequency;

                if (IsEnabled)
                {
                    _lastOutput = NextDutyCycleValue();
                }
            }
        }

        internal override void Trigger()
        {
            base.Trigger();
            _frequencyPeriod = SoundFrequency;
            _lastOutput = 0;
            Envelope.Trigger();
        }

        internal override int GetOutputVolume()
        {
            return _lastOutput * Envelope.Volume;
        }
    }
}
