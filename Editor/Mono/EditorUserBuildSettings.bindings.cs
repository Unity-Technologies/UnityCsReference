// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;
using UnityEngine.Bindings;
using System;
using System.ComponentModel;

namespace UnityEditor
{
    /// Target PSP2 build platform.
    ///
    /// SA: EditorUserBuildSettings.psp2BuildSubtarget.
    ///
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum PSP2BuildSubtarget
    {
        /// Build package that it's hosted on the PC
        /// SA: EditorUserBuildSettings.psp2BuildSubtarget.
        PCHosted = 0,
        /// Build a package suited for TestKit testing
        /// SA: EditorUserBuildSettings.psp2BuildSubtarget.
        Package = 1,
    }

    /// Target PS4 build platform.
    ///
    /// SA: EditorUserBuildSettings.ps4BuildSubtarget.
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum PS4BuildSubtarget
    {
        /// Build package that it's hosted on the PC
        /// SA: EditorUserBuildSettings.ps4BuildSubtarget.
        PCHosted = 0,
        /// Build a package suited for TestKit testing
        /// SA: EditorUserBuildSettings.ps4BuildSubtarget.
        Package = 1,
        Iso = 2,
    }


    /// Target PS4 build Hardware Target.
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum PS4HardwareTarget
    {
        /// Target only Base hardware (works identically on Neo hardware)
        BaseOnly = 0,

        /// Obsolete.  Use PS4ProAndBase instead.
        [Obsolete("Enum member PS4HardwareTarget.NeoAndBase has been deprecated. Use PS4HardwareTarget.ProAndBase instead (UnityUpgradable) -> ProAndBase", true)]
        NeoAndBase = 1,

        /// Target PS4 Pro hardware, also must work on Base hardware
        ProAndBase = 1,
    }


    // Target Xbox build type.
    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    public enum XboxBuildSubtarget
    {
        // Development player
        Development = 0,
        // Master player (submission-proof)
        Master = 1,
        // Debug player (for building with source code)
        Debug = 2,
    }

    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    public enum XboxOneDeployMethod
    {
        // copies files to the kit
        Push = 0,
        // run and load files from a connected PC
        Pull = 1,
        // PC network share loose files to the kit
        RunFromPC = 2,
        // Build Xbox One Package
        Package = 3,
        // Build Xbox One Package - if installed only install launch chunk for testing
        PackageStreaming = 4,
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("UnityEditor.AndroidBuildSubtarget has been deprecated. Use UnityEditor.MobileTextureSubtarget instead (UnityUpgradable)", true)]
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum AndroidBuildSubtarget
    {
        Generic = -1,
        DXT = -1,
        PVRTC = -1,
        ATC = -1,
        ETC = -1,
        ETC2 = -1,
        ASTC = -1,
    }

    // Target texture build platform.
    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    public enum MobileTextureSubtarget
    {
        // Don't override texture compression.
        Generic = 0,
        // S3 texture compression, nonspecific to DXT variant. Supported on devices running Nvidia Tegra2 platform, including Motorala Xoom, Motorola Atrix, Droid Bionic, and others.
        DXT = 1,
        // PowerVR texture compression. Available in devices running PowerVR SGX530/540 GPU, such as Motorola DROID series; Samsung Galaxy S, Nexus S, and Galaxy Tab; and others.
        PVRTC = 2,
        [System.Obsolete("UnityEditor.MobileTextureSubtarget.ATC has been deprecated. Use UnityEditor.MobileTextureSubtarget.ETC instead (UnityUpgradable) -> UnityEditor.MobileTextureSubtarget.ETC", true)]
        ATC = 3,
        // ETC1 texture compression (or RGBA16 for textures with alpha), supported by all devices.
        ETC = 4,
        // ETC2/EAC texture compression, supported by GLES 3.0 devices
        ETC2 = 5,
        // Adaptive Scalable Texture Compression
        ASTC = 6,
    }

    // Fallback texture format for Android if ETC2 is selected, but not supported
    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    public enum AndroidETC2Fallback
    {
        // 32-bit uncompressed
        Quality32Bit = 0,
        // 16-bit uncompressed
        Quality16Bit = 1,
        // 32-bit uncompressed, downscaled 2x
        Quality32BitDownscaled = 2,
    }

    // Keep in sync with WSASubtarget in SerializationMetaFlags.h
    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    public enum WSASubtarget
    {
        AnyDevice = 0,
        PC = 1,
        Mobile = 2,
        HoloLens = 3
    }

    // *undocumented*
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum WSASDK
    {
        // *undocumented*
        SDK80 = 0,
        // *undocumented*
        SDK81 = 1,
        // *undocumented*
        PhoneSDK81 = 2,
        // *undocumented*
        UniversalSDK81 = 3,
        // *undocumented*
        UWP = 4,
    }

