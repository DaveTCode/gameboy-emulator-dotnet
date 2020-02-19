using System;
using System.Collections.Generic;

namespace Gameboy.VM.Joypad
{
    internal class JoypadHandler
    {
        private readonly Device _device;

        private readonly Dictionary<DeviceKey, bool> _keyStates = new Dictionary<DeviceKey, bool>
        {
            { DeviceKey.A, false },
            { DeviceKey.B, false },
            { DeviceKey.Select, false },
            { DeviceKey.Start, false },
            { DeviceKey.Right, false },
            { DeviceKey.Left, false },
            { DeviceKey.Up, false },
            { DeviceKey.Down, false }
        };
        private P1RegisterMode _p1RegisterMode = P1RegisterMode.None;

        internal byte P1Register
        {
            get
            {
                var p1 = 0xFF; // By default the register returns 1 in all bits
                switch (_p1RegisterMode)
                {
                    case P1RegisterMode.None: // Noop, no keys pressed since we're not looking at either modes
                        break;
                    case P1RegisterMode.Directions:
                        p1 &= 0b11101111; // Unset bit 4 to reflect mode = directions
                        p1 &= _keyStates[DeviceKey.Right] ? DeviceKey.Right.BitMask() : 0xFF; // RIGHT = unset bit 0
                        p1 &= _keyStates[DeviceKey.Left] ? DeviceKey.Left.BitMask() : 0xFF; // LEFT = unset bit 1
                        p1 &= _keyStates[DeviceKey.Up] ? DeviceKey.Up.BitMask() : 0xFF; // UP = unset bit 2
                        p1 &= _keyStates[DeviceKey.Down] ? DeviceKey.Down.BitMask() : 0xFF; // DOWN = unset bit 3
                        break;
                    case P1RegisterMode.ABSelectStart:
                        p1 &= 0b11011111; // Unset bit 5 to reflect mode = buttons
                        p1 &= _keyStates[DeviceKey.A] ? DeviceKey.A.BitMask() : 0xFF; // A = unset bit 0
                        p1 &= _keyStates[DeviceKey.B] ? DeviceKey.B.BitMask() : 0xFF; // B = unset bit 1
                        p1 &= _keyStates[DeviceKey.Select] ? DeviceKey.Select.BitMask() : 0xFF; // Select = unset bit 2
                        p1 &= _keyStates[DeviceKey.Start] ? DeviceKey.Start.BitMask() : 0xFF; // Start = unset bit 3
                        break;
                    case P1RegisterMode.Both:
                        p1 &= 0b11001111; // Unset bit 4 & 5 to reflect mode = both - TODO - not really sure about this behaviour but it seems correct based on wiring
                        p1 &= _keyStates[DeviceKey.Right] || _keyStates[DeviceKey.A] ? DeviceKey.A.BitMask() : 0xFF; // RIGHT OR A = bit 0
                        p1 &= _keyStates[DeviceKey.Left] || _keyStates[DeviceKey.B] ? DeviceKey.B.BitMask() : 0xFF; // LEFT OR B = bit 1
                        p1 &= _keyStates[DeviceKey.Up] || _keyStates[DeviceKey.Select] ? DeviceKey.Select.BitMask() : 0xFF; // UP OR Select = bit 2
                        p1 &= _keyStates[DeviceKey.Down] || _keyStates[DeviceKey.Start] ? DeviceKey.Start.BitMask() : 0xFF; // DOWN OR Start = bit 3
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_p1RegisterMode), _p1RegisterMode, "Unknown P1 register mode");
                }
                return (byte)p1;
            }
            set
            {
                _p1RegisterMode = (P1RegisterMode)(((value & 0x20) | (value & 0x10)) >> 4);

                // TODO - Do we really ignore setting the key values here? Doing so means we never catch key interrupts so presumably...

                CheckForInterrupts();
            }
        }

        internal JoypadHandler(Device device)
        {
            _device = device;
        }

        internal void Keydown(DeviceKey key)
        {
            _keyStates[key] = true;

            CheckForInterrupts();
        }

        internal void Keyup(DeviceKey key)
        {
            _keyStates[key] = false;

            // TODO - Handle interrupts on key up? Some docs say true.
        }

        private void CheckForInterrupts()
        {
            foreach (var (key, state) in _keyStates)
            {
                if (state && _p1RegisterMode == key.RegisterMode())
                {
                    _device.InterruptRegisters.RequestInterrupt(Interrupts.Interrupt.Joypad);
                    break;
                }
            }
        }

        internal void Clear()
        {
            _p1RegisterMode = P1RegisterMode.None;
            foreach (var key in _keyStates.Keys)
            {
                _keyStates[key] = false;
            }
        }
    }
}
