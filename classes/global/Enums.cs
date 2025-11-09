namespace KON.OctoScan.NET
{
    public static class Enums
    {
        public enum DVBTableSection : byte
        {
            PMT = 0x02,
            //CAT = 0x01,
            //NIT = 0x40,
            //SDT = 0x42,
            //EIT = 0x4E,
            //TDT = 0x70,
            //TOT = 0x73
        }
        
        public enum ElementaryStreamDescriptors : byte
        {
            video_stream_descriptor                         = 0x02,
            audio_stream_descriptor                         = 0x03,
            data_stream_alignment_descriptor                = 0x06,
            ISO_639_language_descriptor                     = 0x0A,
            system_clock_descriptor                         = 0x0B,
            carousel_identifier_descriptor                  = 0x13,
            AVC_video_descriptor                            = 0x28,
            VBI_data_descriptor                             = 0x45,
            VBI_teletext_descriptor                         = 0x46,
            mosaic_descriptor                               = 0x51,
            stream_identifier_descriptor                    = 0x52,
            teletext_descriptor                             = 0x56,
            subtitling_descriptor                           = 0x59,
            private_data_specifier_descriptor               = 0x5F,
            service_move_descriptor                         = 0x60,
            scrambling_descriptor                           = 0x65,
            data_broadcast_id_descriptor                    = 0x66,
            AC_3_descriptor                                 = 0x6A,
            ancillary_data_descriptor                       = 0x6B,
            application_signalling_descriptor               = 0x6F,
            adaptation_field_data_descriptor                = 0x70,
            related_content_descriptor                      = 0x74,
            ECM_repetition_rate_descriptor                  = 0x78,
            enhanced_ac3_descriptor                         = 0x7A,
            DTS_descriptor                                  = 0x7B,
            AAC_descriptor                                  = 0x7C,
            XAIT_location_descriptor                        = 0x7D,
            extension_descriptor                            = 0x7F
        }

        public enum MPEG2TSStreamType : byte
        {
            ITU_T_ISO_IEC_Reserved_Data                         = 0x00,     // ITU-T | ISO/IEC Reserved Data
            ISO_IEC_11172_2_MPEG1_Video                         = 0x01,     // ISO/IEC 11172-2 MPEG1 Video
            ITU_T_Rec_H_262_ISO_IEC_13818_2_MPEG2_Video         = 0x02,     // ITU-T Rec. H.262 | ISO/IEC 13818-2 MPEG2 Video
            ISO_IEC_11172_3_MPEG1_Audio                         = 0x03,     // ISO/IEC 11172-3 MPEG1 Audio
            ISO_IEC_13818_3_MPEG2_Audio                         = 0x04,     // ISO/IEC 13818-3 MPEG2 Audio
            ITU_T_Rec_H_222_0_ISO_IEC_13818_1_Data              = 0x05,     // ITU-T Rec. H.222.0 | ISO/IEC 13818-1 Private Sections Data
            ITU_T_Rec_H_222_0_ISO_IEC_13818_1_PES_Audio_Data    = 0x06,     // ITU-T Rec. H.222.0 | ISO/IEC 13818-1 PES Private Audio & Data
            ISO_IEC_13522_MHEG_Audio_Video                      = 0x07,     // ISO/IEC 13522 MHEG Audio & Video
            ITU_T_Rec_H_222_0_ISO_IEC_13818_1_AnnexA_Data       = 0x08,     // ITU-T Rec. H.222.0 | ISO/IEC 13818-1 Annex A DSM-CC Data
            ITU_T_Rec_H_222_1_Data                              = 0x09,     // ITU-T Rec. H.222.1
            ISO_IEC_13818_6_Type_A_Data                         = 0x0A,     // ISO/IEC 13818-6 type A Data
            ISO_IEC_13818_6_Type_B_Data                         = 0x0B,     // ISO/IEC 13818-6 type B Data
            ISO_IEC_13818_6_Type_C_Data                         = 0x0C,     // ISO/IEC 13818-6 type C Data
            ISO_IEC_13818_6_Type_D_Data                         = 0x0D,     // ISO/IEC 13818-6 type D Data
            ITU_T_Rec_H_222_0_ISO_IEC_13818_1_Auxiliary_Data    = 0x0E,     // ITU-T Rec. H.222.0 | ISO/IEC 13818-1 Auxiliary Data
            ISO_IEC_13818_7_AAC_Audio                           = 0x0F,     // ISO/IEC 13818-7 AAC Audio
            ISO_IEC_14496_2_MPEG4_Video                         = 0x10,     // ISO/IEC 14496-2 MPEG4 Video
            ISO_IEC_14496_3_LATM_AAC_Audio                      = 0x11,     // ISO/IEC 14496-3 LATM AAC Audio
            ISO_IEC_14496_1_SL_PES_Data                         = 0x12,     // ISO/IEC 14496-1 SL PES Data
            ISO_IEC_14496_1_SL_Sections_Data                    = 0x13,     // ISO/IEC 14496-1 SL Sections Data
            ISO_IEC_13818_6_SDP_Data                            = 0x14,     // ISO/IEC 13818-6 Synchronized Download Protocol Data
            Metadata_PES_Data                                   = 0x15,     // Metadata PES Packets Data
            Metadata_Sectioned_Data                             = 0x16,     // Metadata Sections Packets Data
            Metadata_ISO_IEC_13818_6_Data_Carousel_Data         = 0x17,     // Metadata ISO/IEC 13818-6 Data Carousel Data
            Metadata_ISO_IEC_13818_6_Object_Carousel_Data       = 0x18,     // Metadata ISO/IEC 13818-6 Object Carousel Data
            Metadata_ISO_IEC_13818_6_SDP_Data                   = 0x19,     // Metadata ISO/IEC 13818-6 Synchronized Download Protocol Data
            IPMP_Stream_MPEG2_Video                             = 0x1A,     // IPMP Stream ISO/IEC 13818-11 MPEG2 IPMP Video
            AVC_Stream_H264_Video                               = 0x1B,     // AVC ITU-T Rec.H.264 | ISO/IEC 14496-10 H264 Video
            ITU_T_Rec_H_222_0_ISO_IEC_13818_1_Reserved_Audio    = 0x1C,     // ITU-T Rec. H.222.0 | ISO/IEC 13818-1 Reserved Audio
            ISO_IEC_14496_17_MPEG4_Text_Data                    = 0x1D,     // ISO/IEC 14496-17 MPEG-4 Text Data
            ISO_IEC_23002_3_MPEG4_Auxiliary_Video               = 0x1E,     // ISO/IEC 23002-3 MPEG-4 Auxiliary Video
            ISO_IEC_14496_10_SVC_MPEG4_AVC_Sub_Bitstream_Video  = 0x1F,     // ISO/IEC 14496-10 SVC MPEG-4 AVC Sub-Bitstream Video
            ISO_IEC_14496_10_MVC_MPEG4_AVC_Sub_Bitstream_Video  = 0x20,     // ISO/IEC 14496-10 MVC MPEG-4 MVC Sub-Bitstream Video
            ITU_T_Rec_T_800_ISO_IEC_15444_JPEG_2000_Video       = 0x21,     // ITU-T Rec. T.800 | ISO/IEC 15444 JPEG 2000 Video
            H265_HEVC_Video                                     = 0x24,     // H.265 HEVC Video
            Chinese_Video_Standard_CAVS_Video                   = 0x42,     // Chinese Video Standard CAVS Video
            IPMP_Stream_DRM_Data                                = 0x7F,     // IPMP Stream DRM Data
            DigiCipher_II_Video                                 = 0x80,     // DigiCipher II Video
            A52_AC3_Audio                                       = 0x81,     // A/52 AC-3 Audio
            HDMV_DTS_Audio                                      = 0x82,     // HDMV DTS Audio
            LPCM_TrueHD_Audio                                   = 0x83,     // LPCM TrueHD Audio
            SDDS_Audio                                          = 0x84,     // SDDS Audio
            ATSC_Program_ID_Data                                = 0x85,     // ATSC Program ID Data
            DTS_HD_Audio                                        = 0x86,     // DTS-HD Audio
            EAC3_Audio                                          = 0x87,     // E-AC-3 Audio
            DTS_Audio                                           = 0x8A,     // DTS Audio
            Presentation_Graphic_Stream_Subtitle_Data           = 0x90,     // Presentation Graphic Stream Subtitle Data
            A52B_AC3_Audio                                      = 0x91,     // A/52B AC-3 Audio
            DVD_SPU_VLS_Subtitle_Data                           = 0x92,     // DVD SPU VLS Subtitle Data
            SDDS_V2_Audio                                       = 0x94,     // SDDS V2 Audio
            MSCODEC_Video                                       = 0xA0,     // MS-Codec Video
            BBC_Dirac_Ultra_HD_Video                            = 0xD1,     // BBC Dirac Ultra HD Video
            Private_ES_VC1_Video                                = 0xEA,     // Private ES VC-1 Video
        }
    }
}