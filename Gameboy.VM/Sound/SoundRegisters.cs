using System;

namespace Gameboy.VM.Sound
{
    internal class SoundRegisters
    {
        #region Sound 1
        internal byte NR10 { get; private set; }
        internal byte NR11 { get; private set; }
        internal byte NR12 { get; private set; }
        internal byte NR13 { get; private set; }
        internal byte NR14 { get; private set; }
        #endregion

        #region Sound 2
        internal byte NR21 { get; private set; }
        internal byte NR22 { get; private set; }
        internal byte NR23 { get; private set; }
        internal byte NR24 { get; private set; }
        #endregion

        #region Sound 3
        internal byte NR30 { get; private set; }
        internal byte NR31 { get; private set; }
        internal byte NR32 { get; private set; }
        internal byte NR33 { get; private set; }
        internal byte NR34 { get; private set; }
        #endregion

        #region Sound 4
        internal byte NR41 { get; private set; }
        internal byte NR42 { get; private set; }
        internal byte NR43 { get; private set; }
        internal byte NR44 { get; private set; }
        #endregion

        #region Control Registers
        internal byte NR50 { get; private set; }
        internal byte NR51 { get; private set; }
        internal byte NR52 { get; private set; }
        #endregion

        internal void Clear()
        {

        }

        internal byte ReadFromRegister(ushort address)
        {
            return address switch
            {
                0xFF10 => NR10,
                0xFF11 => NR11,
                0xFF12 => NR12,
                0xFF13 => NR13,
                0xFF14 => NR14,
                0xFF16 => NR21,
                0xFF17 => NR22,
                0xFF18 => NR23,
                0xFF19 => NR24,
                0xFF1A => NR30,
                0xFF1B => NR31,
                0xFF1C => NR32,
                0xFF1D => NR33,
                0xFF1E => NR34,
                0xFF20 => NR41,
                0xFF21 => NR42,
                0xFF22 => NR43,
                0xFF23 => NR44,
                0xFF24 => NR50,
                0xFF25 => NR51,
                0xFF26 => NR52,
                _ => 0x0
            };
        }

        internal void WriteToRegister(ushort address, byte value)
        {
            // TODO - Many of these registers have fixed/unused bits which need implementing
            switch (address)
            {
                case 0xFF10:
                    NR10 = value;
                    break;
                case 0xFF11:
                    NR11 = value;
                    break;
                case 0xFF12:
                    NR12 = value;
                    break;
                case 0xFF13:
                    NR13 = value;
                    break;
                case 0xFF14:
                    NR14 = value;
                    break;
                case 0xFF15: // Unused address
                    break;
                case 0xFF16:
                    NR21 = value;
                    break;
                case 0xFF17:
                    NR22 = value;
                    break;
                case 0xFF18:
                    NR23 = value;
                    break;
                case 0xFF19:
                    NR24 = value;
                    break;
                case 0xFF1A:
                    NR30 = value;
                    break;
                case 0xFF1B:
                    NR31 = value;
                    break;
                case 0xFF1C:
                    NR32 = value;
                    break;
                case 0xFF1D:
                    NR33 = value;
                    break;
                case 0xFF1E:
                    NR34 = value;
                    break;
                case 0xFF1F:// Unused address
                    break;
                case 0xFF20:
                    NR41 = value;
                    break;
                case 0xFF21:
                    NR42 = value;
                    break;
                case 0xFF22:
                    NR43 = value;
                    break;
                case 0xFF23:
                    NR44 = value;
                    break;
                case 0xFF24:
                    NR50 = value;
                    break;
                case 0xFF25:
                    NR51 = value;
                    break;
                case 0xFF26:
                    NR52 = value;
                    break;
                default: // Unmapped address
                    throw new ArgumentOutOfRangeException(nameof(address), address, "Sound register doesn't exist at address");
            }
        }

        public override string ToString()
        {
            return $@"
[NR10:{NR10:X1},NR11:{NR11:X1},NR12:{NR12:X1},NR13:{NR13:X1},NR14:{NR14:X1}]; 
[NR21:{NR21:X1},NR22:{NR22:X1},NR23:{NR23:X1},NR24:{NR24:X1}]; 
[NR30:{NR30:X1},NR31:{NR31:X1},NR22:{NR22:X1},NR23:{NR23:X1},NR24:{NR24:X1}]; 
[NR41:{NR41:X1},NR42:{NR42:X1},NR43:{NR43:X1},NR44:{NR44:X1}]";
        }
    }
}
