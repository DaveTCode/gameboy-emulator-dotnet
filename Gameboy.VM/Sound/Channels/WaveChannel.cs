namespace Gameboy.VM.Sound.Channels
{
    /// <summary>
    /// SOUND 3
    /// Outputs waveforms from waveform RAM
    /// </summary>
    internal class WaveChannel : BaseChannel
    {
        private const byte NR30Mask = 0b0111_1111;
        private const byte NR32Mask = 0b1001_1111;
        private const byte NR34Mask = 0b1011_1111; // TODO - Doesn't match official programming manual but does match other emulators

        private const int WaveRAMSize = 0x10;
        internal readonly byte[] WaveRam = new byte[WaveRAMSize];

        internal OutputLevel Volume { get; private set; }

        internal int FrequencyData { get; private set; }

        private byte _nr30 = NR30Mask;
        internal byte NR30
        {
            get => _nr30;
            set
            {
                _nr30 = (byte)(value | NR30Mask);
                IsEnabled = (value & 0x80) == 0x80;
            }
        }

        private byte _nr31;
        internal byte NR31
        {
            get => _nr31;
            set
            {
                _nr31 = value;
                SoundLength = 256 - value;
            }
        }

        private byte _nr32 = NR32Mask;
        internal byte NR32
        {
            get => _nr32;
            set
            {
                _nr32 = (byte)(value | NR32Mask);
                Volume = (OutputLevel)((value >> 5) & 0x3);
            }
        }

        private byte _nr33;
        internal byte NR33
        {
            get => _nr33;
            set
            {
                _nr33 = value;
                FrequencyData = (FrequencyData & 0x700) | value;
            }
        }

        private byte _nr34 = NR34Mask;
        internal byte NR34
        {
            get => _nr34;
            set
            {
                _nr34 = (byte)(value | NR34Mask);
                FrequencyData = (FrequencyData & 0xFF) | ((value & 0x7) << 8);
                UseSoundLength = (value & 0x40) == 0x40;
                IsEnabled = (value & 0x80) == 0x80;
                if (IsEnabled)
                {
                    Trigger();
                }
            }
        }

        internal void Trigger()
        {
            // TODO - Unimplemented
        }

        internal override void Reset()
        {
            _nr30 = NR30Mask;
            _nr31 = 0x00;
            _nr32 = NR32Mask;
            _nr33 = 0x00;
            _nr34 = NR34Mask;
            UseSoundLength = false;
            FrequencyData = 0x0;
            SoundLength = 0x0;
            Volume = OutputLevel.Mute;
            IsEnabled = false;
        }

        internal override void Step()
        {
            // TODO - Handle channel 3 properly
        }

        internal override int GetOutputVolume()
        {
            return 0x0; // TODO - Implement channel 3 properly
        }
    }
}
