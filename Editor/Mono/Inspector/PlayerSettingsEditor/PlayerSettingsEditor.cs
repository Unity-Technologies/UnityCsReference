// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.PlatformSupport;
using UnityEditor.Presets;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEditor.Modules;
using UnityEditorInternal.VR;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;

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
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal partial class PlayerSettingsEditor : Editor
    {
        class Styles
        {
            public static readonly GUIStyle categoryBox = new GUIStyle(EditorStyles.helpBox);
            static Styles()
            {
                categoryBox.padding.left = 4;
            }
        }

        class SettingsContent
        {
            public static readonly GUIContent recordingInfo = EditorGUIUtility.TrTextContent("Reordering the list will switch editor to the first available platform");
            public static readonly GUIContent appleSiliconOpenGLWarning = EditorGUIUtility.TrTextContent("OpenGL is not supported on Apple Silicon chips. Metal will be used on devices with Apple Silicon chips instead.");
            public static readonly GUIContent sharedBetweenPlatformsInfo = EditorGUIUtility.TrTextContent("* Shared setting between multiple platforms.");

            public static readonly GUIContent cursorHotspot = EditorGUIUtility.TrTextContent("Cursor Hotspot");
            public static readonly GUIContent defaultCursor = EditorGUIUtility.TrTextContent("Default Cursor");
            public static readonly GUIContent vertexChannelCompressionMask = EditorGUIUtility.TrTextContent("Vertex Compression*", "Select which vertex channels should be compressed. Compression can save memory and bandwidth, but precision will be lower.");

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
            public static readonly GUIContent vulkanSettingsTitle = EditorGUIUtility.TrTextContent("Vulkan Settings");
            public static readonly GUIContent identificationTitle = EditorGUIUtility.TrTextContent("Identification");
            public static readonly GUIContent configurationTitle = EditorGUIUtility.TrTextContent("Configuration");
            public static readonly GUIContent optimizationTitle = EditorGUIUtility.TrTextContent("Optimization");
            public static readonly GUIContent loggingTitle = EditorGUIUtility.TrTextContent("Stack Trace*");
            public static readonly GUIContent legacyTitle = EditorGUIUtility.TrTextContent("Legacy");
            public static readonly GUIContent publishingSettingsTitle = EditorGUIUtility.TrTextContent("Publishing Settings");
            public static readonly GUIContent captureLogsTitle = EditorGUIUtility.TrTextContent("Capture Logs");

            public static readonly GUIContent usePlayerLog = EditorGUIUtility.TrTextContent("Use Player Log");
            public static readonly GUIContent resizableWindow = EditorGUIUtility.TrTextContent("Resizable Window");
            public static readonly GUIContent forceSingleInstance = EditorGUIUtility.TrTextContent("Force Single Instance");

            public static readonly GUIContent shaderSectionTitle = EditorGUIUtility.TrTextContent("Shader Settings");
            public static readonly GUIContent shaderVariantLoadingTitle = EditorGUIUtility.TrTextContent("Shader Variant Loading Settings");
            public static readonly GUIContent defaultShaderChunkSize = EditorGUIUtility.TrTextContent("Default chunk size (MB)*", "Use this setting to control how much memory is used when loading shader variants.");
            public static readonly GUIContent defaultShaderChunkCount = EditorGUIUtility.TrTextContent("Default chunk count*", "Use this setting to control how much memory is used when loading shader variants.");
            public static readonly GUIContent overrideDefaultChunkSettings = EditorGUIUtility.TrTextContent("Override", "Override the default settings for this build target.");
            public static readonly GUIContent platformShaderChunkSize = EditorGUIUtility.TrTextContent("Chunk size (MB)", "Use this setting to control how much memory is used when loading shader variants.");
            public static readonly GUIContent platformShaderChunkCount = EditorGUIUtility.TrTextContent("Chunk count", "Use this setting to control how much memory is used when loading shader variants.");

            public static readonly GUIContent bakeCollisionMeshes = EditorGUIUtility.TrTextContent("Prebake Collision Meshes*", "Bake collision data into the meshes on build time");
            public static readonly GUIContent dedicatedServerOptimizations = EditorGUIUtility.TrTextContent("Enable Dedicated Server optimizations", "Performs additional optimizations on Dedicated Server builds.");
            public static readonly GUIContent keepLoadedShadersAlive = EditorGUIUtility.TrTextContent("Keep Loaded Shaders Alive*", "Prevents shaders from being unloaded");
            public static readonly GUIContent preloadedAssets = EditorGUIUtility.TrTextContent("Preloaded Assets*", "Assets to load at start up in the player and kept alive until the player terminates");
            public static readonly GUIContent stripEngineCode = EditorGUIUtility.TrTextContent("Strip Engine Code*", "Strip Unused Engine Code - Note that byte code stripping of managed assemblies is always enabled for the IL2CPP scripting backend.");
            public static readonly GUIContent iPhoneScriptCallOptimization = EditorGUIUtility.TrTextContent("Script Call Optimization*");
            public static readonly GUIContent enableInternalProfiler = EditorGUIUtility.TrTextContent("Enable Internal Profiler* (Deprecated)", "Internal profiler counters should be accessed by scripts using UnityEngine.Profiling::Profiler API.");
            public static readonly GUIContent stripUnusedMeshComponents = EditorGUIUtility.TrTextContent("Optimize Mesh Data*", "Remove unused mesh components");
            public static readonly GUIContent strictShaderVariantMatching = EditorGUIUtility.TrTextContent("Strict shader variant matching*", "When enabled, if a shader variant is missing, Unity uses the error shader and displays an error in the Console.");
            public static readonly GUIContent mipStripping = EditorGUIUtility.TrTextContent("Texture Mipmap Stripping*", "Remove unused texture mipmap levels from package builds, reducing package size on disk. Limits the texture quality settings to the highest mipmap level that was included during the build.");
            public static readonly GUIContent enableFrameTimingStats = EditorGUIUtility.TrTextContent("Frame Timing Stats", "Enable gathering of CPU/GPU frame timing statistics.");
            public static readonly GUIContent enableOpenGLProfilerGPURecorders = EditorGUIUtility.TrTextContent("OpenGL: Profiler GPU Recorders", "Enable Profiler Recorders when rendering with OpenGL. Always enabled with other rendering APIs. Optional on OpenGL due to potential incompatibility with Frame Timing Stats and the GPU Profiler.");
            public static readonly GUIContent openGLFrameTimingStatsOnGPURecordersOnWarning = EditorGUIUtility.TrTextContent("On OpenGL, Frame Timing Stats may disable Profiler GPU Recorders and the GPU Profiler.");
            public static readonly GUIContent openGLFrameTimingStatsOnGPURecordersOffInfo = EditorGUIUtility.TrTextContent("On OpenGL, Frame Timing Stats may disable the GPU Profiler.");
            public static readonly GUIContent openGLFrameTimingStatsOffGPURecordersOnInfo = EditorGUIUtility.TrTextContent("On OpenGL, Profiler GPU Recorders may disable the GPU Profiler.");
            public static readonly GUIContent useOSAutoRotation = EditorGUIUtility.TrTextContent("Use Animated Autorotation (Deprecated)", "If set OS native animated autorotation method will be used. Otherwise orientation will be changed immediately. This is has no effect on iOS 16 later versions as autorotation is always animated. This option is deprecated and will be removed in a future release.");
            public static readonly GUIContent defaultScreenWidth = EditorGUIUtility.TrTextContent("Default Screen Width");
            public static readonly GUIContent defaultScreenHeight = EditorGUIUtility.TrTextContent("Default Screen Height");
            public static readonly GUIContent macRetinaSupport = EditorGUIUtility.TrTextContent("Mac Retina Support");
            public static readonly GUIContent runInBackground = EditorGUIUtility.TrTextContent("Run In Background*");
            public static readonly GUIContent defaultIsNativeResolution = EditorGUIUtility.TrTextContent("Default Is Native Resolution");
            public static readonly GUIContent defaultScreenOrientation = EditorGUIUtility.TrTextContent("Default Orientation*");
            public static readonly GUIContent allowedAutoRotateToPortrait = EditorGUIUtility.TrTextContent("Portrait");
            public static readonly GUIContent allowedAutoRotateToPortraitUpsideDown = EditorGUIUtility.TrTextContent("Portrait Upside Down");
            public static readonly GUIContent allowedAutoRotateToLandscapeRight = EditorGUIUtility.TrTextContent("Landscape Right");
            public static readonly GUIContent allowedAutoRotateToLandscapeLeft = EditorGUIUtility.TrTextContent("Landscape Left");
            public static readonly GUIContent UIRequiresFullScreen = EditorGUIUtility.TrTextContent("Requires Fullscreen");
            public static readonly GUIContent UIStatusBarHidden = EditorGUIUtility.TrTextContent("Status Bar Hidden");
            public static readonly GUIContent UIStatusBarStyle = EditorGUIUtility.TrTextContent("Status Bar Style");
            public static readonly GUIContent fullscreenMode = EditorGUIUtility.TrTextContent("Fullscreen Mode ", " Not all platforms support all modes");
            public static readonly GUIContent exclusiveFullscreen = EditorGUIUtility.TrTextContent("Exclusive Fullscreen");
            public static readonly GUIContent fullscreenWindow = EditorGUIUtility.TrTextContent("Fullscreen Window");
            public static readonly GUIContent maximizedWindow = EditorGUIUtility.TrTextContent("Maximized Window");
            public static readonly GUIContent windowed = EditorGUIUtility.TrTextContent("Windowed");
            public static readonly GUIContent displayResolutionDialogEnabledLabel = EditorGUIUtility.TrTextContent("Enabled (Deprecated)");
            public static readonly GUIContent displayResolutionDialogHiddenLabel = EditorGUIUtility.TrTextContent("Hidden by Default (Deprecated)");
            public static readonly GUIContent displayResolutionDialogDeprecationWarning = EditorGUIUtility.TrTextContent("The Display Resolution Dialog has been deprecated and will be removed in a future version.");
            public static readonly GUIContent visibleInBackground = EditorGUIUtility.TrTextContent("Visible In Background");
            public static readonly GUIContent allowFullscreenSwitch = EditorGUIUtility.TrTextContent("Allow Fullscreen Switch*");
            public static readonly GUIContent useFlipModelSwapChain = EditorGUIUtility.TrTextContent("Use DXGI flip model swapchain for D3D11", "Disable this option to fallback to Windows 7-style BitBlt model. Using flip model (leaving this option enabled) ensures the best performance. This setting affects only D3D11 graphics API.");
            public static readonly GUIContent flipModelSwapChainWarning = EditorGUIUtility.TrTextContent("Disabling DXGI flip model swapchain will result in Unity falling back to the slower and less efficient BitBlt model. See documentation for more information.");
            public static readonly GUIContent use32BitDisplayBuffer = EditorGUIUtility.TrTextContent("Use 32-bit Display Buffer*", "If set Display Buffer will be created to hold 32-bit color values. Use it only if you see banding, as it has performance implications.");
            public static readonly GUIContent disableDepthAndStencilBuffers = EditorGUIUtility.TrTextContent("Disable Depth and Stencil*");
            public static readonly GUIContent preserveFramebufferAlpha = EditorGUIUtility.TrTextContent("Render Over Native UI*", "Enable this option ONLY if you want Unity to render on top of the native Android or iOS UI.");
            public static readonly GUIContent actionOnDotNetUnhandledException = EditorGUIUtility.TrTextContent("On .Net UnhandledException*");
            public static readonly GUIContent logObjCUncaughtExceptions = EditorGUIUtility.TrTextContent("Log Obj-C Uncaught Exceptions*");
            public static readonly GUIContent enableCrashReportAPI = EditorGUIUtility.TrTextContent("Enable CrashReport API*");
            public static readonly GUIContent activeColorSpace = EditorGUIUtility.TrTextContent("Color Space*");
            public static readonly GUIContent unsupportedMSAAFallback = EditorGUIUtility.TrTextContent("MSAA Fallback");
            public static readonly GUIContent colorGamut = EditorGUIUtility.TrTextContent("Color Gamut*");
            public static readonly GUIContent colorGamutForMac = EditorGUIUtility.TrTextContent("Color Gamut For Mac*");
            public static readonly GUIContent metalForceHardShadows = EditorGUIUtility.TrTextContent("Force hard shadows on Metal*");
            public static readonly GUIContent metalAPIValidation = EditorGUIUtility.TrTextContent("Metal API Validation*", "When enabled, additional binding state validation is applied.");
            public static readonly GUIContent metalFramebufferOnly = EditorGUIUtility.TrTextContent("Metal Write-Only Backbuffer*", "Set framebufferOnly flag on backbuffer. This prevents readback from backbuffer but enables some driver optimizations.");
            public static readonly GUIContent framebufferDepthMemorylessMode = EditorGUIUtility.TrTextContent("Memoryless Depth*", "Memoryless mode of framebuffer depth");
            public static readonly GUIContent[] memorylessModeNames = { EditorGUIUtility.TrTextContent("Unused"), EditorGUIUtility.TrTextContent("Forced"), EditorGUIUtility.TrTextContent("Automatic") };
            public static readonly GUIContent vulkanEnableSetSRGBWrite = EditorGUIUtility.TrTextContent("SRGB Write Mode*", "If set, enables Graphics.SetSRGBWrite() for toggling sRGB write mode during the frame but may decrease performance especially on tiled GPUs.");
            public static readonly GUIContent vulkanNumSwapchainBuffers = EditorGUIUtility.TrTextContent("Number of swapchain buffers*");
            public static readonly GUIContent vulkanEnableLateAcquireNextImage = EditorGUIUtility.TrTextContent("Acquire swapchain image late as possible*", "If set, renders to a staging image to delay acquiring the swapchain buffer.");
            public static readonly GUIContent vulkanEnableCommandBufferRecycling = EditorGUIUtility.TrTextContent("Recycle command buffers*", "When enabled, command buffers are recycled after they have been executed as opposed to being freed.");
            public static readonly GUIContent mTRendering = EditorGUIUtility.TrTextContent("Multithreaded Rendering*");
            public static readonly GUIContent staticBatching = EditorGUIUtility.TrTextContent("Static Batching");
            public static readonly GUIContent dynamicBatching = EditorGUIUtility.TrTextContent("Dynamic Batching", "Toggle Dynamic Batching. Note: Sprites are always dynamically batched.");
            public static readonly GUIContent spriteBatchingVertexThreshold = EditorGUIUtility.TrTextContent("Sprite Batching Threshold", "Maximum vertex threshold of a sprite to be batched. Any sprite with vertex count above this value is not batched.");
            public static readonly GUIContent spriteBatchingMaxVertexCount = EditorGUIUtility.TrTextContent("Sprite Batching Max Vertex Count", "Maximum vertex count per batch.");
            public static readonly GUIContent graphicsJobsNonExperimental = EditorGUIUtility.TrTextContent("Graphics Jobs");
            public static readonly GUIContent graphicsJobsExperimental = EditorGUIUtility.TrTextContent("Graphics Jobs (Experimental)");
            public static readonly GUIContent graphicsJobsMode = EditorGUIUtility.TrTextContent("Graphics Jobs Mode");
            public static readonly GUIContent applicationIdentifierWarning = EditorGUIUtility.TrTextContent("Invalid characters have been removed from the Application Identifier.");
            public static readonly GUIContent applicationIdentifierError = EditorGUIUtility.TrTextContent("The Application Identifier must follow the convention 'com.YourCompanyName.YourProductName' and must contain only alphanumeric and hyphen characters.");
            public static readonly GUIContent packageNameError = EditorGUIUtility.TrTextContent("The Package Name must follow the convention 'com.YourCompanyName.YourProductName' and must contain only alphanumeric and underscore characters. Each segment must start with an alphabetical character.");
            public static readonly GUIContent applicationBuildNumber = EditorGUIUtility.TrTextContent("Build");
            public static readonly GUIContent appleDeveloperTeamID = EditorGUIUtility.TrTextContent("iOS Developer Team ID", "Developers can retrieve their Team ID by visiting the Apple Developer site under Account > Membership.");
            public static readonly GUIContent gcIncremental = EditorGUIUtility.TrTextContent("Use incremental GC*", "With incremental Garbage Collection, the Garbage Collector will try to time-slice the collection task into multiple steps, to avoid long GC times preventing content from running smoothly.");
            public static readonly GUIContent accelerometerFrequency = EditorGUIUtility.TrTextContent("Accelerometer Frequency*");
            public static readonly GUIContent cameraUsageDescription = EditorGUIUtility.TrTextContent("Camera Usage Description*", "String shown to the user when requesting permission to use the device camera. Written to the NSCameraUsageDescription field in Xcode project's info.plist file");
            public static readonly GUIContent locationUsageDescription = EditorGUIUtility.TrTextContent("Location Usage Description*", "String shown to the user when requesting permission to access the device location. Written to the NSLocationWhenInUseUsageDescription field in Xcode project's info.plist file.");
            public static readonly GUIContent microphoneUsageDescription = EditorGUIUtility.TrTextContent("Microphone Usage Description*", "String shown to the user when requesting to use the device microphone. Written to the NSMicrophoneUsageDescription field in Xcode project's info.plist file");
            public static readonly GUIContent muteOtherAudioSources = EditorGUIUtility.TrTextContent("Mute Other Audio Sources*");
            public static readonly GUIContent prepareIOSForRecording = EditorGUIUtility.TrTextContent("Prepare iOS for Recording");
            public static readonly GUIContent forceIOSSpeakersWhenRecording = EditorGUIUtility.TrTextContent("Force iOS Speakers when Recording");
            public static readonly GUIContent UIRequiresPersistentWiFi = EditorGUIUtility.TrTextContent("Requires Persistent WiFi*");
            public static readonly GUIContent insecureHttpOption = EditorGUIUtility.TrTextContent("Allow downloads over HTTP*", "");
            public static readonly GUIContent insecureHttpWarning = EditorGUIUtility.TrTextContent("Plain text HTTP connections are not secure and can make your application vulnerable to attacks.");
            public static readonly GUIContent[] insecureHttpOptions =
            {
                EditorGUIUtility.TrTextContent("Not allowed"),
                EditorGUIUtility.TrTextContent("Allowed in development builds"),
                EditorGUIUtility.TrTextContent("Always allowed"),
            };

            public static readonly GUIContent autoGraphicsAPI = EditorGUIUtility.TrTextContent("Auto Graphics API");
            public static readonly GUIContent autoGraphicsAPIForWindows = EditorGUIUtility.TrTextContent("Auto Graphics API for Windows");
            public static readonly GUIContent autoGraphicsAPIForMac = EditorGUIUtility.TrTextContent("Auto Graphics API for Mac");
            public static readonly GUIContent autoGraphicsAPIForLinux = EditorGUIUtility.TrTextContent("Auto Graphics API for Linux");

            public static readonly GUIContent iOSURLSchemes = EditorGUIUtility.TrTextContent("Supported URL schemes*");
            public static readonly GUIContent iOSExternalAudioInputNotSupported = EditorGUIUtility.TrTextContent("Audio input from Bluetooth microphones is not supported when Mute Other Audio Sources is off.");
            public static readonly GUIContent require31 = EditorGUIUtility.TrTextContent("Require ES3.1");
            public static readonly GUIContent requireAEP = EditorGUIUtility.TrTextContent("Require ES3.1+AEP");
            public static readonly GUIContent require32 = EditorGUIUtility.TrTextContent("Require ES3.2");
            public static readonly GUIContent skinOnGPU = EditorGUIUtility.TrTextContent("GPU Skinning*", "Calculate mesh skinning and blend shapes on the GPU via shaders");
            public static readonly GUIContent[] meshDeformations = { EditorGUIUtility.TrTextContent("CPU"), EditorGUIUtility.TrTextContent("GPU"), EditorGUIUtility.TrTextContent("GPU (Batched)") };
            public static readonly GUIContent scriptingDefineSymbols = EditorGUIUtility.TrTextContent("Scripting Define Symbols", "Preprocessor defines passed to the C# script compiler.");
            public static readonly GUIContent additionalCompilerArguments = EditorGUIUtility.TrTextContent("Additional Compiler Arguments", "Additional arguments passed to the C# script compiler.");
            public static readonly GUIContent scriptingDefineSymbolsApply = EditorGUIUtility.TrTextContent("Apply");
            public static readonly GUIContent scriptingDefineSymbolsApplyRevert = EditorGUIUtility.TrTextContent("Revert");
            public static readonly GUIContent scriptingDefineSymbolsCopyDefines = EditorGUIUtility.TrTextContent("Copy Defines", "Copy applied defines");
            public static readonly GUIContent suppressCommonWarnings = EditorGUIUtility.TrTextContent("Suppress Common Warnings", "Suppresses C# warnings CS0169, CS0649, and CS0282.");
            public static readonly GUIContent scriptingBackend = EditorGUIUtility.TrTextContent("Scripting Backend");
            public static readonly GUIContent managedStrippingLevel = EditorGUIUtility.TrTextContent("Managed Stripping Level", "If scripting backend is IL2CPP, managed stripping can't be disabled.");
            public static readonly GUIContent il2cppCompilerConfiguration = EditorGUIUtility.TrTextContent("C++ Compiler Configuration");
            public static readonly GUIContent il2cppCodeGeneration = EditorGUIUtility.TrTextContent("IL2CPP Code Generation", "Determines whether IL2CPP should generate code optimized for runtime performance or build size/iteration.");
            public static readonly GUIContent[] il2cppCodeGenerationNames =  new GUIContent[] { EditorGUIUtility.TrTextContent("Faster runtime"), EditorGUIUtility.TrTextContent("Faster (smaller) builds") };
            public static readonly GUIContent il2cppStacktraceInformation = EditorGUIUtility.TrTextContent("IL2CPP Stacktrace Information", "Which information to include in stack traces. Including the file name and line number may increase build size.");
            public static readonly GUIContent scriptingMono2x = EditorGUIUtility.TrTextContent("Mono");
            public static readonly GUIContent scriptingIL2CPP = EditorGUIUtility.TrTextContent("IL2CPP");
            public static readonly GUIContent scriptingCoreCLR = EditorGUIUtility.TrTextContent("CoreCLR");
            public static readonly GUIContent scriptingDefault = EditorGUIUtility.TrTextContent("Default");
            public static readonly GUIContent strippingDisabled = EditorGUIUtility.TrTextContent("Disabled");
            public static readonly GUIContent strippingMinimal = EditorGUIUtility.TrTextContent("Minimal");
            public static readonly GUIContent strippingLow = EditorGUIUtility.TrTextContent("Low");
            public static readonly GUIContent strippingMedium = EditorGUIUtility.TrTextContent("Medium");
            public static readonly GUIContent strippingHigh = EditorGUIUtility.TrTextContent("High");
            public static readonly GUIContent apiCompatibilityLevel = EditorGUIUtility.TrTextContent("Api Compatibility Level*");
            public static readonly GUIContent apiCompatibilityLevel_NET_2_0 = EditorGUIUtility.TrTextContent(".NET 2.0");
            public static readonly GUIContent apiCompatibilityLevel_NET_2_0_Subset = EditorGUIUtility.TrTextContent(".NET 2.0 Subset");
            public static readonly GUIContent apiCompatibilityLevel_NET_4_6 = EditorGUIUtility.TrTextContent(".NET 4.x");
            public static readonly GUIContent apiCompatibilityLevel_NET_Standard_2_0 = EditorGUIUtility.TrTextContent(".NET Standard 2.0");
            public static readonly GUIContent apiCompatibilityLevel_NET_FW_Unity = EditorGUIUtility.TrTextContent(".NET Framework");
            public static readonly GUIContent apiCompatibilityLevel_NET_Standard = EditorGUIUtility.TrTextContent(".NET Standard 2.1");
            public static readonly GUIContent editorAssembliesCompatibilityLevel = EditorGUIUtility.TrTextContent("Editor Assemblies Compatibility Level*");
            public static readonly GUIContent editorAssembliesCompatibilityLevel_Default = EditorGUIUtility.TrTextContent("Default (.NET Framework)");
            public static readonly GUIContent editorAssembliesCompatibilityLevel_NET_Framework = EditorGUIUtility.TrTextContent(".NET Framework");
            public static readonly GUIContent editorAssembliesCompatibilityLevel_NET_Standard = EditorGUIUtility.TrTextContent(".NET Standard");
            public static readonly GUIContent scriptCompilationTitle = EditorGUIUtility.TrTextContent("Script Compilation");
            public static readonly GUIContent allowUnsafeCode = EditorGUIUtility.TrTextContent("Allow 'unsafe' Code", "Allow compilation of unsafe code for predefined assemblies (Assembly-CSharp.dll, etc.)");
            public static readonly GUIContent useDeterministicCompilation = EditorGUIUtility.TrTextContent("Use Deterministic Compilation", "Compile with -deterministic compilation flag");
            public static readonly GUIContent activeInputHandling = EditorGUIUtility.TrTextContent("Active Input Handling*");
            public static readonly GUIContent[] activeInputHandlingOptions = new GUIContent[] { EditorGUIUtility.TrTextContent("Input Manager (Old)"), EditorGUIUtility.TrTextContent("Input System Package (New)"), EditorGUIUtility.TrTextContent("Both") };
            public static readonly GUIContent activeInputHandlingDeprecationError = EditorGUIUtility.TrTextContent("The Input Manager is a legacy feature and not recommended for new projects. For new projects you should use the Input System Package.");
            public static readonly GUIContent activeInputHandlingError = EditorGUIUtility.TrTextContent("The Active Input Handling is invalid. To use Input System Package (New) or Both, install the Input System package. Otherwise set the Active Input Handling to Input Manager (Old).");
            public static readonly GUIContent normalMapEncodingLabel = EditorGUIUtility.TrTextContent("Normal Map Encoding");
            public static readonly GUIContent[] normalMapEncodingNames = { EditorGUIUtility.TrTextContent("XYZ"), EditorGUIUtility.TrTextContent("DXT5nm-style") };
            public static readonly GUIContent lightmapEncodingLabel = EditorGUIUtility.TrTextContent("Lightmap Encoding", "Affects the encoding scheme and compression format of the lightmaps.");
            public static readonly GUIContent[] lightmapEncodingNames = { EditorGUIUtility.TrTextContent("Low Quality"), EditorGUIUtility.TrTextContent("Normal Quality"), EditorGUIUtility.TrTextContent("High Quality") };
            public static readonly GUIContent hdrCubemapEncodingLabel = EditorGUIUtility.TrTextContent("HDR Cubemap Encoding", "Determines which encoding scheme Unity uses to encode HDR cubemaps.");
            public static readonly GUIContent[] hdrCubemapEncodingNames = { EditorGUIUtility.TrTextContent("Low Quality"), EditorGUIUtility.TrTextContent("Normal Quality"), EditorGUIUtility.TrTextContent("High Quality") };
            public static readonly GUIContent lightmapStreamingEnabled = EditorGUIUtility.TrTextContent("Lightmap Streaming", "Only load larger lightmap mipmap levels as needed to render the current game cameras. Requires texture mipmap streaming to be enabled in quality settings. This value is applied to the light map textures as they are generated.");
            public static readonly GUIContent lightmapStreamingPriority = EditorGUIUtility.TrTextContent("Streaming Priority", "Lightmap mipmap streaming priority when there's contention for resources. Positive numbers represent higher priority. Valid range is -128 to 127. This value is applied to the light map textures as they are generated.");
            public static readonly GUIContent legacyClampBlendShapeWeights = EditorGUIUtility.TrTextContent("Clamp BlendShapes (Deprecated)*", "If set, the range of BlendShape weights in SkinnedMeshRenderers will be clamped.");
            public static readonly GUIContent virtualTexturingSupportEnabled = EditorGUIUtility.TrTextContent("Virtual Texturing (Experimental)*", "Enable Virtual Texturing. This feature is experimental and not ready for production use. Changing this value requires an Editor restart.");
            public static readonly GUIContent virtualTexturingUnsupportedPlatformWarning = EditorGUIUtility.TrTextContent("The current target platform does not support Virtual Texturing. To build for this platform, uncheck Enable Virtual Texturing.");
            public static readonly GUIContent shaderPrecisionModel = EditorGUIUtility.TrTextContent("Shader Precision Model*", "Controls the default sampler precision and the definition of HLSL half.");
            public static readonly GUIContent[] shaderPrecisionModelOptions = { EditorGUIUtility.TrTextContent("Platform Default"), EditorGUIUtility.TrTextContent("Unified") };
            public static readonly GUIContent stereo360CaptureCheckbox = EditorGUIUtility.TrTextContent("360 Stereo Capture*");
            public static readonly GUIContent forceSRGBBlit = EditorGUIUtility.TrTextContent("Force SRGB blit", "Force SRGB blit for Linear color space.");
            public static readonly GUIContent notApplicableInfo = EditorGUIUtility.TrTextContent("Not applicable for this platform.");
            public static readonly GUIContent loadStoreDebugModeCheckbox = EditorGUIUtility.TrTextContent("Load/Store Action Debug Mode", "Initializes Framebuffer such that errors in the load/store actions will be visually apparent. (Removed in Release Builds)");
            public static readonly GUIContent loadStoreDebugModeEditorOnlyCheckbox = EditorGUIUtility.TrTextContent("Editor Only", "Load/Store Action Debug Mode will only affect the Editor");
            public static readonly GUIContent allowHDRDisplay = EditorGUIUtility.TrTextContent("Allow HDR Display Output*", "Enable the use of HDR displays and include all the resources required for them to function correctly.");
            public static readonly GUIContent useHDRDisplay = EditorGUIUtility.TrTextContent("Use HDR Display Output*", "Checks if the main display supports HDR and if it does, switches to HDR output at the start of the application.");
            public static readonly GUIContent hdrOutputRequireHDRRenderingWarning = EditorGUIUtility.TrTextContent("The active Render Pipeline does not have HDR enabled. Enable HDR in the Render Pipeline Asset to see the changes.");
            public static readonly GUIContent graphicsAPIDeprecationMessage = EditorGUIUtility.TrTextContent("There are select Graphics API included that are deprecated and will be removed in a future version. For more information, refer to the Graphics API documentation.");

            public static readonly GUIContent captureStartupLogs = EditorGUIUtility.TrTextContent("Capture Startup Logs", "Capture startup logs for later processing (e.g., by com.unity.logging");
            public static readonly string undoChangedBatchingString                 = L10n.Tr("Changed Batching Settings");
            public static readonly string undoChangedGraphicsAPIString              = L10n.Tr("Changed Graphics API Settings");
            public static readonly string undoChangedScriptingDefineString          = L10n.Tr("Changed Scripting Define Settings");
            public static readonly string undoChangedGraphicsJobsString             = L10n.Tr("Changed Graphics Jobs Setting");
            public static readonly string undoChangedGraphicsJobModeString          = L10n.Tr("Changed Graphics Job Mode Setting");
            public static readonly string changeColorSpaceString                    = L10n.Tr("Changing the color space may take a significant amount of time.");
            public static readonly string undoChangedPlatformShaderChunkSizeString  = L10n.Tr("Changed Shader Chunk Size Platform Setting");
            public static readonly string undoChangedPlatformShaderChunkCountString = L10n.Tr("Changed Shader Chunk Count Platform Setting");
            public static readonly string undoChangedDefaultShaderChunkSizeString   = L10n.Tr("Changed Shader Chunk Size Default Setting");
            public static readonly string undoChangedDefaultShaderChunkCountString  = L10n.Tr("Changed Shader Chunk Count Default Setting");

            public static readonly string globalPlayerSettingsInfo =
            L10n.Tr("Editing these global player settings will not affect the current state of the project, because the active build profile is using its own customized player settings. Edit the build profile to change them.");
            public static readonly string globalPlayerSettingsInfoButton = L10n.Tr("Edit Build Profile");
        }

        internal class RecompileReason
        {
            public static readonly string scriptingDefineSymbolsModified             = L10n.Tr("Scripting define symbols setting modified");
            public static readonly string suppressCommonWarningsModified             = L10n.Tr("Suppress common warnings setting modified");
            public static readonly string allowUnsafeCodeModified                    = L10n.Tr("Allow 'unsafe' code setting modified");
            public static readonly string apiCompatibilityLevelModified              = L10n.Tr("API Compatibility level modified");
            public static readonly string editorAssembliesCompatibilityLevelModified = L10n.Tr("Editor Assemblies Compatibility level modified");
            public static readonly string useDeterministicCompilationModified        = L10n.Tr("Use deterministic compilation modified");
            public static readonly string additionalCompilerArgumentsModified        = L10n.Tr("Additional compiler arguments modified");
            public static readonly string activeBuildTargetGroupModified             = L10n.Tr("Active build target group modified");

            public static readonly string presetChanged = L10n.Tr("Preset changed");
        }

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

        PlayerSettingsIconsEditor m_IconsEditor;
        PlayerSettingsIconsEditor iconsEditor
        {
            get
            {
                if (m_IconsEditor == null)
                    m_IconsEditor = new PlayerSettingsIconsEditor(this);
                return m_IconsEditor;
            }
        }

        private static MeshDeformation[] m_MeshDeformations = { MeshDeformation.CPU, MeshDeformation.GPU, MeshDeformation.GPUBatched };

        internal static void SyncEditors(BuildTarget target)
        {
            foreach(var editor in s_activeEditors)
                editor.OnSyncEditor(target);
        }

        // Section and tab selection state

        SavedInt m_SelectedSection = new SavedInt("PlayerSettings.ShownSection", -1);

        BuildPlatform[] validPlatforms;
        NamedBuildTarget lastNamedBuildTarget;

        // il2cpp
        SerializedProperty m_StripEngineCode;

        // macOS
        SerializedProperty m_ApplicationBundleVersion;

        // vulkan
        SerializedProperty m_VulkanNumSwapchainBuffers;
        SerializedProperty m_VulkanEnableLateAcquireNextImage;
        SerializedProperty m_VulkanEnableCommandBufferRecycling;
        SerializedProperty m_VulkanEnableSetSRGBWrite;

        // iOS, tvOS
#pragma warning disable 169
        SerializedProperty m_IPhoneApplicationDisplayName;

        SerializedProperty m_CameraUsageDescription;
        SerializedProperty m_LocationUsageDescription;
        SerializedProperty m_MicrophoneUsageDescription;

        SerializedProperty m_IPhoneScriptCallOptimization;

        SerializedProperty m_DefaultScreenOrientation;
        SerializedProperty m_AllowedAutoRotateToPortrait;
        SerializedProperty m_AllowedAutoRotateToPortraitUpsideDown;
        SerializedProperty m_AllowedAutoRotateToLandscapeRight;
        SerializedProperty m_AllowedAutoRotateToLandscapeLeft;
        SerializedProperty m_UseOSAutoRotation;
        SerializedProperty m_Use32BitDisplayBuffer;
        SerializedProperty m_PreserveFramebufferAlpha;
        SerializedProperty m_DisableDepthAndStencilBuffers;

        SerializedProperty m_AndroidProfiler;

        SerializedProperty m_UIRequiresPersistentWiFi;
        SerializedProperty m_UIStatusBarHidden;
        SerializedProperty m_UIRequiresFullScreen;
        SerializedProperty m_UIStatusBarStyle;

        SerializedProperty m_InsecureHttpOption;
        SerializedProperty m_SubmitAnalytics;

        SerializedProperty m_IOSURLSchemes;

        SerializedProperty m_AccelerometerFrequency;
        SerializedProperty m_MuteOtherAudioSources;
        SerializedProperty m_PrepareIOSForRecording;
        SerializedProperty m_ForceIOSSpeakersWhenRecording;

        SerializedProperty m_EnableInternalProfiler;
        SerializedProperty m_ActionOnDotNetUnhandledException;
        SerializedProperty m_LogObjCUncaughtExceptions;
        SerializedProperty m_EnableCrashReportAPI;

        SerializedProperty m_SuppressCommonWarnings;
        SerializedProperty m_AllowUnsafeCode;
        SerializedProperty m_GCIncremental;

        SerializedProperty m_OverrideDefaultApplicationIdentifier;
        SerializedProperty m_ApplicationIdentifier;

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
        SerializedProperty m_UnsupportedMSAAFallback;
        SerializedProperty m_StripUnusedMeshComponents;
        SerializedProperty m_StrictShaderVariantMatching;
        SerializedProperty m_MipStripping;
        SerializedProperty m_VertexChannelCompressionMask;
        SerializedProperty m_MetalAPIValidation;
        SerializedProperty m_MetalFramebufferOnly;
        SerializedProperty m_MetalForceHardShadows;
        SerializedProperty m_FramebufferDepthMemorylessMode;

        SerializedProperty m_DefaultIsNativeResolution;
        SerializedProperty m_MacRetinaSupport;

        SerializedProperty m_UsePlayerLog;
        SerializedProperty m_CaptureStartupLogs;
        SerializedProperty m_KeepLoadedShadersAlive;
        SerializedProperty m_PreloadedAssets;
        SerializedProperty m_BakeCollisionMeshes;
        SerializedProperty m_DedicatedServerOptimizations;
        SerializedProperty m_ResizableWindow;
        SerializedProperty m_FullscreenMode;
        SerializedProperty m_VisibleInBackground;
        SerializedProperty m_AllowFullscreenSwitch;
        SerializedProperty m_ForceSingleInstance;
        SerializedProperty m_UseFlipModelSwapchain;

        SerializedProperty m_RunInBackground;

        SerializedProperty m_SkinOnGPU;
        SerializedProperty m_MeshDeformation;

        SerializedProperty m_EnableLoadStoreDebugMode;

        // OpenGL ES 3.1+
        SerializedProperty m_RequireES31;
        SerializedProperty m_RequireES31AEP;
        SerializedProperty m_RequireES32;

        SerializedProperty m_LightmapEncodingQuality;
        SerializedProperty m_HDRCubemapEncodingQuality;
        SerializedProperty m_LightmapStreamingEnabled;
        SerializedProperty m_LightmapStreamingPriority;

        SerializedProperty m_EnableOpenGLProfilerGPURecorders;

        SerializedProperty m_EnableFrameTimingStats;

        SerializedProperty m_AllowHDRDisplaySupport;
        SerializedProperty m_UseHDRDisplay;
        SerializedProperty m_HDRBitDepth;

        // WebGPU
        SerializedProperty m_WebGPUSupportEnabled;

        // Legacy
        SerializedProperty m_LegacyClampBlendShapeWeights;
        SerializedProperty m_AndroidEnableTango;
        SerializedProperty m_Enable360StereoCapture;

        SerializedProperty m_VirtualTexturingSupportEnabled;
        SerializedProperty m_ShaderPrecisionModel;

        // Scripting
        SerializedProperty m_UseDeterministicCompilation;
        SerializedProperty m_ScriptingBackend;
        SerializedProperty m_APICompatibilityLevel;
        SerializedProperty m_DefaultAPICompatibilityLevel;
        SerializedProperty m_EditorAssembliesCompatibilityLevel;
        SerializedProperty m_Il2CppCompilerConfiguration;
        SerializedProperty m_Il2CppCodeGeneration;
        SerializedProperty m_Il2CppStacktraceInformation;
        SerializedProperty m_ScriptingDefines;
        SerializedProperty m_AdditionalCompilerArguments;
        SerializedProperty m_StackTraceTypes;
        SerializedProperty m_ManagedStrippingLevel;
        SerializedProperty m_ActiveInputHandler;

        SerializedProperty m_SpriteBatchVertexThreshold;
        SerializedProperty m_SpriteBatchMaxVertexCount;


        // Embedded Linux specific
        SerializedProperty m_ForceSRGBBlit;

        // Localization Cache
        string m_LocalizedTargetName;

        // reorderable lists of graphics devices, per platform
        Dictionary<BuildTarget, ReorderableList> m_GraphicsDeviceLists = new Dictionary<BuildTarget, ReorderableList>();
        ReorderableList m_ColorGamutList;

        int scriptingDefinesControlID = 0;

        int serializedActiveInputHandler = 0;
        string[] serializedAdditionalCompilerArguments;
        bool serializedSuppressCommonWarnings = true;
        bool serializedAllowUnsafeCode = false;
        string serializedScriptingDefines;
        bool serializedUseDeterministicCompilation;

        List<string> scriptingDefinesList;
        bool hasScriptingDefinesBeenModified;
        ReorderableList scriptingDefineSymbolsList;

        List<string> additionalCompilerArgumentsList;
        bool hasAdditionalCompilerArgumentsBeenModified;
        ReorderableList additionalCompilerArgumentsReorderableList;

        ISettingEditorExtension[] m_SettingsExtensions;
        private HashSet<string> m_Reasons = new HashSet<string>();

        // Section animation state
        const int kNumberGUISections = 7;
        List<AnimBool> m_SectionAnimators = new List<AnimBool>(kNumberGUISections);
        readonly AnimBool m_ShowDefaultIsNativeResolution = new AnimBool();
        readonly AnimBool m_ShowResolution = new AnimBool();
        private static Texture2D s_WarningIcon;

        // Preset check
        bool isPresetWindowOpen = false;
        bool hasPresetWindowClosed = false;

        /// <summary>
        /// Internal callback set by the build profile window when tracking
        /// changes to settings not represented by a serialized property.
        /// </summary>
        Action<SerializedObject> m_OnTrackSerializedObjectValueChanged;

        internal bool IsPreset() => playerSettingsType == PlayerSettingsType.Preset;

        internal enum PlayerSettingsType
        {
            Global,
            Preset,
            ActiveBuildProfile,
            NonActiveBuildProfile
        }
        internal PlayerSettingsType playerSettingsType = PlayerSettingsType.Global;
        internal bool IsBuildProfileEditor() => playerSettingsType == PlayerSettingsType.ActiveBuildProfile || playerSettingsType == PlayerSettingsType.NonActiveBuildProfile;
        internal bool IsActivePlayerSettingsEditor() => (playerSettingsType == PlayerSettingsType.Global && !BuildProfileContext.ProjectHasActiveProfileWithPlayerSettings()) || playerSettingsType == PlayerSettingsType.ActiveBuildProfile;

        internal void OnTargetObjectChangedDirectly() => m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);

        internal void OnSyncEditor(BuildTarget target)
        {
            SyncColorGamuts();

            if (target == BuildTarget.NoTarget)
                return;

            SyncPlatformAPIsList(target);
        }

        const string kSelectedPlatform = "PlayerSettings.SelectedPlatform";

        /// <summary>
        /// Current serialized object target as <see cref="PlayerSettings"/>.
        /// </summary>
        PlayerSettings m_CurrentTarget;

        public SerializedProperty FindPropertyAssert(string name)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            if (property == null)
                Debug.LogError("Failed to find:" + name);
            return property;
        }

        private static List<PlayerSettingsEditor> s_activeEditors = new List<PlayerSettingsEditor>();
        void OnEnable()
        {
            s_activeEditors.Add(this);
            if (Preset.IsEditorTargetAPreset(target))
                playerSettingsType = PlayerSettingsType.Preset;
            validPlatforms = BuildPlatforms.instance.GetValidPlatforms(true).ToArray();
            m_CurrentTarget = target as PlayerSettings;

            m_StripEngineCode               = FindPropertyAssert("stripEngineCode");

            m_IPhoneScriptCallOptimization  = FindPropertyAssert("iPhoneScriptCallOptimization");
            m_AndroidProfiler               = FindPropertyAssert("AndroidProfiler");
            m_CompanyName                   = FindPropertyAssert("companyName");
            m_ProductName                   = FindPropertyAssert("productName");

            m_DefaultCursor                 = FindPropertyAssert("defaultCursor");
            m_CursorHotspot                 = FindPropertyAssert("cursorHotspot");


            m_UIRequiresFullScreen          = FindPropertyAssert("uIRequiresFullScreen");

            m_UIStatusBarHidden             = FindPropertyAssert("uIStatusBarHidden");
            m_UIStatusBarStyle              = FindPropertyAssert("uIStatusBarStyle");
            m_ActiveColorSpace              = FindPropertyAssert("m_ActiveColorSpace");
            m_UnsupportedMSAAFallback       = FindPropertyAssert("unsupportedMSAAFallback");
            m_StripUnusedMeshComponents     = FindPropertyAssert("StripUnusedMeshComponents");
            m_StrictShaderVariantMatching   = FindPropertyAssert("strictShaderVariantMatching");
            m_MipStripping                  = FindPropertyAssert("mipStripping");
            m_VertexChannelCompressionMask  = FindPropertyAssert("VertexChannelCompressionMask");
            m_MetalAPIValidation            = FindPropertyAssert("metalAPIValidation");
            m_MetalFramebufferOnly          = FindPropertyAssert("metalFramebufferOnly");
            m_MetalForceHardShadows         = FindPropertyAssert("iOSMetalForceHardShadows");
            m_FramebufferDepthMemorylessMode = FindPropertyAssert("framebufferDepthMemorylessMode");

            m_OverrideDefaultApplicationIdentifier = FindPropertyAssert("overrideDefaultApplicationIdentifier");
            m_ApplicationIdentifier         = FindPropertyAssert("applicationIdentifier");

            m_ApplicationBundleVersion      = serializedObject.FindProperty("bundleVersion");
            if (m_ApplicationBundleVersion == null)
                m_ApplicationBundleVersion  = FindPropertyAssert("iPhoneBundleVersion");

            m_AccelerometerFrequency        = FindPropertyAssert("accelerometerFrequency");

            m_MuteOtherAudioSources         = FindPropertyAssert("muteOtherAudioSources");
            m_PrepareIOSForRecording        = FindPropertyAssert("Prepare IOS For Recording");
            m_ForceIOSSpeakersWhenRecording = FindPropertyAssert("Force IOS Speakers When Recording");
            m_UIRequiresPersistentWiFi      = FindPropertyAssert("uIRequiresPersistentWiFi");
            m_InsecureHttpOption            = FindPropertyAssert("insecureHttpOption");
            m_SubmitAnalytics               = FindPropertyAssert("submitAnalytics");

            m_IOSURLSchemes                 = FindPropertyAssert("iOSURLSchemes");

            m_CameraUsageDescription        = FindPropertyAssert("cameraUsageDescription");
            m_LocationUsageDescription      = FindPropertyAssert("locationUsageDescription");
            m_MicrophoneUsageDescription    = FindPropertyAssert("microphoneUsageDescription");

            m_EnableInternalProfiler        = FindPropertyAssert("enableInternalProfiler");
            m_ActionOnDotNetUnhandledException  = FindPropertyAssert("actionOnDotNetUnhandledException");
            m_LogObjCUncaughtExceptions     = FindPropertyAssert("logObjCUncaughtExceptions");
            m_EnableCrashReportAPI          = FindPropertyAssert("enableCrashReportAPI");

            m_SuppressCommonWarnings        = FindPropertyAssert("suppressCommonWarnings");
            m_AllowUnsafeCode               = FindPropertyAssert("allowUnsafeCode");
            m_GCIncremental                 = FindPropertyAssert("gcIncremental");
            m_UseDeterministicCompilation   = FindPropertyAssert("useDeterministicCompilation");
            m_ScriptingBackend              = FindPropertyAssert("scriptingBackend");
            m_APICompatibilityLevel         = FindPropertyAssert("apiCompatibilityLevelPerPlatform");
            m_DefaultAPICompatibilityLevel  = FindPropertyAssert("apiCompatibilityLevel");
            m_EditorAssembliesCompatibilityLevel = FindPropertyAssert("editorAssembliesCompatibilityLevel");
            m_Il2CppCompilerConfiguration   = FindPropertyAssert("il2cppCompilerConfiguration");
            m_Il2CppCodeGeneration          = FindPropertyAssert("il2cppCodeGeneration");
            m_Il2CppStacktraceInformation   = FindPropertyAssert("il2cppStacktraceInformation");
            m_ScriptingDefines              = FindPropertyAssert("scriptingDefineSymbols");
            m_StackTraceTypes               = FindPropertyAssert("m_StackTraceTypes");
            m_ManagedStrippingLevel         = FindPropertyAssert("managedStrippingLevel");
            m_ActiveInputHandler            = FindPropertyAssert("activeInputHandler");
            m_AdditionalCompilerArguments   = FindPropertyAssert("additionalCompilerArguments");

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
            m_PreserveFramebufferAlpha              = FindPropertyAssert("preserveFramebufferAlpha");
            m_DisableDepthAndStencilBuffers         = FindPropertyAssert("disableDepthAndStencilBuffers");

            m_DefaultIsNativeResolution     = FindPropertyAssert("defaultIsNativeResolution");
            m_MacRetinaSupport              = FindPropertyAssert("macRetinaSupport");
            m_UsePlayerLog                  = FindPropertyAssert("usePlayerLog");
            m_CaptureStartupLogs            = FindPropertyAssert("captureStartupLogs");

            m_KeepLoadedShadersAlive           = FindPropertyAssert("keepLoadedShadersAlive");
            m_PreloadedAssets                  = FindPropertyAssert("preloadedAssets");
            m_BakeCollisionMeshes              = FindPropertyAssert("bakeCollisionMeshes");
            m_DedicatedServerOptimizations     = FindPropertyAssert("dedicatedServerOptimizations");
            m_ResizableWindow                  = FindPropertyAssert("resizableWindow");
            m_VulkanNumSwapchainBuffers        = FindPropertyAssert("vulkanNumSwapchainBuffers");
            m_VulkanEnableLateAcquireNextImage = FindPropertyAssert("vulkanEnableLateAcquireNextImage");
            m_VulkanEnableCommandBufferRecycling = FindPropertyAssert("vulkanEnableCommandBufferRecycling");
            m_VulkanEnableSetSRGBWrite         = FindPropertyAssert("vulkanEnableSetSRGBWrite");
            m_FullscreenMode                   = FindPropertyAssert("fullscreenMode");
            m_VisibleInBackground              = FindPropertyAssert("visibleInBackground");
            m_AllowFullscreenSwitch            = FindPropertyAssert("allowFullscreenSwitch");
            m_SkinOnGPU                        = FindPropertyAssert("gpuSkinning");
            m_MeshDeformation                  = FindPropertyAssert("meshDeformation");
            m_ForceSingleInstance              = FindPropertyAssert("forceSingleInstance");
            m_UseFlipModelSwapchain            = FindPropertyAssert("useFlipModelSwapchain");

            m_AllowHDRDisplaySupport = FindPropertyAssert("allowHDRDisplaySupport");
            m_UseHDRDisplay = FindPropertyAssert("useHDRDisplay");
            m_HDRBitDepth = FindPropertyAssert("hdrBitDepth");
            m_EnableFrameTimingStats = FindPropertyAssert("enableFrameTimingStats");
            m_EnableOpenGLProfilerGPURecorders = FindPropertyAssert("enableOpenGLProfilerGPURecorders");

            m_RequireES31                   = FindPropertyAssert("openGLRequireES31");
            m_RequireES31AEP                = FindPropertyAssert("openGLRequireES31AEP");
            m_RequireES32                   = FindPropertyAssert("openGLRequireES32");

            m_LegacyClampBlendShapeWeights = FindPropertyAssert("legacyClampBlendShapeWeights");
            m_AndroidEnableTango           = FindPropertyAssert("AndroidEnableTango");

            m_SpriteBatchVertexThreshold = FindPropertyAssert("m_SpriteBatchVertexThreshold");
            m_SpriteBatchMaxVertexCount = FindPropertyAssert("m_SpriteBatchMaxVertexCount");

            SerializedProperty property = FindPropertyAssert("vrSettings");
            if (property != null)
                m_Enable360StereoCapture = property.FindPropertyRelative("enable360StereoCapture");

            m_VirtualTexturingSupportEnabled = FindPropertyAssert("virtualTexturingSupportEnabled");
            m_ShaderPrecisionModel = FindPropertyAssert("shaderPrecisionModel");

            m_ForceSRGBBlit                 = FindPropertyAssert("hmiForceSRGBBlit");

            var validPlatformsLength = validPlatforms.Length;
            m_SettingsExtensions = new ISettingEditorExtension[validPlatformsLength];
            var currentPlatform = 0;
            var isStandaloneGroup = EditorUserBuildSettings.activeBuildTargetGroup == BuildTargetGroup.Standalone;
            for (int i = 0; i < validPlatformsLength; i++)
            {
                // Show the settings of the active standalone platform for the standalone tab in global player settings
                var buildTargetGroup = validPlatforms[i].namedBuildTarget.ToBuildTargetGroup();
                var module = (isStandaloneGroup && buildTargetGroup == BuildTargetGroup.Standalone) ?
                    ModuleManager.GetTargetStringFromBuildTarget(EditorUserBuildSettings.activeBuildTarget) :
                    ModuleManager.GetTargetStringFromBuildTargetGroup(buildTargetGroup);

                m_SettingsExtensions[i] = ModuleManager.GetEditorSettingsExtension(module);
                if (m_SettingsExtensions[i] != null)
                    m_SettingsExtensions[i].OnEnable(this);
                if (validPlatforms[i].IsActive())
                    currentPlatform = i;
            }

            for (int i = 0; i < kNumberGUISections; i++)
                m_SectionAnimators.Add(new AnimBool(m_SelectedSection.value == i));
            SetValueChangeListeners(Repaint);

            splashScreenEditor.OnEnable();
            iconsEditor.OnEnable();

            // we clear it just to be on the safe side:
            // we access this cache both from player settings editor and script side when changing api
            m_GraphicsDeviceLists.Clear();

            var selectedPlatform = SessionState.GetInt(kSelectedPlatform, currentPlatform);
            if (selectedPlatform < 0)
                selectedPlatform = 0;

            if (selectedPlatform >= validPlatformsLength)
                selectedPlatform = validPlatformsLength - 1;

            // Setup initial values to prevent immediate script recompile (or editor restart)
            NamedBuildTarget namedBuildTarget = validPlatforms[selectedPlatform].namedBuildTarget;
            serializedActiveInputHandler = m_ActiveInputHandler.intValue;
            serializedSuppressCommonWarnings = m_SuppressCommonWarnings.boolValue;
            serializedAllowUnsafeCode = m_AllowUnsafeCode.boolValue;
            serializedAdditionalCompilerArguments = GetAdditionalCompilerArgumentsForGroup(namedBuildTarget);
            serializedScriptingDefines = GetScriptingDefineSymbolsForGroup(namedBuildTarget);
            serializedUseDeterministicCompilation = m_UseDeterministicCompilation.boolValue;

            InitReorderableScriptingDefineSymbolsList(namedBuildTarget);
            InitReorderableAdditionalCompilerArgumentsList(namedBuildTarget);

            FindPlayerSettingsAttributeSections();
        }

        void OnDisable()
        {
            s_activeEditors.Remove(this);
            HandlePendingChangesRequiringRecompilation();

            // Ensure script compilation handling is returned to to EditorOnlyPlayerSettings
            if (!IsPreset())
                PlayerSettings.isHandlingScriptRecompile = true;
        }

        /// <summary>
        /// Configures the player settings editor for a build profile, ensuring only one platform
        /// tab is displayed in the platform grouping.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal void ConfigurePlayerSettingsForBuildProfile(
            SerializedObject serializedProfile,
            GUID buildProfilePlatformGuid,
            bool isActiveBuildProfile,
            Action<SerializedObject> onTrackSerializedObjectChanged)
        {
            m_OnTrackSerializedObjectValueChanged = onTrackSerializedObjectChanged;
            playerSettingsType = isActiveBuildProfile ? PlayerSettingsType.ActiveBuildProfile : PlayerSettingsType.NonActiveBuildProfile;

            // We don't want to show other platform tabs that it's not the build profile one
            var gotValidPlatform = false;
            var buildProfileBasePlatformGuid = BuildTargetDiscovery.GetBasePlatformGUID(buildProfilePlatformGuid);
            var buildProfileSubtarget = BuildTargetDiscovery.GetBuildTargetAndSubtargetFromGUID(buildProfileBasePlatformGuid).Item2;
            var isBuildProfilePlatformStandalone = buildProfileSubtarget == StandaloneBuildSubtarget.Player;
            var isBuildProfilePlatformServer = buildProfileSubtarget == StandaloneBuildSubtarget.Server;
            for (int i = 0; i < validPlatforms.Length; i++)
            {
                var buildTarget = validPlatforms[i].defaultTarget;
                var namedBuildTarget = validPlatforms[i].namedBuildTarget;
                var basePlatformGuid = BuildTargetDiscovery.GetBasePlatformGUIDFromBuildTarget(namedBuildTarget, buildTarget);

                // Player settings tabs are shown by BuildPlatform/NamedBuildTarget, so we need to compare the
                // NamedBuildTarget in addition to the base platform guid for standalone and server platforms
                var isStandalone = namedBuildTarget == NamedBuildTarget.Standalone && isBuildProfilePlatformStandalone;
                var isServer = namedBuildTarget == NamedBuildTarget.Server && isBuildProfilePlatformServer;
                if (basePlatformGuid != buildProfileBasePlatformGuid && !(isStandalone || isServer))
                    continue;

                var copy = (BuildPlatform)validPlatforms[i].Clone();
                copy.tooltip = string.Empty;
                validPlatforms[0] = copy;
                gotValidPlatform = true;
                break;
            }

            if (!gotValidPlatform)
                return;

            Array.Resize(ref validPlatforms, 1);
            m_SettingsExtensions = new ISettingEditorExtension[1];
            m_SettingsExtensions[0] = ModuleManager.GetEditorSettingsExtension(buildProfilePlatformGuid);
            m_SettingsExtensions[0]?.OnEnable(this);
            m_SettingsExtensions[0]?.ConfigurePlatformProfile(serializedProfile);
        }

        /// <summary>
        /// Handles editor update when player setting changes outside the UI. If required,
        /// schedules background work during the next editor update. After the global player settings
        /// has changes.
        /// </summary>
        internal static void HandlePlayerSettingsChanged(
            PlayerSettings current, PlayerSettings next,
            BuildTarget currentBuildTarget, BuildTarget nextBuildTarget)
        {
            BuildTargetGroup currentBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(currentBuildTarget);
            BuildTargetGroup nextBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(nextBuildTarget);
            bool isLightmapEncodingChanged =
                current.GetLightmapEncodingQualityForPlatform_Internal(currentBuildTarget)
                != next.GetLightmapEncodingQualityForPlatform_Internal(nextBuildTarget);
            bool isHDRCubemapEncodingChanged =
                current.GetHDRCubemapEncodingQualityForPlatform_Internal(currentBuildTarget)
                != next.GetHDRCubemapEncodingQualityForPlatform_Internal(nextBuildTarget);
            bool isLightmapStreamingChanged =
                (current.GetLightmapStreamingEnabledForPlatformGroup_Internal(currentBuildTargetGroup)
                    != next.GetLightmapStreamingEnabledForPlatformGroup_Internal(nextBuildTargetGroup))
                || (current.GetLightmapStreamingPriorityForPlatformGroup_Internal(currentBuildTargetGroup)
                    != next.GetLightmapStreamingPriorityForPlatformGroup_Internal(nextBuildTargetGroup));
            bool isShaderPrecisionChanged = PlayerSettings.ShouldSyncShaderPrecisionModel(current, next);

            EditorApplication.delayCall += () =>
            {
                if (isHDRCubemapEncodingChanged)
                {
                    Lightmapping.OnUpdateHDRCubemapEncoding(nextBuildTargetGroup);
                }

                if (isLightmapEncodingChanged)
                {
                    Lightmapping.OnUpdateLightmapEncoding(nextBuildTargetGroup);
                }

                if (isLightmapStreamingChanged)
                {
                    Lightmapping.OnUpdateLightmapStreaming(nextBuildTargetGroup);
                }

                if (isShaderPrecisionChanged)
                {
                    PlayerSettings.SyncShaderPrecisionModel();
                }
            };
        }

        /// <summary>
        /// Check if the platform-specific player settings in ISettingsExtensionData on the managed side
        /// are equal to the corresponding data in the project settings
        /// </summary>
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal bool IsPlayerSettingsExtensionDataEqualToProjectSettings()
        {
            if (m_SettingsExtensions == null || m_SettingsExtensions.Length == 0 || m_SettingsExtensions[0] == null)
                return false;

            return m_SettingsExtensions[0].IsPlayerSettingsDataEqualToProjectSettings();
        }

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal bool CopyProjectSettingsToPlayerSettingsExtension()
        {
            if (m_SettingsExtensions == null || m_SettingsExtensions.Length == 0 || m_SettingsExtensions[0] == null)
                return false;

            return m_SettingsExtensions[0].CopyProjectSettingsPlayerSettingsToBuildProfile();
        }

        [RequiredByNativeCode]
        private static void HandlePendingChangesBeforeEnterPlaymode()
        {
            foreach (var editor in s_activeEditors)
            {
                editor.HandlePendingChangesRequiringRecompilation();
            }

            // Handle build profile pending recompilation.
            BuildProfileContext.HandlePendingChangesBeforeEnterPlaymode();
        }

        private void HandlePendingChangesRequiringRecompilation()
        {
            if (hasScriptingDefinesBeenModified)
            {
                if (EditorUtility.DisplayDialog("Scripting Define Symbols Have Been Modified", "Do you want to apply changes?", "Apply", "Revert"))
                {
                    SetScriptingDefineSymbolsForGroup(lastNamedBuildTarget, scriptingDefinesList.ToArray());
                    SetReason(RecompileReason.scriptingDefineSymbolsModified);
                }
                else
                {
                    InitReorderableScriptingDefineSymbolsList(lastNamedBuildTarget);
                }

                hasScriptingDefinesBeenModified = false;
            }

            if (hasAdditionalCompilerArgumentsBeenModified)
            {
                if (EditorUtility.DisplayDialog("Additional Compiler Arguments Have Been Modified", "Do you want to apply changes?", "Apply", "Revert"))
                {
                    SetAdditionalCompilerArgumentsForGroup(lastNamedBuildTarget, additionalCompilerArgumentsList.ToArray());
                    SetReason(RecompileReason.additionalCompilerArgumentsModified);
                }
                else
                {
                    InitReorderableAdditionalCompilerArgumentsList(lastNamedBuildTarget);
                }

                hasAdditionalCompilerArgumentsBeenModified = false;
            }

            if (HasReasonToCompile())
            {
                serializedObject.ApplyModifiedProperties();
                RecompileScripts();
            }
        }

        public void SetValueChangeListeners(UnityAction action)
        {
            for (int i = 0; i < m_SectionAnimators.Count; i++)
            {
                m_SectionAnimators[i].valueChanged.RemoveAllListeners();
                m_SectionAnimators[i].valueChanged.AddListener(action);
            }

            m_ShowDefaultIsNativeResolution.valueChanged.RemoveAllListeners();
            m_ShowDefaultIsNativeResolution.valueChanged.AddListener(action);

            m_ShowResolution.valueChanged.RemoveAllListeners();
            m_ShowResolution.valueChanged.AddListener(action);
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

        private void CheckUpdatePresetSelectorStatus()
        {
            if (playerSettingsType != PlayerSettingsType.Global)
                return;

            bool isOpen = PresetEditorHelper.presetEditorOpen;
            hasPresetWindowClosed = (isPresetWindowOpen && !isOpen);
            isPresetWindowOpen = isOpen;

            if (isPresetWindowOpen)
                PlayerSettings.isHandlingScriptRecompile = false;
        }

        private void SetReason(string reason)
        {
            if (!IsActivePlayerSettingsEditor())
            {
                return;
            }

            m_Reasons.Add(reason);
        }

        private string ConvertReasonsToString()
        {
            var sb = new StringBuilder();
            foreach (var reason in m_Reasons)
            {
                sb.AppendLine(reason);
            }

            return sb.ToString();
        }

        private void RecompileScripts()
        {
            if (!IsActivePlayerSettingsEditor() || isPresetWindowOpen)
            {
                return;
            }

            var reasons = ConvertReasonsToString();
            PlayerSettings.RecompileScripts(reasons);
            m_Reasons.Clear();
        }

        private bool HasReasonToCompile()
        {
            return m_Reasons.Count > 0;
        }

        private bool SupportsRunInBackground(NamedBuildTarget buildTarget)
        {
            return buildTarget == NamedBuildTarget.Standalone;
        }

        private void OnPresetSelectorClosed()
        {
            hasPresetWindowClosed = false;

            if (playerSettingsType != PlayerSettingsType.Global)
                return;

            if (HasReasonToCompile())
            {
                RecompileScripts();
            }

            PlayerSettings.isHandlingScriptRecompile = true;
        }

        public override void OnInspectorGUI()
        {
            DisplayBuildProfileHelpBoxIfNeeded();

            var serializedObjectUpdated = serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.BeginVertical();
            {
                CommonSettings();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            int oldPlatform = SessionState.GetInt(kSelectedPlatform, 0);
            int selectedPlatformValue = EditorGUILayout.BeginPlatformGrouping(validPlatforms, null);
            if (selectedPlatformValue != oldPlatform)
            {
                SessionState.SetInt(kSelectedPlatform, selectedPlatformValue);
            }

            if (EditorGUI.EndChangeCheck())
            {
                // Awesome hackery to get string from delayed textfield when switching platforms
                if (EditorGUI.s_DelayedTextEditor.IsEditingControl(scriptingDefinesControlID))
                {
                    EditorGUI.EndEditingActiveTextField();
                    GUIUtility.keyboardControl = 0;
                    string[] defines = ScriptingDefinesHelper.ConvertScriptingDefineStringToArray(EditorGUI.s_DelayedTextEditor.text);
                    SetScriptingDefineSymbolsForGroup(validPlatforms[oldPlatform].namedBuildTarget, defines);
                }
                // Reset focus when changing between platforms.
                // If we don't do this, the resolution width/height value will not update correctly when they have the focus
                GUI.FocusControl("");

                m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            BuildPlatform platform = validPlatforms[selectedPlatformValue];

            if (playerSettingsType == PlayerSettingsType.Global)
                CheckUpdatePresetSelectorStatus();

            if (!IsBuildProfileEditor())
                GUILayout.Label(string.Format(L10n.Tr("Settings for {0}"), validPlatforms[selectedPlatformValue].title.text));

            // Increase the offset to accomodate large labels, though keep a minimum of 150.
            EditorGUIUtility.labelWidth = Mathf.Max(150, EditorGUIUtility.labelWidth + 4);

            int sectionIndex = 0;

            if (serializedObjectUpdated)
            {
                m_IconsEditor.SerializedObjectUpdated();
                foreach (var settingsExtension in m_SettingsExtensions)
                {
                    settingsExtension?.SerializedObjectUpdated();
                }
            }

            m_IconsEditor.IconSectionGUI(platform, m_SettingsExtensions[selectedPlatformValue], selectedPlatformValue, sectionIndex++);

            ResolutionSectionGUI(platform, m_SettingsExtensions[selectedPlatformValue], sectionIndex++);
            m_SplashScreenEditor.SplashSectionGUI(platform, m_SettingsExtensions[selectedPlatformValue], sectionIndex++);
            DebugAndCrashReportingGUI(platform, m_SettingsExtensions[selectedPlatformValue], sectionIndex++);
            OtherSectionGUI(platform, m_SettingsExtensions[selectedPlatformValue], sectionIndex++);
            PublishSectionGUI(platform, m_SettingsExtensions[selectedPlatformValue], sectionIndex++);

            PlayerSettingsAttributeSectionsGUI(platform.namedBuildTarget, m_SettingsExtensions[selectedPlatformValue], ref sectionIndex);

            EditorGUILayout.EndPlatformGrouping();

            serializedObject.ApplyModifiedProperties();

            if (hasPresetWindowClosed)
            {
                // We recompile after the window is closed just to make sure all the values are set/shown correctly.
                // There might be a smarter idea where you detect the values that have changed and only do it if it's required,
                // but the way the Preset window applies those changes as well as the way IMGUI works makes it difficult to track.
                SetReason(RecompileReason.presetChanged);

                OnPresetSelectorClosed();
            }
            else if (HasReasonToCompile())
            {
                RecompileScripts();
            }
        }

        void DisplayBuildProfileHelpBoxIfNeeded()
        {
            if (playerSettingsType == PlayerSettingsType.Global && BuildProfileContext.activeProfile?.playerSettings != null)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.BeginVertical();
                GUILayout.Space(5);
                GUILayout.Label(EditorGUIUtility.GetHelpIcon(MessageType.Warning), GUILayout.ExpandWidth(false));
                GUILayout.EndVertical();
                GUILayout.Label(SettingsContent.globalPlayerSettingsInfo, EditorStyles.wordWrappedMiniLabel);
                GUILayout.BeginVertical();
                GUILayout.Space(5);
                if (GUILayout.Button(SettingsContent.globalPlayerSettingsInfoButton))
                    BuildPipeline.ShowBuildProfileWindow();
                GUILayout.Space(5);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        private void CommonSettings()
        {
            EditorGUILayout.PropertyField(m_CompanyName);
            EditorGUILayout.PropertyField(m_ProductName);
            EditorGUILayout.PropertyField(m_ApplicationBundleVersion, EditorGUIUtility.TrTextContent("Version"));
            EditorGUILayout.Space();

            m_IconsEditor.LegacyIconSectionGUI();

            GUILayout.Space(3);

            Rect cursorPropertyRect = EditorGUILayout.GetControlRect(true, EditorGUI.kObjectFieldThumbnailHeight);
            EditorGUI.BeginProperty(cursorPropertyRect, SettingsContent.defaultCursor, m_DefaultCursor);
            m_DefaultCursor.objectReferenceValue = EditorGUI.ObjectField(cursorPropertyRect, SettingsContent.defaultCursor, m_DefaultCursor.objectReferenceValue, typeof(Texture2D), false);
            EditorGUI.EndProperty();

            Rect rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, 0, SettingsContent.cursorHotspot);
            EditorGUI.PropertyField(rect, m_CursorHotspot, GUIContent.none);
        }

        public bool BeginSettingsBox(int nr, GUIContent header)
        {
            if (nr >= m_SectionAnimators.Count)
                m_SectionAnimators.Add(new AnimBool());
            bool enabled = GUI.enabled;
            GUI.enabled = true; // we don't want to disable the expand behavior
            EditorGUILayout.BeginVertical(Styles.categoryBox);
            Rect r = GUILayoutUtility.GetRect(20, 21);
            EditorGUI.BeginChangeCheck();
            bool expanded = EditorGUI.FoldoutTitlebar(r, header, m_SelectedSection.value == nr, true, EditorStyles.inspectorTitlebarFlat, EditorStyles.inspectorTitlebarText);
            if (EditorGUI.EndChangeCheck())
            {
                m_SelectedSection.value = (expanded ? nr : -1);
                GUIUtility.keyboardControl = 0;
            }
            m_SectionAnimators[nr].target = expanded;
            GUI.enabled = enabled;
            EditorGUI.indentLevel++;
            return EditorGUILayout.BeginFadeGroup(m_SectionAnimators[nr].faded);
        }

        public void EndSettingsBox()
        {
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        public void ShowSharedNote()
        {
            GUILayout.Label(SettingsContent.sharedBetweenPlatformsInfo, EditorStyles.miniLabel);
        }

        internal static void ShowNoSettings()
        {
            GUILayout.Label(SettingsContent.notApplicableInfo, EditorStyles.miniLabel);
        }

        private static bool TargetSupportsOptionalBuiltinSplashScreen(BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension)
        {
            if (settingsExtension != null)
                return settingsExtension.CanShowUnitySplashScreen();

            return targetGroup == BuildTargetGroup.Standalone;
        }

        public void ResolutionSectionGUI(BuildPlatform platform, ISettingEditorExtension settingsExtension, int sectionIndex = 0)
        {
            NamedBuildTarget namedBuildTarget = platform.namedBuildTarget;
            if (BeginSettingsBox(sectionIndex, SettingsContent.resolutionPresentationTitle))
            {
                // PLEASE DO NOT COPY SETTINGS TO APPEAR MULTIPLE PLACES IN THE CODE! See top of file for more info.
                if (namedBuildTarget == NamedBuildTarget.Server)
                {
                    ShowNoSettings();
                    EditorGUILayout.Space();
                }
                else
                {
                    GUILayout.Label(SettingsContent.resolutionTitle, EditorStyles.boldLabel);
                    if (SupportsRunInBackground(namedBuildTarget))
                        EditorGUILayout.PropertyField(m_RunInBackground, SettingsContent.runInBackground);

                    // Resolution itself
                    if (settingsExtension != null)
                    {
                        float h = EditorGUI.kSingleLineHeight;
                        float kLabelFloatMinW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
                        float kLabelFloatMaxW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
                        settingsExtension.ResolutionSectionGUI(h, kLabelFloatMinW, kLabelFloatMaxW);
                    }

                    if (namedBuildTarget == NamedBuildTarget.Standalone)
                    {

                        var fullscreenModes = new[] { FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.MaximizedWindow, FullScreenMode.Windowed };
                        var fullscreenModeNames = new[] { SettingsContent.fullscreenWindow, SettingsContent.exclusiveFullscreen, SettingsContent.maximizedWindow, SettingsContent.windowed };
                        var fullscreenModeNew = FullScreenMode.FullScreenWindow;
                        using (var horizontal = new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.PropertyScope(horizontal.rect, GUIContent.none, m_FullscreenMode))
                            {
                                fullscreenModeNew = BuildEnumPopup(m_FullscreenMode, SettingsContent.fullscreenMode, fullscreenModes, fullscreenModeNames);
                            }
                        }

                        bool defaultIsFullScreen = fullscreenModeNew != FullScreenMode.Windowed;
                        m_ShowDefaultIsNativeResolution.target = defaultIsFullScreen;
                        if (EditorGUILayout.BeginFadeGroup(m_ShowDefaultIsNativeResolution.faded))
                            EditorGUILayout.PropertyField(m_DefaultIsNativeResolution, SettingsContent.defaultIsNativeResolution);
                        EditorGUILayout.EndFadeGroup();

                        m_ShowResolution.target = !(defaultIsFullScreen && m_DefaultIsNativeResolution.boolValue);
                        if (EditorGUILayout.BeginFadeGroup(m_ShowResolution.faded))
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(m_DefaultScreenWidth, SettingsContent.defaultScreenWidth);
                            if (EditorGUI.EndChangeCheck() && m_DefaultScreenWidth.intValue < 1)
                                m_DefaultScreenWidth.intValue = 1;

                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(m_DefaultScreenHeight, SettingsContent.defaultScreenHeight);
                            if (EditorGUI.EndChangeCheck() && m_DefaultScreenHeight.intValue < 1)
                                m_DefaultScreenHeight.intValue = 1;
                        }
                        EditorGUILayout.EndFadeGroup();
                    }

                    if (namedBuildTarget == NamedBuildTarget.Standalone &&
                        BuildTargetDiscovery.TryGetProperties(platform.defaultTarget, out IGraphicsPlatformProperties properties) &&
                        (properties?.RetinaSupport ?? false))
                        EditorGUILayout.PropertyField(m_MacRetinaSupport, SettingsContent.macRetinaSupport);

                    if (settingsExtension != null && settingsExtension.SupportsOrientation())
                    {
                        GUILayout.Label(SettingsContent.orientationTitle, EditorStyles.boldLabel);

                        EditorGUILayout.PropertyField(m_DefaultScreenOrientation, SettingsContent.defaultScreenOrientation);

                        if (m_DefaultScreenOrientation.enumValueIndex == (int)UIOrientation.AutoRotation)
                        {
                            if (namedBuildTarget == NamedBuildTarget.iOS)
                                EditorGUILayout.PropertyField(m_UseOSAutoRotation, SettingsContent.useOSAutoRotation);

                            if (settingsExtension != null)
                                settingsExtension.AutoRotationSectionGUI();

                            EditorGUI.indentLevel++;

                            GUILayout.Label(SettingsContent.allowedOrientationTitle, EditorStyles.boldLabel);

                            bool somethingAllowed = m_AllowedAutoRotateToPortrait.boolValue
                                || m_AllowedAutoRotateToPortraitUpsideDown.boolValue
                                || m_AllowedAutoRotateToLandscapeRight.boolValue
                                || m_AllowedAutoRotateToLandscapeLeft.boolValue;

                            if (!somethingAllowed)
                            {
                                m_AllowedAutoRotateToPortrait.boolValue = true;
                                Debug.LogError("All orientations are disabled. Allowing portrait");
                            }

                            EditorGUILayout.PropertyField(m_AllowedAutoRotateToPortrait, SettingsContent.allowedAutoRotateToPortrait);
                            EditorGUILayout.PropertyField(m_AllowedAutoRotateToPortraitUpsideDown, SettingsContent.allowedAutoRotateToPortraitUpsideDown);
                            EditorGUILayout.PropertyField(m_AllowedAutoRotateToLandscapeRight, SettingsContent.allowedAutoRotateToLandscapeRight);
                            EditorGUILayout.PropertyField(m_AllowedAutoRotateToLandscapeLeft, SettingsContent.allowedAutoRotateToLandscapeLeft);

                            EditorGUI.indentLevel--;
                        }
                    }

                    if (namedBuildTarget == NamedBuildTarget.iOS)
                    {
                        GUILayout.Label(SettingsContent.multitaskingSupportTitle, EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(m_UIRequiresFullScreen, SettingsContent.UIRequiresFullScreen);
                        EditorGUILayout.Space();

                        GUILayout.Label(SettingsContent.statusBarTitle, EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(m_UIStatusBarHidden, SettingsContent.UIStatusBarHidden);
                        EditorGUILayout.PropertyField(m_UIStatusBarStyle, SettingsContent.UIStatusBarStyle);
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.Space();

                    // Standalone Player
                    if (namedBuildTarget == NamedBuildTarget.Standalone)
                    {
                        GUILayout.Label(SettingsContent.standalonePlayerOptionsTitle, EditorStyles.boldLabel);

                        EditorGUILayout.PropertyField(m_UsePlayerLog, SettingsContent.usePlayerLog);

                        EditorGUILayout.PropertyField(m_ResizableWindow, SettingsContent.resizableWindow);

                        EditorGUILayout.PropertyField(m_VisibleInBackground, SettingsContent.visibleInBackground);

                        EditorGUILayout.PropertyField(m_AllowFullscreenSwitch, SettingsContent.allowFullscreenSwitch);

                        EditorGUILayout.PropertyField(m_ForceSingleInstance, SettingsContent.forceSingleInstance);
                        EditorGUILayout.PropertyField(m_UseFlipModelSwapchain, SettingsContent.useFlipModelSwapChain);

                        if (!PlayerSettings.useFlipModelSwapchain)
                        {
                            EditorGUILayout.HelpBox(SettingsContent.flipModelSwapChainWarning.text, MessageType.Warning, true);
                        }

                        EditorGUILayout.Space();
                    }

                    // integrated gpu color/depth bits setup
                    if (BuildTargetDiscovery.PlatformGroupHasFlag(namedBuildTarget.ToBuildTargetGroup(), TargetAttributes.HasIntegratedGPU))
                    {
                        // iOS, while supports 16bit FB through GL interface, use 32bit in hardware, so there is no need in 16bit
                        if (namedBuildTarget != NamedBuildTarget.iOS &&
                            namedBuildTarget != NamedBuildTarget.tvOS)
                        {
                            EditorGUILayout.PropertyField(m_Use32BitDisplayBuffer, SettingsContent.use32BitDisplayBuffer);
                        }

                        EditorGUILayout.PropertyField(m_DisableDepthAndStencilBuffers, SettingsContent.disableDepthAndStencilBuffers);
                        EditorGUILayout.PropertyField(m_PreserveFramebufferAlpha, SettingsContent.preserveFramebufferAlpha);
                    }

                    ShowSharedNote();
                }
            }
            EndSettingsBox();
        }

        // Checks if the GraphicsDeviceType is deprecated
        static private bool IsGraphicsDeviceTypeDeprecated(BuildTarget target, GraphicsDeviceType graphicsDeviceType)
        {
            switch (graphicsDeviceType)
            {
                case GraphicsDeviceType.PlayStation5: return true;
                case GraphicsDeviceType.OpenGLCore: return target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64;
                default: return false;
            }
        }

        static private GraphicsDeviceType RecommendedGraphicsDeviceTypeFromDeprecated(BuildTarget target, GraphicsDeviceType graphicsDeviceType)
        {
            switch (graphicsDeviceType)
            {
                case GraphicsDeviceType.PlayStation5: return GraphicsDeviceType.PlayStation5NGGC;
                case GraphicsDeviceType.OpenGLCore: return target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64 ?
                        GraphicsDeviceType.Vulkan : graphicsDeviceType;
                default: return graphicsDeviceType;
            }
        }

        // Checks if the GraphicsDeviceType is experimental
        static private bool IsGraphicsDeviceTypeExperimental(BuildTarget target, GraphicsDeviceType graphicsDeviceType)
        {
            switch (graphicsDeviceType)
            {
                case GraphicsDeviceType.WebGPU: return true;
                default: return false;
            }
        }

        // Converts a GraphicsDeviceType to a string, along with visual modifiers for given target platform
        static private string GraphicsDeviceTypeToString(BuildTarget target, GraphicsDeviceType graphicsDeviceType)
        {
            if (graphicsDeviceType != GraphicsDeviceType.WebGPU && target == BuildTarget.WebGL)
            {
                return "WebGL 2";
            }

            if (IsGraphicsDeviceTypeDeprecated(target, graphicsDeviceType))
                return graphicsDeviceType.ToString() + " (Deprecated)";
            else if (IsGraphicsDeviceTypeExperimental(target, graphicsDeviceType))
            {
                return graphicsDeviceType.ToString() + " (Experimental)";
            }

            return graphicsDeviceType.ToString();
        }

        // Parses a GraphicsDeviceType from a string.
        static private GraphicsDeviceType GraphicsDeviceTypeFromString(string graphicsDeviceType)
        {
            graphicsDeviceType = graphicsDeviceType.Replace(" (Deprecated)", "");
            graphicsDeviceType = graphicsDeviceType.Replace(" (Experimental)", "");
            if (graphicsDeviceType == "WebGL 2") return GraphicsDeviceType.OpenGLES3;
            return (GraphicsDeviceType)Enum.Parse(typeof(GraphicsDeviceType), graphicsDeviceType, true);
        }

        private void AddGraphicsDeviceMenuSelected(object userData, string[] options, int selected)
        {
            var target = (BuildTarget)userData;
            var apis = m_CurrentTarget.GetGraphicsAPIs_Internal(target);
            if (apis == null)
                return;
            var apiToAdd = GraphicsDeviceTypeFromString(options[selected]);
            apis = apis.Append(apiToAdd).ToArray();
            m_CurrentTarget.SetGraphicsAPIs_Internal(target, apis, true);
            OnTargetObjectChangedDirectly();
        }

        private void AddGraphicsDeviceElement(BuildTarget target, Rect rect, ReorderableList list)
        {
            GraphicsDeviceType[] availableDevices = PlayerSettings.GetSupportedGraphicsAPIs(target);

            if (availableDevices == null || availableDevices.Length == 0)
                return;

            //As part of OpenGL deprection from MacOS, hide the option of adding OpenGL
            if (target == BuildTarget.StandaloneOSX)
            {
                var availableDeviceList = availableDevices.ToList();
                availableDeviceList.Remove(GraphicsDeviceType.OpenGLCore);
                availableDevices = availableDeviceList.ToArray();
            }

            var names = new string[availableDevices.Length];
            var enabled = new bool[availableDevices.Length];
            for (int i = 0; i < availableDevices.Length; ++i)
            {
                names[i] = L10n.Tr(GraphicsDeviceTypeToString(target, availableDevices[i]));
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
            var apis = m_CurrentTarget.GetGraphicsAPIs_Internal(target);
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
            var previousAPIs = m_CurrentTarget.GetGraphicsAPIs_Internal(target);
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
            bool doRestart = false;
            // If we're changing the first API for relevant editor, this will cause editor to switch: ask for scene save & confirmation
            if (firstEntryChanged && WillEditorUseFirstGraphicsAPI(target))
            {
                // If we have dirty scenes we need to save or discard changes before we restart editor.
                // Otherwise user will get a dialog later on where they can click cancel and put editor in a bad device state.
                var dirtyScenes = new List<Scene>();
                for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (scene.isDirty)
                        dirtyScenes.Add(scene);
                }
                if (dirtyScenes.Count != 0)
                {
                    var result = EditorUtility.DisplayDialogComplex("Changing editor graphics API",
                        "You've changed the active graphics API. This requires a restart of the Editor. Do you want to save the Scene when restarting?",
                        "Save and Restart", "Cancel Changing API", "Discard Changes and Restart");
                    if (result == 1)
                    {
                        doRestart = false; // Cancel was selected
                    }
                    else
                    {
                        doRestart = true;
                        if (result == 0) // Save and Restart was selected
                        {
                            for (int i = 0; i < dirtyScenes.Count; ++i)
                            {
                                var saved = EditorSceneManager.SaveScene(dirtyScenes[i]);
                                if (saved == false)
                                {
                                    doRestart = false;
                                }
                            }
                        }
                        else // Discard Changes and Restart was selected
                        {
                            for (int i = 0; i < dirtyScenes.Count; ++i)
                                EditorSceneManager.ClearSceneDirtiness(dirtyScenes[i]);
                        }
                    }
                }
                else
                {
                    doRestart = EditorUtility.DisplayDialog("Changing editor graphics API",
                        "You've changed the active graphics API. This requires a restart of the Editor.",
                        "Restart Editor", "Not now");
                }
                return new ChangeGraphicsApiAction(doRestart, doRestart);
            }
            else
            {
                return new ChangeGraphicsApiAction(true, false);
            }
        }

        private void ApplyChangeGraphicsApiAction(BuildTarget target, GraphicsDeviceType[] apis, ChangeGraphicsApiAction action)
        {
            if (action.changeList)
            {
                m_CurrentTarget.SetGraphicsAPIs_Internal(target, apis, true);
                OnTargetObjectChangedDirectly();
            }
            else
                m_GraphicsDeviceLists.Remove(target); // we cancelled the list change, so remove the cached one

            if (action.reloadGfx)
            {
                EditorApplication.RequestCloseAndRelaunchWithCurrentArguments();
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
            var name = GraphicsDeviceTypeToString(target, (GraphicsDeviceType)m_GraphicsDeviceLists[target].list[index]);

            GUI.Label(rect, name, EditorStyles.label);
        }

        private static bool WillEditorUseFirstGraphicsAPI(BuildTarget targetPlatform)
        {
            return
                Application.platform == RuntimePlatform.WindowsEditor && targetPlatform == BuildTarget.StandaloneWindows ||
                Application.platform == RuntimePlatform.LinuxEditor && targetPlatform == BuildTarget.StandaloneLinux64 ||
                Application.platform == RuntimePlatform.OSXEditor && targetPlatform == BuildTarget.StandaloneOSX;
        }

        private bool CheckApplyGraphicsJobsModeChange(BuildTarget target)
        {
            bool doRestart = false;

            if (WillEditorUseFirstGraphicsAPI(target))
            {
                // If we have dirty scenes we need to save or discard changes before we restart editor.
                // Otherwise user will get a dialog later on where they can click cancel and put editor in a bad device state.
                var dirtyScenes = new List<Scene>();
                for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (scene.isDirty)
                        dirtyScenes.Add(scene);
                }
                if (dirtyScenes.Count != 0)
                {
                    var result = EditorUtility.DisplayDialogComplex("Changing editor graphics jobs mode",
                        "You've changed the active graphics jobs mode. This requires a restart of the Editor. Do you want to save the Scene when restarting?",
                        "Save and Restart", "Cancel Changing API", "Discard Changes and Restart");
                    if (result == 1)
                    {
                        doRestart = false; // Cancel was selected
                    }
                    else
                    {
                        doRestart = true;
                        if (result == 0) // Save and Restart was selected
                        {
                            for (int i = 0; i < dirtyScenes.Count; ++i)
                            {
                                var saved = EditorSceneManager.SaveScene(dirtyScenes[i]);
                                if (saved == false)
                                {
                                    doRestart = false;
                                }
                            }
                        }
                        else // Discard Changes and Restart was selected
                        {
                            for (int i = 0; i < dirtyScenes.Count; ++i)
                                EditorSceneManager.ClearSceneDirtiness(dirtyScenes[i]);
                        }
                    }
                }
                else
                {
                    doRestart = EditorUtility.DisplayDialog("Changing editor graphics jobs mode",
                        "You've changed the active graphics jobs mode. This requires a restart of the Editor.",
                        "Restart Editor", "Not now");
                }
            }
            return doRestart;
        }

        void OpenGLES31OptionsGUI(BuildTargetGroup targetGroup, BuildTarget targetPlatform)
        {
            // ES3.1 options only applicable on some platforms now
            var hasES31Options = (targetGroup == BuildTargetGroup.Android);
            if (!hasES31Options)
                return;

            var apis = m_CurrentTarget.GetGraphicsAPIs_Internal(targetPlatform);
            // only available if we include ES3
            var hasMinES3 = apis.Contains(GraphicsDeviceType.OpenGLES3);
            if (!hasMinES3)
                return;

            EditorGUILayout.PropertyField(m_RequireES31, SettingsContent.require31);
            EditorGUILayout.PropertyField(m_RequireES31AEP, SettingsContent.requireAEP);
            EditorGUILayout.PropertyField(m_RequireES32, SettingsContent.require32);
        }

        void ExclusiveGraphicsAPIsGUI(BuildTarget targetPlatform, string displayTitle)
        {
            EditorGUI.BeginChangeCheck();
            GraphicsDeviceType[] currentDevices = m_CurrentTarget.GetGraphicsAPIs_Internal(targetPlatform);
            GraphicsDeviceType[] availableDevices = PlayerSettings.GetSupportedGraphicsAPIs(targetPlatform);

            GUIContent[] names = new GUIContent[availableDevices.Length];
            for (int i = 0; i < availableDevices.Length; ++i)
            {
                names[i] = EditorGUIUtility.TrTextContent(L10n.Tr(GraphicsDeviceTypeToString(targetPlatform, availableDevices[i])));
            }

            GraphicsDeviceType selected = BuildEnumPopup(EditorGUIUtility.TrTextContent(displayTitle), currentDevices[0], availableDevices, names);
            if (EditorGUI.EndChangeCheck() && selected != currentDevices[0])
            {
                Undo.RecordObject(target, SettingsContent.undoChangedGraphicsAPIString);
                m_CurrentTarget.SetGraphicsAPIs_Internal(targetPlatform, new GraphicsDeviceType[] { selected }, true);
                OnTargetObjectChangedDirectly();
            }

            if (IsGraphicsDeviceTypeDeprecated(targetPlatform, selected))
            {
                GraphicsDeviceType recommendedAPI = RecommendedGraphicsDeviceTypeFromDeprecated(targetPlatform, selected);
                string text = $"The Graphics API has been deprecated and will be removed in a future version. Use {GraphicsDeviceTypeToString(targetPlatform, recommendedAPI)} instead.";
                EditorGUILayout.HelpBox(L10n.Tr(text), MessageType.Info, true);
            }
        }

        void GraphicsAPIsGUIOnePlatform(BuildTargetGroup targetGroup, BuildTarget targetPlatform, GUIContent platformTitleContent)
        {
            if (IsPreset())
                return;

            GraphicsDeviceType[] availableDevices = PlayerSettings.GetSupportedGraphicsAPIs(targetPlatform);
            // if no devices (e.g. no platform module), or we only have one possible choice, then no
            // point in having any UI
            if (availableDevices == null || availableDevices.Length < 2)
                return;

            // toggle for automatic API selection
            EditorGUI.BeginChangeCheck();
            var automatic = m_CurrentTarget.GetUseDefaultGraphicsAPIs_Internal(targetPlatform);
            automatic = EditorGUILayout.Toggle(platformTitleContent ?? GUIContent.none, automatic);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, SettingsContent.undoChangedGraphicsAPIString);
                m_CurrentTarget.SetUseDefaultGraphicsAPIs_Internal(targetPlatform, automatic);
                OnTargetObjectChangedDirectly();
            }

            // graphics API list if not automatic
            if (!automatic)
            {
                // note that editor will use first item, when we're in standalone settings
                if (WillEditorUseFirstGraphicsAPI(targetPlatform))
                {
                    EditorGUILayout.HelpBox(SettingsContent.recordingInfo.text, MessageType.Info, true);
                }

                string displayTitle = String.Empty;
                if (platformTitleContent != null)
                {
                    displayTitle = platformTitleContent.text;
                    if (displayTitle.StartsWith("Auto "))
                        displayTitle = displayTitle.Substring(5);
                }

                if (targetPlatform == BuildTarget.PS5)
                {
                    ExclusiveGraphicsAPIsGUI(targetPlatform, displayTitle);
                    return;
                }

                GraphicsDeviceType[] devices = m_CurrentTarget.GetGraphicsAPIs_Internal(targetPlatform);
                var devicesList = (devices != null) ? devices.ToList() : new List<GraphicsDeviceType>();
                // create reorderable list for this target if needed
                if (!m_GraphicsDeviceLists.ContainsKey(targetPlatform))
                {
                    var rlist = new ReorderableList(devicesList, typeof(GraphicsDeviceType), true, true, true, true);
                    rlist.onAddDropdownCallback = (rect, list) => AddGraphicsDeviceElement(targetPlatform, rect, list);
                    rlist.onCanRemoveCallback = CanRemoveGraphicsDeviceElement;
                    rlist.onRemoveCallback = (list) => RemoveGraphicsDeviceElement(targetPlatform, list);
                    rlist.onReorderCallback = (list) => ReorderGraphicsDeviceElement(targetPlatform, list);
                    rlist.drawElementCallback = (rect, index, isActive, isFocused) => DrawGraphicsDeviceElement(targetPlatform, rect, index, isActive, isFocused);
                    rlist.drawHeaderCallback = (rect) => GUI.Label(rect, displayTitle, EditorStyles.label);
                    rlist.elementHeight = 16;

                    m_GraphicsDeviceLists.Add(targetPlatform, rlist);
                }

                if (targetPlatform == BuildTarget.StandaloneOSX && m_GraphicsDeviceLists[BuildTarget.StandaloneOSX].list.Contains(GraphicsDeviceType.OpenGLCore))
                {
                    EditorGUILayout.HelpBox(SettingsContent.appleSiliconOpenGLWarning.text, MessageType.Warning, true);
                }

                m_GraphicsDeviceLists[targetPlatform].DoLayoutList();

                bool containsDeprecatedAPIs = devicesList.Exists(device => IsGraphicsDeviceTypeDeprecated(targetPlatform, device));
                if (containsDeprecatedAPIs)
                    EditorGUILayout.HelpBox(SettingsContent.graphicsAPIDeprecationMessage.text, MessageType.Info, true);

                //@TODO: undo
            }

            // ES3.1 options
            OpenGLES31OptionsGUI(targetGroup, targetPlatform);
        }

        void GraphicsAPIsGUI(BuildTargetGroup targetGroup, BuildTarget target)
        {
            // "standalone" is a generic group;
            // split it into win/mac/linux manually
            if (targetGroup == BuildTargetGroup.Standalone)
            {
                GraphicsAPIsGUIOnePlatform(targetGroup, BuildTarget.StandaloneWindows, SettingsContent.autoGraphicsAPIForWindows);
                GraphicsAPIsGUIOnePlatform(targetGroup, BuildTarget.StandaloneOSX, SettingsContent.autoGraphicsAPIForMac);
                GraphicsAPIsGUIOnePlatform(targetGroup, BuildTarget.StandaloneLinux64, SettingsContent.autoGraphicsAPIForLinux);
            }
            else
            {
                GraphicsAPIsGUIOnePlatform(targetGroup, target, SettingsContent.autoGraphicsAPI);
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
            { BuildTargetGroup.Standalone, new List<ColorGamut> { ColorGamut.sRGB, ColorGamut.DisplayP3 } },
            { BuildTargetGroup.iOS, new List<ColorGamut> { ColorGamut.sRGB, ColorGamut.DisplayP3 } },
            { BuildTargetGroup.tvOS, new List<ColorGamut> { ColorGamut.sRGB, ColorGamut.DisplayP3 } },
            { BuildTargetGroup.VisionOS, new List<ColorGamut> { ColorGamut.sRGB, ColorGamut.DisplayP3 } },
            { BuildTargetGroup.Android, new List<ColorGamut> {ColorGamut.sRGB, ColorGamut.DisplayP3 } }
        };

        private static bool IsColorGamutSupportedOnTargetGroup(BuildTargetGroup targetGroup, ColorGamut gamut)
        {
            if (gamut == ColorGamut.sRGB)
                return true;
            if (s_SupportedColorGamuts.ContainsKey(targetGroup) && s_SupportedColorGamuts[targetGroup].Contains(gamut))
                return true;
            return false;
        }

        private static string GetColorGamutDisplayString(BuildTargetGroup targetGroup, ColorGamut gamut)
        {
            string name = gamut.ToString();
            if (!IsColorGamutSupportedOnTargetGroup(targetGroup, gamut))
                name += L10n.Tr(" (not supported on this platform)");
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
            var colorGamutList = m_CurrentTarget.GetColorGamuts_Internal().ToList();
            colorGamutList.Add(colorGamuts[selected]);
            m_CurrentTarget.SetColorGamuts_Internal(colorGamutList.ToArray());
            OnTargetObjectChangedDirectly();
            SyncColorGamuts();
        }

        private bool CanRemoveColorGamutElement(ReorderableList list)
        {
            // don't allow removing the sRGB
            var colorGamutList = (List<ColorGamut>)list.list;
            return colorGamutList[list.index] != ColorGamut.sRGB;
        }

        private void RemoveColorGamutElement(ReorderableList list)
        {
            var colorGamutList = m_CurrentTarget.GetColorGamuts_Internal().ToList();
            // don't allow removing the last ColorGamut
            if (colorGamutList.Count < 2)
            {
                EditorApplication.Beep();
                return;
            }
            colorGamutList.RemoveAt(list.index);
            m_CurrentTarget.SetColorGamuts_Internal(colorGamutList.ToArray());
            OnTargetObjectChangedDirectly();
            SyncColorGamuts();
        }

        private void ReorderColorGamutElement(ReorderableList list)
        {
            var colorGamutList = (List<ColorGamut>)list.list;
            m_CurrentTarget.SetColorGamuts_Internal(colorGamutList.ToArray());
            OnTargetObjectChangedDirectly();
            SyncColorGamuts();
        }

        private void DrawColorGamutElement(BuildTargetGroup targetGroup, Rect rect, int index, bool selected, bool focused)
        {
            var colorGamut = m_ColorGamutList.list[index];
            GUI.Label(rect, GetColorGamutDisplayString(targetGroup, (ColorGamut)colorGamut), EditorStyles.label);
        }

        void ColorGamutGUI(BuildPlatform platform)
        {
            BuildTargetGroup targetGroup = platform.namedBuildTarget.ToBuildTargetGroup();

            if (IsPreset())
                return;
            if (!s_SupportedColorGamuts.ContainsKey(targetGroup))
                return;

            // Color gamut is not supported for other standalones besides Mac
            if (!(BuildTargetDiscovery.TryGetProperties(platform.defaultTarget, out IGraphicsPlatformProperties properties) &&
               (properties?.SupportsColorGamut ?? false)))
                return;

            if (m_ColorGamutList == null)
            {
                ColorGamut[] colorGamuts = m_CurrentTarget.GetColorGamuts_Internal();
                var colorGamutsList = (colorGamuts != null) ? colorGamuts.ToList() : new List<ColorGamut>();
                var rlist = new ReorderableList(colorGamutsList, typeof(ColorGamut), true, true, true, true);
                rlist.onCanRemoveCallback = CanRemoveColorGamutElement;
                rlist.onRemoveCallback = RemoveColorGamutElement;
                rlist.onReorderCallback = ReorderColorGamutElement;
                rlist.elementHeight = 16;

                m_ColorGamutList = rlist;
            }

            // On standalone inspector mention that the setting applies only to Mac
            // (Temporarily until other standalones support this setting)
            GUIContent header = targetGroup == BuildTargetGroup.Standalone ? SettingsContent.colorGamutForMac : SettingsContent.colorGamut;
            m_ColorGamutList.drawHeaderCallback = (rect) =>
                GUI.Label(rect, header, EditorStyles.label);

            // we want to change the displayed text per platform, to indicate unsupported gamuts
            m_ColorGamutList.onAddDropdownCallback = (rect, list) =>
                AddColorGamutElement(targetGroup, rect, list);

            m_ColorGamutList.drawElementCallback = (rect, index, selected, focused) =>
                DrawColorGamutElement(targetGroup, rect, index, selected, focused);

            m_ColorGamutList.DoLayoutList();
        }

        public void DebugAndCrashReportingGUI(BuildPlatform platform,
            ISettingEditorExtension settingsExtension, int sectionIndex = 3)
        {
            if (platform.namedBuildTarget != NamedBuildTarget.iOS && platform.namedBuildTarget != NamedBuildTarget.tvOS)
                return;

            if (BeginSettingsBox(sectionIndex, SettingsContent.debuggingCrashReportingTitle))
            {
                // PLEASE DO NOT COPY SETTINGS TO APPEAR MULTIPLE PLACES IN THE CODE! See top of file for more info.
                {
                    // Debugging
                    GUILayout.Label(SettingsContent.debuggingTitle, EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(m_EnableInternalProfiler, SettingsContent.enableInternalProfiler);
                    EditorGUILayout.Space();
                }

                {
                    // Crash reporting
                    GUILayout.Label(SettingsContent.crashReportingTitle, EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(m_ActionOnDotNetUnhandledException, SettingsContent.actionOnDotNetUnhandledException);
                    EditorGUILayout.PropertyField(m_LogObjCUncaughtExceptions, SettingsContent.logObjCUncaughtExceptions);

                    GUIContent crashReportApiContent = SettingsContent.enableCrashReportAPI;

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
                EditorGUI.Popup(EditorGUILayout.GetControlRect(true), uiString, 0, new GUIContent[] { selected });
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

        public static void EnumPropertyField<T>(SerializedProperty property, GUIContent name) where T : Enum
        {
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.PropertyScope(horizontal.rect, GUIContent.none, property))
                {
                    var values = (T[])Enum.GetValues(typeof(T));
                    var valueNames = Enum.GetNames(typeof(T)).Select(e => new GUIContent(e)).ToArray();
                    PlayerSettingsEditor.BuildEnumPopup(property, name, values, valueNames);
                }
            }
        }

        public void OtherSectionGUI(BuildPlatform platform, ISettingEditorExtension settingsExtension, int sectionIndex = 4)
        {
            if (BeginSettingsBox(sectionIndex, SettingsContent.otherSettingsTitle))
            {
                // PLEASE DO NOT COPY SETTINGS TO APPEAR MULTIPLE PLACES IN THE CODE! See top of file for more info.
                if (platform.namedBuildTarget != NamedBuildTarget.Server)
                {
                    OtherSectionRenderingGUI(platform, settingsExtension);
                    OtherSectionVulkanSettingsGUI(platform, settingsExtension);
                    OtherSectionIdentificationGUI(platform, settingsExtension);
                }
                OtherSectionConfigurationGUI(platform, settingsExtension);
                OtherSectionShaderSettingsGUI(platform);
                OtherSectionScriptCompilationGUI(platform);
                OtherSectionOptimizationGUI(platform);
                OtherSectionLoggingGUI();
                OtherSectionLegacyGUI(platform);
                if (platform.namedBuildTarget == NamedBuildTarget.Standalone || platform.namedBuildTarget == NamedBuildTarget.Server)
                {
                    OtherSectionCaptureLogsGUI(platform.namedBuildTarget);
                }
                ShowSharedNote();
            }
            EndSettingsBox();
        }

        public void PlayerSettingsAttributeSectionsGUI(NamedBuildTarget namedBuildTarget, ISettingEditorExtension settingsExtension, ref int sectionIndex)
        {
            foreach (var box in m_boxes)
            {
                if (box.TargetName == namedBuildTarget.TargetName)
                {
                    if (BeginSettingsBox(sectionIndex, box.title))
                    {
                        box.mi.Invoke(null, null);
                    }
                    EndSettingsBox();
                    sectionIndex++;
                }
            }
        }

        private void OtherSectionShaderSettingsGUI(BuildPlatform platform)
        {
            GUILayout.Label(SettingsContent.shaderSectionTitle, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || EditorApplication.isCompiling))
            {
                EditorGUI.BeginChangeCheck();
                ShaderPrecisionModel currShaderPrecisionModel = (ShaderPrecisionModel) m_ShaderPrecisionModel.intValue;
                ShaderPrecisionModel[] shaderPrecisionModelValues = { ShaderPrecisionModel.PlatformDefault, ShaderPrecisionModel.Unified };
                ShaderPrecisionModel newShaderPrecisionModel = BuildEnumPopup(
                    SettingsContent.shaderPrecisionModel, currShaderPrecisionModel, shaderPrecisionModelValues,
                    SettingsContent.shaderPrecisionModelOptions);
                if (EditorGUI.EndChangeCheck() && currShaderPrecisionModel != newShaderPrecisionModel)
                {
                    m_ShaderPrecisionModel.intValue = (int) newShaderPrecisionModel;
                    serializedObject.ApplyModifiedProperties();
                    if (IsActivePlayerSettingsEditor())
                        PlayerSettings.SyncShaderPrecisionModel();
                }
            }

            EditorGUILayout.PropertyField(m_StrictShaderVariantMatching, SettingsContent.strictShaderVariantMatching);

            EditorGUILayout.PropertyField(m_KeepLoadedShadersAlive, SettingsContent.keepLoadedShadersAlive);

            GUILayout.Label(SettingsContent.shaderVariantLoadingTitle, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int defaultChunkSize = PlayerSettings.GetDefaultShaderChunkSizeInMB_Internal(m_CurrentTarget);
            int newDefaultChunkSize = EditorGUILayout.IntField(SettingsContent.defaultShaderChunkSize, defaultChunkSize);
            if (EditorGUI.EndChangeCheck() && newDefaultChunkSize > 0 && newDefaultChunkSize != defaultChunkSize)
            {
                Undo.RecordObject(target, SettingsContent.undoChangedDefaultShaderChunkSizeString);
                PlayerSettings.SetDefaultShaderChunkSizeInMB_Internal(m_CurrentTarget, newDefaultChunkSize);
                m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);
            }

            EditorGUI.BeginChangeCheck();
            int defaultChunkCount = PlayerSettings.GetDefaultShaderChunkCount_Internal(m_CurrentTarget);
            int newDefaultChunkCount = EditorGUILayout.IntField(SettingsContent.defaultShaderChunkCount, defaultChunkCount);
            if (EditorGUI.EndChangeCheck() && newDefaultChunkCount >= 0 && newDefaultChunkCount != defaultChunkCount)
            {
                Undo.RecordObject(target, SettingsContent.undoChangedDefaultShaderChunkCountString);
                PlayerSettings.SetDefaultShaderChunkCount_Internal(m_CurrentTarget, newDefaultChunkCount);
                m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);
            }

            bool oldOverride = PlayerSettings.GetOverrideShaderChunkSettingsForPlatform_Internal(m_CurrentTarget, platform.defaultTarget);
            bool newOverride = EditorGUILayout.Toggle(SettingsContent.overrideDefaultChunkSettings, oldOverride);
            if (oldOverride != newOverride)
            {
                PlayerSettings.SetOverrideShaderChunkSettingsForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, newOverride);
                m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);
            }

            if (newOverride)
            {
                int currentChunkSize = PlayerSettings.GetShaderChunkSizeInMBForPlatform_Internal(m_CurrentTarget, platform.defaultTarget);
                int newChunkSize = EditorGUILayout.IntField(SettingsContent.platformShaderChunkSize, currentChunkSize);
                if (EditorGUI.EndChangeCheck() && newChunkSize > 0 && newChunkSize != currentChunkSize)
                {
                    Undo.RecordObject(target, SettingsContent.undoChangedPlatformShaderChunkSizeString);
                    PlayerSettings.SetShaderChunkSizeInMBForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, newChunkSize);
                    m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);
                }

                EditorGUI.BeginChangeCheck();
                int currentChunkCount = PlayerSettings.GetShaderChunkCountForPlatform_Internal(m_CurrentTarget, platform.defaultTarget);
                int newChunkCount = EditorGUILayout.IntField(SettingsContent.platformShaderChunkCount, currentChunkCount);
                if (EditorGUI.EndChangeCheck() && newChunkCount >= 0 && newChunkCount != currentChunkCount)
                {
                    Undo.RecordObject(target, SettingsContent.undoChangedPlatformShaderChunkCountString);
                    PlayerSettings.SetShaderChunkCountForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, newChunkCount);
                    m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);
                }
            }
        }

        private void OtherSectionRenderingGUI(BuildPlatform platform, ISettingEditorExtension settingsExtension)
        {
            // Rendering related settings
            GUILayout.Label(SettingsContent.renderingTitle, EditorStyles.boldLabel);

            // Color space (supported by all non deprecated platforms)
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying)) // switching color spaces in play mode is not supported
            {
                EditorGUI.BeginChangeCheck();
                int selectedValue = m_ActiveColorSpace.enumValueIndex;
                EditorGUILayout.PropertyField(m_ActiveColorSpace, SettingsContent.activeColorSpace);

                if (EditorGUI.EndChangeCheck() && IsActivePlayerSettingsEditor())
                {
                    if (m_ActiveColorSpace.enumValueIndex != selectedValue && EditorUtility.DisplayDialog("Changing Color Space", SettingsContent.changeColorSpaceString, $"Change to {(ColorSpace)m_ActiveColorSpace.enumValueIndex}", "Cancel"))
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                    else m_ActiveColorSpace.enumValueIndex = selectedValue;
                    GUIUtility.ExitGUI(); // Fixes case 690421
                }
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (BuildTargetDiscovery.TryGetBuildTarget(platform.defaultTarget, out IBuildTarget iBuildTarget) && (iBuildTarget.GraphicsPlatformProperties?.HasUnsupportedMSAAFallback ?? false))
                    EditorGUILayout.PropertyField(m_UnsupportedMSAAFallback, SettingsContent.unsupportedMSAAFallback);
            }

            // Special cases for some platform with limitations regarding linear colorspace
            if ((PlayerSettings.colorSpace == ColorSpace.Linear) &&
                (null != settingsExtension) && settingsExtension.SupportsForcedSrgbBlit())
            {
                EditorGUILayout.PropertyField(m_ForceSRGBBlit, SettingsContent.forceSRGBBlit);
            }

            // Graphics APIs
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                GraphicsAPIsGUI(platform.namedBuildTarget.ToBuildTargetGroup(), platform.defaultTarget);
            }

            // Output color spaces
            ColorGamutGUI(platform);

            // What we call "Metal Validation" is a random bunch of extra checks we do in editor in metal code
            if (Application.platform == RuntimePlatform.OSXEditor && BuildTargetDiscovery.BuildTargetSupportsRenderer(platform, GraphicsDeviceType.Metal))
                m_MetalAPIValidation.boolValue = EditorGUILayout.Toggle(SettingsContent.metalAPIValidation, m_MetalAPIValidation.boolValue);

            // Metal
            if (BuildTargetDiscovery.BuildTargetSupportsRenderer(platform, GraphicsDeviceType.Metal))
            {
                EditorGUILayout.PropertyField(m_MetalFramebufferOnly, SettingsContent.metalFramebufferOnly);
                if (platform.namedBuildTarget == NamedBuildTarget.iOS || platform.namedBuildTarget == NamedBuildTarget.tvOS)
                    EditorGUILayout.PropertyField(m_MetalForceHardShadows, SettingsContent.metalForceHardShadows);

                int[] memorylessModeValues = { 0, 1, 2 };
                BuildEnumPopup(m_FramebufferDepthMemorylessMode, SettingsContent.framebufferDepthMemorylessMode, memorylessModeValues, SettingsContent.memorylessModeNames);
            }

            if (!IsPreset())
            {
                // Multithreaded rendering
                if (settingsExtension != null && settingsExtension.SupportsMultithreadedRendering())
                    settingsExtension.MultithreadedRenderingGUI(platform.namedBuildTarget);

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
                    PlayerSettings.GetBatchingForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, out staticBatching, out dynamicBatching);

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
                        PlayerSettings.SetBatchingForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, staticBatching, dynamicBatching);
                        m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);
                    }

                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUI.DisabledScope(!staticBatchingSupported))
                    {
                        if (GUI.enabled)
                            staticBatching = EditorGUILayout.Toggle(SettingsContent.staticBatching, staticBatching != 0) ? 1 : 0;
                        else
                            EditorGUILayout.Toggle(SettingsContent.staticBatching, false);
                    }

                    if (GraphicsSettings.currentRenderPipeline == null)
                    {
                        using (new EditorGUI.DisabledScope(!dynamicBatchingSupported))
                        {
                            dynamicBatching = EditorGUILayout.Toggle(SettingsContent.dynamicBatching, dynamicBatching != 0) ? 1 : 0;
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, SettingsContent.undoChangedBatchingString);
                        PlayerSettings.SetBatchingForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, staticBatching, dynamicBatching);
                        m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);
                    }
                }

                EditorGUILayout.IntSlider(m_SpriteBatchVertexThreshold, 300, 8000, SettingsContent.spriteBatchingVertexThreshold);
                EditorGUILayout.IntSlider(m_SpriteBatchMaxVertexCount, 1024, 65535, SettingsContent.spriteBatchingMaxVertexCount);
            }

            bool hdrDisplaySupported = false;
            bool gfxJobModesSupported = false;
            bool hdrEncodingSupportedByPlatform = (platform.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone || platform.namedBuildTarget == NamedBuildTarget.WebGL);
            if (settingsExtension != null)
            {
                hdrDisplaySupported = settingsExtension.SupportsHighDynamicRangeDisplays();
                gfxJobModesSupported = settingsExtension.SupportsGfxJobModes();
                hdrEncodingSupportedByPlatform = hdrEncodingSupportedByPlatform || settingsExtension.SupportsCustomLightmapEncoding();
            }
            else
            {
                if (platform.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone)
                {
                    GraphicsDeviceType[] gfxAPIs = m_CurrentTarget.GetGraphicsAPIs_Internal(platform.defaultTarget);

                    hdrDisplaySupported = gfxAPIs[0] == GraphicsDeviceType.Direct3D11 || gfxAPIs[0] == GraphicsDeviceType.Direct3D12 || gfxAPIs[0] == GraphicsDeviceType.Vulkan || gfxAPIs[0] == GraphicsDeviceType.Metal;
                }
            }

            if (platform.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone)
            {
                GraphicsDeviceType[] gfxAPIs = m_CurrentTarget.GetGraphicsAPIs_Internal(platform.defaultTarget);
                gfxJobModesSupported = (gfxAPIs[0] == GraphicsDeviceType.Direct3D12) || (gfxAPIs[0] == GraphicsDeviceType.Vulkan);
            }
            else if (platform.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Android)
            {
                GraphicsDeviceType[] gfxAPIs = m_CurrentTarget.GetGraphicsAPIs_Internal(platform.defaultTarget);
                gfxJobModesSupported = (gfxAPIs[0] == GraphicsDeviceType.Vulkan);
            }

            // GPU Skinning toggle (only show on relevant platforms)
            if (!BuildTargetDiscovery.PlatformHasFlag(platform.defaultTarget, TargetAttributes.GPUSkinningNotSupported))
            {
                bool platformSupportsBatching = false;

                GraphicsDeviceType[] gfxAPIs = m_CurrentTarget.GetGraphicsAPIs_Internal(platform.defaultTarget);
                foreach (GraphicsDeviceType api in gfxAPIs)
                {
                    if (api == GraphicsDeviceType.Switch ||
                        api == GraphicsDeviceType.PlayStation5 ||
                        api == GraphicsDeviceType.PlayStation5NGGC ||
                        api == GraphicsDeviceType.Direct3D11 ||
                        api == GraphicsDeviceType.Metal ||
                        api == GraphicsDeviceType.Vulkan ||
                        api == GraphicsDeviceType.OpenGLES3 ||
                        api == GraphicsDeviceType.Direct3D12)
                    {
                        platformSupportsBatching = true;
                        break;
                    }
                    // TODO: GraphicsDeviceType.OpenGLCore does not have GPU skinning enabled yet
                }


                if (platformSupportsBatching)
                {
                    MeshDeformation currentDeformation = (MeshDeformation)m_MeshDeformation.intValue;

                    EditorGUI.BeginChangeCheck();
                    MeshDeformation newDeformation = BuildEnumPopup(SettingsContent.skinOnGPU, currentDeformation, m_MeshDeformations, SettingsContent.meshDeformations);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SkinOnGPU.boolValue = newDeformation != MeshDeformation.CPU;
                        m_MeshDeformation.intValue = (int)newDeformation;
                        serializedObject.ApplyModifiedProperties();
                        ShaderUtil.RecreateSkinnedMeshResources();
                    }
                }
                else
                {
                    // Use the original checkbox UI but preserve underlying batching mode whenever possible.
                    // We need to do this because gpuSkinning/meshDeformation are properties which are shared between all platforms
                    // and if the user sets gpuSkinning mode to "enabled", we actually want to preserve "batchEnabled" if it was set for other platforms.
                    // Platforms that do not support batching but have meshDeformation == GPUBatched just silently use original non-batched code.

                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(m_SkinOnGPU, SettingsContent.skinOnGPU);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // Preserve the value of m_MeshDeformation when possible.
                            if (!m_SkinOnGPU.boolValue)
                                m_MeshDeformation.intValue = (int)MeshDeformation.CPU;
                            else
                                m_MeshDeformation.intValue = m_MeshDeformation.intValue != (int)MeshDeformation.CPU ? m_MeshDeformation.intValue : (int)MeshDeformation.GPUBatched;
                            serializedObject.ApplyModifiedProperties();
                            ShaderUtil.RecreateSkinnedMeshResources();
                        }
                    }
                }
            }

            bool graphicsJobsOptionEnabled = true;
            bool graphicsJobs = PlayerSettings.GetGraphicsJobsForPlatform_Internal(m_CurrentTarget,
                platform.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone ? EditorUserBuildSettings.selectedStandaloneTarget : platform.defaultTarget);
            bool newGraphicsJobs = graphicsJobs;

            if (platform.namedBuildTarget == NamedBuildTarget.XboxOne)
            {
                // on XBoxOne, we only have kGfxJobModeNative active for DX12 API and kGfxJobModeLegacy for the DX11 API
                // no need for a drop down popup for XBoxOne
                // also if XboxOneD3D12 is selected as GraphicsAPI, then we want to set graphics jobs and disable the user option
                GraphicsDeviceType[] gfxAPIs = m_CurrentTarget.GetGraphicsAPIs_Internal(platform.defaultTarget);
                GraphicsJobMode newGfxJobMode = GraphicsJobMode.Legacy;
                if (gfxAPIs[0] == GraphicsDeviceType.XboxOneD3D12)
                {
                    newGfxJobMode = GraphicsJobMode.Split;
                    if (graphicsJobs == false)
                    {
                        PlayerSettings.SetGraphicsJobsForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, true);
                        graphicsJobs = true;
                        newGraphicsJobs = true;
                    }
                }
                PlayerSettings.SetGraphicsJobModeForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, newGfxJobMode);
                PlayerSettings.SetGraphicsThreadingModeForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, GfxThreadingMode.SplitJobs);
                OnTargetObjectChangedDirectly();
            }
            else if (platform.namedBuildTarget == NamedBuildTarget.PS5)
            {
                // On PS5NGGC, we always have graphics jobs enabled so we disable the option in that case
                GraphicsDeviceType[] gfxAPIs = m_CurrentTarget.GetGraphicsAPIs_Internal(platform.defaultTarget);
                if (gfxAPIs[0] == GraphicsDeviceType.PlayStation5NGGC)
                {
                    graphicsJobsOptionEnabled = false;
                    if (graphicsJobs == false)
                    {
                        PlayerSettings.SetGraphicsJobsForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, true);
                        OnTargetObjectChangedDirectly();
                        graphicsJobs = true;
                        newGraphicsJobs = true;
                    }
                }
            }

            if (!IsPreset())
            {
                EditorGUI.BeginChangeCheck();
                GUIContent graphicsJobsGUI = SettingsContent.graphicsJobsNonExperimental;

                if (BuildTargetDiscovery.TryGetBuildTarget(platform.defaultTarget, out IBuildTarget iBuildTarget) && (iBuildTarget.GraphicsPlatformProperties?.AreGraphicsJobsExperimental ?? false))
                    graphicsJobsGUI = SettingsContent.graphicsJobsExperimental;

                using (new EditorGUI.DisabledScope(!graphicsJobsOptionEnabled))
                {
                    if (GUI.enabled)
                    {
                        newGraphicsJobs = EditorGUILayout.Toggle(graphicsJobsGUI, graphicsJobs);
                    }
                    else
                    {
                        EditorGUILayout.Toggle(graphicsJobsGUI, graphicsJobs);
                    }
                }
                if (EditorGUI.EndChangeCheck() && (newGraphicsJobs != graphicsJobs))
                {
                    Undo.RecordObject(target, SettingsContent.undoChangedGraphicsJobsString);
                    PlayerSettings.SetGraphicsJobsForPlatform_Internal(m_CurrentTarget,
                        platform.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone ? EditorUserBuildSettings.selectedStandaloneTarget : platform.defaultTarget, newGraphicsJobs);

                    OnTargetObjectChangedDirectly();

                    if (IsActivePlayerSettingsEditor() && CheckApplyGraphicsJobsModeChange(platform.defaultTarget))
                    {
                        EditorApplication.RequestCloseAndRelaunchWithCurrentArguments();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            if (gfxJobModesSupported && newGraphicsJobs)
            {
                // For a platform extension to support a gfx job mode, it means it wouldn't modify it. So we check if it's the same after adjustments.
                var checkGfxJobModeSupport = (Enum value) => { return settingsExtension != null ? settingsExtension.AdjustGfxJobMode((GraphicsJobMode)value) == (GraphicsJobMode)value : true; };

                EditorGUI.BeginChangeCheck();
                GraphicsJobMode currGfxJobMode = PlayerSettings.GetGraphicsJobModeForPlatform_Internal(m_CurrentTarget, platform.defaultTarget);
                GraphicsJobMode newGfxJobMode = (GraphicsJobMode)EditorGUILayout.EnumPopup(SettingsContent.graphicsJobsMode, currGfxJobMode, checkGfxJobModeSupport, false);

                if (EditorGUI.EndChangeCheck() && (newGfxJobMode != currGfxJobMode))
                {
                    Undo.RecordObject(target, SettingsContent.undoChangedGraphicsJobModeString);
                }

                GraphicsJobMode fallbackGfxJobMode = settingsExtension != null ? settingsExtension.AdjustGfxJobMode(currGfxJobMode) : currGfxJobMode;
                // If we changed other settings and the selected gfx job mode is suddently not supported, we fallback to what the platform settings extension wants
                if (fallbackGfxJobMode != currGfxJobMode)
                {
                    newGfxJobMode = fallbackGfxJobMode;
                }

                // Finally we apply the change of gfx job mode
                if (newGfxJobMode != currGfxJobMode)
                {
                    PlayerSettings.SetGraphicsJobModeForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, newGfxJobMode);

                    if(newGfxJobMode == GraphicsJobMode.Native)
                        PlayerSettings.SetGraphicsThreadingModeForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, GfxThreadingMode.ClientWorkerNativeJobs);
                    else if (newGfxJobMode == GraphicsJobMode.Legacy)
                        PlayerSettings.SetGraphicsThreadingModeForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, GfxThreadingMode.ClientWorkerJobs);
                    else if (newGfxJobMode == GraphicsJobMode.Split)
                        PlayerSettings.SetGraphicsThreadingModeForPlatform_Internal(m_CurrentTarget, platform.defaultTarget, GfxThreadingMode.SplitJobs);

                    OnTargetObjectChangedDirectly();
                    if (IsActivePlayerSettingsEditor() && CheckApplyGraphicsJobsModeChange(platform.defaultTarget))
                    {
                        EditorApplication.RequestCloseAndRelaunchWithCurrentArguments();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (settingsExtension != null)
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    settingsExtension.RenderingSectionGUI();
                }
            }

            if ((settingsExtension != null && settingsExtension.SupportsCustomNormalMapEncoding()) && !IsPreset())
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || Lightmapping.isRunning))
                {
                    EditorGUI.BeginChangeCheck();
                    var oldEncoding = PlayerSettings.GetNormalMapEncoding_Internal(m_CurrentTarget, platform.name);
                    NormalMapEncoding[] encodingValues = { NormalMapEncoding.XYZ, NormalMapEncoding.DXT5nm };
                    var newEncoding = BuildEnumPopup(SettingsContent.normalMapEncodingLabel, oldEncoding, encodingValues, SettingsContent.normalMapEncodingNames);
                    if (EditorGUI.EndChangeCheck() && newEncoding != oldEncoding)
                    {
                        PlayerSettings.SetNormalMapEncoding_Internal(m_CurrentTarget, platform.name, newEncoding);
                        m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            // Show Lightmap Encoding and HDR Cubemap Encoding quality options
            if (hdrEncodingSupportedByPlatform && !IsPreset())
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || Lightmapping.isRunning))
                {
                    {
                        EditorGUI.BeginChangeCheck();
                        LightmapEncodingQuality encodingQuality = m_CurrentTarget.GetLightmapEncodingQualityForPlatform_Internal(platform.defaultTarget);
                        LightmapEncodingQuality[] lightmapEncodingValues = { LightmapEncodingQuality.Low, LightmapEncodingQuality.Normal, LightmapEncodingQuality.High };
                        LightmapEncodingQuality newEncodingQuality = BuildEnumPopup(SettingsContent.lightmapEncodingLabel, encodingQuality, lightmapEncodingValues, SettingsContent.lightmapEncodingNames);
                        if (EditorGUI.EndChangeCheck() && encodingQuality != newEncodingQuality)
                        {
                            m_CurrentTarget.SetLightmapEncodingQualityForPlatform_Internal(platform.defaultTarget, newEncodingQuality);
                            m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);

                            if(IsActivePlayerSettingsEditor())
                                Lightmapping.OnUpdateLightmapEncoding(platform.namedBuildTarget.ToBuildTargetGroup());

                            GUIUtility.ExitGUI();
                        }
                    }

                    {
                        EditorGUI.BeginChangeCheck();
                        HDRCubemapEncodingQuality encodingQuality = m_CurrentTarget.GetHDRCubemapEncodingQualityForPlatform_Internal(platform.defaultTarget);
                        HDRCubemapEncodingQuality[] hdrCubemapProbeEncodingValues = { HDRCubemapEncodingQuality.Low, HDRCubemapEncodingQuality.Normal, HDRCubemapEncodingQuality.High };
                        HDRCubemapEncodingQuality newEncodingQuality = BuildEnumPopup(SettingsContent.hdrCubemapEncodingLabel, encodingQuality, hdrCubemapProbeEncodingValues, SettingsContent.hdrCubemapEncodingNames);
                        if (EditorGUI.EndChangeCheck() && encodingQuality != newEncodingQuality)
                        {
                            m_CurrentTarget.SetHDRCubemapEncodingQualityForPlatform_Internal(platform.defaultTarget, newEncodingQuality);
                            m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);

                            if (IsActivePlayerSettingsEditor())
                                Lightmapping.OnUpdateHDRCubemapEncoding(platform.namedBuildTarget.ToBuildTargetGroup());

                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            if (!IsPreset())
            {
                // Light map settings
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || Lightmapping.isRunning))
                {
                    bool streamingEnabled = m_CurrentTarget.GetLightmapStreamingEnabledForPlatformGroup_Internal(platform.namedBuildTarget.ToBuildTargetGroup());
                    int streamingPriority = m_CurrentTarget.GetLightmapStreamingPriorityForPlatformGroup_Internal(platform.namedBuildTarget.ToBuildTargetGroup());

                    EditorGUI.BeginChangeCheck();
                    streamingEnabled = EditorGUILayout.Toggle(SettingsContent.lightmapStreamingEnabled, streamingEnabled);
                    if (streamingEnabled)
                    {
                        EditorGUI.indentLevel++;
                        streamingPriority = EditorGUILayout.DelayedIntField(SettingsContent.lightmapStreamingPriority, streamingPriority);
                        streamingPriority = Math.Clamp(streamingPriority, Texture2D.streamingMipmapsPriorityMin, Texture2D.streamingMipmapsPriorityMax);
                        EditorGUI.indentLevel--;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_CurrentTarget.SetLightmapStreamingEnabledForPlatformGroup_Internal(platform.namedBuildTarget.ToBuildTargetGroup(), streamingEnabled);
                        m_CurrentTarget.SetLightmapStreamingPriorityForPlatformGroup_Internal(platform.namedBuildTarget.ToBuildTargetGroup(), streamingPriority);
                        m_OnTrackSerializedObjectValueChanged?.Invoke(serializedObject);

                        if (IsActivePlayerSettingsEditor())
                            Lightmapping.OnUpdateLightmapStreaming(platform.namedBuildTarget.ToBuildTargetGroup());

                        serializedObject.ApplyModifiedProperties();

                        GUIUtility.ExitGUI();
                    }
                }

                // Tickbox for Frame Timing Stats.
                if (platform.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone || platform.namedBuildTarget == NamedBuildTarget.WindowsStoreApps || platform.namedBuildTarget == NamedBuildTarget.WebGL || (settingsExtension != null && settingsExtension.SupportsFrameTimingStatistics()))
                {
                    EditorGUILayout.PropertyField(m_EnableFrameTimingStats, SettingsContent.enableFrameTimingStats);
                    if (m_EnableFrameTimingStats.boolValue)
                    {
                        EditorGUILayout.HelpBox(SettingsContent.openGLFrameTimingStatsOnGPURecordersOffInfo.text, MessageType.Info);
                    }
                }

                // Tickbox for OpenGL-only option to toggle Profiler GPU Recorders.
                if (platform.namedBuildTarget == NamedBuildTarget.Standalone || platform.namedBuildTarget == NamedBuildTarget.Android || platform.namedBuildTarget == NamedBuildTarget.EmbeddedLinux  || platform.namedBuildTarget == NamedBuildTarget.QNX)
                {
                    EditorGUILayout.PropertyField(m_EnableOpenGLProfilerGPURecorders, SettingsContent.enableOpenGLProfilerGPURecorders);

                    // Add different notes/warnings depending on the tickbox combinations.
                    // These concern Frame Timing Stats as well as Profiler GPU Recorders,
                    // so are listed below both to (hopefully) highlight that they're linked.
                    if (m_EnableOpenGLProfilerGPURecorders.boolValue)
                    {
                        EditorGUILayout.HelpBox(SettingsContent.openGLFrameTimingStatsOffGPURecordersOnInfo.text, MessageType.Info);
                    }
                }

                if (hdrDisplaySupported)
                {
                    bool requestRepaint = false;
                    bool oldAllowHDRDisplaySupport = m_AllowHDRDisplaySupport.boolValue;
                    EditorGUILayout.PropertyField(m_AllowHDRDisplaySupport, SettingsContent.allowHDRDisplay);
                    if (oldAllowHDRDisplaySupport != m_AllowHDRDisplaySupport.boolValue)
                        requestRepaint = true;

                    using (new EditorGUI.DisabledScope(!m_AllowHDRDisplaySupport.boolValue))
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            bool oldUseHDRDisplay = m_UseHDRDisplay.boolValue;
                            EditorGUILayout.PropertyField(m_UseHDRDisplay, SettingsContent.useHDRDisplay);

                            if (oldUseHDRDisplay != m_UseHDRDisplay.boolValue)
                                requestRepaint = true;

                            if (platform.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone || platform.namedBuildTarget == NamedBuildTarget.WindowsStoreApps || platform.namedBuildTarget == NamedBuildTarget.iOS)
                            {
                                using (new EditorGUI.DisabledScope(!m_UseHDRDisplay.boolValue))
                                {
                                    using (new EditorGUI.IndentLevelScope())
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        HDRDisplayBitDepth oldBitDepth = (HDRDisplayBitDepth)m_HDRBitDepth.intValue;
                                        HDRDisplayBitDepth[] bitDepthValues = { HDRDisplayBitDepth.BitDepth10, HDRDisplayBitDepth.BitDepth16 };
                                        GUIContent hdrBitDepthLabel = EditorGUIUtility.TrTextContent("Swap Chain Bit Depth", "Affects the bit depth of the final swap chain format and color space.");
                                        GUIContent[] hdrBitDepthNames = { EditorGUIUtility.TrTextContent("Bit Depth 10"), EditorGUIUtility.TrTextContent("Bit Depth 16") };

                                        HDRDisplayBitDepth bitDepth = BuildEnumPopup(hdrBitDepthLabel, oldBitDepth, bitDepthValues, hdrBitDepthNames);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            m_HDRBitDepth.intValue = (int)bitDepth;
                                            if (oldBitDepth != bitDepth)
                                                requestRepaint = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (m_AllowHDRDisplaySupport.boolValue && GraphicsSettings.currentRenderPipeline != null && !SupportedRenderingFeatures.active.supportsHDR)
                    {
                        EditorGUILayout.HelpBox(SettingsContent.hdrOutputRequireHDRRenderingWarning.text, MessageType.Info);
                    }

                    if (requestRepaint)
                        EditorApplication.RequestRepaintAllViews();
                }

                // Virtual Texturing settings
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || EditorApplication.isCompiling))
                {
                    EditorGUI.BeginChangeCheck();
                    bool toggledValue =  EditorGUILayout.Toggle(SettingsContent.virtualTexturingSupportEnabled, m_VirtualTexturingSupportEnabled.boolValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (IsActivePlayerSettingsEditor())
                        {
                            if (PlayerSettings.OnVirtualTexturingChanged())
                            {
                                // updating the property only after the user agrees to restart the editor for an active build profile
                                // because on Windows, the dialog gives control back to the editor *before* the user interacts with it
                                // so if the property is updated at that point, the editor will call ApplyModifiedProperties
                                // which will call PlayerSettings::AwakeFromLoad and then PlayerSettings::SyncCurrentVirtualTexturingState
                                // and incorrectly check against a value in its intermediate state
                                m_VirtualTexturingSupportEnabled.boolValue = toggledValue;
                                PlayerSettings.SetVirtualTexturingSupportEnabled(toggledValue);
                                m_VirtualTexturingSupportEnabled.serializedObject.ApplyModifiedProperties();
                                EditorApplication.delayCall += EditorApplication.RestartEditorAndRecompileScripts;
                            }
                        }
                        else
                        {
                            // updating the serialized property for an inactive build profile is fine because it won't show a restart prompt
                            // and SyncCurrentVirtualTexturingState will only work for active build profiles
                            m_VirtualTexturingSupportEnabled.boolValue = toggledValue;
                        }
                        GUIUtility.ExitGUI();
                    }
                }

                if (PlayerSettings.GetVirtualTexturingSupportEnabled())
                {
                    // Test Platform compatibility
                    bool platformSupportsVT = UnityEngine.Rendering.VirtualTexturingEditor.Building.IsPlatformSupportedForPlayer(platform.defaultTarget);
                    if (!platformSupportsVT)
                    {
                        EditorGUILayout.HelpBox(SettingsContent.virtualTexturingUnsupportedPlatformWarning.text, MessageType.Warning);
                    }

                    // Test for all three 'Automatic Graphics API for X' checkboxes and report API/Platform-specific error
                    if (platform.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone)
                    {
                        var duplicatedBuildTargetCheck = new List<String>();
                        foreach (var buildTarget in BuildTargetDiscovery.StandaloneBuildTargets)
                        {
                            if (BuildTargetDiscovery.TryGetBuildTarget(buildTarget, out IBuildTarget iBuildTarget))
                            {
                                if (duplicatedBuildTargetCheck.Contains(iBuildTarget.TargetName)) // Win64 and Win have the same target name and would duplicate the hint box
                                    continue;

                                duplicatedBuildTargetCheck.Add(iBuildTarget.TargetName);

                                ShowWarningIfVirtualTexturingUnsupportedByAPI(iBuildTarget, true);
                            }
                        }
                    }
                    else
                    {
                        if (platformSupportsVT && BuildTargetDiscovery.TryGetBuildTarget(platform.defaultTarget, out IBuildTarget iBuildTarget))
                            ShowWarningIfVirtualTexturingUnsupportedByAPI(iBuildTarget, false);
                    }
                }
            }
            if (!IsPreset())
                EditorGUILayout.Space();

            Stereo360CaptureGUI(platform.namedBuildTarget.ToBuildTargetGroup());

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || EditorApplication.isCompiling))
            {
                var target = platform.namedBuildTarget.ToBuildTargetGroup();
                bool debugModeEnabled = m_CurrentTarget.GetLoadStoreDebugModeEnabledForPlatformGroup_Internal(target);
                bool debugModeEditorOnly = m_CurrentTarget.GetLoadStoreDebugModeEditorOnlyForPlatformGroup_Internal(target);

                EditorGUI.BeginChangeCheck();
                debugModeEnabled = EditorGUILayout.Toggle(SettingsContent.loadStoreDebugModeCheckbox, debugModeEnabled);
                if (debugModeEnabled)
                {
                    EditorGUI.indentLevel++;
                    debugModeEditorOnly = EditorGUILayout.Toggle(SettingsContent.loadStoreDebugModeEditorOnlyCheckbox, debugModeEditorOnly);
                    EditorGUI.indentLevel--;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    m_CurrentTarget.SetLoadStoreDebugModeEnabledForPlatformGroup_Internal(target, debugModeEnabled);
                    m_CurrentTarget.SetLoadStoreDebugModeEditorOnlyForPlatformGroup_Internal(target, debugModeEditorOnly);
                    OnTargetObjectChangedDirectly();

                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space();
        }

        private bool VirtualTexturingInvalidGfxAPI(BuildTarget target, bool checkEditor)
        {
            GraphicsDeviceType[] gfxTypes = m_CurrentTarget.GetGraphicsAPIs_Internal(target);

            bool supportedAPI = true;
            foreach (GraphicsDeviceType api in gfxTypes)
            {
                supportedAPI &= UnityEngine.Rendering.VirtualTexturingEditor.Building.IsRenderAPISupported(api, target, checkEditor);
            }

            return !supportedAPI;
        }

        private static readonly Dictionary<IBuildTarget, GUIContent> virtualTexturingUnsupportedAPIContents = new();

        void ShowWarningIfVirtualTexturingUnsupportedByAPI(IBuildTarget buildTarget, bool checkEditor)
        {
            GUIContent warningText = null;
            if(!VirtualTexturingInvalidGfxAPI((BuildTarget)buildTarget.GetLegacyId, checkEditor))
                return;

            if (virtualTexturingUnsupportedAPIContents.TryGetValue(buildTarget, out var guiContent))
                warningText = guiContent;
            else
                warningText = virtualTexturingUnsupportedAPIContents[buildTarget] = EditorGUIUtility.TrTextContent($"The target {buildTarget.DisplayName} graphics API does not support Virtual Texturing. To target compatible graphics APIs, uncheck 'Auto Graphics API', and remove OpenGL ES 2/3 and OpenGLCoreOpenGLCore.");

            if (warningText != null)
                EditorGUILayout.HelpBox(warningText.text, MessageType.Warning);
        }

        // WebGPU
        private static IReadOnlyList<BuildTarget> k_WebGPUSupportedBuildTargets => new List<BuildTarget> {
            BuildTarget.WebGL,
            // The following is Google's Dawn native implementatation for WebGPU.
            BuildTarget.StandaloneWindows,
            BuildTarget.StandaloneWindows64,
            // Dawn currently isn't supported on OSX or Linux.
        };

        private bool CheckIfWebGPUInGfxAPIList()
        {
            foreach (var target in k_WebGPUSupportedBuildTargets)
            {
                if (m_CurrentTarget.GetGraphicsAPIs_Internal(target).Contains(GraphicsDeviceType.WebGPU))
                {
                    return true;
                }
            }

            return false;
        }

        private void OtherSectionIdentificationGUI(BuildPlatform platform, ISettingEditorExtension settingsExtension)
        {
            // Identification
            if (settingsExtension != null && settingsExtension.HasIdentificationGUI())
            {
                GUILayout.Label(SettingsContent.identificationTitle, EditorStyles.boldLabel);
                settingsExtension.IdentificationSectionGUI();

                EditorGUILayout.Space();
            }
        }

        private void OtherSectionVulkanSettingsGUI(BuildPlatform platform, ISettingEditorExtension settingsExtension)
        {
            // Standalone targets don't have a settingsExtension but support vulkan
            if (settingsExtension != null && !settingsExtension.ShouldShowVulkanSettings())
                return;

            GUILayout.Label(SettingsContent.vulkanSettingsTitle, EditorStyles.boldLabel);
            if (!IsPreset())
            {
                EditorGUILayout.PropertyField(m_VulkanEnableSetSRGBWrite, SettingsContent.vulkanEnableSetSRGBWrite);
                EditorGUILayout.PropertyField(m_VulkanNumSwapchainBuffers, SettingsContent.vulkanNumSwapchainBuffers);

                // Not a No-OP, VulkanNumSwapchainBuffers has native work that should run when active setting changes.
                if (IsActivePlayerSettingsEditor())
                {
                    PlayerSettings.vulkanNumSwapchainBuffers = m_VulkanNumSwapchainBuffers.uintValue;
                }
            }
            EditorGUILayout.PropertyField(m_VulkanEnableLateAcquireNextImage, SettingsContent.vulkanEnableLateAcquireNextImage);
            EditorGUILayout.PropertyField(m_VulkanEnableCommandBufferRecycling, SettingsContent.vulkanEnableCommandBufferRecycling);

            if (settingsExtension != null && settingsExtension.ShouldShowVulkanSettings())
                settingsExtension.VulkanSectionGUI();

            EditorGUILayout.Space();
        }

        internal void ShowPlatformIconsByKind(PlatformIconFieldGroup iconFieldGroup, bool foldByKind = true, bool foldBySubkind = true)
        {
            m_IconsEditor.ShowPlatformIconsByKind(iconFieldGroup, foldByKind, foldBySubkind);
        }

        internal static GUIContent GetApplicationIdentifierError(BuildTargetGroup targetGroup)
        {
            if (targetGroup == BuildTargetGroup.Android)
                return SettingsContent.packageNameError;

            return SettingsContent.applicationIdentifierError;
        }

        internal void ShowApplicationIdentifierUI(BuildTargetGroup targetGroup, string label, string tooltip)
        {
            var overrideDefaultID = m_OverrideDefaultApplicationIdentifier.boolValue;
            var defaultIdentifier = String.Format("com.{0}.{1}", m_CompanyName.stringValue, m_ProductName.stringValue);
            var oldIdentifier = "";
            var currentIdentifier = PlayerSettings.SanitizeApplicationIdentifier(defaultIdentifier, targetGroup);
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroupName(targetGroup);
            var warningMessage = SettingsContent.applicationIdentifierWarning.text;
            var errorMessage = GetApplicationIdentifierError(targetGroup).text;

            string GetSanitizedApplicationIdentifier()
            {
                var sanitizedIdentifier = PlayerSettings.SanitizeApplicationIdentifier(currentIdentifier, targetGroup);

                if (currentIdentifier != oldIdentifier)
                {
                    if (!overrideDefaultID && !PlayerSettings.IsApplicationIdentifierValid(currentIdentifier, targetGroup))
                        Debug.LogError(errorMessage);
                    else if (overrideDefaultID && sanitizedIdentifier != currentIdentifier)
                        Debug.LogWarning(warningMessage);
                }

                return sanitizedIdentifier;
            }

            if (!m_ApplicationIdentifier.serializedObject.isEditingMultipleObjects)
            {
                m_ApplicationIdentifier.TryGetMapEntry(buildTargetGroup, out var entry);

                if (entry != null)
                    oldIdentifier = entry.FindPropertyRelative("second").stringValue;

                if (currentIdentifier != oldIdentifier)
                {
                    if (overrideDefaultID)
                        currentIdentifier = oldIdentifier;
                    else
                        m_ApplicationIdentifier.SetMapValue(buildTargetGroup, currentIdentifier);
                }

                EditorGUILayout.BeginVertical();
                EditorGUI.BeginChangeCheck();

                using (new EditorGUI.DisabledScope(!overrideDefaultID))
                {
                    currentIdentifier = GetSanitizedApplicationIdentifier();
                    currentIdentifier = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent(label, tooltip), currentIdentifier);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    currentIdentifier = GetSanitizedApplicationIdentifier();
                    m_ApplicationIdentifier.SetMapValue(buildTargetGroup, currentIdentifier);
                }

                if (currentIdentifier == "com.Company.ProductName" || currentIdentifier == "com.unity3d.player")
                    EditorGUILayout.HelpBox("Don't forget to set the Application Identifier.", MessageType.Warning);
                else if (!PlayerSettings.IsApplicationIdentifierValid(currentIdentifier, targetGroup))
                    EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                else if (!overrideDefaultID && currentIdentifier != defaultIdentifier)
                    EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);

                EditorGUILayout.EndVertical();
            }
        }

        internal static void ShowBuildNumberUI(SerializedProperty prop, NamedBuildTarget buildTarget, string label, string tooltip)
        {
            if (!prop.serializedObject.isEditingMultipleObjects)
            {
                prop.TryGetMapEntry(buildTarget.TargetName, out var entry);

                if (entry != null)
                {
                    var buildNumber = entry.FindPropertyRelative("second");
                    EditorGUILayout.PropertyField(buildNumber, EditorGUIUtility.TrTextContent(label, tooltip));
                }
            }
        }

        private bool ShouldRestartEditorToApplySetting()
        {
            return EditorUtility.DisplayDialog("Unity editor restart required", "The Unity editor must be restarted for this change to take effect.  Cancel to revert changes.", "Apply", "Cancel");
        }

        private ScriptingImplementation GetCurrentBackendForTarget(NamedBuildTarget namedBuildTarget)
        {
            if (m_ScriptingBackend.TryGetMapEntry(namedBuildTarget.TargetName, out var entry))
                return (ScriptingImplementation)entry.FindPropertyRelative("second").intValue;
            else
                return PlayerSettings.GetDefaultScriptingBackend(namedBuildTarget);
        }

        private Il2CppCompilerConfiguration GetCurrentIl2CppCompilerConfigurationForTarget(NamedBuildTarget namedBuildTarget)
        {
            if (m_Il2CppCompilerConfiguration.TryGetMapEntry(namedBuildTarget.TargetName, out var entry))
                return (Il2CppCompilerConfiguration)entry.FindPropertyRelative("second").intValue;
            else
                return Il2CppCompilerConfiguration.Release;
        }
        private Il2CppCodeGeneration GetCurrentIl2CppCodeGenerationForTarget(NamedBuildTarget namedBuildTarget)
        {
            if (m_Il2CppCodeGeneration.TryGetMapEntry(namedBuildTarget.TargetName, out var entry))
                return (Il2CppCodeGeneration)entry.FindPropertyRelative("second").intValue;
            else
                return Il2CppCodeGeneration.OptimizeSpeed;
        }

        private Il2CppStacktraceInformation GetCurrentIl2CppStacktraceInformationOptionForTarget(NamedBuildTarget namedBuildTarget)
        {
            if (m_Il2CppStacktraceInformation.TryGetMapEntry(namedBuildTarget.TargetName, out var entry))
                return (Il2CppStacktraceInformation)entry.FindPropertyRelative("second").intValue;
            else
                return Il2CppStacktraceInformation.MethodOnly;
        }

        private ManagedStrippingLevel GetCurrentManagedStrippingLevelForTarget(NamedBuildTarget namedBuildTarget, ScriptingImplementation backend)
        {
            if (m_ManagedStrippingLevel.TryGetMapEntry(namedBuildTarget.TargetName, out var entry))
                return (ManagedStrippingLevel)entry.FindPropertyRelative("second").intValue;
            else
            {
                if (backend == ScriptingImplementation.IL2CPP)
                    return ManagedStrippingLevel.Minimal;
                else
                    return ManagedStrippingLevel.Disabled;
            }
        }

        private ApiCompatibilityLevel GetApiCompatibilityLevelForTarget(NamedBuildTarget namedBuildTarget)
        {
            if (m_APICompatibilityLevel.TryGetMapEntry(namedBuildTarget.TargetName, out var entry))
                return (ApiCompatibilityLevel)entry.FindPropertyRelative("second").intValue;
            else
                // See comment in EditorOnlyPlayerSettings regarding defaultApiCompatibilityLevel
                return (ApiCompatibilityLevel)m_DefaultAPICompatibilityLevel.intValue;
        }

        private void SetApiCompatibilityLevelForTarget(string targetGroup, ApiCompatibilityLevel apiCompatibilityLevel)
        {
            if (m_APICompatibilityLevel.TryGetMapEntry(targetGroup, out _))
                m_APICompatibilityLevel.SetMapValue(targetGroup, (int)apiCompatibilityLevel);
            else
                // See comment in EditorOnlyPlayerSettings regarding defaultApiCompatibilityLevel
                m_DefaultAPICompatibilityLevel.intValue = (int)apiCompatibilityLevel;
        }

        private EditorAssembliesCompatibilityLevel GetEditorAssembliesCompatibilityLevel()
        {
            return (EditorAssembliesCompatibilityLevel)m_EditorAssembliesCompatibilityLevel.intValue;
        }

        bool HasAnyNetFXCompatibilityLevel()
        {
            return m_CurrentTarget?.HasAnyNetFXCompatibilityLevel() ?? false;
        }

        private void SetEditorAssembliesCompatibilityLevel(EditorAssembliesCompatibilityLevel editorAssembliesCompatibilityLevel)
        {
            // We won't allow switching back to the "Default" value.
            if (editorAssembliesCompatibilityLevel != EditorAssembliesCompatibilityLevel.Default)
                m_EditorAssembliesCompatibilityLevel.intValue = (int)editorAssembliesCompatibilityLevel;
        }

        private const string kInputSystemPackageName = "com.unity.inputsystem";


        private void OtherSectionConfigurationGUI(BuildPlatform platform, ISettingEditorExtension settingsExtension)
        {
            // Configuration
            GUILayout.Label(SettingsContent.configurationTitle, EditorStyles.boldLabel);

            // scripting runtime settings in play mode are not supported
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                // Scripting back-end
                bool allowCompilerConfigurationSelection = false;
                ScriptingImplementation currentBackend = GetCurrentBackendForTarget(platform.namedBuildTarget);
                using (new EditorGUI.DisabledScope(m_SerializedObject.isEditingMultipleObjects))
                {
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, GUIContent.none, m_ScriptingBackend))
                        {
                            IScriptingImplementations scripting = ModuleManager.GetScriptingImplementations(platform.namedBuildTarget);

                            if (scripting == null)
                            {
                                allowCompilerConfigurationSelection = true; // All platforms that support only one scripting backend are IL2CPP platforms
                                BuildDisabledEnumPopup(SettingsContent.scriptingDefault, SettingsContent.scriptingBackend);
                            }
                            else
                            {
                                var backends = scripting.Enabled();

                                allowCompilerConfigurationSelection = currentBackend == ScriptingImplementation.IL2CPP && scripting.AllowIL2CPPCompilerConfigurationSelection();
                                ScriptingImplementation newBackend;

                                if (backends.Length == 1)
                                {
                                    newBackend = backends[0];
                                    BuildDisabledEnumPopup(GetNiceScriptingBackendName(backends[0]), SettingsContent.scriptingBackend);
                                }
                                else
                                {
                                    newBackend = BuildEnumPopup(SettingsContent.scriptingBackend, currentBackend, backends, GetNiceScriptingBackendNames(backends));
                                }

                                if (newBackend != currentBackend)
                                {
                                    m_ScriptingBackend.SetMapValue(platform.namedBuildTarget.TargetName, (int)newBackend);
                                    currentBackend = newBackend;
                                }
                            }
                        }
                    }
                }

                // Api Compatibility Level
                using (new EditorGUI.DisabledScope(m_SerializedObject.isEditingMultipleObjects))
                {
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, GUIContent.none, m_APICompatibilityLevel))
                        {
                            var currentAPICompatibilityLevel = GetApiCompatibilityLevelForTarget(platform.namedBuildTarget);
                            var availableCompatibilityLevels = new ApiCompatibilityLevel[] { ApiCompatibilityLevel.NET_Unity_4_8, ApiCompatibilityLevel.NET_Standard };

                            var newAPICompatibilityLevel = BuildEnumPopup(
                                SettingsContent.apiCompatibilityLevel,
                                currentAPICompatibilityLevel,
                                availableCompatibilityLevels,
                                GetNiceApiCompatibilityLevelNames(availableCompatibilityLevels)
                            );

                            if (newAPICompatibilityLevel != currentAPICompatibilityLevel)
                            {
                                SetApiCompatibilityLevelForTarget(platform.namedBuildTarget.TargetName, newAPICompatibilityLevel);

                                if (platform.IsActive())
                                {
                                    SetReason(RecompileReason.apiCompatibilityLevelModified);
                                }
                            }
                        }
                    }
                }

                // Editor Assemblies Compatibility level
                using (new EditorGUI.DisabledScope(m_SerializedObject.isEditingMultipleObjects || HasAnyNetFXCompatibilityLevel()))
                {
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, GUIContent.none, m_EditorAssembliesCompatibilityLevel))
                        {
                            var currentEditorAssembliesCompatibilityLevel = GetEditorAssembliesCompatibilityLevel();

                            List<EditorAssembliesCompatibilityLevel> availableEditorAssemblyCompatibilityLevels = new List<EditorAssembliesCompatibilityLevel>(3);
                            if (currentEditorAssembliesCompatibilityLevel == EditorAssembliesCompatibilityLevel.Default)
                            {
                                availableEditorAssemblyCompatibilityLevels.Add(EditorAssembliesCompatibilityLevel.Default);
                            }

                            availableEditorAssemblyCompatibilityLevels.Add(EditorAssembliesCompatibilityLevel.NET_Unity_4_8);
                            availableEditorAssemblyCompatibilityLevels.Add(EditorAssembliesCompatibilityLevel.NET_Standard);

                            var newEditorAssembliesCompatibilityLevel = BuildEnumPopup(
                               SettingsContent.editorAssembliesCompatibilityLevel,
                               currentEditorAssembliesCompatibilityLevel,
                               availableEditorAssemblyCompatibilityLevels.ToArray(),
                               GetNiceEditorAssembliesCompatibilityLevelNames(availableEditorAssemblyCompatibilityLevels.ToArray())
                           );
                            if (newEditorAssembliesCompatibilityLevel != currentEditorAssembliesCompatibilityLevel)
                            {
                                SetEditorAssembliesCompatibilityLevel(newEditorAssembliesCompatibilityLevel);

                                if (platform.IsActive())
                                {
                                    SetReason(RecompileReason.editorAssembliesCompatibilityLevelModified);
                                }
                            }
                        }
                    }
                }

                // Il2cpp Code Generation
                using (new EditorGUI.DisabledScope(m_SerializedObject.isEditingMultipleObjects))
                {
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, GUIContent.none, m_Il2CppCodeGeneration))
                        {
                            using (new EditorGUI.DisabledScope(currentBackend != ScriptingImplementation.IL2CPP))
                            {
                                var currentCodeGeneration = GetCurrentIl2CppCodeGenerationForTarget(platform.namedBuildTarget);

                                var codeGenerationValues = new[] { Il2CppCodeGeneration.OptimizeSpeed, Il2CppCodeGeneration.OptimizeSize };
                                var newCodeGeneration = BuildEnumPopup(SettingsContent.il2cppCodeGeneration, currentCodeGeneration, codeGenerationValues, SettingsContent.il2cppCodeGenerationNames);

                                if (currentCodeGeneration != newCodeGeneration)
                                    m_Il2CppCodeGeneration.SetMapValue(platform.namedBuildTarget.TargetName, (int)newCodeGeneration);
                            }
                        }
                    }
                }

                // Il2cpp Compiler Configuration
                using (new EditorGUI.DisabledScope(m_SerializedObject.isEditingMultipleObjects))
                {
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, GUIContent.none, m_Il2CppCompilerConfiguration))
                        {
                            using (new EditorGUI.DisabledScope(!allowCompilerConfigurationSelection))
                            {
                                Il2CppCompilerConfiguration currentConfiguration = GetCurrentIl2CppCompilerConfigurationForTarget(platform.namedBuildTarget);

                                var configurations = GetIl2CppCompilerConfigurations();
                                var configurationNames = GetIl2CppCompilerConfigurationNames();

                                var newConfiguration = BuildEnumPopup(SettingsContent.il2cppCompilerConfiguration, currentConfiguration, configurations, configurationNames);

                                if (currentConfiguration != newConfiguration)
                                    m_Il2CppCompilerConfiguration.SetMapValue(platform.namedBuildTarget.TargetName, (int)newConfiguration);
                            }
                        }
                    }
                }

                // Il2Cpp Stacktrace Configuration
                using (new EditorGUI.DisabledScope(currentBackend != ScriptingImplementation.IL2CPP || platform.namedBuildTarget == NamedBuildTarget.WebGL))
                {
                    Il2CppStacktraceInformation config = GetCurrentIl2CppStacktraceInformationOptionForTarget(platform.namedBuildTarget);

                    var newConfiguration = BuildEnumPopup(SettingsContent.il2cppStacktraceInformation, config,
                        GetIl2CppStacktraceOptions(), GetIl2CppStacktraceOptionNames());

                    if (config != newConfiguration)
                        m_Il2CppStacktraceInformation.SetMapValue(platform.namedBuildTarget.TargetName, (int)newConfiguration);
                }

                bool gcIncrementalEnabled = BuildPipeline.IsFeatureSupported("ENABLE_SCRIPTING_GC_WBARRIERS", platform.defaultTarget);

                using (new EditorGUI.DisabledScope(!gcIncrementalEnabled))
                {
                    var oldValue = m_GCIncremental.boolValue;
                    EditorGUILayout.PropertyField(m_GCIncremental, SettingsContent.gcIncremental);
                    if (m_GCIncremental.boolValue != oldValue)
                    {
                        if (!IsActivePlayerSettingsEditor())
                        {
                            return;
                        }
                        // Give the user a chance to change mind and revert changes.
                        if (ShouldRestartEditorToApplySetting())
                        {
                            m_GCIncremental.serializedObject.ApplyModifiedProperties();
                            EditorApplication.delayCall += EditorApplication.RestartEditorAndRecompileScripts;
                        }
                        else
                            m_GCIncremental.boolValue = oldValue;
                    }
                }
            }

            var insecureHttp = BuildEnumPopup(m_InsecureHttpOption, SettingsContent.insecureHttpOption, new[] { InsecureHttpOption.NotAllowed, InsecureHttpOption.DevelopmentOnly, InsecureHttpOption.AlwaysAllowed }, SettingsContent.insecureHttpOptions);
            if (insecureHttp == InsecureHttpOption.AlwaysAllowed)
                EditorGUILayout.HelpBox(SettingsContent.insecureHttpWarning.text, MessageType.Warning);

            // Privacy permissions
            bool showPrivacyPermissions =
                platform.namedBuildTarget == NamedBuildTarget.iOS || platform.namedBuildTarget == NamedBuildTarget.tvOS || platform.namedBuildTarget == NamedBuildTarget.VisionOS;

            if (showPrivacyPermissions)
            {
                EditorGUILayout.PropertyField(m_CameraUsageDescription, SettingsContent.cameraUsageDescription);
                EditorGUILayout.PropertyField(m_MicrophoneUsageDescription, SettingsContent.microphoneUsageDescription);

                if (platform.namedBuildTarget == NamedBuildTarget.iOS || platform.namedBuildTarget == NamedBuildTarget.tvOS)
                    EditorGUILayout.PropertyField(m_LocationUsageDescription, SettingsContent.locationUsageDescription);
            }

            bool showMobileSection =
                platform.namedBuildTarget == NamedBuildTarget.iOS ||
                platform.namedBuildTarget == NamedBuildTarget.tvOS ||
                platform.namedBuildTarget == NamedBuildTarget.Android ||
                platform.namedBuildTarget == NamedBuildTarget.WindowsStoreApps;

            // mobile-only settings
            if (showMobileSection)
            {
                bool supportsAccelerometerFrequency =
                    platform.namedBuildTarget == NamedBuildTarget.iOS ||
                    platform.namedBuildTarget == NamedBuildTarget.tvOS ||
                    platform.namedBuildTarget == NamedBuildTarget.WindowsStoreApps;
                if (supportsAccelerometerFrequency)
                    EditorGUILayout.PropertyField(m_AccelerometerFrequency, SettingsContent.accelerometerFrequency);

                if (platform.namedBuildTarget == NamedBuildTarget.iOS || platform.namedBuildTarget == NamedBuildTarget.tvOS || platform.namedBuildTarget == NamedBuildTarget.Android)
                {
                    EditorGUILayout.PropertyField(m_MuteOtherAudioSources, SettingsContent.muteOtherAudioSources);

                    if (m_MuteOtherAudioSources.boolValue == false && platform.namedBuildTarget == NamedBuildTarget.iOS)
                        EditorGUILayout.HelpBox(SettingsContent.iOSExternalAudioInputNotSupported.text, MessageType.Warning);
                }

                // TVOS TODO: check what should stay or go
                if (platform.namedBuildTarget == NamedBuildTarget.iOS || platform.namedBuildTarget == NamedBuildTarget.tvOS)
                {
                    if (platform.namedBuildTarget == NamedBuildTarget.iOS)
                    {
                        EditorGUILayout.PropertyField(m_PrepareIOSForRecording, SettingsContent.prepareIOSForRecording);

                        EditorGUILayout.PropertyField(m_ForceIOSSpeakersWhenRecording, SettingsContent.forceIOSSpeakersWhenRecording);
                    }
                    EditorGUILayout.PropertyField(m_UIRequiresPersistentWiFi, SettingsContent.UIRequiresPersistentWiFi);
                }
            }

            if (platform.namedBuildTarget == NamedBuildTarget.iOS || platform.namedBuildTarget == NamedBuildTarget.tvOS || platform.namedBuildTarget == NamedBuildTarget.VisionOS)
                EditorGUILayout.PropertyField(m_IOSURLSchemes, SettingsContent.iOSURLSchemes, true);

            // Active input handling
            if (platform.namedBuildTarget != NamedBuildTarget.Server)
            {
                using (var vertical = new EditorGUILayout.VerticalScope())
                {
                    var currValue = m_ActiveInputHandler.intValue;

                    using (var propertyScope = new EditorGUI.PropertyScope(vertical.rect, GUIContent.none, m_ActiveInputHandler))
                    {
                        m_ActiveInputHandler.intValue = EditorGUILayout.Popup(SettingsContent.activeInputHandling, m_ActiveInputHandler.intValue, SettingsContent.activeInputHandlingOptions);
                    }

                    if (m_ActiveInputHandler.intValue != currValue)
                    {
                        if (!IsActivePlayerSettingsEditor())
                        {
                            return;
                        }
                        // Give the user a chance to change mind and revert changes.
                        if (ShouldRestartEditorToApplySetting())
                        {
                            m_ActiveInputHandler.serializedObject.ApplyModifiedProperties();
                            EditorApplication.delayCall += EditorApplication.RestartEditorAndRecompileScripts;
                        }
                        else
                            m_ActiveInputHandler.intValue = currValue;
                    }
                    var isInputSystemPackageInstalled = UnityEditor.PackageManager.PackageInfo.IsPackageRegistered(kInputSystemPackageName);
                    if(m_ActiveInputHandler.intValue != 0 && !isInputSystemPackageInstalled)
                    {
                        EditorGUILayout.HelpBox(SettingsContent.activeInputHandlingError.text, MessageType.Error, true);
                    }
                    if(m_ActiveInputHandler.intValue != 1)
                    {
                        EditorGUILayout.HelpBox(SettingsContent.activeInputHandlingDeprecationError.text, MessageType.Warning, true);
                    }
                }
            }

            if (settingsExtension != null)
                settingsExtension.ConfigurationSectionGUI();

            EditorGUILayout.Space();
        }

        private string GetScriptingDefineSymbolsForGroup(NamedBuildTarget buildTarget)
        {
            string defines = string.Empty;
            if (m_ScriptingDefines.TryGetMapEntry(buildTarget.TargetName, out var entry))
            {
                defines = entry.FindPropertyRelative("second").stringValue;
            }
            return defines;
        }

        private void SetScriptingDefineSymbolsForGroup(NamedBuildTarget buildTarget, string[] defines)
        {
            m_ScriptingDefines.SetMapValue(buildTarget.TargetName, ScriptingDefinesHelper.ConvertScriptingDefineArrayToString(defines));
        }

        string[] GetAdditionalCompilerArgumentsForGroup(NamedBuildTarget buildTarget)
        {
            if (m_AdditionalCompilerArguments.TryGetMapEntry(buildTarget.TargetName, out var entry))
            {
                var serializedArguments = entry.FindPropertyRelative("second");
                var arguments = new string[serializedArguments.arraySize];

                for (int i = 0; i < serializedArguments.arraySize; ++i)
                {
                    arguments[i] = serializedArguments.GetArrayElementAtIndex(i).stringValue;
                }

                return arguments;
            }

            return new string[0];
        }

        void SetAdditionalCompilerArgumentsForGroup(NamedBuildTarget buildTarget, string[] arguments)
        {
            m_AdditionalCompilerArguments.SetMapValue(buildTarget.TargetName, arguments);
        }

        bool GetCaptureStartupLogsForTarget(NamedBuildTarget buildTarget)
        {
            if (m_CaptureStartupLogs.TryGetMapEntry(buildTarget.TargetName, out var entry))
            {
                if (entry != null)
                    return entry.FindPropertyRelative("second").boolValue;
            }
            return buildTarget == NamedBuildTarget.Server ? true : false;
        }

        private void OtherSectionScriptCompilationGUI(BuildPlatform platform)
        {
            // Configuration
            GUILayout.Label(SettingsContent.scriptCompilationTitle, EditorStyles.boldLabel);

            // User script defines
            using (new EditorGUI.DisabledScope(m_SerializedObject.isEditingMultipleObjects))
            {
                using (var vertical = new EditorGUILayout.VerticalScope())
                {
                    if (serializedScriptingDefines == null || scriptingDefineSymbolsList == null)
                    {
                        InitReorderableScriptingDefineSymbolsList(platform.namedBuildTarget);
                    }

                    if (lastNamedBuildTarget.TargetName == platform.namedBuildTarget.TargetName)
                    {
                        scriptingDefineSymbolsList.DoLayoutList();
                    }
                    else
                    {
                        // If platform changes, update define symbols
                        serializedScriptingDefines = GetScriptingDefineSymbolsForGroup(platform.namedBuildTarget);
                        UpdateScriptingDefineSymbolsLists();
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        var GUIState = GUI.enabled;


                        GUI.enabled = serializedScriptingDefines.Count() > 0;

                        if (GUILayout.Button(SettingsContent.scriptingDefineSymbolsCopyDefines, EditorStyles.miniButton))
                        {
                            EditorGUIUtility.systemCopyBuffer = PlayerSettings.GetScriptingDefineSymbols(platform.namedBuildTarget);
                        }

                        GUI.enabled = hasScriptingDefinesBeenModified;

                        if (GUILayout.Button(SettingsContent.scriptingDefineSymbolsApplyRevert, EditorStyles.miniButton))
                        {
                            // Make sure to remove focus from reorderable list text field on revert
                            GUI.FocusControl(null);

                            UpdateScriptingDefineSymbolsLists();
                        }

                        if (GUILayout.Button(SettingsContent.scriptingDefineSymbolsApply, EditorStyles.miniButton))
                        {
                            // Make sure to remove focus from reorderable list text field on apply
                            GUI.FocusControl(null);

                            SetScriptingDefineSymbolsForGroup(platform.namedBuildTarget, scriptingDefinesList.ToArray());

                            // Get Scripting Define Symbols without duplicates
                            serializedScriptingDefines = GetScriptingDefineSymbolsForGroup(platform.namedBuildTarget);
                            UpdateScriptingDefineSymbolsLists();

                            if (platform.IsActive())
                                SetReason(RecompileReason.scriptingDefineSymbolsModified);
                        }

                        // Set previous GUIState
                        GUI.enabled = GUIState;
                    }

                    scriptingDefinesControlID = EditorGUIUtility.s_LastControlID;
                }

                EditorGUILayout.Space();

                using (var vertical = new EditorGUILayout.VerticalScope())
                {
                    if (serializedAdditionalCompilerArguments == null || additionalCompilerArgumentsReorderableList == null)
                    {
                        InitReorderableAdditionalCompilerArgumentsList(platform.namedBuildTarget);
                    }

                    using (new EditorGUI.PropertyScope(vertical.rect, GUIContent.none, m_AdditionalCompilerArguments))
                    {
                        if (lastNamedBuildTarget.TargetName == platform.namedBuildTarget.TargetName)
                        {
                            additionalCompilerArgumentsReorderableList.DoLayoutList();
                        }
                        else
                        {
                            // If platform changes, update define symbols
                            serializedAdditionalCompilerArguments = GetAdditionalCompilerArgumentsForGroup(platform.namedBuildTarget);
                            UpdateAdditionalCompilerArgumentsLists();
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();

                            using (new EditorGUI.DisabledScope(!hasAdditionalCompilerArgumentsBeenModified))
                            {
                                if (GUILayout.Button(SettingsContent.scriptingDefineSymbolsApplyRevert, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                                {
                                    UpdateAdditionalCompilerArgumentsLists();
                                }

                                if (GUILayout.Button(SettingsContent.scriptingDefineSymbolsApply, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                                {
                                    SetAdditionalCompilerArgumentsForGroup(platform.namedBuildTarget, additionalCompilerArgumentsList.ToArray());

                                    // Get Additional Compiler Arguments without duplicates
                                    serializedAdditionalCompilerArguments = GetAdditionalCompilerArgumentsForGroup(platform.namedBuildTarget);
                                    UpdateAdditionalCompilerArgumentsLists();

                                    if (platform.IsActive())
                                    {
                                        SetReason(RecompileReason.additionalCompilerArgumentsModified);
                                    }
                                }
                            }
                        }
                    }
                    //We want to cache latest build target only after rendering both Scripting Defines and Additional Args
                    //Because both elements share the same logic
                    lastNamedBuildTarget = platform.namedBuildTarget;
                }
            }

            // Suppress common warnings
            EditorGUILayout.PropertyField(m_SuppressCommonWarnings, SettingsContent.suppressCommonWarnings);
            if (serializedSuppressCommonWarnings != m_SuppressCommonWarnings.boolValue)
            {
                serializedSuppressCommonWarnings = m_SuppressCommonWarnings.boolValue;
                SetReason(RecompileReason.suppressCommonWarningsModified);
            }

            // Allow unsafe code
            EditorGUILayout.PropertyField(m_AllowUnsafeCode, SettingsContent.allowUnsafeCode);
            if (serializedAllowUnsafeCode != m_AllowUnsafeCode.boolValue)
            {
                serializedAllowUnsafeCode = m_AllowUnsafeCode.boolValue;
                SetReason(RecompileReason.allowUnsafeCodeModified);
            }

            // Use deterministic compliation
            EditorGUILayout.PropertyField(m_UseDeterministicCompilation, SettingsContent.useDeterministicCompilation);
            if (serializedUseDeterministicCompilation != m_UseDeterministicCompilation.boolValue)
            {
                serializedUseDeterministicCompilation = m_UseDeterministicCompilation.boolValue;
                SetReason(RecompileReason.useDeterministicCompilationModified);
            }
        }

        void DrawTextField(Rect rect, int index)
        {
            // Handle list selection before the TextField grabs input
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
            {
                if (scriptingDefineSymbolsList.index != index)
                {
                    scriptingDefineSymbolsList.index = index;
                    scriptingDefineSymbolsList.onSelectCallback?.Invoke(scriptingDefineSymbolsList);
                }
            }

            string define = scriptingDefinesList[index];
            scriptingDefinesList[index] = EditorGUI.TextField(rect, scriptingDefinesList[index]);

            if (!scriptingDefinesList[index].Equals(define))
                SetScriptingDefinesListDirty();
        }

        void DrawTextFieldAdditionalCompilerArguments(Rect rect, int index)
        {
            // Handle list selection before the TextField grabs input
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
            {
                if (additionalCompilerArgumentsReorderableList.index != index)
                {
                    additionalCompilerArgumentsReorderableList.index = index;
                    additionalCompilerArgumentsReorderableList.onSelectCallback?.Invoke(additionalCompilerArgumentsReorderableList);
                }
            }

            string additionalCompilerArgument = additionalCompilerArgumentsList[index];
            additionalCompilerArgumentsList[index] = GUI.TextField(rect, additionalCompilerArgumentsList[index]);
            if (!additionalCompilerArgumentsList[index].Equals(additionalCompilerArgument))
                SetAdditionalCompilerArgumentListDirty();
        }

        void AddScriptingDefineCallback(ReorderableList list)
        {
            scriptingDefinesList.Add("");
            SetScriptingDefinesListDirty();
        }

        void RemoveScriptingDefineCallback(ReorderableList list)
        {
            scriptingDefinesList.RemoveAt(list.index);
            SetScriptingDefinesListDirty();
        }

        void DrawScriptingDefinesHeaderCallback(Rect rect)
        {
            using (new EditorGUI.PropertyScope(rect, GUIContent.none, m_ScriptingDefines))
            {
                GUI.Label(rect, SettingsContent.scriptingDefineSymbols, EditorStyles.label);
            }
        }

        void SetScriptingDefinesListDirty(ReorderableList list = null)
        {
            hasScriptingDefinesBeenModified = true;
        }

        void AddAdditionalCompilerArgumentCallback(ReorderableList list)
        {
            additionalCompilerArgumentsList.Add("");
            SetAdditionalCompilerArgumentListDirty();
        }

        void RemoveAdditionalCompilerArgumentCallback(ReorderableList list)
        {
            additionalCompilerArgumentsList.RemoveAt(list.index);
            SetAdditionalCompilerArgumentListDirty();
        }

        void SetAdditionalCompilerArgumentListDirty(ReorderableList list = null)
        {
            hasAdditionalCompilerArgumentsBeenModified = true;
        }

        private void OtherSectionOptimizationGUI(BuildPlatform platform)
        {
            // Optimization
            GUILayout.Label(SettingsContent.optimizationTitle, EditorStyles.boldLabel);

            if (platform.namedBuildTarget == NamedBuildTarget.Server)
                EditorGUILayout.PropertyField(m_DedicatedServerOptimizations, SettingsContent.dedicatedServerOptimizations);

            EditorGUILayout.PropertyField(m_BakeCollisionMeshes, SettingsContent.bakeCollisionMeshes);

            if (IsPreset())
                EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_PreloadedAssets, SettingsContent.preloadedAssets, true);

            if (IsPreset())
                EditorGUI.indentLevel--;

            bool platformSupportsStripping = !BuildTargetDiscovery.PlatformGroupHasFlag(platform.namedBuildTarget.ToBuildTargetGroup(), TargetAttributes.StrippingNotSupported);

            if (platformSupportsStripping)
            {
                ScriptingImplementation backend = GetCurrentBackendForTarget(platform.namedBuildTarget);
                if (BuildPipeline.IsFeatureSupported("ENABLE_ENGINE_CODE_STRIPPING", platform.defaultTarget) && backend == ScriptingImplementation.IL2CPP)
                    EditorGUILayout.PropertyField(m_StripEngineCode, SettingsContent.stripEngineCode);

                using (var vertical = new EditorGUILayout.VerticalScope())
                {
                    using (var propertyScope = new EditorGUI.PropertyScope(vertical.rect, GUIContent.none, m_ManagedStrippingLevel))
                    {
                        var availableStrippingLevels = GetAvailableManagedStrippingLevels(backend);
                        ManagedStrippingLevel currentManagedStrippingLevel = GetCurrentManagedStrippingLevelForTarget(platform.namedBuildTarget, backend);
                        ManagedStrippingLevel newManagedStrippingLevel;

                        newManagedStrippingLevel = BuildEnumPopup(SettingsContent.managedStrippingLevel, currentManagedStrippingLevel, availableStrippingLevels, GetNiceManagedStrippingLevelNames(availableStrippingLevels));
                        if (newManagedStrippingLevel != currentManagedStrippingLevel)
                            m_ManagedStrippingLevel.SetMapValue(platform.namedBuildTarget.TargetName, (int)newManagedStrippingLevel);
                    }
                }
            }

            if (platform.namedBuildTarget == NamedBuildTarget.iOS || platform.namedBuildTarget == NamedBuildTarget.tvOS)
            {
                EditorGUILayout.PropertyField(m_IPhoneScriptCallOptimization, SettingsContent.iPhoneScriptCallOptimization);
            }
            if (platform.namedBuildTarget == NamedBuildTarget.Android)
            {
                EditorGUILayout.PropertyField(m_AndroidProfiler, SettingsContent.enableInternalProfiler);
            }

            EditorGUILayout.Space();

            // Vertex compression flags dropdown
            VertexChannelCompressionFlags vertexFlags = (VertexChannelCompressionFlags)m_VertexChannelCompressionMask.intValue;
            vertexFlags = (VertexChannelCompressionFlags)EditorGUILayout.EnumFlagsField(SettingsContent.vertexChannelCompressionMask, vertexFlags);
            m_VertexChannelCompressionMask.intValue = (int)vertexFlags;

            EditorGUILayout.PropertyField(m_StripUnusedMeshComponents, SettingsContent.stripUnusedMeshComponents);
            EditorGUILayout.PropertyField(m_MipStripping, SettingsContent.mipStripping);

            EditorGUILayout.Space();
        }

        static ManagedStrippingLevel[] mono_levels = new ManagedStrippingLevel[] { ManagedStrippingLevel.Disabled, ManagedStrippingLevel.Minimal, ManagedStrippingLevel.Low, ManagedStrippingLevel.Medium, ManagedStrippingLevel.High };
        static ManagedStrippingLevel[] il2cpp_levels = new ManagedStrippingLevel[] { ManagedStrippingLevel.Minimal, ManagedStrippingLevel.Low, ManagedStrippingLevel.Medium, ManagedStrippingLevel.High };
        // stripping levels vary based on scripting backend
        private ManagedStrippingLevel[] GetAvailableManagedStrippingLevels(ScriptingImplementation backend)
        {
            if (backend == ScriptingImplementation.IL2CPP)
            {
                return il2cpp_levels;
            }
            else
            {
                return mono_levels;
            }
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
                    Il2CppCompilerConfiguration.Master,
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

        static Il2CppStacktraceInformation[] m_Il2cppStacktraceOptions;
        static GUIContent[] m_Il2cppStacktraceOptionNames;

        private Il2CppStacktraceInformation[] GetIl2CppStacktraceOptions()
        {
            if (m_Il2cppStacktraceOptions == null)
                m_Il2cppStacktraceOptions = (Il2CppStacktraceInformation[])Enum.GetValues(typeof(Il2CppStacktraceInformation));

            return m_Il2cppStacktraceOptions;
        }

        private GUIContent[] GetIl2CppStacktraceOptionNames()
        {
            if (m_Il2cppStacktraceOptionNames == null)
            {
                m_Il2cppStacktraceOptionNames = new GUIContent[]
                {
                    EditorGUIUtility.TextContent("Method Name"),
                    EditorGUIUtility.TextContent("Method Name, File Name, and Line Number"),
                };
            }

            return m_Il2cppStacktraceOptionNames;
        }

        public static bool IsLatestApiCompatibility(ApiCompatibilityLevel level)
        {
            return (level == ApiCompatibilityLevel.NET_4_6 || level == ApiCompatibilityLevel.NET_Standard_2_0);
        }

        private void OtherSectionLoggingGUI()
        {
            GUILayout.Label(SettingsContent.loggingTitle, EditorStyles.boldLabel);

            using (var vertical = new EditorGUILayout.VerticalScope())
            {
                using (var propertyScope = new EditorGUI.PropertyScope(vertical.rect, GUIContent.none, m_StackTraceTypes))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Log Type");
                        foreach (StackTraceLogType stackTraceLogType in Enum.GetValues(typeof(StackTraceLogType)))
                            GUILayout.Label(stackTraceLogType.ToString(), GUILayout.Width(70));
                    }

                    foreach (LogType logType in Enum.GetValues(typeof(LogType)))
                    {
                        var logProperty = m_StackTraceTypes.GetArrayElementAtIndex((int)logType);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(logType.ToString(), GUILayout.MinWidth(60));
                            foreach (StackTraceLogType stackTraceLogType in Enum.GetValues(typeof(StackTraceLogType)))
                            {
                                StackTraceLogType inStackTraceLogType = (StackTraceLogType)logProperty.intValue;
                                EditorGUI.BeginChangeCheck();
                                bool val = EditorGUILayout.ToggleLeft(" ", inStackTraceLogType == stackTraceLogType, GUILayout.Width(65));
                                if (EditorGUI.EndChangeCheck() && val)
                                {
                                    logProperty.intValue = (int)stackTraceLogType;

                                    if (IsActivePlayerSettingsEditor())
                                        PlayerSettings.SetGlobalStackTraceLogType(logType, stackTraceLogType);
                                }
                            }
                        }
                    }
                }
            }

            EditorGUILayout.Space();
        }

        private void Stereo360CaptureGUI(BuildTargetGroup targetGroup)
        {
            EditorGUILayout.PropertyField(m_Enable360StereoCapture, SettingsContent.stereo360CaptureCheckbox);
        }

        private void OtherSectionLegacyGUI(BuildPlatform platform)
        {
            GUILayout.Label(SettingsContent.legacyTitle, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_LegacyClampBlendShapeWeights, SettingsContent.legacyClampBlendShapeWeights);

            EditorGUILayout.Space();
        }

        private void OtherSectionCaptureLogsGUI(NamedBuildTarget namedBuildTarget)
        {
            GUILayout.Label(SettingsContent.captureLogsTitle, EditorStyles.boldLabel);

            bool val = GetCaptureStartupLogsForTarget(namedBuildTarget);
            bool newVal = EditorGUILayout.Toggle(SettingsContent.captureStartupLogs, val);
            if (val != newVal)
                m_CaptureStartupLogs.SetMapValue(namedBuildTarget.TargetName, newVal);

            EditorGUILayout.Space();
        }

        private static Dictionary<ApiCompatibilityLevel, GUIContent> m_NiceApiCompatibilityLevelNames;
        private static Dictionary<EditorAssembliesCompatibilityLevel, GUIContent> m_NiceEditorAssembliesCompatibilityLevelNames;
        private static Dictionary<ManagedStrippingLevel, GUIContent> m_NiceManagedStrippingLevelNames;

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

        static GUIContent[] GetNiceScriptingBackendNames(ScriptingImplementation[] scriptingBackends)
        {
            return scriptingBackends.Select(s => GetNiceScriptingBackendName(s)).ToArray();
        }

        static GUIContent GetNiceScriptingBackendName(ScriptingImplementation scriptingBackend)
        {
            switch (scriptingBackend)
            {
                case ScriptingImplementation.Mono2x:
                    return SettingsContent.scriptingMono2x;
                case ScriptingImplementation.IL2CPP:
                    return SettingsContent.scriptingIL2CPP;
#pragma warning disable 618
                case ScriptingImplementation.CoreCLR:
                    return SettingsContent.scriptingCoreCLR;
                default:
                    throw new ArgumentException($"Scripting backend value {scriptingBackend} is not supported.", nameof(scriptingBackend));
            }
        }

        private static GUIContent[] GetNiceApiCompatibilityLevelNames(ApiCompatibilityLevel[] apiCompatibilityLevels)
        {
            if (m_NiceApiCompatibilityLevelNames == null)
            {
                m_NiceApiCompatibilityLevelNames = new Dictionary<ApiCompatibilityLevel, GUIContent>
                {
                    { ApiCompatibilityLevel.NET_2_0, SettingsContent.apiCompatibilityLevel_NET_2_0 },
                    { ApiCompatibilityLevel.NET_2_0_Subset, SettingsContent.apiCompatibilityLevel_NET_2_0_Subset },
                    { ApiCompatibilityLevel.NET_Unity_4_8, SettingsContent.apiCompatibilityLevel_NET_FW_Unity },
                    { ApiCompatibilityLevel.NET_Standard, SettingsContent.apiCompatibilityLevel_NET_Standard },
                };
            }

            return GetGUIContentsForValues(m_NiceApiCompatibilityLevelNames, apiCompatibilityLevels);
        }

        private static GUIContent[] GetNiceEditorAssembliesCompatibilityLevelNames(EditorAssembliesCompatibilityLevel[] editorAssembliesCompatibilityLevels)
        {
            if (m_NiceEditorAssembliesCompatibilityLevelNames == null)
            {
                m_NiceEditorAssembliesCompatibilityLevelNames = new Dictionary<EditorAssembliesCompatibilityLevel, GUIContent>
                {
                    { EditorAssembliesCompatibilityLevel.Default, SettingsContent.editorAssembliesCompatibilityLevel_Default },
                    { EditorAssembliesCompatibilityLevel.NET_Unity_4_8, SettingsContent.editorAssembliesCompatibilityLevel_NET_Framework },
                    { EditorAssembliesCompatibilityLevel.NET_Standard, SettingsContent.editorAssembliesCompatibilityLevel_NET_Standard },
                };
            }

            return GetGUIContentsForValues(m_NiceEditorAssembliesCompatibilityLevelNames, editorAssembliesCompatibilityLevels);
        }

        private static GUIContent[] GetNiceManagedStrippingLevelNames(ManagedStrippingLevel[] managedStrippingLevels)
        {
            if (m_NiceManagedStrippingLevelNames == null)
            {
                m_NiceManagedStrippingLevelNames = new Dictionary<ManagedStrippingLevel, GUIContent>
                {
                    { ManagedStrippingLevel.Disabled, SettingsContent.strippingDisabled },
                    { ManagedStrippingLevel.Minimal, SettingsContent.strippingMinimal },
                    { ManagedStrippingLevel.Low, SettingsContent.strippingLow },
                    { ManagedStrippingLevel.Medium, SettingsContent.strippingMedium },
                    { ManagedStrippingLevel.High, SettingsContent.strippingHigh },
                };
            }
            return GetGUIContentsForValues(m_NiceManagedStrippingLevelNames, managedStrippingLevels);
        }

        internal static void BuildPathBoxButton(SerializedProperty prop, string uiString, string directory)
        {
            BuildPathBoxButton(prop, uiString, directory, null, null);
        }

        internal static void BuildPathBoxButton(SerializedProperty prop, string uiString, string directory, Action onSelect, Action onChanged)
        {
            float h = EditorGUI.kSingleLineHeight;
            float kLabelFloatMinW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
            float kLabelFloatMaxW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
            Rect r = GUILayoutUtility.GetRect(kLabelFloatMinW, kLabelFloatMaxW, h, h, EditorStyles.layerMaskField, null);

            float labelWidth = EditorGUIUtility.labelWidth;
            Rect buttonRect = new Rect(r.x + EditorGUI.indent, r.y, labelWidth - EditorGUI.indent, r.height);
            Rect fieldRect = new Rect(r.x + labelWidth, r.y, r.width - labelWidth, r.height);

            string display = (prop.stringValue.Length == 0) ? "Not selected" : prop.stringValue;
            EditorGUI.LabelField(fieldRect, display, EditorStyles.label);

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

                if (changed && onChanged != null)
                    onChanged();

                // Necessary to avoid the error "BeginLayoutGroup must be called first".
                GUIUtility.ExitGUI();
            }
        }

        internal static void BuildFileBoxButton(SerializedProperty prop, string uiString, string directory, string ext)
        {
            BuildFileBoxButton(prop, uiString, directory, ext, null, null);
        }

        internal static void BuildFileBoxButton(SerializedProperty prop, string uiString, string directory,
            string ext, Action onSelect, Action onChanged)
        {
            bool changed = false;
            using (var vertical = new EditorGUILayout.VerticalScope())
            using (new EditorGUI.PropertyScope(vertical.rect, GUIContent.none, prop))
            {
                float h = EditorGUI.kSingleLineHeight;
                float kLabelFloatMinW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
                float kLabelFloatMaxW = EditorGUI.kLabelW + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
                Rect r = GUILayoutUtility.GetRect(kLabelFloatMinW, kLabelFloatMaxW, h, h, EditorStyles.layerMaskField, null);

                float labelWidth = EditorGUIUtility.labelWidth;
                Rect buttonRect = new Rect(r.x + EditorGUI.indent, r.y, labelWidth - EditorGUI.indent, r.height);
                Rect fieldRect = new Rect(r.x + labelWidth, r.y, r.width - labelWidth, r.height);

                string display = (prop.stringValue.Length == 0) ? "Not selected" : prop.stringValue;
                EditorGUI.LabelField(fieldRect, display, EditorStyles.label);

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

                    if (changed && onChanged != null)
                        onChanged();

                    // Necessary to avoid the error "BeginLayoutGroup must be called first".
                    GUIUtility.ExitGUI();
                }
            }
        }

        public void PublishSectionGUI(BuildPlatform platform, ISettingEditorExtension settingsExtension, int sectionIndex = 5)
        {
            if (platform.namedBuildTarget != NamedBuildTarget.WindowsStoreApps &&
                !(settingsExtension != null && settingsExtension.HasPublishSection()))
                return;

            if (BeginSettingsBox(sectionIndex, SettingsContent.publishingSettingsTitle))
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

        protected override bool ShouldHideOpenButton()
        {
            return true;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var keywordsList = new List<string>();

            keywordsList.AddRange(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<SettingsContent>());
            keywordsList.AddRange(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<PlayerSettingsSplashScreenEditor.Texts>());
            keywordsList.AddRange(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<PlayerSettingsIconsEditor.SettingsContent>());

            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Player", "ProjectSettings/ProjectSettings.asset",
                keywordsList);
            provider.activateHandler = (searchContext, rootElement) =>
            {
                var playerSettingsProvider = provider.settingsEditor as PlayerSettingsEditor;
                if (playerSettingsProvider != null)
                {
                    playerSettingsProvider.SetValueChangeListeners(provider.Repaint);
                    playerSettingsProvider.splashScreenEditor.SetValueChangeListeners(provider.Repaint);
                }
            };
            return provider;
        }

        void InitReorderableScriptingDefineSymbolsList(NamedBuildTarget namedBuildTarget)
        {
            // Get Scripting Define Symbols data
            string defines = GetScriptingDefineSymbolsForGroup(namedBuildTarget);
            scriptingDefinesList = new List<string>(ScriptingDefinesHelper.ConvertScriptingDefineStringToArray(serializedScriptingDefines));

            // Initialize Reorderable List
            scriptingDefineSymbolsList = new ReorderableList(scriptingDefinesList, typeof(string), true, true, true, true);
            scriptingDefineSymbolsList.drawElementCallback = (rect, index, isActive, isFocused) => DrawTextField(rect, index);
            scriptingDefineSymbolsList.drawHeaderCallback = (rect) => DrawScriptingDefinesHeaderCallback(rect);
            scriptingDefineSymbolsList.onAddCallback = AddScriptingDefineCallback;
            scriptingDefineSymbolsList.onRemoveCallback = RemoveScriptingDefineCallback;
            scriptingDefineSymbolsList.onChangedCallback = SetScriptingDefinesListDirty;
        }

        void UpdateScriptingDefineSymbolsLists()
        {
            scriptingDefinesList = new List<string>(ScriptingDefinesHelper.ConvertScriptingDefineStringToArray(serializedScriptingDefines));
            scriptingDefineSymbolsList.list = scriptingDefinesList;
            scriptingDefineSymbolsList.DoLayoutList();
            hasScriptingDefinesBeenModified = false;
        }

        void InitReorderableAdditionalCompilerArgumentsList(NamedBuildTarget namedBuildTarget)
        {
            var additionalCompilerArgumentsArray = GetAdditionalCompilerArgumentsForGroup(namedBuildTarget);
            additionalCompilerArgumentsList = additionalCompilerArgumentsArray.ToList();

            additionalCompilerArgumentsReorderableList = new ReorderableList(additionalCompilerArgumentsList, typeof(string), true, true, true, true);
            additionalCompilerArgumentsReorderableList.drawElementCallback = (rect, index, isActive, isFocused) => DrawTextFieldAdditionalCompilerArguments(rect, index);
            additionalCompilerArgumentsReorderableList.drawHeaderCallback = (rect) => GUI.Label(rect, SettingsContent.additionalCompilerArguments, EditorStyles.label);
            additionalCompilerArgumentsReorderableList.onAddCallback = AddAdditionalCompilerArgumentCallback;
            additionalCompilerArgumentsReorderableList.onRemoveCallback = RemoveAdditionalCompilerArgumentCallback;
            additionalCompilerArgumentsReorderableList.onChangedCallback = SetAdditionalCompilerArgumentListDirty;
        }

        void UpdateAdditionalCompilerArgumentsLists()
        {
            additionalCompilerArgumentsList = new List<string>(serializedAdditionalCompilerArguments);
            additionalCompilerArgumentsReorderableList.list = additionalCompilerArgumentsList;
            additionalCompilerArgumentsReorderableList.DoLayoutList();
            hasAdditionalCompilerArgumentsBeenModified = false;
        }

        private struct PlayerSettingsBox
        {
            public MethodInfo mi;
            public GUIContent title;
            public int order;
            public string TargetName;

            public PlayerSettingsBox(MethodInfo mi, string targetName, string title, int order)
            {
                this.mi = mi;
                this.title = EditorGUIUtility.TrTextContent(title);
                this.order = order;
                this.TargetName = targetName;
            }
        };

        private List<PlayerSettingsBox> m_boxes;

        private PlayerSettingsSectionAttribute GetSectionAttribute(MethodInfo mi)
        {
            foreach (var attr in mi.GetCustomAttributes())
            {
                if (attr is PlayerSettingsSectionAttribute)
                    return (PlayerSettingsSectionAttribute)attr;
            }
            return null;
        }

        private bool IsValidSectionSetting(MethodInfo mi)
        {
            if (!mi.IsStatic)
            {
                Debug.LogError($"Method {mi.Name} with attribute PlayerSettingsSection must be static.");
                return false;
            }
            if (mi.IsGenericMethod || mi.IsGenericMethodDefinition)
            {
                Debug.LogError($"Method {mi.Name} with attribute PlayerSettingsSection cannot be generic.");
                return false;
            }
            if (mi.GetParameters().Length != 0)
            {
                Debug.LogError($"Method {mi.Name} with attribute PlayerSettingsSection does not have the correct signature, expected: static void {mi.Name}()");
                return false;
            }
            return true;
        }

        private void FindPlayerSettingsAttributeSections()
        {
            m_boxes = new List<PlayerSettingsBox>();

            foreach (var method in TypeCache.GetMethodsWithAttribute<PlayerSettingsSectionAttribute>())
            {
                if (IsValidSectionSetting(method))
                {
                    PlayerSettingsSectionAttribute attr = GetSectionAttribute(method);
                    m_boxes.Add(new PlayerSettingsBox(method, attr.TargetName, attr.Title, attr.Order));
                }
            }

            m_boxes.Sort((a, b) => a.order.CompareTo(b.order));
        }

        void SyncColorGamuts()
        {
            if (m_ColorGamutList == null)
                return;

            m_ColorGamutList.list = m_CurrentTarget.GetColorGamuts_Internal().ToList();
        }

        void SyncPlatformAPIsList(BuildTarget target)
        {
            if (!m_GraphicsDeviceLists.ContainsKey(target))
                return;
            m_GraphicsDeviceLists[target].list = m_CurrentTarget.GetGraphicsAPIs_Internal(target).ToList();
        }
    }
}