    // *undocumented*
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum WSAUWPBuildType
    {
        // *undocumented*
        XAML = 0,
        // *undocumented*
        D3D = 1,
    }

    // *undocumented*
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum WSABuildAndRunDeployTarget
    {
        /// *undocumented*
        LocalMachine = 0,
        /// *undocumented*
        WindowsPhone = 1,
        /// *undocumented*
        LocalMachineAndWindowsPhone = 2
    }

    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum WSABuildType
    {
        Debug = 0,
        Release = 1,
        Master = 2
    }

    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum iOSBuildType
    {
        Debug = 0,

        Release = 1,
    }

    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    internal enum Compression
    {
        None = 0,
        Lz4 = 2,
        Lz4HC = 3,
    }

    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum AndroidBuildSystem
    {
        Internal = 0,
        Gradle = 1,
        [Obsolete("ADT/eclipse project export for Android is no longer supported - please use Gradle export instead", true)]
        ADT = 2,
        /// *undocumented*
        VisualStudio = 3,
    }

    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum AndroidBuildType
    {
        Debug = 0,
        Development = 1,
        Release = 2,
    }

    // *undocumented*
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    internal enum AppleBuildAndRunType
    {
        Xcode = 0,
        Xcodebuild = 1,
        iOSDeploy = 2,
    }

    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum AndroidMinification
    {
        None = 0,
        Proguard = 1,
        Gradle = 2,
    }

