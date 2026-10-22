using System.Data;
using System.Text.Encodings.Web;
using System.Text.Json;

using NanoXLSX;
using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public static class OSScanIP_Extension
{
    private static readonly (string strName, Type tType, Func<OSTransponderInfo, OSService, object> fValue)[] taServiceColumns =
    [
        ("iFrequency", typeof(int), (otiCurrentOSTransponderInfo, _) => otiCurrentOSTransponderInfo.iFrequency),
        ("strProviderName", typeof(string), (_, osCurrentOSService) => osCurrentOSService.strProviderName),
        ("strName", typeof(string), (_, osCurrentOSService) => osCurrentOSService.strName),
        ("iServiceID", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iServiceID),
        ("iConditionalAccessMode", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iConditionalAccessMode),
        ("iRunningStatus", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iRunningStatus),
        ("iTransportStreamID", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iTransportStreamID),
        ("iOriginalNetworkID", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iOriginalNetworkID),
        ("iProgramMapTable", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iProgramMapTable),
        ("iProgramClockReferencePacketIdentifier", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iProgramClockReferencePacketIdentifier),
        ("iVideoPacketIdentifier", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iVideoPacketIdentifier),
        ("byVideoPacketIdentifierStreamType", typeof(string), (_, osCurrentOSService) => osCurrentOSService.byVideoPacketIdentifierStreamType.ToString("X2")),
        ("liAudioPacketIdentifiers", typeof(string), (_, osCurrentOSService) => osCurrentOSService.liAudioPacketIdentifiers.Count > 0 ? string.Join(",", osCurrentOSService.liAudioPacketIdentifiers) : "0"),
        ("lbAudioPacketIdentifiersStreamType", typeof(string), (_, osCurrentOSService) => string.Join(",", osCurrentOSService.lbAudioPacketIdentifiersStreamType.Select(byCurrentStreamType => byCurrentStreamType.ToString("X2")))),
        ("lsAudioPacketIdentifiersLanguage", typeof(string), (_, osCurrentOSService) => string.Join(",", osCurrentOSService.lsAudioPacketIdentifiersLanguage)),
        ("iSubtitlePacketIdentifier", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iSubtitlePacketIdentifier),
        ("iTeletextPacketIdentifier", typeof(int), (_, osCurrentOSService) => osCurrentOSService.iTeletextPacketIdentifier),
        ("liOtherPacketIdentifiers", typeof(string), (_, osCurrentOSService) => string.Join(",", osCurrentOSService.liOtherPacketIdentifiers)),
        ("lbOtherPacketIdentifiersStreamType", typeof(string), (_, osCurrentOSService) => string.Join(",", osCurrentOSService.lbOtherPacketIdentifiersStreamType.Select(byCurrentStreamType => byCurrentStreamType.ToString("X2"))))
    ];
    private static readonly (string strName, Type tType, Func<OSService, OSEvent, object> fValue)[] taEventColumns =
    [
        ("iOriginalNetworkID", typeof(int), (_, oeCurrentOSEvent) => oeCurrentOSEvent.iOriginalNetworkID),
        ("iTransportStreamID", typeof(int), (_, oeCurrentOSEvent) => oeCurrentOSEvent.iTransportStreamID),
        ("iServiceID", typeof(int), (_, oeCurrentOSEvent) => oeCurrentOSEvent.iServiceID),
        ("strServiceName", typeof(string), (osCurrentOSService, _) => osCurrentOSService.strName),
        ("iEventID", typeof(int), (_, oeCurrentOSEvent) => oeCurrentOSEvent.iEventID),
        ("strStartTime", typeof(string), (_, oeCurrentOSEvent) => oeCurrentOSEvent.GetStartTime()),
        ("strDuration", typeof(string), (_, oeCurrentOSEvent) => oeCurrentOSEvent.GetDuration()),
        ("strLanguage", typeof(string), (_, oeCurrentOSEvent) => oeCurrentOSEvent.strLanguage ?? string.Empty),
        ("strName", typeof(string), (_, oeCurrentOSEvent) => oeCurrentOSEvent.strName ?? string.Empty),
        ("strText", typeof(string), (_, oeCurrentOSEvent) => oeCurrentOSEvent.strText ?? string.Empty)
    ];

    extension(OSScanIP osiLocalOSScanIP)
    {
        /// <summary>
        /// OSScanIP.AddTransponderInfo()
        /// </summary>
        /// <param name="otiLocalOSTransponderInfo"></param>
        /// <returns>bool</returns>
        public bool AddTransponderInfo(OSTransponderInfo otiLocalOSTransponderInfo)
        {
            LogTrace("OSScanIP.AddTransponderInfo()");

            if (osiLocalOSScanIP.olotiOSListOSTransponderInfo == null || osiLocalOSScanIP.olotiOSListOSTransponderInfoDone == null)
                return false;

            if (osiLocalOSScanIP.olotiOSListOSTransponderInfo.Concat(osiLocalOSScanIP.olotiOSListOSTransponderInfoDone).Any(otiCurrentOSTransponderInfo => CompareTransponderInfo(otiCurrentOSTransponderInfo, otiLocalOSTransponderInfo)))
                return false;

            otiLocalOSTransponderInfo.olosOSListOSService.Initialize();
            osiLocalOSScanIP.olotiOSListOSTransponderInfo.AddLast(otiLocalOSTransponderInfo);

            return true;
        }

        /// <summary>
        /// OSScanIP.Scan()
        /// </summary>
        /// <param name="iLocalScanRetries"></param>
        /// <param name="iLocalTunerLockGraceTime"></param>
        /// <param name="iLocalServerPort"></param>
        /// <returns>bool</returns>
        public bool Scan(int? iLocalScanRetries, int? iLocalTunerLockGraceTime, int iLocalServerPort = 554)
        {
            LogTrace("OSScanIP.Scan()");

            var ostCurrentOSScanTransponder = osiLocalOSScanIP.ostOSScanTransponder;
            var otsiCurrentOSTransportStreamInfo = ostCurrentOSScanTransponder?.otsiOSTransportStreamInfo;
            var osicCurrentOSSatIPConnection = ostCurrentOSScanTransponder?.osicOSSatIPConnection;

            if (ostCurrentOSScanTransponder == null || otsiCurrentOSTransportStreamInfo == null || osicCurrentOSSatIPConnection == null || osiLocalOSScanIP.olotiOSListOSTransponderInfo == null)
                return false;

            ostCurrentOSScanTransponder.osiOSScanIP = osiLocalOSScanIP;
            otsiCurrentOSTransportStreamInfo.ostOSScanTransponder = ostCurrentOSScanTransponder;
            osicCurrentOSSatIPConnection.iPort = iLocalServerPort;
            osicCurrentOSSatIPConnection.strHost = osiLocalOSScanIP.strHost;

            try
            {
                while (!bDone && osiLocalOSScanIP.olotiOSListOSTransponderInfo.First?.Value is { } otiCurrentOSTransponderInfo)
                {
                    osicCurrentOSSatIPConnection.strTune = otiCurrentOSTransponderInfo.ToString();
                    ostCurrentOSScanTransponder.otiOSTransponderInfo = otiCurrentOSTransponderInfo;

                    lCurrentLogger.Info($"TUNE {osicCurrentOSSatIPConnection.strTune}".Pastel(ConsoleColor.Magenta));

                    var lStartTime = CurrentTimestamp();

                    Retry(iLocalScanRetries ?? 3, delegate
                    {
                        otsiCurrentOSTransportStreamInfo.Init();

                        if (!ostCurrentOSScanTransponder.Scan(iLocalTunerLockGraceTime ?? TUNER_LOCK_GRACE_TIME))
                            ostCurrentOSScanTransponder.bFailed = true;

                        return ostCurrentOSScanTransponder is { bFailed: false, bTimeout: false } or { bFailed: false, bNoSignal: true } or { bPermanentError: true };
                    }, out var iCurrentRetries);

                    osiLocalOSScanIP.lRetries = iCurrentRetries;
                    otiCurrentOSTransponderInfo.ltiiMissingProgramMapTables = ostCurrentOSScanTransponder.bFailed || ostCurrentOSScanTransponder.bNoSignal ? [] : otsiCurrentOSTransportStreamInfo.lotsfTimedOutProgramMapTables.Where(otsfCurrentOSTransportStreamFilter => otsfCurrentOSTransportStreamFilter.opiiOSPacketIdentifierInfo != null).Select(otsfCurrentOSTransportStreamFilter => (otsfCurrentOSTransportStreamFilter.opiiOSPacketIdentifierInfo!.iPacketID, otsfCurrentOSTransportStreamFilter.iTableExtension)).Distinct().ToList();
                    var lEndTime = CurrentTimestamp();
                    otsiCurrentOSTransportStreamInfo.Dispose();
                    osiLocalOSScanIP.olotiOSListOSTransponderInfo.Remove(otiCurrentOSTransponderInfo);
                    osiLocalOSScanIP.olotiOSListOSTransponderInfoDone?.AddLast(otiCurrentOSTransponderInfo);

                    var strCurrentResult = ostCurrentOSScanTransponder switch
                    {
                        { bFailed: true } => ostCurrentOSScanTransponder.strFailReason != null ? $"FAILED ({ostCurrentOSScanTransponder.strFailReason})" : "FAILED",
                        { bNoSignal: true } => $"NO SIGNAL (LEVEL {ostCurrentOSScanTransponder.iTunerLevel} QUALITY {ostCurrentOSScanTransponder.iTunerQuality})",
                        { bTimeout: true } => "TIMEOUT",
                        _ => $"FINISHED ({otiCurrentOSTransponderInfo.olosOSListOSService.Count} SERVICES FOUND)"
                    };
                    var ccCurrentResultColor = ostCurrentOSScanTransponder.bFailed ? ConsoleColor.Red : ostCurrentOSScanTransponder.bNoSignal || ostCurrentOSScanTransponder.bTimeout ? ConsoleColor.Yellow : ConsoleColor.Green;

                    lCurrentLogger.Info($"OPERATION(S) {strCurrentResult} (AND TOOK {lEndTime - lStartTime} SECOND(S) - RETRIES: {osiLocalOSScanIP.lRetries})".Pastel(ccCurrentResultColor));
                }

                osiLocalOSScanIP.ScanMissingProgramMapTables(iLocalScanRetries, iLocalTunerLockGraceTime);
            }
            finally
            {
                ostCurrentOSScanTransponder.CloseSession();
            }

            return true;
        }

        /// <summary>
        /// OSScanIP.ScanMissingProgramMapTables()
        /// </summary>
        /// <param name="iLocalScanRetries"></param>
        /// <param name="iLocalTunerLockGraceTime"></param>
        public void ScanMissingProgramMapTables(int? iLocalScanRetries, int? iLocalTunerLockGraceTime)
        {
            LogTrace("OSScanIP.ScanMissingProgramMapTables()");

            var ostCurrentOSScanTransponder = osiLocalOSScanIP.ostOSScanTransponder;
            var otsiCurrentOSTransportStreamInfo = ostCurrentOSScanTransponder?.otsiOSTransportStreamInfo;
            var osicCurrentOSSatIPConnection = ostCurrentOSScanTransponder?.osicOSSatIPConnection;

            if (ostCurrentOSScanTransponder == null || otsiCurrentOSTransportStreamInfo == null || osicCurrentOSSatIPConnection == null)
                return;

            foreach (var otiCurrentOSTransponderInfo in osiLocalOSScanIP.olotiOSListOSTransponderInfoDone?.OfType<OSTransponderInfo>().Where(otiCurrentOSTransponderInfo => otiCurrentOSTransponderInfo.ltiiMissingProgramMapTables.Count > 0).ToList() ?? [])
            {
                if (bDone)
                    break;

                var ltiiCurrentMissing = otiCurrentOSTransponderInfo.ltiiMissingProgramMapTables;
                osicCurrentOSSatIPConnection.strTune = otiCurrentOSTransponderInfo.ToString();
                ostCurrentOSScanTransponder.otiOSTransponderInfo = otiCurrentOSTransponderInfo;
                lCurrentLogger.Info($"TUNE {osicCurrentOSSatIPConnection.strTune} (WAITING FOR {ltiiCurrentMissing.Count} PMT(S))".Pastel(ConsoleColor.Magenta));
                var lStartTime = CurrentTimestamp();

                Retry(iLocalScanRetries ?? 3, delegate
                {
                    otsiCurrentOSTransportStreamInfo.Init();

                    if (!ostCurrentOSScanTransponder.Scan(iLocalTunerLockGraceTime ?? TUNER_LOCK_GRACE_TIME, ltiiCurrentMissing))
                        ostCurrentOSScanTransponder.bFailed = true;

                    return !ostCurrentOSScanTransponder.bFailed || ostCurrentOSScanTransponder.bPermanentError;
                }, out var iCurrentRetries);

                var hsiCurrentStillMissing = otsiCurrentOSTransportStreamInfo.lotsfTimedOutProgramMapTables.Select(otsfCurrentOSTransportStreamFilter => otsfCurrentOSTransportStreamFilter.iTableExtension).ToHashSet();
                var ltiiCurrentStillMissing = ostCurrentOSScanTransponder.bFailed || ostCurrentOSScanTransponder.bNoSignal ? ltiiCurrentMissing : ltiiCurrentMissing.Where(tCurrentEntry => hsiCurrentStillMissing.Contains(tCurrentEntry.iServiceID)).ToList();
                var liCurrentReceived = ltiiCurrentMissing.Except(ltiiCurrentStillMissing).Select(tCurrentEntry => tCurrentEntry.iServiceID).ToList();

                if (ltiiCurrentStillMissing.Count > 0)
                    LogTrace($"Warning: {ltiiCurrentStillMissing.Count} PMT(s) without data within {FILTER_TIMEOUT_PMT_SECOND_PASS} s - {string.Join(", ", ltiiCurrentStillMissing.Select(tCurrentEntry => $"SID {tCurrentEntry.iServiceID} (PID 0x{tCurrentEntry.iPacketID:X4})"))}");

                if (liCurrentReceived.Count > 0 && oCurrentOptions?.cloPrintServices == true)
                    otiCurrentOSTransponderInfo.PrintServices(liCurrentReceived);

                otiCurrentOSTransponderInfo.ltiiMissingProgramMapTables = ltiiCurrentStillMissing;
                otsiCurrentOSTransportStreamInfo.Dispose();
                var strCurrentResult = ostCurrentOSScanTransponder.bFailed ? $"FAILED{(ostCurrentOSScanTransponder.strFailReason != null ? $" ({ostCurrentOSScanTransponder.strFailReason})" : string.Empty)}" : $"{liCurrentReceived.Count} OF {ltiiCurrentMissing.Count} PMT(S) RECEIVED";
                var ccCurrentResultColor = ostCurrentOSScanTransponder.bFailed ? ConsoleColor.Red : ltiiCurrentStillMissing.Count > 0 ? ConsoleColor.Yellow : ConsoleColor.Green;

                lCurrentLogger.Info($"OPERATION(S) {strCurrentResult} (AND TOOK {CurrentTimestamp() - lStartTime} SECOND(S) - RETRIES: {iCurrentRetries})".Pastel(ccCurrentResultColor));
            }
        }

        /// <summary>
        /// OSScanIP.ExportServicesToDataTable()
        /// </summary>
        /// <param name="strLocalDataTableName"></param>
        /// <returns>DataTable</returns>
        public DataTable ExportServicesToDataTable(string strLocalDataTableName = "ExportServices")
        {
            LogTrace("OSScanIP.ExportServicesToDataTable()");

            var dtCurrentDataTable = new DataTable { TableName = strLocalDataTableName };

            foreach (var tCurrentColumn in taServiceColumns)
                dtCurrentDataTable.Columns.Add(tCurrentColumn.strName, tCurrentColumn.tType);

            foreach (var otiCurrentOSTransponderInfo in osiLocalOSScanIP.olotiOSListOSTransponderInfoDone?.OfType<OSTransponderInfo>() ?? [])
            {
                foreach (var osCurrentOSService in otiCurrentOSTransponderInfo.olosOSListOSService)
                    dtCurrentDataTable.Rows.Add(taServiceColumns.Select(tCurrentColumn => tCurrentColumn.fValue(otiCurrentOSTransponderInfo, osCurrentOSService)).ToArray());
            }

            return dtCurrentDataTable;
        }

        /// <summary>
        /// OSScanIP.ExportEventsToDataTable()
        /// </summary>
        /// <param name="strLocalDataTableName"></param>
        /// <returns>DataTable</returns>
        public DataTable ExportEventsToDataTable(string strLocalDataTableName = "ExportEvents")
        {
            LogTrace("OSScanIP.ExportEventsToDataTable()");

            var dtCurrentDataTable = new DataTable { TableName = strLocalDataTableName };

            foreach (var tCurrentColumn in taEventColumns)
                dtCurrentDataTable.Columns.Add(tCurrentColumn.strName, tCurrentColumn.tType);

            foreach (var osCurrentOSService in osiLocalOSScanIP.olotiOSListOSTransponderInfoDone?.OfType<OSTransponderInfo>().SelectMany(otiCurrentOSTransponderInfo => otiCurrentOSTransponderInfo.olosOSListOSService) ?? [])
                foreach (var oeCurrentOSEvent in osCurrentOSService.oloeOSListOSEvent)
                    dtCurrentDataTable.Rows.Add([.. taEventColumns.Select(tCurrentColumn => tCurrentColumn.fValue(osCurrentOSService, oeCurrentOSEvent))]);

            return dtCurrentDataTable;
        }

        /// <summary>
        /// OSScanIP.ExportServicesToJson()
        /// </summary>
        public void ExportServicesToJson()
        {
            LogTrace("OSScanIP.ExportServicesToJson()");

            WriteDataTableToJson(osiLocalOSScanIP.ExportServicesToDataTable());
        }

        /// <summary>
        /// OSScanIP.ExportEventsToJson()
        /// </summary>
        public void ExportEventsToJson()
        {
            LogTrace("OSScanIP.ExportEventsToJson()");

            WriteDataTableToJson(osiLocalOSScanIP.ExportEventsToDataTable());
        }

        /// <summary>
        /// OSScanIP.ExportServicesToExcel()
        /// </summary>
        public void ExportServicesToExcel()
        {
            LogTrace("OSScanIP.ExportServicesToExcel()");

            WriteDataTableToExcel(osiLocalOSScanIP.ExportServicesToDataTable());
        }

        /// <summary>
        /// OSScanIP.ExportEventsToExcel()
        /// </summary>
        public void ExportEventsToExcel()
        {
            LogTrace("OSScanIP.ExportEventsToExcel()");

            WriteDataTableToExcel(osiLocalOSScanIP.ExportEventsToDataTable());
        }
    }

    /// <summary>
    /// OSScanIP.GetExportFilename()
    /// </summary>
    /// <param name="strLocalName"></param>
    /// <param name="strLocalExtension"></param>
    /// <returns>string</returns>
    private static string GetExportFilename(string strLocalName, string strLocalExtension)
    {
        LogTrace("OSScanIP.GetExportFilename()");

        return $"{DateTime.Now:yyyyMMddHHmmssfff}_{strLocalName}.{strLocalExtension}";
    }

    /// <summary>
    /// OSScanIP.WriteDataTableToJson()
    /// </summary>
    /// <param name="dtLocalDataTable"></param>
    private static void WriteDataTableToJson(DataTable dtLocalDataTable)
    {
        LogTrace("OSScanIP.WriteDataTableToJson()");

        var strCurrentFilename = GetExportFilename(dtLocalDataTable.TableName, "json");
        var ldsoCurrentRows = dtLocalDataTable.Rows.Cast<DataRow>().Select(drCurrentRow => dtLocalDataTable.Columns.Cast<DataColumn>().ToDictionary(dcCurrentColumn => dcCurrentColumn.ColumnName, dcCurrentColumn => drCurrentRow[dcCurrentColumn])).ToList();
        File.WriteAllText(strCurrentFilename, JsonSerializer.Serialize(ldsoCurrentRows, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        lCurrentLogger.Info($"EXPORT {strCurrentFilename} ({ldsoCurrentRows.Count} ROWS)".Pastel(ConsoleColor.Cyan));
    }

    /// <summary>
    /// OSScanIP.WriteDataTableToExcel()
    /// </summary>
    /// <param name="dtLocalDataTable"></param>
    private static void WriteDataTableToExcel(DataTable dtLocalDataTable)
    {
        LogTrace("OSScanIP.WriteDataTableToExcel()");

        var strCurrentFilename = GetExportFilename(dtLocalDataTable.TableName, "xlsx");
        var wbCurrentWorkbook = new Workbook(strCurrentFilename, dtLocalDataTable.TableName);
        var wsCurrentWorksheet = wbCurrentWorkbook.GetWorksheet(dtLocalDataTable.TableName);
        var fCorrectionFactorForColumnFilter = 3 * EXCEL_CHARACTER_TO_WIDTH_CONSTANT;

        wbCurrentWorkbook.SetCurrentWorksheet(wsCurrentWorksheet);
        wsCurrentWorksheet.SetCurrentRowNumber(0);

        foreach (DataColumn dcCurrentColumn in dtLocalDataTable.Columns)
            wsCurrentWorksheet.AddNextCell(dcCurrentColumn.ColumnName);

        foreach (DataRow drCurrentRow in dtLocalDataTable.Rows)
        {
            wsCurrentWorksheet.GoToNextRow();

            foreach (var oCurrentValue in drCurrentRow.ItemArray)
                wsCurrentWorksheet.AddNextCell(oCurrentValue);
        }

        for (var iCurrentColumnNumber = 0; iCurrentColumnNumber < dtLocalDataTable.Columns.Count; iCurrentColumnNumber++)
        {
            var iCurrentMaximumLength = dtLocalDataTable.Rows.Cast<DataRow>().Select(drCurrentRow => Convert.ToString(drCurrentRow[iCurrentColumnNumber])?.Length ?? 0).Prepend(dtLocalDataTable.Columns[iCurrentColumnNumber].ColumnName.Length).Max();
            wsCurrentWorksheet.SetColumnWidth(iCurrentColumnNumber, Math.Min(iCurrentMaximumLength * EXCEL_CHARACTER_TO_WIDTH_CONSTANT + fCorrectionFactorForColumnFilter, EXCEL_MAXIMUM_COLUMN_WIDTH));
        }

        wsCurrentWorksheet.SetAutoFilter(0, dtLocalDataTable.Columns.Count - 1);
        wbCurrentWorkbook.Save();

        lCurrentLogger.Info($"EXPORT {strCurrentFilename} ({dtLocalDataTable.Rows.Count} ROWS)".Pastel(ConsoleColor.Cyan));
    }

    /// <summary>
    /// OSScanIP.CompareTransponderInfo()
    /// </summary>
    /// <param name="otiLocalSourceOSTransponderInfo"></param>
    /// <param name="otiLocalDestinationOSTransponderInfo"></param>
    /// <returns></returns>
    public static bool CompareTransponderInfo(OSTransponderInfo? otiLocalSourceOSTransponderInfo, OSTransponderInfo? otiLocalDestinationOSTransponderInfo)
    {
        LogTrace("OSScanIP.CompareTransponderInfo()");

        if (otiLocalSourceOSTransponderInfo == null || otiLocalDestinationOSTransponderInfo == null)
            return false;

        return otiLocalSourceOSTransponderInfo.iModulationSystem == otiLocalDestinationOSTransponderInfo.iModulationSystem
               && otiLocalSourceOSTransponderInfo.iSource == otiLocalDestinationOSTransponderInfo.iSource
               && Math.Abs(otiLocalSourceOSTransponderInfo.iFrequency - otiLocalDestinationOSTransponderInfo.iFrequency) <= 1
               && otiLocalSourceOSTransponderInfo.iPolarisation == otiLocalDestinationOSTransponderInfo.iPolarisation;
    }
}