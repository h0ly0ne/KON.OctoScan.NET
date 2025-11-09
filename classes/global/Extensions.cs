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
            lCurrentLogger.Trace("Global.CleanupString()".Pastel(ConsoleColor.Cyan));

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
            return string.IsNullOrEmpty(strLocalString) ? string.Empty : BitConverter.ToString(Encoding.Default.GetBytes(strLocalString)).Replace("-", string.Empty);
        }
    }
}