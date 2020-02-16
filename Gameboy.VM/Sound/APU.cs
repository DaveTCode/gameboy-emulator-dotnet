using System.Collections.Generic;
using Gameboy.VM.Sound.Channels;

namespace Gameboy.VM.Sound
{
    internal class APU
    {
        private const byte ControlMasterMask = 0b0111_0000;

        private const int FrameSequencerTimer512Hz = Device.CyclesPerSecondHz / 512;
        private int _frameSequencerTimer = FrameSequencerTimer512Hz;

        private bool _isEnabled;
        private readonly SquareChannel1 _squareChannel1;
        private readonly SquareChannel2 _squareChannel2;
        private readonly WaveChannel _waveChannel;
        private readonly NoiseChannel _noiseChannel;
        private readonly BaseChannel[] _channels;

        private int _leftOutputVolume;
        private bool _leftVinOn;
        private int _rightOutputVolume;
        private bool _rightVinOn;

        // TODO - This isn't really an int, clock / audio freq is not an integer and pretending that it is will screw up audio _slightly_
        private int _downSampleClock;

        private readonly Dictionary<BaseChannel, (bool, bool)> _soundChannels;

        private readonly Device _device;

        internal APU(Device device)
        {
            _device = device;
            _squareChannel1 = new SquareChannel1(_device);
            _squareChannel2 = new SquareChannel2(_device);
            _waveChannel = new WaveChannel(_device);
            _noiseChannel = new NoiseChannel(_device);
            _channels = new BaseChannel[] { _squareChannel1, _squareChannel2, _waveChannel, _noiseChannel };
            _soundChannels = new Dictionary<BaseChannel, (bool, bool)>
            {
                { _squareChannel1, (false, false) },
                { _squareChannel2, (false, false) },
                { _waveChannel, (false, false) },
                { _noiseChannel, (false, false) },
            };
            _downSampleClock = Device.CyclesPerSecondHz / _device.SoundOutput.AudioFrequency;
        }

        #region Control Registers
        private byte _nr50;
        internal byte NR50
        {
            get => _nr50; private set
            {
                _nr50 = value;
                _rightOutputVolume = value & 0x7;
                _rightVinOn = (value & 0x8) == 0x8;
                _leftOutputVolume = (value >> 4) & 0x7;
                _rightVinOn = (value & 0x80) == 0x80;
            }
        }

        private byte _nr51;
        internal byte NR51
        {
            get => _nr51;
            private set
            {
                _nr51 = value;

                for (var ii = 0; ii < 4; ii++)
                {
                    var rightBit = 1 << ii;
                    var leftBit = 1 << (ii + 4);
                    _soundChannels[_channels[ii]] = ((value & rightBit) == rightBit, (value & leftBit) == leftBit);
                }
            }
        }

        internal byte NR52
        {
            get =>
                (byte)(ControlMasterMask
                        | (_isEnabled ? 0x80 : 0x0)
                        | (_noiseChannel.IsEnabled ? 0x08 : 0x0)
                        | (_waveChannel.IsEnabled ? 0x04 : 0x0)
                        | (_squareChannel2.IsEnabled ? 0x02 : 0x0)
                        | (_squareChannel1.IsEnabled ? 0x01 : 0x0));
            private set
            {
                _isEnabled = (value & 0x80) == 0x80;

                if (!_isEnabled)
                {
                    Reset();
                }
            }
        }

        #endregion


        // TODO - Implement when sound work completed
        internal byte PCM12 { get; set; }
        internal byte PCM34 { get; set; }

        private void Reset()
        {
            _leftOutputVolume = 0;
            _leftVinOn = false;
            _rightOutputVolume = 0;
            _rightVinOn = false;
            _downSampleClock = Device.CyclesPerSecondHz / _device.SoundOutput.AudioFrequency;

            foreach (var channel in _channels)
            {
                channel.Reset();
                _soundChannels[channel] = (false, false);
            }
        }

        internal void Write(ushort address, byte value)
        {
            // Can't write to normal sound registers whilst sound is not enabled
            if (!_isEnabled)
            {
                if (address == 0xFF26) // CTRL master still available (to reenable) when disabled
                {
                    NR52 = value;
                }
                else if (address >= 0xFF30 && address <= 0xFF3F) // Waveform RAM still available
                {
                    _waveChannel.WriteRam(address, value);
                }
            }

            if (address == 0xFF10)
                _squareChannel1.Sweep.Register = value;
            else if (address == 0xFF11)
                _squareChannel1.ControlByte = value;
            else if (address == 0xFF12)
                _squareChannel1.Envelope.Register = value;
            else if (address == 0xFF13)
                _squareChannel1.LowByte = value;
            else if (address == 0xFF14)
                _squareChannel1.HighByte = value;
            // FF15 is an unused register
            else if (address == 0xFF16)
                _squareChannel2.ControlByte = value;
            else if (address == 0xFF17)
                _squareChannel2.Envelope.Register = value;
            else if (address == 0xFF18)
                _squareChannel2.LowByte = value;
            else if (address == 0xFF19)
                _squareChannel2.HighByte = value;
            else if (address == 0xFF1A)
                _waveChannel.NR30 = value;
            else if (address == 0xFF1B)
                _waveChannel.NR31 = value;
            else if (address == 0xFF1C)
                _waveChannel.NR32 = value;
            else if (address == 0xFF1D)
                _waveChannel.NR33 = value;
            else if (address == 0xFF1E)
                _waveChannel.NR34 = value;
            // FF1F is an unused register
            else if (address == 0xFF20)
                _noiseChannel.NR41 = value;
            else if (address == 0xFF21)
                _noiseChannel.Envelope.Register = value;
            else if (address == 0xFF22)
                _noiseChannel.NR43 = value;
            else if (address == 0xFF23)
                _noiseChannel.NR44 = value;
            else if (address == 0xFF24)
                NR50 = value;
            else if (address == 0xFF25)
                NR51 = value;
            else if (address == 0xFF26)
                NR52 = value;
            else if (address >= 0xFF30 && address <= 0xFF3F) // Waveform RAM
                _waveChannel.WriteRam(address, value);
        }

