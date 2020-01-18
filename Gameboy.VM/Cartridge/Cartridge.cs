using System;
using System.Linq;
using System.Text;

namespace Gameboy.VM.Cartridge
{
    public abstract class Cartridge
    {
        protected const int RomBankSizeBytes = 0x4000;
        protected const int RamAddressStart = 0xA000;

        protected bool IsRamEnabled;

        protected int RamBank;

        protected readonly byte[] Contents;
        protected readonly byte[] RamBanks;

        internal Cartridge(byte[] contents)
        {
            Contents = contents;
            RamBanks = new byte[RAMSize.NumberBanks() * RAMSize.BankSizeBytes()];
            RamBank = 0x0;
            IsRamEnabled = false;
        }

        internal abstract byte ReadRom(ushort address);

        internal virtual byte ReadRam(ushort address)
        {
            // If RAM isn't enabled all wires return high (i.e. 0xFF)
            if (!IsRamEnabled) return 0xFF;

            // Is the address mappable? Could throw assert away if we trust calling code
            if (address < RamAddressStart || address >= 0xC000) throw new ArgumentOutOfRangeException(nameof(address), address, $"Can't access RAM at address {address}");

            return RamBanks[address - RamAddressStart + RamBank * RAMSize.BankSizeBytes()];
        }

        internal abstract void WriteRom(ushort address, byte value);

        internal virtual void WriteRam(ushort address, byte value)
        {
            if (!IsRamEnabled) return; // Don't accept writes if RAM disabled

            var bankedAddress = (address - RamAddressStart + RamBank * RAMSize.BankSizeBytes()) % RamBanks.Length;

            RamBanks[bankedAddress] = value;
        }

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
