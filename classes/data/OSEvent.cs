namespace KON.OctoScan.NET
{
    public class OSEvent
    {
        public int iOriginalNetworkID;
        public int iTransportStreamID;
        public int iServiceID;
        public int iEventID;
        public int iModifiedJulianDate;
        public byte bStartHour;
        public byte bStartMinute;
        public byte bStartSecond;
        public byte bDurationHour;
        public byte bDurationMinute;
        public byte bDurationSecond;
        public string? strLanguage;
        public string? strName;
        public string? strText;
        public byte bTableID;
    }
}