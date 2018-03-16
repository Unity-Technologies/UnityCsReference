// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.PlatformSupport;
using UnityEditor.Rendering;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Modules;
using UnityEditor.Scripting.ScriptCompilation;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;
using VR = UnityEditorInternal.VR;

// ************************************* READ BEFORE EDITING **************************************
//
// DO NOT COPY/PASTE! Please do not have the same setting more than once in the code.
// If a setting for one platform needs to be exposed to more platforms,
// change the if statements so the same lines of code are executed for both platforms,
// instead of copying the lines into multiple code blocks.
// This ensures that if we change labels, or headers, or the order of settings, it will remain
// consistent without having to remember to make the same change multiple places. THANK YOU!
//
// ADD_NEW_PLATFORM_HERE: review this file

namespace UnityEditor
{
    [CustomEditor(typeof(PlayerSettings))]
    internal partial class PlayerSettingsEditor : Editor
    {
        static class Styles
        {
            public static readonly GUIStyle categoryBox = new GUIStyle(EditorStyles.helpBox);

            public static readonly GUIContent colorSpaceAndroidWarning = EditorGUIUtility.TrTextContent("Linear colorspace requires OpenGL ES 3.0 or Vulkan, uncheck 'Automatic Graphics API' to remove OpenGL ES 2 API, Blit Type must be Always Blit or Auto and 'Minimum API Level' must be at least Android 4.3");
            public static readonly GUIContent colorSpaceWebGLWarning = EditorGUIUtility.TrTextContent("Linear colorspace requires WebGL 2.0, uncheck 'Automatic Graphics API' to remove WebGL 1.0 API. WARNING: If DXT sRGB is not supported by the browser, texture will be decompressed");
            public static readonly GUIContent colorSpaceIOSWarning = EditorGUIUtility.TrTextContent("Linear colorspace requires Metal API only. Uncheck 'Automatic Graphics API' and remove OpenGL ES 2 API. Additionally, 'minimum iOS version' set to 8.0 at least");
            public static readonly GUIContent colorSpaceTVOSWarning = EditorGUIUtility.TrTextContent("Linear colorspace requires Metal API only. Uncheck 'Automatic Graphics API' and remove OpenGL ES 2 API.");
            public static readonly GUIContent recordingInfo = EditorGUIUtility.TrTextContent("Reordering the list will switch editor to the first available platform");
            public static readonly GUIContent notApplicableInfo = EditorGUIUtility.TrTextContent("Not applicable for this platform.");
            public static readonly GUIContent sharedBetweenPlatformsInfo = EditorGUIUtility.TrTextContent("* Shared setting between multiple platforms.");
            public static readonly GUIContent vrOrientationInfo = EditorGUIUtility.TrTextContent("Virtual Reality Support is enabled. Upon entering VR mode, landscape left orientation will be the default orientation unless only landscape right is available.");
            public static readonly GUIContent IL2CPPAndroidExperimentalInfo = EditorGUIUtility.TrTextContent("IL2CPP on Android is experimental and unsupported");

            public static readonly GUIContent cursorHotspot = EditorGUIUtility.TrTextContent("Cursor Hotspot");
            public static readonly GUIContent defaultCursor = EditorGUIUtility.TrTextContent("Default Cursor");
            public static readonly GUIContent defaultIcon = EditorGUIUtility.TrTextContent("Default Icon");
            public static readonly GUIContent vertexChannelCompressionMask = EditorGUIUtility.TrTextContent("Vertex Compression*", "Select which vertex channels should be compressed. Compression can save memory and bandwidth but precision will be lower.");

            public static readonly GUIContent iconTitle = EditorGUIUtility.TrTextContent("Icon");
            public static readonly GUIContent resolutionPresentationTitle = EditorGUIUtility.TrTextContent("Resolution and Presentation");
            public static readonly GUIContent resolutionTitle = EditorGUIUtility.TrTextContent("Resolution");
            public static readonly GUIContent orientationTitle = EditorGUIUtility.TrTextContent("Orientation");
            public static readonly GUIContent allowedOrientationTitle = EditorGUIUtility.TrTextContent("Allowed Orientations for Auto Rotation");
            public static readonly GUIContent multitaskingSupportTitle = EditorGUIUtility.TrTextContent("Multitasking Support");
            public static readonly GUIContent statusBarTitle = EditorGUIUtility.TrTextContent("Status Bar");
            public static readonly GUIContent standalonePlayerOptionsTitle = EditorGUIUtility.TrTextContent("Standalone Player Options");
            public static readonly GUIContent debuggingCrashReportingTitle = EditorGUIUtility.TrTextContent("Debugging and crash reporting");
            public static readonly GUIContent debuggingTitle = EditorGUIUtility.TrTextContent("Debugging");
            public static readonly GUIContent crashReportingTitle = EditorGUIUtility.TrTextContent("Crash Reporting");
            public static readonly GUIContent otherSettingsTitle = EditorGUIUtility.TrTextContent("Other Settings");
            public static readonly GUIContent renderingTitle = EditorGUIUtility.TrTextContent("Rendering");
            public static readonly GUIContent identificationTitle = EditorGUIUtility.TrTextContent("Identification");
            public static readonly GUIContent macAppStoreTitle = EditorGUIUtility.TrTextContent("Mac App Store Options");
            public static readonly GUIContent configurationTitle = EditorGUIUtility.TrTextContent("Configuration");
            public static readonly GUIContent optimizationTitle = EditorGUIUtility.TrTextContent("Optimization");
            public static readonly GUIContent loggingTitle = EditorGUIUtility.TrTextContent("Logging*");
            public static readonly GUIContent publishingSettingsTitle = EditorGUIUtility.TrTextContent("Publishing Settings");

            public static readonly GUIContent bakeCollisionMeshes = EditorGUIUtility.TrTextContent("Prebake Collision Meshes*", "Bake collision data into the meshes on build time");
            public static readonly GUIContent keepLoadedShadersAlive = EditorGUIUtility.TrTextContent("Keep Loaded Shaders Alive*", "Prevents shaders from being unloaded");
            public static readonly GUIContent preloadedAssets = EditorGUIUtility.TrTextContent("Preloaded Assets*", "Assets to load at start up in the player and kept alive until the player terminates");
            public static readonly GUIContent stripEngineCode = EditorGUIUtility.TrTextContent("Strip Engine Code*", "Strip Unused Engine Code - Note that byte code stripping of managed assemblies is always enabled for the IL2CPP scripting backend.");
            public static readonly GUIContent iPhoneStrippingLevel = EditorGUIUtility.TrTextContent("Stripping Level*");
            public static readonly GUIContent iPhoneScriptCallOptimization = EditorGUIUtility.TrTextContent("Script Call Optimization*");
            public static readonly GUIContent enableInternalProfiler = EditorGUIUtility.TrTextContent("Enable Internal Profiler* (Deprecated)", "Internal profiler counters should be accessed by scripts using UnityEngine.Profiling::Profiler API.");
            public static readonly GUIContent stripUnusedMeshComponents = EditorGUIUtility.TrTextContent("Optimize Mesh Data*", "Remove unused mesh components");
            public static readonly GUIContent videoMemoryForVertexBuffers = EditorGUIUtility.TrTextContent("Mesh Video Mem*", "How many megabytes of video memory to use for mesh data before we use main memory");
            public static readonly GUIContent protectGraphicsMemory = EditorGUIUtility.TrTextContent("Protect Graphics Memory", "Protect GPU memory from being read (on supported devices). Will prevent user from taking screenshots");
            public static readonly GUIContent useOSAutoRotation = EditorGUIUtility.TrTextContent("Use Animated Autorotation", "If set OS native animated autorotation method will be used. Otherwise orientation will be changed immediately.");
            public static readonly GUIContent UIPrerenderedIcon = EditorGUIUtility.TrTextContent("Prerendered Icon");
            public static readonly GUIContent defaultScreenWidth = EditorGUIUtility.TrTextContent("Default Screen Width");
            public static readonly GUIContent defaultScreenHeight = EditorGUIUtility.TrTextContent("Default Screen Height");
            public static readonly GUIContent macRetinaSupport = EditorGUIUtility.TrTextContent("Mac Retina Support");
            public static readonly GUIContent runInBackground = EditorGUIUtility.TrTextContent("Run In Background*");
            public static readonly GUIContent defaultScreenOrientation = EditorGUIUtility.TrTextContent("Default Orientation*");
            public static readonly GUIContent allowedAutoRotateToPortrait = EditorGUIUtility.TrTextContent("Portrait");
            public static readonly GUIContent allowedAutoRotateToPortraitUpsideDown = EditorGUIUtility.TrTextContent("Portrait Upside Down");
            public static readonly GUIContent allowedAutoRotateToLandscapeRight = EditorGUIUtility.TrTextContent("Landscape Right");
            public static readonly GUIContent allowedAutoRotateToLandscapeLeft = EditorGUIUtility.TrTextContent("Landscape Left");
            public static readonly GUIContent UIRequiresFullScreen = EditorGUIUtility.TrTextContent("Requires Fullscreen");
            public static readonly GUIContent UIStatusBarHidden = EditorGUIUtility.TrTextContent("Status Bar Hidden");
            public static readonly GUIContent UIStatusBarStyle = EditorGUIUtility.TrTextContent("Status Bar Style");
            public static readonly GUIContent useMacAppStoreValidation = EditorGUIUtility.TrTextContent("Mac App Store Validation");
            public static readonly GUIContent macAppStoreCategory = EditorGUIUtility.TrTextContent("Category", "'LSApplicationCategoryType'");
            public static readonly GUIContent fullscreenMode = EditorGUIUtility.TrTextContent("Fullscreen Mode ", " Not all platforms support all modes");
            public static readonly GUIContent exclusiveFullscreen = EditorGUIUtility.TrTextContent("Exclusive Fullscreen");
            public static readonly GUIContent fullscreenWindow = EditorGUIUtility.TrTextContent("Fullscreen Window");
            public static readonly GUIContent maximizedWindow = EditorGUIUtility.TrTextContent("Maximized Window");
            public static readonly GUIContent windowed = EditorGUIUtility.TrTextContent("Windowed");
            public static readonly GUIContent visibleInBackground = EditorGUIUtility.TrTextContent("Visible In Background");
            public static readonly GUIContent allowFullscreenSwitch = EditorGUIUtility.TrTextContent("Allow Fullscreen Switch");
            public static readonly GUIContent use32BitDisplayBuffer = EditorGUIUtility.TrTextContent("Use 32-bit Display Buffer*", "If set Display Buffer will be created to hold 32-bit color values. Use it only if you see banding, as it has performance implications.");
            public static readonly GUIContent disableDepthAndStencilBuffers = EditorGUIUtility.TrTextContent("Disable Depth and Stencil*");
            public static readonly GUIContent iosShowActivityIndicatorOnLoading = EditorGUIUtility.TrTextContent("Show Loading Indicator");
            public static readonly GUIContent androidShowActivityIndicatorOnLoading = EditorGUIUtility.TrTextContent("Show Loading Indicator");
            public static readonly GUIContent actionOnDotNetUnhandledException = EditorGUIUtility.TrTextContent("On .Net UnhandledException*");
            public static readonly GUIContent logObjCUncaughtExceptions = EditorGUIUtility.TrTextContent("Log Obj-C Uncaught Exceptions*");
            public static readonly GUIContent enableCrashReportAPI = EditorGUIUtility.TrTextContent("Enable CrashReport API*");
            public static readonly GUIContent activeColorSpace = EditorGUIUtility.TrTextContent("Color Space*");
            public static readonly GUIContent colorGamut = EditorGUIUtility.TrTextContent("Color Gamut*");
            public static readonly GUIContent colorGamutForMac = EditorGUIUtility.TrTextContent("Color Gamut For Mac*");
            public static readonly GUIContent metalForceHardShadows = EditorGUIUtility.TrTextContent("Force hard shadows on Metal*");
            public static readonly GUIContent metalEditorSupport = EditorGUIUtility.TextContent("Metal Editor Support*");
            public static readonly GUIContent metalAPIValidation = EditorGUIUtility.TrTextContent("Metal API Validation*");
            public static readonly GUIContent metalFramebufferOnly = EditorGUIUtility.TrTextContent("Metal Restricted Backbuffer Use", "Set framebufferOnly flag on backbuffer. This prevents readback from backbuffer but enables some driver optimizations.");
            public static readonly GUIContent mTRendering = EditorGUIUtility.TrTextContent("Multithreaded Rendering*");
            public static readonly GUIContent staticBatching = EditorGUIUtility.TrTextContent("Static Batching");
            public static readonly GUIContent dynamicBatching = EditorGUIUtility.TrTextContent("Dynamic Batching");
            public static readonly GUIContent graphicsJobs = EditorGUIUtility.TrTextContent("Graphics Jobs (Experimental)*");
            public static readonly GUIContent graphicsJobsMode = EditorGUIUtility.TrTextContent("Graphics Jobs Mode*");
            public static readonly GUIContent applicationBuildNumber = EditorGUIUtility.TrTextContent("Build");
            public static readonly GUIContent appleDeveloperTeamID = EditorGUIUtility.TrTextContent("iOS Developer Team ID", "Developers can retrieve their Team ID by visiting the Apple Developer site under Account > Membership.");
            public static readonly GUIContent useOnDemandResources = EditorGUIUtility.TrTextContent("Use on demand resources*");
            public static readonly GUIContent accelerometerFrequency = EditorGUIUtility.TrTextContent("Accelerometer Frequency*");
            public static readonly GUIContent cameraUsageDescription = EditorGUIUtility.TrTextContent("Camera Usage Description*");
            public static readonly GUIContent locationUsageDescription = EditorGUIUtility.TrTextContent("Location Usage Description*");
            public static readonly GUIContent microphoneUsageDescription = EditorGUIUtility.TrTextContent("Microphone Usage Description*");
            public static readonly GUIContent muteOtherAudioSources = EditorGUIUtility.TrTextContent("Mute Other Audio Sources*");
            public static readonly GUIContent prepareIOSForRecording = EditorGUIUtility.TrTextContent("Prepare iOS for Recording");
            public static readonly GUIContent forceIOSSpeakersWhenRecording = EditorGUIUtility.TrTextContent("Force iOS Speakers when Recording");
            public static readonly GUIContent UIRequiresPersistentWiFi = EditorGUIUtility.TrTextContent("Requires Persistent WiFi*");
            public static readonly GUIContent iOSAllowHTTPDownload = EditorGUIUtility.TrTextContent("Allow downloads over HTTP (nonsecure)*");
            public static readonly GUIContent iOSURLSchemes = EditorGUIUtility.TrTextContent("Supported URL schemes*");
            public static readonly GUIContent aotOptions = EditorGUIUtility.TrTextContent("AOT Compilation Options*");
            public static readonly GUIContent require31 = EditorGUIUtility.TrTextContent("Require ES3.1");
            public static readonly GUIContent requireAEP = EditorGUIUtility.TrTextContent("Require ES3.1+AEP");
            public static readonly GUIContent skinOnGPU = EditorGUIUtility.TrTextContent("GPU Skinning*", "Use DX11/ES3 GPU Skinning");
            public static readonly GUIContent skinOnGPUPS4 = EditorGUIUtility.TrTextContent("Compute Skinning*", "Use Compute pipeline for Skinning");
            public static readonly GUIContent skinOnGPUAndroidWarning = EditorGUIUtility.TrTextContent("GPU skinning on Android devices is only enabled in VR builds, and is experimental. Be sure to validate behavior and performance on your target devices.");
            public static readonly GUIContent disableStatistics = EditorGUIUtility.TrTextContent("Disable HW Statistics*", "Disables HW Statistics (Pro Only)");
            public static readonly GUIContent scriptingDefineSymbols = EditorGUIUtility.TrTextContent("Scripting Define Symbols*");
            public static readonly GUIContent scriptingRuntimeVersion = EditorGUIUtility.TrTextContent("Scripting Runtime Version*", "The scripting runtime version to be used. Unity uses different scripting backends based on platform, so these options are listed as equivalent expected behavior.");
            public static readonly GUIContent scriptingRuntimeVersionLegacy = EditorGUIUtility.TrTextContent("Legacy (.NET 3.5 Equivalent)");
            public static readonly GUIContent scriptingRuntimeVersionLatest = EditorGUIUtility.TrTextContent("Stable (.NET 4.x Equivalent)");
            public static readonly GUIContent scriptingBackend = EditorGUIUtility.TrTextContent("Scripting Backend");
            public static readonly GUIContent il2cppCompilerConfiguration = EditorGUIUtility.TrTextContent("C++ Compiler Configuration");
            public static readonly GUIContent scriptingMono2x = EditorGUIUtility.TrTextContent("Mono");
            public static readonly GUIContent scriptingWinRTDotNET = EditorGUIUtility.TrTextContent(".NET");
            public static readonly GUIContent scriptingIL2CPP = EditorGUIUtility.TrTextContent("IL2CPP");
            public static readonly GUIContent scriptingDefault = EditorGUIUtility.TrTextContent("Default");
            public static readonly GUIContent apiCompatibilityLevel = EditorGUIUtility.TrTextContent("Api Compatibility Level*");
            public static readonly GUIContent apiCompatibilityLevel_NET_2_0 = EditorGUIUtility.TrTextContent(".NET 2.0");
            public static readonly GUIContent apiCompatibilityLevel_NET_2_0_Subset = EditorGUIUtility.TrTextContent(".NET 2.0 Subset");
            public static readonly GUIContent apiCompatibilityLevel_NET_4_6 = EditorGUIUtility.TrTextContent(".NET 4.x");
            public static readonly GUIContent apiCompatibilityLevel_NET_Standard_2_0 = EditorGUIUtility.TrTextContent(".NET Standard 2.0");
            public static readonly GUIContent allowUnsafeCode = EditorGUIUtility.TrTextContent("Allow 'unsafe' Code", "Allow compilation of unsafe code for predefined assemblies (Assembly-CSharp.dll, etc.)");
            public static readonly GUIContent activeInputHandling = EditorGUIUtility.TrTextContent("Active Input Handling*");
            public static readonly GUIContent[] activeInputHandlingOptions = new GUIContent[] { EditorGUIUtility.TrTextContent("Input Manager"), EditorGUIUtility.TrTextContent("Input System (Preview)"), EditorGUIUtility.TrTextContent("Both") };
            public static readonly GUIContent vrSettingsMoved = EditorGUIUtility.TrTextContent("Virtual Reality moved to XR Settings");
            public static readonly GUIContent lightmapEncodingLabel = EditorGUIUtility.TrTextContent("Lightmap Encoding", "Affects the encoding scheme and compression format of the lightmaps.");
            public static readonly GUIContent[] lightmapEncodingNames = { EditorGUIUtility.TrTextContent("Normal Quality"), EditorGUIUtility.TrTextContent("High Quality")};
            public static readonly GUIContent monoNotSupportediOS11WarningGUIContent = EditorGUIUtility.TrTextContent("Mono is not supported on iOS11 and above.");
            public static string undoChangedBundleIdentifierString { get { return LocalizationDatabase.GetLocalizedString("Changed macOS bundleIdentifier"); } }
            public static string undoChangedBuildNumberString { get { return LocalizationDatabase.GetLocalizedString("Changed macOS build number"); } }
            public static string undoChangedBatchingString { get { return LocalizationDatabase.GetLocalizedString("Changed Batching Settings"); } }
            public static string undoChangedIconString { get { return LocalizationDatabase.GetLocalizedString("Changed Icon"); } }
            public static string undoChangedGraphicsAPIString { get { return LocalizationDatabase.GetLocalizedString("Changed Graphics API Settings"); } }

