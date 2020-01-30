using System;
using Gameboy.VM.LCD;

namespace Gameboy.VM
{
    internal class DMAController
    {
        private readonly Device _device;

        internal DMAController(Device device)
        {
            _device = device;
        }

        #region DMG DMA Transfer code

        private DMATransferState _dmaTransferState = DMATransferState.Stopped;
        private DMATransferState _dmaTransferOldState = DMATransferState.Stopped;
        private int _currentDmaTransferIndex;
        private ushort _dmaTransferAddress;
        private byte _dma;
        internal byte DMA
        {
            get => _dma;
            set
            {
                // Note that the below code implies that starting DMA whilst one is active will cancel the previous and restart, this is correct behavior
                _dma = value;
                
                _dmaTransferOldState = _dmaTransferState;
                _dmaTransferState = DMATransferState.Requested;
            }
        }

        /// <summary>
        /// DMA transfer isn't instant, it takes 1 m-cycle to setup then 1
        /// m-cycle per byte to read/write, this code steps along with the
        /// CPU speed to perform the transfer roughly at the correct timing.
        /// </summary>
        /// 
        /// <param name="tCycles">
        /// The number of t-cycles since the last execution
        /// </param>
        private void StepDMATransfer(int tCycles)
        {
            // First 4 t-cycles are DMA set up and don't move any bytes
            if (_dmaTransferState == DMATransferState.Requested || _dmaTransferState == DMATransferState.SettingUp)
            {
                tCycles -= 4;

                // If the DMA was restarted then a byte is still copied on this m-cycle
                if (_dmaTransferOldState == DMATransferState.Running)
                {
                    _device.LCDDriver.WriteOAMByte(
                    (ushort)(0xFE00 + _currentDmaTransferIndex),
                    _device.MMU.ReadByte((ushort)(_dmaTransferAddress + _currentDmaTransferIndex)));
                }

                _dmaTransferAddress = (ushort)(_dma << 8);
                _dmaTransferState = _dmaTransferState == DMATransferState.Requested ? DMATransferState.SettingUp : DMATransferState.Running;
                _currentDmaTransferIndex = 0;
            }

            // Since we're using instruction step speed there might be more than one copy in this step
            while (tCycles > 0)
            {
                // Copy the next byte to OAM RAM
                _device.LCDDriver.WriteOAMByte(
                    (ushort)(0xFE00 + _currentDmaTransferIndex), 
                    _device.MMU.ReadByte((ushort)(_dmaTransferAddress + _currentDmaTransferIndex)));

                tCycles -= 4;

                // Stop transfer after 160 bytes
                _currentDmaTransferIndex++;
                if (_currentDmaTransferIndex == 160)
                {
                    _currentDmaTransferIndex = 0;
                    _dmaTransferState = DMATransferState.Stopped;
                    tCycles = 0;
                }
            }
        }

        #endregion

        private ushort _hdmaSourceAddress;
        private ushort _hdmaDestinationAddress;
        private byte _hdma5;
        private HDMAMode _hdmaMode;
        private int _hdmaTransferBlocks;
        private int _hdmaTransferSize;
        private HDMAState _hdmaState;
        private int _hdmaBytesRemainingThisCopy;

        private DMATransferState _hdmaTransferState = DMATransferState.Stopped;

        /// <summary>
        /// Perform the next <see cref="tCycles"/> parts of the current DMA transfer
        /// </summary>
        /// <param name="tCycles">The number of t-cycles since the last step call.</param>
        internal void Step(int tCycles)
        {
            if (_dmaTransferState != DMATransferState.Stopped)
            {
                StepDMATransfer(tCycles);
            }

            if (_hdmaTransferState != DMATransferState.Stopped)
            {
                StepHDMATransfer(tCycles);
            }
        }

        internal bool HaltCpu()
        {
            // TODO - In theory we should stop the CPU during the copy of horizontal blanking DMA but not sure yet how to implement that
            return _hdmaTransferState == DMATransferState.Running && _hdmaMode == HDMAMode.GDMA;
        }

        internal bool BlockInterrupts()
        {
            // TODO - Does this happen during HBlank DMA as well? Probably
            return _hdmaTransferState == DMATransferState.Running && _hdmaMode == HDMAMode.GDMA;
        }

        internal bool BlocksOAMRAM()
        {
            return _dmaTransferState == DMATransferState.Running;
        }

        private void StepHDMATransfer(int tCycles)
        {
            // Handle HDMA state machine
            if (_hdmaMode == HDMAMode.HDMA)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (_hdmaState)
                {
                    case HDMAState.FinishedLine when _device.LCDRegisters.StatMode != StatMode.HBlankPeriod:
                        _hdmaState = HDMAState.AwaitingHBlank;
                        break;
                    case HDMAState.AwaitingHBlank when _device.LCDRegisters.StatMode == StatMode.HBlankPeriod:
                        _hdmaState = HDMAState.Copying;
                        _hdmaBytesRemainingThisCopy = 16;
                        break;
                }
            }

