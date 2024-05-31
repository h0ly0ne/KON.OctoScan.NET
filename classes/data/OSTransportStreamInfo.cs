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

    public static class OSTransportStreamInfo_Extension
    {
        public static void ProcessTransponder(this OSTransportStreamInfo otsiLocalOSTransportStreamInfo, byte[] byaLocalBuffer)
        {
            lCurrentLogger.Trace("OSTransportStreamInfo.ProcessTransponder()".Pastel(ConsoleColor.Cyan));

            var iPID = 0x1FFF & ((byaLocalBuffer[1] << 8) | byaLocalBuffer[2]);
            var opiiCurrentOSPacketIdentifierInfo = otsiLocalOSTransportStreamInfo.opiiOSPacketIdentifierInfo?[iPID];

            if (opiiCurrentOSPacketIdentifierInfo != null)
            {
                if (opiiCurrentOSPacketIdentifierInfo is { bUsed: false })
                    return;

                if (opiiCurrentOSPacketIdentifierInfo is { byaBuffer: null })
                {
                    opiiCurrentOSPacketIdentifierInfo.byaBuffer = new byte[4096];
                    if (opiiCurrentOSPacketIdentifierInfo is { byaBuffer: null })
                        return;

                    opiiCurrentOSPacketIdentifierInfo.byContinuityCounter = 0xFF;
                }

                opiiCurrentOSPacketIdentifierInfo.BuildSection(byaLocalBuffer);
            }
        }
    }
}