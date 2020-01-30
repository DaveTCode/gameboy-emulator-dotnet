using Gameboy.VM.Sound.Envelope;
using Gameboy.VM.Sound.Sweep;

namespace Gameboy.VM.Sound
{
    /// <summary>
    /// SOUND 1
    /// Rectangular waveform with sweep and envelope functions
    /// </summary>
    internal class Sound1 : SquareWave.SquareWave
    {
        // NR10 register
        internal FrequencySweep Sweep { get; } = new FrequencySweep();

        // NR12 register
        internal SoundEnvelope Envelope { get; } = new SoundEnvelope();

        private int _frequencyDivider;
        private int _lastOutput;

        private int FrequencyDividerStart => (2048 - FrequencyData) * 4;

        internal override void Reset()
        {
            base.Reset();
            Sweep.Reset();
            Envelope.Reset();
        }

        internal override void Step()
        {
            _frequencyDivider--;
            if (_frequencyDivider < 0)
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
