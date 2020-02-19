namespace Gameboy.VM.Interrupts
{
    internal class InterruptRegisters
    {
        internal bool AreInterruptsEnabledGlobally { get; set; }

        /// <summary>
        /// Top 3 bits of the IF flag (0xff0f) always return 1. Other bits are settable
        /// </summary>
        private byte _interruptFlags = 0b11100000;
        internal byte InterruptFlags
        {
            get => _interruptFlags;
            set => _interruptFlags = (byte)(0b11100000 | value);
        }

        internal byte InterruptEnable { get; set; }

        /// <summary>
        /// Called by subsystem components when they want to set the
        /// appropriate bit on the IF flag.
        /// </summary>
        /// <param name="interrupt">The interrupt to request</param>
        internal void RequestInterrupt(Interrupt interrupt)
        {
            _interruptFlags = (byte)(_interruptFlags | interrupt.Mask());
        }

        /// <summary>
        /// Reset an interrupt so we are no longer requesting it. Called when
        /// processing an interrupt.
        /// </summary>
        /// <param name="interrupt">The interrupt to reset request for</param>
        internal void ResetInterrupt(Interrupt interrupt)
        {
            _interruptFlags = (byte)(_interruptFlags & ~interrupt.Mask());
        }

        public override string ToString()
        {
            return $"MIE:{AreInterruptsEnabledGlobally}, IF:{InterruptFlags:X1}, IE:{InterruptEnable:X1}";
        }
    }
}
