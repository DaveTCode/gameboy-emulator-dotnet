﻿namespace Gameboy.VM
{
    internal class DMAController
    {
        private readonly Device _device;

        internal DMAController(Device device)
        {
            _device = device;
        }

        /// <summary>
        /// TODO - This does an instant transfer where really it should take 
        /// ~160 m-cycles during which various bits of memory are not 
        /// accessible.
        /// </summary>
        /// <param name="dataAddress"></param>
        internal void InitiateDMATransfer(byte dataAddress)
        {
            var address = dataAddress << 8;

            for (var ii = 0; ii < 160; ii++) // DMA transfers exactly 160 bytes
            {
                _device.MMU.WriteByte((ushort)(0xFE00 + ii), _device.MMU.ReadByte((ushort)(address + ii)));
            }
        }
    }
}
