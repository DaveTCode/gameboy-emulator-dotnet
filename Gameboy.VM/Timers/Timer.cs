using System;

namespace Gameboy.VM.Timers
{
    internal class Timer
    {
        internal ushort SystemCounter { get; private set; }

        private readonly Device _device;
        private int _internalCount;
        private int _reloadingClockTCycles;

        internal Timer(Device device)
        {
            _device = device;
        }

        internal void Reset(bool skipBootrom)
        {
            if (!skipBootrom)
            {
                SystemCounter = 0x0;
                return;
            }

            SystemCounter = (_device.Type, _device.Mode) switch
            {
                (DeviceType.DMG, DeviceType.DMG) => 0xABCC,
                (DeviceType.CGB, DeviceType.DMG) => 0x267C,
                (DeviceType.CGB, DeviceType.CGB) => 0x1EA0,
                _ => throw new ArgumentOutOfRangeException(nameof(_device.Type), $"Invalid device type & mode combination ({_device.Type}, {_device.Mode})")
            };
        }

        internal void Step(int tCycles)
        {
            // Handle system counter - note that it happens regardless of whether timer is turned on
            SystemCounter = (ushort)(SystemCounter + tCycles);

            if (_reloadingClockTCycles > 0) _reloadingClockTCycles -= _reloadingClockTCycles;

            // Handle standard timer
            if (!_isTimerEnabled) return;

            _internalCount += tCycles;

            while (_internalCount >= _timerClockSelect.Step())
            {
                StepTimerCounter();
            }
        }

        private void StepTimerCounter()
        {
            if (TimerCounter == 0xFF)
            {
                _reloadingClockTCycles = 4;
                _timerCounter = TimerModulo;
                _device.InterruptRegisters.RequestInterrupt(Interrupts.Interrupt.Timer);
            }
            else
            {
                _timerCounter = (byte)(_timerCounter + 1);
            }

            _internalCount -= _timerClockSelect.Step();
        }

        #region Timer Registers
        private bool _isTimerEnabled;
        private TimerClockSelect _timerClockSelect;

        private byte _timerController = 0b11111000;
        internal byte TimerController
        {
            get => _timerController;
            set
            {
                _timerController = (byte)((value & 0x7) | 0b11111000); // Unused bits always return 1
                _isTimerEnabled = (value & 0x4) == 0x4;
                _timerClockSelect = (TimerClockSelect)(value & 0x3);
            }
        }

        internal byte Divider
        { 
            get => (byte)(SystemCounter >> 8); // DIV is just 8MSB of system counter
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                // This covers some obscure behaviour where the timer counter
                // is really triggered by bit 9 in the internal divider going
                // from high to low. That includes (as now) when that happens
                // because of a register write.
                if ((SystemCounter & 0b1_0000_0000) == 0b1_0000_0000)
                {
                    StepTimerCounter();
                }
                SystemCounter = 0;
                _internalCount = 0x0;
            }
        }

        private byte _timerCounter;
        internal byte TimerCounter 
        { 
            // Note obscure behavior here, during a reload of TIMA writes are
            // "ignored" and reads return 0 for 4 t-cycles
            get => _reloadingClockTCycles > 0 ? (byte) 0x0 : _timerCounter;
            set
            {
                if (_reloadingClockTCycles > 0) return;
                _timerCounter = value;
            }
        }
        internal byte TimerModulo { get; set; }
        #endregion

        public override string ToString()
        {
            return $"DIV:{Divider:X2}, TAC:{TimerCounter:X2}, TIM:{TimerModulo:X2}, TC:{TimerController:X2}, Internal:{_internalCount:X2}";
        }
    }
}
