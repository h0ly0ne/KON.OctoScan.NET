using static KON.OctoScan.NET.Constants;

namespace KON.OctoScan.NET
{
    public class OSService
    {
        public OSList<OSEvent> oloeOSListOSEvent = [];

        public string strProviderName = new(Convert.ToChar(0x00), 80);
        public string strName = new(Convert.ToChar(0x00), 80);
        public bool bGotFromProgramMapTable;
        public bool bGotFromServiceDescriptorTable;
        public int iConditionalAccessMode;
        public int iEventInformationTablePresentFollowing;
        public int iEventInformationTableSchedule;
        public int iServiceID;
        public int iTransportStreamID;
        public int iOriginalNetworkID;
        public int iProgramMapTable;
        public int iProgramClockReferencePacketIdentifier;

        public int iVideoPacketIdentifier;
        public byte byVideoPacketIdentifierStreamType;

        public byte byAudioChannels;
        public int[] iaAudioPacketIdentifiers = new int[MAX_ANUM];
        public byte[] byaAudioPacketIdentifiersStreamType = new byte[MAX_ANUM];
        public string[] saAudioPacketIdentifiersLanguage = new string[MAX_ANUM];

        public int iSubtitlePacketIdentifier;
        public int iTeletextPacketIdentifier;
    }
}