// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor.Modules;

namespace UnityEditor
{
    // Resolution dialog setting
    [Obsolete("The Display Resolution Dialog has been removed.", false)]
    public enum ResolutionDialogSetting
    {
        // Never show the resolutions dialog.
        Disabled = 0,

        // Show the resolutions dialog on first launch.
        Enabled = 1,

        // Hide the resolutions dialog on first launch.
        HiddenByDefault = 2,
    }

    public enum ScriptingImplementation
    {
        Mono2x = 0,
        IL2CPP = 1,
        WinRTDotNET = 2,
        [Obsolete("CoreCLR support is still a work in progress and is disabled for now.")] // Hide from intellisense while CORECLR_FIXME
        [EditorBrowsable(EditorBrowsableState.Never)]
        CoreCLR = 3,
    }

    // Must be in sync with Il2CppCompilerConfiguration enum in SerializationMetaFlags.h
    public enum Il2CppCompilerConfiguration
    {
        Debug = 0,
        Release = 1,
        Master = 2,
    }

    // Mac fullscreen mode
    public enum MacFullscreenMode
    {
        [Obsolete("Capture Display mode is deprecated, Use FullscreenWindow instead")]
        CaptureDisplay = 0,

        // Fullscreen window.
        FullscreenWindow = 1,

        // Fullscreen window with Dock and Menu bar.
        FullscreenWindowWithDockAndMenuBar = 2,
    }

    [Obsolete("D3D9 support has been removed")]
    public enum D3D9FullscreenMode
    {
        [Obsolete("D3D9 support has been removed")]
        ExclusiveMode = 0,
        [Obsolete("D3D9 support has been removed")]
        FullscreenWindow = 1,
    }

    // Direct3D 11 fullscreen mode
    public enum D3D11FullscreenMode
    {
        // Exclusive mode.
        ExclusiveMode = 0,

        // Fullscreen window.
        FullscreenWindow = 1,
    }

    // Must be in sync with StereoRenderingPath enum in GfxDeviceTypes.h
    public enum StereoRenderingPath
    {
        // Slow multi pass method ( For reference only )
        MultiPass = 0,

        // Single pass stereo rendering
        SinglePass = 1,

        // Single pass stereo rendering with instancing
        Instancing = 2
    }

    // Managed code stripping level - must be in sync with StrippingLevel enum in BuildTargetPlatformSpecific.h
    public enum StrippingLevel
    {
        // Managed code stripping is disabled
        Disabled = 0,

        // Unused parts of managed code are stripped away
        StripAssemblies = 1,

        // Managed method bodies are stripped away. AOT platforms only.
        StripByteCode = 2,

        // Lightweight mscorlib version will be used at expense of limited compatibility.
        UseMicroMSCorlib = 3
    }

    // Script call optimization level
    public enum ScriptCallOptimizationLevel
    {
        // Default setting
        SlowAndSafe = 0,

        // Script method call overhead decreased at the expense of limited compatibility.
        FastButNoExceptions = 1
    }

    // Default mobile device orientation
    public enum UIOrientation
    {
        // Portrait
        Portrait = 0,

        // Portrait upside down
        PortraitUpsideDown = 1,

        // Landscape: clockwise from Portrait
        LandscapeRight = 2,

        // Landscape : counter-clockwise from Portrait
        LandscapeLeft = 3,

        // Auto Rotation Enabled
        AutoRotation = 4
    }

    // Scripting runtime version
    [Obsolete("ScriptingRuntimeVersion has been deprecated in 2019.3 now that legacy mono has been removed")]
    public enum ScriptingRuntimeVersion
    {
        // .NET 3.5
        Legacy = 0,

        // .NET 4.6
        Latest = 1
    }

    // .NET API compatibility level
    public enum ApiCompatibilityLevel
    {
        // .NET 2.0
        NET_2_0 = 1,

        // .NET 2.0 Subset
        NET_2_0_Subset = 2,

        // .NET 4.6
        NET_4_6 = 3,

        // unity_web profile, currently unused. Formerly used by Samsung TV
        NET_Web = 4,

        // micro profile, used by Mono scripting backend if stripping level is set to "Use micro mscorlib"
        NET_Micro = 5,

        // .NET Standard 2.0
        NET_Standard_2_0 = 6,

        // Latest .NET Standard version Unity supports
        NET_Standard = NET_Standard_2_0,

        // .NET Framework 8 + .NET Standard 2.1 APIs
        NET_Unity_4_8 = NET_4_6,
    }


    // Editor Assembly compatibility level
    public enum EditorAssembliesCompatibilityLevel
    {
        // For now it will be the same as #2. In future updates, the default will be the same as #3.
        Default = 1,
        NET_Unity_4_8 = 2,
        NET_Standard = 3,
    }

    public enum ManagedStrippingLevel
    {
        Disabled = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Minimal = 4,
    }

    // What to do on uncaught .NET exception (on iOS)
    public enum ActionOnDotNetUnhandledException
    {
        // Silent exit
        SilentExit = 0,

        // Crash
        Crash = 1
    }

    [Obsolete("SplashScreenStyle deprecated, Use PlayerSettings.SplashScreen.UnityLogoStyle instead")]
    public enum SplashScreenStyle
    {
        Light = 0,
        Dark = 1
    }

    // Must be in sync with GraphicsJobMode enum in GfxDeviceTypes.h
    public enum GraphicsJobMode
    {
        Native = 0,
        Legacy = 1
    }

    // Must be in sync with IconKind enum in EditorOnlyPlayerSettings.h
    public enum IconKind
    {
        Any = -1,
        Application = 0,
        Settings = 1,
        Notification = 2,
        Spotlight = 3,
        Store = 4
    }

    // Keep in synch with LightmapEncodingQuality enum from GfxDeviceTypes.h
    internal enum LightmapEncodingQuality
    {
        Low = 0,
        Normal = 1,
        High = 2
    }

    // Keep in synch with HDRCubemapEncodingQuality enum from GfxDeviceTypes.h
    internal enum HDRCubemapEncodingQuality
    {
        Low = 0,
        Normal = 1,
        High = 2
    }

    // Must be in sync with ShaderPrecisionModel enum in EditorOnlyPlayerSettings.h
    public enum ShaderPrecisionModel
    {
        PlatformDefault = 0,
        Unified = 1,
    }

    public enum NormalMapEncoding
    {
        XYZ = 0,
        DXT5nm = 1
    }

    internal enum TextureCompressionFormat
    {
        Unknown = 0,
        ETC = 1,
        ETC2 = 2,
        ASTC = 3,
        PVRTC = 4,
        DXTC = 5,
        BPTC = 6,
        DXTC_RGTC = 7
    }

    public enum InsecureHttpOption
    {
        NotAllowed = 0,
        DevelopmentOnly = 1,
        AlwaysAllowed = 2,
    }

    // Windows platform input APIs
    // Keep in sync with WindowsGamepadBackendHint enum from PlayerSettings.h
    public enum WindowsGamepadBackendHint
    {
        // Backend selected automatically based on platform support
        WindowsGamepadBackendHintDefault = 0,

        // XInput
        WindowsGamepadBackendHintXInput = 1,

        // GameInput
        WindowsGamepadBackendHintWindowsGamingInput = 2
    }

    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    [NativeClass(null)]
    [NativeHeader("Editor/Mono/PlayerSettings.bindings.h")]
    [NativeHeader("Runtime/Misc/BuildSettings.h")]
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    [NativeHeader("Runtime/Misc/PlayerSettingsSplashScreen.h")]
    [StaticAccessor("GetPlayerSettings()")]
    public sealed partial class PlayerSettings : UnityEngine.Object
    {
        private PlayerSettings() {}