            while (tCycles > 0)
            {
                switch (_hdmaTransferState)
                {
                    case DMATransferState.Requested:
                        tCycles -= 4;
                        _hdmaTransferState = DMATransferState.SettingUp;
                        break;
                    case DMATransferState.SettingUp:
                        tCycles -= 4;
                        _hdmaState = HDMAState.AwaitingHBlank;
                        _hdmaTransferState = DMATransferState.Running;
                        break;
                    case DMATransferState.Running:
                        if ((_hdmaState == HDMAState.Copying && _hdmaMode == HDMAMode.HDMA) || _hdmaMode == HDMAMode.GDMA)
                        {
                            // HDMA works at 2 bytes per m-cycle
                            for (var ii = 0; ii < 2; ii++)
                            {
                                // Copy the next byte directly to VRAM without going through MMU
                                _device.LCDDriver.WriteVRAMByte(_hdmaDestinationAddress, _device.MMU.ReadByte(_hdmaSourceAddress));

                                // Note that we deliberately use the source/destination addresses to track the pointer to where DMA is accessing
                                // to emulate that a DMA followed by another without changing source/destination continues on from where it left
                                // off.
                                // TODO - There's claim that if the address overflows destination area the transfer halts rather than wrapping (current behavior)
                                _hdmaDestinationAddress++;
                                _hdmaSourceAddress++;
                                _hdmaTransferSize -= 1;

                                // Stop transfer when we have reached the correct transfer size
                                if (_hdmaTransferSize == 0)
                                {
                                    _hdmaTransferState = DMATransferState.Stopped;
                                    _hdma5 = 0xFF; // Reset HDMA5 on completion
                                    tCycles = 0;
                                    break;
                                }

                                if (_hdmaMode == HDMAMode.HDMA)
                                {
                                    _hdmaBytesRemainingThisCopy -= 1;
                                    // Stop transfer if we've finished the 16 bytes for this HBlank period
                                    if (_hdmaBytesRemainingThisCopy == 0)
                                    {
                                        _hdmaState = HDMAState.FinishedLine;
                                        tCycles = 0;
                                        break;
                                    }
                                }
                            }

                            tCycles -= 4;
                        }
                        else
                        {
                            tCycles = 0;
                        }
                        break;
                    case DMATransferState.Stopped:
                        tCycles = 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// HDMA1 always returns 0xFF when read
        /// </summary>
        internal byte HDMA1
        {
            get => 0xFF;
            set => _hdmaSourceAddress = (ushort)((_hdmaSourceAddress & 0x00FF) | (value << 8));
        }

        /// <summary>
        /// HDMA2 aligns to 16 bytes and always returns 0xFF when read
        /// </summary>
        internal byte HDMA2
        {
            get => 0xFF;
            set => _hdmaSourceAddress = (ushort)((_hdmaSourceAddress & 0xFF00) | (value & 0b1111_0000));
        }

        /// <summary>
        /// HDMA3 drops the upper 3 bits and always returns 0xFF on read. Note that top bit is always set
        /// </summary>
        internal byte HDMA3
        {
            get => 0xFF; 
            set => _hdmaDestinationAddress = (ushort)((_hdmaDestinationAddress & 0x00FF) | 
                                                      ((value & 0b0001_1111) << 8) |
                                                      0x8000);
        }

        /// <summary>
        /// HDMA2 aligns to 16 bytes and always returns 0xFF when read
        /// </summary>
        internal byte HDMA4
        {
            get => 0xFF;
            set => _hdmaDestinationAddress = (ushort)((_hdmaDestinationAddress & 0xFF00) | (value & 0b1111_0000));
        }

        internal byte HDMA5
        {
            get => _hdma5;
            set
            {
                _hdma5 = value;
                _hdmaMode = (HDMAMode)(value >> 7);
                _hdmaState = HDMAState.AwaitingHBlank;
                _hdmaTransferBlocks = value & 0b0111_1111;
                _hdmaTransferSize = (_hdmaTransferBlocks + 1) * 16; // (Blocks + 1) * 16
                _hdmaTransferState = DMATransferState.Requested;
                _device.Log.Information("Requesting HDMA transfer from {0} to {1} of {2} bytes", _hdmaSourceAddress, _hdmaDestinationAddress, _hdmaTransferSize);
            }
        }

        private enum DMATransferState
        {
            Requested,
            SettingUp,
            Running,
            Stopped
        }

        private enum HDMAMode
        {
            GDMA = 0x0,
            HDMA = 0x1
        }

        private enum HDMAState
        {
            AwaitingHBlank,
            Copying,
            FinishedLine
        }
    }
}
