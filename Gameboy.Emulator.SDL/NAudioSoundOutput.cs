using System;
using Gameboy.VM;
using Gameboy.VM.Sound;
using NAudio.Wave;

namespace Gameboy.Emulator.SDL
{
    public class NAudioSoundOutput : ISoundOutput
    {
        private const int AudioFrequency = 44100;
        private const int AudioSamples = 2048;
        private const int DownSampleCount = Device.CyclesPerSecondHz / AudioFrequency / 32;

        private readonly BufferedWaveProvider _waveProvider;
        private readonly IWavePlayer _wavePlayer;

        private int _sampleCount;
        private readonly byte[] _soundBuffer = new byte[AudioSamples * 4]; // 2 bytes per channel
        private int _soundBufferIndex;

        internal NAudioSoundOutput()
        {
            _waveProvider = new BufferedWaveProvider(new WaveFormat(AudioFrequency, 16, 2));
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.Init(_waveProvider);
            _wavePlayer.Play();
        }

        public void PlaySoundByte(int left, int right)
        {
            _sampleCount++;
            if (_sampleCount < DownSampleCount) return;
            _sampleCount = 0;

            // Apply gain
            left *= 5000;
            right *= 5000;

            _soundBuffer[_soundBufferIndex] = (byte)left;
            _soundBuffer[_soundBufferIndex + 1] = (byte)(left >> 8);
            _soundBuffer[_soundBufferIndex + 2] = (byte)right;
            _soundBuffer[_soundBufferIndex + 3] = (byte)(right >> 8);
            _soundBufferIndex += 4;

            if (_soundBufferIndex == _soundBuffer.Length)
            {
                //Console.WriteLine(_waveProvider.BufferedBytes);
                _waveProvider.ClearBuffer();
                _waveProvider.AddSamples(_soundBuffer, 0, _soundBufferIndex);
                _soundBufferIndex = 0;
                //Console.WriteLine(string.Join(",", _soundBuffer));
                Array.Clear(_soundBuffer, 0, _soundBuffer.Length);
            }
        }
    }
}
