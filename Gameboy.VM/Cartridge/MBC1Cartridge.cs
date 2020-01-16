using System;

namespace Gameboy.VM.Cartridge
{
    internal class MBC1Cartridge : Cartridge
    {
        private bool _isRamEnabled;

        private byte _bankRegister1 = 0x1; // Defaults to 1 and can't be 0
        private byte _bankRegister2;
        private byte _modeRegister;
        private int _ramOffset;
        private int _offsetLowRom;
        private int _offsetHighRom;
        private readonly byte[] _ramBanks;

        public MBC1Cartridge(byte[] contents) : base(contents)
        {
            _isRamEnabled = false;
            _ramBanks = new byte[RAMSize.NumberBanks() * RAMSize.BankSizeBytes()];
            UpdateBankValues();
        }

        internal override byte ReadRom(ushort address)
        {
            var bankAddress = address switch
            {
                _ when (address < RomBankSizeBytes) => _offsetLowRom + address,
                _ when (address < RomBankSizeBytes * 2) => _offsetHighRom + address,
                _ => 0x0
            } % Contents.Length; // TODO - Is this wrapping behavior correct?

            return Contents[bankAddress];
        }

        internal override byte ReadRam(ushort address)
        {
            if (!_isRamEnabled) return 0xFF;

            if (address < 0xA000 || address >= 0xC000) throw new ArgumentOutOfRangeException(nameof(address), address, $"Can't access RAM at address {address}");

            return _ramBanks[(address + _ramOffset) % _ramBanks.Length]; // TODO - Is wrapping behavior correct?
        }

        internal override void WriteRom(ushort address, in byte value)
        {
            if (address <= 0x1FFF)
            {
                _isRamEnabled = (value & 0x0F) == 0x0A;
            }
            else if (address >= 0x2000 && address <= 0x3FFF)
            {
                var regValue = value & 0x1F;
                _bankRegister1 = (byte)(regValue == 0x0 ? 0x1 : regValue);
            }
            else if (address >= 0x4000 && address <= 0x5FFF)
            {
                _bankRegister2 = (byte)(value & 0x3);
            }
            else if (address >= 0x6000 && address <= 0x7FFF)
            {
                _modeRegister = (byte)(value & 0x1);
            }

            UpdateBankValues();
        }

        internal override void WriteRam(ushort address, in byte value)
        {
            if (!_isRamEnabled || _ramBanks.Length == 0) return; // Writes only accepted when RAM enabled

            var bankedAddress = (address + _ramOffset) % _ramBanks.Length; // TODO - Is wrapping correct?

            _ramBanks[bankedAddress] = value;
        }

        private void UpdateBankValues()
        {
            _ramOffset = (_modeRegister == 0x0 ? 0x0 : _bankRegister2 * RAMSize.BankSizeBytes()) - 0xA000;
            var romBank = _bankRegister2 << 5 | _bankRegister1;
            _offsetHighRom = (romBank - 1) * RomBankSizeBytes;
            _offsetLowRom = _modeRegister == 0x0 ? 0x0 : (_bankRegister2 << 5) * RomBankSizeBytes;

            //Console.WriteLine(ToString());
        }

        public override string ToString()
        {
            return $"LOW_OFFSET:{_offsetLowRom} HIGH_OFFSET:{_offsetHighRom} RAM_OFFSET:{_ramOffset}";
        }
    }
}
