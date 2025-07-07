using System.Data;
using NanoXLSX;
using Pastel;
using Polly;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class OSScanIP : IDisposable
    {
        public OSList<OSTransponderInfo?>? olotiOSListOSTransponderInfo;
        public OSList<OSTransponderInfo?>? olotiOSListOSTransponderInfoDone;
        public OSScanTransponder? ostOSScanTransponder;

        public string? strHost;
        public bool bDone;
        public long lRetries;

        public void Init(string? strLocalHost)
        {
            lCurrentLogger.Trace("OSScanIP.Init()".Pastel(ConsoleColor.Cyan));

            olotiOSListOSTransponderInfo = [];
            olotiOSListOSTransponderInfoDone = [];
            olotiOSListOSTransponderInfo?.Initialize();
            olotiOSListOSTransponderInfoDone?.Initialize();

            ostOSScanTransponder = new OSScanTransponder();
            bDone = false;
            strHost = strLocalHost;
            lRetries = 0;
        }

        public void Dispose()
        {
            lCurrentLogger.Trace("OSScanIP.Dispose()".Pastel(ConsoleColor.Cyan));

            olotiOSListOSTransponderInfo?.Clear();
            olotiOSListOSTransponderInfoDone?.Clear();

            ostOSScanTransponder = null;
            bDone = false;
            strHost = null;
            lRetries = 0;
        }
    }

    public static class OSScanIP_Extension
    {
        public static bool AddTransponderInfo(this OSScanIP? osiLocalOSScanIP, OSTransponderInfo otiLocalOSTransponderInfo)
        {
            lCurrentLogger.Trace("OSScanIP.AddTransponderInfo()".Pastel(ConsoleColor.Cyan));

            if (osiLocalOSScanIP != null)
            {
                if (osiLocalOSScanIP.olotiOSListOSTransponderInfo == null)
                    return false;

                if (osiLocalOSScanIP.olotiOSListOSTransponderInfoDone == null)
                    return false;

                if (osiLocalOSScanIP.olotiOSListOSTransponderInfo.Any(otiCurrentOSTransponderInfo => CompareTransponderInfo(otiCurrentOSTransponderInfo, otiLocalOSTransponderInfo)))
                    return false;

                if (osiLocalOSScanIP.olotiOSListOSTransponderInfoDone.Any(otiCurrentOSTransponderInfo => CompareTransponderInfo(otiCurrentOSTransponderInfo, otiLocalOSTransponderInfo)))
                    return false;

                otiLocalOSTransponderInfo.olosOSListOSService.Initialize();
                osiLocalOSScanIP.olotiOSListOSTransponderInfo.AddLast(otiLocalOSTransponderInfo);

                return true;
            }

            return false;
        }

        public static bool CompareTransponderInfo(OSTransponderInfo? otiLocalSourceOSTransponderInfo, OSTransponderInfo? otiLocalDestinationOSTransponderInfo)
        {
            lCurrentLogger.Trace("OSScanIP.CompareTransponderInfo()".Pastel(ConsoleColor.Cyan));

            if (otiLocalSourceOSTransponderInfo == null || otiLocalDestinationOSTransponderInfo == null)
                return false;

            if (otiLocalSourceOSTransponderInfo.iModulationSystem != otiLocalDestinationOSTransponderInfo.iModulationSystem)
                return false;

            if (otiLocalSourceOSTransponderInfo.iSource != otiLocalDestinationOSTransponderInfo.iSource)
                return false;

            if (otiLocalSourceOSTransponderInfo.iFrequency != otiLocalDestinationOSTransponderInfo.iFrequency && otiLocalSourceOSTransponderInfo.iFrequency != otiLocalDestinationOSTransponderInfo.iFrequency + 1 && otiLocalSourceOSTransponderInfo.iFrequency != otiLocalDestinationOSTransponderInfo.iFrequency - 1)
                return false;

            return otiLocalSourceOSTransponderInfo.iPolarisation == otiLocalDestinationOSTransponderInfo.iPolarisation;
        }

        public static bool Scan(this OSScanIP? osiLocalOSScanIP, long lLocalTimeout = 600, int iLocalScanRetries = 3, int iLocalTransponderRetries = 10)
        {
            lCurrentLogger.Trace("OSScanIP.Scan()".Pastel(ConsoleColor.Cyan));

            while (!bDone && osiLocalOSScanIP is { olotiOSListOSTransponderInfo: not null } && !osiLocalOSScanIP.olotiOSListOSTransponderInfo.IsEmpty())
            {
                var ostCurrentOSScanTransponder = osiLocalOSScanIP.ostOSScanTransponder;
                var otsiCurrentOSTransportStreamInfo = ostCurrentOSScanTransponder?.otsiOSTransportStreamInfo;

                otsiCurrentOSTransportStreamInfo?.Init();

                if (ostCurrentOSScanTransponder == null)
                    return false;

                ostCurrentOSScanTransponder.osiOSScanIP = osiLocalOSScanIP;

                if (ostCurrentOSScanTransponder.osicOSSatIPConnection == null)
                    return false;

                ostCurrentOSScanTransponder.osicOSSatIPConnection.iPort = 554;
                ostCurrentOSScanTransponder.osicOSSatIPConnection.strHost = osiLocalOSScanIP.strHost;

                if (otsiCurrentOSTransportStreamInfo == null)
                    return false;

                otsiCurrentOSTransportStreamInfo.ostOSScanTransponder = ostCurrentOSScanTransponder;
                var otiCurrentOSTransponderInfo = osiLocalOSScanIP.olotiOSListOSTransponderInfo.First();
                ostCurrentOSScanTransponder.osicOSSatIPConnection.strTune = otiCurrentOSTransponderInfo?.ToString();

                lCurrentLogger.Info($"TUNE {ostCurrentOSScanTransponder.osicOSSatIPConnection.strTune}".Pastel(ConsoleColor.Magenta));
                
                ostCurrentOSScanTransponder.otiOSTransponderInfo = otiCurrentOSTransponderInfo;
                var iStartTime = CurrentTimestamp();

                var cCurrentContext = new Context { { "Retries", 0 } };
                Policy.HandleResult<bool>(r => r != true).WaitAndRetry(iLocalScanRetries, _ => TimeSpan.FromSeconds(0), onRetry: (_, _, retryCount, context) => { context["Retries"] = retryCount; }).Execute(delegate { ostCurrentOSScanTransponder.Scan(lLocalTimeout: lLocalTimeout, iLocalTransponderRetries: iLocalTransponderRetries); return otiCurrentOSTransponderInfo?.olosOSListOSService.Count > 0; }, cCurrentContext);
                osiLocalOSScanIP.lRetries = Convert.ToInt32(cCurrentContext["Retries"]);

                var iEndTime = CurrentTimestamp();
                otsiCurrentOSTransportStreamInfo.Dispose();

                osiLocalOSScanIP.olotiOSListOSTransponderInfo?.Remove(otiCurrentOSTransponderInfo);
                osiLocalOSScanIP.olotiOSListOSTransponderInfoDone?.AddLast(otiCurrentOSTransponderInfo);

                lCurrentLogger.Info($"OPERATION(S) {(ostCurrentOSScanTransponder.bTimedOut?"TIMED OUT/NO RESULT":"FINISHED (" + otiCurrentOSTransponderInfo?.olosOSListOSService.Count + " SERVICES FOUND)")} (AND TOOK {iEndTime-iStartTime} SECOND(S) AND {ostCurrentOSScanTransponder.lRetries + osiLocalOSScanIP.lRetries} RETRY)".Pastel(ostCurrentOSScanTransponder.bTimedOut?ConsoleColor.Yellow:ConsoleColor.Green));
            }

            return true;
        }
        
        public static void ExportEventsToExcel(this OSScanIP osiLocalOSScanIP)
        {
            lCurrentLogger.Trace("OSScanIP.ExportEventsToExcel()".Pastel(ConsoleColor.Cyan));

            //TODO - Implementation missing
        }

        /// <summary>
        /// OSScanIP.ExportServicesToExcel()
        /// </summary>
        /// <param name="osiLocalOSScanIP"></param>
        /// <param name="strLocalFilename"></param>
        public static void ExportServicesToExcel(this OSScanIP osiLocalOSScanIP, string strLocalFilename = "ExportServices")
        {
            lCurrentLogger.Trace("OSScanIP.ExportServicesToExcel()".Pastel(ConsoleColor.Cyan));

            var strCurrentFilename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + strLocalFilename + ".xlsx";
            var wbCurrentWorkbook = new Workbook(strCurrentFilename, strLocalFilename);
            var wsCurrentWorksheet = wbCurrentWorkbook.GetWorksheet(strLocalFilename);

            wbCurrentWorkbook.SetCurrentWorksheet(wsCurrentWorksheet);
            wsCurrentWorksheet.SetCurrentRowNumber(0);

            if (osiLocalOSScanIP.olotiOSListOSTransponderInfoDone is { Count: > 0 })
            {
                foreach (var fiCurrentOSTransponderInfoHeaderFieldInfo in typeof(OSTransponderInfo).GetFields().Where(fiCurrentFieldInfo => fiCurrentFieldInfo.Name == "iFrequency"))
                {
                    wsCurrentWorksheet.AddNextCell(fiCurrentOSTransponderInfoHeaderFieldInfo.Name);
                }

                foreach (var fiCurrentOSServiceHeaderFieldInfo in typeof(OSService).GetFields().Where(fiCurrentFieldInfo => fiCurrentFieldInfo.Name != "oloeOSListOSEvent" && fiCurrentFieldInfo.Name != "byAudioChannels" && fiCurrentFieldInfo.Name != "bGotFromProgramMapTable" && fiCurrentFieldInfo.Name != "bGotFromServiceDescriptorTable" && fiCurrentFieldInfo.Name != "iEventInformationTablePresentFollowing" && fiCurrentFieldInfo.Name != "iEventInformationTableSchedule"))
                {
                    wsCurrentWorksheet.AddNextCell(fiCurrentOSServiceHeaderFieldInfo.Name);
                }

                var fCorrectionFactorForColumnFilter = 3 * EXCEL_CHARACTER_TO_WIDTH_CONSTANT;
                float fProviderNamePropertyMaximumLength = 0;
                float fNamePropertyMaximumLength = 0;

                foreach (var otiCurrentOSTransponderInfo in osiLocalOSScanIP.olotiOSListOSTransponderInfoDone)
                {
                    foreach (var osCurrentOSServiceItem in otiCurrentOSTransponderInfo?.olosOSListOSService!)
                    {
                        wsCurrentWorksheet.GoToNextRow();

                        foreach (var fiCurrentOSTransponderInfoHeaderFieldInfo in typeof(OSTransponderInfo).GetFields().Where(fiCurrentFieldInfo => fiCurrentFieldInfo.Name == "iFrequency"))
                        {
                            wsCurrentWorksheet.AddNextCell(fiCurrentOSTransponderInfoHeaderFieldInfo.GetValue(otiCurrentOSTransponderInfo));
                        }

                        foreach (var fiCurrentOSServiceItemFieldInfo in typeof(OSService).GetFields().Where(fiCurrentFieldInfo => fiCurrentFieldInfo.Name != "oloeOSListOSEvent" && fiCurrentFieldInfo.Name != "byAudioChannels" && fiCurrentFieldInfo.Name != "bGotFromProgramMapTable" && fiCurrentFieldInfo.Name != "bGotFromServiceDescriptorTable" && fiCurrentFieldInfo.Name != "iEventInformationTablePresentFollowing" && fiCurrentFieldInfo.Name != "iEventInformationTableSchedule"))
                        {
                            if (fiCurrentOSServiceItemFieldInfo.Name is not "iaAudioPacketIdentifiers")
                            {
                                wsCurrentWorksheet.AddNextCell(fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem));
                            }
                            else
                            {
                                if ((byte)(typeof(OSService).GetField("byAudioChannels")?.GetValue(osCurrentOSServiceItem) ?? 0) > 0 && ((int[])fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem)!)[0] != 0)
                                {
                                    var strCurrentAPIDs = Convert.ToString(((int[])fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem)!)[0]);

                                    for (var iCurrentAPIDCounter = 1; iCurrentAPIDCounter < (byte)(typeof(OSService).GetField("byAudioChannels")?.GetValue(osCurrentOSServiceItem) ?? 0); iCurrentAPIDCounter += 1)
                                    {
                                        if (((int[])fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem)!)[iCurrentAPIDCounter] != 0)
                                            strCurrentAPIDs += "," + Convert.ToString(((int[])fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem)!)[iCurrentAPIDCounter]);
                                    }

                                    wsCurrentWorksheet.AddNextCell(strCurrentAPIDs);
                                }
                                else
                                    wsCurrentWorksheet.AddNextCell(string.Empty);
                            }
                        }
                    }

                    var fProviderNamePropertyMaximumLengthCurrent = otiCurrentOSTransponderInfo.olosOSListOSService.Select(osCurrentOSServiceItem => osCurrentOSServiceItem.strProviderName.Length).Prepend(0).Max() * EXCEL_CHARACTER_TO_WIDTH_CONSTANT + fCorrectionFactorForColumnFilter;
                    if (fProviderNamePropertyMaximumLengthCurrent > fProviderNamePropertyMaximumLength)
                        fProviderNamePropertyMaximumLength = fProviderNamePropertyMaximumLengthCurrent;
                    var fNamePropertyMaximumLengthCurrent = otiCurrentOSTransponderInfo.olosOSListOSService.Select(osCurrentOSServiceItem => osCurrentOSServiceItem.strName.Length).Prepend(0).Max() * EXCEL_CHARACTER_TO_WIDTH_CONSTANT + fCorrectionFactorForColumnFilter;
                    if (fNamePropertyMaximumLengthCurrent > fNamePropertyMaximumLength)
                        fNamePropertyMaximumLength = fNamePropertyMaximumLengthCurrent;
                }

                for (var iCurrentColumnNumber = 0; iCurrentColumnNumber <= wsCurrentWorksheet.GetLastColumnNumber(); iCurrentColumnNumber++)
                {
                    var fCurrentMaximumLengthValuesWithHeader = Convert.ToSingle(Convert.ToString(wsCurrentWorksheet.GetCell(iCurrentColumnNumber, 0).Value)!.Length) * EXCEL_CHARACTER_TO_WIDTH_CONSTANT + fCorrectionFactorForColumnFilter;

                    switch (iCurrentColumnNumber)
                    {
                        case 1:
                        {
                            if (fCurrentMaximumLengthValuesWithHeader < fProviderNamePropertyMaximumLength)
                                fCurrentMaximumLengthValuesWithHeader = fProviderNamePropertyMaximumLength;
                            break;
                        }
                        case 2:
                        {
                            if (fCurrentMaximumLengthValuesWithHeader < fNamePropertyMaximumLength)
                                fCurrentMaximumLengthValuesWithHeader = fNamePropertyMaximumLength;
                            break;
                        }
                    }

                    wsCurrentWorksheet.SetColumnWidth(iCurrentColumnNumber, fCurrentMaximumLengthValuesWithHeader);
                }
            }

            wsCurrentWorksheet.SetAutoFilter(0, wsCurrentWorksheet.GetLastColumnNumber());
            wbCurrentWorkbook.Save();
        }

        /// <summary>
        /// OSScanIP.ExportServicesToDataTable()
        /// </summary>
        /// <param name="osiLocalOSScanIP"></param>
        /// <param name="strLocalDataTableName"></param>
        /// <returns></returns>
        public static DataTable ExportServicesToDataTable(this OSScanIP osiLocalOSScanIP, string strLocalDataTableName = "ExportServices")
        {
            lCurrentLogger.Trace("OSScanIP.ExportServicesToDataTable()".Pastel(ConsoleColor.Cyan));

            DataTable dtLocalDataTable = new() { TableName = strLocalDataTableName };

            if (osiLocalOSScanIP.olotiOSListOSTransponderInfoDone is not { Count: > 0 }) 
                return dtLocalDataTable;

            foreach (var fiCurrentOSTransponderInfoHeaderFieldInfo in typeof(OSTransponderInfo).GetFields().Where(fiCurrentFieldInfo => fiCurrentFieldInfo.Name == "iFrequency"))
            {
                dtLocalDataTable.Columns.Add(fiCurrentOSTransponderInfoHeaderFieldInfo.Name);
            }

            foreach (var fiCurrentOSServiceHeaderFieldInfo in typeof(OSService).GetFields().Where(fiCurrentFieldInfo => fiCurrentFieldInfo.Name != "oloeOSListOSEvent" && fiCurrentFieldInfo.Name != "byAudioChannels" && fiCurrentFieldInfo.Name != "bGotFromProgramMapTable" && fiCurrentFieldInfo.Name != "bGotFromServiceDescriptorTable" && fiCurrentFieldInfo.Name != "iEventInformationTablePresentFollowing" && fiCurrentFieldInfo.Name != "iEventInformationTableSchedule"))
            {
                dtLocalDataTable.Columns.Add(fiCurrentOSServiceHeaderFieldInfo.Name);
            }

            foreach (var otiCurrentOSTransponderInfo in osiLocalOSScanIP.olotiOSListOSTransponderInfoDone)
            {
                foreach (var osCurrentOSServiceItem in otiCurrentOSTransponderInfo?.olosOSListOSService!)
                {
                    var drLocalDataRow = dtLocalDataTable.NewRow();

                    foreach (var fiCurrentOSTransponderInfoHeaderFieldInfo in typeof(OSTransponderInfo).GetFields().Where(fiCurrentFieldInfo => fiCurrentFieldInfo.Name == "iFrequency"))
                    {
                        drLocalDataRow[fiCurrentOSTransponderInfoHeaderFieldInfo.Name] = fiCurrentOSTransponderInfoHeaderFieldInfo.GetValue(otiCurrentOSTransponderInfo);
                    }

                    foreach (var fiCurrentOSServiceItemFieldInfo in typeof(OSService).GetFields().Where(fiCurrentFieldInfo => fiCurrentFieldInfo.Name != "oloeOSListOSEvent" && fiCurrentFieldInfo.Name != "byAudioChannels" && fiCurrentFieldInfo.Name != "bGotFromProgramMapTable" && fiCurrentFieldInfo.Name != "bGotFromServiceDescriptorTable" && fiCurrentFieldInfo.Name != "iEventInformationTablePresentFollowing" && fiCurrentFieldInfo.Name != "iEventInformationTableSchedule"))
                    {
                        if (fiCurrentOSServiceItemFieldInfo.Name is not "iaAudioPacketIdentifiers")
                        {
                            drLocalDataRow[fiCurrentOSServiceItemFieldInfo.Name] = fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem);
                        }
                        else
                        {
                            if ((byte)(typeof(OSService).GetField("byAudioChannels")?.GetValue(osCurrentOSServiceItem) ?? 0) > 0 && ((int[])fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem)!)[0] != 0)
                            {
                                var strCurrentAPIDs = Convert.ToString(((int[])fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem)!)[0]);

                                for (var iCurrentAPIDCounter = 1; iCurrentAPIDCounter < (byte)(typeof(OSService).GetField("byAudioChannels")?.GetValue(osCurrentOSServiceItem) ?? 0); iCurrentAPIDCounter += 1)
                                {
                                    if (((int[])fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem)!)[iCurrentAPIDCounter] != 0)
                                        strCurrentAPIDs += "," + Convert.ToString(((int[])fiCurrentOSServiceItemFieldInfo.GetValue(osCurrentOSServiceItem)!)[iCurrentAPIDCounter]);
                                }

                                drLocalDataRow[fiCurrentOSServiceItemFieldInfo.Name] = strCurrentAPIDs;
                            }
                            else
                                drLocalDataRow[fiCurrentOSServiceItemFieldInfo.Name] = string.Empty;
                        }
                    }

                    dtLocalDataTable.Rows.Add(drLocalDataRow);
                }
            }

            return dtLocalDataTable;
        }
    }
}