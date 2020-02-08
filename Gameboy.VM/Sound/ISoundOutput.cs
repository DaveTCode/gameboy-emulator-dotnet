namespace Gameboy.VM.Sound
{
    public interface ISoundOutput
    {
        public void PlaySoundByte(int left, int right);
    }
}
