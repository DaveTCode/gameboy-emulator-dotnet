# Gameboy Emulator in C# 8

Gameboy emulator written in C# as an educational exercise (not as a production emulator).

## State

- Passing test roms section at bottom of readme
- All MBC1 & MBC5 mooneye tests pass
- Mooneye TIM00,01,10,11 tests all pass (so timer is basically accurate)
- Tetris is playable without sound
- Mooneye unused_hwio-GS passes implying all registers are returning correct values for unreadable bits

## TODO

### Fixes

- Zelda Links Awakening has various graphical bugs
- Dr Mario doesn't get past startup screen
- Blargg interrupt tests hanging indefinitely
- Mooneye Timer Rapid Write test fails

![Blargg CPU Instr Failures](./blargg_cpu_instr_output.png)

### Specific

- STOP instruction implementation
- Thorough testing all opcodes through cartridges
- Test timer subsystem
- DMA testing
- Proper configurable serial port support
- Thorough testing of MBC function
- Prevent pressing multiple buttons (just direction keys?) at a time

### Ideas/Future

- Integration testing using headless runner and comparing frame buffer to known good values for variety of roms? Specifically all test roms?
- Display FPS using SDL text rendering?
- Native debugger with winforms/wpf?
- CGB/SGB support
- MBC !={0,1} support
- Cycle accuracy rather than opcode atomicity assumption

### Known Tests

| Test                        | Pass | Notes |
| --------------------------- |:----:| -----:|
| Blargg - cpu_instrs         | Y    |       |
| Blargg - instr_timing       | Y    |       |
| Mooneye - MBC1 - bits_bank1 | Y    |       |
| Mooneye - MBC1 - bits_bank2 | Y    |       |
| Mooneye - MBC1 - bits_mode  | Y    |       |
| Mooneye - MBC1 - bits_ramg  | Y    |       |
| Mooneye - MBC1 - ram64kb    | Y    |       |
| Mooneye - MBC1 - ram256kb   | Y    |       |
| Mooneye - MBC1 - rom512kb   | Y    |       |
| Mooneye - MBC1 - rom1Mb     | Y    |       |
| Mooneye - MBC1 - rom2Mb     | Y    |       |
| Mooneye - MBC1 - rom4Mb     | Y    |       |
| Mooneye - MBC1 - rom8Mb     | Y    |       |
| Mooneye - MBC1 - rom16Mb    | Y    |       |
| Mooneye - MBC5 - rom512kb   | Y    |       |
| Mooneye - MBC5 - rom1Mb     | Y    |       |
| Mooneye - MBC5 - rom2Mb     | Y    |       |
| Mooneye - MBC5 - rom4Mb     | Y    |       |
| Mooneye - MBC5 - rom8Mb     | Y    |       |
| Mooneye - MBC5 - rom16Mb    | Y    |       |
| Mooneye - MBC5 - rom32Mb    | Y    |       |
| Mooneye - MBC5 - rom64Mb    | Y    |       |
