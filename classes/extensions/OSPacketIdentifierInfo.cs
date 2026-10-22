using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public static class OSPacketIdentifierInfo_Extension
{
    extension(OSPacketIdentifierInfo opiiLocalOSPacketIdentifierInfo)
    {
        /// <summary>
        /// OSPacketIdentifierInfo.ValidateContinuityCounter()
        /// </summary>
        /// <param name="rosLocalPacket"></param>
        /// <returns>bool</returns>
        public bool ValidateContinuityCounter(ReadOnlySpan<byte> rosLocalPacket)
        {
            LogTrace("OSPacketIdentifierInfo.ValidateContinuityCounter()");

            if (!HasTransportStreamPayload(rosLocalPacket))
                return true;

            var byCurrentContinuityCounter = rosLocalPacket[3].GetLSB();
            var bIsValid = ((byte)(opiiLocalOSPacketIdentifierInfo.byContinuityCounter + 1)).GetLSB() == byCurrentContinuityCounter || opiiLocalOSPacketIdentifierInfo.byContinuityCounter == byte.MaxValue;

            if (bIsValid)
                opiiLocalOSPacketIdentifierInfo.byContinuityCounter = byCurrentContinuityCounter;
            else
                opiiLocalOSPacketIdentifierInfo.Reset();

            return bIsValid;
        }

        /// <summary>
        /// OSPacketIdentifierInfo.WriteSectionToBuffer()
        /// </summary>
        /// <param name="rosLocalBuffer"></param>
        /// <param name="iLocalBufferLength"></param>
        public void WriteSectionToBuffer(ReadOnlySpan<byte> rosLocalBuffer, int iLocalBufferLength)
        {
            LogTrace("OSPacketIdentifierInfo.WriteSectionToBuffer()");

            if (opiiLocalOSPacketIdentifierInfo.byaBuffer == null)
                return;

            rosLocalBuffer[..iLocalBufferLength].CopyTo(opiiLocalOSPacketIdentifierInfo.byaBuffer.AsSpan(opiiLocalOSPacketIdentifierInfo.iBufferPointer));
            opiiLocalOSPacketIdentifierInfo.iBufferPointer += iLocalBufferLength;
        }

        /// <summary>
        /// OSPacketIdentifierInfo.ProcessSection()
        /// </summary>
        public void ProcessSection()
        {
            LogTrace("OSPacketIdentifierInfo.ProcessSection()");

            if (opiiLocalOSPacketIdentifierInfo.iBufferPointer != opiiLocalOSPacketIdentifierInfo.iBufferLength)
            {
                if (opiiLocalOSPacketIdentifierInfo.iBufferLength != 0 && opiiLocalOSPacketIdentifierInfo.iBufferPointer > opiiLocalOSPacketIdentifierInfo.iBufferLength)
                    opiiLocalOSPacketIdentifierInfo.Reset();

                return;
            }

            var byaCurrentBuffer = opiiLocalOSPacketIdentifierInfo.byaBuffer;

            if (byaCurrentBuffer != null)
            {
                if ((byaCurrentBuffer[1] & 0x80) != 0 && GetCRC32(byaCurrentBuffer.AsSpan(0, opiiLocalOSPacketIdentifierInfo.iBufferLength)) != 0)
                {
                    lCurrentLogger.Error($"Error: CRC invalid for PID {opiiLocalOSPacketIdentifierInfo.iPacketID:X4}".Pastel(ConsoleColor.Red));
                    opiiLocalOSPacketIdentifierInfo.Reset();
                    return;
                }

                if (opiiLocalOSPacketIdentifierInfo.iBufferLength >= 8 && (byaCurrentBuffer[5] & 1) != 0)
                    opiiLocalOSPacketIdentifierInfo.CheckSection();
            }

            opiiLocalOSPacketIdentifierInfo.Reset();
        }

        /// <summary>
        /// OSPacketIdentifierInfo.CheckSection()
        /// </summary>
        public void CheckSection()
        {
            LogTrace("OSPacketIdentifierInfo.CheckSection()");

            var byaCurrentBuffer = opiiLocalOSPacketIdentifierInfo.byaBuffer;
            var olotsfCurrentOSListOSTransportStreamFilter = opiiLocalOSPacketIdentifierInfo.olotsfOSListOSTransportStreamFilter;

            if (byaCurrentBuffer == null || olotsfCurrentOSListOSTransportStreamFilter == null)
                return;

            var byCurrentTableID = byaCurrentBuffer[0];
            var iCurrentTableExtension = (byaCurrentBuffer[3] << 8) | byaCurrentBuffer[4];
            var byCurrentVersionNumber = (byte)((byaCurrentBuffer[5] & 0x3E) >> 1);
            var byCurrentSectionNumber = byaCurrentBuffer[6];
            var byCurrentLastSectionNumber = byaCurrentBuffer[7];
            var otsiCurrentOSTransportStreamInfo = opiiLocalOSPacketIdentifierInfo.otsiOSTransportStreamInfo;

            if (byCurrentTableID is DEFAULT_TID_PA or DEFAULT_TID_SD && otsiCurrentOSTransportStreamInfo is { iStaleTransportStreamID: >= 0 } && iCurrentTableExtension == otsiCurrentOSTransportStreamInfo.iStaleTransportStreamID && Environment.TickCount64 < otsiCurrentOSTransportStreamInfo.lStaleTransportStreamUntil)
                return;

            for (var llnCurrentNode = olotsfCurrentOSListOSTransportStreamFilter.First; llnCurrentNode != null; llnCurrentNode = llnCurrentNode.Next)
            {
                var otsfCurrentOSTransportStreamFilter = llnCurrentNode.Value;

                if (byCurrentTableID != otsfCurrentOSTransportStreamFilter.byTableID)
                    continue;

                if (opiiLocalOSPacketIdentifierInfo.iUseTableExtension != 0)
                {
                    if (otsfCurrentOSTransportStreamFilter.iUseTableExtension == 2)
                    {
                        if (iCurrentTableExtension != otsfCurrentOSTransportStreamFilter.iTableExtension)
                            continue;
                    }
                    else
                    {
                        otsfCurrentOSTransportStreamFilter.iTableExtension = iCurrentTableExtension;
                        otsfCurrentOSTransportStreamFilter.iUseTableExtension = 2;
                    }
                }

                var bDoEITRefresh = false;

                if (!otsfCurrentOSTransportStreamFilter.bIsVersionNumberSet)
                {
                    otsfCurrentOSTransportStreamFilter.byVersionNumber = byCurrentVersionNumber;
                    otsfCurrentOSTransportStreamFilter.bIsVersionNumberSet = true;
                }

                if (otsfCurrentOSTransportStreamFilter.byVersionNumber != byCurrentVersionNumber)
                {
                    lCurrentLogger.Warn($"Warning: TableID {byCurrentTableID:X2} TableExtension {iCurrentTableExtension:X4} - VNR change {otsfCurrentOSTransportStreamFilter.byVersionNumber}->{byCurrentVersionNumber}".Pastel(ConsoleColor.Yellow));

                    otsfCurrentOSTransportStreamFilter.bIsToDoSet = false;
                    otsfCurrentOSTransportStreamFilter.byVersionNumber = byCurrentVersionNumber;
                    bDoEITRefresh = true;
                }

                if (otsfCurrentOSTransportStreamFilter.bDone)
                    break;

                if (!otsfCurrentOSTransportStreamFilter.bIsToDoSet)
                {
                    for (var j = 0; j <= byCurrentLastSectionNumber; j++)
                        otsfCurrentOSTransportStreamFilter.laToDo[j >> 5] |= 1U << (j & 31);

                    otsfCurrentOSTransportStreamFilter.bIsToDoSet = true;

                    if (byCurrentTableID is 0x50 or 0x60)
                    {
                        var byTableIDLimit = (byte)(byaCurrentBuffer[13] & 0x0F);
                        for (var j = 1; j <= byTableIDLimit; j++)
                            opiiLocalOSPacketIdentifierInfo.otsiOSTransportStreamInfo?.ostOSScanTransponder?.AddSFilter(DEFAULT_PID_EIT, (byte)(byCurrentTableID + j), otsfCurrentOSTransportStreamFilter.iTableExtension, 2, j < 2 ? FILTER_TIMEOUT_EIT : FILTER_TIMEOUT_EIT_SCHEDULE);
                    }
                }

                if ((otsfCurrentOSTransportStreamFilter.laToDo[byCurrentSectionNumber >> 5] & (1U << (byCurrentSectionNumber & 31))) == 0)
                    continue;

                bool bTableResult;

                try
                {
                    bTableResult = byCurrentTableID switch
                    {
                        0x00 => otsfCurrentOSTransportStreamFilter.AddProgramAssociationTableData(),
                        0x02 => otsfCurrentOSTransportStreamFilter.AddProgramMapTableData(),
                        0x40 => otsfCurrentOSTransportStreamFilter.AddNetworkInformationTableData(),
                        0x42 => otsfCurrentOSTransportStreamFilter.AddServiceDescriptionTableData(),
                        >= 0x4E and <= 0x6F => otsfCurrentOSTransportStreamFilter.AddEventInformationTableData(bDoEITRefresh),
                        _ => false
                    };
                }
                catch (Exception excCurrentException) when (excCurrentException is IndexOutOfRangeException or ArgumentException)
                {
                    lCurrentLogger.Warn($"Warning: Malformed section PID {opiiLocalOSPacketIdentifierInfo.iPacketID:X4} TableID {byCurrentTableID:X2} TableExtension {iCurrentTableExtension:X4} skipped ({excCurrentException.Message})".Pastel(ConsoleColor.Yellow));
                    bTableResult = false;
                }

                if (!bTableResult)
                    continue;

                otsfCurrentOSTransportStreamFilter.laToDo[byCurrentSectionNumber >> 5] &= ~(1U << (byCurrentSectionNumber & 31));

                if (byCurrentTableID is >= 0x4E and <= 0x6F)
                {
                    var bySegmentLastSectionNumber = byaCurrentBuffer[12];
                    for (var j = bySegmentLastSectionNumber + 1; j <= (bySegmentLastSectionNumber | 7); j++)
                        otsfCurrentOSTransportStreamFilter.laToDo[j >> 5] &= ~(1U << (j & 31));
                }

                if (AllZeroOrCounted(otsfCurrentOSTransportStreamFilter.laToDo))
                {
                    otsfCurrentOSTransportStreamFilter.bDone = true;
                    olotsfCurrentOSListOSTransportStreamFilter.Remove(llnCurrentNode);
                }
                else
                    otsfCurrentOSTransportStreamFilter.lTimestamp = opiiLocalOSPacketIdentifierInfo.otsiOSTransportStreamInfo?.GetStreamTime() ?? 0;

                break;
            }
        }

        /// <summary>
        /// OSPacketIdentifierInfo.BuildSection()
        /// </summary>
        /// <param name="rosLocalPacket"></param>
        /// <returns>bool</returns>
        public bool BuildSection(ReadOnlySpan<byte> rosLocalPacket)
        {
            LogTrace("OSPacketIdentifierInfo.BuildSection()");

            var iToDo = GetTransportStreamPayload(rosLocalPacket);
            var iIndex = TS_PACKET_LENGTH - iToDo;

            if (iToDo == 0)
                return false;

            var iOffset = (rosLocalPacket[1] & 0x40) != 0 ? rosLocalPacket[iIndex++] : iToDo;

            if (iOffset + iIndex > TS_PACKET_LENGTH)
            {
                lCurrentLogger.Error("Error: Section building failed".Pastel(ConsoleColor.Red));
                opiiLocalOSPacketIdentifierInfo.Reset();

                return false;
            }

            if (opiiLocalOSPacketIdentifierInfo.ValidateContinuityCounter(rosLocalPacket) && iOffset != 0 && opiiLocalOSPacketIdentifierInfo.iBufferPointer != 0)
            {
                var iOffsetAdapted = iOffset;
                var iBufferLimit = opiiLocalOSPacketIdentifierInfo.iBufferLength != 0 ? opiiLocalOSPacketIdentifierInfo.iBufferLength : MAX_SECTION_LENGTH;

                if (opiiLocalOSPacketIdentifierInfo.iBufferPointer + iOffsetAdapted > iBufferLimit)
                    iOffsetAdapted = iBufferLimit - opiiLocalOSPacketIdentifierInfo.iBufferPointer;

                opiiLocalOSPacketIdentifierInfo.WriteSectionToBuffer(rosLocalPacket[iIndex..], iOffsetAdapted);

                if (opiiLocalOSPacketIdentifierInfo.byaBuffer != null)
                {
                    if (opiiLocalOSPacketIdentifierInfo is { iBufferLength: 0, iBufferPointer: >= 3 } && (opiiLocalOSPacketIdentifierInfo.iBufferLength = SectorLength(opiiLocalOSPacketIdentifierInfo.byaBuffer)) > MAX_SECTION_LENGTH)
                        opiiLocalOSPacketIdentifierInfo.Reset();
                    else
                        opiiLocalOSPacketIdentifierInfo.ProcessSection();
                }
            }

            iIndex += iOffset;

            while ((iToDo = TS_PACKET_LENGTH - iIndex) > 0 && rosLocalPacket[iIndex] != 0xFF)
            {
                opiiLocalOSPacketIdentifierInfo.Reset();

                if (iToDo < 3)
                    lCurrentLogger.Warn("Warning: Section start < 3".Pastel(ConsoleColor.Yellow));

                if (iToDo < 3 || (opiiLocalOSPacketIdentifierInfo.iBufferLength = SectorLength(rosLocalPacket[iIndex..])) > iToDo)
                {
                    if (opiiLocalOSPacketIdentifierInfo.iBufferLength > MAX_SECTION_LENGTH)
                    {
                        lCurrentLogger.Error($"Error: iBufferLength > {MAX_SECTION_LENGTH}".Pastel(ConsoleColor.Red));
                        opiiLocalOSPacketIdentifierInfo.Reset();
                        return false;
                    }

                    opiiLocalOSPacketIdentifierInfo.WriteSectionToBuffer(rosLocalPacket[iIndex..], iToDo);
                    iIndex += iToDo;
                }
                else
                {
                    opiiLocalOSPacketIdentifierInfo.WriteSectionToBuffer(rosLocalPacket[iIndex..], opiiLocalOSPacketIdentifierInfo.iBufferLength);
                    iIndex += opiiLocalOSPacketIdentifierInfo.iBufferLength;
                    opiiLocalOSPacketIdentifierInfo.ProcessSection();
                }
            }

            return true;
        }
    }
}