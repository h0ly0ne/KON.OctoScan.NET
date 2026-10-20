using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

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

    /// <summary>
    /// OSPacketIdentifierInfo.Init()
    /// </summary>
    /// <param name="iLocalPID"></param>
    /// <param name="tsiLocalTSInfo"></param>
    public void Init(int iLocalPID, OSTransportStreamInfo? tsiLocalTSInfo)
    {
        LogTrace("OSPacketIdentifierInfo.Init()");
        
        olotsfOSListOSTransportStreamFilter = [];
        iPacketID = iLocalPID;
        otsiOSTransportStreamInfo = tsiLocalTSInfo;
        bHasContinuityErrors = false;
    }

    /// <summary>
    /// OSPacketIdentifierInfo.Dispose()
    /// </summary>
    public void Dispose()
    {
        LogTrace("OSPacketIdentifierInfo.Dispose()");
        
        olotsfOSListOSTransportStreamFilter?.Clear();
        iPacketID = 0;
        otsiOSTransportStreamInfo = null;
        byaBuffer = null;
    }

    /// <summary>
    /// OSPacketIdentifierInfo.Reset()
    /// </summary>
    public void Reset()
    {
        LogTrace("OSPacketIdentifierInfo.Reset()");
        
        iBufferPointer = iBufferLength = 0;
    }
}