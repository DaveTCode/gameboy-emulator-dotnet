using System;
using Gameboy.VM.Sound.Envelope;
using Gameboy.VM.Sound.Sweep;

namespace Gameboy.VM.Sound.Channels
{
    /// <summary>
    /// SOUND 1
    /// Rectangular waveform with sweep and envelope functions
    /// </summary>
    internal class SquareChannel1 : SquareWave.SquareWave
    {
        internal SquareChannel1()
        {
            Sweep = new FrequencySweep(this);
            Envelope = new SoundEnvelope(this);
        }

        // NR10 register
        internal FrequencySweep Sweep { get; }

        // NR12 register
        internal SoundEnvelope Envelope { get; }

        private int _currentFrequencyPeriod;
        private int _lastOutput;

        internal override void Reset()
        {
            base.Reset();
            Sweep.Reset();
            Envelope.Reset();
        }

        internal override void Step()
        {
            _currentFrequencyPeriod--;
            if (_currentFrequencyPeriod < 0)
            {
                _currentFrequencyPeriod = FrequencyPeriod;

                if (IsEnabled)
                {
                    _lastOutput = NextDutyCycleValue();
                }
            }
        }

        internal override void Trigger()
        {
            base.Trigger();
            _currentFrequencyPeriod = FrequencyPeriod;
            _lastOutput = 0;
            Envelope.Trigger();
            Sweep.Trigger(FrequencyPeriod);

            Console.WriteLine("Triggering sound 1 with frequency {0}Hz period {1}, envelope ({2}), sweep ({3})", ActualFrequencyHz, FrequencyPeriod, Envelope, Sweep);
        }

        internal override void SkipBootRom()
        {
            Sweep.Register = 0x80;
            ControlByte = 0xBF;
            Envelope.Register = 0xF3;
            LowByte = 0xFF;
            HighByte = 0xBF; // Note that this triggers the channel so we disable it immediately after
            IsEnabled = false;
        }

        internal override int GetOutputVolume()
        {
            return _lastOutput * Envelope.Volume;
        }
    }
}
