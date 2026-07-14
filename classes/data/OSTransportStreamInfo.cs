using Pastel;

using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class OSTransportStreamInfo : IDisposable
    {
        public OSList<OSPacketIdentifierInfo>? olopiiOSListOSPacketIdentifierInfo;
        public OSList<OSTransportStreamFilter>? olotsfOSListOSTransportStreamFilter;
        public OSScanTransponder? ostOSScanTransponder;
        public OSPacketIdentifierInfo[]? opiiOSPacketIdentifierInfo;

        public int iTransportStreamID;
        public bool bDone;

        public void Init()
        {
            lCurrentLogger.Trace("OSTransportStreamInfo.Init()".Pastel(ConsoleColor.Cyan));

            bDone = false;
            olopiiOSListOSPacketIdentifierInfo = [];
            olotsfOSListOSTransportStreamFilter = [];
            opiiOSPacketIdentifierInfo = new OSPacketIdentifierInfo[8192];

            for (var i = 0; i < 8192; i++)
            {
                opiiOSPacketIdentifierInfo[i] = new OSPacketIdentifierInfo();
                opiiOSPacketIdentifierInfo[i].Init(i, this);
            }
        }

        public void Dispose()
        {
            lCurrentLogger.Trace("OSTransportStreamInfo.Dispose()".Pastel(ConsoleColor.Cyan));

            olopiiOSListOSPacketIdentifierInfo = null;
            olotsfOSListOSTransportStreamFilter = null;
            opiiOSPacketIdentifierInfo = null;

            for (var i = 0; i < 8192; i++)
            {
                opiiOSPacketIdentifierInfo?[i].Dispose();
            }
        }
    }
}