using NLog;
using Pastel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Program;

namespace KON.OctoScan.NET;

public static class Global
{
    public static readonly Logger lCurrentLogger = LogManager.GetCurrentClassLogger();
    private static Encoding? eISO6937Encoding;
    private static readonly Dictionary<int, Encoding> dieISO8859Encodings = [];
    private static readonly Dictionary<string, string> dssEventLanguages = new(StringComparer.OrdinalIgnoreCase);
    
    public static bool bDone = false;
    public static int iEITSize = 0;
    public static int iEITSections = 0;
    public static int iEITShortSize = 0;
    public static int iEITEventsDeleted = 0;
    private const int NET_FW_IP_PROTOCOL_TCP = 6;
    private const int NET_FW_IP_PROTOCOL_UDP = 17;
    private const int NET_FW_RULE_DIR_IN = 1;
    private const int NET_FW_RULE_DIR_OUT = 2;
    private const int NET_FW_ACTION_ALLOW = 1;

    public enum FlagMode
    {
        Scan = 0,
        Parse = 1
    }
    public enum FlagSubMode
    {
        None = 0,
        MJD = 1
    }
    public enum FlagModulationSystem
    {
        None = 0,
        DVBC = 1,
        DVBCB = 2,
        DVBT = 3,
        DSS = 4,
        DVBS = 5,
        DVBS2 = 6,
        DVBH = 7,
        ISDBT = 8,
        ISDBS = 9,
        ISDBC = 10,
        ATSC = 11,
        ATSCMH = 12,
        DTMB = 13,
        CMMB = 14,
        DAB = 15,
        DVBT2 = 16,
        TURBO = 17,
        DVBCC = 18,
        DVBC2 = 19
    }
    public enum FlagModulationType
    {
        None = 0,
        TQPSK = 1,
        T16QAM = 2,
        T32QAM = 3,
        T64QAM = 4,
        T128QAM = 5,
        T256QAM = 6,
        TAUTOQAM = 7,
        T8VSB = 8,
        T16VSB = 9,
        T8PSK = 10,
        T16APSK = 11,
        T32APSK = 12,
        TDQPSK = 13,
        T4QAMNR = 14
    }
    
    /// <summary>
    /// Global.LogTrace()
    /// </summary>
    /// <param name="strLocalMessage"></param>
    public static void LogTrace(string strLocalMessage)
    {
        if (lCurrentLogger.IsTraceEnabled)
            lCurrentLogger.Trace(strLocalMessage.Pastel(ConsoleColor.Cyan));
    }

