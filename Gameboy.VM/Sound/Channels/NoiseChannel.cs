using System;
using Gameboy.VM.Sound.Envelope;

namespace Gameboy.VM.Sound.Channels
{
    /// <summary>
    /// SOUND 4
    /// Generates white noise with an envelope function
    /// </summary>
    internal class NoiseChannel : BaseChannel
    {
        internal NoiseChannel(Device device) : base(device)
        {
            Envelope = new SoundEnvelope(this);
        }

        protected override int BaseSoundLength => 64;

        internal byte NR41
        {
            get => 0xFF;
            set => SoundLength = 64 - (value & 0b0011_1111);
        }

        internal SoundEnvelope Envelope { get; }

        private int _internalTimerPeriod;
        private int _currentTimerCycle;
        private int _lfsr;
        private bool _widthModeOn;
        private int _outputVolume;

        private byte _nr43;
        internal byte NR43
        {
            get => _nr43;
            set
            {
                _nr43 = value;
                var clockShift = value >> 4;
                _widthModeOn = (value & 0x8) == 0x8;
                var divisor = (value & 0x7) switch
                {
                    0 => 8,
                    1 => 16,
                    2 => 32,
                    3 => 48,
                    4 => 64,
                    5 => 80,
                    6 => 96,
                    7 => 112,
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Coding error in calculating divisor for sound channel 4")
                };
                _internalTimerPeriod = divisor << clockShift;
                _currentTimerCycle = 1;
            }
        }

        internal byte NR44
        {
            get =>
                (byte)(0xBF |
                        (UseSoundLength ? 0x40 : 0x0) |
                        (IsEnabled ? 0x80 : 0x0));
            set
            {
                UseSoundLength = (value & 0x40) == 0x40;
                if ((value & 0x80) == 0x80)
                {
                    Trigger();
                }
            }
        }

        internal override void Trigger()
        {
            base.Trigger();

            _currentTimerCycle = _internalTimerPeriod;
            _lfsr = 0x7FFF;
            Envelope.Trigger();
            Device.Log.Information("Triggering Noise Channel with period {0}, length ({1}) enabled {2}, volume: ({3})", _internalTimerPeriod, SoundLength, UseSoundLength, Envelope);
        }

        internal override void SkipBootRom()
        {
            NR41 = 0xFF;
            Envelope.Register = 0x0;
            NR43 = 0x0;
            NR44 = 0xBF;
            IsEnabled = false;
        }

        internal override void Reset()
        {
            base.Reset();
            Envelope.Reset();
            _internalTimerPeriod = 0;
            _currentTimerCycle = 1;
            _lfsr = 0x7FFF;
            _widthModeOn = false;
            _outputVolume = 0;
        }

        internal override void Step()
        {
            _currentTimerCycle--;
            if (_currentTimerCycle < 0)
            {
                _currentTimerCycle = _internalTimerPeriod;

                var xorBit = (_lfsr & 0x1) ^ ((_lfsr & 0x2) >> 1);
                _lfsr >>= 1;
                _lfsr |= (xorBit << 14);
                if (_widthModeOn)
                {
                    _lfsr |= (xorBit << 6);
                }

                _outputVolume = (_lfsr & 0x1) == 0x1 ? 0 : 0x1;
            }
        }

        internal override int GetOutputVolume()
        {
            return _outputVolume * Envelope.Volume;
        }
    }
}
