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
}