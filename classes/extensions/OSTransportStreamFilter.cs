using System.Buffers.Binary;
using System.Text;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Enums;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public static class OSTransportStreamFilter_Extension
{
    extension(OSTransportStreamFilter otsfLocalOSTransportStreamFilter)
    {
        /// <summary>
        /// OSTransportStreamFilter.AddProgramAssociationTableData()
        /// </summary>
        /// <returns>bool</returns>
        public bool AddProgramAssociationTableData()
        {
            LogTrace("OSTransportStreamFilter.AddProgramAssociationTableData()");

            var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;
            var ostCurrentOSScanTransponder = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder;

            if (opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo == null || byaCurrentBuffer == null)
                return true;

            var iNetworkInformationTablePacketIdentifier = DEFAULT_PID_NIT;
            var iSectionLength = opiiCurrentOSPacketIdentifierInfo.iBufferLength;

            otsfLocalOSTransportStreamFilter.iTableExtension = byaCurrentBuffer.ToUShortAsInteger(3);
            opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.iTransportStreamID = otsfLocalOSTransportStreamFilter.iTableExtension;

            for (var i = 8; i < iSectionLength - 4; i += 4)
            {
                var iProgramNumber = byaCurrentBuffer.ToUShortAsInteger(i);
                var iPacketIdentifier = byaCurrentBuffer.ToPIDAsInteger(i + 2);

                if (iProgramNumber != 0)
                    ostCurrentOSScanTransponder?.AddSFilter(iPacketIdentifier, DEFAULT_TID_PM, iProgramNumber, 2, FILTER_TIMEOUT_PMT);
                else
                    iNetworkInformationTablePacketIdentifier = iPacketIdentifier;
            }

            if (ostCurrentOSScanTransponder?.otiOSTransponderInfo is { bUseNetworkInformationTable: true })
                ostCurrentOSScanTransponder.AddSFilter(iNetworkInformationTablePacketIdentifier, DEFAULT_TID_NI_CURRENT, 0, 1, FILTER_TIMEOUT_NIT);

            return true;
        }

        /// <summary>
        /// OSTransportStreamFilter.AddProgramMapTableData()
        /// </summary>
        /// <returns>bool</returns>
        public bool AddProgramMapTableData()
        {
            LogTrace("OSTransportStreamFilter.AddProgramMapTableData()");

            var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;

            if (opiiCurrentOSPacketIdentifierInfo == null || byaCurrentBuffer == null)
                return true;

            var iServiceID = byaCurrentBuffer.ToUShortAsInteger(3);

            if (lCurrentLogger.IsTraceEnabled)
            {
                var iSectionLength = opiiCurrentOSPacketIdentifierInfo.iBufferLength;
                var iVersionNumber = (byaCurrentBuffer[5] & 0x3E) >> 1;
                var iCurrentNextIndicator = byaCurrentBuffer[5] & 0x01;

                LogTrace($"Section Length: 0x{iSectionLength:X2} ({iSectionLength})");
                LogTrace($"SID (Table ID Extension): 0x{iServiceID:X4} ({iServiceID})");
                LogTrace($"Version Number: 0x{iVersionNumber:X2} ({iVersionNumber})");
                LogTrace($"Current/Next Indicator: 0x{iCurrentNextIndicator:X2} ({iCurrentNextIndicator})");
                LogTrace($"Section Number: 0x{byaCurrentBuffer[6]:X2} ({byaCurrentBuffer[6]})");
                LogTrace($"Last Section Number: 0x{byaCurrentBuffer[7]:X2} ({byaCurrentBuffer[7]})");
            }

            if (byaCurrentBuffer[0] != DEFAULT_TID_PM || iServiceID != otsfLocalOSTransportStreamFilter.iTableExtension)
                return true;

            var osCurrentOSService = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo?.GetService(iServiceID);

            if (osCurrentOSService == null)
                return true;

            osCurrentOSService.iProgramClockReferencePacketIdentifier = byaCurrentBuffer.ToPIDAsInteger(8);
            osCurrentOSService.iProgramMapTable = opiiCurrentOSPacketIdentifierInfo.iPacketID;
            osCurrentOSService.iVideoPacketIdentifier = 0;
            osCurrentOSService.byVideoPacketIdentifierStreamType = 0;
            osCurrentOSService.iSubtitlePacketIdentifier = 0;
            osCurrentOSService.iTeletextPacketIdentifier = 0;
            osCurrentOSService.liAudioPacketIdentifiers = [];
            osCurrentOSService.lbAudioPacketIdentifiersStreamType = [];
            osCurrentOSService.lsAudioPacketIdentifiersLanguage = [];
            osCurrentOSService.liOtherPacketIdentifiers = [];
            osCurrentOSService.lbOtherPacketIdentifiersStreamType = [];

            var iSectionEnd = opiiCurrentOSPacketIdentifierInfo.iBufferLength - 4;
            var iProgramInfoLength = byaCurrentBuffer.ToDVBLengthAsInteger(10);
            var iCurrentStreamPosition = 12 + iProgramInfoLength;

            if (lCurrentLogger.IsTraceEnabled)
            {
                LogTrace($"PCRPID: 0x{osCurrentOSService.iProgramClockReferencePacketIdentifier:X2} ({osCurrentOSService.iProgramClockReferencePacketIdentifier})");
                LogTrace($"Program Info Length: 0x{iProgramInfoLength:X2} ({iProgramInfoLength})");

                if (iProgramInfoLength != 0 && 12 + iProgramInfoLength <= iSectionEnd)
                    LogTrace($"Program Descriptor Data: 0x{Convert.ToHexString(byaCurrentBuffer, 12, iProgramInfoLength)}");
            }

            while (iCurrentStreamPosition + 5 <= iSectionEnd)
            {
                var byStreamType = byaCurrentBuffer[iCurrentStreamPosition];
                var iElementaryPID = byaCurrentBuffer.ToPIDAsInteger(iCurrentStreamPosition + 1);
                var iESInfoLength = byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentStreamPosition + 3);
                var iESDescriptorEnd = Math.Min(iCurrentStreamPosition + 5 + iESInfoLength, iSectionEnd);

                if (lCurrentLogger.IsTraceEnabled)
                {
                    LogTrace($"Stream Type: 0x{byStreamType:X2} ({(MPEG2TSStreamType)byStreamType})");
                    LogTrace($"Elementary PID: 0x{iElementaryPID:X2} ({iElementaryPID})");
                    LogTrace($"ES Info Length: 0x{iESInfoLength:X2} ({iESInfoLength})");
                }

                var bIsVideo = IsVideoStreamType(byStreamType);
                var bIsAudio = IsAudioStreamType(byStreamType);
                var bIsTeletext = false;
                var bIsSubtitle = false;
                string? strLanguage = null;

                for (var iCurrentESDescriptorPosition = iCurrentStreamPosition + 5; iCurrentESDescriptorPosition + 2 <= iESDescriptorEnd; iCurrentESDescriptorPosition += 2 + byaCurrentBuffer[iCurrentESDescriptorPosition + 1])
                {
                    var iESDescriptorLength = byaCurrentBuffer[iCurrentESDescriptorPosition + 1];

                    if (lCurrentLogger.IsTraceEnabled)
                        LogTrace($"ES Descriptor Tag: 0x{byaCurrentBuffer[iCurrentESDescriptorPosition]:X2} ({(ElementaryStreamDescriptors)byaCurrentBuffer[iCurrentESDescriptorPosition]})");

                    switch ((ElementaryStreamDescriptors)byaCurrentBuffer[iCurrentESDescriptorPosition])
                    {
                        case ElementaryStreamDescriptors.teletext_descriptor:
                            bIsTeletext = true;
                            break;
                        case ElementaryStreamDescriptors.subtitling_descriptor:
                            bIsSubtitle = true;
                            break;
                        case ElementaryStreamDescriptors.AC_3_descriptor:
                        case ElementaryStreamDescriptors.enhanced_ac3_descriptor:
                        case ElementaryStreamDescriptors.DTS_descriptor:
                        case ElementaryStreamDescriptors.AAC_descriptor:
                            bIsAudio |= iESDescriptorLength != 0;
                            break;
                        case ElementaryStreamDescriptors.extension_descriptor when iESDescriptorLength >= 1 && EXTENSION_DESCRIPTOR_AUDIO_TAGS.Contains(byaCurrentBuffer[iCurrentESDescriptorPosition + 2]):
                            bIsAudio = true;
                            break;
                        case ElementaryStreamDescriptors.ISO_639_language_descriptor when iESDescriptorLength >= 3:
                            strLanguage ??= GetISO639LanguageCode(Encoding.ASCII.GetString(byaCurrentBuffer, iCurrentESDescriptorPosition + 2, 3));

                            if (lCurrentLogger.IsTraceEnabled)
                                LogTrace($"ISO639 Language Code: 0x{Convert.ToHexString(Encoding.ASCII.GetBytes(strLanguage))} ({strLanguage})");

                            break;
                    }
                }

                if (bIsVideo)
                {
                    osCurrentOSService.iVideoPacketIdentifier = iElementaryPID;
                    osCurrentOSService.byVideoPacketIdentifierStreamType = byStreamType;
                }
                else if (bIsTeletext)
                    osCurrentOSService.iTeletextPacketIdentifier = iElementaryPID;
                else if (bIsSubtitle)
                    osCurrentOSService.iSubtitlePacketIdentifier = iElementaryPID;
                else if (bIsAudio)
                {
                    osCurrentOSService.liAudioPacketIdentifiers.Add(iElementaryPID);
                    osCurrentOSService.lbAudioPacketIdentifiersStreamType.Add(byStreamType);
                    osCurrentOSService.lsAudioPacketIdentifiersLanguage.Add(strLanguage ?? "und");
                }
                else
                {
                    osCurrentOSService.liOtherPacketIdentifiers.Add(iElementaryPID);
                    osCurrentOSService.lbOtherPacketIdentifiersStreamType.Add(byStreamType);
                }

                iCurrentStreamPosition += 5 + iESInfoLength;
            }

            if (lCurrentLogger.IsTraceEnabled && iSectionEnd >= 0)
            {
                long lCRC32 = BinaryPrimitives.ReadUInt32BigEndian(byaCurrentBuffer.AsSpan(iSectionEnd, 4));
                long lCalculatedCRC32 = GetCRC32(byaCurrentBuffer.AsSpan(0, iSectionEnd));

                LogTrace($"CRC32: 0x{lCRC32:X8} ({lCRC32})");
                LogTrace($"Calculated CRC32: 0x{lCalculatedCRC32:X8} ({lCalculatedCRC32})");
            }

            osCurrentOSService.bGotFromProgramMapTable = true;

            return true;
        }

        /// <summary>
        /// OSTransportStreamFilter.AddNetworkInformationTableData()
        /// </summary>
        /// <returns>bool</returns>
        public bool AddNetworkInformationTableData()
        {
            LogTrace("OSTransportStreamFilter.AddNetworkInformationTableData()");

            var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            var osiCurrentOSScanIP = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder?.osiOSScanIP;
            var otiParentOSTransponderInfo = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo;
            var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;

            if (opiiCurrentOSPacketIdentifierInfo == null || otiParentOSTransponderInfo is not { bUseNetworkInformationTable: true } || byaCurrentBuffer == null)
                return true;

            var iSectionEnd = opiiCurrentOSPacketIdentifierInfo.iBufferLength - ((byaCurrentBuffer[1] & 0x80) != 0 ? 4 : 0);
            var iNetworkIdentifier = byaCurrentBuffer.ToUShortAsInteger(3);
            var iTransportStreamDataPosition = 10 + byaCurrentBuffer.ToDVBLengthAsInteger(8);

            for (var iTransponderInfoPosition = iTransportStreamDataPosition + 2; iTransponderInfoPosition + 6 <= iSectionEnd; iTransponderInfoPosition += 6 + byaCurrentBuffer.ToDVBLengthAsInteger(iTransponderInfoPosition + 4))
            {
                var iDescriptorEnd = Math.Min(iTransponderInfoPosition + 6 + byaCurrentBuffer.ToDVBLengthAsInteger(iTransponderInfoPosition + 4), iSectionEnd);

                for (var iDescriptorPosition = iTransponderInfoPosition + 6; iDescriptorPosition + 2 <= iDescriptorEnd; iDescriptorPosition += 2 + byaCurrentBuffer[iDescriptorPosition + 1])
                {
                    var byDescriptorTag = byaCurrentBuffer[iDescriptorPosition];

                    if (byDescriptorTag is not (DESCRIPTORTAG_SATELLITE_DELIVERY_SYSTEM_DESCRIPTOR or DESCRIPTORTAG_CABLE_DELIVERY_SYSTEM_DESCRIPTOR))
                        continue;

                    var otiCurrentOSTransponderInfo = new OSTransponderInfo
                    {
                        iTransportStreamID = byaCurrentBuffer.ToUShortAsInteger(iTransponderInfoPosition),
                        iOriginalNetworkID = byaCurrentBuffer.ToUShortAsInteger(iTransponderInfoPosition + 2),
                        iNetworkID = iNetworkIdentifier,
                        bUseNetworkInformationTable = otiParentOSTransponderInfo.bUseNetworkInformationTable,
                        bScanEventInformationTable = otiParentOSTransponderInfo.bScanEventInformationTable,
                        iaEventInformationTableServiceID = (int[])otiParentOSTransponderInfo.iaEventInformationTableServiceID.Clone(),
                        iInputStreamID = otiParentOSTransponderInfo.iInputStreamID
                    };

                    if (byDescriptorTag == DESCRIPTORTAG_SATELLITE_DELIVERY_SYSTEM_DESCRIPTOR)
                    {
                        otiCurrentOSTransponderInfo.iFrequency = byaCurrentBuffer.ToBCDAsInteger(iDescriptorPosition + 2, 8) / 100;
                        otiCurrentOSTransponderInfo.iFrequencyFraction = 0;
                        otiCurrentOSTransponderInfo.iPosition = byaCurrentBuffer.ToBCDAsInteger(iDescriptorPosition + 6, 4);
                        otiCurrentOSTransponderInfo.iSymbolRate = byaCurrentBuffer.ToBCDAsInteger(iDescriptorPosition + 9, 7) / 10;

                        var byFlagsByte = byaCurrentBuffer[iDescriptorPosition + 8];
                        otiCurrentOSTransponderInfo.iEAST = (byFlagsByte & 0x80) >> 7;
                        otiCurrentOSTransponderInfo.iPolarisation = 1 ^ ((byFlagsByte & 0x60) >> 5);
                        otiCurrentOSTransponderInfo.iRollOff = (byFlagsByte & 0x18) >> 3;
                        otiCurrentOSTransponderInfo.iType = otiCurrentOSTransponderInfo.iModulationSystem = (byFlagsByte & 0x04) != 0 ? 6 : 5;
                        otiCurrentOSTransponderInfo.iModulationType = (byFlagsByte & 0x03) switch
                        {
                            1 => Array.IndexOf(ModulationType2String, "qpsk"),
                            2 => Array.IndexOf(ModulationType2String, "8psk"),
                            3 => Array.IndexOf(ModulationType2String, "16qam"),
                            _ => -1
                        };

                        otiCurrentOSTransponderInfo.iFEC = byaCurrentBuffer[iDescriptorPosition + 12] & 0x0F;
                        otiCurrentOSTransponderInfo.iSource = otiParentOSTransponderInfo.iSource;

                        osiCurrentOSScanIP?.AddTransponderInfo(otiCurrentOSTransponderInfo);
                    }
                    else
                    {
                        var iCurrentFrequency = byaCurrentBuffer.ToBCDAsInteger(iDescriptorPosition + 2, 8);
                        otiCurrentOSTransponderInfo.iFrequency = iCurrentFrequency / 10000;
                        otiCurrentOSTransponderInfo.iFrequencyFraction = iCurrentFrequency % 10000;
                        otiCurrentOSTransponderInfo.iSymbolRate = byaCurrentBuffer.ToBCDAsInteger(iDescriptorPosition + 9, 7) / 10;
                        otiCurrentOSTransponderInfo.iModulationType = byaCurrentBuffer[iDescriptorPosition + 8];
                        otiCurrentOSTransponderInfo.iModulationSystem = 1;
                        otiCurrentOSTransponderInfo.iType = 1;

                        if (otiCurrentOSTransponderInfo is { iFrequency: >= 50 and <= 1000, iSymbolRate: >= 1000 and <= 7100, iModulationType: >= 1 and <= 5 })
                            osiCurrentOSScanIP?.AddTransponderInfo(otiCurrentOSTransponderInfo);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// OSTransportStreamFilter.AddServiceDescriptionTableData()
        /// </summary>
        /// <returns>bool</returns>
        public bool AddServiceDescriptionTableData()
        {
            LogTrace("OSTransportStreamFilter.AddServiceDescriptionTableData()");

            var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;
            var ostCurrentOSScanTransponder = opiiCurrentOSPacketIdentifierInfo?.otsiOSTransportStreamInfo?.ostOSScanTransponder;
            var otiCurrentOSTransponderInfo = ostCurrentOSScanTransponder?.otiOSTransponderInfo;

            if (opiiCurrentOSPacketIdentifierInfo == null || byaCurrentBuffer == null)
                return false;

            var iCurrentTransportStreamID = byaCurrentBuffer.ToUShortAsInteger(3);

            if (opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo != null)
                opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo.iServiceDescriptionTransportStreamID = iCurrentTransportStreamID;
            var iCurrentOriginalNetworkID = byaCurrentBuffer.ToUShortAsInteger(8);
            var iSectionEnd = opiiCurrentOSPacketIdentifierInfo.iBufferLength - 4;

            for (var iCurrentServicePosition = 11; iCurrentServicePosition + 5 <= iSectionEnd; iCurrentServicePosition += 5 + byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentServicePosition + 3))
            {
                var iCurrentServiceID = byaCurrentBuffer.ToUShortAsInteger(iCurrentServicePosition);
                var iCurrentDescriptorsEnd = Math.Min(iCurrentServicePosition + 5 + byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentServicePosition + 3), iSectionEnd);
                var osCurrentOSService = otiCurrentOSTransponderInfo?.GetService(iCurrentServiceID);

                if (osCurrentOSService == null)
                    continue;

                osCurrentOSService.iOriginalNetworkID = iCurrentOriginalNetworkID;
                osCurrentOSService.iTransportStreamID = iCurrentTransportStreamID;

                var byEitFlagsByte = byaCurrentBuffer[iCurrentServicePosition + 2];
                osCurrentOSService.iEventInformationTableSchedule = (byEitFlagsByte & 0x02) >> 1;
                osCurrentOSService.iEventInformationTablePresentFollowing = byEitFlagsByte & 0x01;

                var byStatusFlagsByte = byaCurrentBuffer[iCurrentServicePosition + 3];
                osCurrentOSService.iConditionalAccessMode = (byStatusFlagsByte & 0x10) >> 4;
                osCurrentOSService.iRunningStatus = (byStatusFlagsByte & 0xE0) >> 5;

                if (otiCurrentOSTransponderInfo is { bScanEventInformationTable: true } && osCurrentOSService.iEventInformationTableSchedule != 0)
                    if (otiCurrentOSTransponderInfo.iaEventInformationTableServiceID[0] == 0 || otiCurrentOSTransponderInfo.iaEventInformationTableServiceID.Contains(iCurrentServiceID))
                        ostCurrentOSScanTransponder?.AddSFilter(DEFAULT_PID_EIT, DEFAULT_TID_EI, iCurrentServiceID, 2, FILTER_TIMEOUT_EIT);

                for (var iCurrentDescriptorPosition = iCurrentServicePosition + 5; iCurrentDescriptorPosition + 2 <= iCurrentDescriptorsEnd; iCurrentDescriptorPosition += 2 + byaCurrentBuffer[iCurrentDescriptorPosition + 1])
                {
                    if (byaCurrentBuffer[iCurrentDescriptorPosition] != DESCRIPTORTAG_SERVICE_DESCRIPTOR || byaCurrentBuffer[iCurrentDescriptorPosition + 1] < 3)
                        continue;

                    var iDescriptorEnd = Math.Min(iCurrentDescriptorPosition + 2 + byaCurrentBuffer[iCurrentDescriptorPosition + 1], iCurrentDescriptorsEnd);
                    var iProviderNamePosition = iCurrentDescriptorPosition + 4;
                    var iProviderNameLength = byaCurrentBuffer[iCurrentDescriptorPosition + 3];
                    var iServiceNamePosition = iProviderNamePosition + iProviderNameLength + 1;

                    if (iServiceNamePosition > iDescriptorEnd || iServiceNamePosition + byaCurrentBuffer[iServiceNamePosition - 1] > iDescriptorEnd)
                    {
                        LogTrace($"Service descriptor of SID {iCurrentServiceID} with invalid name lengths skipped");
                        continue;
                    }

                    var iServiceNameLength = byaCurrentBuffer[iServiceNamePosition - 1];

                    osCurrentOSService.strProviderName = DecodeDVBString(byaCurrentBuffer.AsSpan(iProviderNamePosition, iProviderNameLength));
                    osCurrentOSService.strName = DecodeDVBString(byaCurrentBuffer.AsSpan(iServiceNamePosition, iServiceNameLength));
                    osCurrentOSService.bGotFromServiceDescriptorTable = true;
                }
            }

            return true;
        }

        /// <summary>
        /// OSTransportStreamFilter.AddEventInformationTableData()
        /// </summary>
        /// <param name="bLocalDoRefresh"></param>
        /// <returns>bool</returns>
        public bool AddEventInformationTableData(bool bLocalDoRefresh)
        {
            LogTrace("OSTransportStreamFilter.AddEventInformationTableData()");

            var opiiCurrentOSPacketIdentifierInfo = otsfLocalOSTransportStreamFilter.opiiOSPacketIdentifierInfo;
            var byaCurrentBuffer = opiiCurrentOSPacketIdentifierInfo?.byaBuffer;

            if (opiiCurrentOSPacketIdentifierInfo == null || byaCurrentBuffer == null)
                return true;

            var byCurrentTableID = byaCurrentBuffer[0];
            var iCurrentServiceID = byaCurrentBuffer.ToUShortAsInteger(3);
            var iCurrentTransportStreamID = byaCurrentBuffer.ToUShortAsInteger(8);
            var iCurrentOriginalNetworkID = byaCurrentBuffer.ToUShortAsInteger(10);
            var iSectionEnd = opiiCurrentOSPacketIdentifierInfo.iBufferLength - ((byaCurrentBuffer[1] & 0x80) != 0 ? 4 : 0);
            var osCurrentOSService = opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo?.ostOSScanTransponder?.otiOSTransponderInfo?.GetService(iCurrentServiceID);

            if (bLocalDoRefresh && osCurrentOSService != null)
            {
                for (var llnCurrentNode = osCurrentOSService.oloeOSListOSEvent.First; llnCurrentNode != null;)
                {
                    var llnNextNode = llnCurrentNode.Next;

                    if (llnCurrentNode.Value.bTableID == byCurrentTableID)
                    {
                        osCurrentOSService.dtllnOSEventsByID.Remove((llnCurrentNode.Value.bTableID, llnCurrentNode.Value.iEventID));
                        osCurrentOSService.oloeOSListOSEvent.Remove(llnCurrentNode);
                        iEITEventsDeleted += 1;
                    }

                    llnCurrentNode = llnNextNode;
                }
            }

            iEITSize += iSectionEnd;
            iEITSections += 1;

            for (var iCurrentEventPosition = 14; iCurrentEventPosition + 12 <= iSectionEnd; iCurrentEventPosition += 12 + byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentEventPosition + 10))
            {
                var oeCurrentOSEvent = new OSEvent
                {
                    bTableID = byCurrentTableID,
                    iServiceID = iCurrentServiceID,
                    iTransportStreamID = iCurrentTransportStreamID,
                    iOriginalNetworkID = iCurrentOriginalNetworkID,
                    iEventID = byaCurrentBuffer.ToUShortAsInteger(iCurrentEventPosition),
                    iModifiedJulianDate = byaCurrentBuffer.ToUShortAsInteger(iCurrentEventPosition + 2),

                    bStartHour = (byte)byaCurrentBuffer.ToBCDAsInteger(iCurrentEventPosition + 4, 2),
                    bStartMinute = (byte)byaCurrentBuffer.ToBCDAsInteger(iCurrentEventPosition + 5, 2),
                    bStartSecond = (byte)byaCurrentBuffer.ToBCDAsInteger(iCurrentEventPosition + 6, 2),
                    bDurationHour = (byte)byaCurrentBuffer.ToBCDAsInteger(iCurrentEventPosition + 7, 2),
                    bDurationMinute = (byte)byaCurrentBuffer.ToBCDAsInteger(iCurrentEventPosition + 8, 2),
                    bDurationSecond = (byte)byaCurrentBuffer.ToBCDAsInteger(iCurrentEventPosition + 9, 2)
                };

                var iDescriptorsEnd = Math.Min(iCurrentEventPosition + 12 + byaCurrentBuffer.ToDVBLengthAsInteger(iCurrentEventPosition + 10), iSectionEnd);

                for (var iCurrentDescriptorPosition = iCurrentEventPosition + 12; iCurrentDescriptorPosition + 2 <= iDescriptorsEnd; iCurrentDescriptorPosition += 2 + byaCurrentBuffer[iCurrentDescriptorPosition + 1])
                {
                    if (byaCurrentBuffer[iCurrentDescriptorPosition] != DESCRIPTORTAG_SHORT_EVENT_DESCRIPTOR || byaCurrentBuffer[iCurrentDescriptorPosition + 1] < 5)
                        continue;

                    var iDescriptorEnd = Math.Min(iCurrentDescriptorPosition + 2 + byaCurrentBuffer[iCurrentDescriptorPosition + 1], iDescriptorsEnd);
                    var iNamePosition = iCurrentDescriptorPosition + 6;
                    var iNameLength = byaCurrentBuffer[iCurrentDescriptorPosition + 5];
                    var iTextPosition = iNamePosition + iNameLength + 1;

                    if (iTextPosition > iDescriptorEnd || iTextPosition + byaCurrentBuffer[iTextPosition - 1] > iDescriptorEnd)
                    {
                        LogTrace($"Short event descriptor of event {oeCurrentOSEvent.iEventID} (SID {iCurrentServiceID}) with invalid lengths skipped");
                        continue;
                    }

                    var iTextLength = byaCurrentBuffer[iTextPosition - 1];
                    oeCurrentOSEvent.strLanguage = GetEventLanguageCode(Encoding.ASCII.GetString(byaCurrentBuffer, iCurrentDescriptorPosition + 2, 3));

                    if (iNameLength > 0)
                        oeCurrentOSEvent.strName = DecodeDVBString(byaCurrentBuffer.AsSpan(iNamePosition, iNameLength));

                    if (iTextLength > 0)
                        oeCurrentOSEvent.strText = DecodeDVBString(byaCurrentBuffer.AsSpan(iTextPosition, iTextLength));

                    iEITShortSize += iNameLength + iTextLength;
                }

                if (osCurrentOSService == null)
                    continue;

                if (osCurrentOSService.dtllnOSEventsByID.TryGetValue((byCurrentTableID, oeCurrentOSEvent.iEventID), out var llnExistingEvent))
                    llnExistingEvent.Value = oeCurrentOSEvent;
                else
                    osCurrentOSService.dtllnOSEventsByID[(byCurrentTableID, oeCurrentOSEvent.iEventID)] = osCurrentOSService.oloeOSListOSEvent.AddLast(oeCurrentOSEvent);
            }

            return true;
        }
    }

    /// <summary>
    /// OSTransportStreamFilter.IsVideoStreamType()
    /// </summary>
    /// <param name="byLocalStreamType"></param>
    /// <returns>bool</returns>
    private static bool IsVideoStreamType(byte byLocalStreamType)
    {
        LogTrace("OSTransportStreamFilter.IsVideoStreamType()");

        return (MPEG2TSStreamType)byLocalStreamType is MPEG2TSStreamType.ISO_IEC_11172_2_MPEG1_Video
            or MPEG2TSStreamType.ITU_T_Rec_H_262_ISO_IEC_13818_2_MPEG2_Video
            or MPEG2TSStreamType.ISO_IEC_14496_2_MPEG4_Video
            or MPEG2TSStreamType.AVC_Stream_H264_Video
            or MPEG2TSStreamType.H265_HEVC_Video
            or MPEG2TSStreamType.Chinese_Video_Standard_CAVS_Video
            or MPEG2TSStreamType.BBC_Dirac_Ultra_HD_Video
            or MPEG2TSStreamType.Private_ES_VC1_Video;
    }

    /// <summary>
    /// OSTransportStreamFilter.IsAudioStreamType()
    /// </summary>
    /// <param name="byLocalStreamType"></param>
    /// <returns>bool</returns>
    private static bool IsAudioStreamType(byte byLocalStreamType)
    {
        LogTrace("OSTransportStreamFilter.IsAudioStreamType()");

        return (MPEG2TSStreamType)byLocalStreamType is MPEG2TSStreamType.ISO_IEC_11172_3_MPEG1_Audio
            or MPEG2TSStreamType.ISO_IEC_13818_3_MPEG2_Audio
            or MPEG2TSStreamType.ISO_IEC_13818_7_AAC_Audio
            or MPEG2TSStreamType.ISO_IEC_14496_3_LATM_AAC_Audio
            or MPEG2TSStreamType.A52_AC3_Audio
            or MPEG2TSStreamType.HDMV_DTS_Audio
            or MPEG2TSStreamType.LPCM_TrueHD_Audio
            or MPEG2TSStreamType.EAC3_Audio;
    }
}