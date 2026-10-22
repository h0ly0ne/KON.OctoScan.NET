using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public class OSTransponderInfo
{
    public OSList<OSService> olosOSListOSService = [];
    public List<(int iPacketID, int iServiceID)> ltiiMissingProgramMapTables = [];

    public int iType;
    public bool bUseNetworkInformationTable;
    public bool bScanEventInformationTable;
    public int iSource;
    public int iNetworkID;
    public int iOriginalNetworkID;
    public int iTransportStreamID;
    public int iPosition;
    public int iEAST;
    public int iModulationSystem;
    public int iFrequency;
    public int iFrequencyFraction;
    public int iPolarisation;
    public int iSymbolRate;
    public int iRollOff;
    public int iModulationType;
    public int iBandwidth;
    public int iFEC;
    public int iInputStreamID;
    public int[] iaEventInformationTableServiceID = new int[MAX_EIT_SID];

    /// <summary>
    /// OSTransponderInfo.ToString()
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        LogTrace("OSTransponderInfo.ToString()");

        var strCurrentFrequency = iFrequencyFraction != 0 ? $"{iFrequency}.{iFrequencyFraction:D4}" : $"{iFrequency}";

        return ModulationSystem2String.ElementAtOrDefault(iModulationSystem) switch
        {
            "dvbc" => $"freq={strCurrentFrequency}&msys=dvbc&sr={iSymbolRate}&mtype={ModulationType2String.ElementAtOrDefault(iModulationType)}",
            "dvbt" => $"freq={strCurrentFrequency}&msys=dvbt&bw={Bandwidth2String.ElementAtOrDefault(iBandwidth)}",
            "dvbt2" => $"freq={strCurrentFrequency}&msys=dvbt2&bw={Bandwidth2String.ElementAtOrDefault(iBandwidth)}&plp={iInputStreamID}",
            "dvbs" or "dvbs2" => $"src={iSource}&freq={iFrequency}&pol={Polarisation2String[iPolarisation & 3]}&msys={ModulationSystem2String[iModulationSystem]}&sr={iSymbolRate}",
            _ => string.Empty
        };
    }
}