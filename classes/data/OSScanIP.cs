using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public class OSScanIP : IDisposable
{
    public OSList<OSTransponderInfo?>? olotiOSListOSTransponderInfo;
    public OSList<OSTransponderInfo?>? olotiOSListOSTransponderInfoDone;
    public OSScanTransponder? ostOSScanTransponder;

    public string? strHost;

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
        strHost = strLocalHost;
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
        strHost = null;
    }
}