# Gameboy Emulator in C# 8

Gameboy emulator written in C# as an educational exercise (not as a production emulator).

## State

- Blargg instruction tests all pass except for interrupt
- Blargg instruction timing tests pass
- All MBC5 mooneye tests pass
- Tetris is playable without sound

## TODO

### Fixes

- Blargg interrupt tests hanging indefinitely
- Timer seems off by a few instructions (mooneye timer-Tim** tests all fail)

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