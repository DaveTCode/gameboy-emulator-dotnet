namespace Gameboy.VM.Sound.Sweep
{
    internal enum SweepTime
    {
        Off = 0b000,
        ts_1_f128 = 0b001,
        ts_2_f128 = 0b010,
        ts_3_f128 = 0b011,
        ts_4_f128 = 0b100,
        ts_5_f128 = 0b101,
        ts_6_f128 = 0b110,
        ts_7_f128 = 0b111
    }
}