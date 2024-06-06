# KON.OctoScan.NET
.NET Port of octoscan (from DigitalDevices)
```KON.OctoScan.NET
Copyright (C) 2016 Digital Devices GmbH
Copyright (C) 2024 Oswald Oliver

  --mode                     Required. Execution mode / Operation selection: scan/parsedate
  --mjd                      Parse Date from Modified Julian Date.
  --frequencies              Frequencies in MHz.
                             Example: --frequencies=378
  --modulationsystem         Required. Modulation System.
                             Example: --modulationsystem=dvbc
  --modulationtype           Modulation Type (required for DVB-C).
                             Example: --modulationtype=t256qam
  --symbolrate               Symbolrate in kSymbols (required for DVB-S/S2 and DVB-C).
                             Example: --symbolrate=6900
  --transpondertimeout       Timeout in seconds for transponder scanning.
                             Example: --transpondertimeout=30
  --satellitesource          Satellite source 1,2,3,4 (required for DVB-S/S2).
                             Example: --satellitesource=1
  --polarisation             Polarisation v,h,r,l (required for DVB-S/S2).
                             Example: --polarisation=v
  --bandwidth                Bandwidth 1.712,5,6,7,8,10 (required for DVB-T/T2).
                             Example: --bandwidth=8
  --nit                      Use network information table. If specified additional transponders
                             will be scanned from network information table.
  --eit                      Use event information table. If specified event information will be
                             collected from transponders.
  --eitfiltersid             Comma separated list of services that will be looked up from event
                             information table.
                             Example: --eitfiltersid=1000,1002,3003
  --imagestreamidentifier    ImageStreamIdentifier for physical layer pipe.
                             Example: --imagestreamidentifier=1
  --printservices            Output result from services scan.
  --exportservices           Export result from services scan.
  --exportservicesexcel      Export result from services scan to excel file.
  --printevents              Output result from events scan.
  --exportevents             Export result from events scan.
  --exporteventsexcel        Export result from events scan to excel file.
  --verbose                  Set output to verbose level.
  --help                     Display this help screen.
  --version                  Display version information.
  <server ip> (pos. 0)       Required. IP Address of SAT>IP server

* Notes on NIT scanning:
    With some cable providers or inhouse retransmission systems
    it may be not usable, i.e. due to wrong frequencies in the NIT.

* Notes on hardware depencies:
    Depending on hardware configuration the scan will succeed even if
    some required parameters are wrong. This will result in a channel list
    which is usable only on the same hardware configuration.

* Example: NIT based scan which should work on Unitymedia in Germany
    KON.OctoScan.NET --mode scan --frequencies=138 --modulationsystem=dvbc
                     --symbolrate=6900 --modulationtype=t256qam --nit 10.0.4.24