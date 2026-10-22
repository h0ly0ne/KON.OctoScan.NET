namespace KON.OctoScan.NET;

public class OSScanTransponder
{
    public OSScanIP? osiOSScanIP;
    public OSTransponderInfo? otiOSTransponderInfo;
    public OSTransportStreamInfo? otsiOSTransportStreamInfo = new();
    public OSSatIPConnection? osicOSSatIPConnection = new();

    public bool bFailed = false;
    public bool bTimeout = false;
    public bool bNoSignal = false;
    public bool bPermanentError = false;
    public string? strFailReason;
    public int iTunerLock = -1;
    public int iTunerLevel;
    public int iTunerQuality;
    public bool bTunerStatusReceived;
    public bool bTunerFrequencyConfirmed;
    public int iLastTransportStreamID = -1;
}