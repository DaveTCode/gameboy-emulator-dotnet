namespace Gameboy.VM.Sound.Channels
{
    internal abstract class BaseChannel
    {
        internal bool IsEnabled { get; set; }

        internal int SoundLength { get; set; }

        internal bool UseSoundLength { get; set; }

        internal abstract int GetOutputVolume();

        internal abstract void Reset();

        internal abstract void Step();

        internal void StepLength()
        {
            if (UseSoundLength && SoundLength > 0)
            {
                SoundLength--;

                if (SoundLength == 0)
                {
                    IsEnabled = false;
                }
            }
        }
    }
}
