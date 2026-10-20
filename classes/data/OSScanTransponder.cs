namespace KON.OctoScan.NET
{
    public class OSScanTransponder
    {
        public OSScanIP? osiOSScanIP;
        public OSTransponderInfo? otiOSTransponderInfo;
        public OSTransportStreamInfo? otsiOSTransportStreamInfo = new();
        public OSSatIPConnection? osicOSSatIPConnection = new();

        public bool bFailed = false;
        public bool bTimeout = false;
        public long lRetries;
    }
}