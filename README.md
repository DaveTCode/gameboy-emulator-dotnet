# Gameboy Emulator in C# 8

TODO - Description

## TODO

### Fixes

- ###IMPORTANT### Joypad uses 1 to indicate key down instead of 0
- Blargg test all locks up on test 01, maybe MBC1 bank switch bug?
- Can't exit bootrom? Fails checksum?
- Blargg tests all run but apparently _all_ fail (one bad opcode somewhere?)
- Tetris just display single line at the bottom

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