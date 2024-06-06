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
            [Value(0, MetaName = "<server ip>", HelpText = "IP Address of SAT>IP server", Required = true)]
            public string? cloServerIP { get; set; }

            [Option(longName: "mode", Required = true, HelpText = "Execution mode / Operation selection: scan/parsedate")]
            public FlagMode? cloMode { get; set; }
            
            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

            [Option(longName: "mjd", Required = false, HelpText = "Parse Date from Modified Julian Date.")]
            public long? cloModifiedJulianDate { get; set; }

            [Option(longName: "frequencies", Required = false, HelpText = "Frequencies in MHz. Example: --frequencies=378")]
            public string? cloFrequenciesInMHz { get; set; }

            [Option(longName: "modulationsystem", Required = true, HelpText = "Modulation System. Example: --modulationsystem=dvbc")]
            public FlagModulationSystem? cloModulationSystem { get; set; }

            [Option(longName: "modulationtype", Required = false, HelpText = "Modulation Type (required for DVB-C). Example: --modulationtype=t256qam")]
            public FlagModulationType? cloModulationType { get; set; }

            [Option(longName: "symbolrate", Required = false, HelpText = "Symbolrate in kSymbols (required for DVB-S/S2 and DVB-C). Example: --symbolrate=6900")]
            public int? cloSymbolRateInkSymbols { get; set; }

            [Option(longName: "transpondertimeout", Required = false, HelpText = "Timeout in seconds for transponder scanning. Example: --transpondertimeout=30")]
            public int? cloTransponderTimeout { get; set; }

            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

            [Option(longName: "satellitesource", Required = false, HelpText = "Satellite source 1,2,3,4 (required for DVB-S/S2). Example: --satellitesource=1")]
            public int? cloSatelliteSource { get; set; }

            [Option(longName: "polarisation", Required = false, HelpText = "Polarisation v,h,r,l (required for DVB-S/S2). Example: --polarisation=v")]
            public string? cloPolarisation { get; set; }

            [Option(longName: "bandwidth", Required = false, HelpText = "Bandwidth 1.712,5,6,7,8,10 (required for DVB-T/T2). Example: --bandwidth=8")]
            public string? cloBandwidth { get; set; }

            [Option(longName: "nit", Required = false, HelpText = "Use network information table. If specified additional transponders will be scanned from network information table.")]
            public bool cloNetworkInformationTable { get; set; }

            [Option(longName: "eit", Required = false, HelpText = "Use event information table. If specified event information will be collected from transponders.")]
            public bool cloEventInformationTable { get; set; }

            [Option(longName: "eitfiltersid", Required = false, HelpText = "Comma separated list of services that will be looked up from event information table. Example: --eitfiltersid=1000,1002,3003")]
            public string? cloEventInformationTableSIDFilter { get; set; }

            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

            [Option(longName: "imagestreamidentifier", Required = false, HelpText = "ImageStreamIdentifier for physical layer pipe. Example: --imagestreamidentifier=1")]
            public int? cloPhysicalLayerPipes { get; set; }

            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
            
            [Option(longName: "printservices", Required = false, HelpText = "Output result from services scan.")]
            public bool cloPrintServices { get; set; }

            [Option(longName: "exportservices", Required = false, HelpText = "Export result from services scan.")]
            public bool cloExportServices { get; set; }

            [Option(longName: "exportservicesexcel", Required = false, HelpText = "Export result from services scan to excel file.")]
            public bool cloExportServicesExcel { get; set; }

            [Option(longName: "printevents", Required = false, HelpText = "Output result from events scan.")]
            public bool cloPrintEvents { get; set; }

            [Option(longName: "exportevents", Required = false, HelpText = "Export result from events scan.")]
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
                htCurrentHelpText.AddPostOptionsLine("    KON.OctoScan.NET -M Scan --freq=138 --msys=dvbc --sr=6900 --mtype=256qam --use_nit 10.0.4.24");
                htCurrentHelpText.AddPostOptionsLine("");

                return HelpText.DefaultParsingErrorsHandler(result, htCurrentHelpText);
            }, e => e);

            lCurrentLogger.Info(htCurrentHelpText);

            #if DEBUG
                Console.ReadKey();
            #endif
        }

        private static void Run(Options oLocalOptions)
        {
            #if DEBUG
                if (OperatingSystem.IsWindows())
                    Console.BufferHeight = short.MaxValue - 1;
            #endif

            oCurrentOptions = oLocalOptions;

            if (CreateFirewallRule())
            {
                switch (oLocalOptions.cloMode)
                {
                    case FlagMode.parsedate:
                    {
                        var iModifiedJulianDate = Convert.ToInt32(oLocalOptions.cloModifiedJulianDate);
                        int iYear = 0, iMonth = 0, iDay = 0;

                        GetDateFromModifiedJulianDate(iModifiedJulianDate, ref iYear, ref iMonth, ref iDay);
                        lCurrentLogger.Info(" Date = {0:00}.{1:00}.{2:00}", iDay, iMonth, iYear);

                        break;
                    }
                    case FlagMode.scan:
                    {
                        if(!string.IsNullOrEmpty(oLocalOptions.cloFrequenciesInMHz) && oLocalOptions is { cloModulationSystem: not null, cloSymbolRateInkSymbols: not null })
                        {
                            using (var osiCurrentOSScanIP = new OSScanIP())
                            {
                                osiCurrentOSScanIP.Init(oLocalOptions.cloServerIP);

                                var saFrequencies = oLocalOptions.cloFrequenciesInMHz.Split(',');
                                var lsFrequencies = new List<string>();

                                foreach (var strFrequency in saFrequencies)
                                {
                                    if (strFrequency.Contains('-') && strFrequency[^2] == ':')
                                    {
                                        var saFrequenciesRange = strFrequency.Split('-');

                                        if (saFrequenciesRange.Length == 2)
                                        {
                                            var iFrequenciesRangeStart = Convert.ToInt32(saFrequenciesRange[0]);
                                            var iFrequenciesRangeEnd = Convert.ToInt32(saFrequenciesRange[1].Split(':')[0]);
                                            var iFrequenciesRangeStep = Convert.ToInt32(saFrequenciesRange[1].Split(':')[1]);

                                            if (iFrequenciesRangeEnd > iFrequenciesRangeStart && (iFrequenciesRangeEnd - iFrequenciesRangeStart) % iFrequenciesRangeStep == 0)
                                            {
                                                for (var iCurrentFrequency = iFrequenciesRangeStart; iCurrentFrequency <= iFrequenciesRangeEnd; iCurrentFrequency += iFrequenciesRangeStep)
                                                {
                                                    lsFrequencies.Add(Convert.ToString(iCurrentFrequency));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        lsFrequencies.Add(strFrequency);
                                    }
                                }

                                foreach (var lsFrequency in lsFrequencies)
                                {
                                    var otiCurrentOSTransponderInfo = new OSTransponderInfo();

                                    otiCurrentOSTransponderInfo.iFrequency = Convert.ToInt32(lsFrequency);

                                    for (var iModulationSystemCounter = 0; iModulationSystemCounter < ModulationSystem2String.Length; iModulationSystemCounter++)
                                    {
                                        if (ModulationSystem2String[iModulationSystemCounter] != oLocalOptions.cloModulationSystem.ToString())
                                            continue;

                                        otiCurrentOSTransponderInfo.iModulationSystem = iModulationSystemCounter;
                                        break;
                                    }

                                    otiCurrentOSTransponderInfo.iSymbolRate = Convert.ToInt32(oLocalOptions.cloSymbolRateInkSymbols);

                                    if (oLocalOptions.cloModulationType != null)
                                    {
                                        for (var iModulationTypeCounter = 0; iModulationTypeCounter < ModulationType2String.Length; iModulationTypeCounter++)
                                        {
                                            if (ModulationType2String[iModulationTypeCounter] != oLocalOptions.cloModulationType.ToString()?[1..])
                                                continue;

                                            otiCurrentOSTransponderInfo.iModulationType = iModulationTypeCounter;
                                            break;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(oLocalOptions.cloPolarisation))
                                    {
                                        var iCurrentCounter = 0;
                                        foreach (var strPolarisation in Polarisation2String)
                                        {
                                            if (strPolarisation == oLocalOptions.cloPolarisation)
                                                otiCurrentOSTransponderInfo.iPolarisation = iCurrentCounter;

                                            iCurrentCounter++;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(oLocalOptions.cloBandwidth))
                                    {
                                        var iCurrentCounter = 0;
                                        foreach (var strBandwidth in Bandwidth2String)
                                        {
                                            if (strBandwidth == oLocalOptions.cloBandwidth)
                                                otiCurrentOSTransponderInfo.iBandwidth = iCurrentCounter;

                                            iCurrentCounter++;
                                        }
                                    }

                                    if (oLocalOptions.cloSatelliteSource != null)
                                        otiCurrentOSTransponderInfo.iSource = Convert.ToInt32(oLocalOptions.cloSatelliteSource);

                                    if (oLocalOptions.cloNetworkInformationTable)
                                        otiCurrentOSTransponderInfo.bUseNetworkInformationTable = oLocalOptions.cloNetworkInformationTable;

                                    if (oLocalOptions.cloEventInformationTable)
                                        otiCurrentOSTransponderInfo.bScanEventInformationTable = oLocalOptions.cloEventInformationTable;

                                    if (!string.IsNullOrEmpty(oLocalOptions.cloEventInformationTableSIDFilter))
                                    {
                                        var saEitSidList = oLocalOptions.cloEventInformationTableSIDFilter.Split(',');
                                        var istrCurrentEitSidListCounter = 0;

                                        foreach (var strCurrentEitSidListItem in saEitSidList.Take(MAX_EIT_SID))
                                        {
                                            if (otiCurrentOSTransponderInfo.iaEventInformationTableServiceID[istrCurrentEitSidListCounter] == 0)
                                                otiCurrentOSTransponderInfo.iaEventInformationTableServiceID[istrCurrentEitSidListCounter] = Convert.ToInt32(strCurrentEitSidListItem);

                                            istrCurrentEitSidListCounter++;
                                        }
                                    }

                                    if (oLocalOptions.cloPhysicalLayerPipes is not null)
                                        otiCurrentOSTransponderInfo.iInputStreamID = Convert.ToInt32(oLocalOptions.cloPhysicalLayerPipes) > 255 ? 0 : Convert.ToInt32(oLocalOptions.cloPhysicalLayerPipes);

                                    osiCurrentOSScanIP.AddTransponderInfo(otiCurrentOSTransponderInfo);
                                }

                                if (oLocalOptions.cloTransponderTimeout != null)
                                    osiCurrentOSScanIP.Scan(Convert.ToInt32(oLocalOptions.cloTransponderTimeout));
                                else
                                    osiCurrentOSScanIP.Scan();

                                bDone = true;

                                if (oLocalOptions is { cloEventInformationTable: true, cloExportEventsExcel: true })
                                    osiCurrentOSScanIP.ExportEventsToExcel();

                                if (oLocalOptions.cloExportServicesExcel)
                                    osiCurrentOSScanIP.ExportServicesToExcel();
                            }

                            lCurrentLogger.Info($"SCAN    Services: {iServices}".Pastel(ConsoleColor.Cyan));
                            lCurrentLogger.Info($"EIT   Total size: {iEITSize}           Short size: {iEITShortSize}".Pastel(ConsoleColor.Cyan));
                            lCurrentLogger.Info($"EIT     Services: {iEITServices}             Sections: {iEITSections}               Events: {iEITEvents - iEITEventsDeleted}                 ({iEITEventsDeleted} deleted)".Pastel(ConsoleColor.Cyan));
                        }

                        break;
                    }
                }
            }
            else
            {
                lCurrentLogger.Error("Firewall rule has not been created as there are not sufficient permissions or wrong operation system used. Operation stopped!".Pastel(ConsoleColor.Red));
            }

            #if DEBUG
                Console.ReadKey();
            #endif
        }
    }
}