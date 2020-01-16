using System.Linq;
using System.Text;

namespace Gameboy.VM.Cartridge
{
    public abstract class Cartridge
    {
        protected const int RomBankSizeBytes = 0x4000;

        protected readonly byte[] Contents;

        internal Cartridge(byte[] contents)
        {
            Contents = contents;
        }

        internal virtual byte ReadRom(ushort address)
        {
            if (address >= Contents.Length)
            {
                return 0x0;
            }

            return Contents[address];
        }

        internal abstract byte ReadRam(ushort address);

        internal abstract void WriteRom(ushort address, in byte value);

        internal abstract void WriteRam(ushort address, in byte value);

        public string GameTitle => Encoding.ASCII.GetString(Contents[0x134..0x13F]);

        public string ManufacturerCode => Encoding.ASCII.GetString(Contents[0x13F..0x143]);

        public CGBSupportCode CGBSupportCode => (CGBSupportCode)Contents[0x143];

        public string MakerCode => Encoding.ASCII.GetString(Contents[0x144..0x146]);

        public SGBSupportCode SGBSupportCode => (SGBSupportCode)Contents[0x146];

        public CartridgeROMSize ROMSize => (CartridgeROMSize)Contents[0x148];

        public CartridgeRAMSize RAMSize => (CartridgeRAMSize)Contents[0x149];

        public CartridgeDestinationCode DestinationCode => (CartridgeDestinationCode)Contents[0x14A];

        public byte MaskRomNumber => Contents[0x14C];

        public byte HeaderChecksum => Contents[0x14D];

        public ushort ROMChecksum => (ushort)(Contents[0x14E] << 8 | Contents[0x14F]);

        public bool IsHeaderValid()
        {
            var calculatedChecksum = Contents[0x134..0x14D].Aggregate(0, (c, b) => c - b - 1);

            return (byte)calculatedChecksum == HeaderChecksum;
        }

        public bool IsROMChecksumValid()
        {
            var calculatedChecksum = Contents[..0x14D].Aggregate(0, (i, b) => i + b);
            return (ushort)calculatedChecksum == ROMChecksum;
        }

        public override string ToString()
        {
            return $"{GameTitle} - {ManufacturerCode} - {MakerCode}";
        }
    }
}
