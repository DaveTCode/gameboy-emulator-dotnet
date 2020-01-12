# Gameboy Emulator in C# 8

TODO - Description

## TODO

### Fixes

- Post boot rom doesn't work? Fails checksum
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