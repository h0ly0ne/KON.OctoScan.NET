using System.Globalization;
using System.Text;

using Nito.HashAlgorithms;
using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Enums;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public static class OSTransportStreamFilter_Extension
    {
        extension(OSTransportStreamFilter otsfLocalOSTransportStreamFilter)
        {
            public bool AddProgramAssociationTableData()
            {
                lCurrentLogger.Trace("OSTransportStreamFilter.AddProgramAssociationTableData()".Pastel(ConsoleColor.Cyan));

                var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
                var baCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;

                if (opiiCurrentOSPacketIdentifierInfo is { otsiOSTransportStreamInfo: not null } && baCurrentBuffer != null)
                {
                    var iNetworkInformationTablePacketIdentifier = DEFAULT_PID_NIT;
                    var iSectionLength = baCurrentBuffer.ToDVBLengthAsInteger(1) + 3;

                    otsfLocalOSTransportStreamFilter.iTableExtension = (ushort)baCurrentBuffer.ToUShortAsInteger(3);
                    opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.iTransportStreamID = otsfLocalOSTransportStreamFilter.iTableExtension;

                    for (var i = 8; i < iSectionLength - 4; i += 4)
                    {
                        var iProgramNumber = baCurrentBuffer.ToUShortAsInteger(i);
                        var iPacketIdentifier = ((baCurrentBuffer[i + 2] & 0x1F) << 8) | baCurrentBuffer[i + 3];

                        if (iProgramNumber != 0)
                        {
                            opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder?.AddSFilter(iPacketIdentifier, DEFAULT_TID_PM, (ushort)iProgramNumber, 2, 5);
                            opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder?.AddSFilter(DEFAULT_PID_SDT, DEFAULT_TID_SD, (ushort)iProgramNumber, 2, 5);
                        }
                        else
                            iNetworkInformationTablePacketIdentifier = iPacketIdentifier;
                    }

                    opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder?.AddSFilter(iNetworkInformationTablePacketIdentifier, DEFAULT_TID_NI_CURRENT, 0, 1, 15);
                }

                return true;
            }

            public bool AddProgramMapTableData()
            {
                lCurrentLogger.Trace("OSTransportStreamFilter.AddProgramMapTableData()".Pastel(ConsoleColor.Cyan));

                var crc32CurrentCalculatedCRC32 = new CRC32(CRC32.Definition.Mpeg2);
                crc32CurrentCalculatedCRC32.Initialize();
                var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;

                if (opiiCurrentOSPacketIdentifierInfo != null)
                {
                    var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo.byaBuffer;
                    if (byaCurrentBuffer != null)
                    {
                        for (int iCurrentBufferPosition = 0; iCurrentBufferPosition < byaCurrentBuffer.Length;)
                        {
                            if (byaCurrentBuffer[iCurrentBufferPosition] == 0)
                                break;

                            DVBTableSection dvbtTableIdentifier = (DVBTableSection)byaCurrentBuffer[iCurrentBufferPosition];

                            var iSectionLength = byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentBufferPosition + 1) + 3;
                            lCurrentLogger.Trace($"Section Length: 0x{iSectionLength:X2} ({iSectionLength})".Pastel(ConsoleColor.Cyan));
                            var iTableIDExtension = byaCurrentBuffer.ToUShortAsInteger(iCurrentBufferPosition + 3);
                            lCurrentLogger.Trace($"SID (Table ID Extension): 0x{iTableIDExtension:X4} ({iTableIDExtension})".Pastel(ConsoleColor.Cyan));

                            byte byVersionByte = byaCurrentBuffer[iCurrentBufferPosition + 5];
                            var iVersionNumber = (byVersionByte & 0x3E) >> 1; // Bits 5-1
                            lCurrentLogger.Trace($"Version Number: 0x{iVersionNumber:X2} ({iVersionNumber})".Pastel(ConsoleColor.Cyan));
                            var iCurrentNextIndicator = byVersionByte & 0x01;  // Bit 0
                            lCurrentLogger.Trace($"Current/Next Indicator: 0x{iCurrentNextIndicator:X2} ({iCurrentNextIndicator})".Pastel(ConsoleColor.Cyan));

                            var iSectionNumber = byaCurrentBuffer[iCurrentBufferPosition + 6];
                            lCurrentLogger.Trace($"Section Number: 0x{iSectionNumber:X2} ({iSectionNumber})".Pastel(ConsoleColor.Cyan));
                            var iLastSectionNumber = byaCurrentBuffer[iCurrentBufferPosition + 7];
                            lCurrentLogger.Trace($"Last Section Number: 0x{iLastSectionNumber:X2} ({iLastSectionNumber})".Pastel(ConsoleColor.Cyan));

                            if (dvbtTableIdentifier == DVBTableSection.PMT)
                            {
                                if (iTableIDExtension != otsfLocalOSTransportStreamFilter.iTableExtension)
                                    return true;

                                var osCurrentOSService = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo?.GetService(iTableIDExtension);
                                if (osCurrentOSService != null)
                                {
                                    var iProgramClockReferencePacketIdentifier = ((byaCurrentBuffer[iCurrentBufferPosition + 8] & 0x1F) << 8) | byaCurrentBuffer[iCurrentBufferPosition + 9];
                                    lCurrentLogger.Trace($"PCRPID: 0x{iProgramClockReferencePacketIdentifier:X2} ({iProgramClockReferencePacketIdentifier})".Pastel(ConsoleColor.Cyan));

                                    osCurrentOSService.iProgramClockReferencePacketIdentifier = iProgramClockReferencePacketIdentifier;
                                    osCurrentOSService.iProgramMapTable = (ushort)opiiCurrentOSPacketIdentifierInfo.iPacketID;
                                    osCurrentOSService.iVideoPacketIdentifier = 0;
                                    osCurrentOSService.byVideoPacketIdentifierStreamType = 0;
                                    osCurrentOSService.liAudioPacketIdentifiers = [];
                                    osCurrentOSService.lbAudioPacketIdentifiersStreamType = [];
                                    osCurrentOSService.lsAudioPacketIdentifiersLanguage = [];

                                    var iProgramInfoLength = byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentBufferPosition + 10);
                                    lCurrentLogger.Trace($"Program Info Length: 0x{iProgramInfoLength:X2} ({iProgramInfoLength})".Pastel(ConsoleColor.Cyan));

                                    var iCurrentDescriptorBufferPosition = iCurrentBufferPosition + 12;
                                    if (iProgramInfoLength != 0)
                                    {
                                        if (lCurrentLogger.IsTraceEnabled)
                                            lCurrentLogger.Trace($"Program Descriptor Data: 0x{BitConverter.ToString(byaCurrentBuffer, iCurrentDescriptorBufferPosition, iProgramInfoLength).Replace("-", string.Empty)}".Pastel(ConsoleColor.Cyan));
                                        
                                        iCurrentDescriptorBufferPosition += iProgramInfoLength;
                                    }

                                    while (iCurrentDescriptorBufferPosition < iCurrentBufferPosition + iSectionLength - 4)
                                    {
                                        var iStreamType = byaCurrentBuffer[iCurrentDescriptorBufferPosition]; // 1 Byte
                                        lCurrentLogger.Trace($"Stream Type: 0x{iStreamType:X2} ({((MPEG2TSStreamType)iStreamType).ToString()})".Pastel(ConsoleColor.Cyan));

                                        var iElementaryPID = ((byaCurrentBuffer[iCurrentDescriptorBufferPosition + 1] & 0x1F) << 8) | byaCurrentBuffer[iCurrentDescriptorBufferPosition + 2];
                                        lCurrentLogger.Trace($"Elementary PID: 0x{iElementaryPID:X2} ({iElementaryPID})".Pastel(ConsoleColor.Cyan));

                                        var iESInfoLength = byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentDescriptorBufferPosition + 3);
                                        lCurrentLogger.Trace($"ES Info Length: 0x{iESInfoLength:X2} ({iESInfoLength})".Pastel(ConsoleColor.Cyan));

                                        var bAudioInformationExisting = false;

                                        switch ((MPEG2TSStreamType)iStreamType)
                                        {
                                            case MPEG2TSStreamType.ISO_IEC_11172_2_MPEG1_Video:
                                            case MPEG2TSStreamType.ITU_T_Rec_H_262_ISO_IEC_13818_2_MPEG2_Video:
                                            case MPEG2TSStreamType.ISO_IEC_14496_2_MPEG4_Video:
                                            case MPEG2TSStreamType.AVC_Stream_H264_Video:
                                            case MPEG2TSStreamType.H265_HEVC_Video:
                                            case MPEG2TSStreamType.Chinese_Video_Standard_CAVS_Video:
                                            case MPEG2TSStreamType.BBC_Dirac_Ultra_HD_Video:
                                            case MPEG2TSStreamType.Private_ES_VC1_Video:
                                            {
                                                osCurrentOSService.iVideoPacketIdentifier = iElementaryPID;
                                                osCurrentOSService.byVideoPacketIdentifierStreamType = iStreamType;
                                                break;
                                            }
                                            case MPEG2TSStreamType.ISO_IEC_11172_3_MPEG1_Audio:
                                            case MPEG2TSStreamType.ISO_IEC_13818_3_MPEG2_Audio:
                                            case MPEG2TSStreamType.ISO_IEC_13818_7_AAC_Audio:
                                            case MPEG2TSStreamType.ISO_IEC_14496_3_LATM_AAC_Audio:
                                            case MPEG2TSStreamType.A52_AC3_Audio:
                                            case MPEG2TSStreamType.HDMV_DTS_Audio:
                                            case MPEG2TSStreamType.LPCM_TrueHD_Audio:
                                            case MPEG2TSStreamType.ITU_T_Rec_H_222_0_ISO_IEC_13818_1_PES_Audio_Data:
                                            {
                                                bAudioInformationExisting = true;
                                                break;
                                            }
                                        }

                                        var iCurrentESDescriptorBufferPosition = iCurrentDescriptorBufferPosition + 5;
                                        int iESDescriptorEnd = iCurrentDescriptorBufferPosition + 5 + iESInfoLength;

                                        while (iCurrentESDescriptorBufferPosition < iESDescriptorEnd)
                                        {
                                            var iESDescriptorTag = byaCurrentBuffer[iCurrentESDescriptorBufferPosition];
                                            lCurrentLogger.Trace($"ES Descriptor Tag: 0x{iESDescriptorTag:X2} ({((ElementaryStreamDescriptors)iESDescriptorTag).ToString()})".Pastel(ConsoleColor.Cyan));
                                            var iESDescriptorLength = byaCurrentBuffer[iCurrentESDescriptorBufferPosition + 1];

                                            if (iESDescriptorLength != 0)
                                            {
                                                switch ((ElementaryStreamDescriptors)iESDescriptorTag)
                                                {
                                                    case ElementaryStreamDescriptors.teletext_descriptor:
                                                    {
                                                        osCurrentOSService.iTeletextPacketIdentifier = iElementaryPID;
                                                        break;
                                                    }
                                                    case ElementaryStreamDescriptors.subtitling_descriptor:
                                                    {
                                                        osCurrentOSService.iSubtitlePacketIdentifier = iElementaryPID;
                                                        break;
                                                    }
                                                    case ElementaryStreamDescriptors.AC_3_descriptor:
                                                    {
                                                        bAudioInformationExisting = true;
                                                        break;
                                                    }
                                                    case ElementaryStreamDescriptors.ISO_639_language_descriptor:
                                                    {
                                                        var strISO639LanguageCode = Encoding.UTF8.GetString(byaCurrentBuffer, iCurrentESDescriptorBufferPosition + 2, 3);
                                                        var lCurrentLanguage = ISO639LanguageCollection.AllISO639Languages.FirstOrDefault(llCurrentLanguage => llCurrentLanguage.ISO6393Code.ToLower().Equals(strISO639LanguageCode.ToLower(), StringComparison.InvariantCultureIgnoreCase));
                                                        strISO639LanguageCode = lCurrentLanguage != null ? lCurrentLanguage.ISO6393Code : ISO639LanguageCollection.AllISO639Languages.First(llCurrentLanguage => llCurrentLanguage.ISO6393Code.ToLower().Equals("mis", StringComparison.InvariantCultureIgnoreCase)).ISO6393Code;
                                                        lCurrentLogger.Trace($"ISO639 Language Code: 0x{strISO639LanguageCode.ToHexString()} ({strISO639LanguageCode})".Pastel(ConsoleColor.Cyan));
                                                        osCurrentOSService.lsAudioPacketIdentifiersLanguage.Add(strISO639LanguageCode);
                                                        break;
                                                    }
                                                }
                                            }

                                            iCurrentESDescriptorBufferPosition += 2 + iESDescriptorLength;
                                        }

                                        if (bAudioInformationExisting)
                                        {
                                            osCurrentOSService.liAudioPacketIdentifiers.Add(iElementaryPID);
                                            osCurrentOSService.lbAudioPacketIdentifiersStreamType.Add(iStreamType);

                                            if (osCurrentOSService.iTeletextPacketIdentifier != 0)
                                                osCurrentOSService.liAudioPacketIdentifiers.Remove(osCurrentOSService.iTeletextPacketIdentifier);

                                            if (osCurrentOSService.iSubtitlePacketIdentifier != 0)
                                                osCurrentOSService.liAudioPacketIdentifiers.Remove(osCurrentOSService.iSubtitlePacketIdentifier);
                                        }

                                        iCurrentDescriptorBufferPosition += 5 + iESInfoLength;
                                    }

                                    osCurrentOSService.bGotFromProgramMapTable = true;
                                }
                            }

                            int iCrcIndex = iCurrentBufferPosition + iSectionLength - 4;
                            long iCRC32 = ((long)byaCurrentBuffer[iCrcIndex] << 24) | ((long)byaCurrentBuffer[iCrcIndex + 1] << 16) | ((long)byaCurrentBuffer[iCrcIndex + 2] << 8) | byaCurrentBuffer[iCrcIndex + 3];
                            lCurrentLogger.Trace($"CRC32: 0x{iCRC32:X8} ({iCRC32})".Pastel(ConsoleColor.Cyan));
                            long baCalculatedCRC32 = BitConverter.ToInt32(crc32CurrentCalculatedCRC32.ComputeHash(byaCurrentBuffer, iCurrentBufferPosition, iSectionLength - 4), 0);
                            lCurrentLogger.Trace($"Calculated CRC32: 0x{baCalculatedCRC32:X8} ({baCalculatedCRC32})".Pastel(ConsoleColor.Cyan));
                            iCurrentBufferPosition += iSectionLength;
                        }
                    }
                }

                return true;
            }

            public bool AddNetworkInformationTableData()
            {
                lCurrentLogger.Trace("OSTransportStreamFilter.AddNetworkInformationTableData()".Pastel(ConsoleColor.Cyan));

                var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
                var osiCurrentOSScanIP = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder?.osiOSScanIP;
                var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;

                if (opiiCurrentOSPacketIdentifierInfo is { otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo: not null } && byaCurrentBuffer != null)
                {
                    var usCurrentBufferStringLength = byaCurrentBuffer.ToDVBLengthAsInteger(1) + 3;
                    var usCurrentNetworkIdentifier = byaCurrentBuffer.ToUShortAsInteger(3);
                    var usCurrentNetworkDataLength = byaCurrentBuffer.ToDVBLengthAsInteger(8);
                    var usCurrentTransportStreamDataPosition = 10 + usCurrentNetworkDataLength;

                    if ((byaCurrentBuffer[1] & 0x80) != 0)
                        usCurrentBufferStringLength -= 4;

                    if (opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.bUseNetworkInformationTable)
                    {
                        int iCurrentTableDataLength;

                        for (var iTransponderInfoItterator = usCurrentTransportStreamDataPosition + 2; iTransponderInfoItterator < usCurrentBufferStringLength; iTransponderInfoItterator += 6 + iCurrentTableDataLength)
                        {
                            var otiCurrentOSTransponderInfo = new OSTransponderInfo
                            {
                                iTransportStreamID = byaCurrentBuffer.ToUShortAsInteger(iTransponderInfoItterator),
                                iOriginalNetworkID = byaCurrentBuffer.ToUShortAsInteger(iTransponderInfoItterator + 2),
                                iNetworkID = usCurrentNetworkIdentifier,
                                bUseNetworkInformationTable = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.bUseNetworkInformationTable,
                                bScanEventInformationTable = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.bScanEventInformationTable
                            };

                            iCurrentTableDataLength = byaCurrentBuffer.ToDVBLengthAsInteger(iTransponderInfoItterator + 4);
                            int iDescriptorStart = iTransponderInfoItterator + 6;
                            int iDescriptorEnd = iDescriptorStart + iCurrentTableDataLength;

                            int iSubDescriptorItterator = iDescriptorStart;
                            while (iSubDescriptorItterator < iDescriptorEnd)
                            {
                                byte byDescriptorTag = byaCurrentBuffer[iSubDescriptorItterator];
                                byte byDescriptorLength = byaCurrentBuffer[iSubDescriptorItterator + 1];

                                switch (byDescriptorTag)
                                {
                                    case 0x43: // Satellite Delivery System Descriptor
                                    {
                                        otiCurrentOSTransponderInfo.iFrequency = byaCurrentBuffer.ToBCDAsInteger(iSubDescriptorItterator + 2, 8) / 100;
                                        otiCurrentOSTransponderInfo.iFrequencyFraction = 0;
                                        otiCurrentOSTransponderInfo.iPosition = byaCurrentBuffer.ToBCDAsInteger(iSubDescriptorItterator + 6, 4);
                                        otiCurrentOSTransponderInfo.iSymbolRate = byaCurrentBuffer.ToBCDAsInteger(iSubDescriptorItterator + 9, 7) / 10;

                                        byte byFlagsByte = byaCurrentBuffer[iSubDescriptorItterator + 8];
                                        otiCurrentOSTransponderInfo.iEAST = (byFlagsByte & 0x80) >> 7;
                                        otiCurrentOSTransponderInfo.iPolarisation = 1 ^ ((byFlagsByte & 0x60) >> 5);
                                        otiCurrentOSTransponderInfo.iRollOff = (byFlagsByte & 0x18) >> 3;
                                        otiCurrentOSTransponderInfo.iType = otiCurrentOSTransponderInfo.iModulationSystem = ((byFlagsByte & 0x04) >> 2) != 0 ? 6 : 5;
                                        otiCurrentOSTransponderInfo.iModulationType = byFlagsByte & 0x03;

                                        otiCurrentOSTransponderInfo.iFEC = byaCurrentBuffer[iSubDescriptorItterator + 12] & 0x0F;
                                        otiCurrentOSTransponderInfo.iSource = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.ostOSScanTransponder.otiOSTransponderInfo.iSource;

                                        osiCurrentOSScanIP?.AddTransponderInfo(otiCurrentOSTransponderInfo);

                                        break;
                                    }
                                    case 0x44: // Cable Delivery System Descriptor
                                    {
                                        int iCurrentFrequency = byaCurrentBuffer.ToBCDAsInteger(iSubDescriptorItterator + 2, 8);
                                        otiCurrentOSTransponderInfo.iFrequency = iCurrentFrequency / 10000;
                                        otiCurrentOSTransponderInfo.iFrequencyFraction = iCurrentFrequency % 10000;
                                        otiCurrentOSTransponderInfo.iSymbolRate = byaCurrentBuffer.ToBCDAsInteger(iSubDescriptorItterator + 9, 7) / 10;
                                        otiCurrentOSTransponderInfo.iModulationType = byaCurrentBuffer[iSubDescriptorItterator + 8];
                                        otiCurrentOSTransponderInfo.iModulationSystem = 1;
                                        otiCurrentOSTransponderInfo.iType = 1;

                                        if (otiCurrentOSTransponderInfo.iFrequency is >= 50 and <= 1000 && otiCurrentOSTransponderInfo.iSymbolRate is >= 1000 and <= 7100 && otiCurrentOSTransponderInfo.iModulationSystem is >= 1 and <= 5)
                                            osiCurrentOSScanIP?.AddTransponderInfo(otiCurrentOSTransponderInfo);
                                        
                                        break;
                                    }
                                }

                                iSubDescriptorItterator += 2 + byDescriptorLength;
                            }
                        }
                    }
                }

                return true;
            }

            public bool AddServiceDescriptionTableData()
            {
                lCurrentLogger.Trace("OSTransportStreamFilter.AddServiceDescriptionTableData()".Pastel(ConsoleColor.Cyan));

                var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
                var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;
                var iCurrentTransportStreamID = byaCurrentBuffer.ToUShortAsInteger(3);
                var iCurrentOriginalNetworkID = byaCurrentBuffer.ToUShortAsInteger(8);

                if (byaCurrentBuffer != null)
                {
                    int iCurrentDescriptorsLength;

                    for (var iCurrentServicePosition = 11; iCurrentServicePosition < opiiCurrentOSPacketIdentifierInfo?.iBufferLength - 4; iCurrentServicePosition += iCurrentDescriptorsLength)
                    {
                        var iCurrentServiceID = byaCurrentBuffer.ToUShortAsInteger(iCurrentServicePosition);
                        iCurrentDescriptorsLength = byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentServicePosition + 3) + 5;
                        var osCurrentOSService = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo?.GetService(iCurrentServiceID);

                        if (osCurrentOSService != null)
                        {
                            osCurrentOSService.iOriginalNetworkID = iCurrentOriginalNetworkID;
                            osCurrentOSService.iTransportStreamID = iCurrentTransportStreamID;

                            var byEitFlagsByte = byaCurrentBuffer[iCurrentServicePosition + 2];
                            osCurrentOSService.iEventInformationTableSchedule = (byEitFlagsByte & 0x02) >> 1;
                            osCurrentOSService.iEventInformationTablePresentFollowing = byEitFlagsByte & 0x01;

                            var byStatusFlagsByte = byaCurrentBuffer[iCurrentServicePosition + 3];
                            osCurrentOSService.iConditionalAccessMode = (byStatusFlagsByte & 0x10) >> 4;
                            osCurrentOSService.iRunningStatus = (byStatusFlagsByte & 0xE0) >> 5;

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

                                    osCurrentOSService.strProviderName = osCurrentOSService.strProviderName.CleanupInvalidData();
                                    osCurrentOSService.strName = osCurrentOSService.strName.CleanupInvalidData();

                                    osCurrentOSService.bGotFromServiceDescriptorTable = true;
                                }
                            }
                        }
                    }

                    return true;
                }

                return false;
            }

            public bool AddEventInformationTableData(bool bLocalDoRefresh)
            {
                lCurrentLogger.Trace("OSTransportStreamFilter.AddEventInformationTableData()".Pastel(ConsoleColor.Cyan));

                OSPacketIdentifierInfo? opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
                var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;

                if (byaCurrentBuffer != null)
                {
                    var bCurrentTableID = byaCurrentBuffer[0];

                    var iCurrentSectionLength = byaCurrentBuffer.ToDVBLengthAsInteger(1) + 3;
                    var iCurrentServiceID = byaCurrentBuffer.ToUShortAsInteger(3);
                    var iCurrentTransportStreamID = byaCurrentBuffer.ToUShortAsInteger(8);
                    var iCurrentOriginalNetworkID = byaCurrentBuffer.ToUShortAsInteger(10);
                    var osCurrentOSService = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo?.GetService(iCurrentServiceID);
                    int iCurrentTableDataLengthLimit;

                    if ((byaCurrentBuffer[1] & 0x80) != 0)
                        iCurrentSectionLength -= 4;

                    if (bLocalDoRefresh && osCurrentOSService != null)
                    {
                        using var enumerator = osCurrentOSService.oloeOSListOSEvent.GetEnumerator();

                        if (enumerator.MoveNext())
                        {
                            var hasNext = true;
                            while (hasNext)
                            {
                                var currentEvent = enumerator.Current;
                                hasNext = enumerator.MoveNext();

                                if (currentEvent.bTableID != bCurrentTableID) 
                                    continue;
                                
                                osCurrentOSService.oloeOSListOSEvent.Remove(currentEvent);
                                iEITEventsDeleted += 1;
                            }
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
                            iEventID = byaCurrentBuffer.ToUShortAsInteger(iCurrentItterator),
                            iModifiedJulianDate = byaCurrentBuffer.ToUShortAsInteger(iCurrentItterator + 2),

                            bStartHour = Convert.ToByte(byaCurrentBuffer.ToBCDAsInteger(iCurrentItterator + 4, 2)),
                            bStartMinute = Convert.ToByte(byaCurrentBuffer.ToBCDAsInteger(iCurrentItterator + 5, 2)),
                            bStartSecond = Convert.ToByte(byaCurrentBuffer.ToBCDAsInteger(iCurrentItterator + 6, 2)),
                            bDurationHour = Convert.ToByte(byaCurrentBuffer.ToBCDAsInteger(iCurrentItterator + 7, 2)),
                            bDurationMinute = Convert.ToByte(byaCurrentBuffer.ToBCDAsInteger(iCurrentItterator + 8, 2)),
                            bDurationSecond = Convert.ToByte(byaCurrentBuffer.ToBCDAsInteger(iCurrentItterator + 9, 2))
                        };

                        iCurrentTableDataLengthLimit = byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentItterator + 10);
                        iEITEvents += 1;

                        for (var iCurrentSubItterator = 0; iCurrentSubItterator < iCurrentTableDataLengthLimit; iCurrentSubItterator += iCurrentTableDataLength + 2)
                        {
                            var iCurrentItteratorOffset = iCurrentItterator + iCurrentSubItterator + 12;
                            var iCurrentDescriptorsTag = byaCurrentBuffer[iCurrentItteratorOffset];
                            iCurrentTableDataLength = byaCurrentBuffer[iCurrentItteratorOffset + 1];

                            switch (iCurrentDescriptorsTag)
                            {
                                case 0x4D: // Short Event Descriptor
                                {
                                    if (iCurrentTableDataLength >= 5)
                                    {
                                        oeCurrentOSEvent.strLanguage = Encoding.ASCII.GetString(byaCurrentBuffer, iCurrentItteratorOffset + 2, 3).ToLower();

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
                                            oeCurrentOSEvent.strName = eCurrentEncoding.GetString(byaCurrentBuffer, iCurrentItteratorOffset + 1, iCurrentStringLength).CleanupInvalidData();

                                        iEITShortSize += iCurrentStringLength;
                                        iCurrentItteratorOffset += iCurrentStringLength + 1;
                                        iCurrentStringLength = byaCurrentBuffer[iCurrentItteratorOffset];

                                        if (iCurrentStringLength > 0)
                                            oeCurrentOSEvent.strText = eCurrentEncoding.GetString(byaCurrentBuffer, iCurrentItteratorOffset + 1, iCurrentStringLength).CleanupInvalidData();

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
}