using System.Net.Sockets;
using System.Text;

using static KON.OctoScan.NET.Constants;

namespace KON.OctoScan.NET;

public class OSSatIPConnection
{
    public Socket? nsSocketTCP;
    public Socket? nsSocketUDP;
    public Socket? nsSocketRTCP;
    public readonly StringBuilder sbRTSPReceiveBuffer = new();

    public string? strHost;
    public int iPort;
    public int iNSPort;
    public int iSequence;
    public int iLastSentSequence = -1;
    public string? strLastRequestLine;
    public bool bRTSPConnectionClosed;
    public string? strTune;
    public string? strSessionID;
    public int iSessionTimeout = RTSP_DEFAULT_SESSION_TIMEOUT;
    public int iStreamID;
    public long lLastRTSPActivity;
    public bool bChannelChangeUnsupported;
}