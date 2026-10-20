using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public static class OSTransportStreamInfo_Extension
{
    extension(OSTransportStreamInfo otsiLocalOSTransportStreamInfo)
    {
        /// <summary>
        /// OSTransportStreamInfo.GetStreamTime()
        /// </summary>
        /// <returns>long</returns>
        public long GetStreamTime()
        {
            LogTrace("OSTransportStreamInfo.GetStreamTime()");

            return otsiLocalOSTransportStreamInfo.lStreamClock / RTP_CLOCK_TICKS_PER_MS;
        }

        /// <summary>
        /// OSTransportStreamInfo.AdvanceStreamClock()
        /// </summary>
        /// <param name="uiLocalRTPTimestamp">RTP timestamp of the received datagram, null if nothing was received</param>
        /// <param name="lLocalWallElapsed">Wall clock time in milliseconds since the last call</param>
        public void AdvanceStreamClock(uint? uiLocalRTPTimestamp, long lLocalWallElapsed)
        {
            LogTrace("OSTransportStreamInfo.AdvanceStreamClock()");

            var lWallElapsedTicks = lLocalWallElapsed * RTP_CLOCK_TICKS_PER_MS;

            if (uiLocalRTPTimestamp == null)
            {
                otsiLocalOSTransportStreamInfo.lStreamClock += lWallElapsedTicks;
                otsiLocalOSTransportStreamInfo.lIdleCredit += lWallElapsedTicks;
                return;
            }

            if (otsiLocalOSTransportStreamInfo.bRTPClockFallback || !otsiLocalOSTransportStreamInfo.bRTPTimestampValid)
            {
                otsiLocalOSTransportStreamInfo.lStreamClock += otsiLocalOSTransportStreamInfo.bRTPTimestampValid ? lWallElapsedTicks : 0;
                otsiLocalOSTransportStreamInfo.uiLastRTPTimestamp = uiLocalRTPTimestamp.Value;
                otsiLocalOSTransportStreamInfo.bRTPTimestampValid = true;
                otsiLocalOSTransportStreamInfo.lIdleCredit = 0;
                return;
            }

            long lDeltaTicks = unchecked((int)(uiLocalRTPTimestamp.Value - otsiLocalOSTransportStreamInfo.uiLastRTPTimestamp));
            otsiLocalOSTransportStreamInfo.uiLastRTPTimestamp = uiLocalRTPTimestamp.Value;

            var lAdvanceTicks = Math.Max(lDeltaTicks - otsiLocalOSTransportStreamInfo.lIdleCredit, 0);
            otsiLocalOSTransportStreamInfo.lIdleCredit = 0;

            if (lAdvanceTicks > (long)RTP_CLOCK_MAX_JUMP * RTP_CLOCK_TICKS_PER_MS || lDeltaTicks < -(long)RTP_CLOCK_MAX_REORDER * RTP_CLOCK_TICKS_PER_MS)
            {
                LogTrace($"RTP clock discontinuity of {lDeltaTicks / RTP_CLOCK_TICKS_PER_MS} ms - using wall clock");
                otsiLocalOSTransportStreamInfo.lStreamClock += lWallElapsedTicks;
                otsiLocalOSTransportStreamInfo.lRTPClockStall = 0;

                if (++otsiLocalOSTransportStreamInfo.iRTPClockDiscontinuities >= RTP_CLOCK_MAX_DISCONTINUITIES)
                {
                    lCurrentLogger.Warn("Warning: RTP timestamps are not continuous - filter timeouts use the wall clock".Pastel(ConsoleColor.Yellow));
                    otsiLocalOSTransportStreamInfo.bRTPClockFallback = true;
                }

                return;
            }

            otsiLocalOSTransportStreamInfo.iRTPClockDiscontinuities = 0;

            if (lDeltaTicks <= 0)
            {
                otsiLocalOSTransportStreamInfo.lRTPClockStall += lLocalWallElapsed;

                if (otsiLocalOSTransportStreamInfo.lRTPClockStall > RTP_CLOCK_STALL_LIMIT)
                {
                    lCurrentLogger.Warn("Warning: RTP timestamps are not advancing - filter timeouts use the wall clock".Pastel(ConsoleColor.Yellow));
                    otsiLocalOSTransportStreamInfo.bRTPClockFallback = true;
                    otsiLocalOSTransportStreamInfo.lStreamClock += otsiLocalOSTransportStreamInfo.lRTPClockStall * RTP_CLOCK_TICKS_PER_MS;
                }

                return;
            }

            otsiLocalOSTransportStreamInfo.lRTPClockStall = 0;
            otsiLocalOSTransportStreamInfo.lStreamClock += lAdvanceTicks;
        }

        /// <summary>
        /// OSTransportStreamInfo.ProcessTransponder()
        /// </summary>
        /// <param name="rosLocalPacket"></param>
        public void ProcessTransponder(ReadOnlySpan<byte> rosLocalPacket)
        {
            LogTrace("OSTransportStreamInfo.ProcessTransponder()");

            var iPID = 0x1FFF & ((rosLocalPacket[1] << 8) | rosLocalPacket[2]);
            var opiiCurrentOSPacketIdentifierInfo = otsiLocalOSTransportStreamInfo.opiiOSPacketIdentifierInfo?[iPID];

            if (opiiCurrentOSPacketIdentifierInfo is not { bUsed: true })
                return;

            if (opiiCurrentOSPacketIdentifierInfo.byaBuffer == null)
            {
                opiiCurrentOSPacketIdentifierInfo.byaBuffer = new byte[MAX_SECTION_LENGTH];
                opiiCurrentOSPacketIdentifierInfo.byContinuityCounter = 0xFF;
            }

            opiiCurrentOSPacketIdentifierInfo.BuildSection(rosLocalPacket);
        }
    }
}