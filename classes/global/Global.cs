using System.Collections;
using System.Data;
using System.Reflection;

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
        public static int iServices = 0;
        public static int iEITSize = 0;
        public static int iEITServices = 0;
        public static int iEITSections = 0;
        public static int iEITEvents = 0;
        public static int iEITShortSize = 0;
        public static int iEITEventsDeleted = 0;

        public static DataTable dtExportServices = new();
        public static DataTable dtExportEvents = new();

        /// <summary>
        /// Global.CurrentTimestamp()
        /// </summary>
        /// <returns></returns>
        public static long CurrentTimestamp()
        {
            lCurrentLogger.Trace("Global.CurrentTimestamp()".Pastel(ConsoleColor.Cyan));

            return DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Global.CreateFirewallRule()
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Global.GetBytesAsInt32()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <param name="bLocalReverse"></param>
        /// <param name="iLocalStartBit"></param>
        /// <param name="iLocalBitAmount"></param>
        /// <returns></returns>
        public static int GetBytesAsInt32(byte[]? byaLocalBuffer, bool bLocalReverse, int? iLocalStartBit, int? iLocalBitAmount)
        {
            lCurrentLogger.Trace("Global.GetBytesAsInt32()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer == null)
                return 0;

            var baCurrentBitArray = bLocalReverse ? new BitArray(byaLocalBuffer.Reverse().ToArray()) : new BitArray(byaLocalBuffer);
            return baCurrentBitArray.GetBitsAsInt32(iLocalStartBit, iLocalBitAmount);
        }

        /// <summary>
        /// Global.GetBytesAsInt64()
        /// </summary>
        /// <param name="byaLocalBuffer"></param>
        /// <param name="bLocalReverse"></param>
        /// <param name="iLocalStartBit"></param>
        /// <param name="iLocalBitAmount"></param>
        /// <returns></returns>
        public static long GetBytesAsInt64(byte[]? byaLocalBuffer, bool bLocalReverse, int? iLocalStartBit, int? iLocalBitAmount)
        {
            lCurrentLogger.Trace("Global.GetBytesAsInt64()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer == null)
                return 0;

            var baCurrentBitArray = bLocalReverse ? new BitArray(byaLocalBuffer.Reverse().ToArray()) : new BitArray(byaLocalBuffer);
            return baCurrentBitArray.GetBitsAsInt64(iLocalStartBit, iLocalBitAmount);
        }
        
        public static int ToByteAsInteger(byte[]? byaLocalBuffer)
        {
            lCurrentLogger.Trace("Global.ToByteAsInteger()".Pastel(ConsoleColor.Cyan));

            if (byaLocalBuffer != null)
                return ((byaLocalBuffer[0] & 0x0F) << 8) | byaLocalBuffer[1];

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

        //public static bool HasDescriptor(byte byLocalSearchTerm, byte[] byaLocalSearchCollection, int iLocalSearchCollectionLength)
        //{
        //    lCurrentLogger.Trace("Global.HasDescriptor()".Pastel(ConsoleColor.Cyan));

        //    for (var i = 0; i < iLocalSearchCollectionLength; i += byaLocalSearchCollection[i + 1] + 2)
        //    {
        //        if (byLocalSearchTerm == byaLocalSearchCollection[i])
        //            return true;
        //    }

        //    return false;
        //}

        //public static bool HasESDescriptor(ElementaryStreamDescriptors esdLocalSearchESDescriptor, byte[] byaLocalSearchCollection)
        //{
        //    lCurrentLogger.Trace("Global.HasESDescriptor()".Pastel(ConsoleColor.Cyan));

        //    if (byaLocalSearchCollection.Length <= 0)
        //        return false;

        //    var iCurrentESDescriptorBufferPosition = 0;
        //    do
        //    {
        //        var iESDescriptorTag = GetBytesAsInt32(byaLocalSearchCollection[iCurrentESDescriptorBufferPosition..(iCurrentESDescriptorBufferPosition + 1)], true, 0, 8);
        //        var iESDescriptorLength = GetBytesAsInt32(byaLocalSearchCollection[(iCurrentESDescriptorBufferPosition + 1)..(iCurrentESDescriptorBufferPosition + 2)], true, 0, 8);

        //        if (iESDescriptorLength != 0 && (ElementaryStreamDescriptors)iESDescriptorTag == esdLocalSearchESDescriptor)
        //            return true;

        //        iCurrentESDescriptorBufferPosition += 2 + iESDescriptorLength;
        //    }
        //    while (iCurrentESDescriptorBufferPosition < byaLocalSearchCollection.Length);

        //    return false;
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