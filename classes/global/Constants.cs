namespace KON.OctoScan.NET;

public static class Constants
{
    public static readonly string cstrCurrentProjectName = "KON.OctoScan.NET";
    public static readonly string cstrCurrentProjectCopyrightLine1 = "Copyright (C) 2016 Digital Devices GmbH";
    public static readonly string cstrCurrentProjectCopyrightLine2 = "Copyright (C) 2024-2026 Oswald Oliver";

    public const string FIREWALL_RULE_APP_NAME = "KON.OctoScan.NET";

    public const int MAX_EIT_SID = 64;
    public const int RETRY_WAITTIME = 1000;
    public const string RTSP_VERSION = "RTSP/1.0";
    public const string RTSP_NEWLINE = "\r\n";
    public const int RTSP_RECEIVE_TIMEOUT = 5000;
    public const int RTSP_SETUP_TIMEOUT = 15000;
    public const int RTSP_SLOW_RESPONSE_WARNING = 2000;
    public const int CHANNEL_CHANGE_SETTLE_TIME = 300;
    public const int CHANNEL_CHANGE_MAX_WAIT = 3000;
    public const int STALE_TRANSPORT_STREAM_WINDOW = 5000;
    public const int RTSP_RESPONSE_COMPLETION_WAIT = 250;
    public const int RTSP_DEFAULT_SESSION_TIMEOUT = 60;
    public const int PID_UPDATE_MIN_INTERVAL = 250;
    public const int MAX_PID_LIST_LENGTH = 300;
    public const int FILTER_TIMEOUT_PAT = 5;
    public const int FILTER_TIMEOUT_SDT = 5;
    public const int FILTER_TIMEOUT_PMT = 5;
    public const int FILTER_TIMEOUT_NIT = 15;
    public const int FILTER_TIMEOUT_EIT = 15;
    public const int FILTER_TIMEOUT_EIT_SCHEDULE = 45;
    public const int FILTER_TIMEOUT_PMT_SECOND_PASS = 30;

    // Scrambling probe - a few packets of one elementary stream per service show whether it is actually scrambled
    // (transport_scrambling_control). 0xFF is never a real table_id (stuffing), it marks the probe filters.
    public const byte TID_SCRAMBLING_PROBE = 0xFF;
    public const int SCRAMBLING_PROBE_PACKETS = 10;
    public const int FILTER_TIMEOUT_SCRAMBLING_PROBE = 5;
    public const int PID_REFRESH_TIMEOUT = 1000;
    public const int MAX_PID_REFRESHES = 2;
    public static readonly int[] RTSP_TRANSIENT_STATUS_CODES = [408, 453, 454, 500, 503];
    public const int RTP_HEADER_LENGTH = 12;
    public const int RTCP_PACKET_TYPE_APP = 204;
    public const int RTP_CLOCK_TICKS_PER_MS = 90;
    public const int RTP_CLOCK_MAX_JUMP = 5000;
    public const int RTP_CLOCK_STALL_LIMIT = 2000;
    public const int RTP_CLOCK_MAX_REORDER = 1000;
    public const int RTP_CLOCK_MAX_DISCONTINUITIES = 10;
    public const int RTP_CLOCK_MAX_AHEAD = 500;
    public const int RTP_CLOCK_MAX_AHEAD_FALLBACK = 1000;
    public const int TRANSPONDER_SCAN_RETRIES = 3;
    public const int TUNER_LOCK_GRACE_TIME = 3000;
    public const int UDP_PORT_PAIR_BIND_ATTEMPTS = 10;
    public const int UDP_RECEIVE_BUFFER_SIZE = 4 * 1024 * 1024;
    public const int UDP_DATAGRAM_BUFFER_SIZE = 65536;
    public const int TS_PACKET_LENGTH = 188;
    public const byte TS_SYNC_BYTE = 0x47;
    public const int TS_PID_COUNT = 8192;
    public const int MAX_SECTION_LENGTH = 4096;
    public const int DEFAULT_PID_PAT = 0x00;
    public const int DEFAULT_PID_NIT = 0x10;
    public const int DEFAULT_PID_SDT = 0x11;
    public const int DEFAULT_PID_EIT = 0x12;
    public const int DEFAULT_TID_PA = 0x00;
    public const int DEFAULT_TID_PM = 0x02;
    public const int DEFAULT_TID_NI_CURRENT = 0x40;
    public const int DEFAULT_TID_SD = 0x42;
    public const int DEFAULT_TID_EI = 0x50;
    public const int DESCRIPTORTAG_CA_DESCRIPTOR = 0x09;
    public const int DESCRIPTORTAG_SERVICE_DESCRIPTOR = 0x48;
    public const int DESCRIPTORTAG_SHORT_EVENT_DESCRIPTOR = 0x4D;
    public const int DESCRIPTORTAG_SATELLITE_DELIVERY_SYSTEM_DESCRIPTOR = 0x43;
    public const int DESCRIPTORTAG_CABLE_DELIVERY_SYSTEM_DESCRIPTOR = 0x44;
    public static readonly byte[] EXTENSION_DESCRIPTOR_AUDIO_TAGS = [0x06, 0x0E, 0x0F, 0x15, 0x21];

