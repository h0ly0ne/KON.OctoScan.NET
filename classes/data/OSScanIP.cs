using Pastel;

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
        }

        public void Dispose()
        {
            lCurrentLogger.Trace("OSScanIP.Dispose()".Pastel(ConsoleColor.Cyan));

            olotiOSListOSTransponderInfo?.Clear();
            olotiOSListOSTransponderInfoDone?.Clear();

            ostOSScanTransponder = null;
            bDone = false;
            strHost = null;
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

            return otiLocalSourceOSTransponderInfo.iPolarity == otiLocalDestinationOSTransponderInfo.iPolarity;
        }

        public static bool Scan(this OSScanIP? osiLocalOSScanIP)
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
                ostCurrentOSScanTransponder.Scan();
                var iEndTime = CurrentTimestamp();
                otsiCurrentOSTransportStreamInfo.Dispose();

                osiLocalOSScanIP.olotiOSListOSTransponderInfo?.Remove(otiCurrentOSTransponderInfo);
                osiLocalOSScanIP.olotiOSListOSTransponderInfoDone?.AddLast(otiCurrentOSTransponderInfo);

                lCurrentLogger.Info($"OPERATION(S) FINISHED (AND TOOK {iEndTime-iStartTime} SECONDS)".Pastel(ConsoleColor.Green));
            }

            return true;
        }
    }
}