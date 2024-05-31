using System.Net.Sockets;

namespace KON.OctoScan.NET
{
    public class OSSatIPConnection
    {
        public Socket? nsSocketTCP;
        public Socket? nsSocketUDP;

        public string? strHost;
        public int iPort;
        public int iNSPort;
        public int iSequence;
        public string? strTune;
        public int iSessionID;
        public int iStreamID;
    }
}