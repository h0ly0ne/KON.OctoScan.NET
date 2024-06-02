﻿using System.Globalization;
using System.Text;

using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class OSTransportStreamFilter
    {
        public OSPacketIdentifierInfo? opiiOSPacketIdentifierInfo;

        public byte byTableID;
        public int iTableExtension;
        public byte byVersionNumber;
        public bool bIsToDoSet;
        public bool bDone;
        public int iUseTableExtension;
        public bool bIsVersionNumberSet;
        public uint[] laToDo = new uint[8];
        public long lTimestamp;
        public long lTimeout;
    }

    public static class OSTransportStreamFilter_Extension
    {
        public static bool AddProgramAssociationTableData(this OSTransportStreamFilter otsfLocalOSTransportStreamFilter)
        {
            lCurrentLogger.Trace("OSTransportStreamFilter.AddProgramAssociationTableData()".Pastel(ConsoleColor.Cyan));

            var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            var baCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;

            if (opiiCurrentOSPacketIdentifierInfo is { otsiOSTransportStreamInfo: not null } && baCurrentBuffer != null)
            {
                var iNetworkInformationTablePacketIdentifier = DEFAULT_PID_NIT;
                var iSectionLength = (((baCurrentBuffer[1] & 0x03) << 8) | baCurrentBuffer[2]) + 3;

                otsfLocalOSTransportStreamFilter.iTableExtension = (ushort)((baCurrentBuffer[3] << 8) | baCurrentBuffer[4]);
                opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.iTransportStreamID = otsfLocalOSTransportStreamFilter.iTableExtension;

                for (var i = 8; i < iSectionLength - 4; i += 4)
                {
                    var iProgramNumber = (baCurrentBuffer[i] << 8) | baCurrentBuffer[i + 1];
                    var iPacketIdentifier = GetPID(baCurrentBuffer[(i + 2)..]);

                    if (iProgramNumber != 0)
                    {
                        opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.AddSFilter(iPacketIdentifier, DEFAULT_TID_PM, (ushort)iProgramNumber, 2, 5);
                        opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.AddSFilter(DEFAULT_PID_SDT, DEFAULT_TID_SD, (ushort)iProgramNumber, 2, 5);
                    }
                    else
                        iNetworkInformationTablePacketIdentifier = iPacketIdentifier;
                }

                opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.AddSFilter(iNetworkInformationTablePacketIdentifier, DEFAULT_TID_NI_CURRENT, 0, 1, 15);
            }
            
            return true;
        }

        public static bool AddProgramMapTableData(this OSTransportStreamFilter otsfLocalOSTransportStreamFilter)
        {
            lCurrentLogger.Trace("OSTransportStreamFilter.AddProgramMapTableData()".Pastel(ConsoleColor.Cyan));

            var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            if (opiiCurrentOSPacketIdentifierInfo != null)
            {
                var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo.byaBuffer;
                if (byaCurrentBuffer != null)
                {
                    var iSectionLength = ToByteAsInteger(byaCurrentBuffer[1..]) + 3;
                    var iProgramNumber = ToUShortAsInteger(byaCurrentBuffer[3..]);

                    if (iProgramNumber != otsfLocalOSTransportStreamFilter.iTableExtension)
                        return true;

                    var i = 12;
                    int iLength;

                    if ((iLength = ToByteAsInteger(byaCurrentBuffer[10..])) != 0)
                        i += opiiCurrentOSPacketIdentifierInfo.GetDescription(byaCurrentBuffer[i..], iLength);

                    if (i != 12 + iLength)
                        return true;

                    var osCurrentOSService = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo.GetService(iProgramNumber);

                    if (osCurrentOSService != null)
                    {
                        osCurrentOSService.iProgramClockReferencePacketIdentifier = GetPID(byaCurrentBuffer[8..]);
                        osCurrentOSService.byAudioChannels = 0;
                        osCurrentOSService.iProgramMapTable = (ushort)opiiCurrentOSPacketIdentifierInfo.iPacketID;

                        while (i < iSectionLength - 4)
                        {
                            var iExtractedPID = GetPID(byaCurrentBuffer?[(i + 1)..]);
                            var iExtractedLength = ToByteAsInteger(byaCurrentBuffer?[(i + 3)..]);

                            if (byaCurrentBuffer != null)
                            {
                                switch (byaCurrentBuffer[i])
                                {
                                    case 0x01: // MPEG1
                                    case 0x02: // MPEG2
                                    case 0x10: // MPEG4
                                    case 0x1b: // H264
                                    case 0x24: // HEVC
                                    case 0x42: // CAVS
                                    case 0xea: // VC1
                                    case 0xd1: // DIRAC
                                    {
                                        osCurrentOSService.iVideoPacketIdentifier = iExtractedPID;

                                        break;
                                    }
                                    case 0x03: // MPEG1
                                    case 0x04: // MPEG2
                                    case 0x0F: // AAC
                                    case 0x11: // AAC_LATM
                                    case 0x81: // AC3
                                    case 0x82: // DTS
                                    case 0x83: // TRUEHD
                                    {
                                        if (osCurrentOSService.byAudioChannels < MAX_ANUM)
                                            osCurrentOSService.iaAudioPacketIdentifiers[osCurrentOSService.byAudioChannels++] = iExtractedPID;

                                        break;
                                    }
                                    case 0x06:
                                    {
                                        if (HasDescription(0x56, byaCurrentBuffer[(i + 5)..], iExtractedLength))
                                            osCurrentOSService.iTeletextPacketIdentifier = iExtractedPID;
                                        else if (HasDescription(0x59, byaCurrentBuffer[(i + 5)..], iExtractedLength))
                                            osCurrentOSService.iSubtitlePacketIdentifier = iExtractedPID;
                                        else if (HasDescription(0x6a, byaCurrentBuffer[(i + 5)..], iExtractedLength) ||
                                                 HasDescription(0x7a, byaCurrentBuffer[(i + 5)..], iExtractedLength))
                                        {
                                            if (osCurrentOSService.byAudioChannels < MAX_ANUM)
                                                osCurrentOSService.iaAudioPacketIdentifiers[osCurrentOSService.byAudioChannels++] = iExtractedPID;
                                        }

                                        break;
                                    }
                                }
                            }

                            i += 5 + iExtractedLength;
                        }

                        osCurrentOSService.bGotFromProgramMapTable = true;
                    }
                }
            }

            return true;
        }

        public static bool AddNetworkInformationTableData(this OSTransportStreamFilter otsfLocalOSTransportStreamFilter)
        {
            lCurrentLogger.Trace("OSTransportStreamFilter.AddNetworkInformationTableData()".Pastel(ConsoleColor.Cyan));

            var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            var osiCurrentOSScanIP = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder?.osiOSScanIP;
            var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;
            var usCurrentBufferStringLength = ToByteAsInteger(byaCurrentBuffer?[1..]) + 3;
            var usCurrentNetworkIdentifier = ToUShortAsInteger(byaCurrentBuffer?[3..]);
            var usCurrentNetworkDataLength = ToByteAsInteger(byaCurrentBuffer?[8..]);
            var usCurrentTransportStreamDataPosition = 10 + usCurrentNetworkDataLength;

            if (opiiCurrentOSPacketIdentifierInfo is { otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo: not null } && byaCurrentBuffer != null)
            {
                if ((byaCurrentBuffer[1] & 0x80) != 0)
                    usCurrentBufferStringLength -= 4;

                if (opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.bUseNetworkInformationTable)
                {
                    int usCurrentTableDataLength;

                    for (var iTransponderInfoItterator = usCurrentTransportStreamDataPosition + 2; iTransponderInfoItterator < usCurrentBufferStringLength; iTransponderInfoItterator += usCurrentTableDataLength)
                    {
                        var otiCurrentOSTransponderInfo = new OSTransponderInfo
                        {
                            iTransportStreamID = ToUShortAsInteger(byaCurrentBuffer[iTransponderInfoItterator..]),
                            iOriginalNetworkID = ToUShortAsInteger(byaCurrentBuffer[(iTransponderInfoItterator + 2)..]),
                            iNetworkID = usCurrentNetworkIdentifier,
                            bUseNetworkInformationTable = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.bUseNetworkInformationTable,
                            bScanEventInformationTable = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.bScanEventInformationTable
                        };

                        usCurrentTableDataLength = ToByteAsInteger(byaCurrentBuffer[(iTransponderInfoItterator + 4)..]);
                        iTransponderInfoItterator += 6;

                        switch (byaCurrentBuffer[iTransponderInfoItterator])
                        {
                            case 0x43:
                            {
                                otiCurrentOSTransponderInfo.iFrequency = GetBCD(byaCurrentBuffer[(iTransponderInfoItterator + 2)..], 8) / 100;
                                otiCurrentOSTransponderInfo.iFrequencyFraction = 0;
                                otiCurrentOSTransponderInfo.iPosition = GetBCD(byaCurrentBuffer[(iTransponderInfoItterator + 6)..], 4);
                                otiCurrentOSTransponderInfo.iSymbolRate = GetBCD(byaCurrentBuffer[(iTransponderInfoItterator + 9)..], 7) / 10;
                                otiCurrentOSTransponderInfo.iEAST = (byaCurrentBuffer[iTransponderInfoItterator + 8] & 0x80) >> 7;
                                otiCurrentOSTransponderInfo.iPolarity = 1 ^ ((byaCurrentBuffer[iTransponderInfoItterator + 8] & 0x60) >> 5);
                                otiCurrentOSTransponderInfo.iRollOff = (byaCurrentBuffer[iTransponderInfoItterator + 8] & 0x18) >> 3;
                                otiCurrentOSTransponderInfo.iType = otiCurrentOSTransponderInfo.iModulationSystem = ((byaCurrentBuffer[iTransponderInfoItterator + 8] & 0x04) >> 2) != 0 ? 6 : 5;
                                otiCurrentOSTransponderInfo.iModulationType = byaCurrentBuffer[iTransponderInfoItterator + 8] & 0x03;
                                otiCurrentOSTransponderInfo.iFEC = byaCurrentBuffer[iTransponderInfoItterator + 12] & 0x0f;
                                otiCurrentOSTransponderInfo.iSource = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.iSource;
                                osiCurrentOSScanIP.AddTransponderInfo(otiCurrentOSTransponderInfo);

                                break;
                            }
                            case 0x44:
                            {
                                var iCurrentFrequency = GetBCD(byaCurrentBuffer[(iTransponderInfoItterator + 2)..], 8);
                                otiCurrentOSTransponderInfo.iFrequency = iCurrentFrequency / 10000;
                                otiCurrentOSTransponderInfo.iFrequencyFraction = iCurrentFrequency % 10000;
                                otiCurrentOSTransponderInfo.iSymbolRate = GetBCD(byaCurrentBuffer[(iTransponderInfoItterator + 9)..], 7) / 10;
                                otiCurrentOSTransponderInfo.iModulationType = byaCurrentBuffer[iTransponderInfoItterator + 8];
                                otiCurrentOSTransponderInfo.iModulationSystem = 1;
                                otiCurrentOSTransponderInfo.iType = 1;

                                if (otiCurrentOSTransponderInfo.iFrequency is >= 50 and <= 1000 && otiCurrentOSTransponderInfo.iSymbolRate is >= 1000 and <= 7100 && otiCurrentOSTransponderInfo.iModulationSystem is >= 1 and <= 5)
                                    osiCurrentOSScanIP.AddTransponderInfo(otiCurrentOSTransponderInfo);

                                break;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public static bool AddServiceDescriptionTableData(this OSTransportStreamFilter otsfLocalOSTransportStreamFilter)
        {
            lCurrentLogger.Trace("OSTransportStreamFilter.AddServiceDescriptionTableData()".Pastel(ConsoleColor.Cyan));

            var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;
            var iCurrentTransportStreamID = ToUShortAsInteger(byaCurrentBuffer?[3..]);
            var iCurrentOriginalNetworkID = ToUShortAsInteger(byaCurrentBuffer?[8..]);

            if (byaCurrentBuffer != null)
            {
                int iCurrentDescriptorsLength;

                for (var iCurrentServicePosition = 11; iCurrentServicePosition < opiiCurrentOSPacketIdentifierInfo?.iBufferLength - 4; iCurrentServicePosition += iCurrentDescriptorsLength)
                {
                    var iCurrentServiceID = ToUShortAsInteger(byaCurrentBuffer?[iCurrentServicePosition..(iCurrentServicePosition + 2)]);
                    iCurrentDescriptorsLength = ToByteAsInteger(byaCurrentBuffer?[(iCurrentServicePosition + 3)..(iCurrentServicePosition + 5)]) + 5;
                    var osCurrentOSService = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo.GetService(iCurrentServiceID);

                    if (osCurrentOSService != null)
                    {
                        osCurrentOSService.iOriginalNetworkID = iCurrentOriginalNetworkID;
                        osCurrentOSService.iTransportStreamID = iCurrentTransportStreamID;
                        osCurrentOSService.iEventInformationTableSchedule = (byaCurrentBuffer![iCurrentServicePosition + 2] & 0x02) >> 1;
                        osCurrentOSService.iEventInformationTablePresentFollowing = byaCurrentBuffer[iCurrentServicePosition + 2] & 0x01;
                        osCurrentOSService.iConditionalAccessMode = (byaCurrentBuffer[iCurrentServicePosition + 3] & 0x10) >> 4;

                        iServices += 1;

                        if (opiiCurrentOSPacketIdentifierInfo is { otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo: not null })
                        {
                            if (opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.bScanEventInformationTable && osCurrentOSService.iEventInformationTableSchedule != 0)
                            {
                                for (var i = 0; i < MAX_EIT_SID; i++)
                                {
                                    if (opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.iaEventInformationTableServiceID[0] == 0 || opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.iaEventInformationTableServiceID[i] == iCurrentServiceID)
                                    {
                                        iEITServices += 1;
                                        opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.AddSFilter(DEFAULT_PID_EIT, DEFAULT_TID_EI, iCurrentServiceID, 2, 15);
                                        break;
                                    }
                                }
                            }
                        }

                        byte byCurrentDescriptorLength;

                        for (var iCurrentDescriptorPosition = iCurrentServicePosition + 5; iCurrentDescriptorPosition < iCurrentServicePosition + iCurrentDescriptorsLength; iCurrentDescriptorPosition += byCurrentDescriptorLength)
                        {
                            var byCurrentDescriptorTag = byaCurrentBuffer[iCurrentDescriptorPosition];
                            byCurrentDescriptorLength = (byte)(byaCurrentBuffer[iCurrentDescriptorPosition + 1] + 2);

                            if (byCurrentDescriptorTag == DESCRIPTORTAG_SERVICE_DESCRIPTOR)
                            {
                                var byCurrentProviderNameLength = byaCurrentBuffer[iCurrentDescriptorPosition + 3];
                                var byCurrentProviderNameEncoding = byaCurrentBuffer[iCurrentDescriptorPosition + 4];
                                byte[] byCurrentProviderNameString;
                                int iCurrentProviderNameEndPosition;

                                if (byCurrentProviderNameEncoding >= CCT_ENCODING_ISO_6937)
                                {
                                    byCurrentProviderNameString = byaCurrentBuffer[(iCurrentDescriptorPosition + 4)..(iCurrentDescriptorPosition + 4 + byCurrentProviderNameLength)];
                                    iCurrentProviderNameEndPosition = (iCurrentDescriptorPosition + 4 + byCurrentProviderNameLength);
                                    osCurrentOSService.strProviderName = Encoding.UTF8.GetString(Encoding.Convert(Encoding.GetEncoding(20269), Encoding.UTF8, byCurrentProviderNameString));
                                }
                                else
                                {
                                    Encoding eCurrentProviderNameEncoding;

                                    switch (byCurrentProviderNameEncoding)
                                    {
                                        case CCT_ENCODING_ISO_8859_5:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("iso-8859-5");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_6:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("iso-8859-6");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_7:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("iso-8859-7");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_8:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("iso-8859-8");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_9:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("iso-8859-9");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_10:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("iso-8859-10");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_11:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("iso-8859-11");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_13:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("iso-8859-13");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_15:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("iso-8859-15");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_DYNAMIC_PARTS:
                                        {
                                            var byCurrentProviderNameEncodingTable = byaCurrentBuffer[(iCurrentDescriptorPosition + 5)..(iCurrentDescriptorPosition + 7)];

                                            eCurrentProviderNameEncoding = byCurrentProviderNameEncodingTable[1] switch
                                            {
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_1 => Encoding.GetEncoding("iso-8859-1"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_2 => Encoding.GetEncoding("iso-8859-2"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_3 => Encoding.GetEncoding("iso-8859-3"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_4 => Encoding.GetEncoding("iso-8859-4"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_5 => Encoding.GetEncoding("iso-8859-5"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_6 => Encoding.GetEncoding("iso-8859-6"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_7 => Encoding.GetEncoding("iso-8859-7"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_8 => Encoding.GetEncoding("iso-8859-8"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_9 => Encoding.GetEncoding("iso-8859-9"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_10 => Encoding.GetEncoding("iso-8859-10"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_11 => Encoding.GetEncoding("iso-8859-11"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_13 => Encoding.GetEncoding("iso-8859-13"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_14 => Encoding.Unicode,
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_15 => Encoding.GetEncoding("iso-8859-15"),
                                                _ => Encoding.UTF8
                                            };

                                            break;
                                        }
                                        case CCT_ENCODING_KSX_1001:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("x-cp20949");
                                            break;
                                        }
                                        case CCT_ENCODING_GB_2312:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.GetEncoding("gb2312");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_14:
                                        case CCT_ENCODING_ISO_10646:
                                        case CCT_ENCODING_ISO_10646_BIG5:
                                        case CCT_ENCODING_ISO_10646_UTF8:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.Unicode;
                                            break;
                                        }
                                        default:
                                        {
                                            eCurrentProviderNameEncoding = Encoding.UTF8;
                                            break;
                                        }
                                    }

                                    switch (byCurrentProviderNameEncoding)
                                    {
                                        case CCT_ENCODING_ISO_8859_DYNAMIC_PARTS:
                                        {
                                            byCurrentProviderNameString = byaCurrentBuffer[(iCurrentDescriptorPosition + 7)..(iCurrentDescriptorPosition + 7 + byCurrentProviderNameLength - 1)];
                                            iCurrentProviderNameEndPosition = (iCurrentDescriptorPosition + 7 + byCurrentProviderNameLength - 3);
                                            break;
                                        }
                                        case CCT_ENCODING_CUSTOM:
                                        {
                                            byCurrentProviderNameString = byaCurrentBuffer[(iCurrentDescriptorPosition + 6)..(iCurrentDescriptorPosition + 6 + byCurrentProviderNameLength - 1)];
                                            iCurrentProviderNameEndPosition = (iCurrentDescriptorPosition + 6 + byCurrentProviderNameLength - 2);
                                            break;
                                        }
                                        default:
                                        {
                                            byCurrentProviderNameString = byaCurrentBuffer[(iCurrentDescriptorPosition + 5)..(iCurrentDescriptorPosition + 5 + byCurrentProviderNameLength - 1)];
                                            iCurrentProviderNameEndPosition = (iCurrentDescriptorPosition + 5 + byCurrentProviderNameLength - 1);
                                            break;
                                        }
                                    }

                                    osCurrentOSService.strProviderName = Encoding.UTF8.GetString(Encoding.Convert(eCurrentProviderNameEncoding, Encoding.UTF8, byCurrentProviderNameString));
                                }

                                var byCurrentNameLength = byaCurrentBuffer[iCurrentProviderNameEndPosition];
                                var byCurrentNameEncoding = byaCurrentBuffer[iCurrentProviderNameEndPosition + 1];
                                byte[] byCurrentNameString;

                                if (byCurrentNameEncoding >= CCT_ENCODING_ISO_6937)
                                {
                                    byCurrentNameString = byaCurrentBuffer[(iCurrentProviderNameEndPosition + 1)..(iCurrentProviderNameEndPosition + 1 + byCurrentNameLength)];
                                    osCurrentOSService.strName = Encoding.UTF8.GetString(Encoding.Convert(Encoding.GetEncoding(20269), Encoding.UTF8, byCurrentNameString));
                                }
                                else
                                {
                                    Encoding eCurrentNameEncoding;

                                    switch (byCurrentNameEncoding)
                                    {
                                        case CCT_ENCODING_ISO_8859_5:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("iso-8859-5");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_6:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("iso-8859-6");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_7:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("iso-8859-7");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_8:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("iso-8859-8");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_9:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("iso-8859-9");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_10:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("iso-8859-10");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_11:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("iso-8859-11");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_13:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("iso-8859-13");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_15:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("iso-8859-15");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_DYNAMIC_PARTS:
                                        {
                                            var byCurrentNameEncodingTable = byaCurrentBuffer[(iCurrentProviderNameEndPosition + 2)..(iCurrentProviderNameEndPosition + 4)];

                                            eCurrentNameEncoding = byCurrentNameEncodingTable[1] switch
                                            {
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_1 => Encoding.GetEncoding("iso-8859-1"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_2 => Encoding.GetEncoding("iso-8859-2"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_3 => Encoding.GetEncoding("iso-8859-3"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_4 => Encoding.GetEncoding("iso-8859-4"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_5 => Encoding.GetEncoding("iso-8859-5"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_6 => Encoding.GetEncoding("iso-8859-6"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_7 => Encoding.GetEncoding("iso-8859-7"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_8 => Encoding.GetEncoding("iso-8859-8"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_9 => Encoding.GetEncoding("iso-8859-9"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_10 => Encoding.GetEncoding("iso-8859-10"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_11 => Encoding.GetEncoding("iso-8859-11"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_13 => Encoding.GetEncoding("iso-8859-13"),
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_14 => Encoding.Unicode,
                                                CCT_ENCODING_ISO_8859_DYNAMIC_PARTS_8859_15 => Encoding.GetEncoding("iso-8859-15"),
                                                _ => Encoding.UTF8
                                            };

                                            break;
                                        }
                                        case CCT_ENCODING_KSX_1001:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("x-cp20949");
                                            break;
                                        }
                                        case CCT_ENCODING_GB_2312:
                                        {
                                            eCurrentNameEncoding = Encoding.GetEncoding("gb2312");
                                            break;
                                        }
                                        case CCT_ENCODING_ISO_8859_14:
                                        case CCT_ENCODING_ISO_10646:
                                        case CCT_ENCODING_ISO_10646_BIG5:
                                        case CCT_ENCODING_ISO_10646_UTF8:
                                        {
                                            eCurrentNameEncoding = Encoding.Unicode;
                                            break;
                                        }
                                        default:
                                        {
                                            eCurrentNameEncoding = Encoding.UTF8;
                                            break;
                                        }
                                    }

                                    switch (byCurrentNameEncoding)
                                    {
                                        case CCT_ENCODING_ISO_8859_DYNAMIC_PARTS:
                                        {
                                            byCurrentNameString = byaCurrentBuffer[(iCurrentProviderNameEndPosition + 4)..(iCurrentProviderNameEndPosition + 4 + byCurrentNameLength - 3)];
                                            break;
                                        }
                                        case CCT_ENCODING_CUSTOM:
                                        {
                                            byCurrentNameString = byaCurrentBuffer[(iCurrentProviderNameEndPosition + 3)..(iCurrentProviderNameEndPosition + 3 + byCurrentNameLength - 2)];
                                            break;
                                        }
                                        default:
                                        {
                                            byCurrentNameString = byaCurrentBuffer[(iCurrentProviderNameEndPosition + 2)..(iCurrentProviderNameEndPosition + 2 + byCurrentNameLength - 1)];
                                            break;
                                        }
                                    }

                                    osCurrentOSService.strName = Encoding.UTF8.GetString(Encoding.Convert(eCurrentNameEncoding, Encoding.UTF8, byCurrentNameString));
                                }

                                if (osCurrentOSService.strProviderName.Length > 80)
                                    lCurrentLogger.Warn($"PROVIDERNAME OVERFLOW {osCurrentOSService.strProviderName} LENGTH = {osCurrentOSService.strProviderName.Length}".Pastel(ConsoleColor.Yellow));

                                if (osCurrentOSService.strName.Length > 80)
                                    lCurrentLogger.Warn($"SERVICENAME OVERFLOW {osCurrentOSService.strName} LENGTH = {osCurrentOSService.strName.Length}".Pastel(ConsoleColor.Yellow));

                                osCurrentOSService.strProviderName = osCurrentOSService.strProviderName.CleanupString();
                                osCurrentOSService.strName = osCurrentOSService.strName.CleanupString();

                                osCurrentOSService.bGotFromServiceDescriptorTable = true;
                            }
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public static bool AddEventInformationTableData(this OSTransportStreamFilter otsfLocalOSTransportStreamFilter, bool bLocalDoRefresh)
        {
            lCurrentLogger.Trace("OSTransportStreamFilter.AddEventInformationTableData()".Pastel(ConsoleColor.Cyan));

            OSPacketIdentifierInfo? opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;

            if (byaCurrentBuffer != null)
            {
                var bCurrentTableID = byaCurrentBuffer[0];
                var iCurrentSectionLength = ToByteAsInteger(byaCurrentBuffer[1..3]) + 3;
                var iCurrentServiceID = ToUShortAsInteger(byaCurrentBuffer[3..5]);
                var iCurrentTransportStreamID = ToUShortAsInteger(byaCurrentBuffer[8..10]);
                var iCurrentOriginalNetworkID = ToUShortAsInteger(byaCurrentBuffer[10..12]);
                var osCurrentOSService = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo.GetService(iCurrentServiceID);
                int iCurrentTableDataLengthLimit;

                if ((byaCurrentBuffer[1] & 0x80) != 0)
                    iCurrentSectionLength -= 4;

                if (bLocalDoRefresh && osCurrentOSService != null)
                {
                    OSList<OSEvent> oloeOSListOSEventToRemove = [];

                    foreach (var oeCurrentOSEvent in osCurrentOSService.oloeOSListOSEvent.Where(oeCurrentOSEvent => oeCurrentOSEvent.bTableID == bCurrentTableID))
                    {
                        oloeOSListOSEventToRemove.Add(oeCurrentOSEvent);
                    }

                    foreach (var oeCurrentOSEvent in oloeOSListOSEventToRemove)
                    {
                        osCurrentOSService.oloeOSListOSEvent.Remove(oeCurrentOSEvent);
                        iEITEventsDeleted += 1;
                    }
                }

                iEITSize += iCurrentSectionLength;
                iEITSections += 1;

                for (var iCurrentItterator = 14; iCurrentItterator < iCurrentSectionLength; iCurrentItterator += iCurrentTableDataLengthLimit + 12)
                {
                    int iCurrentTableDataLength;
                    var oeCurrentOSEvent = new OSEvent
                    {
                        bTableID = bCurrentTableID,
                        iServiceID = iCurrentServiceID,
                        iTransportStreamID = iCurrentTransportStreamID,
                        iOriginalNetworkID = iCurrentOriginalNetworkID,
                        iEventID = ToUShortAsInteger(byaCurrentBuffer[(iCurrentItterator + 0)..]),
                        iModifiedJulianDate = ToUShortAsInteger(byaCurrentBuffer[(iCurrentItterator + 2)..]),
                        bStartHour = Convert.ToByte(GetBCD(byaCurrentBuffer[(iCurrentItterator + 4)..], 2)),
                        bStartMinute = Convert.ToByte(GetBCD(byaCurrentBuffer[(iCurrentItterator + 5)..], 2)),
                        bStartSecond = Convert.ToByte(GetBCD(byaCurrentBuffer[(iCurrentItterator + 6)..], 2)),
                        bDurationHour = Convert.ToByte(GetBCD(byaCurrentBuffer[(iCurrentItterator + 7)..], 2)),
                        bDurationMinute = Convert.ToByte(GetBCD(byaCurrentBuffer[(iCurrentItterator + 8)..], 2)),
                        bDurationSecond = Convert.ToByte(GetBCD(byaCurrentBuffer[(iCurrentItterator + 9)..], 2))
                    };

                    iCurrentTableDataLengthLimit = ToByteAsInteger(byaCurrentBuffer[(iCurrentItterator + 10)..]);
                    iEITEvents += 1;

                    for (var iCurrentSubItterator = 0; iCurrentSubItterator < iCurrentTableDataLengthLimit; iCurrentSubItterator += iCurrentTableDataLength + 2)
                    {
                        var iCurrentItteratorOffset = iCurrentItterator + iCurrentSubItterator + 12;
                        var iCurrentDescriptorsTag = byaCurrentBuffer[iCurrentItteratorOffset];
                        iCurrentTableDataLength = byaCurrentBuffer[iCurrentItteratorOffset + 1];
                        switch (iCurrentDescriptorsTag)
                        {
                            case 0x4D:
                            {
                                if (iCurrentTableDataLength >= 5)
                                {
                                    oeCurrentOSEvent.strLanguage = Encoding.ASCII.GetString(byaCurrentBuffer[(iCurrentItteratorOffset + 2)..(iCurrentItteratorOffset + 5)]).ToLower();

                                    var eCurrentEncoding = Encoding.UTF8;
                                    var lCurrentLanguage = ISO639LanguageCollection.AllISO639Languages.First(lCurrentLanguage => lCurrentLanguage.ISO6393Code.ToLower().Equals(oeCurrentOSEvent.strLanguage.ToLower(), StringComparison.InvariantCultureIgnoreCase));

                                    if (lCurrentLanguage.ISO6392Code != null)
                                    {
                                        var ciCurrentCultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).First(ciCurrentCultureInfo => ciCurrentCultureInfo.TwoLetterISOLanguageName.ToLower().Equals(lCurrentLanguage.ISO6392Code.ToLower(), StringComparison.InvariantCultureIgnoreCase));
                                        oeCurrentOSEvent.strLanguage = ciCurrentCultureInfo.ThreeLetterISOLanguageName.ToLower();
                                        eCurrentEncoding = Encoding.GetEncoding(ciCurrentCultureInfo.TextInfo.ANSICodePage);
                                    }

                                    iCurrentItteratorOffset += 5;
                                    var iCurrentStringLength = Convert.ToInt32(byaCurrentBuffer[iCurrentItteratorOffset]);

                                    if (iCurrentStringLength > 0)
                                        oeCurrentOSEvent.strName = eCurrentEncoding.GetString(byaCurrentBuffer, iCurrentItteratorOffset + 1, iCurrentStringLength).CleanupString();

                                    iEITShortSize += iCurrentStringLength;
                                    iCurrentItteratorOffset += iCurrentStringLength + 1;
                                    iCurrentStringLength = byaCurrentBuffer[iCurrentItteratorOffset];

                                    if (iCurrentStringLength > 0)
                                        oeCurrentOSEvent.strText = eCurrentEncoding.GetString(byaCurrentBuffer, iCurrentItteratorOffset + 1, iCurrentStringLength).CleanupString();

                                    iEITShortSize += iCurrentStringLength;
                                }

                                break;
                            }
                        }
                    }

                    osCurrentOSService?.oloeOSListOSEvent.Add(oeCurrentOSEvent);
                }
            }

            return true;
        }
    }
}