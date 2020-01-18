using System;

namespace Gameboy.VM.Cartridge
{
    internal class MBC3Cartridge : Cartridge
    {
        // RTC registers
        private byte _rtcSecondsRegister;
        private byte _rtcMinutesRegister;
        private byte _rtcHoursRegister;
        private byte _rtcLowBitsDayCounter;
        private byte _rtcHighBitsDayCounter;

        private byte? _mappedRTCRegister;

        private DateTime? _latchedTime;

        // TODO - MBC3 doesn't support more than 3 banks of RAM, how to ensure this?
        public MBC3Cartridge(byte[] contents) : base(contents)
        {
        }

        internal override byte ReadRam(ushort address)
        {
            if (!IsRamEnabled) return 0xFF;

            if (_mappedRTCRegister.HasValue)
            {
                return _mappedRTCRegister.Value;
            }

            return base.ReadRam(address);
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1FFF)
            {
                IsRamEnabled = (value & 0x0F) == 0x0A;
            }
            else if (address >= 0x2000 && address <= 0x3FFF)
            {
                RomBank = value % ROMSize.NumberBanks();
                if (RomBank == 0x0) RomBank = 0x1;
            }
            else if (address >= 0x4000 && address < 0x5FFF)
            {
                if (value <= 0x3)
                {
                    RamBank = value % RAMSize.NumberBanks();
                    _mappedRTCRegister = null;
                }
                else if (value >= 0x8 && value <= 0xC)
                {
                    _mappedRTCRegister = value switch
                    {
                        0x8 => _rtcSecondsRegister,
                        0x9 => _rtcMinutesRegister,
                        0xA => _rtcHoursRegister,
                        0xB => _rtcLowBitsDayCounter,
                        0xC => _rtcHighBitsDayCounter,
                        _ => throw new ArgumentOutOfRangeException(nameof(value), value, $"Value {value} isn't mapped to an RTC register")
                    };
                }
            }
            else if (address < 0x7FFF)
            {
                // Allow clock to start counting again
                if (value == 0x1 && _latchedTime.HasValue) _latchedTime = null;
                // Latch current time
                else if (value == 0x1) _latchedTime = DateTime.UtcNow;
            }
        }

        internal override void WriteRam(ushort address, byte value)
        {
            if (_mappedRTCRegister.HasValue)
            {
                // TODO - What does it mean to write to a mapped RTC register?
            }

            base.WriteRam(address, value);
        }
    }
}