            static Styles()
            {
                categoryBox.padding.left = 14;
            }
        }

        // Icon layout constants
        const int kSlotSize = 64;
        const int kMaxPreviewSize = 96;
        const int kIconSpacing = 6;

        PlayerSettingsSplashScreenEditor m_SplashScreenEditor;
        PlayerSettingsSplashScreenEditor splashScreenEditor
        {
            get
            {
                if (m_SplashScreenEditor == null)
                    m_SplashScreenEditor = new PlayerSettingsSplashScreenEditor(this);
                return m_SplashScreenEditor;
            }
        }

        private static GraphicsJobMode[] m_GfxJobModeValues = new GraphicsJobMode[] { GraphicsJobMode.Native, GraphicsJobMode.Legacy };
        private static GUIContent[] m_GfxJobModeNames = new GUIContent[] { EditorGUIUtility.TrTextContent("Native"), EditorGUIUtility.TrTextContent("Legacy") };

        // Section and tab selection state

        SavedInt m_SelectedSection = new SavedInt("PlayerSettings.ShownSection", -1);

        BuildPlatform[] validPlatforms;

        // il2cpp
        SerializedProperty m_StripEngineCode;

        // macOS
        SerializedProperty m_ApplicationBundleVersion;
        SerializedProperty m_UseMacAppStoreValidation;
        SerializedProperty m_MacAppStoreCategory;

        // iOS, tvOS
#pragma warning disable 169
        SerializedProperty m_IPhoneApplicationDisplayName;

        SerializedProperty m_CameraUsageDescription;
        SerializedProperty m_LocationUsageDescription;
        SerializedProperty m_MicrophoneUsageDescription;

        SerializedProperty m_IPhoneStrippingLevel;
        SerializedProperty m_IPhoneScriptCallOptimization;
        SerializedProperty m_AotOptions;

        SerializedProperty m_DefaultScreenOrientation;
        SerializedProperty m_AllowedAutoRotateToPortrait;
        SerializedProperty m_AllowedAutoRotateToPortraitUpsideDown;
        SerializedProperty m_AllowedAutoRotateToLandscapeRight;
        SerializedProperty m_AllowedAutoRotateToLandscapeLeft;
        SerializedProperty m_UseOSAutoRotation;
        SerializedProperty m_Use32BitDisplayBuffer;
        SerializedProperty m_DisableDepthAndStencilBuffers;
        SerializedProperty m_iosShowActivityIndicatorOnLoading;
        SerializedProperty m_androidShowActivityIndicatorOnLoading;
        SerializedProperty m_tizenShowActivityIndicatorOnLoading;

        SerializedProperty m_AndroidProfiler;

        SerializedProperty m_UIPrerenderedIcon;
        SerializedProperty m_UIRequiresPersistentWiFi;
        SerializedProperty m_UIStatusBarHidden;
        SerializedProperty m_UIRequiresFullScreen;
        SerializedProperty m_UIStatusBarStyle;

        SerializedProperty m_IOSAllowHTTPDownload;
        SerializedProperty m_SubmitAnalytics;

        SerializedProperty m_IOSURLSchemes;

        SerializedProperty m_AccelerometerFrequency;
        SerializedProperty m_useOnDemandResources;
        SerializedProperty m_MuteOtherAudioSources;
        SerializedProperty m_PrepareIOSForRecording;
        SerializedProperty m_ForceIOSSpeakersWhenRecording;

        SerializedProperty m_EnableInternalProfiler;
        SerializedProperty m_ActionOnDotNetUnhandledException;
        SerializedProperty m_LogObjCUncaughtExceptions;
        SerializedProperty m_EnableCrashReportAPI;
        SerializedProperty m_EnableInputSystem;
        SerializedProperty m_DisableInputManager;

        SerializedProperty m_AllowUnsafeCode;

        // vita
        SerializedProperty m_VideoMemoryForVertexBuffers;

        // General
        SerializedProperty m_CompanyName;
        SerializedProperty m_ProductName;

        // Cursor
        SerializedProperty m_DefaultCursor;
        SerializedProperty m_CursorHotspot;

        // Screen
        SerializedProperty m_DefaultScreenWidth;
        SerializedProperty m_DefaultScreenHeight;

        SerializedProperty m_ActiveColorSpace;
        SerializedProperty m_StripUnusedMeshComponents;
        SerializedProperty m_VertexChannelCompressionMask;
        SerializedProperty m_MetalEditorSupport;
        SerializedProperty m_MetalAPIValidation;
        SerializedProperty m_MetalFramebufferOnly;
        SerializedProperty m_MetalForceHardShadows;

        SerializedProperty m_DisplayResolutionDialog;
        SerializedProperty m_DefaultIsNativeResolution;
        SerializedProperty m_MacRetinaSupport;

        SerializedProperty m_UsePlayerLog;
        SerializedProperty m_KeepLoadedShadersAlive;
        SerializedProperty m_PreloadedAssets;
        SerializedProperty m_BakeCollisionMeshes;
        SerializedProperty m_ResizableWindow;
        SerializedProperty m_FullscreenMode;
        SerializedProperty m_VisibleInBackground;
        SerializedProperty m_AllowFullscreenSwitch;
        SerializedProperty m_ForceSingleInstance;

        SerializedProperty m_RunInBackground;
        SerializedProperty m_CaptureSingleScreen;

        SerializedProperty m_SupportedAspectRatios;

        SerializedProperty m_SkinOnGPU;
        SerializedProperty m_GraphicsJobs;

        // OpenGL ES 3.1
        SerializedProperty m_RequireES31;
        SerializedProperty m_RequireES31AEP;

        SerializedProperty m_LightmapEncodingQuality;

        // Localization Cache
        string m_LocalizedTargetName;

        // reorderable lists of graphics devices, per platform
        static Dictionary<BuildTarget, ReorderableList> s_GraphicsDeviceLists = new Dictionary<BuildTarget, ReorderableList>();

        public static void SyncPlatformAPIsList(BuildTarget target)
        {
            if (!s_GraphicsDeviceLists.ContainsKey(target))
                return;
            s_GraphicsDeviceLists[target].list = PlayerSettings.GetGraphicsAPIs(target).ToList();
        }

        static ReorderableList s_ColorGamutList;

        public static void SyncColorGamuts()
        {
            s_ColorGamutList.list = PlayerSettings.GetColorGamuts().ToList();
        }

        public VR.PlayerSettingsEditorVR m_VRSettings;

        int selectedPlatform = 0;
        int scriptingDefinesControlID = 0;
        ISettingEditorExtension[] m_SettingsExtensions;

        // Section animation state
        const int kNumberGUISections = 7;
        AnimBool[] m_SectionAnimators = new AnimBool[kNumberGUISections];
        readonly AnimBool m_ShowDefaultIsNativeResolution = new AnimBool();
        readonly AnimBool m_ShowResolution = new AnimBool();
        private static Texture2D s_WarningIcon;

        public bool IsMobileTarget(BuildTargetGroup targetGroup)
        {
            return targetGroup == BuildTargetGroup.iOS
                ||  targetGroup == BuildTargetGroup.tvOS
                ||  targetGroup == BuildTargetGroup.Android
                ||  targetGroup == BuildTargetGroup.Tizen;
        }

        public SerializedProperty FindPropertyAssert(string name)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            if (property == null)
                Debug.LogError("Failed to find:" + name);
            return property;
        }

