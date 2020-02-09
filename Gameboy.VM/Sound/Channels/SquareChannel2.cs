using System;
using Gameboy.VM.Sound.Envelope;

namespace Gameboy.VM.Sound.Channels
{
    /// <summary>
    /// SOUND 2
    /// Rectangular waveform with envelope but no sweep function
    /// </summary>
    internal class SquareChannel2 : SquareWave.SquareWave
    {
        internal SquareChannel2()
        {
            Envelope = new SoundEnvelope(this);
        }

        // NR22 Register
        internal SoundEnvelope Envelope { get; }

        private int _frequencyPeriod;
        private int _lastOutput;

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
                _frequencyPeriod = FrequencyPeriod;

                if (IsEnabled)
                {
                    _lastOutput = NextDutyCycleValue();
                }
            }
        }

        internal override void Trigger()
        {
            base.Trigger();
            _frequencyPeriod = FrequencyPeriod;
            _lastOutput = 0;
            Envelope.Trigger();
            Console.WriteLine("Triggering sound 2 with frequency {0}Hz period {1}, envelope ({2})", ActualFrequencyHz, FrequencyPeriod, Envelope);
        }

        internal override void SkipBootRom()
        {
            ControlByte = 0x3F;
            Envelope.Register = 0x0;
            HighByte = 0xBF;
            IsEnabled = false;
        }

        internal override int GetOutputVolume()
        {
            return _lastOutput * Envelope.Volume;
        }
    }
}
