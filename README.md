[![Build Status](https://dev.azure.com/DavidATyler/Gameboy%20Emulator/_apis/build/status/DaveTCode.gameboy-emulator-dotnet?branchName=master)](https://dev.azure.com/DavidATyler/Gameboy%20Emulator/_build/latest?definitionId=2&branchName=master)
[![Code Coverage](https://img.shields.io/azure-devops/coverage/DavidATyler/Gameboy%20Emulator/2)](https://img.shields.io/azure-devops/coverage/DavidATyler/Gameboy%20Emulator/2)

# Gameboy Emulator in C# 8

Gameboy (DMG & CGB) emulator written in C#8 (dotnet core 3.1) as an educational exercise (not as a production emulator). Uses SDL2 for windowing & graphics and 
NAudio for playing raw audio samples.

## State

- Passing test roms section at bottom of readme
- Most DMG games are playable, most CGB games have graphical glitches although DX style games are typically ok
- MBC1,2,3,5 all implemented and tested

## TODO

### Fixes

- Lots of test failures specified in the table below
- Aladdin screwed up video after boot
- Sound channel 4 is screwed up (1 & 2 seem ok if badly aliased)
- Sounds start during copyright screen of tetris, so I've misunderstood something regarding how sounds are turned on/off. 
Think I need to rewrite the sound channels to call out the DAC as a separate unit

### Specific

- Complete adding integration tests for all known test roms
- Add HDMA transfer tests
- Test timer subsystem
- Prevent pressing multiple buttons (just direction keys?) at a time
- RTC for MBC3 pretends to exist but doesn't really
- Fully update debugging spreadsheets for CGB and Audio

### Ideas/Future

- Display FPS using SDL text rendering?
- Native debugger with winforms/wpf?
- SGB support
- Dot renderer rather than full line renderer
- Proper configurable serial port support
- IR port configuration

### Known Tests

| Test                                        | Pass                  | Notes |
| ------------------------------------------- |:---------------------:| -----:|
| Blargg - cpu_instrs                         | :white_check_mark:    |       |
| Blargg - instr_timing                       | :white_check_mark:    |       |
| Blargg - interrupt_time                     | :white_check_mark:    |       |
| Blargg - mem_timing                         | :white_check_mark:    |       |
| Blargg - mem_timing2                        | :white_check_mark:    |       |
| Blargg - cgb_sound                          | :x:                   | All bar one fail, but this would fail on a DMG so doesn't matter overly |
| Blargg - dmg_sound                          | :x:                   | All 12 fail, but this also fails on a CGB console so doesn't matter too much |
| Blargg - halt_bug                           | :x:                   | Fails but no attempt to implement this bug |
| Blargg - oam_bug                            | :x:                   | Fails but no attempt to implement this bug |
| Mooneye - MBC1 - bits_bank1                 | :white_check_mark:    |       |
| Mooneye - MBC1 - bits_bank2                 | :white_check_mark:    |       |
| Mooneye - MBC1 - bits_mode                  | :white_check_mark:    |       |
| Mooneye - MBC1 - bits_ramg                  | :white_check_mark:    |       |
| Mooneye - MBC1 - ram64kb                    | :white_check_mark:    |       |
| Mooneye - MBC1 - ram256kb                   | :white_check_mark:    |       |
| Mooneye - MBC1 - rom512kb                   | :white_check_mark:    |       |
| Mooneye - MBC1 - rom1Mb                     | :white_check_mark:    |       |
| Mooneye - MBC1 - rom2Mb                     | :white_check_mark:    |       |
| Mooneye - MBC1 - rom4Mb                     | :white_check_mark:    |       |
| Mooneye - MBC1 - rom8Mb                     | :white_check_mark:    |       |
| Mooneye - MBC1 - rom16Mb                    | :white_check_mark:    |       |
| Mooneye - MBC2 - bits_ramg                  | :white_check_mark:    |       |
| Mooneye - MBC2 - bits_romb                  | :white_check_mark:    |       |
| Mooneye - MBC2 - bits_unused                | :white_check_mark:    |       |
| Mooneye - MBC2 - ram                        | :white_check_mark:    |       |
| Mooneye - MBC2 - rom1Mb                     | :white_check_mark:    |       |
| Mooneye - MBC2 - rom2Mb                     | :white_check_mark:    |       |
| Mooneye - MBC2 - rom512kb                   | :white_check_mark:    |       |
| Mooneye - MBC5 - rom512kb                   | :white_check_mark:    |       |
| Mooneye - MBC5 - rom1Mb                     | :white_check_mark:    |       |
| Mooneye - MBC5 - rom2Mb                     | :white_check_mark:    |       |
| Mooneye - MBC5 - rom4Mb                     | :white_check_mark:    |       |
| Mooneye - MBC5 - rom8Mb                     | :white_check_mark:    |       |
| Mooneye - MBC5 - rom16Mb                    | :white_check_mark:    |       |
| Mooneye - MBC5 - rom32Mb                    | :white_check_mark:    |       |
| Mooneye - MBC5 - rom64Mb                    | :white_check_mark:    |       |
| Mooneye - BITS - mem_oam                    | :white_check_mark:    |       |
| Mooneye - BITS - reg_f                      | :white_check_mark:    |       |
| Mooneye - BITS - unused_hwio-GS             | :white_check_mark:    |       |
| Mooneye - Instr - daa                       | :white_check_mark:    |       |
| Mooneye - Interrupts - ie_push              | :x:                   | Subtle bug relating to interrupt causing the PC to get put into the interrupt flag, not emulated |
| Mooneye - OAM_DMA - basic                   | :white_check_mark:    |       |
| Mooneye - OAM_DMA - reg_read                | :white_check_mark:    |       |
| Mooneye - OAM_DMA - sources-GS              | :x:                   | Not supposed to pass on CGB, not really clear what this actually _does_      |
| Mooneye - PPU - hblank_ly_scx_timing-GS     | :x:                   | Just says that the test fails without details, we do take SCX into account so this is a bit surprising      |
| Mooneye - PPU - intr_1_2_timing-GS          | :x:                   | Register values way off what they should be |
| Mooneye - PPU - intr_2_0_timing             | :white_check_mark:    |       |
| Mooneye - PPU - intr_2_mode0_timing         | :white_check_mark:    |       |
| Mooneye - PPU - intr_2_mode0_timing_sprites | :x:                   | TEST #00 FAILS      |
| Mooneye - PPU - intr_2_mode3_timing         | :white_check_mark:    |       |
| Mooneye - PPU - intr_2_oam_ok_timing        | :white_check_mark:    |       |
| Mooneye - PPU - lcd_on_timing               | :x:                   | LY=1 when it should be 0 - maybe a more serious bug than the timing issues we know about      |
| Mooneye - PPU - lcdon_write_timing          | :x:                   | Loads of bad assumptions cause this failure      |
| Mooneye - PPU - stat_irq_blocking           | :x:                   | Unknown reason      |
| Mooneye - PPU - stat_lyc_onoff              | :x:                   | Fail r1 step 1 reason unknown      |
| Mooneye - PPU - vblank_stat_intr-GS         | :white_check_mark:    |       |
| Mooneye - Timer - div_write                 | :white_check_mark:    |       |
| Mooneye - Timer - rapid_toggle              | :x:                   | "the timer circuit design causes some unexpected timer increases" - unsure what this means in the test source so likely the cause of failure      |
| Mooneye - Timer - tim00                     | :white_check_mark:    |       |
| Mooneye - Timer - tim01                     | :white_check_mark:    |       |
| Mooneye - Timer - tim10                     | :white_check_mark:    |       |
| Mooneye - Timer - tim11                     | :white_check_mark:    |       |
| Mooneye - Timer - tim00_div_trigger         | :x:                   | Precise implementation of counts and interesting behavior of div register setting values in timer required to do this and following  |
| Mooneye - Timer - tim01_div_trigger         | :x:                   |       |
| Mooneye - Timer - tim10_div_trigger         | :x:                   |       |
| Mooneye - Timer - tim11_div_trigger         | :x:                   |       |
| Mooneye - Timer - tima_reload               | :white_check_mark:    |       |
| Mooneye - Timer - tima_write_reloading      | :x:                   | Fails, requires non-atomic CPU ops      |
| Mooneye - Timer - tma_write_reloading       | :x:                   | Fails, requires non-atomic CPU ops      |
| Mooneye - General - add_sp_e_timing         | :white_check_mark:    |       |
| Mooneye - General - boot_div-dmg0           | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_div-dmgABCmgb      | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_div-S              | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_div2-S             | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_hwio-dmg0          | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_hwio-dmgABCmgb     | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_hwio-S             | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_regs-dmg0          | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_regs-dmgABC        | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_regs-mgb           | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_regs-sgb           | :x:                   | Only passes on specific model of device      |
| Mooneye - General - boot_regs-sgb2          | :x:                   | Only passes on specific model of device      |
| Mooneye - General - call_cc_timing          | :white_check_mark:    |       |
| Mooneye - General - call_cc_timing2         | :white_check_mark:    |       |
| Mooneye - General - call_timing             | :white_check_mark:    |       |
| Mooneye - General - call_timing2            | :white_check_mark:    |       |
| Mooneye - General - div_timing              | :white_check_mark:    |       |
| Mooneye - General - di_timing-GS            | :white_check_mark:    | Passes in both DMG/CGB mode when it should only pass in DMG     |
| Mooneye - General - ei_sequence             | :white_check_mark:    |       |
| Mooneye - General - ei_timing               | :white_check_mark:    |       |
| Mooneye - General - halt_ime0_ei            | :white_check_mark:    |       |
| Mooneye - General - halt_ime0_nointr_timing | :white_check_mark:    |       |
| Mooneye - General - halt_ime1_timing        | :white_check_mark:    |       |
| Mooneye - General - halt_ime1_timing2-GS    | :x:                   | Fails on CGB device, not sure why it fails on emulator though      |
| Mooneye - General - if_ie_registers         | :white_check_mark:    |       |
| Mooneye - General - intr_timing             | :white_check_mark:    |       |
| Mooneye - General - jp_cc_timing            | :white_check_mark:    |       |
| Mooneye - General - jp_timing               | :white_check_mark:    |       |
| Mooneye - General - ld_hl_sp_e_timing       | :white_check_mark:    |       |
| Mooneye - General - oam_dma_restart         | :white_check_mark:    |       |
| Mooneye - General - oam_dma_start           | :x:                   | Something isn't quite right with restarting DMA?      |
| Mooneye - General - oam_dma_timing          | :white_check_mark:    |       |
| Mooneye - General - pop_timing              | :white_check_mark:    |       |
| Mooneye - General - push_timing             | :white_check_mark:    |       |
| Mooneye - General - rapid_di_ei             | :white_check_mark:    |       |
| Mooneye - General - reti_intr_timing        | :white_check_mark:    |       |
| Mooneye - General - reti_timing             | :white_check_mark:    |       |
| Mooneye - General - ret_cc_timing           | :white_check_mark:    |       |
| Mooneye - General - ret_timing              | :white_check_mark:    |       |
| Mooneye - General - rst_timing              | :white_check_mark:    |       |