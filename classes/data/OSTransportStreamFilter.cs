namespace KON.OctoScan.NET
{
    public class OSTransportStreamFilter
    {
        public OSPacketIdentifierInfo? opiiOSPacketIdentifierInfo;

        public byte byTableID;
        public int iTableExtension;
        public byte byVersionNumber;
        public bool bIsToDoSet;
        public bool bDone;
        public int iUseTableExtension;
        public bool bIsVersionNumberSet;
        public uint[] laToDo = new uint[8];
        public long lTimestamp;
        public long lTimeout;
    }
}