using System.Data;

using Newtonsoft.Json;
using Pastel;

using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public static class OSTransponderInfo_Extension
    {
        extension(OSTransponderInfo otiLocalOSTransponderInfo)
        {
            public OSService GetService(int iLocalServiceId)
            {
                lCurrentLogger.Trace("OSTransponderInfo.GetService()".Pastel(ConsoleColor.Cyan));

                var osCurrentOSService = otiLocalOSTransponderInfo.olosOSListOSService.FirstOrDefault(olosOSListOSServiceNode => olosOSListOSServiceNode.iServiceID == iLocalServiceId);

                if (osCurrentOSService != null)
                    return osCurrentOSService;

                osCurrentOSService = new OSService { iServiceID = (ushort)iLocalServiceId, strName = $"Service {iLocalServiceId}", strProviderName = "~", oloeOSListOSEvent = [] };
                otiLocalOSTransponderInfo.olosOSListOSService.Add(osCurrentOSService);

                return osCurrentOSService;
            }

            public void PrintServices()
            {
                lCurrentLogger.Trace("OSTransponderInfo.PrintServices()".Pastel(ConsoleColor.Cyan));

                foreach (var osCurrentOSService in otiLocalOSTransponderInfo.olosOSListOSService)
                {
                    lCurrentLogger.Info("SERVICE".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" PROVIDERNAME:  {osCurrentOSService.strProviderName}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" SERVICENAME:   {osCurrentOSService.strName}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" ONID:          {osCurrentOSService.iOriginalNetworkID}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" TSID:          {osCurrentOSService.iTransportStreamID}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" SID:           {osCurrentOSService.iServiceID}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" RUNNINGSTATUS: {osCurrentOSService.iRunningStatus}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" GOTFROMSDT:    {osCurrentOSService.bGotFromServiceDescriptorTable.ToString()}".Pastel(ConsoleColor.Green));

                    if (osCurrentOSService.bGotFromProgramMapTable)
                    {
                        var strCurrentPIDs = Convert.ToString(osCurrentOSService.iProgramMapTable);
                        var iCurrentProgramClockReferencePacketIdentifier = osCurrentOSService.iProgramClockReferencePacketIdentifier;

                        if (osCurrentOSService.iProgramMapTable == iCurrentProgramClockReferencePacketIdentifier)
                            iCurrentProgramClockReferencePacketIdentifier = 0;

                        if (osCurrentOSService.iVideoPacketIdentifier != 0)
                        {
                            strCurrentPIDs += "," + Convert.ToString(osCurrentOSService.iVideoPacketIdentifier);

                            if (osCurrentOSService.iVideoPacketIdentifier == iCurrentProgramClockReferencePacketIdentifier)
                                iCurrentProgramClockReferencePacketIdentifier = 0;
                        }

                        strCurrentPIDs += "," + osCurrentOSService.liAudioPacketIdentifiers.StringJoin();

                        if (osCurrentOSService.iSubtitlePacketIdentifier != 0)
                        {
                            strCurrentPIDs += "," + Convert.ToString(osCurrentOSService.iSubtitlePacketIdentifier);

                            if (osCurrentOSService.iSubtitlePacketIdentifier == iCurrentProgramClockReferencePacketIdentifier)
                                iCurrentProgramClockReferencePacketIdentifier = 0;
                        }

                        if (osCurrentOSService.iTeletextPacketIdentifier != 0)
                        {
                            strCurrentPIDs += "," + Convert.ToString(osCurrentOSService.iTeletextPacketIdentifier);

                            if (osCurrentOSService.iTeletextPacketIdentifier == iCurrentProgramClockReferencePacketIdentifier)
                                iCurrentProgramClockReferencePacketIdentifier = 0;
                        }

                        if (iCurrentProgramClockReferencePacketIdentifier != 0)
                            strCurrentPIDs += "," + Convert.ToString(iCurrentProgramClockReferencePacketIdentifier);

                        lCurrentLogger.Info($" PIDS:          {strCurrentPIDs}".Pastel(ConsoleColor.Green));
                        lCurrentLogger.Info($" VPID:          {osCurrentOSService.iVideoPacketIdentifier}".Pastel(ConsoleColor.Green));
                        lCurrentLogger.Info($" APIDS:         {osCurrentOSService.liAudioPacketIdentifiers.StringJoin()}".Pastel(ConsoleColor.Green));

                        if (osCurrentOSService.iVideoPacketIdentifier == 0)
                            lCurrentLogger.Info(" RADIO:         1".Pastel(ConsoleColor.Green));

                        if (osCurrentOSService.iConditionalAccessMode != 0)
                            lCurrentLogger.Info(" ENC:           1".Pastel(ConsoleColor.Green));

                        lCurrentLogger.Info($" EIT:           {osCurrentOSService.iEventInformationTablePresentFollowing}{osCurrentOSService.iEventInformationTableSchedule}".Pastel(ConsoleColor.Green));
                    }

                    lCurrentLogger.Info("END".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info(string.Empty.Pastel(ConsoleColor.Green));
                }
            }

            public void ExportServicesToJson()
            {
                lCurrentLogger.Trace("OSTransponderInfo.ExportServicesToJson()".Pastel(ConsoleColor.Cyan));

                var strCurrentServicesExport = JsonConvert.SerializeObject(otiLocalOSTransponderInfo.olosOSListOSService, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new IgnorePropertiesResolver(["oloeOSListOSEvent", "bGotFromProgramMapTable", "bGotFromServiceDescriptorTable"]) });
                DataTable dtCurrentServicesExportDataTable = (DataTable)JsonConvert.DeserializeObject(strCurrentServicesExport, typeof(DataTable))!;
                dtExportServices.Merge(dtCurrentServicesExportDataTable);
            }

            public void PrintEvents()
            {
                lCurrentLogger.Trace("OSTransponderInfo.PrintEvents()".Pastel(ConsoleColor.Cyan));

                int iCurrentYear = 0, iCurrentMonth = 0, iCurrentDay = 0;

                foreach (var osCurrentOSService in otiLocalOSTransponderInfo.olosOSListOSService)
                {
                    foreach (var oeCurrentOSEvent in osCurrentOSService.oloeOSListOSEvent)
                    {
                        lCurrentLogger.Info("EVENT".Pastel(ConsoleColor.Green));
                        lCurrentLogger.Info($" ID:    {oeCurrentOSEvent.iOriginalNetworkID}:{oeCurrentOSEvent.iTransportStreamID}:{oeCurrentOSEvent.iServiceID}:{oeCurrentOSEvent.iEventID}".Pastel(ConsoleColor.Green));

                        GetDateFromModifiedJulianDate(oeCurrentOSEvent.iModifiedJulianDate, ref iCurrentYear, ref iCurrentMonth, ref iCurrentDay);

                        lCurrentLogger.Info($" TIME:  {iCurrentYear:D4}-{iCurrentMonth:D2}-{iCurrentDay:D2}T{oeCurrentOSEvent.bStartHour:D2}:{oeCurrentOSEvent.bStartMinute:D2}:{oeCurrentOSEvent.bStartSecond:D2}Z".Pastel(ConsoleColor.Green));
                        lCurrentLogger.Info($" DUR:   {oeCurrentOSEvent.bDurationHour:D2}:{oeCurrentOSEvent.bDurationMinute:D2}:{oeCurrentOSEvent.bDurationSecond:D2}".Pastel(ConsoleColor.Green));

                        if (string.IsNullOrEmpty(oeCurrentOSEvent.strLanguage))
                            oeCurrentOSEvent.strLanguage = "?";

                        lCurrentLogger.Info($" LANG:  {oeCurrentOSEvent.strLanguage}".Pastel(ConsoleColor.Green));

                        if (!string.IsNullOrEmpty(oeCurrentOSEvent.strName))
                            lCurrentLogger.Info($" NAME:  {oeCurrentOSEvent.strName}".Pastel(ConsoleColor.Green));

                        if (!string.IsNullOrEmpty(oeCurrentOSEvent.strText))
                            lCurrentLogger.Info($" TEXT:  {oeCurrentOSEvent.strText}".Pastel(ConsoleColor.Green));

                        lCurrentLogger.Info("END".Pastel(ConsoleColor.Green));
                        lCurrentLogger.Info(string.Empty.Pastel(ConsoleColor.Green));
                    }
                }
            }

            public void ExportEventsToJson()
            {
                lCurrentLogger.Trace("OSTransponderInfo.ExportEventsToJson()".Pastel(ConsoleColor.Cyan));

                var strCurrentEventsExport = JsonConvert.SerializeObject(otiLocalOSTransponderInfo, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new IgnorePropertiesResolver(["iType", "bUseNetworkInformationTable", "bScanEventInformationTable", "iSource", "iNetworkID", "iOriginalNetworkID", "iTransportStreamID", "iPosition", "iEAST", "iModulationSystem", "iFrequency", "iFrequencyFraction", "iPolarity", "iSymbolRate", "iRollOff", "iModulationType", "iBandwidth", "iFEC", "iInputStreamID", "iaEventInformationTableServiceID", "strServiceName", "strProviderName", "bGotFromProgramMapTable", "bGotFromServiceDescriptorTable", "iConditionalAccessMode", "iEventInformationTablePresentFollowing", "iEventInformationTableSchedule", "iServiceID", "iTransportStreamID", "iOriginalNetworkID", "iProgramMapTable", "iProgramClockReferencePacketIdentifier", "iVideoPacketIdentifier", "iaAudioPacketIdentifier", "iSubtitlePacketIdentifier", "iTeletextPacketIdentifier", "byAudioChannels"]) });
                DataTable dtCurrentEventsExportDataTable = (DataTable)JsonConvert.DeserializeObject(strCurrentEventsExport, typeof(DataTable))!;
                dtExportEvents.Merge(dtCurrentEventsExportDataTable);
            }
        }
    }
}