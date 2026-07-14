using Pastel;

using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class OSPacketIdentifierInfo : IDisposable
    {
        public OSTransportStreamInfo? otsiOSTransportStreamInfo;
        public OSList<OSTransportStreamFilter>? olotsfOSListOSTransportStreamFilter;

        public int iPacketID;
        public int iUseTableExtension;
        public bool bUsed;
        public byte byContinuityCounter;
        public int iBufferPointer;
        public int iBufferLength;
        public byte[]? byaBuffer;
        public bool bHasContinuityErrors;

        public void Init(int iLocalPID, OSTransportStreamInfo? tsiLocalTSInfo)
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.Init()".Pastel(ConsoleColor.Cyan));

            olotsfOSListOSTransportStreamFilter = [];
            olotsfOSListOSTransportStreamFilter?.Initialize();

            iPacketID = iLocalPID;
            otsiOSTransportStreamInfo = tsiLocalTSInfo;
            bHasContinuityErrors = false;
        }

        public void Dispose()
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.Dispose()".Pastel(ConsoleColor.Cyan));

            olotsfOSListOSTransportStreamFilter?.Clear();

            iPacketID = 0;
            otsiOSTransportStreamInfo = null;
        }

        public void Reset()
        {
            lCurrentLogger.Trace("OSPacketIdentifierInfo.Reset()".Pastel(ConsoleColor.Cyan));

            iBufferPointer = iBufferLength = 0;
        }
    }
}