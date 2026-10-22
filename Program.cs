using System.Diagnostics;
using System.Drawing;
using System.Text;

using CommandLine;
using CommandLine.Text;
using NLog;
using NLog.Layouts;
using Pastel;

using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class Program
    {
        public class Options
        {
            [Option(longName: "mode", Required = true, HelpText = "Execution-Mode or Operation to use.")]
            public FlagMode cloMode { get; set; }

            [Option(longName: "submode", HelpText = "Execution-Sub-Mode or Sub-Operation to use.")]
            public FlagSubMode cloSubMode { get; set; }

            [Option(longName: "ipv6", HelpText = "Enable IPv6 Support.", Default = false)]
            public bool cloIPv6 { get; set; }

            [Option(longName: "serverip", HelpText = "IP Address of SAT>IP server.")]
            public string? cloServerIP { get; set; }

            [Option(longName: "serverport", HelpText = "Port of SAT>IP server.")]
            public string? cloServerPort { get; set; }

            [Option(longName: "frequencies", HelpText = "Frequencies in MHz, comma separated. Ranges as start-end:step.")]
            public string? cloFrequencies { get; set; }

            [Option(longName: "modulationsystem", HelpText = "Modulation System.")]
            public FlagModulationSystem cloModulationSystem { get; set; }

            [Option(longName: "modulationtype", HelpText = "Modulation Type.")]
            public FlagModulationType cloModulationType { get; set; }

            [Option(longName: "symbolrate", HelpText = "Symbolrate in kSymbols.")]
            public int? cloSymbolRate { get; set; }

            [Option(longName: "scanretries", HelpText = "Retries per transponder (new connection and session) on timeouts and temporary server errors.")]
            public int? cloScanRetries { get; set; }

            [Option(longName: "tunerlockgracetime", HelpText = "Time in milliseconds the tuner gets to lock before a transponder is skipped as without signal.")]
            public int? cloTunerLockGraceTime { get; set; }

            [Option(longName: "exportservices", HelpText = "Export result from services scan to json file.")]
            public bool cloExportServices { get; set; }

            [Option(longName: "exportservicesexcel", HelpText = "Export result from services scan to excel file.")]
            public bool cloExportServicesExcel { get; set; }

            [Option(longName: "eit", HelpText = "Use event information table. If specified event information will be collected from transponders.")]
            public bool cloEventInformationTable { get; set; }

            [Option(longName: "eitfiltersid", HelpText = "Comma separated list of services that will be looked up from event information table.")]
            public string? cloEventInformationTableSIDFilter { get; set; }

            [Option(longName: "nit", HelpText = "Use network information table. If specified additional transponders will be scanned from network information table.")]
            public bool cloNetworkInformationTable { get; set; }

            [Option(longName: "satellitesource", HelpText = "Satellite source.")]
            public int? cloSatelliteSource { get; set; }

            [Option(longName: "polarisation", HelpText = "Polarisation.")]
            public string? cloPolarisation { get; set; }

            [Option(longName: "bandwidth", HelpText = "Bandwidth.")]
            public string? cloBandwidth { get; set; }

            [Option(longName: "imagestreamidentifier", HelpText = "ImageStreamIdentifier for physical layer pipe.")]
            public int? cloImageStreamIdentifier { get; set; }

            [Option(longName: "exportevents", HelpText = "Export result from events scan to json file.")]
            public bool cloExportEvents { get; set; }

            [Option(longName: "exporteventsexcel", HelpText = "Export result from events scan to excel file.")]
            public bool cloExportEventsExcel { get; set; }

            [Option(longName: "printservices", HelpText = "Output result from services scan.")]
            public bool cloPrintServices { get; set; }

            [Option(longName: "printevents", HelpText = "Output result from events scan.")]
            public bool cloPrintEvents { get; set; }

            [Option(longName: "mjd", HelpText = "Parse Modified-Julian-Date to get normal date.")]
            public long? cloModifiedJulianDate { get; set; }

            [Option(longName: "verbose", HelpText = "Set output to verbose level.")]
            public bool cloVerbose { get; set; }
        }

        /// <summary>
        /// Program.WaitForKeyPress()
        /// </summary>
        [Conditional("DEBUG")]
        private static void WaitForKeyPress()
        {
            LogTrace("Program.WaitForKeyPress()");

            if (Console.IsInputRedirected)
                return;

            lCurrentLogger.Info("Press any key to continue ...".Pastel(ConsoleColor.Green));
            Console.ReadKey();
        }

        /// <summary>
        /// Program.Main()
        /// </summary>
        /// <param name="saLocalArguments"></param>
        private static void Main(string[] saLocalArguments)
        {
            LogTrace("Program.Main()");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var pCurrentParser = new Parser(apsCurrentConfiguration =>
            {
                apsCurrentConfiguration.HelpWriter = null;
                apsCurrentConfiguration.CaseInsensitiveEnumValues = true;
            });
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

        /// <summary>
        /// Program.DisplayHelp()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prLocalParseResult"></param>
        private static void DisplayHelp<T>(ParserResult<T> prLocalParseResult)
        {
            LogTrace("Program.DisplayHelp()");

            var htCurrentHelpText = HelpText.AutoBuild(prLocalParseResult, htCurrentHelpText =>
            {
                htCurrentHelpText.AddNewLineBetweenHelpSections = false;
                htCurrentHelpText.AdditionalNewLineAfterOption = false;
                htCurrentHelpText.MaximumDisplayWidth = 100;
                htCurrentHelpText.Heading = Constants.cstrCurrentProjectName.Pastel(Color.Green);
                htCurrentHelpText.Copyright = Constants.cstrCurrentProjectCopyrightLine1.Pastel(Color.LimeGreen) + Environment.NewLine + Constants.cstrCurrentProjectCopyrightLine2.Pastel(Color.LimeGreen);

                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan a single DVB-C transponder".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 346 --modulationsystem DVBC");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationtype T256QAM --symbolrate 6900");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan a DVB-C frequency range (start-end:step) and output the services".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 114,130-858:8");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationsystem DVBC --modulationtype T256QAM --symbolrate 6900 --printservices");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: NIT based DVB-C scan - further transponders are taken from the network information table".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 138 --modulationsystem DVBC");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationtype T256QAM --symbolrate 6900 --nit");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan DVB-C services and export them to json and excel".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 346-362:8");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationsystem DVBC --modulationtype T256QAM --symbolrate 6900 --exportservices");
                htCurrentHelpText.AddPostOptionsLine($"    --exportservicesexcel");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan DVB-C services with events, output the events and export them to json and excel".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 346 --modulationsystem DVBC");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationtype T256QAM --symbolrate 6900 --eit --printevents");
                htCurrentHelpText.AddPostOptionsLine($"    --exportevents --exporteventsexcel");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan DVB-C events of selected services only".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 346 --modulationsystem DVBC");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationtype T256QAM --symbolrate 6900 --eit --eitfiltersid 100,200 --printevents");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan a DVB-S transponder (satellite source 1, horizontal polarisation)".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 12188 --modulationsystem DVBS");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationtype TQPSK --symbolrate 27500 --satellitesource 1 --polarisation h");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan DVB-S2 transponders (satellite source 1, vertical polarisation)".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 11494,11523");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationsystem DVBS2 --modulationtype T8PSK --symbolrate 22000 --satellitesource 1");
                htCurrentHelpText.AddPostOptionsLine($"    --polarisation v");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan DVB-T transponders (bandwidth 8 MHz)".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 474-858:8");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationsystem DVBT --bandwidth 8");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan a DVB-T2 transponder (bandwidth 8 MHz, physical layer pipe 0)".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 538 --modulationsystem DVBT2");
                htCurrentHelpText.AddPostOptionsLine($"    --bandwidth 8 --imagestreamidentifier 0");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan DVB-C with 5 retries per transponder and 5000 ms tuner lock grace time".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --serverip 192.168.1.10 --serverport 554 --frequencies 346-858:8");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationsystem DVBC --modulationtype T256QAM --symbolrate 6900 --scanretries 5");
                htCurrentHelpText.AddPostOptionsLine($"    --tunerlockgracetime 5000");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Scan DVB-C via IPv6 with verbose (trace) output".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Scan --ipv6 --serverip fd00::10 --serverport 554 --frequencies 346");
                htCurrentHelpText.AddPostOptionsLine($"    --modulationsystem DVBC --modulationtype T256QAM --symbolrate 6900 --verbose");
                htCurrentHelpText.AddPostOptionsLine($"");
                htCurrentHelpText.AddPostOptionsLine($"* e.g.: Parse a Modified Julian Date to a normal date".Pastel(Color.Chocolate));
                htCurrentHelpText.AddPostOptionsLine($"    --mode Parse --submode MJD --mjd 60000");
                htCurrentHelpText.AddPostOptionsLine($"");

                return HelpText.DefaultParsingErrorsHandler(prLocalParseResult, htCurrentHelpText);
            }, e => e);

            lCurrentLogger.Info(htCurrentHelpText);
        }

        /// <summary>
        /// Program.DisplayHelpWithCustomCondition()
        /// </summary>
        /// <param name="saLocalCustomConditions"></param>
        private static void DisplayHelpWithCustomCondition(string[] saLocalCustomConditions)
        {
            LogTrace("Program.DisplayHelpWithCustomCondition()");

            var prCurrentParseResult = new Parser(x => { x.HelpWriter = null; }).ParseArguments<Options>(["--help"]);
            var htCurrentHelpText = HelpText.AutoBuild(prCurrentParseResult, htCurrentHelpText =>
            {
                htCurrentHelpText.AddNewLineBetweenHelpSections = true;
                htCurrentHelpText.AdditionalNewLineAfterOption = false;
                htCurrentHelpText.MaximumDisplayWidth = 100;
                htCurrentHelpText.Heading = Constants.cstrCurrentProjectName.Pastel(Color.Green);
                htCurrentHelpText.Copyright = Constants.cstrCurrentProjectCopyrightLine1.Pastel(Color.LimeGreen) + Environment.NewLine + Constants.cstrCurrentProjectCopyrightLine2.Pastel(Color.LimeGreen);

                if (saLocalCustomConditions.Length > 0)
                {
                    htCurrentHelpText.AddPreOptionsLine("ERROR(S):");

                    foreach (var strCurrentCustomCondition in saLocalCustomConditions)
                    {
                        if (!string.IsNullOrEmpty(strCurrentCustomCondition))
                            htCurrentHelpText.AddPreOptionsLine($"  * Custom option '{strCurrentCustomCondition}' not met or missing.");
                    }
                }

                return HelpText.DefaultParsingErrorsHandler(prCurrentParseResult, htCurrentHelpText);
            }, e => e);

            lCurrentLogger.Info(htCurrentHelpText);
        }

        /// <summary>
        /// Program.Run()
        /// </summary>
        /// <param name="oLocalOptions"></param>
        private static void Run(Options oLocalOptions)
        {
            LogTrace("Program.Run()");

            #if DEBUG
                try
                {
                    if (OperatingSystem.IsWindows())
                        Console.BufferHeight = short.MaxValue - 1;
                }
                catch (Exception) { /* Not supported by every terminal */ }
            #endif

            if (CreateFirewallRule())
            {
                string[] saLocalCustomCondition = [];

                /* Check for custom conditions */
                switch (oLocalOptions.cloMode)
                {
                    case FlagMode.Scan:
                    {
                        if (oLocalOptions.cloSubMode != FlagSubMode.None)
                            saLocalCustomCondition = [.. saLocalCustomCondition, "submode"];

                        saLocalCustomCondition = string.IsNullOrEmpty(oLocalOptions.cloServerIP) ? [.. saLocalCustomCondition, "serverip"] : saLocalCustomCondition;
                        saLocalCustomCondition = string.IsNullOrEmpty(oLocalOptions.cloServerPort) ? [.. saLocalCustomCondition, "serverport"] : saLocalCustomCondition;
                        saLocalCustomCondition = string.IsNullOrEmpty(oLocalOptions.cloFrequencies) ? [.. saLocalCustomCondition, "frequencies"] : saLocalCustomCondition;

                        if (oLocalOptions.cloModulationSystem == FlagModulationSystem.None)
                            saLocalCustomCondition = [.. saLocalCustomCondition, "modulationsystem"];
                        else
                        {
                            switch (oLocalOptions.cloModulationSystem)
                            {
                                case FlagModulationSystem.DVBC:
                                {
                                    if (oLocalOptions.cloModulationType == FlagModulationType.None)
                                        saLocalCustomCondition = [.. saLocalCustomCondition, "modulationtype"];

                                    if (oLocalOptions.cloSymbolRate is null or <= 0)
                                        saLocalCustomCondition = [.. saLocalCustomCondition, "symbolrate"];

                                    break;
                                }
                                case FlagModulationSystem.DVBS:
                                case FlagModulationSystem.DVBS2:
                                {
                                    if (oLocalOptions.cloModulationType == FlagModulationType.None)
                                        saLocalCustomCondition = [.. saLocalCustomCondition, "modulationtype"];

                                    if (oLocalOptions.cloSatelliteSource is null or <= 0 or > 4)
                                        saLocalCustomCondition = [.. saLocalCustomCondition, "satellitesource"];

                                    if (string.IsNullOrEmpty(oLocalOptions.cloPolarisation) || !Constants.Polarisation2String.Contains(oLocalOptions.cloPolarisation, StringComparer.OrdinalIgnoreCase))
                                        saLocalCustomCondition = [.. saLocalCustomCondition, "polarisation"];

                                    if (oLocalOptions.cloSymbolRate is null or <= 0)
                                        saLocalCustomCondition = [.. saLocalCustomCondition, "symbolrate"];
                                    
                                    break;
                                }
                                case FlagModulationSystem.DVBT:
                                case FlagModulationSystem.DVBT2:
                                {
                                    if (string.IsNullOrEmpty(oLocalOptions.cloBandwidth) || !Constants.Bandwidth2String.Contains(oLocalOptions.cloBandwidth))
                                        saLocalCustomCondition = [.. saLocalCustomCondition, "bandwidth"];

                                    break;
                                }
                            }
                        }

                        if (oLocalOptions.cloScanRetries is <= 0)
                            saLocalCustomCondition = [.. saLocalCustomCondition, "scanretries"];

                        if (oLocalOptions.cloTunerLockGraceTime is <= 0)
                            saLocalCustomCondition = [.. saLocalCustomCondition, "tunerlockgracetime"];

                        if (oLocalOptions.cloImageStreamIdentifier is not (null or (>= 0 and <= 255)))
                            saLocalCustomCondition = [.. saLocalCustomCondition, "imagestreamidentifier"];

                        break;
                    }
                    case FlagMode.Parse:
                    {
                        switch (oLocalOptions.cloSubMode)
                        {
                            case FlagSubMode.None:
                            {
                                saLocalCustomCondition = [.. saLocalCustomCondition, "submode"];
                                break;
                            }
                            case FlagSubMode.MJD:
                            {
                                if (oLocalOptions.cloModifiedJulianDate is not (>= 0 and <= ushort.MaxValue))
                                    saLocalCustomCondition = [.. saLocalCustomCondition, "mjd"];

                                break;
                            }
                        }

                        break;
                    }
                }
                /* End-Check for custom conditions */

                // Show help dialog if custom conditions are not met
                if (saLocalCustomCondition.Length > 0)
                    DisplayHelpWithCustomCondition(saLocalCustomCondition);
                // Do operation if all custom conditions are met
                else
                {
                    switch (oLocalOptions.cloMode)
                    {
                        case FlagMode.Scan:
                        {
                            using var osiCurrentOSScanIP = new OSScanIP();
                            osiCurrentOSScanIP.Init(oLocalOptions.cloServerIP);

                            foreach (var iCurrentFrequency in ParseFrequencies(oLocalOptions.cloFrequencies!))
                                osiCurrentOSScanIP.AddTransponderInfo(CreateTransponderInfo(oLocalOptions, iCurrentFrequency));

                            osiCurrentOSScanIP.Scan(oLocalOptions);
                            bDone = true;

                            foreach (var otiCurrentOSTransponderInfo in osiCurrentOSScanIP.olotiOSListOSTransponderInfoDone?.OfType<OSTransponderInfo>() ?? [])
                            {
                                if (oLocalOptions is { cloEventInformationTable: true, cloPrintEvents: true })
                                    otiCurrentOSTransponderInfo.PrintEvents();

                                if (oLocalOptions.cloPrintServices)
                                    otiCurrentOSTransponderInfo.PrintServices();
                            }

                            if (oLocalOptions.cloExportServices)
                                osiCurrentOSScanIP.ExportServicesToJson();

                            if (oLocalOptions.cloExportServicesExcel)
                                osiCurrentOSScanIP.ExportServicesToExcel();

                            if (oLocalOptions is { cloEventInformationTable: true, cloExportEvents: true })
                                osiCurrentOSScanIP.ExportEventsToJson();

                            if (oLocalOptions is { cloEventInformationTable: true, cloExportEventsExcel: true })
                                osiCurrentOSScanIP.ExportEventsToExcel();

                            var losCurrentServices = osiCurrentOSScanIP.olotiOSListOSTransponderInfoDone?.OfType<OSTransponderInfo>().SelectMany(otiCurrentOSTransponderInfo => otiCurrentOSTransponderInfo.olosOSListOSService).ToList() ?? [];

                            lCurrentLogger.Info($"-» SCAN    Transponders: {osiCurrentOSScanIP.olotiOSListOSTransponderInfoDone?.Count ?? 0}    Services: {losCurrentServices.Count}".Pastel(ConsoleColor.Cyan));

                            if (oLocalOptions is { cloEventInformationTable: true, cloPrintEvents: true })
                            {
                                lCurrentLogger.Info($"-» EIT     Services: {losCurrentServices.Count(osCurrentOSService => osCurrentOSService.oloeOSListOSEvent.Count > 0)}    Events: {losCurrentServices.Sum(osCurrentOSService => osCurrentOSService.oloeOSListOSEvent.Count)}".Pastel(ConsoleColor.Cyan));
                                lCurrentLogger.Info($"-» EIT     Received sections: {iEITSections}    Total size: {iEITSize}    Short size: {iEITShortSize}    Events dropped by version change: {iEITEventsDeleted}".Pastel(ConsoleColor.Cyan));
                            }

                            break;
                        }
                        case FlagMode.Parse:
                        {
                            switch (oLocalOptions.cloSubMode)
                            {
                                case FlagSubMode.MJD:
                                {
                                    lCurrentLogger.Info($"-» {Constants.cstrCurrentProjectName}.MJD {(oLocalOptions.cloModifiedJulianDate != null ? (int)oLocalOptions.cloModifiedJulianDate : 0)} to System.DateOnly = {GetDateFromModifiedJulianDate((oLocalOptions.cloModifiedJulianDate != null ? (int)oLocalOptions.cloModifiedJulianDate : 0)):dd.MM.yyyy}".Pastel(ConsoleColor.Cyan));
                                    break;
                                }
                            }
                            
                            break;
                        }
                    }
                }
            }
            else
            {
                lCurrentLogger.Error("-» Firewall rule has not been created as there are not sufficient permissions or wrong operation system used. Operation stopped!".Pastel(ConsoleColor.Red));
            }

            WaitForKeyPress();
        }
    }
}