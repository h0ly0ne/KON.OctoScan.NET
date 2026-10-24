using static KON.OctoScan.NET.Enums;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public class OSService
{
    public OSList<OSEvent> oloeOSListOSEvent = [];
    public Dictionary<(byte byTableID, int iEventID), LinkedListNode<OSEvent>> dtllnOSEventsByID = [];
    public List<int> liAudioPacketIdentifiers = [];
    public List<byte> lbAudioPacketIdentifiersStreamType = [];
    public List<string> lsAudioPacketIdentifiersLanguage = [];
    public List<string> lsAudioPacketIdentifiersCodec = [];
    public List<int> liOtherPacketIdentifiers = [];
    public List<byte> lbOtherPacketIdentifiersStreamType = [];
    public List<int> liCASystemIDs = [];

    public string strProviderName = string.Empty;
    public string strName = string.Empty;
    public bool bGotFromProgramMapTable;
    public bool bGotFromServiceDescriptorTable;
    public int iServiceID;
    public int iEventInformationTableSchedule;
    public int iEventInformationTablePresentFollowing;
    public int iRunningStatus;
    public int iTransportStreamID;
    public int iOriginalNetworkID;
    public int iProgramMapTable;
    public int iProgramClockReferencePacketIdentifier;
    public int iVideoPacketIdentifier;
    public byte byVideoPacketIdentifierStreamType;
    public int iSubtitlePacketIdentifier;
    public int iTeletextPacketIdentifier;
    public ScramblingState ssScramblingState;

    /// <summary>
    /// OSService.IsEncrypted() - the measured scrambling decides, without measurement (no packets received) the
    /// CA systems of the PMT
    /// </summary>
    /// <returns>bool</returns>
    public bool IsEncrypted()
    {
        LogTrace("OSService.IsEncrypted()");

        return ssScramblingState switch
        {
            ScramblingState.Scrambled => true,
            ScramblingState.Clear => false,
            _ => liCASystemIDs.Count > 0
        };
    }
}