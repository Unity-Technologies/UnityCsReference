// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{


[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
[System.Obsolete ("UnityEditor.AudioImporterFormat has been deprecated. Use UnityEngine.AudioCompressionFormat instead.")]
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

[System.Obsolete ("Setting and getting import channels is not used anymore (use forceToMono instead)", true)]
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

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AudioImporterSampleSettings
{
    public AudioClipLoadType        loadType;
    public AudioSampleRateSetting   sampleRateSetting;
    public uint                     sampleRateOverride;
    
    
    public AudioCompressionFormat   compressionFormat;
    
    
    public float                    quality;
    public int                      conversionMode;
}

public sealed partial class AudioImporter : AssetImporter
{
    public extern AudioImporterSampleSettings defaultSampleSettings
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public bool ContainsSampleSettingsOverride(string platform)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogError("Unknown platform passed to AudioImporter.ContainsSampleSettingsOverride (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'PSP2', 'PSM', 'XboxOne' or 'WSA'");
                return false;
            }

            return Internal_ContainsSampleSettingsOverride(platformGroup);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool Internal_ContainsSampleSettingsOverride (BuildTargetGroup platformGroup) ;

    public AudioImporterSampleSettings GetOverrideSampleSettings(string platform)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogError("Unknown platform passed to AudioImporter.GetOverrideSampleSettings (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'PSP2', 'PSM', 'XboxOne' or 'WSA'");
                return defaultSampleSettings;
            }

            return Internal_GetOverrideSampleSettings(platformGroup);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal AudioImporterSampleSettings Internal_GetOverrideSampleSettings (BuildTargetGroup platformGroup) ;

    public bool SetOverrideSampleSettings(string platform, AudioImporterSampleSettings settings)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogError("Unknown platform passed to AudioImporter.SetOverrideSampleSettings (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'PSP2', 'PSM', 'XboxOne' or 'WSA'");
                return false;
            }

            return Internal_SetOverrideSampleSettings(platformGroup, settings);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool Internal_SetOverrideSampleSettings (BuildTargetGroup platformGroup, AudioImporterSampleSettings settings) ;

    public bool ClearSampleSettingOverride(string platform)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogError("Unknown platform passed to AudioImporter.ClearSampleSettingOverride (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'PSP2', 'PSM', 'XboxOne' or 'WSA'");
                return false;
            }

            return Internal_ClearSampleSettingOverride(platformGroup);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool Internal_ClearSampleSettingOverride (BuildTargetGroup platform) ;

    public extern bool forceToMono
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public bool ambisonic
        {
            get
            {
                return Internal_GetAmbisonic();
            }
            set
            {
                Internal_SetAmbisonic(value);
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetAmbisonic (bool flag) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool Internal_GetAmbisonic () ;

    public bool loadInBackground
        {
            get
            {
                return Internal_GetLoadInBackground();
            }
            set
            {
                Internal_SetLoadInBackground(value);
            }
        }
    
    
    public bool preloadAudioData
        {
            get
            {
                return Internal_GetPreloadAudioData();
            }
            set
            {
                Internal_SetPreloadAudioData(value);
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetLoadInBackground (bool flag) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool Internal_GetLoadInBackground () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetPreloadAudioData (bool flag) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool Internal_GetPreloadAudioData () ;

    [System.Obsolete ("Setting and getting the compression format is not used anymore (use compressionFormat in defaultSampleSettings instead). Source audio file is assumed to be PCM Wav.")]
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
    
    
    
    [System.Obsolete ("Setting and getting import channels is not used anymore (use forceToMono instead)", true)]
    public AudioImporterChannels channels  { get { return 0; } set {} }
    
    
    [System.Obsolete ("AudioImporter.compressionBitrate is no longer supported", true)]
    public extern  int compressionBitrate
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("AudioImporter.loopable is no longer supported. All audio assets encoded by Unity are by default loopable.")]
    public extern  bool loopable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("AudioImporter.hardware is no longer supported. All mixing of audio is done by software and only some platforms use hardware acceleration to perform decoding.")]
    public extern  bool hardware
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Setting/Getting decompressOnLoad is deprecated. Use AudioImporterSampleSettings.loadType instead.")]
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
    
    
    [System.Obsolete ("AudioImporter.quality is no longer supported. Use AudioImporterSampleSettings.")]
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
    
    
    [System.Obsolete ("AudioImporter.threeD is no longer supported")]
    public extern  bool threeD
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("AudioImporter.updateOrigData is deprecated.", true)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void updateOrigData () ;

    [System.Obsolete ("AudioImporter.durationMS is deprecated.", true)]
    internal extern  int durationMS
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("AudioImporter.frequency is deprecated.", true)]
    internal extern  int frequency
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("AudioImporter.origChannelCount is deprecated.", true)]
    internal extern  int origChannelCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("AudioImporter.origIsCompressible is deprecated.", true)]
    internal extern  bool origIsCompressible
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("AudioImporter.origIsMonoForcable is deprecated.", true)]
    internal extern  bool origIsMonoForcable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("AudioImporter.minBitrate is deprecated.", true)]
internal int minBitrate(AudioType type) { return 0; }
    
    
    [System.Obsolete ("AudioImporter.maxBitrate is deprecated.", true)]
internal int maxBitrate(AudioType type) { return 0; }
    
    
    [System.Obsolete ("AudioImporter.defaultBitrate is deprecated.", true)]
    internal extern  int defaultBitrate
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("AudioImporter.origType is deprecated.", true)]
    internal AudioType origType { get { return 0; } }
    
    
    [System.Obsolete ("AudioImporter.origFileSize is deprecated.", true)]
    internal extern  int origFileSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  int origSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  int compSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

}
