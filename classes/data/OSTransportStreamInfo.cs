using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public class OSTransportStreamInfo : IDisposable
{
    public OSList<OSPacketIdentifierInfo>? olopiiOSListOSPacketIdentifierInfo;
    public OSList<OSTransportStreamFilter>? olotsfOSListOSTransportStreamFilter;
    public OSScanTransponder? ostOSScanTransponder;
    public OSPacketIdentifierInfo[]? opiiOSPacketIdentifierInfo;
    public List<OSTransportStreamFilter> lotsfTimedOutProgramMapTables = [];

    public int iTransportStreamID = -1;
    public int iServiceDescriptionTransportStreamID = -1;
    public int iStaleTransportStreamID = -1;
    public long lStaleTransportStreamUntil;
    public bool bDone;
    public long lStreamClock;
    public uint uiLastRTPTimestamp;
    public bool bRTPTimestampValid;
    public bool bRTPClockFallback;
    public long lRTPClockStall;
    public int iRTPClockDiscontinuities;
    public long lIdleCredit;
    public long lFirstDataWallTime;
    public long lFirstDataStreamClock;
    public bool bPIDsChanged;
    public long lLastPIDUpdate;
    public int iPIDRefreshes;

    /// <summary>
    /// OSTransportStreamInfo.Init()
    /// </summary>
    public void Init()
    {
        LogTrace("OSTransportStreamInfo.Init()");

        bDone = false;
        iTransportStreamID = -1;
        iServiceDescriptionTransportStreamID = -1;
        iStaleTransportStreamID = -1;
        lStaleTransportStreamUntil = 0;
        lStreamClock = 0;
        uiLastRTPTimestamp = 0;
        bRTPTimestampValid = false;
        bRTPClockFallback = false;
        lRTPClockStall = 0;
        iRTPClockDiscontinuities = 0;
        lIdleCredit = 0;
        lFirstDataWallTime = 0;
        lFirstDataStreamClock = 0;
        bPIDsChanged = false;
        lLastPIDUpdate = 0;
        iPIDRefreshes = 0;
        lotsfTimedOutProgramMapTables = [];
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