namespace Gameboy.VM.Sound
{
    public interface ISoundOutput
    {
        int AudioFrequency { get; }

        public void PlaySoundByte(int left, int right);
    }
}
