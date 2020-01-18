namespace Gameboy.VM.Cartridge
{
    internal class MBC1Cartridge : Cartridge
    {
        private byte _bankRegister1;
        private byte _bankRegister2;
        private byte _modeRegister;
        private int _offsetLowRom;
        private int _offsetHighRom;

        public MBC1Cartridge(byte[] contents) : base(contents)
        {
            UpdateBankValues();
            _bankRegister1 = 0x1;  // Defaults to 1 and can't be 0
            _bankRegister2 = 0x0;
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

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1FFF)
            {
                IsRamEnabled = (value & 0x0F) == 0x0A;
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

        private void UpdateBankValues()
        {
            RamBank = (_modeRegister == 0x0 ? 0x0 : _bankRegister2);
            var romBank = _bankRegister2 << 5 | _bankRegister1;
            _offsetHighRom = (romBank - 1) * RomBankSizeBytes;
            _offsetLowRom = _modeRegister == 0x0 ? 0x0 : (_bankRegister2 << 5) * RomBankSizeBytes;

            //Console.WriteLine(ToString());
        }
    }
}
