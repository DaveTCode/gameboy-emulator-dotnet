﻿using System;

namespace Gameboy.VM.Joypad
{
    public enum DeviceKey
    {
        Up,
        Down,
        Left,
        Right,
        A,
        B,
        Select,
        Start
    }

    public static class DeviceKeyExtensions
    {
        public static byte BitMask(this DeviceKey key) => key switch
        {
            _ when key == DeviceKey.Right || key == DeviceKey.A => 0b11111110,
            _ when key == DeviceKey.Left || key == DeviceKey.B => 0b11111101,
            _ when key == DeviceKey.Up || key == DeviceKey.Select => 0b11111011,
            _ when key == DeviceKey.Down || key == DeviceKey.Start => 0b11110111,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "DeviceKey not mapped")
        };

        public static P1RegisterMode RegisterMode(this DeviceKey key) => key switch
        {
            DeviceKey.Right => P1RegisterMode.Directions,
            DeviceKey.Left => P1RegisterMode.Directions,
            DeviceKey.Up => P1RegisterMode.Directions,
            DeviceKey.Down => P1RegisterMode.Directions,
            DeviceKey.A => P1RegisterMode.ABSelectStart,
            DeviceKey.B => P1RegisterMode.ABSelectStart,
            DeviceKey.Select => P1RegisterMode.ABSelectStart,
            DeviceKey.Start => P1RegisterMode.ABSelectStart,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "DeviceKey not mapped")
        };
    }
}