        internal byte Read(ushort address)
        {
            return address switch
            {
                0xFF10 => _squareChannel1.Sweep.Register,
                0xFF11 => _squareChannel1.ControlByte,
                0xFF12 => _squareChannel1.Envelope.Register,
                0xFF13 => _squareChannel1.LowByte,
                0xFF14 => _squareChannel1.HighByte,
                0xFF15 => 0xFF, // Unused
                0xFF16 => _squareChannel2.ControlByte,
                0xFF17 => _squareChannel2.Envelope.Register,
                0xFF18 => _squareChannel2.LowByte,
                0xFF19 => _squareChannel2.HighByte,
                0xFF1A => _waveChannel.NR30,
                0xFF1B => _waveChannel.NR31,
                0xFF1C => _waveChannel.NR32,
                0xFF1D => _waveChannel.NR33,
                0xFF1E => _waveChannel.NR34,
                0xFF1F => 0xFF, // Unused
                0xFF20 => _noiseChannel.NR41,
                0xFF21 => _noiseChannel.Envelope.Register,
                0xFF22 => _noiseChannel.NR43,
                0xFF23 => _noiseChannel.NR44,
                0xFF24 => NR50,
                0xFF25 => NR51,
                0xFF26 => NR52,
                _ when address >= 0xFF27 && address <= 0xFF29 => 0xFF, // Unused
                _ when address >= 0xFF30 && address <= 0xFF3F => _waveChannel.ReadRam(address),
                _ => 0xFF
            };
        }

        private int _frameSequence;
        /// <summary>
        /// 512 Hz timer clocking sweep, envelope and length functions of the
        /// different channels.
        /// <ul>
        /// <li>Length at 256Hz</li>
        /// <li>Volume Envelope at 64Hz</li>
        /// <li>Sweep at 128Hz</li>
        /// </ul>
        /// </summary>
        private void StepFrameSequencer()
        {
            if (_frameSequence % 2 == 0)
            {
                foreach (var channel in _channels)
                {
                    if (channel.IsEnabled) channel.StepLength();
                }
            }

            switch (_frameSequence)
            {
                case 2:
                case 6:
                    if (_squareChannel1.IsEnabled) _squareChannel1.Sweep.Step();
                    break;
                case 7:
                    if (_squareChannel1.IsEnabled) _squareChannel1.Envelope.Step();
                    if (_squareChannel2.IsEnabled) _squareChannel2.Envelope.Step();
                    if (_noiseChannel.IsEnabled) _noiseChannel.Envelope.Step();
                    break;
            }

            _frameSequence = (_frameSequence + 1) % 8;
        }

        internal void Step(int tCycles)
        {
            while (tCycles > 0)
            {
                tCycles--;

                _frameSequencerTimer--;
                if (_frameSequencerTimer == 0)
                {
                    _frameSequencerTimer = FrameSequencerTimer512Hz;

                    StepFrameSequencer();
                }

                // Stepping the sounds generates the next output volume in each
                foreach (var sound in _channels)
                {
                    if (sound.IsEnabled)
                    {
                        sound.Step();
                    }
                }

                // TODO - Is downsampling by just throwing bytes away correct or should we use an average of some sort?
                _downSampleClock--;
                if (_downSampleClock == 0)
                {
                    _downSampleClock = Device.CyclesPerSecondHz / _device.SoundOutput.AudioFrequency;
                    var left = 0;
                    var right = 0;

                    _noiseChannel.GetOutputVolume();
                    foreach (var (channel, (rightOn, leftOn)) in _soundChannels)
                    {
                        if (channel.IsEnabled)
                        {
                            if (rightOn) right += channel.GetOutputVolume();
                            if (leftOn) left += channel.GetOutputVolume();
                        }
                    }

                    left *= _leftOutputVolume;
                    right *= _rightOutputVolume;

                    _device.SoundOutput.PlaySoundByte(left, right);
                }
            }
        }

        /// <summary>
        /// To avoid triggering sounds whilst skipping the boot rom we allow
        /// the APU to handle this logic internally.
        /// </summary>
        public void SkipBootRom()
        {
            foreach (var channel in _channels)
            {
                channel.SkipBootRom();
            }

            NR50 = 0x77;
            NR51 = 0xF3;
            NR52 = 0xF1; // TODO - This would be 0xF0 on SGB/SGB2
        }
    }
}
