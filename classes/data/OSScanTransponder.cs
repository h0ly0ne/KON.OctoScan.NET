namespace KON.OctoScan.NET
{
    public class OSScanTransponder
    {
        public OSScanIP? osiOSScanIP;
        public OSTransponderInfo? otiOSTransponderInfo;
        public OSTransportStreamInfo? otsiOSTransportStreamInfo = new();
        public OSSatIPConnection? osicOSSatIPConnection = new();

        public long lTimeout;
        public long lTimestamp;
        public bool bTimedOut;
        public long lRetries;
    }
}