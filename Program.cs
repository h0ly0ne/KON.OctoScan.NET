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
        public class Options
        {
            [Value(0, MetaName = "<server ip>", HelpText = "IP Address of SAT>IP server", Required = true)]
            public string? cloServerIP { get; set; }

            [Option('n', longName: "use_nit", Required = false, HelpText = "Use network information table. If not specified only a single transponder is scanned.")]
            public bool cloUseNetworkInformationTable { get; set; }

            [Option('f', longName: "freq", Required = true, HelpText = "Frequency in MHz.")]
            public int cloFrequencyInMHz { get; set; }

            [Option('s', longName: "sr", Required = false, HelpText = "Symbolrate in kSymbols (required for DVB-S/S2 and DVB-C). DVB-S/S2 example: --sr=27500. DVB-C example: --sr=6900.")]
            public int? cloSymbolRateInkSymbols { get; set; }

            [Option('S', longName: "src", Required = false, HelpText = "Satellite source 1,2,3,4 (required for DVB-S/S2).")]
            public int? cloSatelliteSource { get; set; }

            [Option('p', longName: "pol", Required = false, HelpText = "Polarisation = v,h,r,l (required for DVB-S/S2). Example: --pol=v.")]
            public string? cloPolarisation { get; set; }

            [Option('b', longName: "bw", Required = false, HelpText = "Bandwidth 1.712,5,6,7,8,10 (required for DVB-T/T2).")]
            public string? cloBandwidth { get; set; }

            [Option('P', longName: "isi", Required = false, HelpText = "ImageStreamIdentifier for physical layer pipe. Example: --isi=1")]
            public int? cloPhysicalLayerPipes { get; set; }

            [Option('m', longName: "msys", Required = true, HelpText = "Modulation System = dvbs, dvbs2, dvbc, dvbt, dvbt2. Example: --msys=dvbs.")]
            public string? cloModulationSystem { get; set; }

            [Option('t', longName: "mtype", Required = false, HelpText = "Modulation Type = 16qam,32qam,64qam,128qam,256qam (required for DVB-C).")]
            public string? cloModulationType { get; set; }

            [Option('e', longName: "eit", Required = false, HelpText = "Do an EIT scan.")]
            public bool cloDoEitScan { get; set; }

            [Option('E', longName: "eit_sid", Required = false, HelpText = "Sid list = comma separated list of sid numbers. Example: --eit_sid=1000,1002,3003.")]
            public string? cloEitSidList { get; set; }

            [Option('x', longName: "parse_mjd", Required = false, HelpText = "Parse Date from Modified Julian Date.")]
            public long? cloParseModifiedJulianDate { get; set; }

            [Option('v', longName: "verbose", Required = false, HelpText = "Set output to verbose level.")]
            public bool cloVerbose { get; set; }

            [Option('c', longName: "printservices", Required = false, HelpText = "Output result from services scan.")]
            public bool cloPrintServices { get; set; }

            [Option('g', longName: "exportservices", Required = false, HelpText = "Export result from services scan.")]
            public bool cloExportServices { get; set; }

            [Option('i', longName: "exportservicesexcel", Required = false, HelpText = "Export result from services scan to excel file.")]
            public bool cloExportServicesExcel { get; set; }

            [Option('d', longName: "printevents", Required = false, HelpText = "Output result from events scan.")]
            public bool cloPrintEvents { get; set; }

            [Option('h', longName: "exportevents", Required = false, HelpText = "Export result from events scan.")]
            public bool cloExportEvents { get; set; }

            [Option('j', longName: "exporteventsexcel", Required = false, HelpText = "Export result from events scan to excel file.")]
            public bool cloExportEventsExcel { get; set; }
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
                htCurrentHelpText.AddPostOptionsLine("    KON.OctoScan.NET --use_nit=true --freq=138 --msys=dvbc --sr=6900 --mtype=256qam 10.0.4.24");
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

            var otiCurrentOSTransponderInfo = new OSTransponderInfo();

            oCurrentOptions = oLocalOptions;

            if (oLocalOptions.cloParseModifiedJulianDate != null)
            {
                var iModifiedJulianDate = Convert.ToInt32(oLocalOptions.cloParseModifiedJulianDate);
                int iYear = 0, iMonth = 0, iDay = 0;

                GetDateFromModifiedJulianDate(iModifiedJulianDate, ref iYear, ref iMonth, ref iDay);
                lCurrentLogger.Info(" Date = {0:00}.{1:00}.{2:00}", iDay, iMonth, iYear);

                return;
            }

            if (oLocalOptions.cloUseNetworkInformationTable)
                otiCurrentOSTransponderInfo.bUseNetworkInformationTable = oLocalOptions.cloUseNetworkInformationTable;

            if (oLocalOptions.cloDoEitScan)
                otiCurrentOSTransponderInfo.bScanEventInformationTable = oLocalOptions.cloDoEitScan;

            if (!string.IsNullOrEmpty(oLocalOptions.cloEitSidList))
            {
                var saEitSidList = oLocalOptions.cloEitSidList.Split(',');
                otiCurrentOSTransponderInfo.bScanEventInformationTable = true;

                var iCurrentCounter = 0;
                foreach (var strCurrentEitSidListItem in saEitSidList.Take(MAX_EIT_SID))
                {
                    if (otiCurrentOSTransponderInfo.iaEventInformationTableServiceID[iCurrentCounter] == 0)
                        otiCurrentOSTransponderInfo.iaEventInformationTableServiceID[iCurrentCounter] = Convert.ToInt32(strCurrentEitSidListItem);

                    iCurrentCounter++;
                }
            }

            if (oLocalOptions.cloFrequencyInMHz > 0)
                otiCurrentOSTransponderInfo.iFrequency = Convert.ToInt32(oLocalOptions.cloFrequencyInMHz);

            if (oLocalOptions.cloSymbolRateInkSymbols != null)
                otiCurrentOSTransponderInfo.iSymbolRate = Convert.ToInt32(oLocalOptions.cloSymbolRateInkSymbols);

            if (oLocalOptions.cloSatelliteSource != null)
                otiCurrentOSTransponderInfo.iSource = Convert.ToInt32(oLocalOptions.cloSatelliteSource);

            if (!string.IsNullOrEmpty(oLocalOptions.cloPolarisation))
            {
                var iCurrentCounter = 0;
                foreach (var strPolarisation in Polarity2String)
                {
                    if (strPolarisation == oLocalOptions.cloPolarisation)
                        otiCurrentOSTransponderInfo.iPolarity = iCurrentCounter;

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

            if (oLocalOptions.cloPhysicalLayerPipes is not null)
                otiCurrentOSTransponderInfo.iInputStreamID = Convert.ToInt32(oLocalOptions.cloPhysicalLayerPipes) > 255 ? 0 : Convert.ToInt32(oLocalOptions.cloPhysicalLayerPipes);

            if (!string.IsNullOrEmpty(oLocalOptions.cloModulationSystem))
            {
                var iCurrentCounter = 0;
                foreach (var strModulationSystem in ModulationSystem2String)
                {
                    if (strModulationSystem == oLocalOptions.cloModulationSystem)
                        otiCurrentOSTransponderInfo.iModulationSystem = iCurrentCounter;

                    iCurrentCounter++;
                }
            }

            if (!string.IsNullOrEmpty(oLocalOptions.cloModulationType))
            {
                var iCurrentCounter = 0;
                foreach (var strModulationType in ModulationType2String)
                {
                    if (strModulationType == oLocalOptions.cloModulationType)
                        otiCurrentOSTransponderInfo.iModulationType = iCurrentCounter;

                    iCurrentCounter++;
                }
            }

            if (CreateFirewallRule())
            {
                using (var osiCurrentOSScanIP = new OSScanIP())
                {
                    osiCurrentOSScanIP.Init(oLocalOptions.cloServerIP);
                    osiCurrentOSScanIP.AddTransponderInfo(otiCurrentOSTransponderInfo);
                    osiCurrentOSScanIP.Scan();

                    bDone = true;

                    if (oLocalOptions is { cloDoEitScan: true, cloExportEventsExcel: true })
                        osiCurrentOSScanIP.ExportEventsToExcel();

                    if (oLocalOptions.cloExportServicesExcel)
                        osiCurrentOSScanIP.ExportServicesToExcel();
                }

                lCurrentLogger.Info($"SCAN    Services: {iServices}".Pastel(ConsoleColor.Cyan));
                lCurrentLogger.Info($"EIT   Total size: {iEITSize}           Short size: {iEITShortSize}".Pastel(ConsoleColor.Cyan));
                lCurrentLogger.Info($"EIT     Services: {iEITServices}             Sections: {iEITSections}               Events: {iEITEvents - iEITEventsDeleted}                 ({iEITEventsDeleted} deleted)".Pastel(ConsoleColor.Cyan));

                #if DEBUG
                    Console.ReadKey();
                #endif
            }
            else
            {
                lCurrentLogger.Error("Firewall rule has not been created as there are not sufficient permissions or wrong operation system used. Operation stopped!".Pastel(ConsoleColor.Red));
            }
        }
    }
}