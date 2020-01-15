namespace Gameboy.VM
{
    internal class DMAController
    {
        private readonly Device _device;

        internal DMAController(in Device device)
        {
            _device = device;
        }

        /// <summary>
        /// TODO - This does an instant transfer where really it should take 
        /// ~160 m-cycles during which various bits of memory are not 
        /// accessible.
        /// </summary>
        /// <param name="dataAddress"></param>
        internal void InitiateDMATransfer(in byte dataAddress)
        {
            var address = dataAddress << 8;

            for (var ii = 0; ii < 0x100; ii++)
            {
                _device.MMU.WriteByte((ushort)(0xFE00 + ii), _device.MMU.ReadByte((ushort)(address + ii)));
            }
        }
    }
}
