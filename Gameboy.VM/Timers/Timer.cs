using System;

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
    }

    internal enum TimerClockSelect
    {
        f_2_10 = 0x00, // 4.096 KHz
        f_2_4 = 0x01, // 262.144 KHz
        f_2_6 = 0x02, // 65.536 KHz
        f_2_8 = 0x03, // 16.384 KHz
    }

    internal static class TimerClockSelectExtensions
    {
        internal static int Step(this TimerClockSelect timerClockSelect) => timerClockSelect switch
        {
            TimerClockSelect.f_2_10 => 1024,
            TimerClockSelect.f_2_4 => 16,
            TimerClockSelect.f_2_6 => 64,
            TimerClockSelect.f_2_8 => 256,
            _ => throw new ArgumentOutOfRangeException(nameof(timerClockSelect), timerClockSelect, "Unhandled TimerClockSelect value")
        };
    }
}