    /// <summary>
    /// Global.CurrentTimestamp()
    /// </summary>
    /// <returns>long</returns>
    public static long CurrentTimestamp()
    {
        LogTrace("Global.CurrentTimestamp()");

        return DateTimeOffset.Now.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Global.Retry()
    /// </summary>
    /// <param name="iLocalRetries"></param>
    /// <param name="fLocalAction"></param>
    /// <param name="iLocalRetriesUsed"></param>
    /// <returns>bool</returns>
    public static bool Retry(int iLocalRetries, Func<bool> fLocalAction, out int iLocalRetriesUsed)
    {
        LogTrace("Global.Retry()");

        for (iLocalRetriesUsed = 0; ; iLocalRetriesUsed++)
        {
            if (fLocalAction())
                return true;

            if (iLocalRetriesUsed >= iLocalRetries || bDone)
                return false;

            Thread.Sleep(RETRY_WAITTIME);
        }
    }

    /// <summary>
    /// Global.CreateFirewallRule()
    /// </summary>
    /// <returns>bool</returns>
    public static bool CreateFirewallRule()
    {
        LogTrace("Global.CreateFirewallRule()");

        if (!OperatingSystem.IsWindows())
        {
            lCurrentLogger.Warn("-» No windows operating system - cannot create firewall rules.".Pastel(ConsoleColor.Yellow));
            return false;
        }

        var tCurrentFwPolicyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", false);
        var tCurrentFwRuleType = Type.GetTypeFromProgID("HNetCfg.FWRule", false);

        if (tCurrentFwPolicyType == null || tCurrentFwRuleType == null)
            return false;

        (string strName, int iProtocol, int iDirection)[] taCurrentRules =
        [
            (FIREWALL_RULE_APP_NAME + "_OUT_TCP", NET_FW_IP_PROTOCOL_TCP, NET_FW_RULE_DIR_OUT),
            (FIREWALL_RULE_APP_NAME + "_IN_TCP", NET_FW_IP_PROTOCOL_TCP, NET_FW_RULE_DIR_IN),
            (FIREWALL_RULE_APP_NAME + "_OUT_UDP", NET_FW_IP_PROTOCOL_UDP, NET_FW_RULE_DIR_OUT),
            (FIREWALL_RULE_APP_NAME + "_IN_UDP", NET_FW_IP_PROTOCOL_UDP, NET_FW_RULE_DIR_IN)
        ];

        try
        {
            dynamic dCurrentFwPolicy = Activator.CreateInstance(tCurrentFwPolicyType)!;
            var dCurrentFwRules = dCurrentFwPolicy.Rules;

            if (taCurrentRules.All(tCurrentRule => FirewallRuleExists(dCurrentFwRules, tCurrentRule.strName)))
                return true;

            foreach (var tCurrentRule in taCurrentRules)
            {
                dCurrentFwRules.Remove(tCurrentRule.strName);

                dynamic dCurrentFwRule = Activator.CreateInstance(tCurrentFwRuleType)!;
                dCurrentFwRule.Name = tCurrentRule.strName;
                dCurrentFwRule.Description = tCurrentRule.strName;
                dCurrentFwRule.Protocol = tCurrentRule.iProtocol;
                dCurrentFwRule.Action = NET_FW_ACTION_ALLOW;
                dCurrentFwRule.Direction = tCurrentRule.iDirection;
                dCurrentFwRule.ApplicationName = Environment.ProcessPath;
                dCurrentFwRule.Enabled = true;
                dCurrentFwRule.InterfaceTypes = "All";
                dCurrentFwRules.Add(dCurrentFwRule);
            }

            return true;
        }
        catch (Exception excCurrentException)
        {
            lCurrentLogger.Error($"-» Error on creating firewall rules: {excCurrentException.Message}.".Pastel(ConsoleColor.Red));
            return false;
        }
    }

    /// <summary>
    /// Global.FirewallRuleExists()
    /// </summary>
    /// <param name="dLocalFwRules"></param>
    /// <param name="strLocalRuleName"></param>
    /// <returns>bool</returns>
    private static bool FirewallRuleExists(dynamic dLocalFwRules, string strLocalRuleName)
    {
        LogTrace("Global.FirewallRuleExists()");

        try
        {
            var dCurrentFwRule = dLocalFwRules.Item(strLocalRuleName);
            return (bool)dCurrentFwRule.Enabled && string.Equals((string)dCurrentFwRule.ApplicationName, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Global.GetCRC32()
    /// </summary>
    /// <param name="rosLocalBuffer"></param>
    /// <returns>uint</returns>
    public static uint GetCRC32(ReadOnlySpan<byte> rosLocalBuffer)
    {
        LogTrace("Global.GetCRC32()");

        var uiCurrentCRCValue = 0xFFFFFFFF;

        foreach (var byCurrentByte in rosLocalBuffer)
            uiCurrentCRCValue = (uiCurrentCRCValue << 8) ^ uiaDVBCRCChecksumTable[((uiCurrentCRCValue >> 24) ^ byCurrentByte) & 0xFF];

        return uiCurrentCRCValue;
    }

    /// <summary>
    /// Global.GetDateFromModifiedJulianDate()
    /// </summary>
    /// <param name="iLocalModifiedJulianDate"></param>
    /// <returns>DateOnly</returns>
    public static DateOnly GetDateFromModifiedJulianDate(int iLocalModifiedJulianDate)
    {
        LogTrace("Global.GetDateFromModifiedJulianDate()");

        if (iLocalModifiedJulianDate < 51604)
            iLocalModifiedJulianDate += 65536;

        return new DateOnly(1858, 11, 17).AddDays(iLocalModifiedJulianDate);
    }

    /// <summary>
    /// Global.GetTransportStreamPayload()
    /// </summary>
    /// <param name="rosLocalPacket"></param>
    /// <returns>int</returns>
    public static int GetTransportStreamPayload(ReadOnlySpan<byte> rosLocalPacket)
    {
        LogTrace("Global.GetTransportStreamPayload()");

        if ((rosLocalPacket[3] & 0x10) == 0)
            return 0;

        if ((rosLocalPacket[3] & 0x20) == 0)
            return 184;

        if (rosLocalPacket[4] > 183)
            return 0;

        return 183 - rosLocalPacket[4];
    }

    /// <summary>
    /// Global.HasTransportStreamPayload()
    /// </summary>
    /// <param name="rosLocalPacket"></param>
    /// <returns>bool</returns>
    public static bool HasTransportStreamPayload(ReadOnlySpan<byte> rosLocalPacket)
    {
        LogTrace("Global.HasTransportStreamPayload()");

        if ((rosLocalPacket[3] & 0x10) == 0)
            return false;

        if ((rosLocalPacket[3] & 0x20) == 0)
            return true;

        return rosLocalPacket[4] <= 183;
    }

    /// <summary>
    /// Global.SectorLength()
    /// </summary>
    /// <param name="rosLocalBuffer"></param>
    /// <returns>int</returns>
    public static int SectorLength(ReadOnlySpan<byte> rosLocalBuffer)
    {
        LogTrace("Global.SectorLength()");

        return 3 + ((rosLocalBuffer[1] & 0x0F) << 8) + rosLocalBuffer[2];
    }

    /// <summary>
    /// Global.AllZeroOrCounted()
    /// </summary>
    /// <param name="uiaLocalElements"></param>
    /// <returns>bool</returns>
    public static bool AllZeroOrCounted(uint[] uiaLocalElements)
    {
        LogTrace("Global.AllZeroOrCounted()");

        return uiaLocalElements.All(uiCurrentElement => uiCurrentElement == 0);
    }

    /// <summary>
    /// Global.FormatRTSPHost()
    /// </summary>
    /// <param name="strLocalHost"></param>
    /// <returns>string</returns>
    public static string FormatRTSPHost(string? strLocalHost)
    {
        LogTrace("Global.FormatRTSPHost()");

        return IPAddress.TryParse(strLocalHost, out var ipaCurrentAddress) && ipaCurrentAddress.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{strLocalHost}]" : strLocalHost ?? string.Empty;
    }

    /// <summary>
    /// Global.DecodeDVBString()
    /// </summary>
    /// <param name="rosLocalBuffer"></param>
    /// <returns>string</returns>
    public static string DecodeDVBString(ReadOnlySpan<byte> rosLocalBuffer)
    {
        LogTrace("Global.DecodeDVBString()");

        if (rosLocalBuffer.IsEmpty)
            return string.Empty;

        Encoding eCurrentEncoding;
        var iCurrentOffset = 1;
        var byCurrentCharacterTable = rosLocalBuffer[0];

        switch (byCurrentCharacterTable)
        {
            case >= CCT_ENCODING_ISO_6937:
                eCurrentEncoding = GetISO6937Encoding();
                iCurrentOffset = 0;
                break;
            case >= CCT_ENCODING_ISO_8859_5 and <= CCT_ENCODING_ISO_8859_15:
                eCurrentEncoding = GetISO8859Encoding(byCurrentCharacterTable + 4);
                break;
            case CCT_ENCODING_ISO_8859_DYNAMIC_PARTS when rosLocalBuffer.Length >= 3:
                eCurrentEncoding = GetISO8859Encoding((rosLocalBuffer[1] << 8) | rosLocalBuffer[2]);
                iCurrentOffset = 3;
                break;
            case CCT_ENCODING_ISO_10646:
                eCurrentEncoding = Encoding.BigEndianUnicode;
                break;
            case CCT_ENCODING_KSX_1001:
                eCurrentEncoding = Encoding.GetEncoding("x-cp20949");
                break;
            case CCT_ENCODING_GB_2312:
                eCurrentEncoding = Encoding.GetEncoding("gb2312");
                break;
            case CCT_ENCODING_ISO_10646_BIG5:
                eCurrentEncoding = Encoding.GetEncoding("big5");
                break;
            case CCT_ENCODING_ISO_10646_UTF8:
                eCurrentEncoding = Encoding.UTF8;
                break;
            case CCT_ENCODING_CUSTOM when rosLocalBuffer.Length >= 2:
                eCurrentEncoding = GetISO6937Encoding();
                iCurrentOffset = 2;
                break;
            default:
                eCurrentEncoding = GetISO6937Encoding();
                break;
        }

        return eCurrentEncoding.GetString(rosLocalBuffer[iCurrentOffset..]).CleanupInvalidData();
    }

    /// <summary>
    /// Global.GetISO6937Encoding()
    /// </summary>
    /// <returns>Encoding</returns>
    private static Encoding GetISO6937Encoding()
    {
        LogTrace("Global.GetISO6937Encoding()");

        return eISO6937Encoding ??= Encoding.GetEncoding(20269);
    }

    /// <summary>
    /// Global.GetISO8859Encoding()
    /// </summary>
    /// <param name="iLocalPart"></param>
    /// <returns>Encoding</returns>
    private static Encoding GetISO8859Encoding(int iLocalPart)
    {
        LogTrace("Global.GetISO8859Encoding()");

        if (dieISO8859Encodings.TryGetValue(iLocalPart, out var eCurrentEncoding))
            return eCurrentEncoding;

        try
        {
            eCurrentEncoding = iLocalPart == 11 ? Encoding.GetEncoding(874) : Encoding.GetEncoding($"iso-8859-{iLocalPart}");
        }
        catch (Exception excCurrentException) when (excCurrentException is ArgumentException or NotSupportedException)
        {
            eCurrentEncoding = Encoding.Latin1;
        }

        dieISO8859Encodings[iLocalPart] = eCurrentEncoding;
        return eCurrentEncoding;
    }

    /// <summary>
    /// Global.GetISO639LanguageCode()
    /// </summary>
    /// <param name="strLocalLanguageCode"></param>
    /// <returns>string</returns>
    public static string GetISO639LanguageCode(string strLocalLanguageCode)
    {
        LogTrace("Global.GetISO639LanguageCode()");

        return ISO639LanguageCollection.ISO639LanguagesByCode.TryGetValue(strLocalLanguageCode, out var lCurrentLanguage) ? lCurrentLanguage.ISO6393Code : "mis";
    }

    /// <summary>
    /// Global.GetEventLanguageCode()
    /// </summary>
    /// <param name="strLocalLanguageCode"></param>
    /// <returns>string</returns>
    public static string GetEventLanguageCode(string strLocalLanguageCode)
    {
        LogTrace("Global.GetEventLanguageCode()");

        if (dssEventLanguages.TryGetValue(strLocalLanguageCode, out var strCurrentLanguageCode))
            return strCurrentLanguageCode;

        strCurrentLanguageCode = strLocalLanguageCode.ToLowerInvariant();

        if (ISO639LanguageCollection.ISO639LanguagesByCode.TryGetValue(strLocalLanguageCode, out var lCurrentLanguage) && lCurrentLanguage.ISO6392Code != null)
        {
            try
            {
                strCurrentLanguageCode = CultureInfo.GetCultureInfo(lCurrentLanguage.ISO6392Code).ThreeLetterISOLanguageName.ToLowerInvariant();
            }
            catch (CultureNotFoundException)
            {
                strCurrentLanguageCode = lCurrentLanguage.ISO6393Code;
            }
        }

        dssEventLanguages[strLocalLanguageCode] = strCurrentLanguageCode;
        return strCurrentLanguageCode;
    }

    /// <summary>
    /// Global.ParseFrequencies()
    /// </summary>
    /// <param name="strLocalFrequencies"></param>
    /// <returns>List(int)</returns>
    public static List<int> ParseFrequencies(string strLocalFrequencies)
    {
        LogTrace("Global.ParseFrequencies()");

        var liCurrentFrequencies = new List<int>();

        foreach (var strCurrentFrequency in strLocalFrequencies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var saCurrentRange = strCurrentFrequency.Split('-', ':');

            if (saCurrentRange.Length == 1 && int.TryParse(saCurrentRange[0], out var iCurrentFrequency))
                liCurrentFrequencies.Add(iCurrentFrequency);
            else if (saCurrentRange.Length == 3 && int.TryParse(saCurrentRange[0], out var iRangeStart) && int.TryParse(saCurrentRange[1], out var iRangeEnd) && int.TryParse(saCurrentRange[2], out var iRangeStep) && iRangeStep > 0 && iRangeEnd >= iRangeStart)
            {
                if ((iRangeEnd - iRangeStart) % iRangeStep != 0)
                    lCurrentLogger.Warn($"-» Frequency range {strCurrentFrequency} does not end on a step - last frequency is {iRangeEnd - (iRangeEnd - iRangeStart) % iRangeStep}".Pastel(ConsoleColor.Yellow));

                for (var iCurrentRangeFrequency = iRangeStart; iCurrentRangeFrequency <= iRangeEnd; iCurrentRangeFrequency += iRangeStep)
                    liCurrentFrequencies.Add(iCurrentRangeFrequency);
            }
            else
                lCurrentLogger.Warn($"-» Invalid frequency or frequency range {strCurrentFrequency} skipped".Pastel(ConsoleColor.Yellow));
        }

        return liCurrentFrequencies;
    }

    /// <summary>
    /// Global.CreateTransponderInfo()
    /// </summary>
    /// <param name="oLocalOptions"></param>
    /// <param name="iLocalFrequency"></param>
    /// <returns>OSTransponderInfo</returns>
    public static OSTransponderInfo CreateTransponderInfo(Options oLocalOptions, int iLocalFrequency)
    {
        LogTrace("Global.CreateTransponderInfo()");

        var otiCurrentOSTransponderInfo = new OSTransponderInfo
        {
            iFrequency = iLocalFrequency,
            iModulationSystem = Array.FindIndex(ModulationSystem2String, strCurrentModulationSystem => strCurrentModulationSystem.Equals(oLocalOptions.cloModulationSystem.ToString(), StringComparison.OrdinalIgnoreCase)),
            iSymbolRate = Convert.ToInt32(oLocalOptions.cloSymbolRate),
            iSource = oLocalOptions.cloSatelliteSource ?? 0,
            bUseNetworkInformationTable = oLocalOptions.cloNetworkInformationTable,
            bScanEventInformationTable = oLocalOptions.cloEventInformationTable,
            iInputStreamID = oLocalOptions.cloImageStreamIdentifier is >= 0 and <= 255 ? oLocalOptions.cloImageStreamIdentifier.Value : 0
        };

        if (oLocalOptions.cloModulationType != FlagModulationType.None)
            otiCurrentOSTransponderInfo.iModulationType = Math.Max(Array.FindIndex(ModulationType2String, strCurrentModulationType => strCurrentModulationType.Equals(oLocalOptions.cloModulationType.ToString()[1..], StringComparison.OrdinalIgnoreCase)), 0);

        if (!string.IsNullOrEmpty(oLocalOptions.cloPolarisation))
            otiCurrentOSTransponderInfo.iPolarisation = Array.FindIndex(Polarisation2String, strCurrentPolarisation => strCurrentPolarisation.Equals(oLocalOptions.cloPolarisation, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(oLocalOptions.cloBandwidth))
            otiCurrentOSTransponderInfo.iBandwidth = Array.IndexOf(Bandwidth2String, oLocalOptions.cloBandwidth);

        if (string.IsNullOrEmpty(oLocalOptions.cloEventInformationTableSIDFilter)) 
            return otiCurrentOSTransponderInfo;
        
        var iCurrentServiceIDCounter = 0;

        foreach (var strCurrentServiceID in oLocalOptions.cloEventInformationTableSIDFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(MAX_EIT_SID))
        {
            if (int.TryParse(strCurrentServiceID, out var iCurrentServiceID))
                otiCurrentOSTransponderInfo.iaEventInformationTableServiceID[iCurrentServiceIDCounter++] = iCurrentServiceID;
            else
                lCurrentLogger.Warn($"-» Invalid service id {strCurrentServiceID} in --eitfiltersid skipped".Pastel(ConsoleColor.Yellow));
        }

        return otiCurrentOSTransponderInfo;
    }

    /// <summary>
    /// Global.CreateTransponderInfo()
    /// </summary>
    /// <param name="iLocalFrequency"></param>
    /// <param name="iModulationSystem"></param>
    /// <param name="iSymbolRate"></param>
    /// <param name="iModulationType"></param>
    /// <returns>OSTransponderInfo</returns>
    public static OSTransponderInfo CreateTransponderInfo(int iLocalFrequency, int iModulationSystem, int iSymbolRate, int iModulationType)
    {
        LogTrace("Global.CreateTransponderInfo()");

        var otiCurrentOSTransponderInfo = new OSTransponderInfo
        {
            iFrequency = iLocalFrequency,
            iModulationSystem = iModulationSystem,
            iModulationType = iModulationType,
            iSymbolRate = iSymbolRate,
            iSource = 0,
            bUseNetworkInformationTable = false,
            bScanEventInformationTable = false,
            iInputStreamID = 0
        };

        return otiCurrentOSTransponderInfo;
    }
}