using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

using NLog;
using Pastel;

namespace KON.OctoScan.NET
{
    public static class Extensions
    {
        public static readonly Logger lCurrentLogger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// string.CleanupInvalidData()
        /// </summary>
        /// <param name="strLocalString"></param>
        /// <returns></returns>
        public static string CleanupInvalidData(this string strLocalString)
        {
            lCurrentLogger.Trace("Global.CleanupInvalidData()".Pastel(ConsoleColor.Cyan));

            var strReturnString = strLocalString;

            strReturnString = Regex.Replace(strReturnString, @"[\x00-\x1F]", string.Empty);
            strReturnString = Regex.Replace(strReturnString, "\0", string.Empty);

            return strReturnString;
        }

        /// <summary>
        /// byte.GetLSB()
        /// </summary>
        /// <param name="byLocalByteValue"></param>
        /// <returns></returns>
        public static byte GetLSB(this byte byLocalByteValue)
        {
            lCurrentLogger.Trace("Global.GetLSB()".Pastel(ConsoleColor.Cyan));

            return (byte)(byLocalByteValue & 0x0F);
        }

        /// <summary>
        /// Socket?.SendData()
        /// </summary>
        /// <param name="nsLocalSocket"></param>
        /// <param name="baLocalBuffer"></param>
        /// <param name="iDataLength"></param>
        /// <returns></returns>
        public static int SendData(this Socket? nsLocalSocket, byte[] baLocalBuffer, int iDataLength)
        {
            lCurrentLogger.Trace("Global.SendData()".Pastel(ConsoleColor.Cyan));

            var iPacketDone = 0;

            if (nsLocalSocket == null)
                return 0;

            for (var iPacketToDo = iDataLength; iPacketToDo > 0; iPacketToDo -= iPacketDone)
            {
                try
                {
                    iPacketDone = nsLocalSocket.Send(baLocalBuffer, iDataLength - iPacketToDo, iPacketToDo, SocketFlags.None);
                }
                catch
                {
                    return iPacketDone;
                }
            }

            return iDataLength;
        }

        /// <summary>
        /// BitArray.GetBitsAsInt32()
        /// </summary>
        /// <param name="baLocalBitArray"></param>
        /// <param name="iLocalStartBit"></param>
        /// <param name="iLocalBitAmount"></param>
        /// <returns></returns>
        public static int GetBitsAsInt32(this BitArray baLocalBitArray, int? iLocalStartBit, int? iLocalBitAmount)
        {
            lCurrentLogger.Trace("Global.GetBitsAsInt32()".Pastel(ConsoleColor.Cyan));

            if (baLocalBitArray.Length > 32 || iLocalBitAmount > 32)
                iLocalBitAmount = 32;

            if (iLocalBitAmount is null or < 1)
                iLocalBitAmount = baLocalBitArray.Length;

            if (iLocalStartBit == null || iLocalStartBit > iLocalBitAmount)
                iLocalStartBit = 0;

            var iBitsAsInt32 = 0;
            for (var iBitsItterator = (int)iLocalStartBit; iBitsItterator < (iLocalStartBit + iLocalBitAmount); iBitsItterator++)
            {
                if (baLocalBitArray.Get(iBitsItterator))
                    iBitsAsInt32 |= 1 << (iBitsItterator - (int)iLocalStartBit);
            }

            return iBitsAsInt32;
        }

        /// <summary>
        /// BitArray.GetBitsAsInt64()
        /// </summary>
        /// <param name="baLocalBitArray"></param>
        /// <param name="iLocalStartBit"></param>
        /// <param name="iLocalBitAmount"></param>
        /// <returns></returns>
        public static long GetBitsAsInt64(this BitArray baLocalBitArray, int? iLocalStartBit, int? iLocalBitAmount)
        {
            lCurrentLogger.Trace("Global.GetBitsAsInt64()".Pastel(ConsoleColor.Cyan));

            if (baLocalBitArray.Length > 32 || iLocalBitAmount > 32)
                iLocalBitAmount = 32;

            if (iLocalBitAmount is null or < 1)
                iLocalBitAmount = baLocalBitArray.Length;

            if (iLocalStartBit == null || iLocalStartBit > iLocalBitAmount)
                iLocalStartBit = 0;

            long iBitsAsInt64 = 0;
            for (var iBitsItterator = (int)iLocalStartBit; iBitsItterator < (iLocalStartBit + iLocalBitAmount); iBitsItterator++)
            {
                if (baLocalBitArray.Get(iBitsItterator))
                    iBitsAsInt64 |= (uint)(1 << (iBitsItterator - (int)iLocalStartBit));
            }

            return iBitsAsInt64;
        }

        /// <summary>
        /// string.ToHexString()
        /// </summary>
        /// <param name="strLocalString"></param>
        /// <returns></returns>
        public static string ToHexString(this string strLocalString)
        {
            lCurrentLogger.Trace("Global.ToHexString()".Pastel(ConsoleColor.Cyan));

            return string.IsNullOrEmpty(strLocalString) ? string.Empty : BitConverter.ToString(Encoding.Default.GetBytes(strLocalString)).Replace("-", string.Empty);
        }

