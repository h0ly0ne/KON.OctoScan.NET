using System.Net.Sockets;
using System.Text;

using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public static class Extensions
{
    /// <summary>
    /// Extensions.CleanupInvalidData()
    /// </summary>
    /// <param name="strLocalString"></param>
    /// <returns>string</returns>
    public static string CleanupInvalidData(this string strLocalString)
    {
        LogTrace("Extensions.CleanupInvalidData()");

        var sbCurrentStringBuilder = new StringBuilder(strLocalString.Length);

        foreach (var cCurrentChar in strLocalString)
        {
            if (cCurrentChar is '\u008A' or '')
                sbCurrentStringBuilder.Append(' ');
            else if (!char.IsControl(cCurrentChar) && cCurrentChar is not (>= '' and <= ''))
                sbCurrentStringBuilder.Append(cCurrentChar);
        }

        return sbCurrentStringBuilder.ToString().Trim();
    }

    /// <summary>
    /// Extensions.SendData()
    /// </summary>
    /// <param name="nsLocalSocket"></param>
    /// <param name="strLocalMessage"></param>
    /// <returns>bool</returns>
    public static bool SendData(this Socket? nsLocalSocket, string strLocalMessage)
    {
        LogTrace("Extensions.SendData()");

        if (nsLocalSocket == null)
            return false;

        var byaCurrentBuffer = Encoding.ASCII.GetBytes(strLocalMessage);

        try
        {
            for (var iPacketDone = 0; iPacketDone < byaCurrentBuffer.Length;)
                iPacketDone += nsLocalSocket.Send(byaCurrentBuffer, iPacketDone, byaCurrentBuffer.Length - iPacketDone, SocketFlags.None);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Extensions.ToUShortAsInteger()
    /// </summary>
    /// <param name="byaLocalBuffer"></param>
    /// <param name="iLocalStartIndex"></param>
    /// <returns>int</returns>
    public static int ToUShortAsInteger(this byte[]? byaLocalBuffer, int iLocalStartIndex)
    {
        LogTrace("Extensions.ToUShortAsInteger()");

        if (byaLocalBuffer == null || iLocalStartIndex + 1 >= byaLocalBuffer.Length)
            return 0;

        return (byaLocalBuffer[iLocalStartIndex] << 8) | byaLocalBuffer[iLocalStartIndex + 1];
    }

    /// <summary>
    /// Extensions.ToDVBLengthAsInteger()
    /// </summary>
    /// <param name="byaLocalBuffer"></param>
    /// <param name="iLocalStartIndex"></param>
    /// <returns>int</returns>
    public static int ToDVBLengthAsInteger(this byte[]? byaLocalBuffer, int iLocalStartIndex)
    {
        LogTrace("Extensions.ToDVBLengthAsInteger()");

        if (byaLocalBuffer == null || iLocalStartIndex + 1 >= byaLocalBuffer.Length)
            return 0;

        return ((byaLocalBuffer[iLocalStartIndex] & 0x0F) << 8) | byaLocalBuffer[iLocalStartIndex + 1];
    }

    /// <summary>
    /// Extensions.ToPIDAsInteger()
    /// </summary>
    /// <param name="byaLocalBuffer"></param>
    /// <param name="iLocalStartIndex"></param>
    /// <returns>int</returns>
    public static int ToPIDAsInteger(this byte[]? byaLocalBuffer, int iLocalStartIndex)
    {
        LogTrace("Extensions.ToPIDAsInteger()");

        if (byaLocalBuffer == null || iLocalStartIndex + 1 >= byaLocalBuffer.Length)
            return 0;

        return ((byaLocalBuffer[iLocalStartIndex] & 0x1F) << 8) | byaLocalBuffer[iLocalStartIndex + 1];
    }

    /// <summary>
    /// Extensions.ToBCDAsInteger()
    /// </summary>
    /// <param name="byaLocalBuffer"></param>
    /// <param name="iLocalStartIndex"></param>
    /// <param name="iLocalLength">Amount of BCD digits</param>
    /// <returns>int</returns>
    public static int ToBCDAsInteger(this byte[]? byaLocalBuffer, int iLocalStartIndex, int iLocalLength)
    {
        LogTrace("Extensions.ToBCDAsInteger()");

        if (byaLocalBuffer == null || iLocalStartIndex + (iLocalLength + 1) / 2 > byaLocalBuffer.Length)
            return 0;

        var iCurrentValue = 0;

        for (var i = 0; i < iLocalLength / 2; i++)
        {
            var byCurrentByte = byaLocalBuffer[iLocalStartIndex + i];
            iCurrentValue = iCurrentValue * 100 + (byCurrentByte >> 4) * 10 + (byCurrentByte & 0x0F);
        }

        if ((iLocalLength & 1) != 0)
            iCurrentValue = iCurrentValue * 10 + (byaLocalBuffer[iLocalStartIndex + iLocalLength / 2] >> 4);

        return iCurrentValue;
    }
}