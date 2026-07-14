using Pastel;

using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public static class OSTransportStreamInfo_Extension
    {
        extension(OSTransportStreamInfo otsiLocalOSTransportStreamInfo)
        {
            public void ProcessTransponder(byte[] byaLocalBuffer)
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
}