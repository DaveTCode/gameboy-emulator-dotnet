using System.Collections.Generic;

namespace Gameboy.VM.Sound
{
    internal class APU
    {
        public const int CyclesPerOutputClock1Mhz = 4;
        private const byte ControlMasterMask = 0b0111_0000;

        private const int FrameSequencerTimer512Hz = Device.CyclesPerSecondHz / 512;
        private int _frameSequencerTimer = FrameSequencerTimer512Hz;

        private bool _isEnabled;
        private readonly Sound1 _sound1;
        private readonly Sound2 _sound2;
        private readonly Sound3 _sound3;
        private readonly Sound4 _sound4;
        private readonly BaseSound[] _sounds;

        private int _leftOutputVolume;
        private bool _leftVinOn;
        private int _rightOutputVolume;
        private bool _rightVinOn;

        private readonly Dictionary<BaseSound, (bool, bool)> _soundChannels;

        private readonly Device _device;

        internal APU(Device device)
        {
            _device = device;
            _sound1 = new Sound1();
            _sound2 = new Sound2();
            _sound3 = new Sound3();
            _sound4 = new Sound4();
            _sounds = new BaseSound[] { _sound1, _sound2, _sound3, _sound4 };
            _soundChannels = new Dictionary<BaseSound, (bool, bool)>
            {
                { _sound1, (false, false) },
                { _sound2, (false, false) },
                { _sound3, (false, false) },
                { _sound4, (false, false) },
            };
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
                    _soundChannels[_sounds[ii]] = ((value & rightBit) == rightBit, (value & leftBit) == leftBit);
                }
            }
        }

        internal byte NR52
        {
            get =>
                (byte) (ControlMasterMask 
                        | (_isEnabled ? 0x80 : 0x0)
                        | (_sound4.IsEnabled ? 0x08 : 0x0)
                        | (_sound3.IsEnabled ? 0x04 : 0x0)
                        | (_sound2.IsEnabled ? 0x02 : 0x0)
                        | (_sound1.IsEnabled ? 0x01 : 0x0));
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

            foreach (var soundChannel in _soundChannels.Keys)
            {
                soundChannel.Reset();
                _soundChannels[soundChannel] = (false, false);
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
                    _sound3.WaveRam[address - 0xFF30] = value;
                }
            }

            if (address == 0xFF10)
                _sound1.Sweep.Register = value;
            else if (address == 0xFF11)
                _sound1.ControlByte = value;
            else if (address == 0xFF12)
                _sound1.Envelope.Register = value;
            else if (address == 0xFF13)
                _sound1.LowByte = value;
            else if (address == 0xFF14)
                _sound1.HighByte = value;
            // FF15 is an unused register
            else if (address == 0xFF16)
                _sound2.ControlByte = value;
            else if (address == 0xFF17)
                _sound2.Envelope.Register = value;
            else if (address == 0xFF18)
                _sound2.LowByte = value;
            else if (address == 0xFF19)
                _sound2.HighByte = value;
            else if (address == 0xFF1A)
                _sound3.NR30 = value;
            else if (address == 0xFF1B)
                _sound3.NR31 = value;
            else if (address == 0xFF1C)
                _sound3.NR32 = value;
            else if (address == 0xFF1D)
                _sound3.NR33 = value;
            else if (address == 0xFF1E)
                _sound3.NR34 = value;
            // FF1F is an unused register
            else if (address == 0xFF20)
                _sound4.NR41 = value;
            else if (address == 0xFF21)
                _sound4.Envelope.Register = value;
            else if (address == 0xFF22)
                _sound4.NR43 = value;
            else if (address == 0xFF23)
                _sound4.NR44 = value;
            else if (address == 0xFF24)
                NR50 = value;
            else if (address == 0xFF25)
                NR51 = value;
            else if (address == 0xFF26)
                NR52 = value;
            else if (address >= 0xFF30 && address <= 0xFF3F) // Waveform RAM
                _sound3.WaveRam[address - 0xFF30] = value;
        }

        internal byte Read(ushort address)
        {
            return address switch
            {
                0xFF10 => _sound1.Sweep.Register,
                0xFF11 => _sound1.ControlByte,
                0xFF12 => _sound1.Envelope.Register,
                0xFF13 => _sound1.LowByte,
                0xFF14 => _sound1.HighByte,
                0xFF15 => 0xFF, // Unused
                0xFF16 => _sound2.ControlByte,
                0xFF17 => _sound2.Envelope.Register,
                0xFF18 => _sound2.LowByte,
                0xFF19 => _sound2.HighByte,
                0xFF1A => _sound3.NR30,
                0xFF1B => _sound3.NR31,
                0xFF1C => _sound3.NR32,
                0xFF1D => _sound3.NR33,
                0xFF1E => _sound3.NR34,
                0xFF1F => 0xFF, // Unused
                0xFF20 => _sound4.NR41,
                0xFF21 => _sound4.Envelope.Register,
                0xFF22 => _sound4.NR43,
                0xFF23 => _sound4.NR44,
                0xFF24 => NR50,
                0xFF25 => NR51,
                0xFF26 => NR52,
                _ when address >= 0xFF27 && address <= 0xFF29 => 0xFF, // Unused
                _ when address >= 0xFF30 && address <= 0xFF3F => _sound3.WaveRam[address - 0xFF30], // Unused
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
                foreach (var sound in _sounds)
                {
                    if (sound.IsEnabled) sound.StepLength();
                }
            }

            switch (_frameSequence)
            {
                case 2:
                case 6:
                    if (_sound1.IsEnabled) _sound1.Sweep.Step();
                    break;
                case 7:
                    if (_sound1.IsEnabled) _sound1.Envelope.Step();
                    if (_sound2.IsEnabled) _sound2.Envelope.Step();
                    if (_sound4.IsEnabled) _sound4.Envelope.Step();
                    break;
            }

            _frameSequence = (_frameSequence + 1) % 8;
        }

        private int _outputPeriod = CyclesPerOutputClock1Mhz;
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
                foreach (var sound in _sounds)
                {
                    sound.Step();
                }

                // TODO - This is outputting at 1Mhz which is still a bit daft if technically accurate, we should output somewhere closer to 44Khz like the final audio device is
                _outputPeriod--;
                if (_outputPeriod == 0)
                {
                    _outputPeriod = CyclesPerOutputClock1Mhz;
                    var left = 0;
                    var right = 0;

                    foreach (var (sound, (rightOn, leftOn)) in _soundChannels)
                    {
                        if (sound.IsEnabled)
                        {
                            if (rightOn) right += sound.GetOutputVolume();
                            if (leftOn) left += sound.GetOutputVolume();
                        }
                    }

                    left *= _leftOutputVolume;
                    right *= _rightOutputVolume;

                    _device.SoundHandler?.Invoke(left, right);
                }
            }
        }
    }
}