        /// <summary>
        /// string.StringJoin()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string StringJoin<T>(this IEnumerable<T> source, string separator = ",")
        {
            lCurrentLogger.Trace("Global.StringJoin<T>()".Pastel(ConsoleColor.Cyan));

            return string.Join(separator, source);
        }

        /// <summary>
        /// byte[].ToUShortAsInteger()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <returns></returns>
        public static int ToUShortAsInteger(this byte[]? byaLocalBuffer)
        {
            lCurrentLogger.Trace("Global.ToUShortAsInteger()".Pastel(ConsoleColor.Cyan));

            return byaLocalBuffer.ToUShortAsInteger(0);
        }

        /// <summary>
        /// byte[].ToUShortAsInteger()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <param name="iLocalStartIndex"></param>
        /// <returns></returns>
        public static int ToUShortAsInteger(this byte[]? byaLocalBuffer, int iLocalStartIndex)
        {
            lCurrentLogger.Trace("Global.ToUShortAsInteger()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer == null || iLocalStartIndex + 1 >= byaLocalBuffer.Length)
                return 0;

            return (byaLocalBuffer[iLocalStartIndex] << 8) | byaLocalBuffer[iLocalStartIndex + 1];
        }

        /// <summary>
        /// byte[].ToDVBLengthAsInteger()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <returns></returns>
        public static int ToDVBLengthAsInteger(this byte[]? byaLocalBuffer)
        {
            lCurrentLogger.Trace("Global.ToDVBLengthAsInteger()".Pastel(ConsoleColor.Cyan));

            return byaLocalBuffer.ToDVBLengthAsInteger(0);
        }

        /// <summary>
        /// byte[].ToDVBLengthAsInteger()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <param name="iLocalStartIndex"></param>
        /// <returns></returns>
        public static int ToDVBLengthAsInteger(this byte[]? byaLocalBuffer, int iLocalStartIndex)
        {
            lCurrentLogger.Trace("Global.ToByteAsInteger()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer == null || iLocalStartIndex + 1 >= byaLocalBuffer.Length)
                return 0;

            return ((byaLocalBuffer[iLocalStartIndex] & 0x0F) << 8) | byaLocalBuffer[iLocalStartIndex + 1];
        }

        /// <summary>
        /// byte[].ToByteAsInteger()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <returns></returns>
        public static int ToByteAsInteger(this byte[]? byaLocalBuffer)
        {
            lCurrentLogger.Trace("Global.ToByteAsInteger()".Pastel(ConsoleColor.Cyan));

            return ToByteAsInteger(byaLocalBuffer, 0);
        }

        /// <summary>
        /// byte[].ToByteAsInteger()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <param name="iLocalStartIndex"></param>
        /// <returns></returns>
        public static int ToByteAsInteger(this byte[]? byaLocalBuffer, int iLocalStartIndex)
        {
            lCurrentLogger.Trace("Global.ToByteAsInteger()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer == null || iLocalStartIndex >= byaLocalBuffer.Length)
                return 0;

            return byaLocalBuffer[iLocalStartIndex];
        }

        /// <summary>
        /// byte[].ToBCDAsInteger()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <param name="iLocalLength"></param>
        /// <returns></returns>
        public static int ToBCDAsInteger(this byte[]? byaLocalBuffer, int iLocalLength)
        {
            lCurrentLogger.Trace("Global.ToBCDAsInteger()".Pastel(ConsoleColor.Cyan));

            return ToBCDAsInteger(byaLocalBuffer, 0, iLocalLength);
        }

        /// <summary>
        /// byte[].ToBCDAsInteger()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <param name="iLocalStartIndex"></param>
        /// <param name="iLocalLength"></param>
        /// <returns></returns>
        public static int ToBCDAsInteger(this byte[]? byaLocalBuffer, int iLocalStartIndex, int iLocalLength)
        {
            lCurrentLogger.Trace("Global.ToBCDAsInteger()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer == null || iLocalStartIndex >= byaLocalBuffer.Length)
                return 0;

            var iCurrentValue = 0;
            int iLimit = iLocalLength / 2;

            for (var i = 0; i < iLimit; i++)
            {
                byte currentByte = byaLocalBuffer[iLocalStartIndex + i];
                iCurrentValue = iCurrentValue * 100 + (currentByte >> 4) * 10 + (currentByte & 0x0F);
            }

            if ((iLocalLength & 1) != 0)
            {
                int lastByteIndex = iLocalStartIndex + (iLocalLength / 2);

                if (lastByteIndex < byaLocalBuffer.Length)
                    iCurrentValue = iCurrentValue * 10 + (byaLocalBuffer[lastByteIndex] >> 4);
            }

            return iCurrentValue;
        }
    }
}