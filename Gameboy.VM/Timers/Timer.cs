namespace Gameboy.VM.Timers
{
    internal class Timer
    {
        private readonly Device _device;
        private int _internalDiv;
        private int _internalCount;


        internal Timer(in Device device)
        {
            _device = device;
        }

        internal void Step(in int cycles)
        {
            // Handle divider register - note that it happens regardless of whether timer is turned on
            _internalDiv += cycles;
            if (_internalDiv >= 256)
            {
                Divider = (byte)((Divider + 1) & 0xFF);
                _internalDiv -= 256;
            }

            // Handle standard timer
            if (_isTimerEnabled)
            {
                _internalCount += cycles;

                while (_internalCount > _timerClockSelect.Step())
                {
                    TimerCounter = (byte)((TimerCounter + 1) & 0xFF);

                    if (TimerCounter == 0x0)
                    {
                        TimerCounter = TimerModulo;
                        _device.InterruptRegisters.RequestInterrupt(Interrupts.Interrupt.Timer);
                    }

                    _internalCount -= _timerClockSelect.Step();
                }
            }
        }

        #region Timer Registers
        private bool _isTimerEnabled;
        private TimerClockSelect _timerClockSelect;

        private byte _timerController;
        internal byte TimerController
        {
            get => _timerController;
            set
            {
                _timerController = (byte)(value & 0x7); // TODO - Are the remaining bits 0 or 1?
                _isTimerEnabled = (value & 0x4) == 0x4;
                _timerClockSelect = (TimerClockSelect)(value & 0x3);
            }
        }

        internal byte Divider { get; set; }
        internal byte TimerCounter { get; set; }
        internal byte TimerModulo { get; set; }
        #endregion

        public override string ToString()
        {
            return $"DIV:{Divider:X2}, TAC:{TimerCounter}, TIM:{TimerModulo}, TC:{TimerController}";
        }
    }
}
