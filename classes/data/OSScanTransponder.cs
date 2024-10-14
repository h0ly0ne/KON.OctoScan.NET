using System.Net.Sockets;
using System.Net;
using System.Globalization;
using System.Text;

using Pastel;
using Polly;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class OSScanTransponder
    {
        public OSScanIP? osiOSScanIP;
        public OSTransponderInfo? otiOSTransponderInfo;
        public OSTransportStreamInfo? otsiOSTransportStreamInfo = new();
        public OSSatIPConnection? osicOSSatIPConnection = new();

        public long lTimeout;
        public long lTimestamp;
        public bool bTimedOut;
        public long lRetries;
    }

    public static class OSScanTransponder_Extension
    {
        public static bool Scan(this OSScanTransponder? ostLocalOSScanTransponder, long lLocalTimeout = 600)
        {
            lCurrentLogger.Trace("OSScanTransponder.Scan()".Pastel(ConsoleColor.Cyan));

            if (ostLocalOSScanTransponder?.osiOSScanIP == null)
                return false;
            
            if (ostLocalOSScanTransponder.osicOSSatIPConnection == null)
                return false;

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;

            osicCurrentOSSatIPConnection.iSequence = 0;
            osicCurrentOSSatIPConnection.iNSPort = 0;

            ostLocalOSScanTransponder.lTimestamp = CurrentTimestamp();
            ostLocalOSScanTransponder.lTimeout = lLocalTimeout;

            ostLocalOSScanTransponder.InitSocketUDP();
            if (osicCurrentOSSatIPConnection.nsSocketUDP is not { IsBound: true })
            {
                lCurrentLogger.Error("Error: Could not bind UDP socket".Pastel(ConsoleColor.Red));
                return false;
            }

            ostLocalOSScanTransponder.InitSocketTCP();
            if (osicCurrentOSSatIPConnection.nsSocketTCP is not { Connected: true })
            {
                lCurrentLogger.Error("Error: Could not bind TCP socket".Pastel(ConsoleColor.Red));
                return false;
            }

            ostLocalOSScanTransponder.RTSPSendSetup(false);
            if (!ostLocalOSScanTransponder.RTSPCheckOK())
                return false;

            var bUpdatePIDSuccessful = false;
            var cCurrentContext = new Context { { "Retries", 0 } };
            Policy.HandleResult<bool>(r => r != true).WaitAndRetry(10, _ => TimeSpan.FromSeconds(1), onRetry: (_, _, retryCount, context) => { context["Retries"] = retryCount; }).Execute(delegate { ostLocalOSScanTransponder.UpdatePIDs(); return bUpdatePIDSuccessful = ostLocalOSScanTransponder.RTSPCheckOK(); }, cCurrentContext);
            ostLocalOSScanTransponder.lRetries = Convert.ToInt32(cCurrentContext["Retries"]);
            ostLocalOSScanTransponder.lTimeout += ostLocalOSScanTransponder.lRetries;

            if (!bUpdatePIDSuccessful)
                return false;

            ostLocalOSScanTransponder.AddSFilter(DEFAULT_PID_PAT, DEFAULT_TID_PA, 0, 0, 5);
            ostLocalOSScanTransponder.AddSFilter(DEFAULT_PID_SDT, DEFAULT_TID_SD, 0, 1, 5);

            if (ostLocalOSScanTransponder.otsiOSTransportStreamInfo == null)
                return false;

            Socket.Select(new List<Socket> { osicCurrentOSSatIPConnection.nsSocketTCP, osicCurrentOSSatIPConnection.nsSocketUDP }, null, null, 1000000);

            while (!bDone && !ostLocalOSScanTransponder.otsiOSTransportStreamInfo.bDone && ostLocalOSScanTransponder.lTimestamp + ostLocalOSScanTransponder.lTimeout >= CurrentTimestamp())
            {
                ostLocalOSScanTransponder.RTSPSendOptions();
                if (!ostLocalOSScanTransponder.RTSPCheckOK())
                    break;

                var byCurrentReceiveBufferUDP = new byte[10000];
                if (!osicCurrentOSSatIPConnection.nsSocketUDP.Poll(1000000, SelectMode.SelectRead))
                    continue;

                var iReceivedBytesUDP = osicCurrentOSSatIPConnection.nsSocketUDP.Receive(byCurrentReceiveBufferUDP);

                if (iReceivedBytesUDP <= 12) 
                    continue;

                ostLocalOSScanTransponder.ProcessTransponders(byCurrentReceiveBufferUDP[12..iReceivedBytesUDP], iReceivedBytesUDP - 12);
                ostLocalOSScanTransponder.lTimestamp = CurrentTimestamp();
            }

            if (!bDone && !ostLocalOSScanTransponder.otsiOSTransportStreamInfo.bDone && ostLocalOSScanTransponder.lTimestamp + ostLocalOSScanTransponder.lTimeout <= CurrentTimestamp())
                ostLocalOSScanTransponder.bTimedOut = true;
            else
                ostLocalOSScanTransponder.bTimedOut = false;

            if (ostLocalOSScanTransponder.otiOSTransponderInfo != null)
            {
                if (oCurrentOptions != null)
                {
                    if (ostLocalOSScanTransponder.otiOSTransponderInfo.bScanEventInformationTable && oCurrentOptions.cloPrintEvents)
                        ostLocalOSScanTransponder.otiOSTransponderInfo.PrintEvents();

                    if (ostLocalOSScanTransponder.otiOSTransponderInfo.bScanEventInformationTable && oCurrentOptions.cloExportEvents)
                        ostLocalOSScanTransponder.otiOSTransponderInfo.ExportEventsToJson();

                    if (oCurrentOptions.cloPrintServices)
                        ostLocalOSScanTransponder.otiOSTransponderInfo.PrintServices();

                    if (oCurrentOptions.cloExportServices)
                        ostLocalOSScanTransponder.otiOSTransponderInfo.ExportServicesToJson();
                }
            }

            ostLocalOSScanTransponder.RTSPSendTeardown();

            if (osicCurrentOSSatIPConnection.nsSocketTCP is { Connected: true })
                osicCurrentOSSatIPConnection.nsSocketTCP.Close();

            return true;
        }

        public static void InitSocketUDP(this OSScanTransponder? ostLocalOSScanTransponder)
        {
            lCurrentLogger.Trace("OSScanTransponder.InitSocketUDP()".Pastel(ConsoleColor.Cyan));

            bool bSocketBound;
            var nsSocketUDP = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

            if (ostLocalOSScanTransponder?.osicOSSatIPConnection == null)
                return;
                
            try
            {
                nsSocketUDP.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                nsSocketUDP.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                nsSocketUDP.ReceiveBufferSize = 10000;
                nsSocketUDP.Ttl = 5;

                nsSocketUDP.Bind(new IPEndPoint(IPAddress.IPv6Any, ostLocalOSScanTransponder.osicOSSatIPConnection.iNSPort));
                bSocketBound = true;

                if (nsSocketUDP.LocalEndPoint != null)
                    ostLocalOSScanTransponder.osicOSSatIPConnection.iNSPort = ((IPEndPoint)nsSocketUDP.LocalEndPoint).Port;
            }
            catch
            {
                ostLocalOSScanTransponder.osicOSSatIPConnection.iNSPort = 0;
                return;
            }

            if (!bSocketBound)
            {
                ostLocalOSScanTransponder.osicOSSatIPConnection.iNSPort = 0;
                return;
            }

            ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketUDP = nsSocketUDP;
        }

        public static void InitSocketTCP(this OSScanTransponder? ostLocalOSScanTransponder)
        {
            lCurrentLogger.Trace("OSScanTransponder.InitSocketTCP()".Pastel(ConsoleColor.Cyan));

            var bSocketConnected = false;
            var nsSocketTCP = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            if (ostLocalOSScanTransponder?.osicOSSatIPConnection == null || string.IsNullOrEmpty(ostLocalOSScanTransponder.osicOSSatIPConnection.strHost) )
                return;

            var hostEntry = Dns.GetHostEntry(ostLocalOSScanTransponder.osicOSSatIPConnection.strHost);

            foreach (var address in hostEntry.AddressList)
            {
                try
                {
                    nsSocketTCP.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                    nsSocketTCP.Connect(new IPEndPoint(address, ostLocalOSScanTransponder.osicOSSatIPConnection.iPort));
                    bSocketConnected = true;

                    break;
                }
                catch (SocketException sexcCurrentSocketException)
                {
                    lCurrentLogger.Error($"Error: {sexcCurrentSocketException.Message}".Pastel(ConsoleColor.Red));
                    return;
                }
            }

            if (!bSocketConnected)
                return;

            ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketTCP = nsSocketTCP;
        }

        public static void RTSPSendSetup(this OSScanTransponder? ostLocalOSScanTransponder, bool bLocalIsMulticast)
        {
            lCurrentLogger.Trace("OSScanTransponder.RTSPSendSetup()".Pastel(ConsoleColor.Cyan));

            if (ostLocalOSScanTransponder?.osicOSSatIPConnection == null) 
                return;
            
            var strCurrentMessage = $"SETUP rtsp://{ostLocalOSScanTransponder.osicOSSatIPConnection.strHost}:{ostLocalOSScanTransponder.osicOSSatIPConnection.iPort}/{ostLocalOSScanTransponder.osicOSSatIPConnection.strTune} " + RTSP_VERSION + Environment.NewLine +
                                    $"CSeq: {ostLocalOSScanTransponder.osicOSSatIPConnection.iSequence}" + Environment.NewLine +
                                    $"Transport: RTP/AVP;{(bLocalIsMulticast ? $"multicast;port={ostLocalOSScanTransponder.osicOSSatIPConnection.iNSPort}-{ostLocalOSScanTransponder.osicOSSatIPConnection.iNSPort + 1};ttl=5" : $"unicast;client_port={ostLocalOSScanTransponder.osicOSSatIPConnection.iNSPort}-{ostLocalOSScanTransponder.osicOSSatIPConnection.iNSPort + 1}")}" + Environment.NewLine + Environment.NewLine;

            ostLocalOSScanTransponder.osicOSSatIPConnection.iSequence++;

            if (string.IsNullOrEmpty(strCurrentMessage) || Encoding.ASCII.GetByteCount(strCurrentMessage) >= 256)
                return;

            SendData(ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketTCP, Encoding.ASCII.GetBytes(strCurrentMessage), Encoding.ASCII.GetByteCount(strCurrentMessage));
        }

        public static bool RTSPCheckOK(this OSScanTransponder? ostLocalOSScanTransponder)
        {
            lCurrentLogger.Trace("OSScanTransponder.RTSPCheckOK()".Pastel(ConsoleColor.Cyan));

            if (ostLocalOSScanTransponder?.osicOSSatIPConnection?.nsSocketTCP == null) 
                return false;
            
            var baCurrentReceiveBuffer = new byte[ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketTCP.ReceiveBufferSize];
            ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketTCP.Receive(baCurrentReceiveBuffer);
            var strCurrentResponse = Encoding.ASCII.GetString(baCurrentReceiveBuffer);

            if (!strCurrentResponse.StartsWith(RTSP_VERSION + " 200 OK" + Environment.NewLine, StringComparison.OrdinalIgnoreCase))
                return false;

            var saCurrentResponse = strCurrentResponse[17..].Split([Environment.NewLine], StringSplitOptions.None);
            foreach (var strCurrentResponseLine in saCurrentResponse)
            {
                if (string.IsNullOrWhiteSpace(strCurrentResponseLine))
                    continue;

                if (strCurrentResponseLine.StartsWith("Session:", StringComparison.OrdinalIgnoreCase))
                    ostLocalOSScanTransponder.osicOSSatIPConnection.iSessionID = int.Parse(strCurrentResponseLine.Substring(strCurrentResponseLine.IndexOf(':') + 1, strCurrentResponseLine[(strCurrentResponseLine.IndexOf(':') + 1)..].Trim().IndexOf(';') + 1).Trim(), CultureInfo.InvariantCulture);

                if (strCurrentResponseLine.StartsWith("Transport:", StringComparison.OrdinalIgnoreCase))
                {
                    var saCurrentResponseLineParts = strCurrentResponseLine[11..].Split(';');
                    foreach (var strCurrentResponseLinePart in saCurrentResponseLineParts)
                    {
                        var saCurrentResponseLinePartKeyValuePairs = strCurrentResponseLinePart.Split('=');
                        if (saCurrentResponseLinePartKeyValuePairs[0].Trim().Equals("server_port", StringComparison.OrdinalIgnoreCase))
                        {
                            //var saCurrentServerPorts = saCurrentResponseLinePartKeyValuePairs[1].Split('-');
                            //int iServerPortStart = uint.Parse(saCurrentServerPorts[0], CultureInfo.InvariantCulture);
                            //int iServerPortEnd = uint.Parse(saCurrentServerPorts[1], CultureInfo.InvariantCulture);
                        }
                    }
                }

                if (strCurrentResponseLine.StartsWith("com.ses.streamID:", StringComparison.OrdinalIgnoreCase))
                    ostLocalOSScanTransponder.osicOSSatIPConnection.iStreamID = int.Parse(strCurrentResponseLine[17..].Trim(), CultureInfo.InvariantCulture);
            }

            return true;
        }
        
        public static void RTSPSendTeardown(this OSScanTransponder? ostLocalOSScanTransponder)
        {
            lCurrentLogger.Trace("OSScanTransponder.RTSPSendTeardown()".Pastel(ConsoleColor.Cyan));

            if (ostLocalOSScanTransponder?.osicOSSatIPConnection == null)
                return;

            var strCurrentMessage = $"TEARDOWN rtsp://{ostLocalOSScanTransponder.osicOSSatIPConnection.strHost}:{ostLocalOSScanTransponder.osicOSSatIPConnection.iPort}/{ostLocalOSScanTransponder.osicOSSatIPConnection.iStreamID} " + RTSP_VERSION + Environment.NewLine +
                                    $"CSeq: {ostLocalOSScanTransponder.osicOSSatIPConnection.iSequence}" + Environment.NewLine +
                                    $"Session: {ostLocalOSScanTransponder.osicOSSatIPConnection.iSessionID}" + Environment.NewLine + Environment.NewLine;

            ostLocalOSScanTransponder.osicOSSatIPConnection.iSequence++;

            if (string.IsNullOrEmpty(strCurrentMessage) || Encoding.ASCII.GetByteCount(strCurrentMessage) >= 1024)
                return;

            SendData(ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketTCP, Encoding.ASCII.GetBytes(strCurrentMessage), Encoding.ASCII.GetByteCount(strCurrentMessage));
        }

        public static bool RTSPSendPlay(this OSScanTransponder? ostLocalOSScanTransponder, string strLocalPIDs)
        {
            lCurrentLogger.Trace("OSScanTransponder.RTSPSendPlay()".Pastel(ConsoleColor.Cyan));

            if (ostLocalOSScanTransponder?.osicOSSatIPConnection == null)
                return false;

            var strCurrentMessage = $"PLAY rtsp://{ostLocalOSScanTransponder.osicOSSatIPConnection.strHost}:{ostLocalOSScanTransponder.osicOSSatIPConnection.iPort}/stream={ostLocalOSScanTransponder.osicOSSatIPConnection.iStreamID}" + (!string.IsNullOrEmpty(strLocalPIDs) ? $"?{ostLocalOSScanTransponder.osicOSSatIPConnection.strTune}&pids{strLocalPIDs}" : string.Empty) + " " + RTSP_VERSION + Environment.NewLine +
                                       $"CSeq: {ostLocalOSScanTransponder.osicOSSatIPConnection.iSequence}" + Environment.NewLine +
                                       $"Session: {ostLocalOSScanTransponder.osicOSSatIPConnection.iSessionID}" + Environment.NewLine + Environment.NewLine;

            ostLocalOSScanTransponder.osicOSSatIPConnection.iSequence++;

            if (ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketTCP == null || string.IsNullOrEmpty(strCurrentMessage))
                return false;

            SendData(ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketTCP, Encoding.ASCII.GetBytes(strCurrentMessage), Encoding.ASCII.GetByteCount(strCurrentMessage));
            Thread.Sleep(WAITTIME_FOR_RTSPCOMMAND);

            return true;
        }

        public static void RTSPSendOptions(this OSScanTransponder? ostLocalOSScanTransponder)
        {
            lCurrentLogger.Trace("OSScanTransponder.RTSPSendOptions()".Pastel(ConsoleColor.Cyan));

            if (ostLocalOSScanTransponder?.osicOSSatIPConnection == null)
                return;

            var strCurrentMessage = $"OPTIONS rtsp://{ostLocalOSScanTransponder.osicOSSatIPConnection.strHost}:{ostLocalOSScanTransponder.osicOSSatIPConnection.iPort}/{ostLocalOSScanTransponder.osicOSSatIPConnection.strTune} " + RTSP_VERSION + Environment.NewLine +
                                    $"CSeq: {ostLocalOSScanTransponder.osicOSSatIPConnection.iSequence}" + Environment.NewLine +
                                    $"Session: {ostLocalOSScanTransponder.osicOSSatIPConnection.iSessionID}" + Environment.NewLine + Environment.NewLine;

            ostLocalOSScanTransponder.osicOSSatIPConnection.iSequence++;

            if (string.IsNullOrEmpty(strCurrentMessage) || Encoding.ASCII.GetByteCount(strCurrentMessage) >= 256)
                return;

            SendData(ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketTCP, Encoding.ASCII.GetBytes(strCurrentMessage), Encoding.ASCII.GetByteCount(strCurrentMessage));
        }

        public static bool UpdatePIDs(this OSScanTransponder? ostLocalOSScanTransponder)
        {
            lCurrentLogger.Trace("OSScanTransponder.UpdatePIDs()".Pastel(ConsoleColor.Cyan));

            if (ostLocalOSScanTransponder?.osicOSSatIPConnection == null)
                return false;

            StringBuilder sbPIDs = new();
            sbPIDs.Append('=');

            for (var pid = 0; pid < 8192; pid++)
            {
                if (ostLocalOSScanTransponder?.otsiOSTransportStreamInfo?.opiiOSPacketIdentifierInfo?[pid] == null)
                    continue;

                if (!ostLocalOSScanTransponder.otsiOSTransportStreamInfo.opiiOSPacketIdentifierInfo[pid].bUsed)
                    continue;

                if (sbPIDs.Length >= 300)
                    break;

                if (sbPIDs.Length > 1)
                    sbPIDs.Append($",{pid}");
                else
                    sbPIDs.Append($"{pid}");
            }

            if (sbPIDs.Length <= 1)
                sbPIDs.Append("none");

            return ostLocalOSScanTransponder.RTSPSendPlay(sbPIDs.ToString());
        }

        public static void AddPID(this OSScanTransponder? ostLocalOSScanTransponder, int iLocalPID, int iLocalUseTableExtension)
        {
            lCurrentLogger.Trace("OSScanTransponder.AddPID()".Pastel(ConsoleColor.Cyan));

            if (ostLocalOSScanTransponder?.otsiOSTransportStreamInfo?.opiiOSPacketIdentifierInfo?[iLocalPID] == null)
                return;

            if (!ostLocalOSScanTransponder.otsiOSTransportStreamInfo.opiiOSPacketIdentifierInfo[iLocalPID].bUsed)
            {
                ostLocalOSScanTransponder.otsiOSTransportStreamInfo.opiiOSPacketIdentifierInfo[iLocalPID].Init(iLocalPID, ostLocalOSScanTransponder.otsiOSTransportStreamInfo);
                ostLocalOSScanTransponder.otsiOSTransportStreamInfo.opiiOSPacketIdentifierInfo[iLocalPID].bUsed = true;
                ostLocalOSScanTransponder.UpdatePIDs();
                ostLocalOSScanTransponder.otsiOSTransportStreamInfo.ostOSScanTransponder = ostLocalOSScanTransponder;
                ostLocalOSScanTransponder.otsiOSTransportStreamInfo.olopiiOSListOSPacketIdentifierInfo?.AddLast(ostLocalOSScanTransponder.otsiOSTransportStreamInfo.opiiOSPacketIdentifierInfo[iLocalPID]);
            }

            ostLocalOSScanTransponder.otsiOSTransportStreamInfo.opiiOSPacketIdentifierInfo[iLocalPID].iUseTableExtension = iLocalUseTableExtension;
        }

        public static void AddSFilter(this OSScanTransponder? ostLocalOSScanTransponder, int iLocalPID, byte byLocalTableID, int iLocalTableExtension, int iLocalUseTableExtension, long lLocalTimeout)
        {
            lCurrentLogger.Trace("OSScanTransponder.AddSFilter()".Pastel(ConsoleColor.Cyan));

            ostLocalOSScanTransponder.AddPID(iLocalPID, iLocalUseTableExtension);
            var olotsfOsListOsTransportStreamFilter = ostLocalOSScanTransponder?.otsiOSTransportStreamInfo?.opiiOSPacketIdentifierInfo?[iLocalPID].olotsfOSListOSTransportStreamFilter;
            if (olotsfOsListOsTransportStreamFilter != null && olotsfOsListOsTransportStreamFilter.Any(tsfLocalTransportStreamFilter => tsfLocalTransportStreamFilter.byTableID == byLocalTableID && tsfLocalTransportStreamFilter.iTableExtension == iLocalTableExtension))
                return;

            OSTransportStreamFilter tsfCurrentTransportStreamFilter = new()
            {
                opiiOSPacketIdentifierInfo = ostLocalOSScanTransponder?.otsiOSTransportStreamInfo?.opiiOSPacketIdentifierInfo?[iLocalPID],
                byTableID = byLocalTableID,
                iTableExtension = iLocalTableExtension,
                iUseTableExtension = iLocalUseTableExtension,
                byVersionNumber = 0xFF,
                lTimeout = lLocalTimeout,
                lTimestamp = CurrentTimestamp()
            };

            ostLocalOSScanTransponder?.otsiOSTransportStreamInfo?.opiiOSPacketIdentifierInfo?[iLocalPID].olotsfOSListOSTransportStreamFilter?.AddLast(tsfCurrentTransportStreamFilter);
            ostLocalOSScanTransponder?.otsiOSTransportStreamInfo?.opiiOSPacketIdentifierInfo?[iLocalPID].otsiOSTransportStreamInfo?.olotsfOSListOSTransportStreamFilter?.AddLast(tsfCurrentTransportStreamFilter);
        }

        public static void ProcessTransponders(this OSScanTransponder? ostLocalOSScanTransponder, byte[] byaLocalBuffer, int iReceivedBytes)
        {
            lCurrentLogger.Trace("OSScanTransponder.ProcessTransponders()".Pastel(ConsoleColor.Cyan));

            if (ostLocalOSScanTransponder?.otsiOSTransportStreamInfo?.olotsfOSListOSTransportStreamFilter != null)
            {
                if (ostLocalOSScanTransponder.otsiOSTransportStreamInfo.olotsfOSListOSTransportStreamFilter.Count > 0)
                {
                    for (var i = 0; i < ostLocalOSScanTransponder.otsiOSTransportStreamInfo.olotsfOSListOSTransportStreamFilter.Count; i++)
                    {
                        var otsfCurrentOSTransportStreamFilter = ostLocalOSScanTransponder.otsiOSTransportStreamInfo.olotsfOSListOSTransportStreamFilter.ElementAt(i);

                        if (otsfCurrentOSTransportStreamFilter.bDone || CurrentTimestamp() >= otsfCurrentOSTransportStreamFilter.lTimestamp + otsfCurrentOSTransportStreamFilter.lTimeout)
                            ostLocalOSScanTransponder.otsiOSTransportStreamInfo.olotsfOSListOSTransportStreamFilter.Remove(otsfCurrentOSTransportStreamFilter);
                    }
                }

                if (ostLocalOSScanTransponder.otsiOSTransportStreamInfo.olotsfOSListOSTransportStreamFilter.IsEmpty())
                    ostLocalOSScanTransponder.otsiOSTransportStreamInfo.bDone = true;

                while (iReceivedBytes >= 188)
                {
                    ostLocalOSScanTransponder?.otsiOSTransportStreamInfo?.ProcessTransponder(byaLocalBuffer);
                    byaLocalBuffer = byaLocalBuffer[188..];
                    iReceivedBytes -= 188;
                }
            }
        }
    }
}