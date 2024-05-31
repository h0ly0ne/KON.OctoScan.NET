using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class OSPacketIdentifierInfo : IDisposable
    {
        public OSTransportStreamInfo? otsiOSTransportStreamInfo;
        public OSList<OSTransportStreamFilter>? olotsfOSListOSTransportStreamFilter;

        public int iPacketID;
        public int iUseTableExtension;
        public bool bUsed;
        public byte byContinuityCounter;
        public int iBufferPointer;
        public int iBufferLength;
        public byte[]? byaBuffer;
        public bool bHasContinuityErrors;

        public void Init(int iLocalPID, OSTransportStreamInfo? tsiLocalTSInfo)
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.Init()".Pastel(ConsoleColor.Cyan));

            olotsfOSListOSTransportStreamFilter = [];
            olotsfOSListOSTransportStreamFilter?.Initialize();

            iPacketID = iLocalPID;
            otsiOSTransportStreamInfo = tsiLocalTSInfo;
            bHasContinuityErrors = false;
        }

        public void Dispose()
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.Dispose()".Pastel(ConsoleColor.Cyan));

            olotsfOSListOSTransportStreamFilter?.Clear();

            iPacketID = 0;
            otsiOSTransportStreamInfo = null;
        }

        public void Reset()
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.Reset()".Pastel(ConsoleColor.Cyan));

            iBufferPointer = iBufferLength = 0;
        }
    }

    public static class OSPacketIdentifierInfo_Extension
    {
        public static int GetDescription(this OSPacketIdentifierInfo? opiiLocalOSPacketIdentifierInfo, byte[]? byaLocalBuffer, int iLocalLength)
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.GetDescription()".Pastel(ConsoleColor.Cyan));

            var i = 0;

            if (byaLocalBuffer != null)
            {
                while (i < iLocalLength)
                {
                    int iPartialLength = byaLocalBuffer[(i + 1)..].Length;

                    switch (byaLocalBuffer[i])
                    {
                        case 0x09:
                        {
                            if (iPartialLength == 0)
                                break;
                            else
                            {
                                //var casys = (ushort)((byaLocalBuffer[i + 2] << 8) | byaLocalBuffer[i + 3]);
                                //var capid = GetPID(byaLocalBuffer[(i + 4)..]);

                                break;
                            }
                        }
                    }

                    i += iPartialLength + 2;
                }

                return iLocalLength;
            }

            return 0;
        }

        public static bool ValidateContinuityCounter(this OSPacketIdentifierInfo opiiLocalOSPacketIdentifierInfo, byte[] byaLocalBuffer)
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.ValidateContinuityCounter()".Pastel(ConsoleColor.Cyan));

            if (!HasTransportStreamPayload(byaLocalBuffer))
                return true;

            var byCurrentContinuityCounter = byaLocalBuffer[3].GetLSB();
            var bIsValid = ((byte)(opiiLocalOSPacketIdentifierInfo.byContinuityCounter + 1)).GetLSB() == byCurrentContinuityCounter || opiiLocalOSPacketIdentifierInfo.byContinuityCounter == byte.MaxValue;

            if (bIsValid)
                opiiLocalOSPacketIdentifierInfo.byContinuityCounter = byCurrentContinuityCounter;
            else
            {
                opiiLocalOSPacketIdentifierInfo.Reset();
                opiiLocalOSPacketIdentifierInfo.bHasContinuityErrors = true;
            }

            return bIsValid;
        }

        public static void WriteSectionToBuffer(this OSPacketIdentifierInfo opiiLocalOSPacketIdentifierInfo, byte[] byaLocalBuffer, int iLocalBufferLength)
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.WriteSectionToBuffer()".Pastel(ConsoleColor.Cyan));

            if (opiiLocalOSPacketIdentifierInfo.byaBuffer != null)
            {
                Buffer.BlockCopy(byaLocalBuffer, 0, opiiLocalOSPacketIdentifierInfo.byaBuffer, opiiLocalOSPacketIdentifierInfo.iBufferPointer, iLocalBufferLength);
                opiiLocalOSPacketIdentifierInfo.iBufferPointer += iLocalBufferLength;
            }
        }

        public static void ProcessSection(this OSPacketIdentifierInfo opiiLocalOSPacketIdentifierInfo)
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.ProcessSection()".Pastel(ConsoleColor.Cyan));

            if (opiiLocalOSPacketIdentifierInfo.iBufferPointer != opiiLocalOSPacketIdentifierInfo.iBufferLength)
            {
                if (opiiLocalOSPacketIdentifierInfo.iBufferLength != 0 && opiiLocalOSPacketIdentifierInfo.iBufferPointer > opiiLocalOSPacketIdentifierInfo.iBufferLength)
                    opiiLocalOSPacketIdentifierInfo.Reset();

                return;
            }

            if (opiiLocalOSPacketIdentifierInfo.byaBuffer != null)
            {
                if ((opiiLocalOSPacketIdentifierInfo.byaBuffer[1] & 0x80) != 0)
                {
                    if (GetCRC32(opiiLocalOSPacketIdentifierInfo.byaBuffer, opiiLocalOSPacketIdentifierInfo.iBufferLength) != 0)
                    {
                        lCurrentLogger.Error($"Error: CRC invalid for PID {opiiLocalOSPacketIdentifierInfo.iPacketID:X4}".Pastel(ConsoleColor.Red));
                        opiiLocalOSPacketIdentifierInfo.Reset();
                        return;
                    }
                }

                if (opiiLocalOSPacketIdentifierInfo.iBufferLength < 8)
                    return;

                if ((opiiLocalOSPacketIdentifierInfo.byaBuffer[5] & 1) == 0)
                    return;

                var byaTableID = opiiLocalOSPacketIdentifierInfo.byaBuffer[0];
                var usTableExtension = (ushort)((opiiLocalOSPacketIdentifierInfo.byaBuffer[3] << 8) | opiiLocalOSPacketIdentifierInfo.byaBuffer[4]);

                if (!opiiLocalOSPacketIdentifierInfo.CheckSection() && opiiLocalOSPacketIdentifierInfo.iUseTableExtension != 0)
                {
                    if (byaTableID is 0x42 or 0x02)
                        lCurrentLogger.Error($"Error: Section not matched - Adding {byaTableID:X2}:{usTableExtension:X4}".Pastel(ConsoleColor.Red));
                }
            }

            opiiLocalOSPacketIdentifierInfo.Reset();
        }

        public static bool CheckSection(this OSPacketIdentifierInfo opiiLocalOSPacketIdentifierInfo)
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.CheckSection()".Pastel(ConsoleColor.Cyan));

            if (opiiLocalOSPacketIdentifierInfo.byaBuffer != null)
            {
                byte byCurrentTableID = opiiLocalOSPacketIdentifierInfo.byaBuffer[0];
                int iCurrentTableExtension = (opiiLocalOSPacketIdentifierInfo.byaBuffer[3] << 8) | opiiLocalOSPacketIdentifierInfo.byaBuffer[4];
                byte byCurrentVersionNumber = (byte)((opiiLocalOSPacketIdentifierInfo.byaBuffer[5] & 0x3F) >> 1);
                byte byCurrentSectionNumber = opiiLocalOSPacketIdentifierInfo.byaBuffer[6];
                byte byCurrentLastSectionNumber = opiiLocalOSPacketIdentifierInfo.byaBuffer[7];

                if (opiiLocalOSPacketIdentifierInfo.olotsfOSListOSTransportStreamFilter != null)
                {
                    for (var i = 0; i < opiiLocalOSPacketIdentifierInfo.olotsfOSListOSTransportStreamFilter.Count; i++)
                    {
                        var otsfCurrentOSTransportStreamFilter = opiiLocalOSPacketIdentifierInfo.olotsfOSListOSTransportStreamFilter.ElementAt(i);

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
                                otsfCurrentOSTransportStreamFilter.laToDo[j >> 5] |= (1U << (j & 31));

                            otsfCurrentOSTransportStreamFilter.bIsToDoSet = true;

                            if (byCurrentTableID is 0x50 or 0x60)
                            {
                                var byTableIDLimit = (byte)(opiiLocalOSPacketIdentifierInfo.byaBuffer[13] & 0x0F);
                                for (var j = 1; j <= byTableIDLimit; j++)
                                    opiiLocalOSPacketIdentifierInfo.otsiOSTransportStreamInfo?.ostOSScanTransponder.AddSFilter(DEFAULT_PID_EIT, (byte)(byCurrentTableID + j), otsfCurrentOSTransportStreamFilter.iTableExtension, 2, j < 2 ? 15 : 45);
                            }
                        }

                        if ((otsfCurrentOSTransportStreamFilter.laToDo[byCurrentSectionNumber >> 5] & (1U << (byCurrentSectionNumber & 31))) != 0)
                        {
                            bool bTableResult;

                            switch (byCurrentTableID)
                            {
                                case 0x00:
                                    bTableResult = otsfCurrentOSTransportStreamFilter.AddProgramAssociationTableData();
                                    break;
                                case 0x02:
                                    bTableResult = otsfCurrentOSTransportStreamFilter.AddProgramMapTableData();
                                    break;
                                case >= 0x40 and <= 0x41:
                                    bTableResult = otsfCurrentOSTransportStreamFilter.AddNetworkInformationTableData();
                                    break;
                                case >= 0x42 and <= 0x46:
                                    bTableResult = otsfCurrentOSTransportStreamFilter.AddServiceDescriptionTableData();
                                    break;
                                case >= 0x4E and <= 0x6F:
                                    bTableResult = otsfCurrentOSTransportStreamFilter.AddEventInformationTableData(bDoEITRefresh);
                                    break;
                                default:
                                    bTableResult = false;
                                    break;
                            }

                            if (bTableResult)
                            {
                                otsfCurrentOSTransportStreamFilter.laToDo[byCurrentSectionNumber >> 5] &= ~(1U << (byCurrentSectionNumber & 31));

                                if (byCurrentTableID is >= 0x4E and <= 0x6F)
                                {
                                    byte bySignalNoiseRatioLimitSpecial = opiiLocalOSPacketIdentifierInfo.byaBuffer[12];
                                    for (var j = bySignalNoiseRatioLimitSpecial + 1; j <= (bySignalNoiseRatioLimitSpecial | 7); j++)
                                        otsfCurrentOSTransportStreamFilter.laToDo[j >> 5] &= ~(1U << (j & 31));
                                }

                                if (AllZeroOrCounted(otsfCurrentOSTransportStreamFilter.laToDo, 8))
                                {
                                    otsfCurrentOSTransportStreamFilter.bDone = true;
                                    opiiLocalOSPacketIdentifierInfo.olotsfOSListOSTransportStreamFilter.Remove(otsfCurrentOSTransportStreamFilter);
                                }
                                else
                                {
                                    otsfCurrentOSTransportStreamFilter.lTimestamp = CurrentTimestamp();
                                }

                                break;
                            }
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public static bool BuildSection(this OSPacketIdentifierInfo opiiLocalOSPacketIdentifierInfo, byte[] byaLocalTransponder)
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.BuildSection()".Pastel(ConsoleColor.Cyan));

            var iToDo = GetTransportStreamPayload(byaLocalTransponder);
            var iIndex = 188 - iToDo;

            if (iToDo == 0)
                return false;

            var iOffset = (byaLocalTransponder[1] & 0x40) != 0 ? byaLocalTransponder[iIndex++] : iToDo;

            if (iOffset + iIndex > 188)
            {
                lCurrentLogger.Error("Error: Section building failed".Pastel(ConsoleColor.Red));
                opiiLocalOSPacketIdentifierInfo.Reset();

                return false;
            }

            if (opiiLocalOSPacketIdentifierInfo.ValidateContinuityCounter(byaLocalTransponder) && iOffset != 0 && opiiLocalOSPacketIdentifierInfo.iBufferPointer != 0)
            {
                var iOffsetAdapted = iOffset;

                if (opiiLocalOSPacketIdentifierInfo.iBufferLength != 0)
                {
                    if (opiiLocalOSPacketIdentifierInfo.iBufferPointer + iOffsetAdapted > opiiLocalOSPacketIdentifierInfo.iBufferLength)
                        iOffsetAdapted = opiiLocalOSPacketIdentifierInfo.iBufferLength - opiiLocalOSPacketIdentifierInfo.iBufferPointer;
                }
                else
                {
                    if (opiiLocalOSPacketIdentifierInfo.iBufferPointer + iOffsetAdapted > 4096)
                        iOffsetAdapted = 4096 - opiiLocalOSPacketIdentifierInfo.iBufferPointer;
                }

                opiiLocalOSPacketIdentifierInfo.WriteSectionToBuffer(byaLocalTransponder[iIndex..], iOffsetAdapted);

                if (opiiLocalOSPacketIdentifierInfo.byaBuffer != null)
                {
                    if (opiiLocalOSPacketIdentifierInfo is { iBufferLength: 0, iBufferPointer: >= 3 } && (opiiLocalOSPacketIdentifierInfo.iBufferLength = SectorLength(opiiLocalOSPacketIdentifierInfo.byaBuffer)) > 4096)
                        opiiLocalOSPacketIdentifierInfo.Reset();
                    else
                        opiiLocalOSPacketIdentifierInfo.ProcessSection();
                }
            }

            iIndex += iOffset;

            while ((iToDo = 188 - iIndex) > 0 && byaLocalTransponder[iIndex] != 0xFF)
            {
                opiiLocalOSPacketIdentifierInfo.Reset();

                if (iToDo < 3)
                    lCurrentLogger.Warn("Warning: Section start < 3".Pastel(ConsoleColor.Yellow));

                if (iToDo < 3 || (opiiLocalOSPacketIdentifierInfo.iBufferLength = SectorLength(byaLocalTransponder[iIndex..])) > iToDo)
                {
                    if (opiiLocalOSPacketIdentifierInfo.iBufferLength > 4096)
                    {
                        lCurrentLogger.Error("Error: iBufferLength > 4096".Pastel(ConsoleColor.Red));
                        opiiLocalOSPacketIdentifierInfo.Reset();
                        return false;
                    }

                    opiiLocalOSPacketIdentifierInfo.WriteSectionToBuffer(byaLocalTransponder[iIndex..], iToDo);
                    iIndex += iToDo;
                }
                else
                {
                    opiiLocalOSPacketIdentifierInfo.WriteSectionToBuffer(byaLocalTransponder[iIndex..], opiiLocalOSPacketIdentifierInfo.iBufferLength);
                    iIndex += opiiLocalOSPacketIdentifierInfo.iBufferLength;
                    opiiLocalOSPacketIdentifierInfo.ProcessSection();
                }
            }

            return true;
        }
    }
}