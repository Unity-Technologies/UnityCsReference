// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{


public enum VideoCodec
{
    Auto = 0,
    H264 = 1,
    VP8  = 2
}

public enum VideoBitrateMode
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum VideoDeinterlaceMode
{
    Off = 0,
    Even = 1,
    Odd = 2
}

public enum VideoResizeMode
{
    OriginalSize = 0,
    ThreeQuarterRes = 1,
    HalfRes = 2,
    QuarterRes = 3,
    Square1024 = 4,
    Square512 = 5,
    Square256 = 6,
    CustomSize = 7
}

public enum VideoSpatialQuality
{
    LowSpatialQuality = 0,
    MediumSpatialQuality = 1,
    HighSpatialQuality = 2
}

public enum VideoEncodeAspectRatio
{
    NoScaling = 0,
    Stretch   = 5
}

[RequiredByNativeCode]
public sealed partial class VideoImporterTargetSettings
{
    public bool                   enableTranscoding;
    public VideoCodec             codec;
    public VideoResizeMode        resizeMode;
    public VideoEncodeAspectRatio aspectRatio;
    public int                    customWidth;
    public int                    customHeight;
    public VideoBitrateMode       bitrateMode;
    public VideoSpatialQuality    spatialQuality;
}

public sealed partial class VideoClipImporter : AssetImporter
{
    public extern float quality
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool linearColor
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useLegacyImporter
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern ulong sourceFileSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern ulong outputFileSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int frameCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern double frameRate
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern bool keepAlpha
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool sourceHasAlpha
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern VideoDeinterlaceMode deinterlaceMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool flipVertical
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool flipHorizontal
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool importAudio
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public VideoImporterTargetSettings defaultTargetSettings
        {
            get { return GetTargetSettings(VideoClipImporter.defaultTargetName); }
            set { SetTargetSettings(VideoClipImporter.defaultTargetName, value); }
        }
    
    
    public VideoImporterTargetSettings GetTargetSettings(string platform)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (!platform.Equals(VideoClipImporter.defaultTargetName, StringComparison.OrdinalIgnoreCase) && platformGroup == BuildTargetGroup.Unknown)
            {
                throw new ArgumentException("Unknown platform passed to AudioImporter.GetOverrideSampleSettings (" + platform + "), please use one of " +
                    "'Default', 'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'PSP2', 'PSM', 'XBox360', 'XboxOne', 'WP8', or 'WSA'");
            }

            return Internal_GetTargetSettings(platformGroup);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal VideoImporterTargetSettings Internal_GetTargetSettings (BuildTargetGroup group) ;

    public void SetTargetSettings(string platform, VideoImporterTargetSettings settings)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (!platform.Equals(VideoClipImporter.defaultTargetName, StringComparison.OrdinalIgnoreCase) && platformGroup == BuildTargetGroup.Unknown)
            {
                throw new ArgumentException("Unknown platform passed to AudioImporter.GetOverrideSampleSettings (" + platform + "), please use one of " +
                    "'Default', 'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'PSP2', 'PSM', 'XBox360', 'XboxOne', 'WP8', or 'WSA'");
            }

            Internal_SetTargetSettings(platformGroup, settings);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void Internal_SetTargetSettings (BuildTargetGroup group, VideoImporterTargetSettings settings) ;

    public void ClearTargetSettings(string platform)
        {
            if (platform.Equals(VideoClipImporter.defaultTargetName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Cannot clear the Default VideoClipTargetSettings.");

            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                throw new ArgumentException("Unknown platform passed to AudioImporter.GetOverrideSampleSettings (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'PSP2', 'PSM', 'XBox360', 'XboxOne', 'WP8', or 'WSA'");
            }

            Internal_ClearTargetSettings(platformGroup);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void Internal_ClearTargetSettings (BuildTargetGroup group) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void PlayPreview () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void StopPreview () ;

    public extern bool isPlayingPreview
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Texture GetPreviewTexture () ;

    internal extern static string defaultTargetName
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool EqualsDefaultTargetSettings (VideoImporterTargetSettings settings) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public string GetResizeModeName (VideoResizeMode mode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetResizeWidth (VideoResizeMode mode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetResizeHeight (VideoResizeMode mode) ;

    public extern ushort sourceAudioTrackCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public ushort GetSourceAudioChannelCount (ushort audioTrackIdx) {
        return INTERNAL_CALL_GetSourceAudioChannelCount ( this, audioTrackIdx );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static ushort INTERNAL_CALL_GetSourceAudioChannelCount (VideoClipImporter self, ushort audioTrackIdx);
    public uint GetSourceAudioSampleRate (ushort audioTrackIdx) {
        return INTERNAL_CALL_GetSourceAudioSampleRate ( this, audioTrackIdx );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static uint INTERNAL_CALL_GetSourceAudioSampleRate (VideoClipImporter self, ushort audioTrackIdx);
    public extern int pixelAspectRatioNumerator
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int pixelAspectRatioDenominator
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern bool transcodeSkipped
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

}