        void OnEnable()
        {
            validPlatforms = BuildPlatforms.instance.GetValidPlatforms(true).ToArray();

            m_IPhoneStrippingLevel          = FindPropertyAssert("iPhoneStrippingLevel");
            m_StripEngineCode               = FindPropertyAssert("stripEngineCode");

            m_IPhoneScriptCallOptimization  = FindPropertyAssert("iPhoneScriptCallOptimization");
            m_AndroidProfiler               = FindPropertyAssert("AndroidProfiler");
            m_CompanyName                   = FindPropertyAssert("companyName");
            m_ProductName                   = FindPropertyAssert("productName");

            m_DefaultCursor                 = FindPropertyAssert("defaultCursor");
            m_CursorHotspot                 = FindPropertyAssert("cursorHotspot");


            m_UIPrerenderedIcon             = FindPropertyAssert("uIPrerenderedIcon");

            m_UIRequiresFullScreen          = FindPropertyAssert("uIRequiresFullScreen");

            m_UIStatusBarHidden             = FindPropertyAssert("uIStatusBarHidden");
            m_UIStatusBarStyle              = FindPropertyAssert("uIStatusBarStyle");
            m_ActiveColorSpace              = FindPropertyAssert("m_ActiveColorSpace");
            m_StripUnusedMeshComponents     = FindPropertyAssert("StripUnusedMeshComponents");
            m_VertexChannelCompressionMask  = FindPropertyAssert("VertexChannelCompressionMask");
            m_MetalEditorSupport            = FindPropertyAssert("metalEditorSupport");
            m_MetalAPIValidation            = FindPropertyAssert("metalAPIValidation");
            m_MetalFramebufferOnly          = FindPropertyAssert("metalFramebufferOnly");
            m_MetalForceHardShadows         = FindPropertyAssert("iOSMetalForceHardShadows");

            m_ApplicationBundleVersion      = serializedObject.FindProperty("bundleVersion");
            if (m_ApplicationBundleVersion == null)
                m_ApplicationBundleVersion  = FindPropertyAssert("iPhoneBundleVersion");

            m_useOnDemandResources          = FindPropertyAssert("useOnDemandResources");
            m_AccelerometerFrequency        = FindPropertyAssert("accelerometerFrequency");

            m_MuteOtherAudioSources         = FindPropertyAssert("muteOtherAudioSources");
            m_PrepareIOSForRecording        = FindPropertyAssert("Prepare IOS For Recording");
            m_ForceIOSSpeakersWhenRecording     = FindPropertyAssert("Force IOS Speakers When Recording");
            m_UIRequiresPersistentWiFi      = FindPropertyAssert("uIRequiresPersistentWiFi");
            m_IOSAllowHTTPDownload          = FindPropertyAssert("iosAllowHTTPDownload");
            m_SubmitAnalytics               = FindPropertyAssert("submitAnalytics");

            m_IOSURLSchemes                 = FindPropertyAssert("iOSURLSchemes");

            m_AotOptions                    = FindPropertyAssert("aotOptions");

            m_CameraUsageDescription        = FindPropertyAssert("cameraUsageDescription");
            m_LocationUsageDescription      = FindPropertyAssert("locationUsageDescription");
            m_MicrophoneUsageDescription    = FindPropertyAssert("microphoneUsageDescription");

            m_EnableInternalProfiler        = FindPropertyAssert("enableInternalProfiler");
            m_ActionOnDotNetUnhandledException  = FindPropertyAssert("actionOnDotNetUnhandledException");
            m_LogObjCUncaughtExceptions     = FindPropertyAssert("logObjCUncaughtExceptions");
            m_EnableCrashReportAPI          = FindPropertyAssert("enableCrashReportAPI");
            m_EnableInputSystem             = FindPropertyAssert("enableNativePlatformBackendsForNewInputSystem");
            m_DisableInputManager           = FindPropertyAssert("disableOldInputManagerSupport");

            m_AllowUnsafeCode               = FindPropertyAssert("allowUnsafeCode");

            m_DefaultScreenWidth            = FindPropertyAssert("defaultScreenWidth");
            m_DefaultScreenHeight           = FindPropertyAssert("defaultScreenHeight");
            m_RunInBackground               = FindPropertyAssert("runInBackground");

            m_DefaultScreenOrientation              = FindPropertyAssert("defaultScreenOrientation");
            m_AllowedAutoRotateToPortrait           = FindPropertyAssert("allowedAutorotateToPortrait");
            m_AllowedAutoRotateToPortraitUpsideDown = FindPropertyAssert("allowedAutorotateToPortraitUpsideDown");
            m_AllowedAutoRotateToLandscapeRight     = FindPropertyAssert("allowedAutorotateToLandscapeRight");
            m_AllowedAutoRotateToLandscapeLeft      = FindPropertyAssert("allowedAutorotateToLandscapeLeft");
            m_UseOSAutoRotation                     = FindPropertyAssert("useOSAutorotation");
            m_Use32BitDisplayBuffer                 = FindPropertyAssert("use32BitDisplayBuffer");
            m_DisableDepthAndStencilBuffers         = FindPropertyAssert("disableDepthAndStencilBuffers");
            m_iosShowActivityIndicatorOnLoading     = FindPropertyAssert("iosShowActivityIndicatorOnLoading");
            m_androidShowActivityIndicatorOnLoading = FindPropertyAssert("androidShowActivityIndicatorOnLoading");
            m_tizenShowActivityIndicatorOnLoading   = FindPropertyAssert("tizenShowActivityIndicatorOnLoading");

            m_DefaultIsNativeResolution     = FindPropertyAssert("defaultIsNativeResolution");
            m_MacRetinaSupport              = FindPropertyAssert("macRetinaSupport");
            m_CaptureSingleScreen           = FindPropertyAssert("captureSingleScreen");
            m_DisplayResolutionDialog       = FindPropertyAssert("displayResolutionDialog");
            m_SupportedAspectRatios         = FindPropertyAssert("m_SupportedAspectRatios");
            m_UsePlayerLog                  = FindPropertyAssert("usePlayerLog");

            m_KeepLoadedShadersAlive        = FindPropertyAssert("keepLoadedShadersAlive");
            m_PreloadedAssets               = FindPropertyAssert("preloadedAssets");
            m_BakeCollisionMeshes           = FindPropertyAssert("bakeCollisionMeshes");
            m_ResizableWindow               = FindPropertyAssert("resizableWindow");
            m_UseMacAppStoreValidation      = FindPropertyAssert("useMacAppStoreValidation");
            m_MacAppStoreCategory           = FindPropertyAssert("macAppStoreCategory");
            m_FullscreenMode                = FindPropertyAssert("fullscreenMode");
            m_VisibleInBackground           = FindPropertyAssert("visibleInBackground");
            m_AllowFullscreenSwitch         = FindPropertyAssert("allowFullscreenSwitch");
            m_SkinOnGPU                     = FindPropertyAssert("gpuSkinning");
            m_GraphicsJobs                  = FindPropertyAssert("graphicsJobs");
            m_ForceSingleInstance           = FindPropertyAssert("forceSingleInstance");

            m_RequireES31                   = FindPropertyAssert("openGLRequireES31");
            m_RequireES31AEP                = FindPropertyAssert("openGLRequireES31AEP");

            m_VideoMemoryForVertexBuffers   = FindPropertyAssert("videoMemoryForVertexBuffers");

            m_SettingsExtensions = new ISettingEditorExtension[validPlatforms.Length];
            for (int i = 0; i < validPlatforms.Length; i++)
            {
                string module = ModuleManager.GetTargetStringFromBuildTargetGroup(validPlatforms[i].targetGroup);
                m_SettingsExtensions[i] = ModuleManager.GetEditorSettingsExtension(module);
                if (m_SettingsExtensions[i] != null)
                    m_SettingsExtensions[i].OnEnable(this);
            }

            for (int i = 0; i < m_SectionAnimators.Length; i++)
                m_SectionAnimators[i] = new AnimBool(m_SelectedSection.value == i, Repaint);

            m_ShowDefaultIsNativeResolution.valueChanged.AddListener(Repaint);
            m_ShowResolution.valueChanged.AddListener(Repaint);

            m_VRSettings = new VR.PlayerSettingsEditorVR(this);

            splashScreenEditor.OnEnable();

            // we clear it just to be on the safe side:
            // we access this cache both from player settings editor and script side when changing api
            s_GraphicsDeviceLists.Clear();
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        internal override string targetTitle
        {
            get
            {
                if (m_LocalizedTargetName == null)
                    m_LocalizedTargetName = L10n.Tr(target.name);
                return m_LocalizedTargetName;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            {
                CommonSettings();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            int oldPlatform = selectedPlatform;
            selectedPlatform = EditorGUILayout.BeginPlatformGrouping(validPlatforms, null);
            if (EditorGUI.EndChangeCheck())
            {
                // Awesome hackery to get string from delayed textfield when switching platforms
                if (EditorGUI.s_DelayedTextEditor.IsEditingControl(scriptingDefinesControlID))
                {
                    EditorGUI.EndEditingActiveTextField();
                    GUIUtility.keyboardControl = 0;
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(validPlatforms[oldPlatform].targetGroup, EditorGUI.s_DelayedTextEditor.text);
                }
                // Reset focus when changing between platforms.
                // If we don't do this, the resolution width/height value will not update correctly when they have the focus
                GUI.FocusControl("");
            }
            GUILayout.Label(string.Format(L10n.Tr("Settings for {0}"), validPlatforms[selectedPlatform].title.text));

            // Compensate so settings inside boxes line up with settings at the top, though keep a minimum of 150.
            EditorGUIUtility.labelWidth = Mathf.Max(150, EditorGUIUtility.labelWidth - 8);

            BuildPlatform platform = validPlatforms[selectedPlatform];
            BuildTargetGroup targetGroup = platform.targetGroup;

            int sectionIndex = 0;

            IconSectionGUI(targetGroup, m_SettingsExtensions[selectedPlatform], sectionIndex++);
            ResolutionSectionGUI(targetGroup, m_SettingsExtensions[selectedPlatform], sectionIndex++);
            m_SplashScreenEditor.SplashSectionGUI(platform, targetGroup, m_SettingsExtensions[selectedPlatform], sectionIndex++);
            DebugAndCrashReportingGUI(platform, targetGroup, m_SettingsExtensions[selectedPlatform], sectionIndex++);
            OtherSectionGUI(platform, targetGroup, m_SettingsExtensions[selectedPlatform], sectionIndex++);
            PublishSectionGUI(targetGroup, m_SettingsExtensions[selectedPlatform], sectionIndex++);
            m_VRSettings.XRSectionGUI(targetGroup, sectionIndex++);

            if (sectionIndex != kNumberGUISections)
                Debug.LogError("Mismatched number of GUI sections.");

            EditorGUILayout.EndPlatformGrouping();

            serializedObject.ApplyModifiedProperties();
        }

        private void CommonSettings()
        {
            EditorGUILayout.PropertyField(m_CompanyName);
            EditorGUILayout.PropertyField(m_ProductName);
            EditorGUILayout.Space();

            // Get icons and icon sizes for selected platform (or default)
            GUI.changed = false;
            string platformName = "";
            Texture2D[] icons = PlayerSettings.GetAllIconsForPlatform(platformName);
            int[] widths = PlayerSettings.GetIconWidthsOfAllKindsForPlatform(platformName);

            // Ensure the default icon list is always populated correctly
            if (icons.Length != widths.Length)
            {
                icons = new Texture2D[widths.Length];
            }
            icons[0] = (Texture2D)EditorGUILayout.ObjectField(Styles.defaultIcon, icons[0], typeof(Texture2D), false);
            // Save changes
            if (GUI.changed)
            {
                Undo.RecordObject(this.target, Styles.undoChangedIconString);
                PlayerSettings.SetIconsForPlatform(platformName, icons);
            }

            GUILayout.Space(3);

            Rect cursorPropertyRect = EditorGUILayout.GetControlRect(true, EditorGUI.kObjectFieldThumbnailHeight);
            EditorGUI.BeginProperty(cursorPropertyRect, Styles.defaultCursor, m_DefaultCursor);
            m_DefaultCursor.objectReferenceValue = EditorGUI.ObjectField(cursorPropertyRect, Styles.defaultCursor, m_DefaultCursor.objectReferenceValue, typeof(Texture2D), false);
            EditorGUI.EndProperty();

            Rect rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, 0, Styles.cursorHotspot);
            EditorGUI.PropertyField(rect, m_CursorHotspot, GUIContent.none);
        }

        public bool BeginSettingsBox(int nr, GUIContent header)
        {
            bool enabled = GUI.enabled;
            GUI.enabled = true; // we don't want to disable the expand behavior
            EditorGUILayout.BeginVertical(Styles.categoryBox);
            Rect r = GUILayoutUtility.GetRect(20, 18); r.x += 3; r.width += 6;
            EditorGUI.BeginChangeCheck();
            bool expanded = GUI.Toggle(r, m_SelectedSection.value == nr, header, EditorStyles.inspectorTitlebarText);
            if (EditorGUI.EndChangeCheck())
            {
                m_SelectedSection.value = (expanded ? nr : -1);
                GUIUtility.keyboardControl = 0;
            }
            m_SectionAnimators[nr].target = expanded;
            GUI.enabled = enabled;
            return EditorGUILayout.BeginFadeGroup(m_SectionAnimators[nr].faded);
        }

        public void EndSettingsBox()
        {
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.EndVertical();
        }

        private void ShowNoSettings()
        {
            GUILayout.Label(Styles.notApplicableInfo, EditorStyles.miniLabel);
        }

        public void ShowSharedNote()
        {
            GUILayout.Label(Styles.sharedBetweenPlatformsInfo, EditorStyles.miniLabel);
        }

        private void IconSectionGUI(BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension, int sectionIndex)
        {
            if (BeginSettingsBox(sectionIndex, Styles.iconTitle))
            {
                bool platformUsesStandardIcons = true;
                if (settingsExtension != null)
                    platformUsesStandardIcons = settingsExtension.UsesStandardIcons();

                if (platformUsesStandardIcons)
                {
                    bool selectedDefault = (selectedPlatform < 0);
                    // Set default platform variables
                    BuildPlatform platform = null;
                    targetGroup = BuildTargetGroup.Standalone;
                    string platformName = "";

                    // Override if a platform is selected
                    if (!selectedDefault)
                    {
                        platform = validPlatforms[selectedPlatform];
                        targetGroup = platform.targetGroup;
                        platformName = platform.name;
                    }

                    bool enabled = GUI.enabled;

                    if (targetGroup == BuildTargetGroup.WebGL)
                    {
                        ShowNoSettings();
                        EditorGUILayout.Space();
                    }
                    else if (targetGroup == BuildTargetGroup.WSA)
                    {
                    }
                    else
                    {
                        // Get icons and icon sizes for selected platform (or default)
                        Texture2D[] icons = PlayerSettings.GetAllIconsForPlatform(platformName);
                        int[] widths = PlayerSettings.GetIconWidthsOfAllKindsForPlatform(platformName);
                        int[] heights = PlayerSettings.GetIconHeightsOfAllKindsForPlatform(platformName);
                        IconKind[] kinds = PlayerSettings.GetIconKindsForPlatform(platformName);

                        bool overrideIcons = true;

                        if (!selectedDefault)
                        {
                            // If the list of icons for this platform is not empty (and has the correct size),
                            // consider the icon overridden for this platform
                            GUI.changed = false;
                            overrideIcons = (icons.Length == widths.Length);
                            overrideIcons = GUILayout.Toggle(overrideIcons, string.Format(L10n.Tr("Override for {0}"), platform.title.text));
                            GUI.enabled = enabled && overrideIcons;
                            if (GUI.changed || (!overrideIcons && icons.Length > 0))
                            {
                                // Set the list of icons to correct length if overridden, otherwise to an empty list
                                if (overrideIcons)
                                    icons = new Texture2D[widths.Length];
                                else
                                    icons = new Texture2D[0];

                                if (GUI.changed)
                                    PlayerSettings.SetIconsForPlatform(platformName, icons);
                            }
                        }

                        // Show the icons for this platform (or default)
                        GUI.changed = false;
                        for (int i = 0; i < widths.Length; i++)
                        {
                            int previewWidth = Mathf.Min(kMaxPreviewSize, widths[i]);
                            int previewHeight = (int)((float)heights[i] * previewWidth / widths[i]);  // take into account the aspect ratio

                            if (targetGroup == BuildTargetGroup.iOS)
                            {
                                // Spotlight icons begin with 120 but there are two in the list.
                                // So check if the next one is 80.
                                if (kinds[i] == IconKind.Spotlight && kinds[i - 1] != IconKind.Spotlight)
                                {
                                    Rect labelRect = GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, 20);
                                    GUI.Label(new Rect(labelRect.x, labelRect.y, EditorGUIUtility.labelWidth, 20), "Spotlight icons", EditorStyles.boldLabel);
                                }

                                if (kinds[i] == IconKind.Settings && kinds[i - 1] != IconKind.Settings)
                                {
                                    Rect labelRect = GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, 20);
                                    GUI.Label(new Rect(labelRect.x, labelRect.y, EditorGUIUtility.labelWidth, 20), "Settings icons", EditorStyles.boldLabel);
                                }

                                if (kinds[i] == IconKind.Notification && kinds[i - 1] != IconKind.Notification)
                                {
                                    Rect labelRect = GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, 20);
                                    GUI.Label(new Rect(labelRect.x, labelRect.y, EditorGUIUtility.labelWidth, 20), "Notification icons", EditorStyles.boldLabel);
                                }

                                if (kinds[i] == IconKind.Store && kinds[i - 1] != IconKind.Store)
                                {
                                    Rect labelRect = GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, 20);
                                    GUI.Label(new Rect(labelRect.x, labelRect.y, EditorGUIUtility.labelWidth, 20), "App Store icons", EditorStyles.boldLabel);
                                }
                            }

                            Rect rect = GUILayoutUtility.GetRect(kSlotSize, Mathf.Max(kSlotSize, previewHeight) + kIconSpacing);
                            float width = Mathf.Min(rect.width, EditorGUIUtility.labelWidth + 4 + kSlotSize + kIconSpacing + kMaxPreviewSize);

                            // Label
                            string label = widths[i] + "x" + heights[i];
                            GUI.Label(new Rect(rect.x, rect.y, width - kMaxPreviewSize - kSlotSize - 2 * kIconSpacing, 20), label);

                            // Texture slot
                            if (overrideIcons)
                            {
                                int slotWidth = kSlotSize;
                                int slotHeight = (int)((float)heights[i] / widths[i] * kSlotSize);  // take into account the aspect ratio
                                icons[i] = (Texture2D)EditorGUI.ObjectField(
                                        new Rect(rect.x + width - kMaxPreviewSize - kSlotSize - kIconSpacing, rect.y, slotWidth, slotHeight),
                                        icons[i],
                                        typeof(Texture2D),
                                        false);
                            }

                            // Preview
                            Rect previewRect = new Rect(rect.x + width - kMaxPreviewSize, rect.y, previewWidth, previewHeight);
                            Texture2D closestIcon = PlayerSettings.GetIconForPlatformAtSize(platformName, widths[i], heights[i], kinds[i]);
                            if (closestIcon != null)
                                GUI.DrawTexture(previewRect, closestIcon);
                            else
                                GUI.Box(previewRect, "");
                        }
                        // Save changes
                        if (GUI.changed)
                        {
                            Undo.RecordObject(this.target, Styles.undoChangedIconString);
                            PlayerSettings.SetIconsForPlatform(platformName, icons);
                        }
                        GUI.enabled = enabled;

                        if (targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.tvOS)
                        {
                            EditorGUILayout.PropertyField(m_UIPrerenderedIcon, Styles.UIPrerenderedIcon);
                            EditorGUILayout.Space();
                        }
                    }
                }

