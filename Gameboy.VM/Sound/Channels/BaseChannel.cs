namespace Gameboy.VM.Sound.Channels
{
    internal abstract class BaseChannel
    {
        internal bool IsEnabled { get; set; }

        protected abstract int BaseSoundLength { get; }

        internal int SoundLength { get; set; }

        internal bool UseSoundLength { get; set; }

        internal abstract int GetOutputVolume();

        internal virtual void Reset()
        {
            IsEnabled = false;
            SoundLength = 0;
            UseSoundLength = false;
        }

        internal abstract void Step();

        internal virtual void Trigger()
        {
            IsEnabled = true;
            if (SoundLength == 0)
            {
                SoundLength = 0x3F;
            }
        }

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
