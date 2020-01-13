# Gameboy Emulator in C# 8

TODO - Description

## TODO

### Fixes

- Blargg test failures

![Blargg CPU Instr Failures](./blargg_cpu_instr_output.png)

### Specific

- Drawing sprites
- HALT/STOP instruction implementation
- Thorough testing all opcodes through cartridges
- Test timer subsystem
- DMA testing
- Proper configurable serial port support
- Thorough testing of MBC function
- Prevent pressing multiple buttons (just direction keys?) at a time

### Ideas/Future

- Display FPS using SDL text rendering?
- Native debugger with winforms/wpf?
- CGB/SGB support
- MBC !={0,1} support
- Cycle accuracy rather than opcode atomicity assumption