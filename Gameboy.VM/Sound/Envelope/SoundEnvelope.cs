using Gameboy.VM.Sound.Channels;

namespace Gameboy.VM.Sound.Envelope
{
    /// <summary>
    /// The envelope on different sound channels behaves consistently so the
    /// code is shared here.
    /// </summary>
    internal class SoundEnvelope
    {
        private readonly BaseChannel _channel;

        internal SoundEnvelope(BaseChannel channel)
        {
            _channel = channel;
        }

        private int _currentPeriod;
        internal int Period { get; private set; }

        internal EnvelopeUpDown EnvelopeUpDown { get; private set; }

        internal int InitialVolume { get; private set; }

        internal int Volume;

        internal byte Register
        {
            get =>
                (byte)(Period |
                        (EnvelopeUpDown == EnvelopeUpDown.Amplify ? 0x8 : 0x0) |
                        InitialVolume << 4);
            set
            {
                Period = value & 0x7;
                ResetCurrentPeriod();
                EnvelopeUpDown = (value & 0x8) == 0x8 ? EnvelopeUpDown.Amplify : EnvelopeUpDown.Attenuate;
                InitialVolume = value >> 4;
                Volume = InitialVolume;

                if ((value & 0b1111_1000) == 0)
                {
                    _channel.IsEnabled = false; // DAC disabled so turn sound off
                }
            }
        }

        internal void Reset()
        {
            Period = 0x0;
            ResetCurrentPeriod();
            EnvelopeUpDown = EnvelopeUpDown.Attenuate;
            InitialVolume = 0x0;
        }

        private void ResetCurrentPeriod()
        {
            _currentPeriod = Period == 0 ? 8 : Period;
        }

        internal void Step()
        {
            if ((EnvelopeUpDown == EnvelopeUpDown.Amplify && Volume == 15) ||
                (EnvelopeUpDown == EnvelopeUpDown.Attenuate && Volume == 0))
            {
                return;
            }

            _currentPeriod--;
            if (_currentPeriod == 0)
            {
                ResetCurrentPeriod();
                if (_currentPeriod == 0) _currentPeriod = 8; // Special case, 0 isn't a valid period
                Volume += EnvelopeUpDown == EnvelopeUpDown.Amplify ? 1 : -1;
            }
        }

        internal void Trigger()
        {
            ResetCurrentPeriod();
            Volume = InitialVolume;
        }

        public override string ToString()
        {
            return $"Initial Volume: {InitialVolume}, EnvelopeDirection {EnvelopeUpDown}, Period {Period}";
        }
    }
}