        private static SerializedObject _serializedObject;

        [FreeFunction("GetPlayerSettingsPtr")]
        private static extern UnityEngine.Object InternalGetPlayerSettingsObject();

        internal static void ValidateBuildTargetNameParameter(string buildTargetName, bool unknownIsValid = false)
        {
            // The empty string is the valid form of BuildTargetGroup.Unknown, so don't throw an execption for that case.
            if (BuildPipeline.GetBuildTargetGroupByName(buildTargetName) == BuildTargetGroup.Unknown && (!unknownIsValid || buildTargetName != ""))
                throw new ArgumentException($"The provided target platform group name ({buildTargetName}) is not valid.");
        }

        internal static SerializedObject GetSerializedObject()
        {
            if (_serializedObject == null)
                _serializedObject = new SerializedObject(InternalGetPlayerSettingsObject());
            return _serializedObject;
        }

        internal static SerializedProperty FindProperty(string name)
        {
            SerializedProperty property = GetSerializedObject().FindProperty(name);
            if (property == null)
                Debug.LogError("Failed to find:" + name);
            return property;
        }

        [Obsolete("Use explicit API instead.")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern void SetPropertyInt(string name, int value, BuildTargetGroup target);

        [Obsolete("Use explicit API instead.")]
        public static void SetPropertyInt(string name, int value)
        {
            SetPropertyInt(name, value, BuildTargetGroup.Unknown);
        }

        [Obsolete("Use explicit API instead.")]
        public static void SetPropertyInt(string name, int value, BuildTarget target)
        {
            SetPropertyInt(name, value, BuildPipeline.GetBuildTargetGroup(target));
        }

        [Obsolete("Use explicit API instead.")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern int GetPropertyInt(string name, BuildTargetGroup target);

        [Obsolete("Use explicit API instead.")]
        public static int GetPropertyInt(string name)
        {
            return GetPropertyInt(name, BuildTargetGroup.Unknown);
        }

        [Obsolete("Use explicit API instead.")]
        public static bool GetPropertyOptionalInt(string name, ref int value, BuildTargetGroup target)
        {
            value = GetPropertyInt(name, target);
            return true;
        }

        [Obsolete("Use explicit API instead.")]
        public static bool GetPropertyOptionalInt(string name, ref int value)
        {
            value = GetPropertyInt(name, BuildTargetGroup.Unknown);
            return true;
        }

        [Obsolete("Use explicit API instead.")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern void SetPropertyBool(string name, bool value, BuildTargetGroup target);

        [Obsolete("Use explicit API instead.")]
        public static void SetPropertyBool(string name, bool value)
        {
            SetPropertyBool(name, value, BuildTargetGroup.Unknown);
        }

        [Obsolete("Use explicit API instead.")]
        public static void SetPropertyBool(string name, bool value, BuildTarget target)
        {
            SetPropertyBool(name, value, BuildPipeline.GetBuildTargetGroup(target));
        }

        [Obsolete("Use explicit API instead.")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern bool GetPropertyBool(string name, BuildTargetGroup target);

        [Obsolete("Use explicit API instead.")]
        public static bool GetPropertyBool(string name)
        {
            return GetPropertyBool(name, BuildTargetGroup.Unknown);
        }

        [Obsolete("Use explicit API instead.")]
        public static bool GetPropertyOptionalBool(string name, ref bool value, BuildTargetGroup target)
        {
            value = GetPropertyBool(name, target);
            return true;
        }

        [Obsolete("Use explicit API instead.")]
        public static bool GetPropertyOptionalBool(string name, ref bool value)
        {
            value = GetPropertyBool(name, BuildTargetGroup.Unknown);
            return true;
        }

        [Obsolete("Use explicit API instead.")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern void SetPropertyString(string name, string value, BuildTargetGroup target);

        [Obsolete("Use explicit API instead.")]
        public static void SetPropertyString(string name, string value)
        {
            SetPropertyString(name, value, BuildTargetGroup.Unknown);
        }

        [Obsolete("Use explicit API instead.")]
        public static void SetPropertyString(string name, string value, BuildTarget target)
        {
            SetPropertyString(name, value, BuildPipeline.GetBuildTargetGroup(target));
        }

        [Obsolete("Use explicit API instead.")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern string GetPropertyString(string name, BuildTargetGroup target);

        [Obsolete("Use explicit API instead.")]
        public static string GetPropertyString(string name)
        {
            return GetPropertyString(name, BuildTargetGroup.Unknown);
        }

        [Obsolete("Use explicit API instead.")]
        public static bool GetPropertyOptionalString(string name, ref string value, BuildTargetGroup target)
        {
            value = GetPropertyString(name, target);
            return true;
        }

        [Obsolete("Use explicit API instead.")]
        public static bool GetPropertyOptionalString(string name, ref string value)
        {
            value = GetPropertyString(name, BuildTargetGroup.Unknown);
            return true;
        }

        internal static extern void SetDirty();

        // The name of your company.
        public static extern string companyName { get; set; }

        // The name of your product.
        public static extern string productName { get; set; }

        [Obsolete("Use PlayerSettings.SplashScreen.show instead")]
        [StaticAccessor("GetPlayerSettings().GetSplashScreenSettings()")]
        public static extern bool showUnitySplashScreen { get; set; }

        [Obsolete("Use PlayerSettings.SplashScreen.unityLogoStyle instead")]
        [StaticAccessor("GetPlayerSettings().GetSplashScreenSettings()")]
        [NativeProperty("SplashScreenLogoStyle")]
        public static extern SplashScreenStyle splashScreenStyle { get; set; }

        /// Cloud project id.
        [Obsolete("cloudProjectId is deprecated, use CloudProjectSettings.projectId instead")]
        public static extern string cloudProjectId { get; }

        internal static extern void SetCloudProjectId(string projectId);

        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        internal static extern void SetCloudServiceEnabled(string serviceKey, bool enabled);

        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        internal static extern bool GetCloudServiceEnabled(string serviceKey);

        /// Uniquely identifies your product.
        public static Guid productGUID
        {
            get { return new Guid(productGUIDRaw); }
        }

        /// *undocumented*
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        private static extern byte[] productGUIDRaw { get; }

        // Set the color space for the current project
        public static extern ColorSpace colorSpace { get; set; }

        // Default horizontal dimension of stand-alone player window.
        public static extern int defaultScreenWidth { get; set; }

        // Default vertical dimension of stand-alone player window.
        public static extern int defaultScreenHeight { get; set; }

        // Default horizontal dimension of web player window.
        public static extern int defaultWebScreenWidth { get; set; }

        // Default vertical dimension of web player window.
        public static extern int defaultWebScreenHeight { get; set; }

        // Defines the behaviour of the Resolution Dialog on product launch.
        [Obsolete("displayResolutionDialog has been removed.", false)]
        public static extern ResolutionDialogSetting displayResolutionDialog { get; set; }

        // If enabled, the game will default to fullscreen mode.
        [Obsolete("(defaultIsFullScreen is deprecated, use fullScreenMode instead")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern bool defaultIsFullScreen { get; set; }

        // If enabled, the game will default to native resolution in fullscreen mode.
        public static extern bool defaultIsNativeResolution { get; set; }

        // If enabled, the game will render at retina resolution
        public static extern bool macRetinaSupport { get; set; }

        // If enabled, your game will continue to run after lost focus.
        public static extern bool runInBackground { get; set; }

        // Defines if fullscreen games should darken secondary displays.
        public static extern bool captureSingleScreen { get; set; }

        // Write a log file with debugging information.
        public static extern bool usePlayerLog { get; set; }

        // Use resizable window in standalone player builds.
        public static extern bool resizableWindow { get; set; }

        // Should resolution be reset when native window size changes. Shared between iOS & Android platforms.
        public static extern bool resetResolutionOnWindowResize { get; set; }

        /// Bake collision meshes into the mesh asset.
        public static extern bool bakeCollisionMeshes { get; set; }

        // Enable receipt validation for the Mac App Store.
        public static extern bool useMacAppStoreValidation { get; set; }

        // Define how to handle fullscreen mode in Mac OS X standalones
        [Obsolete("macFullscreenMode is deprecated, use fullScreenMode instead")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern MacFullscreenMode macFullscreenMode { get; set; }

        // Define how to handle fullscreen mode with Direct3D 9
        [Obsolete("d3d9FullscreenMode is deprecated, use fullScreenMode instead")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern D3D9FullscreenMode d3d9FullscreenMode { get; set; }

        // Define how to handle fullscreen mode with Direct3D 11
        [Obsolete("d3d11FullscreenMode is deprecated, use fullScreenMode instead")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern D3D11FullscreenMode d3d11FullscreenMode { get; set; }

        [NativeProperty("FullscreenMode")]
        public static extern FullScreenMode fullScreenMode { get; set; }

        [Obsolete("This API is obsolete, and should no longer be used. Please use XRManagerSettings in the XR Management package instead.")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern bool virtualRealitySupported { get; set; }

        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        internal class PlayerSettings360StereoCapture
        {
            public static extern bool enable360StereoCapture
            {
                get;
                set;
            }
        }

        public static bool enable360StereoCapture
        {
            get { return PlayerSettings360StereoCapture.enable360StereoCapture; }
            set { PlayerSettings360StereoCapture.enable360StereoCapture = value; }
        }

        [Obsolete("singlePassStereoRendering will be deprecated. Use stereoRenderingPath instead.")]
        public static bool singlePassStereoRendering
        {
            get { return stereoRenderingPath == StereoRenderingPath.SinglePass; }
            set { stereoRenderingPath = value ? StereoRenderingPath.SinglePass : StereoRenderingPath.MultiPass; }
        }

        public static extern StereoRenderingPath stereoRenderingPath { get; set; }

        [Obsolete("protectGraphicsMemory is deprecated. This field has no effect.", false)]
        public static bool protectGraphicsMemory { get { return false; } set {} }

        public static extern bool enableFrameTimingStats { get; set; }
        public static extern bool enableOpenGLProfilerGPURecorders { get; set; }

        public static extern bool useHDRDisplay { get; set; }

        public static extern D3DHDRDisplayBitDepth D3DHDRBitDepth { get; set; }


        // What happens with the fullscreen Window when it runs in the background

        public static extern bool visibleInBackground { get; set; }

        // What happens the user presses OS specific full screen switch key combination

        public static extern bool allowFullscreenSwitch { get; set; }

        // Restrict standalone players to a single concurrent running instance.
        public static extern bool forceSingleInstance { get; set; }

        public static extern bool useFlipModelSwapchain { get; set; }

        [NativeProperty(TargetType = TargetType.Field)]
        public static extern bool openGLRequireES31
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
            get;

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
            set;
        }

        [NativeProperty(TargetType = TargetType.Field)]
        public static extern bool openGLRequireES31AEP
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
            get;

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
            set;
        }

        [NativeProperty(TargetType = TargetType.Field)]
        public static extern bool openGLRequireES32
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
            get;

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
            set;
        }

        // Set Maximum Vertex Threshold for Sprite Batching.
        public static extern int spriteBatchVertexThreshold { get; set; }

        // The image to display in the Resolution Dialog window.
        [Obsolete("resolutionDialogBanner has been removed.", false)]
        public static extern Texture2D resolutionDialogBanner { get; set; }

        // The image to display on the Virtual Reality splash screen.
        [StaticAccessor("GetPlayerSettings().GetSplashScreenSettings()")]
        public static extern Texture2D virtualRealitySplashScreen { get; set; }

        // The bundle identifier of the iPhone application.
        [Obsolete("iPhoneBundleIdentifier is deprecated. Use PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS) instead.")]
        public static string iPhoneBundleIdentifier
        {
            get { return GetApplicationIdentifier(NamedBuildTarget.iOS); }
            set { SetApplicationIdentifier(NamedBuildTarget.iOS, value); }
        }

        // Note: If an empty list is returned, no icons are assigned specifically to the specified platform at this point,
        [NativeMethod("GetPlatformIcons")]
        internal static extern Texture2D[] GetIconsForPlatform(string platform, IconKind kind);

        internal static Texture2D[] GetAllIconsForPlatform(string platform)
        {
            return GetIconsForPlatform(platform, IconKind.Any);
        }

        internal static void SetIconsForPlatform(string platform, Texture2D[] icons)
        {
            SetIconsForPlatform(platform, icons, IconKind.Any);
        }

        internal static void SetIconsForPlatform(string platform, Texture2D[] icons, IconKind[] kinds)
        {
            foreach (IconKind kind in GetIconKindsForPlatform(platform).Distinct())
            {
                List<Texture2D> iconsForKind = new List<Texture2D>();
                for (int i = 0; i < icons.Length; i++)
                {
                    if (kinds[i] == kind)
                        iconsForKind.Add(icons[i]);
                }
                SetIconsForPlatform(platform, iconsForKind.ToArray(), kind);
            }
        }

        [NativeThrows]
        [NativeMethod("SetPlatformIcons")]
        internal static extern void SetIconsForPlatform(string platform, Texture2D[] icons, IconKind kind);

        [NativeMethod("GetPlatformIconWidths")]
        internal static extern int[] GetIconWidthsForPlatform(string platform, IconKind kind);

        [NativeMethod("GetPlatformIconHeights")]
        internal static extern int[] GetIconHeightsForPlatform(string platform, IconKind kind);

        internal static int[] GetIconWidthsOfAllKindsForPlatform(string platform)
        {
            return GetIconWidthsForPlatform(platform, IconKind.Any);
        }

        internal static int[] GetIconHeightsOfAllKindsForPlatform(string platform)
        {
            return GetIconHeightsForPlatform(platform, IconKind.Any);
        }

        [NativeMethod("GetPlatformIconKinds")]
        internal static extern IconKind[] GetIconKindsForPlatform(string platform);

        public static extern UnityEngine.Object[] GetPreloadedAssets();

        public static extern void SetPreloadedAssets(UnityEngine.Object[] assets);

        internal static string GetPlatformName(BuildTargetGroup targetGroup)
        {
            return BuildPipeline.GetBuildTargetGroupName(targetGroup);
        }

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        internal static extern void GetBatchingForPlatform(BuildTarget platform, out int staticBatching, out int dynamicBatching);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        internal static extern void SetBatchingForPlatform(BuildTarget platform, int staticBatching, int dynamicBatching);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern int GetShaderChunkSizeInMBForPlatform(BuildTarget buildTarget);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern void SetShaderChunkSizeInMBForPlatform(BuildTarget buildTarget, int sizeInMegabytes);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern int GetShaderChunkCountForPlatform(BuildTarget buildTarget);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern void SetShaderChunkCountForPlatform(BuildTarget buildTarget, int chunkCount);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern int GetDefaultShaderChunkSizeInMB();

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern void SetDefaultShaderChunkSizeInMB(int sizeInMegabytes);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern int GetDefaultShaderChunkCount();

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern void SetDefaultShaderChunkCount(int chunkCount);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern bool GetOverrideShaderChunkSettingsForPlatform(BuildTarget buildTarget);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern void SetOverrideShaderChunkSettingsForPlatform(BuildTarget buildTarget, bool value);

        [NativeMethod("GetLightmapEncodingQuality")]
        internal static extern LightmapEncodingQuality GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup platformGroup);

        [NativeMethod("SetLightmapEncodingQuality")]
        internal static extern void SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup platformGroup, LightmapEncodingQuality encodingQuality);

        [NativeMethod("GetHDRCubemapEncodingQuality")]
        internal static extern HDRCubemapEncodingQuality GetHDRCubemapEncodingQualityForPlatformGroup(BuildTargetGroup platformGroup);

        [NativeMethod("SetHDRCubemapEncodingQuality")]
        internal static extern void SetHDRCubemapEncodingQualityForPlatformGroup(BuildTargetGroup platformGroup, HDRCubemapEncodingQuality encodingQuality);

        [FreeFunction("GetTargetPlatformGraphicsAPIAvailability")]
        internal static extern UnityEngine.Rendering.GraphicsDeviceType[] GetSupportedGraphicsAPIs(BuildTarget platform);

        [NativeMethod("GetPlatformGraphicsAPIs")]
        public static extern UnityEngine.Rendering.GraphicsDeviceType[] GetGraphicsAPIs(BuildTarget platform);

        public static void SetGraphicsAPIs(BuildTarget platform, UnityEngine.Rendering.GraphicsDeviceType[] apis)
        {
            SetGraphicsAPIsImpl(platform, apis);
            // we do cache api list in player settings editor, so if we update from script we should forcibly update cache
            PlayerSettingsEditor.SyncPlatformAPIsList(platform);
        }

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        private static extern void SetGraphicsAPIsImpl(BuildTarget platform, UnityEngine.Rendering.GraphicsDeviceType[] apis);

        [NativeMethod("GetPlatformAutomaticGraphicsAPIs")]
        public static extern bool GetUseDefaultGraphicsAPIs(BuildTarget platform);

        public static void SetUseDefaultGraphicsAPIs(BuildTarget platform, bool automatic)
        {
            SetUseDefaultGraphicsAPIsImpl(platform, automatic);
            // we do cache api list in player settings editor, so if we update from script we should forcibly update cache
            PlayerSettingsEditor.SyncPlatformAPIsList(platform);
        }

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        private static extern void SetUseDefaultGraphicsAPIsImpl(BuildTarget platform, bool automatic);

        // Set the output color space for the current project. This setting only
        // defines the format of the final framebuffer and render textures
        internal static extern ColorGamut[] GetColorGamuts();

        internal static void SetColorGamuts(ColorGamut[] colorSpaces)
        {
            SetColorGamutsImpl(colorSpaces);
            // Color space data is cached in player settings editor
            PlayerSettingsEditor.SyncColorGamuts();
        }

        [NativeMethod("SetColorGamuts")]
        private static extern void SetColorGamutsImpl(ColorGamut[] colorSpaces);

        internal static extern string[] templateCustomKeys { get; set; }

        public static extern void SetTemplateCustomValue(string name, string value);

        public static extern string GetTemplateCustomValue(string name);

        internal static extern string spritePackerPolicy
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly().spritePackerPolicy")]
            [NativeMethod("c_str")]
            get;

            [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
            set;
        }

        internal static readonly char[] defineSplits = new[] { ';', ',', ' ' };

        internal static string[] ConvertScriptingDefineStringToArray(string defines)
        {
            return defines.Split(defineSplits, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static string ConvertScriptingDefineArrayToString(string[] defines)
        {
            List<string> list = new List<string>();
            foreach (var define in defines)
            {
                string[] split = define.Split(' ', ';');

                // Split each define element, since there can be multiple defines added
                foreach (var item in split)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        list.Add(item);
                    }
                }
            }

            // Remove duplicates
            defines = list.Distinct().ToArray();

            // Join all defines to one string
            var joined = new StringBuilder();
            foreach (var define in defines)
            {
                if (joined.Length != 0)
                    joined.Append(';');

                joined.Append(define);
            }

            return joined.ToString();
        }

        // TargetGroup no longer defines the entire build targets space. Now build targets are better
        // identified by a string key (wrapped into NamedBuildTarget), so all the following methods are being replaced.

        [Obsolete("Use GetScriptingDefineSymbols(NamedBuildTarget buildTarget) instead")]
        public static string GetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup) =>
            GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("Use GetScriptingDefineSymbols(NamedBuildTarget buildTarget, out string[] defines) instead")]
        public static void GetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup, out string[] defines) =>
            defines = ConvertScriptingDefineStringToArray(GetScriptingDefineSymbolsForGroup(targetGroup));

        [Obsolete("Use SetScriptingDefineSymbols(NamedBuildTarget buildTarget, string defines) instead")]
        public static void SetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup, string defines) =>
            SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup), defines);

