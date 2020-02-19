using System;

namespace Gameboy.VM.Sound.Channels
{
    /// <summary>
    /// SOUND 3
    /// Outputs waveforms from waveform RAM
    /// </summary>
    internal class WaveChannel : BaseChannel
    {
        // These values are not fixed and vary slightly for each DMG device,
        // however this is a sensible default to use for the emulator
        // TODO - Vary this slightly on startup as per real DMG
        private static readonly byte[] DmgWave =
        {
            0xAC, 0xDD, 0xDA, 0x48, 0x36, 0x02, 0xCF, 0x16,
            0x2C, 0x04, 0xE5, 0x2C, 0xAC, 0xDD, 0xDA, 0x48
        };

        // This is actually correct, these values are fixed on startup of a CGB/SGB device
        private static readonly byte[] CgbWave =
        {
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF
        };

        protected override int BaseSoundLength => 0xFF;

        private const int WaveRAMSize = 0x10;
        private const int WaveSampleSize = WaveRAMSize * 2;
        private readonly byte[] _waveRam = new byte[WaveRAMSize];
        private readonly int[] _waveSamples = new int[WaveSampleSize]; // Using int to represent to avoid casts but actually 4 bits
        private int _waveSamplePositionCounter; // Which sample are we currently looking at?

        internal WaveChannelOutputLevel Volume { get; private set; }

        internal int FrequencyData { get; private set; }

        private int FrequencyPeriod => 2 * (2048 - FrequencyData);
        private int _currentFrequencyPeriod;
        private byte _sampleBuffer;

        internal WaveChannel(Device device) : base(device)
        {
            for (var ii = 0; ii < 0x10; ii++)
            {
                WriteRam((ushort)(ii + 0xFF30), device.Type == DeviceType.DMG ? DmgWave[ii] : CgbWave[ii]);
            }
        }

        internal byte ReadRam(ushort address)
        {
            // TODO - Documentation suggests that there are times that wave RAM can't be read and returns 0xFF
            return _waveRam[address - 0xFF30];
        }

        internal void WriteRam(ushort address, byte value)
        {
            var ramAddress = address - 0xFF30;
            _waveRam[ramAddress] = value;

            _waveSamples[ramAddress * 2] = value >> 4;
            _waveSamples[ramAddress * 2 + 1] = value & 0b1111;
        }

        /// <summary>
        /// Only bit 7 is used and indicates whether the DAC is on for this channel
        /// </summary>
        internal byte NR30
        {
            get => (byte)(0b0111_1111 | (IsEnabled ? 0b1000_0000 : 0));
            set => IsEnabled = (value & 0x80) == 0x80;
        }

        /// <summary>
        /// The wave channel has max sound length of 256, this register holds
        /// 256 - that value.
        /// </summary>
        internal byte NR31
        {
            get => (byte)(256 - SoundLength);
            set => SoundLength = 256 - value;
        }

        /// <summary>
        /// Masked with 0b1001_1111, holds only the volume output shifter in
        /// bits 5 & 6.
        /// </summary>
        internal byte NR32
        {
            get => (byte)(0b1001_1111 | (int)Volume << 5);
            set => Volume = (WaveChannelOutputLevel)((value >> 5) & 0x3);
        }

        /// <summary>
        /// Holds LSB of the Frequency Data
        /// </summary>
        internal byte NR33
        {
            get => (byte)FrequencyData;
            set => FrequencyData = (FrequencyData & 0x700) | value;
        }

        /// <summary>
        /// Holds the MSB of the frequency data in bits 1-3, bit 6 indicates
        /// whether the sound length is used and bit 7 indicates whether the
        /// channel should be triggered.
        /// </summary>
        internal byte NR34
        {
            get =>
                (byte)(0b1011_1000 |
                        (FrequencyData >> 8) |
                        (UseSoundLength ? 0b0100_0000 : 0));
            set
            {
                FrequencyData = (FrequencyData & 0xFF) | ((value & 0x7) << 8);
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
            _currentFrequencyPeriod = FrequencyPeriod;
            _waveSamplePositionCounter = 0;

            Device.Log.Information("Triggering wave channel with volume shift {0} and period {1}", Volume, FrequencyPeriod);
        }

        internal override void SkipBootRom()
        {
            NR30 = 0x7F; // Don't enable DAC
            NR31 = 0xFF;
            NR32 = 0x9F;
            NR34 = 0xBF;
            IsEnabled = false;
        }

        internal override void Reset()
        {
            base.Reset();
            FrequencyData = 0x0;
            Volume = WaveChannelOutputLevel.Mute;
            _waveSamplePositionCounter = 0;
            _currentFrequencyPeriod = 0;
        }

        internal override void Step()
        {
            _currentFrequencyPeriod--;
            if (_currentFrequencyPeriod == 0)
            {
                _currentFrequencyPeriod = FrequencyPeriod;

                // Move to the next sample
                _waveSamplePositionCounter = (_waveSamplePositionCounter + 1) % WaveSampleSize;

                // Set the output to the current sample shifted by the volume shift register
                _sampleBuffer = (byte)_waveSamples[_waveSamplePositionCounter];
            }
        }

        internal override int GetOutputVolume()
        {
            return _sampleBuffer >> Volume.RightShiftValue();
        }
    }
}
