using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public class OSScanIP : IDisposable
{
    public OSList<OSTransponderInfo?>? olotiOSListOSTransponderInfo;
    public OSList<OSTransponderInfo?>? olotiOSListOSTransponderInfoDone;
    public OSScanTransponder? ostOSScanTransponder;

    public string? strHost;
    public bool bDone;
    public long lRetries;

    /// <summary>
    /// OSScanIP.Init()
    /// </summary>
    /// <param name="strLocalHost"></param>
    public void Init(string? strLocalHost)
    {
        LogTrace("OSScanIP.Init()");

        olotiOSListOSTransponderInfo = [];
        olotiOSListOSTransponderInfoDone = [];
        ostOSScanTransponder = new OSScanTransponder();
        bDone = false;
        strHost = strLocalHost;
        lRetries = 0;
    }

    /// <summary>
    /// OSScanIP.Dispose()
    /// </summary>
    public void Dispose()
    {
        LogTrace("OSScanIP.Dispose()");

        olotiOSListOSTransponderInfo?.Clear();
        olotiOSListOSTransponderInfoDone?.Clear();
        ostOSScanTransponder = null;
        bDone = false;
        strHost = null;
        lRetries = 0;
    }
}