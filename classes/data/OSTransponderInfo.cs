using System.Data;
using System.Text;

using Newtonsoft.Json;
using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class OSTransponderInfo
    {
        public OSList<OSService> olosOSListOSService = [];

        public int iType;
        public bool bUseNetworkInformationTable;
        public bool bScanEventInformationTable;
        public int iSource;
        public int iNetworkID;
        public int iOriginalNetworkID;
        public int iTransportStreamID;
        public int iPosition;
        public int iEAST;
        public int iModulationSystem;
        public int iFrequency;
        public int iFrequencyFraction;
        public int iPolarisation;
        public int iSymbolRate;
        public int iRollOff;
        public int iModulationType;
        public int iBandwidth;
        public int iFEC;
        public int iInputStreamID;
        public int[] iaEventInformationTableServiceID = new int[MAX_EIT_SID];

        public override string ToString()
        {
            lCurrentLogger.Trace("OSTransponderInfo.ToString()".Pastel(ConsoleColor.Cyan));

            var sbCurrentStringBuilder = new StringBuilder();

            switch (iModulationSystem)
            {
                case 1:
                {
                    if (iFrequencyFraction != 0)
                        sbCurrentStringBuilder.AppendFormat("freq={0}.{1:D4}&msys=dvbc&sr={2}&mtype={3}", iFrequency, iFrequencyFraction, iSymbolRate, ModulationType2String[iModulationType]);
                    else
                        sbCurrentStringBuilder.AppendFormat("freq={0}&msys=dvbc&sr={1}&mtype={2}", iFrequency, iSymbolRate, ModulationType2String[iModulationType]);

                    break;
                }
                case 2:
                {
                    if (iFrequencyFraction != 0)
                        sbCurrentStringBuilder.AppendFormat("freq={0}.{1:D4}&msys=dvbt&bw={2}", iFrequency, iFrequencyFraction, Bandwidth2String[iBandwidth]);
                    else
                        sbCurrentStringBuilder.AppendFormat("freq={0}&msys=dvbt&bw={1}", iFrequency, Bandwidth2String[iBandwidth]);

                    break;
                }
                case 5:
                case 6:
                {
                    sbCurrentStringBuilder.AppendFormat("src={0}&freq={1}&pol={2}&msys={3}&sr={4}", iSource, iFrequency, Polarisation2String[iPolarisation & 3], ModulationSystem2String[iModulationSystem], iSymbolRate);

                    break;
                }
                case 16:
                {
                    if (iFrequencyFraction != 0)
                        sbCurrentStringBuilder.AppendFormat("freq={0}.{1:D4}&msys=dvbt2&bw={2}&plp={3}", iFrequency, iFrequencyFraction, Bandwidth2String[iBandwidth], iInputStreamID);
                    else
                        sbCurrentStringBuilder.AppendFormat("freq={0}&msys=dvbt2&bw={1}&plp={2}", iFrequency, Bandwidth2String[iBandwidth], iInputStreamID);

                    break;
                }
            }

            return sbCurrentStringBuilder.ToString();
        }
    }

    public static class OSTransponderInfo_Extension
    {
        public static OSService GetService(this OSTransponderInfo? otiLocalOSTransponderInfo, int iLocalServiceId)
        {
            lCurrentLogger.Trace("OSTransponderInfo.GetService()".Pastel(ConsoleColor.Cyan));

            if (otiLocalOSTransponderInfo != null)
            {
                var osCurrentOSService = otiLocalOSTransponderInfo.olosOSListOSService.FirstOrDefault(olosOSListOSServiceNode => olosOSListOSServiceNode.iServiceID == iLocalServiceId);

                if (osCurrentOSService != null)
                    return osCurrentOSService;

                osCurrentOSService = new OSService { iServiceID = (ushort)iLocalServiceId, strName = $"Service {iLocalServiceId}", strProviderName = "~", oloeOSListOSEvent = [] };
                otiLocalOSTransponderInfo.olosOSListOSService.Add(osCurrentOSService);

                return osCurrentOSService;
            }

            return new OSService { iServiceID = (ushort)iLocalServiceId, strName = $"Service {iLocalServiceId}", strProviderName = "~", oloeOSListOSEvent = [] };
        }

        public static void PrintServices(this OSTransponderInfo otiLocalOSTransponderInfo)
        {
            lCurrentLogger.Trace("OSTransponderInfo.PrintServices()".Pastel(ConsoleColor.Cyan));

            foreach (var osCurrentOSService in otiLocalOSTransponderInfo.olosOSListOSService)
            {
                if (osCurrentOSService.bGotFromProgramMapTable && (osCurrentOSService.iVideoPacketIdentifier != 0 || osCurrentOSService.byAudioChannels > 0))
                {
                    lCurrentLogger.Info("SERVICE".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" PROVIDERNAME:  {osCurrentOSService.strProviderName}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" SERVICENAME:   {osCurrentOSService.strName}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" ONID:          {osCurrentOSService.iOriginalNetworkID}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" TSID:          {osCurrentOSService.iTransportStreamID}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" SID:           {osCurrentOSService.iServiceID}".Pastel(ConsoleColor.Green));

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

                    for (int i = 0; i < osCurrentOSService.byAudioChannels; i += 1)
                    {
                        if (osCurrentOSService.iaAudioPacketIdentifiers[i] != 0)
                        {
                            strCurrentPIDs += "," + Convert.ToString(osCurrentOSService.iaAudioPacketIdentifiers[i]);

                            if (osCurrentOSService.iaAudioPacketIdentifiers[i] == iCurrentProgramClockReferencePacketIdentifier)
                                iCurrentProgramClockReferencePacketIdentifier = 0;
                        }
                    }

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

                    if (osCurrentOSService.byAudioChannels > 0 && osCurrentOSService.iaAudioPacketIdentifiers[0] != 0)
                    {
                        var strCurrentAPIDs = Convert.ToString(osCurrentOSService.iaAudioPacketIdentifiers[0]);

                        for (int i = 1; i < osCurrentOSService.byAudioChannels; i += 1)
                        {
                            if (osCurrentOSService.iaAudioPacketIdentifiers[i] != 0)
                                strCurrentAPIDs += "," + Convert.ToString(osCurrentOSService.iaAudioPacketIdentifiers[i]);
                        }

                        lCurrentLogger.Info($" APIDS:         {strCurrentAPIDs}".Pastel(ConsoleColor.Green));
                    }

                    if (osCurrentOSService.iVideoPacketIdentifier == 0)
                        lCurrentLogger.Info(" RADIO:         1".Pastel(ConsoleColor.Green));

                    if (osCurrentOSService.iConditionalAccessMode != 0)
                        lCurrentLogger.Info(" ENC:           1".Pastel(ConsoleColor.Green));

                    lCurrentLogger.Info($" EIT:           {osCurrentOSService.iEventInformationTablePresentFollowing}{osCurrentOSService.iEventInformationTableSchedule}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info("END".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info(string.Empty.Pastel(ConsoleColor.Green));
                }
            }
        }

        public static void ExportServicesToJson(this OSTransponderInfo otiLocalOSTransponderInfo)
        {
            lCurrentLogger.Trace("OSTransponderInfo.ExportServicesToJson()".Pastel(ConsoleColor.Cyan));

            var strCurrentServicesExport = JsonConvert.SerializeObject(otiLocalOSTransponderInfo.olosOSListOSService, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new IgnorePropertiesResolver(["oloeOSListOSEvent", "bGotFromProgramMapTable", "bGotFromServiceDescriptorTable"]) });
            DataTable dtCurrentServicesExportDataTable = (DataTable)JsonConvert.DeserializeObject(strCurrentServicesExport, typeof(DataTable))!;
            dtExportServices.Merge(dtCurrentServicesExportDataTable);
        }

        public static void PrintEvents(this OSTransponderInfo otiLocalOSTransponderInfo)
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

        public static void ExportEventsToJson(this OSTransponderInfo otiLocalOSTransponderInfo)
        {
            lCurrentLogger.Trace("OSTransponderInfo.ExportEventsToJson()".Pastel(ConsoleColor.Cyan));

            var strCurrentEventsExport = JsonConvert.SerializeObject(otiLocalOSTransponderInfo, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new IgnorePropertiesResolver(["iType", "bUseNetworkInformationTable", "bScanEventInformationTable", "iSource", "iNetworkID", "iOriginalNetworkID", "iTransportStreamID", "iPosition", "iEAST", "iModulationSystem", "iFrequency", "iFrequencyFraction", "iPolarity", "iSymbolRate", "iRollOff", "iModulationType", "iBandwidth", "iFEC", "iInputStreamID", "iaEventInformationTableServiceID", "strServiceName", "strProviderName", "bGotFromProgramMapTable", "bGotFromServiceDescriptorTable", "iConditionalAccessMode", "iEventInformationTablePresentFollowing", "iEventInformationTableSchedule", "iServiceID", "iTransportStreamID", "iOriginalNetworkID", "iProgramMapTable", "iProgramClockReferencePacketIdentifier", "iVideoPacketIdentifier", "iaAudioPacketIdentifier", "iSubtitlePacketIdentifier", "iTeletextPacketIdentifier", "byAudioChannels"]) });
            DataTable dtCurrentEventsExportDataTable = (DataTable)JsonConvert.DeserializeObject(strCurrentEventsExport, typeof(DataTable))!;
            dtExportEvents.Merge(dtCurrentEventsExportDataTable);
        }
    }
}