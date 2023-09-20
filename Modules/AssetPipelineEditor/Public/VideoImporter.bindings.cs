// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor
{

    // AssetImporter for importing VideoClip
    [NativeHeader("Modules/VideoEditor/VideoClipTranscode.h")]
    public enum VideoCodec
    {
        Auto = 0,
        H264 = 1,
        H265 = 3,
        VP8 = 2,
    }

    [NativeHeader("Modules/Video/Public/Base/VideoMediaTypes.h")]
    public enum VideoBitrateMode
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    [NativeHeader("Modules/Video/Public/Base/VideoMediaTypes.h")]
    public enum VideoEncodingProfile
    {
        H264Baseline = 0,
        H264Main = 1,
        H264High = 2
    }

    [NativeHeader("Modules/VideoEditor/VideoClipTranscode.h")]
    public enum VideoDeinterlaceMode
    {
        Off = 0,
        Even = 1,
        Odd = 2
    }

    [NativeHeader("Modules/VideoEditor/VideoClipTranscode.h")]
    internal enum VideoColorSpace
    {
        sRGB = 0,
        Linear = 3,
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

    [NativeHeader("Modules/VideoEditor/VideoClipTranscode.h")]
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
    [Serializable]
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
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("VideoClipImporter.quality has no effect anymore (was only used for MovieTexture which is removed)", false)]
        public float quality { get { return 1.0f; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("VideoClipImporter.linearColor has no effect anymore (was only used for MovieTexture which is removed)", false)]
        public bool linearColor { get { return false; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("VideoClipImporter.useLegacyImporter has no effect anymore (was only used for MovieTexture which is removed)", false)]
        public bool useLegacyImporter { get { return false; } set {} }

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

        [NativeName("sRGBClip")]
        public extern bool sRGBClip { get; set; }

        public VideoImporterTargetSettings defaultTargetSettings
        {
            get { return GetTargetSettings(VideoClipImporter.defaultTargetName); }
            set { SetTargetSettings(VideoClipImporter.defaultTargetName, value); }
        }

        public VideoImporterTargetSettings GetTargetSettings(string platform)
        {
            return Internal_GetTargetSettings(new NamedBuildTarget(platform));
        }

        internal VideoImporterTargetSettings Internal_GetTargetSettings(NamedBuildTarget target)
        {
            return Private_GetTargetSettings(target.TargetName) as VideoImporterTargetSettings;
        }

        [FreeFunction(Name = "VideoImporterBindings::GetTargetSettings", HasExplicitThis = true)]
        private extern object Private_GetTargetSettings(string target);

        public void SetTargetSettings(string platform, VideoImporterTargetSettings settings)
        {
            Internal_SetTargetSettings(new NamedBuildTarget(platform), settings);
        }

        internal void Internal_SetTargetSettings(NamedBuildTarget target, VideoImporterTargetSettings settings)
        {
            Private_SetTargetSettings(target.TargetName, settings);
        }

        [NativeName("SetTargetSettings")]
        internal extern void Private_SetTargetSettings(string targetName, VideoImporterTargetSettings settings);

        public void ClearTargetSettings(string platform)
        {
            Internal_ClearTargetSettings(new NamedBuildTarget(platform));
        }

        internal static event Action<string> analyticsSent;

        [RequiredByNativeCode]
        private static void VideoClipImporterInvokeAnalyticsSentCallback_Internal(string analytics)
        {
            if (analyticsSent != null)
                analyticsSent(analytics);
        }

        [RequiredByNativeCode]
        private static bool VideoClipImporterAnalyticsEventHandlerAttached_Internal()
        {
            return analyticsSent != null;
        }

        internal void Internal_ClearTargetSettings(NamedBuildTarget target)
        {
            Private_ClearTargetSettings(target.TargetName);
        }

        [NativeName("ClearTargetSettings")]
        internal extern void Private_ClearTargetSettings(string target);

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
        extern public bool Equals([NotNull] VideoClipImporter rhs);
    }

}