    // Codec of an audio track (descriptor of the PMT, otherwise stream type) - AC3, EAC3 and AC4 are Dolby
    public const string AUDIO_CODEC_MPEG = "MPEG";
    public const string AUDIO_CODEC_AAC = "AAC";
    public const string AUDIO_CODEC_AC3 = "AC3";
    public const string AUDIO_CODEC_EAC3 = "EAC3";
    public const string AUDIO_CODEC_AC4 = "AC4";
    public const string AUDIO_CODEC_DTS = "DTS";
    public const string AUDIO_CODEC_TRUEHD = "TRUEHD";
    public const string AUDIO_CODEC_UNKNOWN = "UNKNOWN";
    public static readonly string[] ModulationSystem2String = ["undef", "dvbc", "dvbcb", "dvbt", "dss", "dvbs", "dvbs2", "dvbh", "isdbt", "isdbs", "isdbc", "atsc", "atscmh", "dtmb", "cmmb", "dab", "dvbt2", "turbo", "dvbcc", "dvbc2"];
    public static readonly string[] ModulationType2String = ["qpsk", "16qam", "32qam", "64qam", "128qam", "256qam", "autoqam", "8vsb", "16vsb", "8psk", "16apsk", "32apsk", "dqpsk", "4qamnr"];
    public static readonly string[] Polarisation2String = ["v", "h", "r", "l"];
    public static readonly string[] Bandwidth2String = ["8", "7", "6", "auto", "5", "10", "1.712"];
    public const int CCT_ENCODING_ISO_8859_5 = 0x01;
    public const int CCT_ENCODING_ISO_8859_15 = 0x0B;
    public const int CCT_ENCODING_ISO_8859_DYNAMIC_PARTS = 0x10;
    public const int CCT_ENCODING_ISO_10646 = 0x11;
    public const int CCT_ENCODING_KSX_1001 = 0x12;
    public const int CCT_ENCODING_GB_2312 = 0x13;
    public const int CCT_ENCODING_ISO_10646_BIG5 = 0x14;
    public const int CCT_ENCODING_ISO_10646_UTF8 = 0x15;
    public const int CCT_ENCODING_CUSTOM = 0x1F;
    public const int CCT_ENCODING_ISO_6937 = 0x20;
    public static readonly uint[] uiaDVBCRCChecksumTable = [
        0x00000000, 0x04c11db7, 0x09823b6e, 0x0d4326d9, 0x130476dc, 0x17c56b6b,
        0x1a864db2, 0x1e475005, 0x2608edb8, 0x22c9f00f, 0x2f8ad6d6, 0x2b4bcb61,
        0x350c9b64, 0x31cd86d3, 0x3c8ea00a, 0x384fbdbd, 0x4c11db70, 0x48d0c6c7,
        0x4593e01e, 0x4152fda9, 0x5f15adac, 0x5bd4b01b, 0x569796c2, 0x52568b75,
        0x6a1936c8, 0x6ed82b7f, 0x639b0da6, 0x675a1011, 0x791d4014, 0x7ddc5da3,
        0x709f7b7a, 0x745e66cd, 0x9823b6e0, 0x9ce2ab57, 0x91a18d8e, 0x95609039,
        0x8b27c03c, 0x8fe6dd8b, 0x82a5fb52, 0x8664e6e5, 0xbe2b5b58, 0xbaea46ef,
        0xb7a96036, 0xb3687d81, 0xad2f2d84, 0xa9ee3033, 0xa4ad16ea, 0xa06c0b5d,
        0xd4326d90, 0xd0f37027, 0xddb056fe, 0xd9714b49, 0xc7361b4c, 0xc3f706fb,
        0xceb42022, 0xca753d95, 0xf23a8028, 0xf6fb9d9f, 0xfbb8bb46, 0xff79a6f1,
        0xe13ef6f4, 0xe5ffeb43, 0xe8bccd9a, 0xec7dd02d, 0x34867077, 0x30476dc0,
        0x3d044b19, 0x39c556ae, 0x278206ab, 0x23431b1c, 0x2e003dc5, 0x2ac12072,
        0x128e9dcf, 0x164f8078, 0x1b0ca6a1, 0x1fcdbb16, 0x018aeb13, 0x054bf6a4,
        0x0808d07d, 0x0cc9cdca, 0x7897ab07, 0x7c56b6b0, 0x71159069, 0x75d48dde,
        0x6b93dddb, 0x6f52c06c, 0x6211e6b5, 0x66d0fb02, 0x5e9f46bf, 0x5a5e5b08,
        0x571d7dd1, 0x53dc6066, 0x4d9b3063, 0x495a2dd4, 0x44190b0d, 0x40d816ba,
        0xaca5c697, 0xa864db20, 0xa527fdf9, 0xa1e6e04e, 0xbfa1b04b, 0xbb60adfc,
        0xb6238b25, 0xb2e29692, 0x8aad2b2f, 0x8e6c3698, 0x832f1041, 0x87ee0df6,
        0x99a95df3, 0x9d684044, 0x902b669d, 0x94ea7b2a, 0xe0b41de7, 0xe4750050,
        0xe9362689, 0xedf73b3e, 0xf3b06b3b, 0xf771768c, 0xfa325055, 0xfef34de2,
        0xc6bcf05f, 0xc27dede8, 0xcf3ecb31, 0xcbffd686, 0xd5b88683, 0xd1799b34,
        0xdc3abded, 0xd8fba05a, 0x690ce0ee, 0x6dcdfd59, 0x608edb80, 0x644fc637,
        0x7a089632, 0x7ec98b85, 0x738aad5c, 0x774bb0eb, 0x4f040d56, 0x4bc510e1,
        0x46863638, 0x42472b8f, 0x5c007b8a, 0x58c1663d, 0x558240e4, 0x51435d53,
        0x251d3b9e, 0x21dc2629, 0x2c9f00f0, 0x285e1d47, 0x36194d42, 0x32d850f5,
        0x3f9b762c, 0x3b5a6b9b, 0x0315d626, 0x07d4cb91, 0x0a97ed48, 0x0e56f0ff,
        0x1011a0fa, 0x14d0bd4d, 0x19939b94, 0x1d528623, 0xf12f560e, 0xf5ee4bb9,
        0xf8ad6d60, 0xfc6c70d7, 0xe22b20d2, 0xe6ea3d65, 0xeba91bbc, 0xef68060b,
        0xd727bbb6, 0xd3e6a601, 0xdea580d8, 0xda649d6f, 0xc423cd6a, 0xc0e2d0dd,
        0xcda1f604, 0xc960ebb3, 0xbd3e8d7e, 0xb9ff90c9, 0xb4bcb610, 0xb07daba7,
        0xae3afba2, 0xaafbe615, 0xa7b8c0cc, 0xa379dd7b, 0x9b3660c6, 0x9ff77d71,
        0x92b45ba8, 0x9675461f, 0x8832161a, 0x8cf30bad, 0x81b02d74, 0x857130c3,
        0x5d8a9099, 0x594b8d2e, 0x5408abf7, 0x50c9b640, 0x4e8ee645, 0x4a4ffbf2,
        0x470cdd2b, 0x43cdc09c, 0x7b827d21, 0x7f436096, 0x7200464f, 0x76c15bf8,
        0x68860bfd, 0x6c47164a, 0x61043093, 0x65c52d24, 0x119b4be9, 0x155a565e,
        0x18197087, 0x1cd86d30, 0x029f3d35, 0x065e2082, 0x0b1d065b, 0x0fdc1bec,
        0x3793a651, 0x3352bbe6, 0x3e119d3f, 0x3ad08088, 0x2497d08d, 0x2056cd3a,
        0x2d15ebe3, 0x29d4f654, 0xc5a92679, 0xc1683bce, 0xcc2b1d17, 0xc8ea00a0,
        0xd6ad50a5, 0xd26c4d12, 0xdf2f6bcb, 0xdbee767c, 0xe3a1cbc1, 0xe760d676,
        0xea23f0af, 0xeee2ed18, 0xf0a5bd1d, 0xf464a0aa, 0xf9278673, 0xfde69bc4,
        0x89b8fd09, 0x8d79e0be, 0x803ac667, 0x84fbdbd0, 0x9abc8bd5, 0x9e7d9662,
        0x933eb0bb, 0x97ffad0c, 0xafb010b1, 0xab710d06, 0xa6322bdf, 0xa2f33668,
        0xbcb4666d, 0xb8757bda, 0xb5365d03, 0xb1f740b4 ];
    public const float EXCEL_PIXEL_TO_WIDTH_CONSTANT = (float)(8.43 / 64);
    public const float EXCEL_CHARACTER_TO_WIDTH_CONSTANT = 8 * EXCEL_PIXEL_TO_WIDTH_CONSTANT;
    public const float EXCEL_MAXIMUM_COLUMN_WIDTH = 100;
}