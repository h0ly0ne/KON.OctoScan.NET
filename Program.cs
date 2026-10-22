using System.Text;

using CommandLine;
using CommandLine.Text;
using NLog;
using NLog.Layouts;
using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class Program
    {
        public enum FlagMode
        {
            scan        = 0,
            parsedate   = 1
        }

        public enum FlagModulationSystem
        {
            dvbc   = 1,
            dvbcb  = 2,
            dvbt   = 3,
            dss    = 4,
            dvbs   = 5,
            dvbs2  = 6,
            dvbh   = 7,
            isdbt  = 8,
            isdbs  = 9,
            isdbc  = 10,
            atsc   = 11,
            atscmh = 12,
            dtmb   = 13,
            cmmb   = 14,
            dab    = 15,
            dvbt2  = 16,
            turbo  = 17,
            dvbcc  = 18,
            dvbc2  = 19
        }

        public enum FlagModulationType
        {
            tqpsk       = 1,
            t16qam      = 2,
            t32qam      = 3,
            t64qam      = 4,
            t128qam     = 5,
            t256qam     = 6,
            tautoqam    = 7,
            t8vsb       = 8,
            t16vsb      = 9,
            t8psk       = 10,
            t16apsk     = 11,
            t32apsk     = 12,
            tdqpsk      = 13,
            t4qamnr     = 14
        }

        public class Options
        {
            [Value(0, MetaName = "<server ip>", HelpText = "IP Address of SAT>IP server (required for scan)", Required = false)]
            public string? cloServerIP { get; set; }

            [Option(longName: "serverport", Required = false, HelpText = "Port of SAT>IP server", Default = 554)]
            public int cloServerPort { get; set; }

            [Option(longName: "ipv6", Required = false, HelpText = "Enable IPv6 Support", Default = false)]
            public bool cloIPv6 { get; set; }

            [Option(longName: "mode", Required = true, HelpText = "Execution mode / Operation selection: scan/parsedate")]
            public FlagMode? cloMode { get; set; }
            
            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

            [Option(longName: "mjd", Required = false, HelpText = "Parse Date from Modified Julian Date.")]
            public long? cloModifiedJulianDate { get; set; }

            [Option(longName: "frequencies", Required = false, HelpText = "Frequencies in MHz, comma separated. Ranges as start-end:step.\r\nExample: --frequencies=378,410-466:8")]
            public string? cloFrequenciesInMHz { get; set; }

            [Option(longName: "modulationsystem", Required = false, HelpText = "Modulation System (required for scan).\r\nExample: --modulationsystem=dvbc")]
            public FlagModulationSystem? cloModulationSystem { get; set; }

            [Option(longName: "modulationtype", Required = false, HelpText = "Modulation Type (required for DVB-C).\r\nExample: --modulationtype=t256qam")]
            public FlagModulationType? cloModulationType { get; set; }

            [Option(longName: "symbolrate", Required = false, HelpText = "Symbolrate in kSymbols (required for DVB-S/S2 and DVB-C).\r\nExample: --symbolrate=6900")]
            public int? cloSymbolRateInkSymbols { get; set; }

            [Option(longName: "scanretries", Required = false, HelpText = "Retries per transponder (new connection and session) on timeouts and temporary server errors (default 3).\r\nExample: --scanretries=3")]
            public int? cloScanRetries { get; set; }

            [Option(longName: "tunerlockgracetime", Required = false, HelpText = "Time in milliseconds the tuner gets to lock before a transponder is skipped as without signal (default 3000).\r\nExample: --tunerlockgracetime=5000")]
            public int? cloTunerLockGraceTime { get; set; }

            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

            [Option(longName: "satellitesource", Required = false, HelpText = "Satellite source 1,2,3,4 (required for DVB-S/S2).\r\nExample: --satellitesource=1")]
            public int? cloSatelliteSource { get; set; }

            [Option(longName: "polarisation", Required = false, HelpText = "Polarisation v,h,r,l (required for DVB-S/S2).\r\nExample: --polarisation=v")]
            public string? cloPolarisation { get; set; }

            [Option(longName: "bandwidth", Required = false, HelpText = "Bandwidth 1.712,5,6,7,8,10 (required for DVB-T/T2).\r\nExample: --bandwidth=8")]
            public string? cloBandwidth { get; set; }

            [Option(longName: "nit", Required = false, HelpText = "Use network information table. If specified additional transponders will be scanned from network information table.")]
            public bool cloNetworkInformationTable { get; set; }

            [Option(longName: "eit", Required = false, HelpText = "Use event information table. If specified event information will be collected from transponders.")]
            public bool cloEventInformationTable { get; set; }

            [Option(longName: "eitfiltersid", Required = false, HelpText = "Comma separated list of services that will be looked up from event information table.\r\nExample: --eitfiltersid=1000,1002,3003")]
            public string? cloEventInformationTableSIDFilter { get; set; }

            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

            [Option(longName: "imagestreamidentifier", Required = false, HelpText = "ImageStreamIdentifier for physical layer pipe.\r\nExample: --imagestreamidentifier=1")]
            public int? cloPhysicalLayerPipes { get; set; }

            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
            
            [Option(longName: "printservices", Required = false, HelpText = "Output result from services scan.")]
            public bool cloPrintServices { get; set; }

            [Option(longName: "exportservices", Required = false, HelpText = "Export result from services scan to json file.")]
            public bool cloExportServices { get; set; }

            [Option(longName: "exportservicesexcel", Required = false, HelpText = "Export result from services scan to excel file.")]
            public bool cloExportServicesExcel { get; set; }

            [Option(longName: "printevents", Required = false, HelpText = "Output result from events scan.")]
            public bool cloPrintEvents { get; set; }

            [Option(longName: "exportevents", Required = false, HelpText = "Export result from events scan to json file.")]
            public bool cloExportEvents { get; set; }

            [Option(longName: "exporteventsexcel", Required = false, HelpText = "Export result from events scan to excel file.")]
            public bool cloExportEventsExcel { get; set; }

            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

            [Option(longName: "verbose", Required = false, HelpText = "Set output to verbose level.")]
            public bool cloVerbose { get; set; }
        }

        private static void Main(string[] saLocalArguments)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var pCurrentParser = new Parser(apsCurrentConfiguration => apsCurrentConfiguration.HelpWriter = null);
            var prCurrentParseResult = pCurrentParser.ParseArguments<Options>(saLocalArguments);

            prCurrentParseResult.WithParsed(delegate(Options oLocalOptions)
            {
                if (oLocalOptions.cloVerbose)
                    LogManager.Setup().LoadConfiguration(aslcbCurrentSetupLoadConfigurationBuilder => { aslcbCurrentSetupLoadConfigurationBuilder.ForLogger().FilterMinLevel(LogLevel.Trace).WriteToColoredConsole(new SimpleLayout("${message}")); });
                else
                    LogManager.Setup().LoadConfiguration(aslcbCurrentSetupLoadConfigurationBuilder => { aslcbCurrentSetupLoadConfigurationBuilder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToColoredConsole(new SimpleLayout("${message}")); });

                Run(oLocalOptions);
            });

            prCurrentParseResult.WithNotParsed(delegate
            {
                LogManager.Setup().LoadConfiguration(aslcbCurrentSetupLoadConfigurationBuilder => { aslcbCurrentSetupLoadConfigurationBuilder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToColoredConsole(new SimpleLayout("${message}")); });

                DisplayHelp(prCurrentParseResult);
            });
        }

        private static void DisplayHelp<T>(ParserResult<T> result)
        {
            var htCurrentHelpText = HelpText.AutoBuild(result, htCurrentHelpText =>
            {
                htCurrentHelpText.AddNewLineBetweenHelpSections = true;
                htCurrentHelpText.AdditionalNewLineAfterOption = false;
                htCurrentHelpText.MaximumDisplayWidth = 100;
                htCurrentHelpText.Heading = "KON.OctoScan.NET";
                htCurrentHelpText.Copyright = "Copyright (C) 2016 Digital Devices GmbH" + Environment.NewLine + "Copyright (C) 2024 Oswald Oliver";

                htCurrentHelpText.AddPostOptionsLine("* Notes on NIT scanning:");
                htCurrentHelpText.AddPostOptionsLine("    With some cable providers or inhouse retransmission systems");
                htCurrentHelpText.AddPostOptionsLine("    it may be not usable, i.e. due to wrong frequencies in the NIT.");
                htCurrentHelpText.AddPostOptionsLine("");
                htCurrentHelpText.AddPostOptionsLine("* Notes on hardware depencies:");
                htCurrentHelpText.AddPostOptionsLine("    Depending on hardware configuration the scan will succeed even if");
                htCurrentHelpText.AddPostOptionsLine("    some required parameters are wrong. This will result in a channel list");
                htCurrentHelpText.AddPostOptionsLine("    which is usable only on the same hardware configuration.");
                htCurrentHelpText.AddPostOptionsLine("");
                htCurrentHelpText.AddPostOptionsLine("* Example: NIT based scan which should work on Unitymedia in Germany");
                htCurrentHelpText.AddPostOptionsLine("    KON.OctoScan.NET --mode scan --frequencies=138 --modulationsystem=dvbc");
                htCurrentHelpText.AddPostOptionsLine("                     --symbolrate=6900 --modulationtype=t256qam --nit 10.0.4.24");
                htCurrentHelpText.AddPostOptionsLine("");

                return HelpText.DefaultParsingErrorsHandler(result, htCurrentHelpText);
            }, e => e);


            lCurrentLogger.Info(htCurrentHelpText);

            WaitForKeyPress();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void WaitForKeyPress()
        {
            if (Console.IsInputRedirected)
                return;

            lCurrentLogger.Info("Press any key to continue ...".Pastel(ConsoleColor.Green));
            Console.ReadKey();
        }

        private static void Run(Options oLocalOptions)
        {
            #if DEBUG
                try
                {
                    if (OperatingSystem.IsWindows())
                        Console.BufferHeight = short.MaxValue - 1;
                }
                catch (Exception) { /* Not supported by every terminal */ }
            #endif

            oCurrentOptions = oLocalOptions;

            switch (oLocalOptions.cloMode)
            {
                case FlagMode.parsedate:
                {
                    if (oLocalOptions.cloModifiedJulianDate is not ( >= 0 and <= ushort.MaxValue))
                    {
                        lCurrentLogger.Error("Error: --mjd with a value between 0 and 65535 is required for mode parsedate".Pastel(ConsoleColor.Red));
                        break;
                    }

                    lCurrentLogger.Info($"Date = {GetDateFromModifiedJulianDate((int)oLocalOptions.cloModifiedJulianDate):dd.MM.yyyy}".Pastel(ConsoleColor.Cyan));

                    break;
                }
                case FlagMode.scan:
                {
                    if (!ValidateScanOptions(oLocalOptions))
                        break;

                    if (!CreateFirewallRule())
                    {
                        lCurrentLogger.Error("Firewall rule has not been created as there are not sufficient permissions or wrong operation system used. Operation stopped!".Pastel(ConsoleColor.Red));
                        break;
                    }

                    using var osiCurrentOSScanIP = new OSScanIP();
                    osiCurrentOSScanIP.Init(oLocalOptions.cloServerIP);

                    foreach (var iCurrentFrequency in ParseFrequencies(oLocalOptions.cloFrequenciesInMHz!))
                        osiCurrentOSScanIP.AddTransponderInfo(CreateTransponderInfo(oLocalOptions, iCurrentFrequency));

                    osiCurrentOSScanIP.Scan(iLocalScanRetries: oLocalOptions.cloScanRetries, iLocalTunerLockGraceTime: oLocalOptions.cloTunerLockGraceTime, iLocalServerPort: oLocalOptions.cloServerPort);
                    bDone = true;

                    if (oLocalOptions.cloExportServices)
                        osiCurrentOSScanIP.ExportServicesToJson();

                    if (oLocalOptions.cloExportServicesExcel)
                        osiCurrentOSScanIP.ExportServicesToExcel();

                    if (oLocalOptions is { cloEventInformationTable: true, cloExportEvents: true })
                        osiCurrentOSScanIP.ExportEventsToJson();

                    if (oLocalOptions is { cloEventInformationTable: true, cloExportEventsExcel: true })
                        osiCurrentOSScanIP.ExportEventsToExcel();

                    var losCurrentServices = osiCurrentOSScanIP.olotiOSListOSTransponderInfoDone?.OfType<OSTransponderInfo>().SelectMany(otiCurrentOSTransponderInfo => otiCurrentOSTransponderInfo.olosOSListOSService).ToList() ?? [];

                    lCurrentLogger.Info($"SCAN    Transponders: {osiCurrentOSScanIP.olotiOSListOSTransponderInfoDone?.Count ?? 0}    Services: {losCurrentServices.Count}".Pastel(ConsoleColor.Cyan));

                    if (oLocalOptions.cloEventInformationTable)
                    {
                        lCurrentLogger.Info($"EIT     Services: {losCurrentServices.Count(osCurrentOSService => osCurrentOSService.oloeOSListOSEvent.Count > 0)}    Events: {losCurrentServices.Sum(osCurrentOSService => osCurrentOSService.oloeOSListOSEvent.Count)}".Pastel(ConsoleColor.Cyan));
                        lCurrentLogger.Info($"EIT     Received sections: {iEITSections}    Total size: {iEITSize}    Short size: {iEITShortSize}    Events dropped by version change: {iEITEventsDeleted}".Pastel(ConsoleColor.Cyan));
                    }

                    break;
                }
            }

            WaitForKeyPress();
        }

        private static bool ValidateScanOptions(Options oLocalOptions)
        {
            var lsCurrentErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(oLocalOptions.cloServerIP))
                lsCurrentErrors.Add("<server ip> is required");

            if (string.IsNullOrWhiteSpace(oLocalOptions.cloFrequenciesInMHz))
                lsCurrentErrors.Add("--frequencies is required");

            switch (oLocalOptions.cloModulationSystem)
            {
                case null:
                    lsCurrentErrors.Add("--modulationsystem is required");
                    break;
                case FlagModulationSystem.dvbc or FlagModulationSystem.dvbs or FlagModulationSystem.dvbs2 when oLocalOptions.cloSymbolRateInkSymbols is null:
                    lsCurrentErrors.Add($"--symbolrate is required for {oLocalOptions.cloModulationSystem}");
                    break;
            }

            if (!string.IsNullOrEmpty(oLocalOptions.cloPolarisation) && !Polarisation2String.Contains(oLocalOptions.cloPolarisation))
                lsCurrentErrors.Add($"--polarisation must be one of {string.Join(",", Polarisation2String)}");

            if (!string.IsNullOrEmpty(oLocalOptions.cloBandwidth) && !Bandwidth2String.Contains(oLocalOptions.cloBandwidth))
                lsCurrentErrors.Add($"--bandwidth must be one of {string.Join(",", Bandwidth2String)}");

            if (oLocalOptions.cloTunerLockGraceTime < 0)
                lsCurrentErrors.Add("--tunerlockgracetime must not be negative");

            foreach (var strCurrentError in lsCurrentErrors)
                lCurrentLogger.Error($"Error: {strCurrentError}".Pastel(ConsoleColor.Red));

            return lsCurrentErrors.Count == 0;
        }
    }
}