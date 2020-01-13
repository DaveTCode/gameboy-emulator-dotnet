# Gameboy Emulator in C# 8

TODO - Description

## TODO

### Fixes

- ###IMPORTANT### Joypad uses 1 to indicate key down instead of 0
- Blargg test failures
- Tetris just display single line at the bottom

![Blargg CPU Instr Failures](./blargg_cpu_instr_output.png)

### Specific

- Thorough testing all opcodes through cartridges
- Drawing sprites
- Joypad testing
- DMA testing
- Proper configurable serial port support
- Thorough testing of MBC function

### Ideas/Future

- CGB/SGB support
- MBC !={0,1} support
- Cycle accuracy rather than opcode atomicity assumption