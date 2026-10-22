using Pastel;

using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public static class OSTransponderInfo_Extension
{
    extension(OSTransponderInfo otiLocalOSTransponderInfo)
    {
        /// <summary>
        /// OSTransponderInfo.GetService()
        /// </summary>
        /// <param name="iLocalServiceId"></param>
        /// <returns>OSService</returns>
        public OSService GetService(int iLocalServiceId)
        {
            LogTrace("OSTransponderInfo.GetService()");

            var osCurrentOSService = otiLocalOSTransponderInfo.olosOSListOSService.FirstOrDefault(olosOSListOSServiceNode => olosOSListOSServiceNode.iServiceID == iLocalServiceId);

            if (osCurrentOSService != null)
                return osCurrentOSService;

            osCurrentOSService = new OSService { iServiceID = (ushort)iLocalServiceId, strName = $"Service {iLocalServiceId}", strProviderName = "~" };
            otiLocalOSTransponderInfo.olosOSListOSService.Add(osCurrentOSService);

            return osCurrentOSService;
        }

        /// <summary>
        /// OSTransponderInfo.PrintServices()
        /// </summary>
        /// <param name="lLocalServiceIDs">Only these services (null for all)</param>
        public void PrintServices(IReadOnlyCollection<int>? lLocalServiceIDs = null)
        {
            LogTrace("OSTransponderInfo.PrintServices()");

            foreach (var osCurrentOSService in otiLocalOSTransponderInfo.olosOSListOSService.Where(osCurrentOSService => lLocalServiceIDs == null || lLocalServiceIDs.Contains(osCurrentOSService.iServiceID)))
            {
                lCurrentLogger.Info("SERVICE".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" PROVIDERNAME:  {osCurrentOSService.strProviderName}".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" SERVICENAME:   {osCurrentOSService.strName}".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" ONID:          {osCurrentOSService.iOriginalNetworkID}".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" TSID:          {osCurrentOSService.iTransportStreamID}".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" SID:           {osCurrentOSService.iServiceID}".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" RUNNINGSTATUS: {osCurrentOSService.iRunningStatus}".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" GOTFROMSDT:    {osCurrentOSService.bGotFromServiceDescriptorTable}".Pastel(ConsoleColor.Green));

                if (osCurrentOSService.bGotFromProgramMapTable)
                {
                    List<int> liCurrentPIDs = [osCurrentOSService.iProgramMapTable, osCurrentOSService.iVideoPacketIdentifier, ..osCurrentOSService.liAudioPacketIdentifiers, osCurrentOSService.iSubtitlePacketIdentifier, osCurrentOSService.iTeletextPacketIdentifier, ..osCurrentOSService.liOtherPacketIdentifiers];
                    liCurrentPIDs.RemoveAll(iCurrentPID => iCurrentPID == 0);

                    if (osCurrentOSService.iProgramClockReferencePacketIdentifier != 0 && !liCurrentPIDs.Contains(osCurrentOSService.iProgramClockReferencePacketIdentifier))
                        liCurrentPIDs.Add(osCurrentOSService.iProgramClockReferencePacketIdentifier);

                    lCurrentLogger.Info($" PIDS:          {string.Join(",", liCurrentPIDs)}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" VPID:          {osCurrentOSService.iVideoPacketIdentifier}".Pastel(ConsoleColor.Green));
                    lCurrentLogger.Info($" APIDS:         {string.Join(",", osCurrentOSService.liAudioPacketIdentifiers)}".Pastel(ConsoleColor.Green));

                    if (osCurrentOSService.liOtherPacketIdentifiers.Count > 0)
                        lCurrentLogger.Info($" OPIDS:         {string.Join(",", osCurrentOSService.liOtherPacketIdentifiers.Select((iCurrentPID, iCurrentIndex) => $"{iCurrentPID} (0x{osCurrentOSService.lbOtherPacketIdentifiersStreamType[iCurrentIndex]:X2})"))}".Pastel(ConsoleColor.Green));

                    if (osCurrentOSService.iVideoPacketIdentifier == 0)
                        lCurrentLogger.Info(" RADIO:         1".Pastel(ConsoleColor.Green));

                    if (osCurrentOSService.iConditionalAccessMode != 0)
                        lCurrentLogger.Info(" ENC:           1".Pastel(ConsoleColor.Green));

                    lCurrentLogger.Info($" EIT:           {osCurrentOSService.iEventInformationTablePresentFollowing}{osCurrentOSService.iEventInformationTableSchedule}".Pastel(ConsoleColor.Green));
                }

                lCurrentLogger.Info("END".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info(string.Empty);
            }
        }

        /// <summary>
        /// OSTransponderInfo.PrintEvents()
        /// </summary>
        public void PrintEvents()
        {
            LogTrace("OSTransponderInfo.PrintEvents()");

            foreach (var oeCurrentOSEvent in otiLocalOSTransponderInfo.olosOSListOSService.SelectMany(osCurrentOSService => osCurrentOSService.oloeOSListOSEvent))
            {
                lCurrentLogger.Info("EVENT".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" ID:    {oeCurrentOSEvent.iOriginalNetworkID}:{oeCurrentOSEvent.iTransportStreamID}:{oeCurrentOSEvent.iServiceID}:{oeCurrentOSEvent.iEventID}".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" TIME:  {oeCurrentOSEvent.GetStartTime()}".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" DUR:   {oeCurrentOSEvent.GetDuration()}".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info($" LANG:  {(string.IsNullOrEmpty(oeCurrentOSEvent.strLanguage) ? "?" : oeCurrentOSEvent.strLanguage)}".Pastel(ConsoleColor.Green));

                if (!string.IsNullOrEmpty(oeCurrentOSEvent.strName))
                    lCurrentLogger.Info($" NAME:  {oeCurrentOSEvent.strName}".Pastel(ConsoleColor.Green));

                if (!string.IsNullOrEmpty(oeCurrentOSEvent.strText))
                    lCurrentLogger.Info($" TEXT:  {oeCurrentOSEvent.strText}".Pastel(ConsoleColor.Green));

                lCurrentLogger.Info("END".Pastel(ConsoleColor.Green));
                lCurrentLogger.Info(string.Empty);
            }
        }
    }
}