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

        private int _frequencyDivider;
        private int _lastOutput;

        private int FrequencyDividerStart => (2048 - FrequencyData) * 4;

        internal override void Reset()
        {
            base.Reset();
            Envelope.Reset();
        }

        internal override void Step()
        {
            _frequencyDivider--;
            if (_frequencyDivider == 0)
            {
                _frequencyDivider = FrequencyDividerStart;

                if (IsEnabled)
                {
                    _lastOutput = NextDutyCycleValue();
                }
            }
        }

        internal override void Trigger()
        {
            base.Trigger();
            _frequencyDivider = FrequencyDividerStart;
            _lastOutput = 0;
        }

        internal override int GetOutputVolume()
        {
            return _lastOutput * Envelope.Volume;
        }
    }
}
