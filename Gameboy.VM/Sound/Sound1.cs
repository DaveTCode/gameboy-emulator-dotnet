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
        internal Sound1()
        {
            Sweep = new FrequencySweep(this);
            Envelope = new SoundEnvelope();
        }

        // NR10 register
        internal FrequencySweep Sweep { get; }

        // NR12 register
        internal SoundEnvelope Envelope { get; }

        private int _frequencyPeriod;
        private int _lastOutput;

        private int SoundFrequency => 131072 / (2048 - FrequencyData);

        internal override void Reset()
        {
            base.Reset();
            Sweep.Reset();
            Envelope.Reset();
        }

        internal override void Step()
        {
            _frequencyPeriod--;
            if (_frequencyPeriod < 0)
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
            Sweep.Trigger(SoundFrequency);
        }

        internal override int GetOutputVolume()
        {
            return _lastOutput * Envelope.Volume;
        }
    }
}
