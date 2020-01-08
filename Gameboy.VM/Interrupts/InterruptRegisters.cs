namespace Gameboy.VM.Interrupts
{
    internal class InterruptRegisters
    {
        internal bool AreInterruptsEnabledGlobally { get; set; }

        internal byte InterruptRequest { get; set; }

        internal byte InterruptEnable { get; set; }

        /// <summary>
        /// Called by subsystem components when they want to set the
        /// appropriate bit on the IF flag.
        /// </summary>
        /// <param name="interrupt">The interrupt to request</param>
        internal void RequestInterrupt(in Interrupt interrupt)
        {
            //Trace.TraceInformation("Requesting interrupt for {0}", interrupt.ToString());
            InterruptRequest = (byte)(InterruptRequest | interrupt.Mask());
        }

        /// <summary>
        /// Reset an interrupt so we are no longer requesting it. Called when
        /// processing an interrupt.
        /// </summary>
        /// <param name="interrupt">The interrupt to reset request for</param>
        internal void ResetInterrupt(in Interrupt interrupt)
        {
            //Trace.TraceInformation("Resetting interrupt flag for {0}", interrupt.ToString());
            InterruptRequest = (byte)(InterruptRequest & ~interrupt.Mask());
        }
    }
}