        [Obsolete("Use SetScriptingDefineSymbols(NamedBuildTarget buildTarget, string[] defines) instead")]
        public static void SetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup, string[] defines) =>
            SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup), defines);

        [Obsolete("Use GetAdditionalCompilerArguments(NamedBuildTarget buildTarget) instead")]
        public static string[] GetAdditionalCompilerArgumentsForGroup(BuildTargetGroup targetGroup) =>
            GetAdditionalCompilerArguments(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("Use SetAdditionalCompilerArguments(NamedBuildTarget buildTarget, string[] additionalCompilerArguments) instead")]
        public static void SetAdditionalCompilerArgumentsForGroup(BuildTargetGroup targetGroup, string[] additionalCompilerArguments) =>
            SetAdditionalCompilerArguments(NamedBuildTarget.FromBuildTargetGroup(targetGroup), additionalCompilerArguments);

        [Obsolete("Use GetArchitecture(NamedBuildTarget buildTarget) instead")]
        public static int GetArchitecture(BuildTargetGroup targetGroup) =>
            GetArchitecture(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("Use SetArchitecture(NamedBuildTarget buildTarget, int architecture) instead")]
        public static void SetArchitecture(BuildTargetGroup targetGroup, int architecture) =>
            SetArchitecture(NamedBuildTarget.FromBuildTargetGroup(targetGroup), architecture);

        [Obsolete("Use GetScriptingBackend(NamedBuildTarget buildTarget) instead")]
        public static ScriptingImplementation GetScriptingBackend(BuildTargetGroup targetGroup) =>
            GetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("Use SetScriptingBackend(NamedBuildTarget buildTarget, ScriptingImplementation backend) instead")]
        public static void SetScriptingBackend(BuildTargetGroup targetGroup, ScriptingImplementation backend) =>
            SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(targetGroup), backend);

        [Obsolete("Use GetDefaultScriptingBackend(NamedBuildTarget buildTarget) instead")]
        public static ScriptingImplementation GetDefaultScriptingBackend(BuildTargetGroup targetGroup) =>
            GetDefaultScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("Use GetApplicationIdentifier(NamedBuildTarget buildTarget) instead")]
        public static string GetApplicationIdentifier(BuildTargetGroup targetGroup) =>
            GetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("Use SetApplicationIdentifier(NamedBuildTarget buildTarget, string identifier) instead")]
        public static void SetApplicationIdentifier(BuildTargetGroup targetGroup, string identifier) =>
            SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(targetGroup), identifier);

        [Obsolete("Use GetIl2CppCompilerConfiguration(NamedBuildTarget buildTarget) instead")]
        public static Il2CppCompilerConfiguration GetIl2CppCompilerConfiguration(BuildTargetGroup targetGroup) =>
            GetIl2CppCompilerConfiguration(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("Use SetIl2CppCompilerConfiguration(NamedBuildTarget buildTarget, Il2CppCompilerConfiguration configuration) instead")]
        public static void SetIl2CppCompilerConfiguration(BuildTargetGroup targetGroup, Il2CppCompilerConfiguration configuration) =>
            SetIl2CppCompilerConfiguration(NamedBuildTarget.FromBuildTargetGroup(targetGroup), configuration);

        [Obsolete("GetIncrementalIl2CppBuild has no impact on the build process")]
        public static bool GetIncrementalIl2CppBuild(BuildTargetGroup targetGroup) =>
            GetIncrementalIl2CppBuild(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("SetIncrementalIl2CppBuild has no impact on the build process")]
        public static void SetIncrementalIl2CppBuild(BuildTargetGroup targetGroup, bool enabled) =>
            SetIncrementalIl2CppBuild(NamedBuildTarget.FromBuildTargetGroup(targetGroup), enabled);

        [Obsolete("Use SetManagedStrippingLevel(NamedBuildTarget buildTarget, ManagedStrippingLevel level) instead")]
        public static void SetManagedStrippingLevel(BuildTargetGroup targetGroup, ManagedStrippingLevel level) =>
            SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(targetGroup), level);

        [Obsolete("Use GetManagedStrippingLevel(NamedBuildTarget buildTarget) instead")]
        public static ManagedStrippingLevel GetManagedStrippingLevel(BuildTargetGroup targetGroup) =>
            GetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("Use GetApiCompatibilityLevel(NamedBuildTarget buildTarget) instead")]
        public static ApiCompatibilityLevel GetApiCompatibilityLevel(BuildTargetGroup buildTargetGroup) =>
            GetApiCompatibilityLevel(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));

        [Obsolete("Use SetApiCompatibilityLevel(NamedBuildTarget buildTarget, ApiCompatibilityLevel value) instead")]
        public static void SetApiCompatibilityLevel(BuildTargetGroup buildTargetGroup, ApiCompatibilityLevel value) =>
            SetApiCompatibilityLevel(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), value);

        [Obsolete("Use GetMobileMTRendering(NamedBuildTarget buildTarget) instead")]
        public static bool GetMobileMTRendering(BuildTargetGroup targetGroup) =>
            GetMobileMTRendering(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

        [Obsolete("Use SetMobileMTRendering(NamedBuildTarget buildTarget, bool enable) instead")]
        public static void SetMobileMTRendering(BuildTargetGroup targetGroup, bool enable) =>
            SetMobileMTRendering(NamedBuildTarget.FromBuildTargetGroup(targetGroup), enable);

        [Obsolete("Use GetNormalMapEncoding(NamedBuildTarget buildTarget) instead")]
        public static NormalMapEncoding GetNormalMapEncoding(BuildTargetGroup platform) =>
            GetNormalMapEncoding(NamedBuildTarget.FromBuildTargetGroup(platform));

        [Obsolete("Use SetNormalMapEncoding(NamedBuildTarget buildTarget, NormalMapEncoding encoding) instead")]
        public static void SetNormalMapEncoding(BuildTargetGroup platform, NormalMapEncoding encoding) =>
            SetNormalMapEncoding(NamedBuildTarget.FromBuildTargetGroup(platform), encoding);


        // Get user-specified symbols for script compilation for the given build target name.
        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetUserScriptingDefineSymbols")]
        private static extern string GetScriptingDefineSymbolsInternal(string buildTargetName);
        public static string GetScriptingDefineSymbols(NamedBuildTarget buildTarget) =>
            GetScriptingDefineSymbolsInternal(buildTarget.TargetName);
        public static void GetScriptingDefineSymbols(NamedBuildTarget buildTarget, out string[] defines) =>
            defines = ConvertScriptingDefineStringToArray(GetScriptingDefineSymbols(buildTarget));

        // Set user-specified symbols for script compilation for the given build target group.
        public static void SetScriptingDefineSymbols(NamedBuildTarget buildTarget, string defines)
        {
            if (!string.IsNullOrEmpty(defines))
                defines = string.Join(";", ConvertScriptingDefineStringToArray(defines));

            SetScriptingDefineSymbolsInternal(buildTarget.TargetName, defines);
        }

        public static void SetScriptingDefineSymbols(NamedBuildTarget buildTarget, string[] defines)
        {
            if (defines == null)
                throw new ArgumentNullException(nameof(defines));

            SetScriptingDefineSymbols(buildTarget, ConvertScriptingDefineArrayToString(defines));
        }

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetUserScriptingDefineSymbols")]
        private static extern void SetScriptingDefineSymbolsInternal(string buildTargetName, string defines);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetAdditionalCompilerArguments")]
        private static extern string[] GetAdditionalCompilerArgumentsInternal(string buildTargetName);
        public static string[] GetAdditionalCompilerArguments(NamedBuildTarget buildTarget) =>
            GetAdditionalCompilerArgumentsInternal(buildTarget.TargetName);

        public static void SetAdditionalCompilerArguments(NamedBuildTarget buildTarget, string[] additionalCompilerArguments)
        {
            if (additionalCompilerArguments == null)
                throw new ArgumentNullException(nameof(additionalCompilerArguments));

            SetAdditionalCompilerArgumentsInternal(buildTarget.TargetName, additionalCompilerArguments);
        }

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetAdditionalCompilerArguments")]
        private static extern void SetAdditionalCompilerArgumentsInternal(string buildTargetName, string[] additionalCompilerArguments);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetPlatformArchitecture")]
        private static extern int GetArchitectureInternal(string buildTargetName);
        public static int GetArchitecture(NamedBuildTarget buildTarget) => GetArchitectureInternal(buildTarget.TargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetPlatformArchitecture")]
        private static extern void SetArchitectureInternal(string buildTargetName, int architecture);
        public static void SetArchitecture(NamedBuildTarget buildTarget, int architecture) =>
            SetArchitectureInternal(buildTarget.TargetName, architecture);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetPlatformScriptingBackend")]
        private static extern ScriptingImplementation GetScriptingBackendInternal(string buildTargetName);
        public static ScriptingImplementation GetScriptingBackend(NamedBuildTarget buildTarget) =>
            GetScriptingBackendInternal(buildTarget.TargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetPlatformScriptingBackend")]
        private static extern void SetScriptingBackendInternal(string buildTargetName, ScriptingImplementation backend);
        public static void SetScriptingBackend(NamedBuildTarget buildTarget, ScriptingImplementation backend) =>
            SetScriptingBackendInternal(buildTarget.TargetName, backend);

        [FreeFunction("GetDefaultScriptingBackendForGroup")]
        internal static extern ScriptingImplementation GetDefaultScriptingBackendForGroup(BuildTargetGroup targetGroup);
        public static ScriptingImplementation GetDefaultScriptingBackend(NamedBuildTarget buildTarget)
        {
            return GetDefaultScriptingBackendForGroup(buildTarget.ToBuildTargetGroup());
        }

        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        internal static extern bool IsApplicationIdentifierValid(string name, BuildTargetGroup targetGroup);

        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        internal static extern string SanitizeApplicationIdentifier(string name, BuildTargetGroup targetGroup);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetApplicationIdentifier")]
        private static extern void SetApplicationIdentifierInternal(string buildTargetName, string identifier);
        public static void SetApplicationIdentifier(NamedBuildTarget buildTarget, string identifier) =>
            SetApplicationIdentifierInternal(buildTarget.TargetName, identifier);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetApplicationIdentifier")]
        private static extern string GetApplicationIdentifierInternal(string buildTargetName);
        public static string GetApplicationIdentifier(NamedBuildTarget buildTarget) =>
            GetApplicationIdentifierInternal(buildTarget.TargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetApplicationBuildNumber")]
        internal static extern string GetBuildNumber(string buildTargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetApplicationBuildNumber")]
        internal static extern void SetBuildNumber(string buildTargetName, string buildNumber);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetIl2CppCompilerConfiguration")]
        private static extern void SetIl2CppCompilerConfigurationInternal(string buildTargetName, Il2CppCompilerConfiguration configuration);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetIl2CppCompilerConfiguration")]
        private static extern Il2CppCompilerConfiguration GetIl2CppCompilerConfigurationInternal(string buildTargetName);
        public static Il2CppCompilerConfiguration GetIl2CppCompilerConfiguration(NamedBuildTarget buildTarget) =>
            GetIl2CppCompilerConfigurationInternal(buildTarget.TargetName);

        public static void SetIl2CppCompilerConfiguration(NamedBuildTarget buildTarget, Il2CppCompilerConfiguration configuration)
        {
            var scriptingImpl = ModuleManager.GetScriptingImplementations(buildTarget);
            if (scriptingImpl != null && !scriptingImpl.AllowIL2CPPCompilerConfigurationSelection())
            {
                Debug.LogWarning($"The C++ compiler configuration option does not apply to the {buildTarget.TargetName} platform as it is configured. Set the configuration in the generated IDE project instead.");
                return;
            }

            SetIl2CppCompilerConfigurationInternal(buildTarget.TargetName, configuration);
        }

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetPlatformIncrementalIl2CppBuild")]
        private static extern bool GetIncrementalIl2CppBuildInternal(string buildTargetName);
        [Obsolete("GetIncrementalIl2CppBuild has no impact on the build process")]
        public static bool GetIncrementalIl2CppBuild(NamedBuildTarget buildTarget) => GetIncrementalIl2CppBuildInternal(buildTarget.TargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetPlatformIncrementalIl2CppBuild")]
        private static extern void SetIncrementalIl2CppBuildInternal(string buildTargetName, bool enabled);
        [Obsolete("SetIncrementalIl2CppBuild has no impact on the build process")]
        public static void SetIncrementalIl2CppBuild(NamedBuildTarget buildTarget, bool enabled) =>
            SetIncrementalIl2CppBuildInternal(buildTarget.TargetName, enabled);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetManagedStrippingLevel")]
        private static extern void SetManagedStrippingLevelInternal(string buildTargetName, ManagedStrippingLevel level);
        public static void SetManagedStrippingLevel(NamedBuildTarget buildTarget, ManagedStrippingLevel level) =>
            SetManagedStrippingLevelInternal(buildTarget.TargetName, level);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetManagedStrippingLevel")]
        private static extern ManagedStrippingLevel GetManagedStrippingLevelInternal(string buildTargetName);
        public static ManagedStrippingLevel GetManagedStrippingLevel(NamedBuildTarget buildTarget) => GetManagedStrippingLevelInternal(buildTarget.TargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetApiCompatibilityLevel")]
        private static extern ApiCompatibilityLevel GetApiCompatibilityLevelInternal(string buildTargetName);
        public static ApiCompatibilityLevel GetApiCompatibilityLevel(NamedBuildTarget buildTarget) => GetApiCompatibilityLevelInternal(buildTarget.TargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetApiCompatibilityLevel")]
        private static extern void SetApiCompatibilityLevelInternal(string buildTargetName, ApiCompatibilityLevel value);
        public static void SetApiCompatibilityLevel(NamedBuildTarget buildTarget, ApiCompatibilityLevel value) =>
            SetApiCompatibilityLevelInternal(buildTarget.TargetName, value);

        internal static extern bool defaultEditorAssembliesCompatibilityLevel
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
            get;
        }

        internal static ApiCompatibilityLevel EditorAssemblyCompatibilityToApiCompatibility(EditorAssembliesCompatibilityLevel level)
        {
            switch (level)
            {
                case EditorAssembliesCompatibilityLevel.NET_Standard:
                    {
                        return ApiCompatibilityLevel.NET_Standard_2_0;
                    }
                default:
                    {
                        return ApiCompatibilityLevel.NET_Unity_4_8;
                    }
            }
        }

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetEditorAssembliesCompatibilityLevel")]
        private static extern EditorAssembliesCompatibilityLevel GetEditorAssembliesCompatibilityLevelInternal();
        public static EditorAssembliesCompatibilityLevel GetEditorAssembliesCompatibilityLevel() => GetEditorAssembliesCompatibilityLevelInternal();

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetEditorAssembliesCompatibilityLevel")]
        private static extern void SetEditorAssembliesCompatibilityLevelInternal(EditorAssembliesCompatibilityLevel value);
        public static void SetEditorAssembliesCompatibilityLevel(EditorAssembliesCompatibilityLevel value) => SetEditorAssembliesCompatibilityLevelInternal(value);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetIl2CppCodeGeneration")]
        private static extern Il2CppCodeGeneration GetIl2CppCodeGenerationInternal(string buildTargetName);
        public static Il2CppCodeGeneration GetIl2CppCodeGeneration(NamedBuildTarget buildTarget) => GetIl2CppCodeGenerationInternal(buildTarget.TargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetIl2CppCodeGeneration")]
        private static extern void SetIl2CppCodeGenerationInternal(string buildTargetName, Il2CppCodeGeneration value);
        public static void SetIl2CppCodeGeneration(NamedBuildTarget buildTarget, Il2CppCodeGeneration value) =>
            SetIl2CppCodeGenerationInternal(buildTarget.TargetName, value);

        [NativeThrows]
        [NativeMethod("SetMobileMTRendering")]
        private static extern void SetMobileMTRenderingInternal(string buildTargetName, bool enable);
        public static void SetMobileMTRendering(NamedBuildTarget buildTarget, bool enable) =>
            SetMobileMTRenderingInternal(buildTarget.TargetName, enable);

        [NativeThrows]
        [NativeMethod("GetMobileMTRendering")]
        private static extern bool GetMobileMTRenderingInternal(string buildTargetName);
        public static bool GetMobileMTRendering(NamedBuildTarget buildTarget) => GetMobileMTRenderingInternal(buildTarget.TargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        [NativeMethod("GetNormalMapEncoding")]
        private static extern NormalMapEncoding GetNormalMapEncodingInternal(string buildTargetName);
        public static NormalMapEncoding GetNormalMapEncoding(NamedBuildTarget buildTarget) => GetNormalMapEncodingInternal(buildTarget.TargetName);

        [NativeThrows]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        [NativeMethod("SetNormalMapEncoding")]
        private static extern void SetNormalMapEncodingInternal(string buildTargetName, NormalMapEncoding encoding);
        public static void SetNormalMapEncoding(NamedBuildTarget buildTarget, NormalMapEncoding encoding) =>
            SetNormalMapEncodingInternal(buildTarget.TargetName, encoding);

        [Obsolete("assemblyVersionValidation has been deprecated due to the introduction of binding redirects")]
        public static bool assemblyVersionValidation
        {
            get => false;
            set { }
        }

        [StaticAccessor("GetPlayerSettings().GetEditorOnly().additionalIl2CppArgs")]
        [NativeMethod("c_str")]
        public static extern string GetAdditionalIl2CppArgs();

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern void SetAdditionalIl2CppArgs(string additionalArgs);

        [Obsolete("ScriptingRuntimeVersion has been deprecated in 2019.3 due to the removal of legacy mono")]
        public static ScriptingRuntimeVersion scriptingRuntimeVersion
        {
            get { return ScriptingRuntimeVersion.Latest; }

            set {}
        }

        public static extern bool suppressCommonWarnings
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
            get;

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
            set;
        }

        public static extern bool allowUnsafeCode
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
            get;

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
            set;
        }

        internal static extern bool UseDeterministicCompilation
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
            get;

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
            set;
        }

        [Obsolete("Use of reference assemblies is always enabled")]
        public static bool useReferenceAssemblies
        {
            get { return true; }
            set {}
        }

        internal static extern bool gcWBarrierValidation
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
            get;

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
            set;
        }

        public static extern bool gcIncremental
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
            get;

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
            set;
        }

        // Password used for interacting with the Android Keystore.
        public static extern string keystorePass
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyNotPersistent().AndroidKeystorePass")]
            [NativeMethod("c_str")]
            get;

            [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
            set;
        }

        // Password for the key used for signing an Android application.
        public static extern string keyaliasPass
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyNotPersistent().AndroidKeyaliasPass")]
            [NativeMethod("c_str")]
            get;

            [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
            set;
        }

        // Xbox 360 title id
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static string xboxTitleId
        {
            get { return String.Empty; }
            set {}
        }

        // Xbox 360 ImageXex override configuration file path
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static string xboxImageXexFilePath
        {
            get { return String.Empty; }
        }

        // Xbox 360 SPA file path
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static string xboxSpaFilePath
        {
            get { return String.Empty; }
        }

        // Xbox 360 auto-generation of _SPAConfig.cs
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static bool xboxGenerateSpa
        {
            get { return false; }
        }

        // Xbox 360 Enable XboxLive guest accounts
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static bool xboxEnableGuest
        {
            get { return false; }
        }

        // Xbox 360 Kinect resource file deployment
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static bool xboxDeployKinectResources
        {
            get { return false; }
        }

        // Xbox 360 Kinect Head Orientation file deployment
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static bool xboxDeployKinectHeadOrientation
        {
            get { return false; }
            set {}
        }

        // Xbox 360 Kinect Head Position file deployment
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static bool xboxDeployKinectHeadPosition
        {
            get { return false; }
            set {}
        }

        // Xbox 360 splash screen
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static Texture2D xboxSplashScreen
        {
            get { return null; }
        }

        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static int xboxAdditionalTitleMemorySize
        {
            get { return 0; }
            set {}
        }

        // Xbox 360 Kinect title flag - if false, the Kinect APIs are inactive
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static bool xboxEnableKinect
        {
            get { return false; }
        }

        // Xbox 360 Kinect automatic skeleton tracking.
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static bool xboxEnableKinectAutoTracking
        {
            get { return false; }
        }

        // Xbox 360 Kinect Enable Speech Engine
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static bool xboxEnableSpeech
        {
            get { return false; }
        }

        // Xbox 360 Kinect Speech DB
        [Obsolete("Xbox 360 has been removed in >=5.5")]
        public static UInt32 xboxSpeechDB
        {
            get { return 0; }
        }

        [NativeProperty("GPUSkinning")]
        public static extern bool gpuSkinning { get; set; }

        public static bool graphicsJobs
        {
            get { return GetGraphicsJobsForPlatform(EditorUserBuildSettings.activeBuildTarget); }
            set { SetGraphicsJobsForPlatform(EditorUserBuildSettings.activeBuildTarget, value); }
        }

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        internal static extern bool GetGraphicsJobsForPlatform(BuildTarget platform);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        internal static extern void SetGraphicsJobsForPlatform(BuildTarget platform, bool graphicsJobs);

        public static GraphicsJobMode graphicsJobMode
        {
            get { return GetGraphicsJobModeForPlatform(EditorUserBuildSettings.activeBuildTarget); }
            set { SetGraphicsJobModeForPlatform(EditorUserBuildSettings.activeBuildTarget, value); }
        }

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        internal static extern GraphicsJobMode GetGraphicsJobModeForPlatform(BuildTarget platform);

        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        internal static extern void SetGraphicsJobModeForPlatform(BuildTarget platform, GraphicsJobMode gfxJobMode);

        [StaticAccessor("GetPlayerSettings()")]
        public static extern bool GetWsaHolographicRemotingEnabled();

        [StaticAccessor("GetPlayerSettings()")]
        public static extern void SetWsaHolographicRemotingEnabled(bool enabled);

        // Xbox 360 Pix Texture Capture
        public static extern bool xboxPIXTextureCapture { get; }

        // Xbox 360 Avatars
        public static extern bool xboxEnableAvatar { get; }

        // Xbox One resolution options
        public static extern int xboxOneResolution { get; }

        /// Whether internal profiler is enabled on iOS
        public static extern bool enableInternalProfiler { get; set; }

        /// What to do on unhandled .NET exceptions on iOS
        public static extern ActionOnDotNetUnhandledException actionOnDotNetUnhandledException { get; set; }

        /// Whether to log Objective-C uncaught exceptions on iOS
        public static extern bool logObjCUncaughtExceptions { get; set; }

        /// Whether to enable the Crash Reporter API on iOS
        public static extern bool enableCrashReportAPI { get; set; }

        // Application (before 5.6 bundle) identifier was shared between iOS, Android and Tizen TV platforms before 5.6.
        public static string applicationIdentifier
        {
            get { return GetApplicationIdentifier(NamedBuildTarget.FromActiveSettings(EditorUserBuildSettings.activeBuildTarget)); }
            set
            {
                Debug.LogWarning("PlayerSettings.applicationIdentifier only changes the identifier for the currently active platform. Please use SetApplicationIdentifier to set it for any platform");
                SetApplicationIdentifier(NamedBuildTarget.FromActiveSettings(EditorUserBuildSettings.activeBuildTarget), value);
            }
        }

        // Application bundle version shared between iOS & Android platforms
        [NativeProperty("ApplicationVersion")]
        public static extern string bundleVersion { get; set; }

        // Should status bar be hidden. Shared between iOS & Android platforms
        [NativeProperty("UIStatusBarHidden")]
        public static extern bool statusBarHidden { get; set; }

        // Code stripping level
        [Obsolete("strippingLevel is deprecated, Use PlayerSettings.GetManagedStrippingLevel()/PlayerSettings.SetManagedStrippingLevel() instead. StripByteCode and UseMicroMSCorlib are no longer supported.")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern StrippingLevel strippingLevel { get; set; }

        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        internal static extern void ReinitialiseShaderCompiler(string platformSDKEnvVar, string EnvVarValue);

        // Strip Engine code
        public static extern bool stripEngineCode { get; set; }

        // Default screen orientation for mobiles
        [NativeProperty("DefaultScreenOrientation")]
        public static extern UIOrientation defaultInterfaceOrientation { get; set; }

        // Is auto-rotation to portrait supported?
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern bool allowedAutorotateToPortrait { get; set; }

        // Is auto-rotation to portrait upside-down supported?
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern bool allowedAutorotateToPortraitUpsideDown { get; set; }

        // Is auto-rotation to landscape right supported?
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern bool allowedAutorotateToLandscapeRight { get; set; }

        // Is auto-rotation to landscape left supported?
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern bool allowedAutorotateToLandscapeLeft { get; set; }

        // Let the OS autorotate the screen as the device orientation changes.
        [NativeProperty("UseAnimatedAutoRotation")]
        public static extern bool useAnimatedAutorotation { get; set; }

        // 32-bit Display Buffer is used
        public static extern bool use32BitDisplayBuffer { get; set; }

        // Preserve framebuffer alpha, iOS and Android only. Enables rendering Unity on top of native UI.
        public static extern bool preserveFramebufferAlpha { get; set; }

        // .NET API compatibility level
        [Obsolete("apiCompatibilityLevel is deprecated. Use PlayerSettings.GetApiCompatibilityLevel()/PlayerSettings.SetApiCompatibilityLevel() instead.")]
        public static ApiCompatibilityLevel apiCompatibilityLevel
        {
            get { return GetApiCompatibilityLevel(EditorUserBuildSettings.activeBuildTargetGroup); }
            set { SetApiCompatibilityLevel(EditorUserBuildSettings.activeBuildTargetGroup, value); }
        }

        // Should unused [[Mesh]] components be excluded from game build?
        public static extern bool stripUnusedMeshComponents { get; set; }

        // Don't do fuzzy variant selection in the player. If the requested variant is not there, use error shader.
        public static extern bool strictShaderVariantMatching { get; set; }

        // Should unused mips be excluded from texture build?
        public static extern bool mipStripping { get; set; }

        // Is the advanced version being used?
        [StaticAccessor("GetBuildSettings()")]
        [NativeProperty("hasAdvancedVersion", TargetType.Field)]
        public static extern bool advancedLicense { get; }

        // Additional AOT compilation options. Shared by AOT platforms.
        public static extern string aotOptions { get; set; }

        public static extern Texture2D defaultCursor { get; set; }

        public static extern Vector2 cursorHotspot { get; set; }

        // Accelerometer update frequency
        public static extern int accelerometerFrequency { get; set; }

        // Is multi-threaded rendering enabled?
        public static extern bool MTRendering { get; set; }

        [NativeMethod("GetStackTraceType")]
        public static extern StackTraceLogType GetStackTraceLogType(LogType logType);

        [NativeMethod("SetStackTraceType")]
        public static extern void SetStackTraceLogType(LogType logType, StackTraceLogType stackTraceType);

        [NativeMethod("GetGlobalStackTraceType")]
        internal static extern StackTraceLogType GetGlobalStackTraceLogType(LogType logType);

        [NativeMethod("SetGlobalStackTraceType")]
        internal static extern void SetGlobalStackTraceLogType(LogType logType, StackTraceLogType stackTraceType);

        [Obsolete("Use UnityEditor.PlayerSettings.SetGraphicsAPIs/GetGraphicsAPIs instead")]
        public static bool useDirect3D11
        {
            get { return GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows); }
            set {}  // setter does nothing; D3D11 is always the fallback
        }

        internal static extern bool submitAnalytics { get; set; }

        [Obsolete("Use VREditor.GetStereoDeviceEnabled instead")]
        [StaticAccessor("PlayerSettingsBindings", StaticAccessorType.DoubleColon)]
        public static extern bool stereoscopic3D { get; set; }

        // Defines whether the application will request audio focus, muting all other audio sources.
        public static extern bool muteOtherAudioSources { get; set; }

        internal static extern bool playModeTestRunnerEnabled { get; set; }

        internal static extern bool runPlayModeTestAsEditModeTest { get; set; }

        // Defines whether the BlendShape weight range in SkinnedMeshRenderers is clamped
        public static extern bool legacyClampBlendShapeWeights { get; set; }

        // If enabled, metal API validation will be turned on in the editor
        [NativeProperty("MetalAPIValidation")]
        public static extern bool enableMetalAPIValidation
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            get;
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            set;
        }

        [FreeFunction("GetPlayerSettings().GetLightmapStreamingEnabled")]
        internal static extern bool GetLightmapStreamingEnabledForPlatformGroup(BuildTargetGroup platformGroup);

        [FreeFunction("GetPlayerSettings().SetLightmapStreamingEnabled")]
        internal static extern void SetLightmapStreamingEnabledForPlatformGroup(BuildTargetGroup platformGroup, bool lightmapStreamingEnabled);

        [FreeFunction("GetPlayerSettings().GetLightmapStreamingPriority")]
        internal static extern int GetLightmapStreamingPriorityForPlatformGroup(BuildTargetGroup platformGroup);

        [FreeFunction("GetPlayerSettings().SetLightmapStreamingPriority")]
        internal static extern void SetLightmapStreamingPriorityForPlatformGroup(BuildTargetGroup platformGroup, int lightmapStreamingPriority);

        [FreeFunction("GetPlayerSettings().GetLoadStoreDebugModeEnabled")]
        internal static extern bool GetLoadStoreDebugModeEnabledForPlatformGroup(BuildTargetGroup platformGroup);

        [FreeFunction("GetPlayerSettings().SetLoadStoreDebugModeEnabled")]
        internal static extern void SetLoadStoreDebugModeEnabledForPlatformGroup(BuildTargetGroup platformGroup, bool loadStoreDebugModeEnabled);

        [FreeFunction("GetPlayerSettings().GetLoadStoreDebugModeEditorOnly")]
        internal static extern bool GetLoadStoreDebugModeEditorOnlyForPlatformGroup(BuildTargetGroup platformGroup);

        [FreeFunction("GetPlayerSettings().SetLoadStoreDebugModeEditorOnly")]
        internal static extern void SetLoadStoreDebugModeEditorOnlyForPlatformGroup(BuildTargetGroup platformGroup, bool loadStoreDebugModeEnabled);

        [FreeFunction("GetPlayerSettings().GetDisableOldInputManagerSupport")]
        internal static extern bool GetDisableOldInputManagerSupport();

        [FreeFunction("GetPlayerSettings().GetWindowsGamepadBackendHint")]
        internal static extern WindowsGamepadBackendHint GetWindowsGamepadBackendHint();

        [FreeFunction("GetPlayerSettings().SetWindowsGamepadBackendHint")]
        internal static extern void SetWindowsGamepadBackendHint(WindowsGamepadBackendHint value);

        // The input API backend used on Windows platforms
        public static WindowsGamepadBackendHint windowsGamepadBackendHint
        {
            get { return GetWindowsGamepadBackendHint(); }
            set { SetWindowsGamepadBackendHint(value); }
        }

        [StaticAccessor("GetPlayerSettings()")]
        [NativeMethod("GetVirtualTexturingSupportEnabled")]
        public static extern bool GetVirtualTexturingSupportEnabled();

        [StaticAccessor("GetPlayerSettings()")]
        [NativeMethod("SetVirtualTexturingSupportEnabled")]
        public static extern void SetVirtualTexturingSupportEnabled(bool enabled);

        [StaticAccessor("GetPlayerSettings()")]
        [NativeMethod("OnVirtualTexturingChanged")]
        internal static extern bool OnVirtualTexturingChanged();

        public static extern InsecureHttpOption insecureHttpOption { get; set; }

        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        public static extern ShaderPrecisionModel GetShaderPrecisionModel();

        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        public static extern void SetShaderPrecisionModel(ShaderPrecisionModel model);


        [StaticAccessor("GetPlayerSettings().GetEditorOnly()")]
        internal static extern TextureCompressionFormat GetDefaultTextureCompressionFormat(BuildTargetGroup platform);

        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()")]
        internal static extern void SetDefaultTextureCompressionFormat(BuildTargetGroup platform, TextureCompressionFormat format);

        [FreeFunction("GetPlayerSettings().GetEditorOnly().RecompileScripts")]
        internal static extern void RecompileScripts(string reason, bool refreshProject = true);

        internal static extern bool isHandlingScriptRecompile
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            get;
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            set;
        }

        // note that we dont expose it in ui (yet) and keep it hidden on purpose
        // when the time comes we can totally rename this before making it public
        [NativeProperty("IOSCopyPluginsCodeInsteadOfSymlink")]
        internal static extern bool iosCopyPluginsCodeInsteadOfSymlink
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            get;
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            set;
        }
    }
}
