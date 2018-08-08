// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor
{

    // AssetImporter for importing VideoClip
    [NativeHeader("Editor/Src/Video/VideoClipTranscode.h")]
    public enum VideoCodec
    {
        Auto = 0,
        H264 = 1,
        VP8  = 2
    }

    [NativeHeader("Modules/Video/Public/Base/VideoMediaTypes.h")]
    public enum VideoBitrateMode
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    [NativeHeader("Editor/Src/Video/VideoClipTranscode.h")]
    public enum VideoDeinterlaceMode
    {
        Off = 0,
        Even = 1,
        Odd = 2
    }

    [NativeHeader("Modules/AssetPipelineEditor/Public/VideoClipImporter.h")]
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

    [NativeHeader("Editor/Src/Video/VideoClipTranscode.h")]
    public enum VideoSpatialQuality
    {
        LowSpatialQuality = 0,
        MediumSpatialQuality = 1,
        HighSpatialQuality = 2
    }

    [NativeHeader("Modules/AssetPipelineEditor/Public/VideoClipImporter.h")]
    public enum VideoEncodeAspectRatio
    {
        NoScaling = 0,
        Stretch   = 5
    }

    [RequiredByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/AssetPipelineEditor/Public/VideoClipImporter.h")]
    public partial class VideoImporterTargetSettings
    {
        public bool                   enableTranscoding;
        public VideoCodec             codec;
        [NativeName("resizeFormat")]
        public VideoResizeMode        resizeMode;
        public VideoEncodeAspectRatio aspectRatio;
        public int                    customWidth;
        public int                    customHeight;
        public VideoBitrateMode       bitrateMode;
        public VideoSpatialQuality    spatialQuality;
    }

    [NativeConditional("ENABLE_VIDEO")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/VideoClipImporter.h")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/VideoClipImporter.bindings.h")]
    public partial class VideoClipImporter : AssetImporter
    {
        // Quality setting to use when importing the movie. This is a float value from 0 to 1.
        public extern float quality { get; set; }

        // Is this a linear texture or an sRGB texture (Only used when performing linear rendering)
        [NativeProperty("ColorLinear")]
        public extern bool linearColor { get; set; }

        // Import a MovieTexture
        [NativeProperty("LegacyImporter")]
        public extern bool useLegacyImporter { get; set; }

        public extern ulong sourceFileSize { get; }
        public extern ulong outputFileSize { get; }

        public extern int frameCount { get; }

        public extern double frameRate { get; }

        // Encode RGB / RGBA Video
        [NativeProperty("EncodeAlpha")]
        public extern bool keepAlpha { get; set; }
        public extern bool sourceHasAlpha { get; }

        // Interlaced / Progressive Video
        [NativeProperty("Deinterlace")]
        public extern VideoDeinterlaceMode deinterlaceMode { get; set; }

        // Flip Image Vertically
        public extern bool flipVertical { get; set; }
        // Flip Image Horizontal
        public extern bool flipHorizontal { get; set; }

        // Import Audio
        public extern bool importAudio { get; set; }

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
                    "'Default', 'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'XBox360', 'XboxOne', 'WP8', or 'WSA'");
            }

            return Internal_GetTargetSettings(platformGroup);
        }

        internal VideoImporterTargetSettings Internal_GetTargetSettings(BuildTargetGroup group)
        {
            return Private_GetTargetSettings(group) as VideoImporterTargetSettings;
        }

        [FreeFunction(Name = "VideoImporterBindings::GetTargetSettings", HasExplicitThis = true)]
        private extern object Private_GetTargetSettings(BuildTargetGroup group);

        public void SetTargetSettings(string platform, VideoImporterTargetSettings settings)
        {
            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (!platform.Equals(VideoClipImporter.defaultTargetName, StringComparison.OrdinalIgnoreCase) && platformGroup == BuildTargetGroup.Unknown)
            {
                throw new ArgumentException("Unknown platform passed to AudioImporter.GetOverrideSampleSettings (" + platform + "), please use one of " +
                    "'Default', 'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'XBox360', 'XboxOne', 'WP8', or 'WSA'");
            }

            Internal_SetTargetSettings(platformGroup, settings);
        }

        [NativeName("SetTargetSettings")]
        internal extern void Internal_SetTargetSettings(BuildTargetGroup group, VideoImporterTargetSettings settings);

        public void ClearTargetSettings(string platform)
        {
            if (platform.Equals(VideoClipImporter.defaultTargetName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Cannot clear the Default VideoClipTargetSettings.");

            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroupByName(platform);
            if (platformGroup == BuildTargetGroup.Unknown)
            {
                throw new ArgumentException("Unknown platform passed to AudioImporter.GetOverrideSampleSettings (" + platform + "), please use one of " +
                    "'Web', 'Standalone', 'iOS', 'Android', 'WebGL', 'PS4', 'XBox360', 'XboxOne', 'WP8', or 'WSA'");
            }

            Internal_ClearTargetSettings(platformGroup);
        }

        [NativeName("ClearTargetSettings")]
        internal extern void Internal_ClearTargetSettings(BuildTargetGroup group);

        // Preview
        [NativeName("StartPreview")]
        public extern void PlayPreview();
        public extern void StopPreview();
        public extern bool isPlayingPreview
        {
            [NativeName("Started")]
            get;
        }
        public extern Texture GetPreviewTexture();

        internal extern static string defaultTargetName
        {
            [NativeName("DefaultSettingsName")]
            get;
        }

        [FreeFunction("VideoImporterBindings::EqualsDefaultTargetSettings", HasExplicitThis = true)]
        internal extern bool EqualsDefaultTargetSettings(VideoImporterTargetSettings settings);

        public extern string GetResizeModeName(VideoResizeMode mode);

        [NativeName("GetDefaultResizeWidth")]
        public extern int GetResizeWidth(VideoResizeMode mode);
        [NativeName("GetDefaultResizeHeight")]
        public extern int GetResizeHeight(VideoResizeMode mode);

        public extern ushort sourceAudioTrackCount { get; }
        public extern ushort GetSourceAudioChannelCount(ushort audioTrackIdx);
        public extern uint GetSourceAudioSampleRate(ushort audioTrackIdx);

        public extern int pixelAspectRatioNumerator { get; }
        public extern int pixelAspectRatioDenominator { get; }

        public extern bool transcodeSkipped { get; }

        [NativeMethod("operator==")]
        extern public bool Equals(VideoClipImporter rhs);
    }

}
