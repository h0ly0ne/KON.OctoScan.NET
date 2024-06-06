# KON.OctoScan.NET
.NET Port of octoscan (from DigitalDevices)
```KON.OctoScan.NET
Copyright (C) 2016 Digital Devices GmbH
Copyright (C) 2024 Oswald Oliver

ERROR(S):
  Required option 'f, freq' is missing.
  Required option 'm, msys' is missing.
  A required value not bound to option name is missing.

  -n, --use_nit           Use network information table. If not specified only a single transponder
                          is scanned.
  -f, --freq              Required. Frequency in MHz.
  -s, --sr                Symbolrate in kSymbols (required for DVB-S/S2 and DVB-C). DVB-S/S2
                          example: --sr=27500. DVB-C example: --sr=6900.
  -S, --src               Satellite source 1,2,3,4 (required for DVB-S/S2).
  -p, --pol               Polarisation = v,h,r,l (required for DVB-S/S2). Example: --pol=v.
  -b, --bw                Bandwidth 1.712,5,6,7,8,10 (required for DVB-T/T2).
  -P, --isi               ImageStreamIdentifier for physical layer pipe. Example: --isi=1
  -m, --msys              Required. Modulation System = dvbs, dvbs2, dvbc, dvbt, dvbt2. Example:
                          --msys=dvbs.
  -t, --mtype             Modulation Type = 16qam,32qam,64qam,128qam,256qam (required for DVB-C).
  -e, --eit               Do an EIT scan.
  -E, --eit_sid           Sid list = comma separated list of sid numbers. Example:
                          --eit_sid=1000,1002,3003.
  -x, --parse_mjd         Parse Date from Modified Julian Date.
  -v, --verbose           Set output to verbose level.
  -c, --printservices     Output result from services scan.
  -g, --exportservices    Export result from services scan.
  -d, --printevents       Output result from events scan.
  -h, --exportevents      Export result from events scan.
  --help                  Display this help screen.
  --version               Display version information.
  <server ip> (pos. 0)    Required. IP Address of SAT>IP server

* Notes on NIT scanning:
    With some cable providers or inhouse retransmission systems
    it may be not usable, i.e. due to wrong frequencies in the NIT.

* Notes on hardware depencies:
    Depending on hardware configuration the scan will succeed even if
    some required parameters are wrong. This will result in a channel list
    which is usable only on the same hardware configuration.

* Example: NIT based scan which should work on Unitymedia in Germany
    KON.OctoScan.NET --use_nit=true --freq=138 --msys=dvbc --sr=6900 --mtype=256qam 10.0.4.24
