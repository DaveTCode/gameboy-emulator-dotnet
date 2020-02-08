namespace Gameboy.VM.Sound
{
    public class NullSoundOutput : ISoundOutput
    {
        public int AudioFrequency => 44100;

        public void PlaySoundByte(int left, int right)
        {
            
        }
    }
}
