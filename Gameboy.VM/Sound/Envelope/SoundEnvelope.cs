namespace Gameboy.VM.Sound.Envelope
{
    /// <summary>
    /// The envelope on different sound channels behaves consistently so the
    /// code is shared here.
    /// </summary>
    internal class SoundEnvelope
    {
        private byte _registerValue;

        internal int LengthOfEnvelopeSteps { get; private set; }

        internal EnvelopeUpDown EnvelopeUpDown { get; private set; }

        internal int InitialVolume { get; private set; }

        internal int Volume;

        private int _internalTimer;

        internal byte Register
        {
            get => _registerValue;
            set
            {
                _registerValue = value;
                LengthOfEnvelopeSteps = value & 0x7;
                EnvelopeUpDown = (value & 0x8) == 0x8 ? EnvelopeUpDown.Amplify : EnvelopeUpDown.Attenuate;
                InitialVolume = value >> 4;
                Volume = InitialVolume;
            }
        }

        internal void Reset()
        {
            LengthOfEnvelopeSteps = 0x0;
            EnvelopeUpDown = EnvelopeUpDown.Attenuate;
            InitialVolume = 0x0;
            _registerValue = 0x0;
        }

        internal void Step()
        {
            if ((EnvelopeUpDown == EnvelopeUpDown.Amplify && Volume == 15) ||
                (EnvelopeUpDown == EnvelopeUpDown.Attenuate && Volume == 0))
            {
                return;
            }

            _internalTimer++;
            if (_internalTimer == LengthOfEnvelopeSteps * Device.CyclesPerSecondHz / 64)
            {
                _internalTimer = 0;
                Volume += EnvelopeUpDown == EnvelopeUpDown.Amplify ? 1 : -1;
            }
        }
    }
}