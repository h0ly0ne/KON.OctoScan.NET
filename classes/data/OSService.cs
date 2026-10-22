namespace KON.OctoScan.NET;

public class OSService
{
    public OSList<OSEvent> oloeOSListOSEvent = [];
    public List<int> liAudioPacketIdentifiers = [];
    public List<byte> lbAudioPacketIdentifiersStreamType = [];
    public List<string> lsAudioPacketIdentifiersLanguage = [];
    public List<int> liOtherPacketIdentifiers = [];
    public List<byte> lbOtherPacketIdentifiersStreamType = [];

    public string strProviderName = string.Empty;
    public string strName = string.Empty;
    public bool bGotFromProgramMapTable;
    public bool bGotFromServiceDescriptorTable;
    public int iServiceID;
    public int iEventInformationTableSchedule;
    public int iEventInformationTablePresentFollowing;
    public int iConditionalAccessMode;
    public int iRunningStatus;
    public int iTransportStreamID;
    public int iOriginalNetworkID;
    public int iProgramMapTable;
    public int iProgramClockReferencePacketIdentifier;
    public int iVideoPacketIdentifier;
    public byte byVideoPacketIdentifierStreamType;
    public int iSubtitlePacketIdentifier;
    public int iTeletextPacketIdentifier;
}