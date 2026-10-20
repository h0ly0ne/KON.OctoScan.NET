using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public class OSTransportStreamInfo : IDisposable
{
    public OSList<OSPacketIdentifierInfo>? olopiiOSListOSPacketIdentifierInfo;
    public OSList<OSTransportStreamFilter>? olotsfOSListOSTransportStreamFilter;
    public OSScanTransponder? ostOSScanTransponder;
    public OSPacketIdentifierInfo[]? opiiOSPacketIdentifierInfo;

    public int iTransportStreamID;
    public bool bDone;
    public long lStreamClock;
    public uint uiLastRTPTimestamp;
    public bool bRTPTimestampValid;
    public bool bRTPClockFallback;
    public long lRTPClockStall;
    public int iRTPClockDiscontinuities;
    public long lIdleCredit;

    /// <summary>
    /// OSTransportStreamInfo.Init()
    /// </summary>
    public void Init()
    {
        LogTrace("OSTransportStreamInfo.Init()");

        bDone = false;
        lStreamClock = 0;
        uiLastRTPTimestamp = 0;
        bRTPTimestampValid = false;
        bRTPClockFallback = false;
        lRTPClockStall = 0;
        iRTPClockDiscontinuities = 0;
        lIdleCredit = 0;
        olopiiOSListOSPacketIdentifierInfo = [];
        olotsfOSListOSTransportStreamFilter = [];
        opiiOSPacketIdentifierInfo = new OSPacketIdentifierInfo[TS_PID_COUNT];

        for (var i = 0; i < TS_PID_COUNT; i++)
        {
            opiiOSPacketIdentifierInfo[i] = new OSPacketIdentifierInfo();
            opiiOSPacketIdentifierInfo[i].Init(i, this);
        }
    }

    /// <summary>
    /// OSTransportStreamInfo.Dispose()
    /// </summary>
    public void Dispose()
    {
        LogTrace("OSTransportStreamInfo.Dispose()");

        if (opiiOSPacketIdentifierInfo != null)
        {
            foreach (var opiiCurrentOSPacketIdentifierInfo in opiiOSPacketIdentifierInfo)
                opiiCurrentOSPacketIdentifierInfo.Dispose();
        }

        olopiiOSListOSPacketIdentifierInfo = null;
        olotsfOSListOSTransportStreamFilter = null;
        opiiOSPacketIdentifierInfo = null;
    }
}