using System;
using Gameboy.VM.Sound;
using NAudio.Wave;

namespace Gameboy.Emulator.SDL
{
    public class NAudioSoundOutput : ISoundOutput, IDisposable
    {
        private const int AudioSamples = 2048;
        private const int Channels = 2;

        private readonly BufferedWaveProvider _waveProvider;
        private readonly IWavePlayer _wavePlayer;

        private readonly byte[] _soundBuffer = new byte[AudioSamples * Channels];
        private int _soundBufferIndex;

        internal NAudioSoundOutput()
        {
            _waveProvider = new BufferedWaveProvider(new WaveFormat(AudioFrequency, 8, Channels));
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.Init(_waveProvider);
            _wavePlayer.Play();
        }

        public int AudioFrequency => 48000;

        public void PlaySoundByte(int left, int right)
        {
            _soundBuffer[_soundBufferIndex] = (byte)left;
            _soundBuffer[_soundBufferIndex + 1] = (byte)right;
            _soundBufferIndex += 2;

            if (_soundBufferIndex == _soundBuffer.Length)
            {
                // TODO - Why do we need to discard bits of the buffer? How does this impact performance?
                while (_waveProvider.BufferedDuration.Milliseconds > 100)
                {
                    SDL2.SDL_Delay(100);
                }

                _waveProvider.AddSamples(_soundBuffer, 0, _soundBufferIndex);
                _soundBufferIndex = 0;
                Array.Clear(_soundBuffer, 0, _soundBuffer.Length);
            }
        }

        public void Dispose()
        {
            _wavePlayer?.Dispose();
        }
    }
}
