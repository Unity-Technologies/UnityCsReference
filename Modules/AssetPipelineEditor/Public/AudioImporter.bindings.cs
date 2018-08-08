// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Obsolete("UnityEditor.AudioImporterFormat has been deprecated. Use UnityEngine.AudioCompressionFormat instead.")]
    public enum AudioImporterFormat
    {
        Native = -1,
        Compressed = 0
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Obsolete("UnityEditor.AudioImporterLoadType has been deprecated. Use UnityEngine.AudioClipLoadType instead (UnityUpgradable) -> [UnityEngine] UnityEngine.AudioClipLoadType", true)]
    public enum AudioImporterLoadType
    {
        DecompressOnLoad = -1,
        CompressedInMemory = -1,
        [System.Obsolete("UnityEditor.AudioImporterLoadType.StreamFromDisc has been deprecated. Use UnityEngine.AudioClipLoadType.Streaming instead (UnityUpgradable) -> UnityEngine.AudioClipLoadType.Streaming", true)]
        StreamFromDisc = -1
    }

    [System.Obsolete("Setting and getting import channels is not used anymore (use forceToMono instead)", true)]
    public enum AudioImporterChannels
    {
        Automatic = 0,
        Mono = 1,
        Stereo = 2,
    }

    public enum AudioSampleRateSetting
    {
        PreserveSampleRate = 0,
        OptimizeSampleRate = 1,
        OverrideSampleRate = 2
    }

    public partial struct AudioImporterSampleSettings
    {
        public AudioClipLoadType        loadType;
        public AudioSampleRateSetting   sampleRateSetting;
        public uint                     sampleRateOverride;

        public AudioCompressionFormat   compressionFormat;

        public float                    quality;
        public int                      conversionMode;
    }

    [NativeHeader("Modules/AssetPipelineEditor/Public/AudioImporter.h")]
    // Audio importer lets you modify [[AudioClip]] import settings from editor scripts.
    public sealed partial class AudioImporter : AssetImporter
    {
        public extern AudioImporterSampleSettings defaultSampleSettings { get; set; }

        public bool ContainsSampleSettingsOverride(string platform)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogError("Unknown platform passed to AudioImporter.ContainsSampleSettingsOverride (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'XboxOne' or 'WSA'");
                return false;
            }

            return Internal_ContainsSampleSettingsOverride(platformGroup);
        }

        [NativeName("ContainsSampleSettingsOverride")]
        internal extern bool Internal_ContainsSampleSettingsOverride(BuildTargetGroup platformGroup);

        public AudioImporterSampleSettings GetOverrideSampleSettings(string platform)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogError("Unknown platform passed to AudioImporter.GetOverrideSampleSettings (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'XboxOne' or 'WSA'");
                return defaultSampleSettings;
            }

            return Internal_GetOverrideSampleSettings(platformGroup);
        }

        [NativeName("GetTranslatedSettingsForPlatform")]
        internal extern AudioImporterSampleSettings Internal_GetOverrideSampleSettings(BuildTargetGroup platformGroup);

        public bool SetOverrideSampleSettings(string platform, AudioImporterSampleSettings settings)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogError("Unknown platform passed to AudioImporter.SetOverrideSampleSettings (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'XboxOne' or 'WSA'");
                return false;
            }

            return Internal_SetOverrideSampleSettings(platformGroup, settings);
        }

        [NativeName("SetSampleSettingsForPlatform")]
        internal extern bool Internal_SetOverrideSampleSettings(BuildTargetGroup platformGroup, AudioImporterSampleSettings settings);

        public bool ClearSampleSettingOverride(string platform)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogError("Unknown platform passed to AudioImporter.ClearSampleSettingOverride (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'XboxOne' or 'WSA'");
                return false;
            }

            return Internal_ClearSampleSettingOverride(platformGroup);
        }

        [NativeName("ClearSampleSettingOverride")]
        internal extern bool Internal_ClearSampleSettingOverride(BuildTargetGroup platform);

        // Force this clip to mono?
        public extern bool forceToMono { get; set; }

        // Is this clip ambisonic?
        public extern bool ambisonic { get; set; }

        //Set/get the way Unity is loading the Audio data.
        public extern bool loadInBackground { get; set; }

        public extern bool preloadAudioData { get; set; }

        [System.Obsolete("Setting and getting the compression format is not used anymore (use compressionFormat in defaultSampleSettings instead). Source audio file is assumed to be PCM Wav.")]
        AudioImporterFormat format
        {
            get
            {
                return (defaultSampleSettings.compressionFormat == AudioCompressionFormat.PCM) ?
                    AudioImporterFormat.Native : AudioImporterFormat.Compressed;
            }
            set
            {
                AudioImporterSampleSettings settings = defaultSampleSettings;
                settings.compressionFormat = (value == AudioImporterFormat.Native) ? AudioCompressionFormat.PCM : AudioCompressionFormat.Vorbis;
                defaultSampleSettings = settings;
            }
        }

        [System.Obsolete("Setting and getting import channels is not used anymore (use forceToMono instead)", true)]
        public AudioImporterChannels channels  { get { return 0; } set {} }

        // Compression bitrate.
        [System.Obsolete("AudioImporter.compressionBitrate is no longer supported", true)]
        public int compressionBitrate { get { return 0; } set {} }

        [System.Obsolete("AudioImporter.loopable is no longer supported. All audio assets encoded by Unity are by default loopable.")]
        public bool loopable { get { return true; } set {} }

        [System.Obsolete("AudioImporter.hardware is no longer supported. All mixing of audio is done by software and only some platforms use hardware acceleration to perform decoding.")]
        public bool hardware { get { return false; } set {} }

        // Should audio data be decompressed on load?
        [System.Obsolete("Setting/Getting decompressOnLoad is deprecated. Use AudioImporterSampleSettings.loadType instead.")]
        bool decompressOnLoad
        {
            get
            {
                return (defaultSampleSettings.loadType == AudioClipLoadType.DecompressOnLoad);
            }
            set
            {
                AudioImporterSampleSettings settings = defaultSampleSettings;
                settings.loadType = value ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
                defaultSampleSettings = settings;
            }
        }

        [System.Obsolete("AudioImporter.quality is no longer supported. Use AudioImporterSampleSettings.")]
        float quality
        {
            get
            {
                return defaultSampleSettings.quality;
            }
            set
            {
                AudioImporterSampleSettings settings = defaultSampleSettings;
                settings.quality = value;
                defaultSampleSettings =  settings;
            }
        }

        // Is this clip a 2D or 3D sound?
        [System.Obsolete("AudioImporter.threeD is no longer supported")]
        public bool threeD { get { return true; } set {} }

        //*undocumented* Update/cache audio info. important to call this before any of the next. you only need to call it once per audiofile
        [System.Obsolete("AudioImporter.updateOrigData is deprecated.", true)]
        internal void updateOrigData() {}

        //*undocumented* Duration of imported audio. call updateOrigData before
        [System.Obsolete("AudioImporter.durationMS is deprecated.", true)]
        internal int durationMS { get { return 0; } }

        // Frequency (sample rate) of imported audio.  call updateOrigData before
        [System.Obsolete("AudioImporter.frequency is deprecated.", true)]
        internal int frequency { get { return 0; } }

        // Original channel count.  call updateOrigData before
        [System.Obsolete("AudioImporter.origChannelCount is deprecated.", true)]
        internal int origChannelCount { get { return 0; } }

        // Is original source compressible to Ogg/MP3 (depending on platform)?  call updateOrigData before
        [System.Obsolete("AudioImporter.origIsCompressible is deprecated.", true)]
        internal bool origIsCompressible { get { return false; } }

        // Is original source forcable(?) to mono?  call updateOrigData before
        [System.Obsolete("AudioImporter.origIsMonoForcable is deprecated.", true)]
        internal bool origIsMonoForcable { get { return false; } }

        //*undocumented* Min bitrate for ogg/mp3 compression.  call updateOrigData before
        [System.Obsolete("AudioImporter.minBitrate is deprecated.", true)]
        internal int minBitrate(AudioType type) { return 0; }

        //*undocumented* Max bitrate for ogg/mp3 compression. call updateOrigData before
        [System.Obsolete("AudioImporter.maxBitrate is deprecated.", true)]
        internal int maxBitrate(AudioType type) { return 0; }

        [System.Obsolete("AudioImporter.defaultBitrate is deprecated.", true)]
        internal int defaultBitrate { get { return 0; } }

        //*undocumented* Get the format for automatic format
        [System.Obsolete("AudioImporter.origType is deprecated.", true)]
        internal AudioType origType { get { return 0; } }

        //*undocumented* Return the size of the original file. Note this is slow
        [System.Obsolete("AudioImporter.origFileSize is deprecated.", true)]
        internal int origFileSize { get { return 0; } }

        [NativeProperty("GetPreviewData().m_OrigSize", TargetType.Field)]
        internal extern int origSize
        {
            get;
        }

        [NativeProperty("GetPreviewData().m_CompSize", TargetType.Field)]
        internal extern int compSize
        {
            get;
        }
    }
}