                if (settingsExtension != null)
                    settingsExtension.IconSectionGUI();
            }
            EndSettingsBox();
        }

        private static bool TargetSupportsOptionalBuiltinSplashScreen(BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension)
        {
            if (settingsExtension != null)
                return settingsExtension.CanShowUnitySplashScreen();

            return targetGroup == BuildTargetGroup.Standalone;
        }

        private static bool TargetSupportsProtectedGraphicsMem(BuildTargetGroup targetGroup)
        {
            return targetGroup == BuildTargetGroup.Android;
        }

        public void ResolutionSectionGUI(BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension, int sectionIndex = 0)
        {
            if (BeginSettingsBox(sectionIndex, Styles.resolutionPresentationTitle))
            {
                // PLEASE DO NOT COPY SETTINGS TO APPEAR MULTIPLE PLACES IN THE CODE! See top of file for more info.

                {
                    // Resolution itself

                    if (settingsExtension != null)
                    {
                        float h = EditorGUI.kSingleLineHeight;
                        float kLabelFloatMinW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
                        float kLabelFloatMaxW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
                        settingsExtension.ResolutionSectionGUI(h, kLabelFloatMinW, kLabelFloatMaxW);
                    }

                    if (targetGroup == BuildTargetGroup.Standalone)
                    {
                        GUILayout.Label(Styles.resolutionTitle, EditorStyles.boldLabel);

                        var fullscreenModes = new[] { FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.MaximizedWindow, FullScreenMode.Windowed };
                        var fullscreenModeNames = new[] { Styles.fullscreenWindow, Styles.exclusiveFullscreen, Styles.maximizedWindow, Styles.windowed };
                        var fullscreenModeNew = BuildEnumPopup(m_FullscreenMode, Styles.fullscreenMode, fullscreenModes, fullscreenModeNames);

                        bool defaultIsFullScreen = fullscreenModeNew != FullScreenMode.Windowed;
                        m_ShowDefaultIsNativeResolution.target = defaultIsFullScreen;
                        if (EditorGUILayout.BeginFadeGroup(m_ShowDefaultIsNativeResolution.faded))
                            EditorGUILayout.PropertyField(m_DefaultIsNativeResolution);
                        if (m_ShowDefaultIsNativeResolution.faded != 0 && m_ShowDefaultIsNativeResolution.faded != 1)
                            EditorGUILayout.EndFadeGroup();

                        m_ShowResolution.target = !(defaultIsFullScreen && m_DefaultIsNativeResolution.boolValue);
                        if (EditorGUILayout.BeginFadeGroup(m_ShowResolution.faded))
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(m_DefaultScreenWidth, Styles.defaultScreenWidth);
                            if (EditorGUI.EndChangeCheck() && m_DefaultScreenWidth.intValue < 1)
                                m_DefaultScreenWidth.intValue = 1;

                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(m_DefaultScreenHeight, Styles.defaultScreenHeight);
                            if (EditorGUI.EndChangeCheck() && m_DefaultScreenHeight.intValue < 1)
                                m_DefaultScreenHeight.intValue = 1;
                        }
                        if (m_ShowResolution.faded != 0 && m_ShowResolution.faded != 1)
                            EditorGUILayout.EndFadeGroup();
                    }
                    if (targetGroup == BuildTargetGroup.Standalone)
                    {
                        EditorGUILayout.PropertyField(m_MacRetinaSupport, Styles.macRetinaSupport);
                        EditorGUILayout.PropertyField(m_RunInBackground, Styles.runInBackground);
                    }

                    if (settingsExtension != null && settingsExtension.SupportsOrientation())
                    {
                        GUILayout.Label(Styles.orientationTitle, EditorStyles.boldLabel);

                        EditorGUILayout.PropertyField(m_DefaultScreenOrientation, Styles.defaultScreenOrientation);

                        if (PlayerSettings.virtualRealitySupported)
                        {
                            EditorGUILayout.HelpBox(Styles.vrOrientationInfo.text, MessageType.Warning);
                        }

                        if (m_DefaultScreenOrientation.enumValueIndex == (int)UIOrientation.AutoRotation)
                        {
                            if (targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.Tizen)
                                EditorGUILayout.PropertyField(m_UseOSAutoRotation, Styles.useOSAutoRotation);

                            EditorGUI.indentLevel++;

                            GUILayout.Label(Styles.allowedOrientationTitle, EditorStyles.boldLabel);

                            bool somethingAllowed =     m_AllowedAutoRotateToPortrait.boolValue
                                ||  m_AllowedAutoRotateToPortraitUpsideDown.boolValue
                                ||  m_AllowedAutoRotateToLandscapeRight.boolValue
                                ||  m_AllowedAutoRotateToLandscapeLeft.boolValue;

                            if (!somethingAllowed)
                            {
                                m_AllowedAutoRotateToPortrait.boolValue = true;
                                Debug.LogError("All orientations are disabled. Allowing portrait");
                            }

                            EditorGUILayout.PropertyField(m_AllowedAutoRotateToPortrait,            Styles.allowedAutoRotateToPortrait);
                            EditorGUILayout.PropertyField(m_AllowedAutoRotateToPortraitUpsideDown,  Styles.allowedAutoRotateToPortraitUpsideDown);
                            EditorGUILayout.PropertyField(m_AllowedAutoRotateToLandscapeRight,          Styles.allowedAutoRotateToLandscapeRight);
                            EditorGUILayout.PropertyField(m_AllowedAutoRotateToLandscapeLeft,           Styles.allowedAutoRotateToLandscapeLeft);

                            EditorGUI.indentLevel--;
                        }
                    }

                    if (targetGroup == BuildTargetGroup.iOS)
                    {
                        GUILayout.Label(Styles.multitaskingSupportTitle, EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(m_UIRequiresFullScreen, Styles.UIRequiresFullScreen);
                        EditorGUILayout.Space();

                        GUILayout.Label(Styles.statusBarTitle, EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(m_UIStatusBarHidden, Styles.UIStatusBarHidden);
                        EditorGUILayout.PropertyField(m_UIStatusBarStyle, Styles.UIStatusBarStyle);
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.Space();

                    // Standalone Player
                    if (targetGroup == BuildTargetGroup.Standalone)
                    {
                        GUILayout.Label(Styles.standalonePlayerOptionsTitle, EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(m_CaptureSingleScreen);
                        EditorGUILayout.PropertyField(m_DisplayResolutionDialog);
                        EditorGUILayout.PropertyField(m_UsePlayerLog);
                        EditorGUILayout.PropertyField(m_ResizableWindow);

                        EditorGUILayout.PropertyField(m_VisibleInBackground, Styles.visibleInBackground);

                        EditorGUILayout.PropertyField(m_AllowFullscreenSwitch, Styles.allowFullscreenSwitch);

                        EditorGUILayout.PropertyField(m_ForceSingleInstance);
                        EditorGUILayout.PropertyField(m_SupportedAspectRatios, true);

                        EditorGUILayout.Space();
                    }

                    // mobiles color/depth bits setup
                    if (IsMobileTarget(targetGroup))
                    {
                        // Tizen only supports 32 bit display buffers.
                        // iOS, while supports 16bit FB through GL interface, use 32bit in hardware, so there is no need in 16bit
                        if (targetGroup != BuildTargetGroup.Tizen &&
                            targetGroup != BuildTargetGroup.iOS &&
                            targetGroup != BuildTargetGroup.tvOS)
                        {
                            EditorGUILayout.PropertyField(m_Use32BitDisplayBuffer, Styles.use32BitDisplayBuffer);
                        }

                        EditorGUILayout.PropertyField(m_DisableDepthAndStencilBuffers, Styles.disableDepthAndStencilBuffers);
                    }
                    // activity indicator on loading
                    if (targetGroup == BuildTargetGroup.iOS)
                    {
                        EditorGUILayout.PropertyField(m_iosShowActivityIndicatorOnLoading, Styles.iosShowActivityIndicatorOnLoading);
                    }
                    if (targetGroup == BuildTargetGroup.Android)
                    {
                        EditorGUILayout.PropertyField(m_androidShowActivityIndicatorOnLoading, Styles.androidShowActivityIndicatorOnLoading);
                    }
                    if (targetGroup == BuildTargetGroup.Tizen)
                    {
                        EditorGUILayout.PropertyField(m_tizenShowActivityIndicatorOnLoading, EditorGUIUtility.TrTextContent("Show Loading Indicator"));
                    }
                    if (targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.Android || targetGroup == BuildTargetGroup.Tizen)
                    {
                        EditorGUILayout.Space();
                    }

                    ShowSharedNote();
                }
            }
            EndSettingsBox();
        }

        private void AddGraphicsDeviceMenuSelected(object userData, string[] options, int selected)
        {
            var target = (BuildTarget)userData;
            var apis = PlayerSettings.GetGraphicsAPIs(target);
            if (apis == null)
                return;
            var apiToAdd = (GraphicsDeviceType)Enum.Parse(typeof(GraphicsDeviceType), options[selected], true);
            var apiList = apis.ToList();
            apiList.Add(apiToAdd);
            apis = apiList.ToArray();
            PlayerSettings.SetGraphicsAPIs(target, apis);
        }

        private void AddGraphicsDeviceElement(BuildTarget target, Rect rect, ReorderableList list)
        {
            GraphicsDeviceType[] availableDevices = PlayerSettings.GetSupportedGraphicsAPIs(target);

            if (availableDevices == null || availableDevices.Length == 0)
                return;

            var names = new string[availableDevices.Length];
            var enabled = new bool[availableDevices.Length];
            for (int i = 0; i < availableDevices.Length; ++i)
            {
                names[i] = availableDevices[i].ToString();
                enabled[i] = !list.list.Contains(availableDevices[i]);
            }

            EditorUtility.DisplayCustomMenu(rect, names, enabled, null, AddGraphicsDeviceMenuSelected, target);
        }

        private bool CanRemoveGraphicsDeviceElement(ReorderableList list)
        {
            // don't allow removing the last API
            return list.list.Count >= 2;
        }

        private void RemoveGraphicsDeviceElement(BuildTarget target, ReorderableList list)
        {
            var apis = PlayerSettings.GetGraphicsAPIs(target);
            if (apis == null)
                return;
            // don't allow removing the last API
            if (apis.Length < 2)
            {
                EditorApplication.Beep();
                return;
            }

            var apiList = apis.ToList();
            apiList.RemoveAt(list.index);
            apis = apiList.ToArray();

            ApplyChangedGraphicsAPIList(target, apis, list.index == 0);
        }

        private void ReorderGraphicsDeviceElement(BuildTarget target, ReorderableList list)
        {
            var previousAPIs = PlayerSettings.GetGraphicsAPIs(target);
            var apiList = (List<GraphicsDeviceType>)list.list;
            var apis = apiList.ToArray();

            var firstAPIDifferent = (previousAPIs[0] != apis[0]);
            ApplyChangedGraphicsAPIList(target, apis, firstAPIDifferent);
        }

        // these two methods are needed for cases when you want to take some action depending on user choice
        // as changing graphics api will call GUIUtility.ExitGUI

        private struct ChangeGraphicsApiAction
        {
            public readonly bool changeList, reloadGfx;
            public ChangeGraphicsApiAction(bool doChange, bool doReload) { changeList = doChange; reloadGfx = doReload; }
        }
        private ChangeGraphicsApiAction CheckApplyGraphicsAPIList(BuildTarget target, bool firstEntryChanged)
        {
            bool doChange = true, doReload = false;
            // If we're changing the first API for relevant editor, this will cause editor to switch: ask for scene save & confirmation
            if (firstEntryChanged && WillEditorUseFirstGraphicsAPI(target))
            {
                doChange = false;
                if (EditorUtility.DisplayDialog("Changing editor graphics device",
                        "Changing active graphics API requires reloading all graphics objects, it might take a while",
                        "Apply", "Cancel"))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        doChange = doReload = true;
                }
            }
            return new ChangeGraphicsApiAction(doChange, doReload);
        }

        private void ApplyChangeGraphicsApiAction(BuildTarget target, GraphicsDeviceType[] apis, ChangeGraphicsApiAction action)
        {
            if (action.changeList)  PlayerSettings.SetGraphicsAPIs(target, apis);
            else                    s_GraphicsDeviceLists.Remove(target); // we cancelled the list change, so remove the cached one

            if (action.reloadGfx)
            {
                ShaderUtil.RecreateGfxDevice();
                GUIUtility.ExitGUI();
            }
        }

        private void ApplyChangedGraphicsAPIList(BuildTarget target, GraphicsDeviceType[] apis, bool firstEntryChanged)
        {
            ChangeGraphicsApiAction action = CheckApplyGraphicsAPIList(target, firstEntryChanged);
            ApplyChangeGraphicsApiAction(target, apis, action);
        }

        private void DrawGraphicsDeviceElement(BuildTarget target, Rect rect, int index, bool selected, bool focused)
        {
            var device = s_GraphicsDeviceLists[target].list[index];
            var name = device.ToString();
            if (name == "Direct3D12")
                name = "Direct3D12 (Experimental)";

            if (name == "Vulkan" && target != BuildTarget.Android)
                name = "Vulkan (Experimental)";

            if (name == "XboxOneD3D12")
                name = "XboxOneD3D12 (Experimental)";

            // For WebGL, display the actual WebGL version names instead of corresponding GLES APIs for clarification.
            if (target == BuildTarget.WebGL)
            {
                if (name == "OpenGLES3")
                    name = "WebGL 2.0";
                else if (name == "OpenGLES2")
                    name = "WebGL 1.0";
            }

            GUI.Label(rect, name, EditorStyles.label);
        }

        private static bool WillEditorUseFirstGraphicsAPI(BuildTarget targetPlatform)
        {
            return
                Application.platform == RuntimePlatform.WindowsEditor && targetPlatform == BuildTarget.StandaloneWindows ||
                Application.platform == RuntimePlatform.OSXEditor && targetPlatform == BuildTarget.StandaloneOSX;
        }

        void OpenGLES31OptionsGUI(BuildTargetGroup targetGroup, BuildTarget targetPlatform)
        {
            // ES3.1 options only applicable on some platforms now
            var hasES31Options = (targetGroup == BuildTargetGroup.Android);
            if (!hasES31Options)
                return;

            var apis = PlayerSettings.GetGraphicsAPIs(targetPlatform);
            // only available if we include ES3, and not ES2
            var hasMinES3 = apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
            if (!hasMinES3)
                return;

            EditorGUILayout.PropertyField(m_RequireES31, Styles.require31);
            EditorGUILayout.PropertyField(m_RequireES31AEP, Styles.requireAEP);
        }

        void GraphicsAPIsGUIOnePlatform(BuildTargetGroup targetGroup, BuildTarget targetPlatform, string platformTitle)
        {
            GraphicsDeviceType[] availableDevices = PlayerSettings.GetSupportedGraphicsAPIs(targetPlatform);
            // if no devices (e.g. no platform module), or we only have one possible choice, then no
            // point in having any UI
            if (availableDevices == null || availableDevices.Length < 2)
                return;

            // toggle for automatic API selection
            EditorGUI.BeginChangeCheck();
            var automatic = PlayerSettings.GetUseDefaultGraphicsAPIs(targetPlatform);
            automatic = EditorGUILayout.Toggle(string.Format(L10n.Tr("Auto Graphics API {0}"), (platformTitle ?? string.Empty)), automatic);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, Styles.undoChangedGraphicsAPIString);
                PlayerSettings.SetUseDefaultGraphicsAPIs(targetPlatform, automatic);
            }

            // graphics API list if not automatic
            if (!automatic)
            {
                // note that editor will use first item, when we're in standalone settings
                if (WillEditorUseFirstGraphicsAPI(targetPlatform))
                {
                    EditorGUILayout.HelpBox(Styles.recordingInfo.text, MessageType.Info, true);
                }

                var displayTitle = "Graphics APIs";
                if (platformTitle != null)
                    displayTitle += platformTitle;

                // create reorderable list for this target if needed
                if (!s_GraphicsDeviceLists.ContainsKey(targetPlatform))
                {
                    GraphicsDeviceType[] devices = PlayerSettings.GetGraphicsAPIs(targetPlatform);
                    var devicesList = (devices != null) ? devices.ToList() : new List<GraphicsDeviceType>();
                    var rlist = new ReorderableList(devicesList, typeof(GraphicsDeviceType), true, true, true, true);
                    rlist.onAddDropdownCallback = (rect, list) => AddGraphicsDeviceElement(targetPlatform, rect, list);
                    rlist.onCanRemoveCallback = CanRemoveGraphicsDeviceElement;
                    rlist.onRemoveCallback = (list) => RemoveGraphicsDeviceElement(targetPlatform, list);
                    rlist.onReorderCallback = (list) => ReorderGraphicsDeviceElement(targetPlatform, list);
                    rlist.drawElementCallback = (rect, index, isActive, isFocused) => DrawGraphicsDeviceElement(targetPlatform, rect, index, isActive, isFocused);
                    rlist.drawHeaderCallback = (rect) => GUI.Label(rect, displayTitle, EditorStyles.label);
                    rlist.elementHeight = 16;

                    s_GraphicsDeviceLists.Add(targetPlatform, rlist);
                }
                s_GraphicsDeviceLists[targetPlatform].DoLayoutList();

                // ES3.1 options
                OpenGLES31OptionsGUI(targetGroup, targetPlatform);

                //@TODO: undo
            }
        }

        void GraphicsAPIsGUI(BuildTargetGroup targetGroup, BuildTarget target)
        {
            // "standalone" is a generic group;
            // split it into win/mac/linux manually
            if (targetGroup == BuildTargetGroup.Standalone)
            {
                GraphicsAPIsGUIOnePlatform(targetGroup, BuildTarget.StandaloneWindows, " for Windows");
                GraphicsAPIsGUIOnePlatform(targetGroup, BuildTarget.StandaloneOSX, " for Mac");
                GraphicsAPIsGUIOnePlatform(targetGroup, BuildTarget.StandaloneLinuxUniversal, " for Linux");
            }
            else
            {
                GraphicsAPIsGUIOnePlatform(targetGroup, target, null);
            }
        }

        // Contains information about color gamuts supported by each platform.
        // If platform group is not in the dictionary, then it's assumed it supports only sRGB.
        // Color gamut player setting is not displayed for such platforms.
        //
        // This information might be useful for users that use the color gamut APIs,
        // we could expose it somehow
        private static Dictionary<BuildTargetGroup, List<ColorGamut>> s_SupportedColorGamuts =
            new Dictionary<BuildTargetGroup, List<ColorGamut>>
        {
            { BuildTargetGroup.Standalone, new List<ColorGamut>{ ColorGamut.sRGB, ColorGamut.DisplayP3 } },
            { BuildTargetGroup.iOS, new List<ColorGamut>{ ColorGamut.sRGB, ColorGamut.DisplayP3 } }
        };

        private static bool IsColorGamutSupportedOnTargetGroup(BuildTargetGroup targetGroup, ColorGamut gamut)
        {
            if (gamut == ColorGamut.sRGB)
                return true;
            if (s_SupportedColorGamuts.ContainsKey(targetGroup) &&
                s_SupportedColorGamuts[targetGroup].Contains(gamut))
                return true;
            return false;
        }

        private static string GetColorGamutDisplayString(BuildTargetGroup targetGroup, ColorGamut gamut)
        {
            string name = gamut.ToString();
            if (!IsColorGamutSupportedOnTargetGroup(targetGroup, gamut))
                name += " (not supported on this platform)";
            return name;
        }

        private void AddColorGamutElement(BuildTargetGroup targetGroup, Rect rect, ReorderableList list)
        {
            var availableColorGamuts = new ColorGamut[]
            {
                // Enable the gamuts when at least one platform supports them
                ColorGamut.sRGB,
                //ColorGamut.Rec709,
                //ColorGamut.Rec2020,
                ColorGamut.DisplayP3,
                //ColorGamut.HDR10,
                //ColorGamut.DolbyHDR
            };

            var names = new string[availableColorGamuts.Length];
            var enabled = new bool[availableColorGamuts.Length];
            for (int i = 0; i < availableColorGamuts.Length; ++i)
            {
                names[i] = GetColorGamutDisplayString(targetGroup, availableColorGamuts[i]);
                enabled[i] = !list.list.Contains(availableColorGamuts[i]);
            }

            EditorUtility.DisplayCustomMenu(rect, names, enabled, null, AddColorGamutMenuSelected, availableColorGamuts);
        }

        private void AddColorGamutMenuSelected(object userData, string[] options, int selected)
        {
            var colorGamuts = (ColorGamut[])userData;
            var colorGamutList = PlayerSettings.GetColorGamuts().ToList();
            colorGamutList.Add(colorGamuts[selected]);
            PlayerSettings.SetColorGamuts(colorGamutList.ToArray());
        }

        private bool CanRemoveColorGamutElement(ReorderableList list)
        {
            // don't allow removing the sRGB
            var colorGamutList = (List<ColorGamut>)list.list;
            return colorGamutList[list.index] != ColorGamut.sRGB;
        }

        private void RemoveColorGamutElement(ReorderableList list)
        {
            var colorGamutList = PlayerSettings.GetColorGamuts().ToList();
            // don't allow removing the last ColorGamut
            if (colorGamutList.Count < 2)
            {
                EditorApplication.Beep();
                return;
            }
            colorGamutList.RemoveAt(list.index);
            PlayerSettings.SetColorGamuts(colorGamutList.ToArray());
        }

        private void ReorderColorGamutElement(ReorderableList list)
        {
            var colorGamutList = (List<ColorGamut>)list.list;
            PlayerSettings.SetColorGamuts(colorGamutList.ToArray());
        }

        private void DrawColorGamutElement(BuildTargetGroup targetGroup, Rect rect, int index, bool selected, bool focused)
        {
            var colorGamut = s_ColorGamutList.list[index];
            GUI.Label(rect, GetColorGamutDisplayString(targetGroup, (ColorGamut)colorGamut), EditorStyles.label);
        }

        void ColorGamutGUI(BuildTargetGroup targetGroup)
        {
            if (!s_SupportedColorGamuts.ContainsKey(targetGroup))
                return;

            if (s_ColorGamutList == null)
            {
                ColorGamut[] colorGamuts = PlayerSettings.GetColorGamuts();
                var colorGamutsList = (colorGamuts != null) ? colorGamuts.ToList() : new List<ColorGamut>();
                var rlist = new ReorderableList(colorGamutsList, typeof(ColorGamut), true, true, true, true);
                rlist.onCanRemoveCallback = CanRemoveColorGamutElement;
                rlist.onRemoveCallback = RemoveColorGamutElement;
                rlist.onReorderCallback = ReorderColorGamutElement;
                rlist.elementHeight = 16;

                s_ColorGamutList = rlist;
            }

            // On standalone inspector mention that the setting applies only to Mac
            // (Temporarily until other standalones support this setting)
            GUIContent header = targetGroup == BuildTargetGroup.Standalone ? Styles.colorGamutForMac : Styles.colorGamut;
            s_ColorGamutList.drawHeaderCallback = (rect) =>
                GUI.Label(rect, header, EditorStyles.label);

            // we want to change the displayed text per platform, to indicate unsupported gamuts
            s_ColorGamutList.onAddDropdownCallback = (rect, list) =>
                AddColorGamutElement(targetGroup, rect, list);

            s_ColorGamutList.drawElementCallback = (rect, index, selected, focused) =>
                DrawColorGamutElement(targetGroup, rect, index, selected, focused);

            s_ColorGamutList.DoLayoutList();
        }

        public void DebugAndCrashReportingGUI(BuildPlatform platform, BuildTargetGroup targetGroup,
            ISettingEditorExtension settingsExtension, int sectionIndex = 3)
        {
            if (targetGroup != BuildTargetGroup.iOS && targetGroup != BuildTargetGroup.tvOS)
                return;

            if (BeginSettingsBox(sectionIndex, Styles.debuggingCrashReportingTitle))
            {
                // PLEASE DO NOT COPY SETTINGS TO APPEAR MULTIPLE PLACES IN THE CODE! See top of file for more info.
                {
                    // Debugging
                    GUILayout.Label(Styles.debuggingTitle, EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(m_EnableInternalProfiler, Styles.enableInternalProfiler);
                    EditorGUILayout.Space();
                }

                {
                    // Crash reporting
                    GUILayout.Label(Styles.crashReportingTitle, EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(m_ActionOnDotNetUnhandledException, Styles.actionOnDotNetUnhandledException);
                    EditorGUILayout.PropertyField(m_LogObjCUncaughtExceptions, Styles.logObjCUncaughtExceptions);

                    GUIContent crashReportApiContent = Styles.enableCrashReportAPI;

                    bool apiFieldDisabled = false;

                    if (UnityEditor.CrashReporting.CrashReportingSettings.enabled)
                    {
                        // CrashReport API must be enabled if cloud crash reporting is enabled,
                        // so don't let them change the value of the checkbox
                        crashReportApiContent = new GUIContent(crashReportApiContent);  // Create a copy so we don't alter the style definition
                        apiFieldDisabled = true;
                        crashReportApiContent.tooltip = "CrashReport API must be enabled for Performance Reporting service.";
                        m_EnableCrashReportAPI.boolValue = true;
                    }

                    EditorGUI.BeginDisabledGroup(apiFieldDisabled);
                    EditorGUILayout.PropertyField(m_EnableCrashReportAPI, crashReportApiContent);
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Space();
                }
            }
            EndSettingsBox();
        }

        public static void BuildDisabledEnumPopup(GUIContent selected, GUIContent uiString)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.Popup(EditorGUILayout.GetControlRect(true), uiString, 0, new GUIContent[] {selected});
            }
        }

        public static T BuildEnumPopup<T>(SerializedProperty prop, GUIContent uiString, T[] options, GUIContent[] optionNames)
        {
            T val = (T)(object)prop.intValue;
            T newVal = BuildEnumPopup(uiString, val, options, optionNames);

            // Update property if the popup value has changed
            if (!newVal.Equals(val))
            {
                prop.intValue = (int)(object)newVal;
                prop.serializedObject.ApplyModifiedProperties();
            }

            return newVal;
        }

        public static T BuildEnumPopup<T>(GUIContent uiString, T selected, T[] options, GUIContent[] optionNames)
        {
            // Display dropdown
            int idx = 0; // pick the first property when not found
            for (int i = 1; i < options.Length; ++i)
            {
                if (selected.Equals(options[i]))
                {
                    idx = i;
                    break;
                }
            }

            int newIdx = EditorGUILayout.Popup(uiString, idx, optionNames);
            return options[newIdx];
        }

        public void OtherSectionGUI(BuildPlatform platform, BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension, int sectionIndex = 4)
        {
            if (BeginSettingsBox(sectionIndex, Styles.otherSettingsTitle))
            {
                // PLEASE DO NOT COPY SETTINGS TO APPEAR MULTIPLE PLACES IN THE CODE! See top of file for more info.
                OtherSectionRenderingGUI(platform, targetGroup, settingsExtension);
                OtherSectionIdentificationGUI(targetGroup, settingsExtension);
                OtherSectionConfigurationGUI(targetGroup, settingsExtension);
                OtherSectionOptimizationGUI(targetGroup);
                OtherSectionLoggingGUI();
                ShowSharedNote();
            }
            EndSettingsBox();
        }

        [Serializable]
        private struct HwStatsServiceState { public bool hwstats; }

        private void OtherSectionRenderingGUI(BuildPlatform platform, BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension)
        {
            // Rendering related settings
            GUILayout.Label(Styles.renderingTitle, EditorStyles.boldLabel);

            // Color space
            if (targetGroup == BuildTargetGroup.Standalone
                || targetGroup == BuildTargetGroup.iOS
                || targetGroup == BuildTargetGroup.tvOS
                || targetGroup == BuildTargetGroup.Android
                || targetGroup == BuildTargetGroup.PS4
                || targetGroup == BuildTargetGroup.XboxOne
                || targetGroup == BuildTargetGroup.WSA
                || targetGroup == BuildTargetGroup.WebGL
                || targetGroup == BuildTargetGroup.Switch)
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying)) // switching color spaces in play mode is not supported
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_ActiveColorSpace, Styles.activeColorSpace);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI(); // Fixes case 690421
                    }
                }

                // Display a warning for platforms that some devices don't support linear rendering if the settings are not fine for linear colorspace
                if (PlayerSettings.colorSpace == ColorSpace.Linear)
                {
                    if (targetGroup == BuildTargetGroup.iOS)
                    {
                        var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
                        var hasMinMetal = !apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);

                        Version requiredVersion = new Version(8, 0);
                        bool hasMinOSVersion = PlayerSettings.iOS.IsTargetVersionEqualOrHigher(requiredVersion);

                        if (!hasMinMetal || !hasMinOSVersion)
                            EditorGUILayout.HelpBox(Styles.colorSpaceIOSWarning.text, MessageType.Warning);
                    }

                    if (targetGroup == BuildTargetGroup.tvOS)
                    {
                        var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.tvOS);
                        var hasMinMetal = !apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);

                        if (!hasMinMetal)
                            EditorGUILayout.HelpBox(Styles.colorSpaceTVOSWarning.text, MessageType.Warning);
                    }

                    if (targetGroup == BuildTargetGroup.Android)
                    {
                        var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                        var hasMinAPI = (apis.Contains(GraphicsDeviceType.Vulkan) || apis.Contains(GraphicsDeviceType.OpenGLES3)) && !apis.Contains(GraphicsDeviceType.OpenGLES2);

                        var hasBlitDisabled = PlayerSettings.Android.blitType == AndroidBlitType.Never;
                        if (hasBlitDisabled || !hasMinAPI || (int)PlayerSettings.Android.minSdkVersion < 18)
                            EditorGUILayout.HelpBox(Styles.colorSpaceAndroidWarning.text, MessageType.Warning);
                    }

                    if (targetGroup == BuildTargetGroup.WebGL)
                    {
                        var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
                        var hasMinAPI = apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);

                        if (!hasMinAPI)
                            EditorGUILayout.HelpBox(Styles.colorSpaceWebGLWarning.text, MessageType.Error);
                    }
                }
            }

            // Graphics APIs
            GraphicsAPIsGUI(targetGroup, platform.defaultTarget);

            // Output color spaces
            ColorGamutGUI(targetGroup);

            // Metal
            if (Application.platform == RuntimePlatform.OSXEditor && (targetGroup == BuildTargetGroup.Standalone || targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.tvOS))
            {
                bool curMetalSupport = m_MetalEditorSupport.boolValue || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal;
                bool newMetalSupport = EditorGUILayout.Toggle(Styles.metalEditorSupport, curMetalSupport);

                if (newMetalSupport != curMetalSupport)
                {
                    if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        GraphicsDeviceType[] api = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneOSX);

                        bool updateCurrentAPI = api[0] != SystemInfo.graphicsDeviceType;
                        if (!newMetalSupport && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                            updateCurrentAPI = true; // running on metal and disabled it
                        if (newMetalSupport && api[0] == GraphicsDeviceType.Metal)
                            updateCurrentAPI = true; // just enabled metal so want to switch to it

                        ChangeGraphicsApiAction action = CheckApplyGraphicsAPIList(BuildTarget.StandaloneOSX, updateCurrentAPI);
                        if (action.changeList)
                        {
                            m_MetalEditorSupport.boolValue = newMetalSupport;
                            serializedObject.ApplyModifiedProperties();
                            // HACK: we pretended to change first api in list to trigger possible gfx device recreation
                            // HACK: but we dont really change api list (as we will simply set bool checked from native code)
                            action = new ChangeGraphicsApiAction(false, action.reloadGfx);
                        }
                        ApplyChangeGraphicsApiAction(BuildTarget.StandaloneOSX, api, action);
                    }
                    else
                    {
                        m_MetalEditorSupport.boolValue = newMetalSupport;
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                if (m_MetalEditorSupport.boolValue)
                {
                    using (new EditorGUI.IndentLevelScope())
                        m_MetalAPIValidation.boolValue = EditorGUILayout.Toggle(Styles.metalAPIValidation, m_MetalAPIValidation.boolValue);
                }

                EditorGUILayout.PropertyField(m_MetalFramebufferOnly, Styles.metalFramebufferOnly);
                if (targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.tvOS)
                    EditorGUILayout.PropertyField(m_MetalForceHardShadows, Styles.metalForceHardShadows);
            }

            // Multithreaded rendering
            if (settingsExtension != null && settingsExtension.SupportsMultithreadedRendering())
                settingsExtension.MultithreadedRenderingGUI(targetGroup);

            // Batching section
            {
                int staticBatching, dynamicBatching;
                bool staticBatchingSupported = true;
                bool dynamicBatchingSupported = true;
                if (settingsExtension != null)
                {
                    staticBatchingSupported = settingsExtension.SupportsStaticBatching();
                    dynamicBatchingSupported = settingsExtension.SupportsDynamicBatching();
                }
                PlayerSettings.GetBatchingForPlatform(platform.defaultTarget, out staticBatching, out dynamicBatching);

                bool reset = false;
                if (staticBatchingSupported == false && staticBatching == 1)
                {
                    staticBatching = 0;
                    reset = true;
                }

                if (dynamicBatchingSupported == false && dynamicBatching == 1)
                {
                    dynamicBatching = 0;
                    reset = true;
                }

                if (reset)
                {
                    PlayerSettings.SetBatchingForPlatform(platform.defaultTarget, staticBatching, dynamicBatching);
                }

                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.DisabledScope(!staticBatchingSupported))
                {
                    if (GUI.enabled)
                        staticBatching = EditorGUILayout.Toggle(Styles.staticBatching, staticBatching != 0) ? 1 : 0;
                    else
                        EditorGUILayout.Toggle(Styles.staticBatching, false);
                }

                using (new EditorGUI.DisabledScope(!dynamicBatchingSupported))
                {
                    dynamicBatching = EditorGUILayout.Toggle(Styles.dynamicBatching, dynamicBatching != 0) ? 1 : 0;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, Styles.undoChangedBatchingString);
                    PlayerSettings.SetBatchingForPlatform(platform.defaultTarget, staticBatching, dynamicBatching);
                }
            }


            bool hdrSupported = false;
            bool gfxJobModesSupported = false;
            bool customLightmapEncodingSupported = (targetGroup == BuildTargetGroup.Standalone);
            if (settingsExtension != null)
            {
                hdrSupported = settingsExtension.SupportsHighDynamicRangeDisplays();
                gfxJobModesSupported = settingsExtension.SupportsGfxJobModes();
                customLightmapEncodingSupported = customLightmapEncodingSupported || settingsExtension.SupportsCustomLightmapEncoding();
            }

            // GPU Skinning toggle (only show on relevant platforms)
            if (targetGroup == BuildTargetGroup.Standalone ||
                targetGroup == BuildTargetGroup.iOS ||
                targetGroup == BuildTargetGroup.tvOS ||
                targetGroup == BuildTargetGroup.Android ||
                targetGroup == BuildTargetGroup.PSP2 ||
                targetGroup == BuildTargetGroup.PS4 ||
                targetGroup == BuildTargetGroup.XboxOne ||
                targetGroup == BuildTargetGroup.WSA ||
                targetGroup == BuildTargetGroup.Switch)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_SkinOnGPU,
                    targetGroup != BuildTargetGroup.PS4 && targetGroup != BuildTargetGroup.Switch ? Styles.skinOnGPU : Styles.skinOnGPUPS4);
                if (EditorGUI.EndChangeCheck())
                {
                    ShaderUtil.RecreateSkinnedMeshResources();
                }
            }

            if ((targetGroup == BuildTargetGroup.Android) && PlayerSettings.gpuSkinning)
            {
                EditorGUILayout.HelpBox(Styles.skinOnGPUAndroidWarning.text, MessageType.Warning);
            }

            if (targetGroup == BuildTargetGroup.XboxOne)
            {
                // on XBoxOne, we only have kGfxJobModeNative active for Dx12 API and kGfxJobModeLegacy for the DX11 API
                // no need for a drop down popup for XBoxOne
                // also if XboxOneD3D12 is selected as GraphicsAPI, then we want to check the graphics jobs checkbox and disable it.
                GraphicsDeviceType[] gfxAPIs = PlayerSettings.GetGraphicsAPIs(platform.defaultTarget);

                PlayerSettings.graphicsJobMode = gfxAPIs[0] == GraphicsDeviceType.XboxOneD3D12 ? GraphicsJobMode.Native : GraphicsJobMode.Legacy;
                if (gfxAPIs[0] == GraphicsDeviceType.XboxOneD3D12)
                    PlayerSettings.graphicsJobs = true;
                using (new EditorGUI.DisabledScope(gfxAPIs[0] == GraphicsDeviceType.XboxOneD3D12))
                {
                    EditorGUILayout.PropertyField(m_GraphicsJobs, Styles.graphicsJobs);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(m_GraphicsJobs, Styles.graphicsJobs);
                if (gfxJobModesSupported)
                {
                    using (new EditorGUI.DisabledScope(!m_GraphicsJobs.boolValue))
                    {
                        GraphicsJobMode currGfxJobMode = PlayerSettings.graphicsJobMode;

                        GraphicsJobMode newGfxJobMode = BuildEnumPopup(Styles.graphicsJobsMode, currGfxJobMode, m_GfxJobModeValues, m_GfxJobModeNames);
                        if (newGfxJobMode != currGfxJobMode)
                        {
                            PlayerSettings.graphicsJobMode = newGfxJobMode;
                        }
                    }
                }
            }

            // Show Lightmap Encoding quality option
            if (customLightmapEncodingSupported)
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || Lightmapping.isRunning))
                {
                    EditorGUI.BeginChangeCheck();
                    LightmapEncodingQuality encodingQuality = PlayerSettings.GetLightmapEncodingQualityForPlatformGroup(targetGroup);
                    LightmapEncodingQuality[] lightmapEncodingValues = {LightmapEncodingQuality.Normal, LightmapEncodingQuality.High};
                    encodingQuality = BuildEnumPopup(Styles.lightmapEncodingLabel, encodingQuality, lightmapEncodingValues, Styles.lightmapEncodingNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayerSettings.SetLightmapEncodingQualityForPlatformGroup(targetGroup, encodingQuality);

                        Lightmapping.OnUpdateLightmapEncoding(targetGroup);

                        serializedObject.ApplyModifiedProperties();

                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (m_VRSettings.TargetGroupSupportsVirtualReality(targetGroup))
            {
                if (EditorGUILayout.LinkLabel(Styles.vrSettingsMoved))
                {
                    m_SelectedSection.value = m_VRSettings.GUISectionIndex;
                }
            }

            if (TargetSupportsProtectedGraphicsMem(targetGroup))
            {
                PlayerSettings.protectGraphicsMemory = EditorGUILayout.Toggle(Styles.protectGraphicsMemory, PlayerSettings.protectGraphicsMemory);
            }

            if (hdrSupported)
            {
                PlayerSettings.useHDRDisplay = EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Use display in HDR mode", "Automatically switch the display to HDR output (on supported displays) at start of application."), PlayerSettings.useHDRDisplay);
            }

            EditorGUILayout.Space();
        }

        private void OtherSectionIdentificationGUI(BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension)
        {
            // Identification

            if (settingsExtension != null && settingsExtension.HasIdentificationGUI())
            {
                GUILayout.Label(Styles.identificationTitle, EditorStyles.boldLabel);
                settingsExtension.IdentificationSectionGUI();

                EditorGUILayout.Space();
            }
            else if (targetGroup == BuildTargetGroup.Standalone)
            {
                // TODO this should be move to an extension if we have one for MacOS or Standalone target at some point.
                GUILayout.Label(Styles.macAppStoreTitle, EditorStyles.boldLabel);

                PlayerSettingsEditor.ShowApplicationIdentifierUI(serializedObject, BuildTargetGroup.Standalone, "Bundle Identifier", "'CFBundleIdentifier'", Styles.undoChangedBundleIdentifierString);
                EditorGUILayout.PropertyField(m_ApplicationBundleVersion, EditorGUIUtility.TrTextContent("Version*", "'CFBundleShortVersionString'"));
                PlayerSettingsEditor.ShowBuildNumberUI(serializedObject, BuildTargetGroup.Standalone, "Build", "'CFBundleVersion'", Styles.undoChangedBuildNumberString);

                EditorGUILayout.PropertyField(m_MacAppStoreCategory, Styles.macAppStoreCategory);
                EditorGUILayout.PropertyField(m_UseMacAppStoreValidation, Styles.useMacAppStoreValidation);

                EditorGUILayout.Space();
            }
        }

        internal static void ShowPlatformIconsByKind(PlatformIconFieldGroup iconFieldGroup, bool foldByKind = true, bool foldBySubkind = true)
        {
            int labelHeight = 20;

            if (iconFieldGroup.m_IconsFields.Count == 0)
            {
                foreach (var kind in PlayerSettings.GetSupportedIconKindsForPlatform(iconFieldGroup.targetGroup))
                {
                    iconFieldGroup.AddPlatformIcons(PlayerSettings.GetPlatformIcons(
                            iconFieldGroup.targetGroup, kind), kind
                        );
                }
            }
            foreach (var kindGroup in iconFieldGroup.m_IconsFields)
            {
                EditorGUI.BeginChangeCheck();

                var key = kindGroup.Key;

                if (foldByKind)
                {
                    string kindName = string.Format("{0} icons ({1}/{2})", key.m_Label, kindGroup.Key.m_SetIconSlots, kindGroup.Key.m_IconSlotCount);
                    Rect rectKindLabel = GUILayoutUtility.GetRect(kSlotSize, labelHeight);
                    rectKindLabel.x += 2;
                    key.m_State = EditorGUI.Foldout(rectKindLabel, key.m_State, kindName, EditorStyles.foldout);
                }
                else
                    key.m_State = true;

                if (key.m_State)
                {
                    kindGroup.Key.m_SetIconSlots = 0;
                    foreach (var subKindGroup in kindGroup.Value)
                    {
                        subKindGroup.Key.m_SetIconSlots =
                            PlayerSettings.GetNonEmptyPlatformIconCount(subKindGroup.Value.Select(x => x.platformIcon)
                                .ToArray());
                        kindGroup.Key.m_SetIconSlots += subKindGroup.Key.m_SetIconSlots;

                        if (foldBySubkind)
                        {
                            string subKindName = string.Format("{0} icons ({1}/{2})", subKindGroup.Key.m_Label, subKindGroup.Key.m_SetIconSlots , subKindGroup.Value.Length);
                            Rect rectSubKindLabel = GUILayoutUtility.GetRect(kSlotSize, labelHeight);
                            rectSubKindLabel.x += 8;

                            subKindGroup.Key.m_State = EditorGUI.Foldout(rectSubKindLabel, subKindGroup.Key.m_State, subKindName, EditorStyles.foldout);
                        }
                        else
                            subKindGroup.Key.m_State = true;

                        if (subKindGroup.Key.m_State || !foldBySubkind)
                        {
                            foreach (var iconField in subKindGroup.Value)
                            {
                                iconField.DrawAt();
                            }
                        }
                    }
                }
                if (EditorGUI.EndChangeCheck())
                    PlayerSettings.SetPlatformIcons(iconFieldGroup.targetGroup, key.m_Kind , iconFieldGroup.m_PlatformIconsByKind[key.m_Kind]);
            }
        }

        internal static void ShowApplicationIdentifierUI(SerializedObject serializedObject, BuildTargetGroup targetGroup, string label, string tooltip, string undoText)
        {
            EditorGUI.BeginChangeCheck();
            string identifier = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent(label, tooltip), PlayerSettings.GetApplicationIdentifier(targetGroup));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(serializedObject.targetObject, undoText);
                PlayerSettings.SetApplicationIdentifier(targetGroup, identifier);
            }
        }

        internal static void ShowBuildNumberUI(SerializedObject serializedObject, BuildTargetGroup targetGroup, string label, string tooltip, string undoText)
        {
            EditorGUI.BeginChangeCheck();
            string buildNumber = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent(label, tooltip), PlayerSettings.GetBuildNumber(targetGroup));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(serializedObject.targetObject, undoText);
                PlayerSettings.SetBuildNumber(targetGroup, buildNumber);
            }
        }

        private void OtherSectionConfigurationGUI(BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension)
        {
            // Configuration
            GUILayout.Label(Styles.configurationTitle, EditorStyles.boldLabel);

            // Scripting Runtime Version
            var scriptingRuntimeVersions = new[] {ScriptingRuntimeVersion.Legacy, ScriptingRuntimeVersion.Latest};
            var scriptingRuntimeVersionNames = new[] {Styles.scriptingRuntimeVersionLegacy, Styles.scriptingRuntimeVersionLatest};
            var newScriptingRuntimeVersions = PlayerSettings.scriptingRuntimeVersion;
            if (EditorApplication.isPlaying)
            {
                var current = PlayerSettings.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy ? Styles.scriptingRuntimeVersionLegacy : Styles.scriptingRuntimeVersionLatest;
                BuildDisabledEnumPopup(current, Styles.scriptingRuntimeVersion);
            }
            else
            {
                newScriptingRuntimeVersions = BuildEnumPopup(Styles.scriptingRuntimeVersion, PlayerSettings.scriptingRuntimeVersion, scriptingRuntimeVersions, scriptingRuntimeVersionNames);
            }

            if (newScriptingRuntimeVersions != PlayerSettings.scriptingRuntimeVersion)
            {
                if (EditorUtility.DisplayDialog(
                        LocalizationDatabase.GetLocalizedString("Restart required"),
                        LocalizationDatabase.GetLocalizedString("Changing scripting runtime version requires a restart of the Editor to take effect. Do you wish to proceed?"),
                        LocalizationDatabase.GetLocalizedString("Restart"),
                        LocalizationDatabase.GetLocalizedString("Cancel")))
                {
                    PlayerSettings.scriptingRuntimeVersion = newScriptingRuntimeVersions;
                    EditorCompilationInterface.Instance.CleanScriptAssemblies();
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorApplication.OpenProject(Environment.CurrentDirectory);
                }
            }

            // Scripting back-end
            IScriptingImplementations scripting = ModuleManager.GetScriptingImplementations(targetGroup);
            bool targetGroupSupportsIl2Cpp = false;
            bool currentBackendIsIl2Cpp = false;

            if (scripting == null)
            {
                BuildDisabledEnumPopup(Styles.scriptingDefault, Styles.scriptingBackend);
            }
            else
            {
                var backends = scripting.Enabled();

                foreach (var backend in backends)
                {
                    if (backend == ScriptingImplementation.IL2CPP)
                    {
                        targetGroupSupportsIl2Cpp = true;
                        break;
                    }
                }

                ScriptingImplementation currBackend = PlayerSettings.GetScriptingBackend(targetGroup);
                currentBackendIsIl2Cpp = currBackend == ScriptingImplementation.IL2CPP;
                ScriptingImplementation newBackend;

                if (targetGroup == BuildTargetGroup.tvOS)
                {
                    newBackend = ScriptingImplementation.IL2CPP;
                    PlayerSettingsEditor.BuildDisabledEnumPopup(Styles.scriptingIL2CPP, Styles.scriptingBackend);
                }
                else
                {
                    newBackend = BuildEnumPopup(Styles.scriptingBackend, currBackend, backends, GetNiceScriptingBackendNames(backends));
                }

                if (targetGroup == BuildTargetGroup.iOS && newBackend == ScriptingImplementation.Mono2x)
                {
                    EditorGUILayout.HelpBox(Styles.monoNotSupportediOS11WarningGUIContent.text, MessageType.Warning);
                }

                if (newBackend != currBackend)
                    PlayerSettings.SetScriptingBackend(targetGroup, newBackend);
            }

            // Api Compatibility Level
            var currentCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(targetGroup);
            var availableCompatibilityLevels = GetAvailableApiCompatibilityLevels(targetGroup);

            var newCompatibilityLevel = BuildEnumPopup(Styles.apiCompatibilityLevel, currentCompatibilityLevel, availableCompatibilityLevels, GetNiceApiCompatibilityLevelNames(availableCompatibilityLevels));

            if (currentCompatibilityLevel != newCompatibilityLevel)
                PlayerSettings.SetApiCompatibilityLevel(targetGroup, newCompatibilityLevel);

            if (targetGroupSupportsIl2Cpp)
            {
                using (new EditorGUI.DisabledScope(!currentBackendIsIl2Cpp || !scripting.AllowIL2CPPCompilerConfigurationSelection()))
                {
                    var currentConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(targetGroup);
                    var configurations = GetIl2CppCompilerConfigurations();
                    var configurationNames = GetIl2CppCompilerConfigurationNames();

                    var newConfiguration = BuildEnumPopup(Styles.il2cppCompilerConfiguration, currentConfiguration, configurations, configurationNames);

                    if (currentConfiguration != newConfiguration)
                        PlayerSettings.SetIl2CppCompilerConfiguration(targetGroup, newConfiguration);
                }
            }

            bool showMobileSection =
                targetGroup == BuildTargetGroup.iOS ||
                targetGroup == BuildTargetGroup.tvOS ||
                targetGroup == BuildTargetGroup.Android ||
                targetGroup == BuildTargetGroup.WSA;

            // mobile-only settings
            if (showMobileSection)
            {
                if (targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.tvOS)
                    EditorGUILayout.PropertyField(m_useOnDemandResources, Styles.useOnDemandResources);

                bool supportsAccelerometerFrequency =
                    targetGroup == BuildTargetGroup.iOS ||
                    targetGroup == BuildTargetGroup.tvOS ||
                    targetGroup == BuildTargetGroup.WSA;
                if (supportsAccelerometerFrequency)
                    EditorGUILayout.PropertyField(m_AccelerometerFrequency, Styles.accelerometerFrequency);

                if (targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.tvOS)
                {
                    EditorGUILayout.PropertyField(m_CameraUsageDescription, Styles.cameraUsageDescription);
                    EditorGUILayout.PropertyField(m_LocationUsageDescription, Styles.locationUsageDescription);
                    EditorGUILayout.PropertyField(m_MicrophoneUsageDescription, Styles.microphoneUsageDescription);
                }

                if (targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.tvOS || targetGroup == BuildTargetGroup.Android)
                {
                    EditorGUILayout.PropertyField(m_MuteOtherAudioSources, Styles.muteOtherAudioSources);
                }

                // TVOS TODO: check what should stay or go
                if (targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.tvOS)
                {
                    if (targetGroup == BuildTargetGroup.iOS)
                    {
                        EditorGUILayout.PropertyField(m_PrepareIOSForRecording, Styles.prepareIOSForRecording);
                        EditorGUILayout.PropertyField(m_ForceIOSSpeakersWhenRecording, Styles.forceIOSSpeakersWhenRecording);
                    }
                    EditorGUILayout.PropertyField(m_UIRequiresPersistentWiFi, Styles.UIRequiresPersistentWiFi);
                    EditorGUILayout.PropertyField(m_IOSAllowHTTPDownload, Styles.iOSAllowHTTPDownload);
                    EditorGUILayout.PropertyField(m_IOSURLSchemes, Styles.iOSURLSchemes, true);
                }
            }

            using (new EditorGUI.DisabledScope(!Application.HasProLicense()))
            {
                bool oldDisableAnalytics = !m_SubmitAnalytics.boolValue;
                bool newDisableAnalytics = EditorGUILayout.Toggle(Styles.disableStatistics, oldDisableAnalytics);
                if (oldDisableAnalytics != newDisableAnalytics)
                {
                    m_SubmitAnalytics.boolValue = !newDisableAnalytics;
                    EditorAnalytics.SendEventServiceInfo(new HwStatsServiceState() { hwstats = !newDisableAnalytics });
                }
                if (!Application.HasProLicense())
                    m_SubmitAnalytics.boolValue = true;
            }

            if (settingsExtension != null)
                settingsExtension.ConfigurationSectionGUI();


            // User script defines
            {
                EditorGUILayout.LabelField(Styles.scriptingDefineSymbols);
                EditorGUI.BeginChangeCheck();
                string scriptDefines = EditorGUILayout.DelayedTextField(PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup), EditorStyles.textField);
                scriptingDefinesControlID = EditorGUIUtility.s_LastControlID;
                if (EditorGUI.EndChangeCheck())
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, scriptDefines);
            }

            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_AllowUnsafeCode, Styles.allowUnsafeCode);
                if (EditorGUI.EndChangeCheck())
                {
                    PlayerSettings.allowUnsafeCode = m_AllowUnsafeCode.boolValue;
                }
            }

            // Active input handling
            int inputOption = (!m_EnableInputSystem.boolValue) ? 0 : m_DisableInputManager.boolValue ? 1 : 2;
            int oldInputOption = inputOption;
            EditorGUI.BeginChangeCheck();
            inputOption = EditorGUILayout.Popup(Styles.activeInputHandling, inputOption, Styles.activeInputHandlingOptions);
            if (EditorGUI.EndChangeCheck())
            {
                if (inputOption != oldInputOption)
                {
                    EditorUtility.DisplayDialog("Unity editor restart required", "The Unity editor must be restarted for this change to take effect.", "OK");
                    m_EnableInputSystem.boolValue = (inputOption == 1 || inputOption == 2);
                    m_DisableInputManager.boolValue = !(inputOption == 0 || inputOption == 2);
                    m_EnableInputSystem.serializedObject.ApplyModifiedProperties();
                }
                EditorGUIUtility.ExitGUI();
            }

            EditorGUILayout.Space();
        }

        private void OtherSectionOptimizationGUI(BuildTargetGroup targetGroup)
        {
            // Optimization
            GUILayout.Label(Styles.optimizationTitle, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_BakeCollisionMeshes, Styles.bakeCollisionMeshes);
            EditorGUILayout.PropertyField(m_KeepLoadedShadersAlive, Styles.keepLoadedShadersAlive);
            EditorGUILayout.PropertyField(m_PreloadedAssets, Styles.preloadedAssets, true);

            bool platformUsesAOT =
                targetGroup == BuildTargetGroup.iOS ||
                targetGroup == BuildTargetGroup.tvOS ||
                targetGroup == BuildTargetGroup.XboxOne ||
                targetGroup == BuildTargetGroup.PS4 ||
                targetGroup == BuildTargetGroup.PSP2;

            if (platformUsesAOT)
                EditorGUILayout.PropertyField(m_AotOptions, Styles.aotOptions);

            bool platformSupportsStripping =
                targetGroup == BuildTargetGroup.iOS ||
                targetGroup == BuildTargetGroup.tvOS ||
                targetGroup == BuildTargetGroup.Android ||
                targetGroup == BuildTargetGroup.Tizen ||
                targetGroup == BuildTargetGroup.WebGL ||
                targetGroup == BuildTargetGroup.PSP2 ||
                targetGroup == BuildTargetGroup.PS4 ||
                targetGroup == BuildTargetGroup.XboxOne ||
                targetGroup == BuildTargetGroup.WSA;

            if (platformSupportsStripping)
            {
                ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(targetGroup);
                if (targetGroup == BuildTargetGroup.WebGL || backend == ScriptingImplementation.IL2CPP)
                {
                    EditorGUILayout.PropertyField(m_StripEngineCode, Styles.stripEngineCode);
                }
                else if (backend != ScriptingImplementation.WinRTDotNET)
                {
                    EditorGUILayout.PropertyField(m_IPhoneStrippingLevel, Styles.iPhoneStrippingLevel);
                }
            }

            if (targetGroup == BuildTargetGroup.iOS || targetGroup == BuildTargetGroup.tvOS)
            {
                EditorGUILayout.PropertyField(m_IPhoneScriptCallOptimization, Styles.iPhoneScriptCallOptimization);
            }
            if (targetGroup == BuildTargetGroup.Android)
            {
                EditorGUILayout.PropertyField(m_AndroidProfiler, Styles.enableInternalProfiler);
            }

            EditorGUILayout.Space();

            // Vertex compression flags dropdown
            VertexChannelCompressionFlags vertexFlags = (VertexChannelCompressionFlags)m_VertexChannelCompressionMask.intValue;
            vertexFlags = (VertexChannelCompressionFlags)EditorGUILayout.EnumFlagsField(Styles.vertexChannelCompressionMask, vertexFlags);
            m_VertexChannelCompressionMask.intValue = (int)vertexFlags;

            EditorGUILayout.PropertyField(m_StripUnusedMeshComponents, Styles.stripUnusedMeshComponents);

            if (targetGroup == BuildTargetGroup.PSP2)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_VideoMemoryForVertexBuffers, Styles.videoMemoryForVertexBuffers);
                if (EditorGUI.EndChangeCheck())
                {
                    const int minVidMemForMesh = 0;
                    const int maxVidMemForMesh = 192;
                    if (m_VideoMemoryForVertexBuffers.intValue < minVidMemForMesh)
                    {
                        m_VideoMemoryForVertexBuffers.intValue = minVidMemForMesh;
                    }
                    else if (m_VideoMemoryForVertexBuffers.intValue > maxVidMemForMesh)
                    {
                        m_VideoMemoryForVertexBuffers.intValue = maxVidMemForMesh;
                    }
                }
            }

            EditorGUILayout.Space();
        }

        static ApiCompatibilityLevel[] only_4_x_profiles = new ApiCompatibilityLevel[] { ApiCompatibilityLevel.NET_4_6, ApiCompatibilityLevel.NET_Standard_2_0 };
        static ApiCompatibilityLevel[] only_2_0_profiles = new ApiCompatibilityLevel[] { ApiCompatibilityLevel.NET_2_0, ApiCompatibilityLevel.NET_2_0_Subset };
        static ApiCompatibilityLevel[] wsa_profiles = new ApiCompatibilityLevel[] { ApiCompatibilityLevel.NET_2_0, ApiCompatibilityLevel.NET_2_0_Subset, ApiCompatibilityLevel.NET_4_6 };

        private ApiCompatibilityLevel[] GetAvailableApiCompatibilityLevels(BuildTargetGroup activeBuildTargetGroup)
        {
            if (EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest)
                return only_4_x_profiles;

            if (activeBuildTargetGroup == BuildTargetGroup.WSA)
                return wsa_profiles;

            return only_2_0_profiles;
        }

        static Il2CppCompilerConfiguration[] m_Il2cppCompilerConfigurations;
        static GUIContent[] m_Il2cppCompilerConfigurationNames;

        private Il2CppCompilerConfiguration[] GetIl2CppCompilerConfigurations()
        {
            if (m_Il2cppCompilerConfigurations == null)
            {
                m_Il2cppCompilerConfigurations = new Il2CppCompilerConfiguration[]
                {
                    Il2CppCompilerConfiguration.Debug,
                    Il2CppCompilerConfiguration.Release,
                };
            }

            return m_Il2cppCompilerConfigurations;
        }

        private GUIContent[] GetIl2CppCompilerConfigurationNames()
        {
            if (m_Il2cppCompilerConfigurationNames == null)
            {
                var configurations = GetIl2CppCompilerConfigurations();
                m_Il2cppCompilerConfigurationNames = new GUIContent[configurations.Length];

                for (int i = 0; i < configurations.Length; i++)
                    m_Il2cppCompilerConfigurationNames[i] = EditorGUIUtility.TextContent(configurations[i].ToString());
            }

            return m_Il2cppCompilerConfigurationNames;
        }

        public static bool IsLatestApiCompatibility(ApiCompatibilityLevel level)
        {
            return (level == ApiCompatibilityLevel.NET_4_6 || level == ApiCompatibilityLevel.NET_Standard_2_0);
        }

        private void OtherSectionLoggingGUI()
        {
            GUILayout.Label(Styles.loggingTitle, EditorStyles.boldLabel);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Log Type");
            foreach (StackTraceLogType stackTraceLogType in Enum.GetValues(typeof(StackTraceLogType)))
            {
                GUILayout.Label(stackTraceLogType.ToString(), GUILayout.Width(70));
            }
            GUILayout.EndHorizontal();

            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(logType.ToString());
                foreach (StackTraceLogType stackTraceLogType in Enum.GetValues(typeof(StackTraceLogType)))
                {
                    StackTraceLogType inStackTraceLogType = PlayerSettings.GetStackTraceLogType(logType);
                    EditorGUI.BeginChangeCheck();
                    bool val = EditorGUILayout.ToggleLeft(" ", inStackTraceLogType == stackTraceLogType, GUILayout.Width(70));
                    if (EditorGUI.EndChangeCheck() && val)
                    {
                        PlayerSettings.SetStackTraceLogType(logType, stackTraceLogType);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private static Dictionary<ScriptingImplementation, GUIContent> m_NiceScriptingBackendNames;
        private static Dictionary<ApiCompatibilityLevel, GUIContent> m_NiceApiCompatibilityLevelNames;

        private static GUIContent[] GetGUIContentsForValues<T>(Dictionary<T, GUIContent> contents, T[] values)
        {
            var names = new GUIContent[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                if (contents.ContainsKey(values[i]))
                    names[i] = contents[values[i]];
                else
                    throw new NotImplementedException(string.Format("Missing name for {0}", values[i]));
            }
            return names;
        }

        private static GUIContent[] GetNiceScriptingBackendNames(ScriptingImplementation[] scriptingBackends)
        {
            if (m_NiceScriptingBackendNames == null)
            {
                m_NiceScriptingBackendNames = new Dictionary<ScriptingImplementation, GUIContent>
                {
                    { ScriptingImplementation.Mono2x, Styles.scriptingMono2x },
                    { ScriptingImplementation.WinRTDotNET, Styles.scriptingWinRTDotNET },
                    { ScriptingImplementation.IL2CPP, Styles.scriptingIL2CPP }
                };
            }

            return GetGUIContentsForValues(m_NiceScriptingBackendNames, scriptingBackends);
        }

        private static GUIContent[] GetNiceApiCompatibilityLevelNames(ApiCompatibilityLevel[] apiCompatibilityLevels)
        {
            if (m_NiceApiCompatibilityLevelNames == null)
            {
                m_NiceApiCompatibilityLevelNames = new Dictionary<ApiCompatibilityLevel, GUIContent>
                {
                    { ApiCompatibilityLevel.NET_2_0, Styles.apiCompatibilityLevel_NET_2_0 },
                    { ApiCompatibilityLevel.NET_2_0_Subset, Styles.apiCompatibilityLevel_NET_2_0_Subset },
                    { ApiCompatibilityLevel.NET_4_6, Styles.apiCompatibilityLevel_NET_4_6 },
                    { ApiCompatibilityLevel.NET_Standard_2_0, Styles.apiCompatibilityLevel_NET_Standard_2_0 }
                };
            }

            return GetGUIContentsForValues(m_NiceApiCompatibilityLevelNames, apiCompatibilityLevels);
        }

        private void AutoAssignProperty(SerializedProperty property, string packageDir, string fileName)
        {
            if (property.stringValue.Length == 0 || !File.Exists(Path.Combine(packageDir, property.stringValue)))
            {
                string filePath = Path.Combine(packageDir, fileName);
                if (File.Exists(filePath))
                    property.stringValue = fileName;
            }
        }

        public void BrowseablePathProperty(string propertyLabel, SerializedProperty property, string browsePanelTitle, string extension, string dir)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(EditorGUIUtility.TextContent(propertyLabel));

            GUIContent browseBtnLabel = EditorGUIUtility.TrTextContent("...");
            Vector2 sizeOfLabel = GUI.skin.GetStyle("Button").CalcSize(browseBtnLabel);

            if (GUILayout.Button(browseBtnLabel, EditorStyles.miniButton, GUILayout.MaxWidth(sizeOfLabel.x)))
            {
                GUI.FocusControl("");

                string title = EditorGUIUtility.TempContent(browsePanelTitle).text;
                string currDirectory = string.IsNullOrEmpty(dir) ? Directory.GetCurrentDirectory().Replace('\\', '/') + "/" : dir.Replace('\\', '/') + "/";
                string newStringValue = "";

                if (string.IsNullOrEmpty(extension))
                    newStringValue = EditorUtility.OpenFolderPanel(title, currDirectory, "");
                else
                    newStringValue = EditorUtility.OpenFilePanel(title, currDirectory, extension);

                if (newStringValue.StartsWith(currDirectory))
                    newStringValue = newStringValue.Substring(currDirectory.Length);

                if (!string.IsNullOrEmpty(newStringValue))
                {
                    property.stringValue = newStringValue;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            GUIContent gc = null;
            bool emptyString = string.IsNullOrEmpty(property.stringValue);
            using (new EditorGUI.DisabledScope(emptyString))
            {
                if (emptyString)
                {
                    gc = EditorGUIUtility.TrTextContent("Not selected.");
                }
                else
                {
                    gc = EditorGUIUtility.TempContent(property.stringValue);
                }

                EditorGUI.BeginChangeCheck();
                GUILayoutOption[] options = { GUILayout.Width(32), GUILayout.ExpandWidth(true) };
                string modifiedString = EditorGUILayout.TextArea(gc.text, options);
                if (EditorGUI.EndChangeCheck())
                {
                    if (string.IsNullOrEmpty(modifiedString))
                    {
                        property.stringValue = "";
                        serializedObject.ApplyModifiedProperties();
                        GUI.FocusControl("");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        internal static bool BuildPathBoxButton(SerializedProperty prop, string uiString, string directory)
        {
            return BuildPathBoxButton(prop, uiString, directory, null);
        }

        internal static bool BuildPathBoxButton(SerializedProperty prop, string uiString, string directory, Action onSelect)
        {
            float h = EditorGUI.kSingleLineHeight;
            float kLabelFloatMinW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
            float kLabelFloatMaxW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
            Rect r = GUILayoutUtility.GetRect(kLabelFloatMinW, kLabelFloatMaxW, h, h, EditorStyles.layerMaskField, null);

            float labelWidth = EditorGUIUtility.labelWidth;
            Rect buttonRect = new Rect(r.x + EditorGUI.indent, r.y, labelWidth - EditorGUI.indent, r.height);
            Rect fieldRect = new Rect(r.x + labelWidth, r.y, r.width - labelWidth, r.height);

            string display = (prop.stringValue.Length == 0) ? "Not selected." : prop.stringValue;
            EditorGUI.TextArea(fieldRect, display, EditorStyles.label);

            bool changed = false;
            if (GUI.Button(buttonRect, EditorGUIUtility.TextContent(uiString)))
            {
                string prevVal = prop.stringValue;
                string path = EditorUtility.OpenFolderPanel(EditorGUIUtility.TextContent(uiString).text, directory, "");

                string relPath = FileUtil.GetProjectRelativePath(path);
                prop.stringValue = (relPath != string.Empty) ? relPath : path;
                changed = (prop.stringValue != prevVal);

                if (onSelect != null)
                    onSelect();

                prop.serializedObject.ApplyModifiedProperties();
            }

            return changed;
        }

        internal static bool BuildFileBoxButton(SerializedProperty prop, string uiString, string directory, string ext)
        {
            return BuildFileBoxButton(prop, uiString, directory, ext, null);
        }

        internal static bool BuildFileBoxButton(SerializedProperty prop, string uiString, string directory,
            string ext, Action onSelect)
        {
            float h = EditorGUI.kSingleLineHeight;
            float kLabelFloatMinW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
            float kLabelFloatMaxW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
            Rect r = GUILayoutUtility.GetRect(kLabelFloatMinW, kLabelFloatMaxW, h, h, EditorStyles.layerMaskField, null);

            float labelWidth = EditorGUIUtility.labelWidth;
            Rect buttonRect  = new Rect(r.x + EditorGUI.indent, r.y, labelWidth - EditorGUI.indent, r.height);
            Rect fieldRect   = new Rect(r.x + labelWidth, r.y, r.width - labelWidth, r.height);

            string display = (prop.stringValue.Length == 0) ? "Not selected." : prop.stringValue;
            EditorGUI.TextArea(fieldRect, display, EditorStyles.label);

            bool changed = false;
            if (GUI.Button(buttonRect, EditorGUIUtility.TextContent(uiString)))
            {
                string prevVal = prop.stringValue;
                string path = EditorUtility.OpenFilePanel(EditorGUIUtility.TextContent(uiString).text, directory, ext);

                string relPath = FileUtil.GetProjectRelativePath(path);
                prop.stringValue = (relPath != string.Empty) ? relPath : path;
                changed = (prop.stringValue != prevVal);

                if (onSelect != null)
                    onSelect();

                prop.serializedObject.ApplyModifiedProperties();
            }

            return changed;
        }

        public void PublishSectionGUI(BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension, int sectionIndex = 5)
        {
            if (targetGroup != BuildTargetGroup.WSA &&
                targetGroup != BuildTargetGroup.PSP2 &&
                !(settingsExtension != null && settingsExtension.HasPublishSection()))
                return;

            if (BeginSettingsBox(sectionIndex, Styles.publishingSettingsTitle))
            {
                float h = EditorGUI.kSingleLineHeight;
                float kLabelFloatMinW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
                float kLabelFloatMaxW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;

                if (settingsExtension != null)
                {
                    settingsExtension.PublishSectionGUI(h, kLabelFloatMinW, kLabelFloatMaxW);
                }
            }
            EndSettingsBox();
        }

        private static void ShowWarning(GUIContent warningMessage)
        {
            if (s_WarningIcon == null)
                s_WarningIcon = EditorGUIUtility.LoadIcon("console.warnicon");

            //          var c = new GUIContent(error) { image = s_WarningIcon };
            warningMessage.image = s_WarningIcon;

            GUILayout.Space(5);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(warningMessage, EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();
        }

        protected override bool ShouldHideOpenButton()
        {
            return true;
        }
    }
}
