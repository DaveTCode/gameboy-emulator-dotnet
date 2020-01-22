﻿using System.Collections.Generic;
using System.IO;
using Gameboy.VM.Cartridge;

namespace Gameboy.VM.Tests
{
    internal static class TestUtils
    {
        /// <summary>
        /// Create a device containing a base MBC0 ROM and some bytes
        /// </summary>
        /// <param name="additionalBytes">
        /// Optional parameter to specify a set of bytes to append at 0x150 (starting PC)
        /// </param>
        /// <param name="mode">
        /// Optional parameter to change the device to CGB mode
        /// </param>
        /// <returns></returns>
        internal static Device CreateTestDevice(byte[] additionalBytes = null, DeviceMode mode = DeviceMode.DMG)
        {
            var l = new List<byte>(File.ReadAllBytes(Path.Join("ROMs", "base.gb")));
            if (additionalBytes != null) l.AddRange(additionalBytes);
            var cartridge = CartridgeFactory.CreateCartridge(l.ToArray());

            var device = new Device(cartridge, mode);
            device.SkipBootRom();
            return device;
        }
    }
}
