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
        protected int RomBank;

        protected readonly byte[] Contents;
        protected readonly byte[] RamBanks;

        internal Cartridge(byte[] contents)
        {
            Contents = contents;
            RamBanks = new byte[RAMSize.NumberBanks() * RAMSize.BankSizeBytes()];
            RamBank = 0x0;
            RomBank = 0x1;
            IsRamEnabled = false;
        }

        internal virtual byte ReadRom(ushort address)
        {
            if (address < RomBankSizeBytes) // Fixed bank 0
            {
                return Contents[address % Contents.Length];
            }

            if (address < RomBankSizeBytes * 2) // Switchable ROM banks
            {
                var bankAddress = address + (RomBank - 1) * RomBankSizeBytes;
                return Contents[bankAddress % Contents.Length];
            }

            return 0x0;
        }

        internal virtual byte ReadRam(ushort address)
        {
            // If RAM isn't enabled or there isn't any all wires return high (i.e. 0xFF)
            if (!IsRamEnabled || RAMSize == CartridgeRAMSize.None) return 0xFF;

            var bankedAddress = (address - RamAddressStart + RamBank * RAMSize.BankSizeBytes()) % RamBanks.Length;

            return RamBanks[bankedAddress];
        }

        internal abstract void WriteRom(ushort address, byte value);

        internal virtual void WriteRam(ushort address, byte value)
        {
            // Don't accept writes if RAM disabled
            if (!IsRamEnabled || RAMSize == CartridgeRAMSize.None) return;

            var bankedAddress = (address - RamAddressStart + RamBank * RAMSize.BankSizeBytes()) % RamBanks.Length;

            RamBanks[bankedAddress] = value;
        }

        // TODO - This was only true on some cartridges, others used all the way to 0x143 and didn't have the manufacturer code
        public string GameTitle => Encoding.ASCII.GetString(Contents[0x134..0x13F]);

        public string ManufacturerCode => Encoding.ASCII.GetString(Contents[0x13F..0x143]);

        public CGBSupportCode CGBSupportCode => (CGBSupportCode)Contents[0x143];

        public string MakerCode => Contents[0x14B] switch
        {
            0x33 => Encoding.ASCII.GetString(Contents[0x144..0x146]),
            _ => Encoding.ASCII.GetString(new[] { Contents[0x14B] })
        };

        public SGBSupportCode SGBSupportCode => (SGBSupportCode)Contents[0x146];

        public CartridgeROMSize ROMSize => (CartridgeROMSize)Contents[0x148];

        public CartridgeRAMSize RAMSize => (CartridgeRAMSize)Contents[0x149];

        public CartridgeDestinationCode DestinationCode => (CartridgeDestinationCode)Contents[0x14A];

        public byte RomVersion => Contents[0x14C];

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
