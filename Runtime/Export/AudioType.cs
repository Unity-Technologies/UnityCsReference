// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    // Not used by new AudioClip, but still reguired for WWW.getAudioClip
    // Type of the imported(native) data
    public enum AudioType
    {
        // 3rd party / unknown plugin format.
        UNKNOWN = 0,
        //acc - not supported
        ACC = 1,         /* [Unity] Not supported/used. But kept here to keep the order of the enum in sync. */
        //aiff
        AIFF = 2,
        //  ASF = 3,             /* Microsoft Advanced Systems Format (ie WMA/ASF/WMV). */
        //  AT3 = 4,             /* Sony ATRAC 3 format */
        //  CDDA = 5,            /* Digital CD audio. */
        //  DLS = 6,             /* Sound font / downloadable sound bank. */
        //  FLAC = 7,            /* FLAC lossless codec. */
        //  FSB = 8,             /* FMOD Sample Bank. */
        //game cube ADPCM
        //    GCADPCM = 9,
        //impulse tracker
        IT = 10,
        //  MIDI = 11,            /* MIDI. */
        //Protracker / Fasttracker MOD.
        MOD = 12,
        //MP2/MP3 MPEG.
        MPEG = 13,
        //ogg vorbis
        OGGVORBIS = 14,
        //  PLAYLIST = 15,        /* Information only from ASX/PLS/M3U/WAX playlists */
        //  RAW = 16,             /* Raw PCM data. */
        // ScreamTracker 3.
        S3M = 17,
        //  SF2 = 18,             /* Sound font 2 format. */
        //  USER = 19,            /* User created sound. */
        //Microsoft WAV.
        WAV = 20,
        // FastTracker 2 XM.
        XM = 21,
        // XboxOne XMA(2)
        XMA = 22,
        VAG = 23,         /* PlayStation 2 / PlayStation Portable adpcm VAG format. */
        //iPhone hardware decoder, supports AAC, ALAC and MP3. Extracodecdata is a pointer to an FMOD_AUDIOQUEUE_EXTRACODECDATA structure.
        AUDIOQUEUE = 24,
        //  XWMA = 25,            /* Xbox360 XWMA */
        //  BCWAV = 26,           /* 3DS BCWAV container format for DSP ADPCM and PCM */
        //  AT9 = 27,             /* NGP ATRAC 9 format */

        // XBONE TODO: these are supported on xbone in hardware, do we care? XMA and XWMA are above and supported in hardware by xbone
        //PCM = 28,
        //ADPCM = 29,
    }
}
