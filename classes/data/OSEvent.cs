using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

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

    /// <summary>
    /// OSEvent.GetStartTime()
    /// </summary>
    /// <returns>string</returns>
    public string GetStartTime()
    {
        LogTrace("OSEvent.GetStartTime()");

        return $"{GetDateFromModifiedJulianDate(iModifiedJulianDate):yyyy-MM-dd}T{bStartHour:D2}:{bStartMinute:D2}:{bStartSecond:D2}Z";
    }

    /// <summary>
    /// OSEvent.GetDuration()
    /// </summary>
    /// <returns>string</returns>
    public string GetDuration()
    {
        LogTrace("OSEvent.GetDuration()");

        return $"{bDurationHour:D2}:{bDurationMinute:D2}:{bDurationSecond:D2}";
    }
}