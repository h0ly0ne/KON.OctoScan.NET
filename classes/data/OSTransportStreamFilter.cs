using System.Collections;
using System.Globalization;
using System.Text;

using DreamersCode.Utilities.Lookups.Languages;
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
                                            osCurrentOSService.iaAudioPacketIdentifier[osCurrentOSService.byAudioChannels++] = iExtractedPID;

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
                                                osCurrentOSService.iaAudioPacketIdentifier[osCurrentOSService.byAudioChannels++] = iExtractedPID;
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
                int iTableDataLengthLimit;

                for (var iCurrentItterator = 11; iCurrentItterator < opiiCurrentOSPacketIdentifierInfo?.iBufferLength - 4; iCurrentItterator += iTableDataLengthLimit + 5)
                {
                    var iCurrentServiceID = ToUShortAsInteger(byaCurrentBuffer?[iCurrentItterator..]);
                    var osCurrentOSService = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo.GetService(iCurrentServiceID);
                    iTableDataLengthLimit = ToByteAsInteger(byaCurrentBuffer?[(iCurrentItterator + 3)..]);

                    if (osCurrentOSService != null)
                    {
                        osCurrentOSService.iOriginalNetworkID = iCurrentOriginalNetworkID;
                        osCurrentOSService.iTransportStreamID = iCurrentTransportStreamID;
                        osCurrentOSService.iEventInformationTableSchedule = (byaCurrentBuffer![iCurrentItterator + 2] & 0x02) >> 1;
                        osCurrentOSService.iEventInformationTablePresentFollowing = byaCurrentBuffer[iCurrentItterator + 2] & 0x01;
                        osCurrentOSService.iConditionalAccessMode = (byaCurrentBuffer[iCurrentItterator + 3] & 0x10) >> 4;

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

                        int iTableDataLength;

                        for (var iCurrentSubItterator = 0; iCurrentSubItterator < iTableDataLengthLimit; iCurrentSubItterator += iTableDataLength + 2)
                        {
                            var iCurrentItteratorOffset = iCurrentItterator + iCurrentSubItterator + 5;
                            var iCurrentDescriptorsTag = byaCurrentBuffer[iCurrentItteratorOffset];
                            iTableDataLength = byaCurrentBuffer[iCurrentItteratorOffset + 1];

                            if (iCurrentDescriptorsTag == DESCRIPTOR_TAG_SERVICE_DESCRIPTOR)
                            {
                                var iCurrentStringProviderNameLength = byaCurrentBuffer[iCurrentItteratorOffset + 3];
                                var iCurrentStringNameLength = byaCurrentBuffer[iCurrentItteratorOffset + 4 + iCurrentStringProviderNameLength];

                                osCurrentOSService.strProviderName = new string(Convert.ToChar(0x00), 80);
                                osCurrentOSService.strServiceName = new string(Convert.ToChar(0x00), 80);

                                osCurrentOSService.strProviderName = osCurrentOSService.strProviderName.ConvertEN300468StringToUTF8(byaCurrentBuffer[(iCurrentItteratorOffset + 4)..], iCurrentStringProviderNameLength);
                                if (osCurrentOSService.strProviderName[79] != 0)
                                    lCurrentLogger.Warn($"********************************************* PROVIDERNAME OVERFLOW {osCurrentOSService.strProviderName} LENGTH = {iCurrentStringProviderNameLength}".Pastel(ConsoleColor.Yellow));

                                osCurrentOSService.strServiceName = osCurrentOSService.strServiceName.ConvertEN300468StringToUTF8(byaCurrentBuffer[(iCurrentItteratorOffset + 5 + iCurrentStringProviderNameLength)..], iCurrentStringNameLength);
                                if (osCurrentOSService.strServiceName[79] != 0)
                                    lCurrentLogger.Warn($"********************************************* SERVICENAME OVERFLOW {osCurrentOSService.strServiceName} LENGTH = {iCurrentStringNameLength}".Pastel(ConsoleColor.Yellow));

                                osCurrentOSService.strProviderName = osCurrentOSService.strProviderName.CleanupString();
                                osCurrentOSService.strServiceName = osCurrentOSService.strServiceName.CleanupString();

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
                                    var byaLanguage = new byte[3];
                                    byaLanguage[0] = byaCurrentBuffer[iCurrentItteratorOffset + 2];
                                    byaLanguage[1] = byaCurrentBuffer[iCurrentItteratorOffset + 3];
                                    byaLanguage[2] = byaCurrentBuffer[iCurrentItteratorOffset + 4];
                                    oeCurrentOSEvent.strLanguage = Encoding.ASCII.GetString(byaLanguage).ToLower();

                                    var eCurrentEncoding = Encoding.UTF8;
                                    var lCurrentLanguage = LanguageCollection.AllLanguages.First(lCurrentLanguage => lCurrentLanguage.ThreeLetterCode.ToLower().Equals(oeCurrentOSEvent.strLanguage.ToLower(), StringComparison.InvariantCultureIgnoreCase));

                                    if (lCurrentLanguage.TwoLetterCode != null)
                                    {
                                        var ciCurrentCultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).First(ciCurrentCultureInfo => ciCurrentCultureInfo.TwoLetterISOLanguageName.ToLower().Equals(lCurrentLanguage.TwoLetterCode.ToLower(), StringComparison.InvariantCultureIgnoreCase));
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