    [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
    [StaticAccessor("GetEditorUserBuildSettings()", StaticAccessorType.Dot)]
    public partial class EditorUserBuildSettings : Object
    {
        private EditorUserBuildSettings() {}

        internal static AppleBuildAndRunType appleBuildAndRunType = AppleBuildAndRunType.Xcode;

        // The currently selected build target group.
        public static extern BuildTargetGroup selectedBuildTargetGroup { get; set; }

        // The currently selected target for a standalone build.
        public static extern BuildTarget selectedStandaloneTarget
        {
            [NativeMethod("GetSelectedStandaloneTarget")]
            get;
            [NativeMethod("SetSelectedStandaloneTargetFromBindings")]
            set;
        }

        internal static extern BuildTarget selectedFacebookTarget
        {
            [NativeMethod("GetSelectedFacebookTarget")]
            get;
            [NativeMethod("SetSelectedFacebookTargetFromBindings")]
            set;
        }

        internal static extern string facebookAccessToken { get; set; }

        ///PSP2 Build Subtarget
        public static extern PSP2BuildSubtarget psp2BuildSubtarget
        {
            [NativeMethod("GetSelectedPSP2BuildSubtarget")]
            get;
            [NativeMethod("SetSelectedPSP2BuildSubtarget")]
            set;
        }


        ///PS4 Build Subtarget
        public static extern PS4BuildSubtarget ps4BuildSubtarget
        {
            [NativeMethod("GetSelectedPS4BuildSubtarget")]
            get;
            [NativeMethod("SetSelectedPS4BuildSubtarget")]
            set;
        }


        ///PS4 Build Hardware Target
        public static extern PS4HardwareTarget ps4HardwareTarget
        {
            [NativeMethod("GetPS4HardwareTarget")]
            get;
            [NativeMethod("SetPS4HardwareTarget")]
            set;
        }


        // Are null references actively checked?
        public static extern bool explicitNullChecks { get; set; }

        // Are divide by zeros actively checked?
        public static extern bool explicitDivideByZeroChecks { get; set; }

        public static extern bool explicitArrayBoundsChecks { get; set; }

        // Should we write out submission materials when building?
        public static extern bool needSubmissionMaterials { get; set; }

        // Should we write out compressed archives (Sony PsArc)
        public static extern bool compressWithPsArc { get; set; }

        // Should we force an install on the build package, even if there are validation errors
        public static extern bool forceInstallation { get; set; }

        // Should we move the package to the Bluray disc outer edge (larger ISO, but faster loading)
        public static extern bool movePackageToDiscOuterEdge { get; set; }

        // Should we compress files added to the package file
        public static extern bool compressFilesInPackage { get; set; }

        // Headless Mode for Linux Players
        public static extern bool enableHeadlessMode { get; set; }


        // Scripts only build
        public static extern bool buildScriptsOnly { get; set; }

        //XboxOne Build subtarget
        public static extern XboxBuildSubtarget xboxBuildSubtarget
        {
            [NativeMethod("GetSelectedXboxBuildSubtarget")]
            get;
            [NativeMethod("SetSelectedXboxBuildSubtarget")]
            set;
        }

        // 0..X levels are all considered part of the launch set of streaming install chunks
        public static extern int streamingInstallLaunchRange { get; set; }

        //selected Xbox One Deploy Method
        public static extern XboxOneDeployMethod xboxOneDeployMethod
        {
            [NativeMethod("GetSelectedXboxOneDeployMethod")]
            get;
            [NativeMethod("SetSelectedXboxOneDeployMethod")]
            set;
        }

        public static extern string xboxOneUsername { get; set; }

        public static extern string xboxOneNetworkSharePath { get; set; }


        // Transitive property used when adding debug ports to the
        // manifest for our test systems. This is required to
        // allow the XboxOne to open the required ports in its
        // manifest.
        public static string xboxOneAdditionalDebugPorts { get; set; }
        public static bool xboxOneRebootIfDeployFailsAndRetry { get; set; }

        // Android platform options.
        public static extern MobileTextureSubtarget androidBuildSubtarget
        {
            [NativeMethod("GetSelectedAndroidBuildSubtarget")]
            get;
            [NativeMethod("SetSelectedAndroidBuildSubtarget")]
            set;
        }

        //Compression set/get methods for the map containing type for BuildTargetGroup
        internal static Compression GetCompressionType(BuildTargetGroup targetGroup)
        {
            return (Compression)GetCompressionTypeInternal(targetGroup);
        }

        [NativeMethod("GetSelectedCompressionType")]
        private static extern int GetCompressionTypeInternal(BuildTargetGroup targetGroup);

        internal static void SetCompressionType(BuildTargetGroup targetGroup, Compression type)
        {
            SetCompressionTypeInternal(targetGroup, (int)type);
        }

        [NativeMethod("SetSelectedCompressionType")]
        private static extern void SetCompressionTypeInternal(BuildTargetGroup targetGroup, int type);

        public static extern AndroidETC2Fallback androidETC2Fallback
        {
            [NativeMethod("GetSelectedAndroidETC2Fallback")]
            get;
            [NativeMethod("SetSelectedAndroidETC2Fallback")]
            set;
        }

        public static extern AndroidBuildSystem androidBuildSystem { get; set; }

        public static extern AndroidBuildType androidBuildType { get; set; }

        public static extern AndroidMinification androidDebugMinification { get; set; }

        public static extern AndroidMinification androidReleaseMinification { get; set; }

        // *undocumented*
        // NOTE: This setting should probably not be a part of the public API as is. Atm it is used by playmode tests
        //  and applied during build post-processing. We will however move towards separating building and launching
        //  which makes it unclear when connections should be attempted. In the future, the DeploymentTargets
        //  API will most likely support connecting to named external devices, which might make this setting obsolete.
        internal static extern string androidDeviceSocketAddress { get; set; }

        internal static extern string androidCurrentDeploymentTargetId { get; set; }

        public static extern WSASubtarget wsaSubtarget
        {
            [NativeMethod("GetSelectedWSABuildSubtarget")]
            get;
            [NativeMethod("SetSelectedWSABuildSubtarget")]
            set;
        }

        [Obsolete("EditorUserBuildSettings.wsaSDK is obsolete and has no effect.It will be removed in a subsequent Unity release.")]
        public static extern WSASDK wsaSDK
        {
            [NativeMethod("GetSelectedWSASDK")]
            get;
            [NativeMethod("SetSelectedWSASDK")]
            set;
        }


        // *undocumented*
        public static extern WSAUWPBuildType wsaUWPBuildType
        {
            [NativeMethod("GetSelectedWSAUWPBuildType")]
            get;
            [NativeMethod("SetSelectedWSAUWPBuildType")]
            set;
        }


        public static extern string wsaUWPSDK
        {
            [NativeMethod("GetSelectedWSAUWPSDK")]
            get;
            [NativeMethod("SetSelectedWSAUWPSDK")]
            set;
        }

        public static extern string wsaUWPVisualStudioVersion
        {
            [NativeMethod("GetSelectedWSAUWPVSVersion")]
            get;
            [NativeMethod("SetSelectedWSAUWPVSVersion")]
            set;
        }

        // *undocumented*
        public static extern WSABuildAndRunDeployTarget wsaBuildAndRunDeployTarget
        {
            [NativeMethod("GetSelectedWSABuildAndRunDeployTarget")]
            get;
            [NativeMethod("SetSelectedWSABuildAndRunDeployTarget")]
            set;
        }
        // *undocumented*
        public static extern bool wsaGenerateReferenceProjects
        {
            [NativeMethod("GetGenerateWSAReferenceProjects")]
            get;
            [NativeMethod("SetGenerateWSAReferenceProjects")]
            set;
        }

        public static extern void SetWSADotNetNative(WSABuildType config, bool enabled);

        public static extern bool GetWSADotNetNative(WSABuildType config);

        // Tizen platform options.
        public static extern MobileTextureSubtarget tizenBuildSubtarget
        {
            [NativeMethod("GetSelectedTizenBuildSubtarget")]
            get;
            [NativeMethod("SetSelectedTizenBuildSubtarget")]
            set;
        }

        // The currently active build target.
        public static extern BuildTarget activeBuildTarget { get; }


        // The currently active build target.
        internal static extern BuildTargetGroup activeBuildTargetGroup { get; }

        [NativeMethod("SwitchActiveBuildTargetSync")]
        public static extern bool SwitchActiveBuildTarget(BuildTargetGroup targetGroup, BuildTarget target);
        [NativeMethod("SwitchActiveBuildTargetAsync")]
        public static extern bool SwitchActiveBuildTargetAsync(BuildTargetGroup targetGroup, BuildTarget target);

        // DEFINE directives for the compiler.
        public static extern string[] activeScriptCompilationDefines
        {
            [NativeMethod("GetActiveScriptCompilationDefinesBindingMethod")]
            get;
        }

        // Get the current location for the build.
        public static extern string GetBuildLocation(BuildTarget target);

        // Set a new location for the build.
        public static extern void SetBuildLocation(BuildTarget target, string location);

        public static void SetPlatformSettings(string platformName, string name, string value)
        {
            string buildTargetGroup = BuildPipeline.GetBuildTargetGroupName(BuildPipeline.GetBuildTargetByName(platformName));
            SetPlatformSettings(buildTargetGroup, platformName, name, value);
        }

        public static extern void SetPlatformSettings(string buildTargetGroup, string buildTarget, string name, string value);

        public static string GetPlatformSettings(string platformName, string name)
        {
            string buildTargetGroup = BuildPipeline.GetBuildTargetGroupName(BuildPipeline.GetBuildTargetByName(platformName));
            return GetPlatformSettings(buildTargetGroup, platformName, name);
        }

        public static extern string GetPlatformSettings(string buildTargetGroup, string platformName, string name);

        // Enables a development build.
        public static extern bool development { get; set; }

        // Use prebuilt UnityNative asm.js module
        public static extern bool webGLUsePreBuiltUnityEngine { get; set; }

        // Start the player with a connection to the profiler.
        public static extern bool connectProfiler { get; set; }

        // Enable source-level debuggers to connect.
        public static extern bool allowDebugging { get; set; }

        // Export as Android Google Project instead of building it
        public static extern bool exportAsGoogleAndroidProject { get; set; }

        // Symlink runtime libraries with an iOS Xcode project.
        public static extern bool symlinkLibraries { get; set; }

        // Symlink trampoline for iOS Xcode project.
        internal static extern bool symlinkTrampoline { get; set; }

        public static extern iOSBuildType iOSBuildConfigType
        {
            [NativeMethod("GetIOSBuildType")]
            get;
            [NativeMethod("SetIOSBuildType")]
            set;
        }

        // Create a 3DS .cia format image for deploying to test kits.
        public static extern bool n3dsCreateCIAFile
        {
            [NativeMethod("GetCreateCIAFileFor3DS")]
            get;
            [NativeMethod("SetCreateCIAFileFor3DS")]
            set;
        }

        // Instead of creating a ROM file, create a buildable Visual Studio 2015 solution.
        public static extern bool switchCreateSolutionFile
        {
            [NativeMethod("GetCreateSolutionFileForSwitch")]
            get;
            [NativeMethod("SetCreateSolutionFileForSwitch")]
            set;
        }


        // Create a .nsp ROM file out of the loose-files .nspd folder
        public static extern bool switchCreateRomFile
        {
            [NativeMethod("GetCreateRomFileForSwitch")]
            get;
            [NativeMethod("SetCreateRomFileForSwitch")]
            set;
        }


        // Enable linkage of NVN Grahpics Debugger for Nintendo Switch.
        public static extern bool switchNVNGraphicsDebugger
        {
            [NativeMethod("GetNVNGraphicsDebuggerForSwitch")]
            get;
            [NativeMethod("SetNVNGraphicsDebuggerForSwitch")]
            set;
        }

        // Enable linkage of DebugPad functionality for Nintendo Switch.
        public static extern bool switchEnableDebugPad
        {
            [NativeMethod("GetEnableDebugPadForSwitch")]
            get;
            [NativeMethod("SetEnableDebugPadForSwitch")]
            set;
        }

        // Redirect attempts to write to "rom:" mount, to "host:" mount for Nintendo Switch (for debugging and tests only)
        public static extern bool switchRedirectWritesToHostMount
        {
            [NativeMethod("GetRedirectWritesToHostMountForSwitch")]
            get;
            [NativeMethod("SetRedirectWritesToHostMountForSwitch")]
            set;
        }

        // Place the built player in the build folder.
        public static extern bool installInBuildFolder { get; set; }
    }
}
