using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public static class OSScanTransponder_Extension
{
    extension(OSScanTransponder ostLocalOSScanTransponder)
    {
        /// <summary>
        /// OSScanTransponder.Scan()
        /// </summary>
        /// <param name="iLocalTunerLockGraceTime"></param>
        /// <returns>bool</returns>
        public bool Scan(int iLocalTunerLockGraceTime)
        {
            LogTrace("OSScanTransponder.Scan()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;
            var otsiCurrentOSTransportStreamInfo = ostLocalOSScanTransponder.otsiOSTransportStreamInfo;

            if (ostLocalOSScanTransponder.osiOSScanIP == null || osicCurrentOSSatIPConnection == null || otsiCurrentOSTransportStreamInfo == null)
                return false;

            ostLocalOSScanTransponder.bFailed = false;
            ostLocalOSScanTransponder.bTimeout = false;
            ostLocalOSScanTransponder.bNoSignal = false;
            ostLocalOSScanTransponder.bPermanentError = false;
            ostLocalOSScanTransponder.strFailReason = null;
            ostLocalOSScanTransponder.iTunerLock = -1;
            ostLocalOSScanTransponder.bTunerStatusReceived = false;
            ostLocalOSScanTransponder.bTunerFrequencyConfirmed = false;

            var bKeepSession = false;

            try
            {
                var bChannelChange = ostLocalOSScanTransponder.HasOpenSession();

                if (bChannelChange && !ostLocalOSScanTransponder.UpdatePIDs())
                {
                    if (ostLocalOSScanTransponder.strFailReason?.StartsWith("RTSP ", StringComparison.Ordinal) == true)
                        osicCurrentOSSatIPConnection.bChannelChangeUnsupported = true;

                    lCurrentLogger.Warn($"Warning: Channel change within the session failed ({ostLocalOSScanTransponder.strFailReason}) - opening a new session".Pastel(ConsoleColor.Yellow));

                    ostLocalOSScanTransponder.CloseSession();
                    ostLocalOSScanTransponder.bPermanentError = false;
                    ostLocalOSScanTransponder.strFailReason = null;
                    bChannelChange = false;
                }

                if (!bChannelChange)
                {
                    ostLocalOSScanTransponder.CloseSession();

                    if (!ostLocalOSScanTransponder.OpenSession() || !ostLocalOSScanTransponder.UpdatePIDs())
                        return false;
                }

                ostLocalOSScanTransponder.DrainUDPSockets();
                ostLocalOSScanTransponder.AddSFilter(DEFAULT_PID_PAT, DEFAULT_TID_PA, 0, 0, 5);
                ostLocalOSScanTransponder.AddSFilter(DEFAULT_PID_SDT, DEFAULT_TID_SD, 0, 1, 5);

                var byaReceiveBufferUDP = new byte[UDP_DATAGRAM_BUFFER_SIZE];
                var iEmptyPacketCounter = 0;
                var bDataReceived = false;
                var bAwaitChannel = bChannelChange;
                var lChannelChangeTime = Environment.TickCount64;
                var lTunerLockDeadline = Environment.TickCount64 + iLocalTunerLockGraceTime;
                var lLastWallTime = Environment.TickCount64;

                while (!bDone && !otsiCurrentOSTransportStreamInfo.bDone && !ostLocalOSScanTransponder.bFailed)
                {
                    if (Environment.TickCount64 >= osicCurrentOSSatIPConnection.lLastRTSPActivity + Math.Max(osicCurrentOSSatIPConnection.iSessionTimeout / 2, 5) * 1000L)
                    {
                        if (!ostLocalOSScanTransponder.RTSPSendOptions() || !ostLocalOSScanTransponder.RTSPCheckOK())
                        {
                            ostLocalOSScanTransponder.bFailed = true;
                            break;
                        }
                    }

                    if (bDataReceived || Environment.TickCount64 >= lTunerLockDeadline)
                        ostLocalOSScanTransponder.RemoveFinishedFilters();

                    ostLocalOSScanTransponder.ProcessRTCPReports();

                    if (!bDataReceived && ostLocalOSScanTransponder.iTunerLock == 0 && Environment.TickCount64 >= lTunerLockDeadline)
                    {
                        ostLocalOSScanTransponder.bNoSignal = true;
                        break;
                    }

                    var iReceivedBytesUDP = osicCurrentOSSatIPConnection.nsSocketUDP!.Poll(1000000, SelectMode.SelectRead) ? osicCurrentOSSatIPConnection.nsSocketUDP.Receive(byaReceiveBufferUDP) : 0;
                    var lCurrentWallTime = Environment.TickCount64;
                    var lWallElapsed = lCurrentWallTime - lLastWallTime;
                    lLastWallTime = lCurrentWallTime;

                    if (iReceivedBytesUDP <= RTP_HEADER_LENGTH)
                    {
                        otsiCurrentOSTransportStreamInfo.AdvanceStreamClock(null, lWallElapsed);

                        if (iEmptyPacketCounter++ >= MAX_EMPTY_PACKETS)
                        {
                            ostLocalOSScanTransponder.bTimeout = true;
                            break;
                        }

                        continue;
                    }

                    if (bAwaitChannel)
                    {
                        if (!ostLocalOSScanTransponder.IsChannelConfirmed(lChannelChangeTime))
                            continue;

                        bAwaitChannel = false;
                    }

                    otsiCurrentOSTransportStreamInfo.AdvanceStreamClock(BinaryPrimitives.ReadUInt32BigEndian(byaReceiveBufferUDP.AsSpan(4, 4)), lWallElapsed);

                    if (!bDataReceived)
                    {
                        bDataReceived = true;
                        ostLocalOSScanTransponder.RestartFilterTimeouts();
                    }

                    ostLocalOSScanTransponder.ProcessTransponders(byaReceiveBufferUDP.AsSpan(RTP_HEADER_LENGTH, iReceivedBytesUDP - RTP_HEADER_LENGTH));
                }

                if (!bDataReceived)
                    ostLocalOSScanTransponder.bTimeout = true;

                if (ostLocalOSScanTransponder.otiOSTransponderInfo != null && oCurrentOptions != null)
                {
                    if (ostLocalOSScanTransponder.otiOSTransponderInfo.bScanEventInformationTable && oCurrentOptions.cloPrintEvents)
                        ostLocalOSScanTransponder.otiOSTransponderInfo.PrintEvents();

                    if (oCurrentOptions.cloPrintServices)
                        ostLocalOSScanTransponder.otiOSTransponderInfo.PrintServices();
                }

                ostLocalOSScanTransponder.bTimeout = ostLocalOSScanTransponder.otiOSTransponderInfo?.olosOSListOSService.Count == 0 && ostLocalOSScanTransponder.bTimeout;
                bKeepSession = !ostLocalOSScanTransponder.bFailed;

                return !ostLocalOSScanTransponder.bFailed;
            }
            catch (SocketException sexcCurrentSocketException)
            {
                ostLocalOSScanTransponder.strFailReason = $"SOCKET ERROR {sexcCurrentSocketException.SocketErrorCode}";
                lCurrentLogger.Error($"Error: {sexcCurrentSocketException.Message}".Pastel(ConsoleColor.Red));
                return false;
            }
            finally
            {
                if (!bKeepSession)
                    ostLocalOSScanTransponder.CloseSession();
            }
        }

        /// <summary>
        /// OSScanTransponder.HasOpenSession()
        /// </summary>
        /// <returns>bool</returns>
        public bool HasOpenSession()
        {
            LogTrace("OSScanTransponder.HasOpenSession()");

            return ostLocalOSScanTransponder.osicOSSatIPConnection is { nsSocketTCP.Connected: true, nsSocketUDP: not null, strSessionID: not null, bChannelChangeUnsupported: false };
        }

        /// <summary>
        /// OSScanTransponder.OpenSession()
        /// </summary>
        /// <returns>bool</returns>
        public bool OpenSession()
        {
            LogTrace("OSScanTransponder.OpenSession()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;

            if (osicCurrentOSSatIPConnection == null)
                return false;

            osicCurrentOSSatIPConnection.iSequence = 0;
            osicCurrentOSSatIPConnection.iLastSentSequence = -1;
            osicCurrentOSSatIPConnection.sbRTSPReceiveBuffer.Clear();
            osicCurrentOSSatIPConnection.bRTSPConnectionClosed = false;
            osicCurrentOSSatIPConnection.iNSPort = 0;
            osicCurrentOSSatIPConnection.iStreamID = 0;
            osicCurrentOSSatIPConnection.strSessionID = null;
            osicCurrentOSSatIPConnection.iSessionTimeout = RTSP_DEFAULT_SESSION_TIMEOUT;
            osicCurrentOSSatIPConnection.lLastRTSPActivity = Environment.TickCount64;

            ostLocalOSScanTransponder.InitSocketUDP();
            if (osicCurrentOSSatIPConnection.nsSocketUDP is not { IsBound: true })
            {
                ostLocalOSScanTransponder.strFailReason = "UDP SOCKET NOT BOUND";
                lCurrentLogger.Error("Error: Could not bind UDP socket".Pastel(ConsoleColor.Red));
                return false;
            }

            ostLocalOSScanTransponder.InitSocketTCP();
            if (osicCurrentOSSatIPConnection.nsSocketTCP is not { Connected: true })
            {
                ostLocalOSScanTransponder.strFailReason = "TCP CONNECTION FAILED";
                lCurrentLogger.Error("Error: Could not connect TCP socket".Pastel(ConsoleColor.Red));
                return false;
            }

            return ostLocalOSScanTransponder.RTSPSendSetup(false) && ostLocalOSScanTransponder.RTSPCheckOK();
        }

        /// <summary>
        /// OSScanTransponder.CloseSession()
        /// </summary>
        public void CloseSession()
        {
            LogTrace("OSScanTransponder.CloseSession()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;

            if (osicCurrentOSSatIPConnection == null)
                return;

            if (osicCurrentOSSatIPConnection is { nsSocketTCP.Connected: true, strSessionID: not null })
                ostLocalOSScanTransponder.RTSPSendTeardown();

            osicCurrentOSSatIPConnection.nsSocketTCP?.Close();
            osicCurrentOSSatIPConnection.nsSocketTCP = null;
            osicCurrentOSSatIPConnection.nsSocketUDP?.Close();
            osicCurrentOSSatIPConnection.nsSocketUDP = null;
            osicCurrentOSSatIPConnection.nsSocketRTCP?.Close();
            osicCurrentOSSatIPConnection.nsSocketRTCP = null;
            osicCurrentOSSatIPConnection.strSessionID = null;
        }

        /// <summary>
        /// OSScanTransponder.DrainUDPSockets()
        /// </summary>
        public void DrainUDPSockets()
        {
            LogTrace("OSScanTransponder.DrainUDPSockets()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;
            var byaCurrentDiscardBuffer = new byte[UDP_DATAGRAM_BUFFER_SIZE];
            var iCurrentDiscarded = 0;

            foreach (var nsCurrentSocket in new[] { osicCurrentOSSatIPConnection?.nsSocketUDP, osicCurrentOSSatIPConnection?.nsSocketRTCP })
            {
                while (nsCurrentSocket is { Available: > 0 })
                {
                    nsCurrentSocket.Receive(byaCurrentDiscardBuffer);
                    iCurrentDiscarded++;
                }
            }

            if (iCurrentDiscarded > 0)
                LogTrace($"Discarded {iCurrentDiscarded} datagram(s) of the previous transponder");
        }

        /// <summary>
        /// OSScanTransponder.IsChannelConfirmed()
        /// </summary>
        /// <param name="lLocalChannelChangeTime"></param>
        /// <returns></returns>
        public bool IsChannelConfirmed(long lLocalChannelChangeTime)
        {
            LogTrace("OSScanTransponder.IsChannelConfirmed()");

            var lCurrentWaitTime = Environment.TickCount64 - lLocalChannelChangeTime;

            if (ostLocalOSScanTransponder.bTunerFrequencyConfirmed)
                return true;

            if (!ostLocalOSScanTransponder.bTunerStatusReceived && lCurrentWaitTime >= CHANNEL_CHANGE_SETTLE_TIME)
                return true;

            if (lCurrentWaitTime < CHANNEL_CHANGE_MAX_WAIT)
                return false;

            lCurrentLogger.Warn($"Warning: Tuner doesn't report the new frequency after {lCurrentWaitTime} ms - data accepted anyway".Pastel(ConsoleColor.Yellow));
            return true;
        }

        /// <summary>
        /// OSScanTransponder.InitSocketUDP()
        /// </summary>
        public void InitSocketUDP()
        {
            LogTrace("OSScanTransponder.InitSocketUDP()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;

            if (osicCurrentOSSatIPConnection == null)
                return;

            osicCurrentOSSatIPConnection.iNSPort = 0;

            for (var iCurrentAttempt = 0; iCurrentAttempt < UDP_PORT_PAIR_BIND_ATTEMPTS; iCurrentAttempt++)
            {
                var nsSocketUDP = CreateUDPSocket(0, UDP_RECEIVE_BUFFER_SIZE);

                if (nsSocketUDP == null)
                    return;

                var iCurrentPort = ((IPEndPoint)nsSocketUDP.LocalEndPoint!).Port;
                var nsSocketRTCP = iCurrentPort % 2 == 0 ? CreateUDPSocket(iCurrentPort + 1, 0) : null;

                if (nsSocketRTCP != null || iCurrentAttempt == UDP_PORT_PAIR_BIND_ATTEMPTS - 1)
                {
                    osicCurrentOSSatIPConnection.iNSPort = iCurrentPort;
                    osicCurrentOSSatIPConnection.nsSocketUDP = nsSocketUDP;
                    osicCurrentOSSatIPConnection.nsSocketRTCP = nsSocketRTCP;
                    return;
                }

                nsSocketUDP.Dispose();
            }
        }

        /// <summary>
        /// OSScanTransponder.ProcessRTCPReports()
        /// </summary>
        public void ProcessRTCPReports()
        {
            LogTrace("OSScanTransponder.ProcessRTCPReports()");

            var nsSocketRTCP = ostLocalOSScanTransponder.osicOSSatIPConnection?.nsSocketRTCP;

            if (nsSocketRTCP == null)
                return;

            Span<byte> byaCurrentReceiveBuffer = stackalloc byte[2048];

            while (nsSocketRTCP.Available > 0)
            {
                var iReceivedBytes = nsSocketRTCP.Receive(byaCurrentReceiveBuffer);
                ostLocalOSScanTransponder.ParseRTCPReport(byaCurrentReceiveBuffer[..iReceivedBytes]);
            }
        }

        /// <summary>
        /// OSScanTransponder.ParseRTCPReport()
        /// </summary>
        /// <param name="rosLocalPacket"></param>
        public void ParseRTCPReport(ReadOnlySpan<byte> rosLocalPacket)
        {
            LogTrace("OSScanTransponder.ParseRTCPReport()");

            for (var iCurrentPosition = 0; iCurrentPosition + 4 <= rosLocalPacket.Length;)
            {
                if (rosLocalPacket[iCurrentPosition] >> 6 != 2)
                    return;

                var iCurrentPacketLength = (BinaryPrimitives.ReadUInt16BigEndian(rosLocalPacket[(iCurrentPosition + 2)..]) + 1) * 4;

                if (iCurrentPosition + iCurrentPacketLength > rosLocalPacket.Length)
                    return;

                if (rosLocalPacket[iCurrentPosition + 1] == RTCP_PACKET_TYPE_APP && iCurrentPacketLength >= 16 && rosLocalPacket.Slice(iCurrentPosition + 8, 4).SequenceEqual("SES1"u8))
                {
                    var iCurrentStringLength = BinaryPrimitives.ReadUInt16BigEndian(rosLocalPacket[(iCurrentPosition + 14)..]);

                    if (16 + iCurrentStringLength <= iCurrentPacketLength)
                        ostLocalOSScanTransponder.ParseTunerStatus(Encoding.ASCII.GetString(rosLocalPacket.Slice(iCurrentPosition + 16, iCurrentStringLength)));
                }

                iCurrentPosition += iCurrentPacketLength;
            }
        }

        /// <summary>
        /// OSScanTransponder.ParseTunerStatus()
        /// </summary>
        /// <param name="strLocalStatus"></param>
        public void ParseTunerStatus(string strLocalStatus)
        {
            LogTrace("OSScanTransponder.ParseTunerStatus()");

            var strCurrentTuner = strLocalStatus.Split(';').FirstOrDefault(strCurrentPart => strCurrentPart.StartsWith("tuner=", StringComparison.OrdinalIgnoreCase));
            var saCurrentTunerValues = strCurrentTuner?[6..]?.Split(',');

            if (saCurrentTunerValues is not { Length: >= 4 } || !int.TryParse(saCurrentTunerValues[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iCurrentLock))
                return;

            ostLocalOSScanTransponder.bTunerStatusReceived = true;

            if (saCurrentTunerValues.Length >= 5 && double.TryParse(saCurrentTunerValues[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var dCurrentFrequency) && ostLocalOSScanTransponder.otiOSTransponderInfo != null && Math.Abs(dCurrentFrequency - ostLocalOSScanTransponder.otiOSTransponderInfo.iFrequency) > 1 && Math.Abs(dCurrentFrequency / 1000 - ostLocalOSScanTransponder.otiOSTransponderInfo.iFrequency) > 1)
            {
                LogTrace($"Tuner status for {dCurrentFrequency.ToString(CultureInfo.InvariantCulture)} MHz ignored (expected {ostLocalOSScanTransponder.otiOSTransponderInfo.iFrequency} MHz)");
                return;
            }

            ostLocalOSScanTransponder.bTunerFrequencyConfirmed = true;

            int.TryParse(saCurrentTunerValues[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ostLocalOSScanTransponder.iTunerLevel);
            int.TryParse(saCurrentTunerValues[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out ostLocalOSScanTransponder.iTunerQuality);

            if (iCurrentLock != ostLocalOSScanTransponder.iTunerLock)
                LogTrace($"Tuner lock {iCurrentLock} level {ostLocalOSScanTransponder.iTunerLevel} quality {ostLocalOSScanTransponder.iTunerQuality}");

            ostLocalOSScanTransponder.iTunerLock = iCurrentLock;
        }

        /// <summary>
        /// OSScanTransponder.InitSocketTCP()
        /// </summary>
        public void InitSocketTCP()
        {
            LogTrace("OSScanTransponder.InitSocketTCP()");

            if (ostLocalOSScanTransponder.osicOSSatIPConnection == null || string.IsNullOrEmpty(ostLocalOSScanTransponder.osicOSSatIPConnection.strHost))
                return;

            var bUseIPv6 = oCurrentOptions?.cloIPv6 == true;
            var strCurrentHost = ostLocalOSScanTransponder.osicOSSatIPConnection.strHost;
            IPAddress[] ipaaCurrentAddresses;

            try
            {
                ipaaCurrentAddresses = IPAddress.TryParse(strCurrentHost, out var ipaCurrentAddress) ? [ipaCurrentAddress] : Dns.GetHostAddresses(strCurrentHost);
            }
            catch (SocketException sexcCurrentSocketException)
            {
                lCurrentLogger.Error($"Error: {sexcCurrentSocketException.Message}".Pastel(ConsoleColor.Red));
                return;
            }

            foreach (var ipaCurrentAddress in ipaaCurrentAddresses.Where(ipaCurrentAddress => bUseIPv6 || ipaCurrentAddress.AddressFamily == AddressFamily.InterNetwork))
            {
                var nsSocketTCP = new Socket(bUseIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    if (bUseIPv6)
                        nsSocketTCP.DualMode = true;

                    nsSocketTCP.ReceiveTimeout = RTSP_RECEIVE_TIMEOUT;
                    nsSocketTCP.SendTimeout = RTSP_RECEIVE_TIMEOUT;
                    nsSocketTCP.Connect(new IPEndPoint(ipaCurrentAddress, ostLocalOSScanTransponder.osicOSSatIPConnection.iPort));

                    ostLocalOSScanTransponder.osicOSSatIPConnection.nsSocketTCP = nsSocketTCP;
                    return;
                }
                catch (SocketException sexcCurrentSocketException)
                {
                    lCurrentLogger.Error($"Error: {sexcCurrentSocketException.Message}".Pastel(ConsoleColor.Red));
                    nsSocketTCP.Dispose();
                }
            }
        }

        /// <summary>
        /// OSScanTransponder.GetRTSPBaseURI()
        /// </summary>
        /// <returns>string</returns>
        public string GetRTSPBaseURI()
        {
            LogTrace("OSScanTransponder.GetRTSPBaseURI()");

            return $"rtsp://{FormatRTSPHost(ostLocalOSScanTransponder.osicOSSatIPConnection?.strHost)}:{ostLocalOSScanTransponder.osicOSSatIPConnection?.iPort}";
        }

        /// <summary>
        /// OSScanTransponder.RTSPSendCommand()
        /// </summary>
        /// <param name="strLocalRequestLine"></param>
        /// <param name="saLocalHeaders"></param>
        /// <returns>bool</returns>
        public bool RTSPSendCommand(string strLocalRequestLine, params string[] saLocalHeaders)
        {
            LogTrace("OSScanTransponder.RTSPSendCommand()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;

            if (osicCurrentOSSatIPConnection?.nsSocketTCP == null)
                return false;

            var sbCurrentMessage = new StringBuilder();
            sbCurrentMessage.Append(strLocalRequestLine).Append(' ').Append(RTSP_VERSION).Append(RTSP_NEWLINE);
            osicCurrentOSSatIPConnection.iLastSentSequence = osicCurrentOSSatIPConnection.iSequence++;
            sbCurrentMessage.Append("CSeq: ").Append(osicCurrentOSSatIPConnection.iLastSentSequence).Append(RTSP_NEWLINE);

            foreach (var strCurrentHeader in saLocalHeaders)
                sbCurrentMessage.Append(strCurrentHeader).Append(RTSP_NEWLINE);

            sbCurrentMessage.Append(RTSP_NEWLINE);

            LogTrace($"RTSP >> {strLocalRequestLine}");
            osicCurrentOSSatIPConnection.strLastRequestLine = strLocalRequestLine;

            return osicCurrentOSSatIPConnection.nsSocketTCP.SendData(sbCurrentMessage.ToString());
        }

        /// <summary>
        /// OSScanTransponder.RTSPSendSetup()
        /// </summary>
        /// <param name="bLocalIsMulticast"></param>
        /// <returns>bool</returns>
        public bool RTSPSendSetup(bool bLocalIsMulticast)
        {
            LogTrace("OSScanTransponder.RTSPSendSetup()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;

            if (osicCurrentOSSatIPConnection == null)
                return false;

            var strCurrentTransport = bLocalIsMulticast ? $"multicast;port={osicCurrentOSSatIPConnection.iNSPort}-{osicCurrentOSSatIPConnection.iNSPort + 1};ttl=5" : $"unicast;client_port={osicCurrentOSSatIPConnection.iNSPort}-{osicCurrentOSSatIPConnection.iNSPort + 1}";

            return ostLocalOSScanTransponder.RTSPSendCommand($"SETUP {ostLocalOSScanTransponder.GetRTSPBaseURI()}/?{osicCurrentOSSatIPConnection.strTune}", $"Transport: RTP/AVP;{strCurrentTransport}");
        }

        /// <summary>
        /// OSScanTransponder.RTSPSendPlay()
        /// </summary>
        /// <param name="strLocalPIDs"></param>
        /// <returns>bool</returns>
        public bool RTSPSendPlay(string strLocalPIDs)
        {
            LogTrace("OSScanTransponder.RTSPSendPlay()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;

            if (osicCurrentOSSatIPConnection == null)
                return false;

            var strCurrentQuery = !string.IsNullOrEmpty(strLocalPIDs) ? $"?{osicCurrentOSSatIPConnection.strTune}&pids={strLocalPIDs}" : string.Empty;

            return ostLocalOSScanTransponder.RTSPSendCommand($"PLAY {ostLocalOSScanTransponder.GetRTSPBaseURI()}/stream={osicCurrentOSSatIPConnection.iStreamID}{strCurrentQuery}", $"Session: {osicCurrentOSSatIPConnection.strSessionID}");
        }

        /// <summary>
        /// OSScanTransponder.RTSPSendOptions()
        /// </summary>
        /// <returns>bool</returns>
        public bool RTSPSendOptions()
        {
            LogTrace("OSScanTransponder.RTSPSendOptions()");

            return ostLocalOSScanTransponder.RTSPSendCommand($"OPTIONS {ostLocalOSScanTransponder.GetRTSPBaseURI()}/", $"Session: {ostLocalOSScanTransponder.osicOSSatIPConnection?.strSessionID}");
        }

        /// <summary>
        /// OSScanTransponder.RTSPSendTeardown()
        /// </summary>
        /// <returns>bool</returns>
        public bool RTSPSendTeardown()
        {
            LogTrace("OSScanTransponder.RTSPSendTeardown()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;

            if (osicCurrentOSSatIPConnection == null || !ostLocalOSScanTransponder.RTSPSendCommand($"TEARDOWN {ostLocalOSScanTransponder.GetRTSPBaseURI()}/stream={osicCurrentOSSatIPConnection.iStreamID}", $"Session: {osicCurrentOSSatIPConnection.strSessionID}"))
                return false;

            try
            {
                var saCurrentResponse = ReadRTSPResponse(osicCurrentOSSatIPConnection, Environment.TickCount64 + RTSP_RECEIVE_TIMEOUT);
                LogTrace(saCurrentResponse != null ? $"RTSP << {saCurrentResponse[0]}" : "RTSP << no TEARDOWN response");
            }
            catch { /* Connection already closed by the server */ }

            return true;
        }

        /// <summary>
        /// OSScanTransponder.RTSPCheckOK()
        /// </summary>
        /// <returns>bool</returns>
        public bool RTSPCheckOK()
        {
            LogTrace("OSScanTransponder.RTSPCheckOK()");

            var osicCurrentOSSatIPConnection = ostLocalOSScanTransponder.osicOSSatIPConnection;

            if (osicCurrentOSSatIPConnection?.nsSocketTCP == null)
                return false;

            string[] saCurrentResponseLines;

            var strCurrentMethod = osicCurrentOSSatIPConnection.strLastRequestLine?.Split(' ')[0];
            var lCurrentWaitStart = Environment.TickCount64;
            var lCurrentDeadline = lCurrentWaitStart + (strCurrentMethod == "SETUP" ? RTSP_SETUP_TIMEOUT : RTSP_RECEIVE_TIMEOUT);

            while (true)
            {
                var saCurrentResponse = ReadRTSPResponse(osicCurrentOSSatIPConnection, lCurrentDeadline);

                if (saCurrentResponse == null)
                {
                    var strCurrentReceived = osicCurrentOSSatIPConnection.sbRTSPReceiveBuffer.ToString();
                    var strCurrentReceivedEscaped = strCurrentReceived.Length > 200 ? strCurrentReceived[..200] + "..." : strCurrentReceived;
                    strCurrentReceivedEscaped = strCurrentReceivedEscaped.Replace("\r", "\\r").Replace("\n", "\\n");

                    if (osicCurrentOSSatIPConnection.bRTSPConnectionClosed)
                    {
                        ostLocalOSScanTransponder.strFailReason = "RTSP CONNECTION CLOSED";
                        lCurrentLogger.Error($"Error: RTSP connection closed by the server while waiting for the response to {strCurrentMethod} (CSeq {osicCurrentOSSatIPConnection.iLastSentSequence})".Pastel(ConsoleColor.Red));
                        return false;
                    }

                    ostLocalOSScanTransponder.strFailReason = "NO RTSP RESPONSE";
                    lCurrentLogger.Error($"Error: No RTSP response to {strCurrentMethod} (CSeq {osicCurrentOSSatIPConnection.iLastSentSequence}) within {Environment.TickCount64 - lCurrentWaitStart} ms - received: {(strCurrentReceived.Length == 0 ? "nothing" : $"\"{strCurrentReceivedEscaped}\"")}".Pastel(ConsoleColor.Red));
                    return false;
                }

                saCurrentResponseLines = saCurrentResponse;
                LogTrace($"RTSP << {saCurrentResponseLines[0]}");

                var strCurrentSequence = saCurrentResponseLines.Skip(1).FirstOrDefault(strCurrentLine => strCurrentLine.StartsWith("CSeq:", StringComparison.OrdinalIgnoreCase));

                if (strCurrentSequence == null || !int.TryParse(strCurrentSequence[5..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var iCurrentSequence) || iCurrentSequence >= osicCurrentOSSatIPConnection.iLastSentSequence)
                    break;

                lCurrentLogger.Warn($"Warning: Outdated RTSP response (CSeq {iCurrentSequence}, expected {osicCurrentOSSatIPConnection.iLastSentSequence}) discarded".Pastel(ConsoleColor.Yellow));
            }

            var lCurrentResponseTime = Environment.TickCount64 - lCurrentWaitStart;
            osicCurrentOSSatIPConnection.lLastRTSPActivity = Environment.TickCount64;

            if (lCurrentResponseTime > RTSP_SLOW_RESPONSE_WARNING)
                lCurrentLogger.Warn($"Warning: RTSP response to {strCurrentMethod} took {lCurrentResponseTime} ms".Pastel(ConsoleColor.Yellow));

            var saCurrentStatusLine = saCurrentResponseLines[0].Split(' ', 3);
            var iCurrentStatusCode = saCurrentStatusLine.Length >= 2 && int.TryParse(saCurrentStatusLine[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iParsedStatusCode) ? iParsedStatusCode : 0;

            if (iCurrentStatusCode != 200)
            {
                ostLocalOSScanTransponder.strFailReason = $"RTSP {string.Join(' ', saCurrentStatusLine.Skip(1))}";
                ostLocalOSScanTransponder.bPermanentError = !RTSP_TRANSIENT_STATUS_CODES.Contains(iCurrentStatusCode);

                lCurrentLogger.Warn($"Warning: RTSP response {saCurrentResponseLines[0]}{(ostLocalOSScanTransponder.bPermanentError ? " - not retried" : string.Empty)}".Pastel(ConsoleColor.Yellow));
                return false;
            }

            foreach (var strCurrentResponseLine in saCurrentResponseLines.Skip(1))
            {
                var iCurrentSeparator = strCurrentResponseLine.IndexOf(':');

                if (iCurrentSeparator <= 0)
                    continue;

                var strCurrentHeaderName = strCurrentResponseLine[..iCurrentSeparator].Trim();
                var strCurrentHeaderValue = strCurrentResponseLine[(iCurrentSeparator + 1)..].Trim();

                if (strCurrentHeaderName.Equals("Session", StringComparison.OrdinalIgnoreCase))
                {
                    var saCurrentSessionParts = strCurrentHeaderValue.Split(';', StringSplitOptions.TrimEntries);
                    osicCurrentOSSatIPConnection.strSessionID = saCurrentSessionParts[0];

                    foreach (var strCurrentSessionPart in saCurrentSessionParts.Skip(1))
                        if (strCurrentSessionPart.StartsWith("timeout=", StringComparison.OrdinalIgnoreCase) && int.TryParse(strCurrentSessionPart[8..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iCurrentSessionTimeout) && iCurrentSessionTimeout > 0)
                            osicCurrentOSSatIPConnection.iSessionTimeout = iCurrentSessionTimeout;
                }
                else if (strCurrentHeaderName.Equals("com.ses.streamID", StringComparison.OrdinalIgnoreCase) && int.TryParse(strCurrentHeaderValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iCurrentStreamID))
                    osicCurrentOSSatIPConnection.iStreamID = iCurrentStreamID;
            }

            return true;
        }

        /// <summary>
        /// OSScanTransponder.UpdatePIDs()
        /// </summary>
        /// <returns></returns>
        public bool UpdatePIDs()
        {
            LogTrace("OSScanTransponder.UpdatePIDs()");

            var opiiaCurrentOSPacketIdentifierInfo = ostLocalOSScanTransponder.otsiOSTransportStreamInfo?.opiiOSPacketIdentifierInfo;
            var sbPIDs = new StringBuilder();

            if (opiiaCurrentOSPacketIdentifierInfo != null)
            {
                foreach (var opiiCurrentOSPacketIdentifierInfo in opiiaCurrentOSPacketIdentifierInfo)
                {
                    if (!opiiCurrentOSPacketIdentifierInfo.bUsed)
                        continue;

                    if (sbPIDs.Length >= 300)
                        break;

                    if (sbPIDs.Length > 0)
                        sbPIDs.Append(',');

                    sbPIDs.Append(opiiCurrentOSPacketIdentifierInfo.iPacketID);
                }
            }

            if (sbPIDs.Length == 0)
                sbPIDs.Append("none");

            return ostLocalOSScanTransponder.RTSPSendPlay(sbPIDs.ToString()) && ostLocalOSScanTransponder.RTSPCheckOK();
        }

        /// <summary>
        /// OSScanTransponder.AddPID()
        /// </summary>
        /// <param name="iLocalPID"></param>
        /// <param name="iLocalUseTableExtension"></param>
        public void AddPID(int iLocalPID, int iLocalUseTableExtension)
        {
            LogTrace("OSScanTransponder.AddPID()");

            var otsiCurrentOSTransportStreamInfo = ostLocalOSScanTransponder.otsiOSTransportStreamInfo;
            var opiiCurrentOSPacketIdentifierInfo = otsiCurrentOSTransportStreamInfo?.opiiOSPacketIdentifierInfo?[iLocalPID];

            if (otsiCurrentOSTransportStreamInfo == null || opiiCurrentOSPacketIdentifierInfo == null)
                return;

            if (!opiiCurrentOSPacketIdentifierInfo.bUsed)
            {
                opiiCurrentOSPacketIdentifierInfo.Init(iLocalPID, otsiCurrentOSTransportStreamInfo);
                opiiCurrentOSPacketIdentifierInfo.bUsed = true;
                otsiCurrentOSTransportStreamInfo.ostOSScanTransponder = ostLocalOSScanTransponder;
                otsiCurrentOSTransportStreamInfo.olopiiOSListOSPacketIdentifierInfo?.AddLast(opiiCurrentOSPacketIdentifierInfo);

                if (!ostLocalOSScanTransponder.UpdatePIDs())
                    ostLocalOSScanTransponder.bFailed = true;
            }

            opiiCurrentOSPacketIdentifierInfo.iUseTableExtension = iLocalUseTableExtension;
        }

        /// <summary>
        /// OSScanTransponder.AddSFilter()
        /// </summary>
        /// <param name="iLocalPID"></param>
        /// <param name="byLocalTableID"></param>
        /// <param name="iLocalTableExtension"></param>
        /// <param name="iLocalUseTableExtension"></param>
        /// <param name="lLocalTimeout">Inactivity timeout in seconds of stream time</param>
        public void AddSFilter(int iLocalPID, byte byLocalTableID, int iLocalTableExtension, int iLocalUseTableExtension, long lLocalTimeout)
        {
            LogTrace("OSScanTransponder.AddSFilter()");

            ostLocalOSScanTransponder.AddPID(iLocalPID, iLocalUseTableExtension);

            var opiiCurrentOSPacketIdentifierInfo = ostLocalOSScanTransponder.otsiOSTransportStreamInfo?.opiiOSPacketIdentifierInfo?[iLocalPID];
            var olotsfCurrentOSListOSTransportStreamFilter = opiiCurrentOSPacketIdentifierInfo?.olotsfOSListOSTransportStreamFilter;

            if (opiiCurrentOSPacketIdentifierInfo == null || olotsfCurrentOSListOSTransportStreamFilter == null)
                return;

            if (olotsfCurrentOSListOSTransportStreamFilter.Any(otsfCurrentOSTransportStreamFilter => otsfCurrentOSTransportStreamFilter.byTableID == byLocalTableID && otsfCurrentOSTransportStreamFilter.iTableExtension == iLocalTableExtension))
                return;

            OSTransportStreamFilter otsfCurrentOSTransportStreamFilter = new()
            {
                opiiOSPacketIdentifierInfo = opiiCurrentOSPacketIdentifierInfo,
                byTableID = byLocalTableID,
                iTableExtension = iLocalTableExtension,
                iUseTableExtension = iLocalUseTableExtension,
                byVersionNumber = 0xFF,
                lTimeout = lLocalTimeout * 1000,
                lTimestamp = ostLocalOSScanTransponder.otsiOSTransportStreamInfo?.GetStreamTime() ?? 0
            };

            olotsfCurrentOSListOSTransportStreamFilter.AddLast(otsfCurrentOSTransportStreamFilter);
            opiiCurrentOSPacketIdentifierInfo.otsiOSTransportStreamInfo?.olotsfOSListOSTransportStreamFilter?.AddLast(otsfCurrentOSTransportStreamFilter);
        }

        /// <summary>
        /// OSScanTransponder.RestartFilterTimeouts()
        /// </summary>
        public void RestartFilterTimeouts()
        {
            LogTrace("OSScanTransponder.RestartFilterTimeouts()");

            var lCurrentTimestamp = ostLocalOSScanTransponder.otsiOSTransportStreamInfo?.GetStreamTime() ?? 0;

            foreach (var otsfCurrentOSTransportStreamFilter in ostLocalOSScanTransponder.otsiOSTransportStreamInfo?.olotsfOSListOSTransportStreamFilter ?? [])
                otsfCurrentOSTransportStreamFilter.lTimestamp = lCurrentTimestamp;
        }

        /// <summary>
        /// OSScanTransponder.RemoveFinishedFilters()
        /// </summary>
        public void RemoveFinishedFilters()
        {
            LogTrace("OSScanTransponder.RemoveFinishedFilters()");

            var otsiCurrentOSTransportStreamInfo = ostLocalOSScanTransponder.otsiOSTransportStreamInfo;
            var olotsfCurrentOSListOSTransportStreamFilter = otsiCurrentOSTransportStreamInfo?.olotsfOSListOSTransportStreamFilter;

            if (otsiCurrentOSTransportStreamInfo == null || olotsfCurrentOSListOSTransportStreamFilter == null)
                return;

            var lCurrentTimestamp = otsiCurrentOSTransportStreamInfo.GetStreamTime();

            for (var llnCurrentNode = olotsfCurrentOSListOSTransportStreamFilter.First; llnCurrentNode != null;)
            {
                var llnNextNode = llnCurrentNode.Next;

                if (llnCurrentNode.Value.bDone || lCurrentTimestamp >= llnCurrentNode.Value.lTimestamp + llnCurrentNode.Value.lTimeout)
                    olotsfCurrentOSListOSTransportStreamFilter.Remove(llnCurrentNode);

                llnCurrentNode = llnNextNode;
            }

            if (olotsfCurrentOSListOSTransportStreamFilter.IsEmpty())
                otsiCurrentOSTransportStreamInfo.bDone = true;
        }

        /// <summary>
        /// OSScanTransponder.ProcessTransponders()
        /// </summary>
        /// <param name="rosLocalBuffer"></param>
        public void ProcessTransponders(ReadOnlySpan<byte> rosLocalBuffer)
        {
            LogTrace($"OSScanTransponder.ProcessTransponders() - Received Bytes {rosLocalBuffer.Length}");

            var otsiCurrentOSTransportStreamInfo = ostLocalOSScanTransponder.otsiOSTransportStreamInfo;

            if (otsiCurrentOSTransportStreamInfo == null)
                return;

            for (var iCurrentPosition = 0; iCurrentPosition + TS_PACKET_LENGTH <= rosLocalBuffer.Length; iCurrentPosition += TS_PACKET_LENGTH)
            {
                var rosCurrentPacket = rosLocalBuffer.Slice(iCurrentPosition, TS_PACKET_LENGTH);

                if (rosCurrentPacket[0] == TS_SYNC_BYTE)
                    otsiCurrentOSTransportStreamInfo.ProcessTransponder(rosCurrentPacket);
            }
        }
    }

    /// <summary>
    /// OSScanTransponder.CreateUDPSocket()
    /// </summary>
    /// <param name="iLocalPort">0 for an ephemeral port</param>
    /// <param name="iLocalReceiveBufferSize">0 to keep the default</param>
    /// <returns>Socket</returns>
    private static Socket? CreateUDPSocket(int iLocalPort, int iLocalReceiveBufferSize)
    {
        LogTrace("OSScanTransponder.CreateUDPSocket()");

        var bUseIPv6 = oCurrentOptions?.cloIPv6 == true;
        var nsCurrentSocket = new Socket(bUseIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        try
        {
            if (bUseIPv6)
                nsCurrentSocket.DualMode = true;

            if (iLocalReceiveBufferSize > 0)
                nsCurrentSocket.ReceiveBufferSize = iLocalReceiveBufferSize;

            nsCurrentSocket.Bind(new IPEndPoint(bUseIPv6 ? IPAddress.IPv6Any : IPAddress.Any, iLocalPort));
            return nsCurrentSocket;
        }
        catch (SocketException sexcCurrentSocketException)
        {
            if (iLocalPort == 0)
                lCurrentLogger.Error($"Error: {sexcCurrentSocketException.Message}".Pastel(ConsoleColor.Red));

            nsCurrentSocket.Dispose();
            return null;
        }
    }

    /// <summary>
    /// OSScanTransponder.ReadRTSPResponse()
    /// </summary>
    /// <param name="osicLocalOSSatIPConnection"></param>
    /// <param name="lLocalDeadline">Environment.TickCount64 until the response has to be complete</param>
    /// <returns>string[]</returns>
    private static string[]? ReadRTSPResponse(OSSatIPConnection osicLocalOSSatIPConnection, long lLocalDeadline)
    {
        LogTrace("OSScanTransponder.ReadRTSPResponse()");

        var nsCurrentSocket = osicLocalOSSatIPConnection.nsSocketTCP;
        var sbCurrentReceiveBuffer = osicLocalOSSatIPConnection.sbRTSPReceiveBuffer;
        var byaCurrentReceiveBuffer = new byte[4096];

        if (nsCurrentSocket == null)
            return null;

        try
        {
            while (true)
            {
                var strCurrentBuffer = sbCurrentReceiveBuffer.ToString();
                var (iCurrentHeaderEnd, iCurrentTerminatorLength) = FindRTSPHeaderEnd(strCurrentBuffer);

                if (iCurrentHeaderEnd >= 0)
                {
                    var saCurrentHeaderLines = SplitRTSPLines(strCurrentBuffer[..iCurrentHeaderEnd]);
                    var iCurrentContentLength = saCurrentHeaderLines
                        .Where(strCurrentLine => strCurrentLine.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        .Select(strCurrentLine => int.TryParse(strCurrentLine[15..].Trim(), out var iCurrentValue) ? iCurrentValue : 0)
                        .FirstOrDefault();
                    var iCurrentResponseLength = iCurrentHeaderEnd + iCurrentTerminatorLength + iCurrentContentLength;

                    if (strCurrentBuffer.Length >= iCurrentResponseLength)
                    {
                        sbCurrentReceiveBuffer.Remove(0, iCurrentResponseLength);
                        return saCurrentHeaderLines;
                    }
                }
                var lRemainingTime = lLocalDeadline - Environment.TickCount64;

                if (iCurrentHeaderEnd < 0 && IsRTSPHeaderWithoutEmptyLine(strCurrentBuffer) && !nsCurrentSocket.Poll((int)Math.Clamp(lRemainingTime, 0, RTSP_RESPONSE_COMPLETION_WAIT) * 1000, SelectMode.SelectRead))
                {
                    LogTrace("RTSP response without terminating empty line accepted");
                    sbCurrentReceiveBuffer.Clear();
                    return SplitRTSPLines(strCurrentBuffer.TrimEnd('\r', '\n'));
                }

                if (lRemainingTime <= 0)
                    return null;

                nsCurrentSocket.ReceiveTimeout = (int)lRemainingTime;
                var iReceivedBytes = nsCurrentSocket.Receive(byaCurrentReceiveBuffer);

                if (iReceivedBytes == 0)
                {
                    if (!IsRTSPHeaderWithoutEmptyLine(strCurrentBuffer))
                    {
                        osicLocalOSSatIPConnection.bRTSPConnectionClosed = true;
                        return null;
                    }

                    sbCurrentReceiveBuffer.Clear();
                    return SplitRTSPLines(strCurrentBuffer.TrimEnd('\r', '\n'));
                }

                sbCurrentReceiveBuffer.Append(Encoding.ASCII.GetString(byaCurrentReceiveBuffer, 0, iReceivedBytes));
            }
        }
        catch (SocketException sexcCurrentSocketException) when (sexcCurrentSocketException.SocketErrorCode == SocketError.TimedOut)
        {
            return null;
        }
    }

    /// <summary>
    /// OSScanTransponder.FindRTSPHeaderEnd()
    /// </summary>
    /// <param name="strLocalBuffer"></param>
    /// <returns>int, int</returns>
    private static (int iHeaderEnd, int iTerminatorLength) FindRTSPHeaderEnd(string strLocalBuffer)
    {
        LogTrace("OSScanTransponder.FindRTSPHeaderEnd()");

        var tCurrentResult = (iHeaderEnd: -1, iTerminatorLength: 0);

        foreach (var strCurrentTerminator in (string[])["\r\n\r\n", "\n\n", "\r\r"])
        {
            var iCurrentPosition = strLocalBuffer.IndexOf(strCurrentTerminator, StringComparison.Ordinal);

            if (iCurrentPosition >= 0 && (tCurrentResult.iHeaderEnd < 0 || iCurrentPosition < tCurrentResult.iHeaderEnd))
                tCurrentResult = (iCurrentPosition, strCurrentTerminator.Length);
        }

        return tCurrentResult;
    }

    /// <summary>
    /// OSScanTransponder.SplitRTSPLines()
    /// </summary>
    /// <param name="strLocalHeader"></param>
    /// <returns>string[]</returns>
    private static string[] SplitRTSPLines(string strLocalHeader)
    {
        LogTrace("OSScanTransponder.SplitRTSPLines()");

        return strLocalHeader.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
    }

    /// <summary>
    /// OSScanTransponder.IsRTSPHeaderWithoutEmptyLine()
    /// </summary>
    /// <param name="strLocalBuffer"></param>
    /// <returns>bool</returns>
    private static bool IsRTSPHeaderWithoutEmptyLine(string strLocalBuffer)
    {
        LogTrace("OSScanTransponder.IsRTSPHeaderWithoutEmptyLine()");

        return strLocalBuffer.StartsWith("RTSP/", StringComparison.OrdinalIgnoreCase)
               && (strLocalBuffer.EndsWith('\n') || strLocalBuffer.EndsWith('\r'))
               && SplitRTSPLines(strLocalBuffer).Any(strCurrentLine => strCurrentLine.StartsWith("CSeq:", StringComparison.OrdinalIgnoreCase));
    }
}