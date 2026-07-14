using System.Text;

using Pastel;

using static KON.OctoScan.NET.Constants;
using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class OSTransponderInfo
    {
        public OSList<OSService> olosOSListOSService = [];

        public int iType;
        public bool bUseNetworkInformationTable;
        public bool bScanEventInformationTable;
        public int iSource;
        public int iNetworkID;
        public int iOriginalNetworkID;
        public int iTransportStreamID;
        public int iPosition;
        public int iEAST;
        public int iModulationSystem;
        public int iFrequency;
        public int iFrequencyFraction;
        public int iPolarisation;
        public int iSymbolRate;
        public int iRollOff;
        public int iModulationType;
        public int iBandwidth;
        public int iFEC;
        public int iInputStreamID;
        public int[] iaEventInformationTableServiceID = new int[MAX_EIT_SID];

        public override string ToString()
        {
            lCurrentLogger.Trace("OSTransponderInfo.ToString()".Pastel(ConsoleColor.Cyan));

            var sbCurrentStringBuilder = new StringBuilder();

            switch (iModulationSystem)
            {
                case 1:
                {
                    if (iFrequencyFraction != 0)
                        sbCurrentStringBuilder.AppendFormat("freq={0}.{1:D4}&msys=dvbc&sr={2}&mtype={3}", iFrequency, iFrequencyFraction, iSymbolRate, ModulationType2String[iModulationType]);
                    else
                        sbCurrentStringBuilder.AppendFormat("freq={0}&msys=dvbc&sr={1}&mtype={2}", iFrequency, iSymbolRate, ModulationType2String[iModulationType]);

                    break;
                }
                case 2:
                {
                    if (iFrequencyFraction != 0)
                        sbCurrentStringBuilder.AppendFormat("freq={0}.{1:D4}&msys=dvbt&bw={2}", iFrequency, iFrequencyFraction, Bandwidth2String[iBandwidth]);
                    else
                        sbCurrentStringBuilder.AppendFormat("freq={0}&msys=dvbt&bw={1}", iFrequency, Bandwidth2String[iBandwidth]);

                    break;
                }
                case 5:
                case 6:
                {
                    sbCurrentStringBuilder.AppendFormat("src={0}&freq={1}&pol={2}&msys={3}&sr={4}", iSource, iFrequency, Polarisation2String[iPolarisation & 3], ModulationSystem2String[iModulationSystem], iSymbolRate);

                    break;
                }
                case 16:
                {
                    if (iFrequencyFraction != 0)
                        sbCurrentStringBuilder.AppendFormat("freq={0}.{1:D4}&msys=dvbt2&bw={2}&plp={3}", iFrequency, iFrequencyFraction, Bandwidth2String[iBandwidth], iInputStreamID);
                    else
                        sbCurrentStringBuilder.AppendFormat("freq={0}&msys=dvbt2&bw={1}&plp={2}", iFrequency, Bandwidth2String[iBandwidth], iInputStreamID);

                    break;
                }
            }

            return sbCurrentStringBuilder.ToString();
        }
    }
}