using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;

using NetFwTypeLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Program;

namespace KON.OctoScan.NET
{
    public static class Global
    {
        public static readonly Logger lCurrentLogger = LogManager.GetCurrentClassLogger();

        public static Options? oCurrentOptions;

        public static bool bDone = false;
        public static int iEITSize = 0;
        public static int iEITServices = 0;
        public static int iEITSections = 0;
        public static int iEITEvents = 0;
        public static int iEITShortSize = 0;
        public static int iEITEventsDeleted = 0;

        public static long CurrentTimestamp()
        {
            lCurrentLogger.Trace("Global.CurrentTimestamp()".Pastel(ConsoleColor.Cyan));

            return DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public static bool CreateFirewallRule()
        {
            lCurrentLogger.Trace("Global.CreateFirewallRule()".Pastel(ConsoleColor.Cyan));

            if (OperatingSystem.IsWindows())
            {
                var tCurrentFwPolicyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", false);
                var tCurrentFwRuleType = Type.GetTypeFromProgID("HNetCfg.FWRule", false);

                if (tCurrentFwPolicyType != null && tCurrentFwRuleType != null)
                {
                    var oCurrentFwPolicyObject = Activator.CreateInstance(tCurrentFwPolicyType);
                    var oCurrentFwRuleObject = Activator.CreateInstance(tCurrentFwRuleType);

                    if (oCurrentFwPolicyObject != null && oCurrentFwRuleObject != null)
                    {
                        var infp2CurrentINetFwPolicy2 = (INetFwPolicy2)oCurrentFwPolicyObject;
                        var infrCurrentINetFwRules = infp2CurrentINetFwPolicy2.Rules;

                        if (infrCurrentINetFwRules != null)
                        {
                            try
                            {
                                infrCurrentINetFwRules.Remove(FIREWALL_RULE_APP_NAME + "_OUT_TCP");
                                infrCurrentINetFwRules.Remove(FIREWALL_RULE_APP_NAME + "_OUT_UDP");
                                infrCurrentINetFwRules.Remove(FIREWALL_RULE_APP_NAME + "_IN_TCP");
                                infrCurrentINetFwRules.Remove(FIREWALL_RULE_APP_NAME + "_IN_UDP");

                                var infrCurrentINetFwRule = (INetFwRule)Activator.CreateInstance(tCurrentFwRuleType)!;
                                infrCurrentINetFwRule.Name = FIREWALL_RULE_APP_NAME + "_OUT_TCP";
                                infrCurrentINetFwRule.Description = FIREWALL_RULE_APP_NAME + "_OUT_TCP";
                                infrCurrentINetFwRule.Protocol = 6;
                                infrCurrentINetFwRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                                infrCurrentINetFwRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                                infrCurrentINetFwRule.ApplicationName = Environment.ProcessPath;
                                infrCurrentINetFwRule.Enabled = true;
                                infrCurrentINetFwRule.InterfaceTypes = "All";
                                infrCurrentINetFwRules.Add(infrCurrentINetFwRule);

                                var infrCurrentINetFwRule2 = (INetFwRule)Activator.CreateInstance(tCurrentFwRuleType)!;
                                infrCurrentINetFwRule2.Name = FIREWALL_RULE_APP_NAME + "_IN_TCP";
                                infrCurrentINetFwRule2.Description = FIREWALL_RULE_APP_NAME + "_IN_TCP";
                                infrCurrentINetFwRule2.Protocol = 6;
                                infrCurrentINetFwRule2.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                                infrCurrentINetFwRule2.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                                infrCurrentINetFwRule2.ApplicationName = Environment.ProcessPath;
                                infrCurrentINetFwRule2.Enabled = true;
                                infrCurrentINetFwRule2.InterfaceTypes = "All";
                                infrCurrentINetFwRules.Add(infrCurrentINetFwRule2);

                                var infrCurrentINetFwRule3 = (INetFwRule)Activator.CreateInstance(tCurrentFwRuleType)!;
                                infrCurrentINetFwRule3.Name = FIREWALL_RULE_APP_NAME + "_OUT_UDP";
                                infrCurrentINetFwRule3.Description = FIREWALL_RULE_APP_NAME + "_OUT_UDP";
                                infrCurrentINetFwRule3.Protocol = 17;
                                infrCurrentINetFwRule3.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                                infrCurrentINetFwRule3.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                                infrCurrentINetFwRule3.ApplicationName = Environment.ProcessPath;
                                infrCurrentINetFwRule3.Enabled = true;
                                infrCurrentINetFwRule3.InterfaceTypes = "All";
                                infrCurrentINetFwRules.Add(infrCurrentINetFwRule3);

                                var infrCurrentINetFwRule4 = (INetFwRule)Activator.CreateInstance(tCurrentFwRuleType)!;
                                infrCurrentINetFwRule4.Name = FIREWALL_RULE_APP_NAME + "_IN_UDP";
                                infrCurrentINetFwRule4.Description = FIREWALL_RULE_APP_NAME + "_IN_UDP";
                                infrCurrentINetFwRule4.Protocol = 17;
                                infrCurrentINetFwRule4.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                                infrCurrentINetFwRule4.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                                infrCurrentINetFwRule4.ApplicationName = Environment.ProcessPath;
                                infrCurrentINetFwRule4.Enabled = true;
                                infrCurrentINetFwRule4.InterfaceTypes = "All";
                                infrCurrentINetFwRules.Add(infrCurrentINetFwRule4);

                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static uint GetCRC32(byte[] byaLocalBuffer, int iLocalChecksumLength)
        {
            lCurrentLogger.Trace("Global.GetCRC32()".Pastel(ConsoleColor.Cyan));

            var uiCurrentCRCValue = 0xFFFFFFFF;

            for (var iCurrentItterator = 0; iCurrentItterator < iLocalChecksumLength; iCurrentItterator++)
                uiCurrentCRCValue = (uiCurrentCRCValue << 8) ^ uiaDVBCRCChecksumTable[((uiCurrentCRCValue >> 24) ^ byaLocalBuffer[iCurrentItterator]) & 0xFF];

            return uiCurrentCRCValue;
        }

        public static int GetBCD(byte[]? byaLocalBuffer, int iLocalLength)
        {
            lCurrentLogger.Trace("Global.GetBCD()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer == null) 
                return 0;
            
            var iCurrentValue = 0;

            for (var i = 0; i < iLocalLength / 2; i++)
            {
                iCurrentValue = iCurrentValue * 100 + (byaLocalBuffer[i] >> 4) * 10 + (byaLocalBuffer[i] & 0x0F);
            }

            if ((iLocalLength & 1) != 0)
                iCurrentValue = iCurrentValue * 10 + (byaLocalBuffer[iLocalLength / 2 - 1] >> 4);

            return iCurrentValue;
        }

        public static int ToUShortAsInteger(byte[]? byaLocalBuffer)
        {
            lCurrentLogger.Trace("Global.ToUShortAsInteger()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer != null)
                return (byaLocalBuffer[0] << 8) | byaLocalBuffer[1];

            return 0;
        }

        public static int ToByteAsInteger(byte[]? byaLocalBuffer)
        {
            lCurrentLogger.Trace("Global.ToByteAsInteger()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer != null)
                return ((byaLocalBuffer[0] & 0x0F) << 8) | byaLocalBuffer[1];

            return 0;
        }

        public static int GetPID(byte[]? byaLocalBuffer)
        {
            lCurrentLogger.Trace("Global.GetPID()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer != null) 
                return ((byaLocalBuffer[0] & 0x1F) << 8) | (byaLocalBuffer[1] & 0xFF);

            return 0;
        }

        public static void GetDateFromModifiedJulianDate(int iLocalModifiedJulianDate, ref int y, ref int m, ref int d)
        {
            lCurrentLogger.Trace("Global.GetDateFromModifiedJulianDate()".Pastel(ConsoleColor.Cyan));

            // Valid till 28.2.2100
            int[] dm = [ 31, 30, 31, 30, 31, 31, 30, 31, 30, 31, 31, 28 ];
            var r = iLocalModifiedJulianDate - 51604; // 0 = 1.3.2000

            if (r < 0)
                r += 65536;

            var p = r / (365 * 4 + 1);

            r -= p * (365 * 4 + 1);
            y = p * 4 + 2000;
            p = r / 365;
            r -= p * 365;
            y += p;

            if (p < 4)
            {
                for (var i = 0; i < 12; i += 1)
                {
                    m = i < 10 ? i + 3 : i - 9;

                    if (r < dm[i])
                    {
                        d = r + 1;

                        if (i >= 10)
                            y += 1;

                        break;
                    }

                    r -= dm[i];
                }
            }
            else
            {
                m = 2;
                d = 29;
            }
        }

        public static int SendData(Socket? nsLocalSocket, byte[] baLocalBuffer, int iDataLength)
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

        //public static void Dump(byte[] b, int l)
        //{
        //    lCurrentLogger.Trace("Global.Dump()".Pastel(ConsoleColor.Cyan));

        //    for (var j = 0; j < l; j += 16)
        //    {
        //        var strCurrentLine = string.Empty;

        //        for (var i = 0; i < 16; i++)
        //            strCurrentLine += i + j < l ? $"{b[j + i]:X2} " : "   ";

        //        strCurrentLine += " | ";

        //        for (var i = 0; i < 16; i++)
        //            if (i + j < l)
        //                strCurrentLine += b[j + i] > 31 && b[j + i] < 127 ? (char)b[j + i] : '.';

        //        lCurrentLogger.Info(strCurrentLine.Pastel(ConsoleColor.Magenta));
        //    }
        //}

        public static int GetTransportStreamPayload(byte[] byaLocalTransponder)
        {
            lCurrentLogger.Trace("Global.GetTransportStreamPayload()".Pastel(ConsoleColor.Cyan));

            if ((byaLocalTransponder[3] & 0x10) == 0)
                return 0;

            if ((byaLocalTransponder[3] & 0x20) == 0)
                return 184;

            if (byaLocalTransponder[4] > 183)
                return 0;

            return 183 - byaLocalTransponder[4];
        }

        public static bool HasTransportStreamPayload(byte[] byaLocalTransponder)
        {
            lCurrentLogger.Trace("Global.HasTransportStreamPayload()".Pastel(ConsoleColor.Cyan));

            if ((byaLocalTransponder[3] & 0x10) == 0)
                return false;

            if ((byaLocalTransponder[3] & 0x20) == 0)
                return true;

            return byaLocalTransponder[4] <= 183;
        }

        //public static int GetTransportStreamPayloadStart(byte[] byaLocalTransponder)
        //{
        //    lCurrentLogger.Trace("Global.GetTransportStreamPayloadStart()".Pastel(ConsoleColor.Cyan));

        //    if ((byaLocalTransponder[3] & 0x10) == 0)
        //        return 188;

        //    if ((byaLocalTransponder[3] & 0x20) == 0)
        //        return 4;

        //    if (byaLocalTransponder[4] >= 184)
        //        return 188;
                
        //    return byaLocalTransponder[4] + 5;
        //}

        //public static int GetTransportStreamID(byte[]? byaLocalBuffer)
        //{
        //    lCurrentLogger.Trace("Global.GetTransportStreamID()".Pastel(ConsoleColor.Cyan));

        //    if (byaLocalBuffer != null)
        //        return ((byaLocalBuffer[1] & 0x1F) << 8) + byaLocalBuffer[2];

        //    return 0;
        //}

        public static int SectorLength(byte[] baLocalBuffer)
        {
            lCurrentLogger.Trace("Global.SectorLength()".Pastel(ConsoleColor.Cyan));

            return 3 + ((baLocalBuffer[1] & 0x0F) << 8) + baLocalBuffer[2];
        }

        public static bool AllZeroOrCounted(uint[] lLocalElement, int iLocalElementCount)
        {
            lCurrentLogger.Trace("Global.AllZeroOrCounted()".Pastel(ConsoleColor.Cyan));

            uint uiCurrentElementOrValue = 0;

            for (var i = 0; i < iLocalElementCount; i++)
            {
                uiCurrentElementOrValue |= lLocalElement[i];
            }

            return uiCurrentElementOrValue == 0;
        }

        public static bool HasDescription(byte byLocalSearchTerm, byte[] byaLocalSearchCollection, int iLocalSearchCollectionLength)
        {
            lCurrentLogger.Trace("Global.HasDescription()".Pastel(ConsoleColor.Cyan));

            for (var i = 0; i < iLocalSearchCollectionLength; i += byaLocalSearchCollection[i + 1] + 2)
            {
                if (byLocalSearchTerm == byaLocalSearchCollection[i])
                    return true;
            }

            return false;
        }

        public static string ConvertEN300468StringToUTF8(this string strLocalDestination, byte[] byLocalSourceBuffer, int iLocalSourceBufferLength)
        {
            lCurrentLogger.Trace("Global.ConvertEN300468StringToUTF8()".Pastel(ConsoleColor.Cyan));

            var iCurrentDestinationLength = strLocalDestination.Length;
            var laCurrentEN300468Codetable = uiaDVBEN300468Codetable_00;

            var encoding = 0;
            var skip = 0;

            if (byLocalSourceBuffer[0] < 0x20)
            {
                encoding = byLocalSourceBuffer[0];
                if (encoding < 12)
                    laCurrentEN300468Codetable = uiaaDVBEN300468Codetable[encoding];
                skip = 1;
            }

            if (encoding == 0x1F)
                skip = 2;

            if (encoding == 0x10)
            {
                if (byLocalSourceBuffer[1] == 0)
                {
                    laCurrentEN300468Codetable = uiaDVBEN300468Codetable_0B;
                    if (byLocalSourceBuffer[1] == 0 && byLocalSourceBuffer[2] >= 5 && byLocalSourceBuffer[2] <= 15)
                    {
                        laCurrentEN300468Codetable = uiaaDVBEN300468Codetable[byLocalSourceBuffer[2] - 4];
                    }
                }
                skip = 3;
            }

            int dest_pos = 0;
            int i;

            for (i = skip; i < iLocalSourceBufferLength && dest_pos < iCurrentDestinationLength - 1; i++)
            {
                uint utf8_ContinuityCounter = byLocalSourceBuffer[i];
                if (utf8_ContinuityCounter >= 128 && encoding == 0x15)
                {
                    if ((utf8_ContinuityCounter & 0xC0) == 0xC0)
                    {
                        i++;
                        utf8_ContinuityCounter &= 0x3F;
                        if ((utf8_ContinuityCounter & 0x20) != 0) utf8_ContinuityCounter &= 0x1F;
                        if ((utf8_ContinuityCounter & 0x10) != 0) utf8_ContinuityCounter &= 0x0F;
                        if ((utf8_ContinuityCounter & 0x08) != 0) utf8_ContinuityCounter &= 0x07;
                        while (i < iLocalSourceBufferLength && (byLocalSourceBuffer[i] & 0xC0) == 0x80)
                        {
                            utf8_ContinuityCounter = (utf8_ContinuityCounter << 6) | (uint)(byLocalSourceBuffer[i] & 0x3F);
                            i++;
                        }
                        if (utf8_ContinuityCounter > 0x10FFFF)
                            break; // UTF-8 error
                    }
                    else
                        break; // UTF-8 error
                }
                else
                {
                    if (encoding <= 0x10 && utf8_ContinuityCounter is >= 0x80 and < 0xA0)
                        utf8_ContinuityCounter |= 0x0100;
                    else if (utf8_ContinuityCounter >= 0xA0)
                    {
                        utf8_ContinuityCounter = laCurrentEN300468Codetable[utf8_ContinuityCounter - 0xA0];
                    }
                }

                if (utf8_ContinuityCounter < 0xA0)
                {
                    switch (utf8_ContinuityCounter)
                    {
                        case 0x00:
                        {
                            break;
                        }
                        case uint and >= 0x01 and <= 0x7F:
                        {
                            strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)utf8_ContinuityCounter));
                            break;
                        }
                        case CHARACTER_CR_LF:
                        {
                            strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)'\n'));
                            break;
                        }
                        default:
                        {
                            if (dest_pos < iCurrentDestinationLength - 2)
                            {
                                strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar(0xC2));
                                strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)((utf8_ContinuityCounter & 0x3F) | 0x80)));
                            }
                            break;
                        }
                    }
                }
                else
                {
                    if (utf8_ContinuityCounter < 0x800)
                    {
                        if (dest_pos >= iCurrentDestinationLength - 2)
                            break;

                        strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)(0xC0 | ((utf8_ContinuityCounter >> 6) & 0x1F))));
                        strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)((utf8_ContinuityCounter & 0x3F) | 0x80)));
                    }
                    else if (utf8_ContinuityCounter < 0x10000)
                    {
                        if (dest_pos >= iCurrentDestinationLength - 3)
                            break;

                        strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)(0xE0 | ((utf8_ContinuityCounter >> 12) & 0x0F))));
                        strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)(((utf8_ContinuityCounter >> 6) & 0x3F) | 0x80)));
                        strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)((utf8_ContinuityCounter & 0x3F) | 0x80)));
                    }
                    else if (utf8_ContinuityCounter < 0x110000)
                    {
                        if (dest_pos >= iCurrentDestinationLength - 4)
                            break;

                        strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)(0xF0 | ((utf8_ContinuityCounter >> 18) & 0x07))));
                        strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)(((utf8_ContinuityCounter >> 12) & 0x3F) | 0x80)));
                        strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)(((utf8_ContinuityCounter >> 6) & 0x3F) | 0x80)));
                        strLocalDestination = strLocalDestination.ReplaceCharacter(dest_pos++, Convert.ToChar((byte)((utf8_ContinuityCounter & 0x3F) | 0x80)));
                    }
                }
            }

            return strLocalDestination.ReplaceCharacter(dest_pos, Convert.ToChar(0x00));
        }

        public static string ReplaceCharacter(this string str, int index, char replace)
        {
            lCurrentLogger.Trace("Global.ReplaceCharacter()".Pastel(ConsoleColor.Cyan));

            return str.Remove(index, Math.Min(1, str.Length - index)).Insert(index, replace.ToString());
        }

        public static string CleanupString(this string strLocalString)
        {
            lCurrentLogger.Trace("Global.CleanupString()".Pastel(ConsoleColor.Cyan));

            string strReturnString = strLocalString;

            strReturnString = Regex.Replace(strReturnString, @"[\x00-\x1F]", string.Empty);
            strReturnString = Regex.Replace(strReturnString, "\0", string.Empty);

            return strReturnString;
        }

        public static byte GetLSB(this byte byLocalByteValue)
        {
            lCurrentLogger.Trace("Global.GetLSB()".Pastel(ConsoleColor.Cyan));

            return (byte)(byLocalByteValue & 0x0F);
        }

        //public static string ToBitString(this BitArray bits)
        //{
        //    var sb = new StringBuilder();

        //    for (int i = 0; i < bits.Count; i++)
        //    {
        //        char c = bits[i] ? '1' : '0';
        //        sb.Append(c);
        //    }

        //    return sb.ToString();
        //}

        //public static byte[] GetBytes(this string bitString)
        //{
        //    return Enumerable.Range(0, bitString.Length / 8).
        //        Select(pos => Convert.ToByte(
        //            bitString.Substring(pos * 8, 8),
        //            2)
        //        ).ToArray();
        //}
    }
    public class IgnorePropertiesResolver(IEnumerable<string> propNamesToIgnore) : DefaultContractResolver
    {
        private readonly HashSet<string?> ignoreProps = [..propNamesToIgnore];

        protected override JsonProperty CreateProperty(MemberInfo miLocalMemberInfo, MemberSerialization msLocalMemberSerialization)
        {
            JsonProperty jpCurrentJsonProperty = base.CreateProperty(miLocalMemberInfo, msLocalMemberSerialization);
            if (ignoreProps.Contains(jpCurrentJsonProperty.PropertyName))
                jpCurrentJsonProperty.ShouldSerialize = _ => false;
            
            return jpCurrentJsonProperty;
        }
    }
}