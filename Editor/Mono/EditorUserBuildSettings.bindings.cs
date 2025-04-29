// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;
using UnityEngine.Bindings;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEditor.Modules;
using UnityEngine.Assertions;

namespace UnityEditor
{
    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    public enum StandaloneBuildSubtarget
    {
        // *undocumented*
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
        Default = 0,
        Player = 2,
        Server = 1
    }

    namespace Build
    {
        [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
        public enum OverrideTextureCompression
        {
            NoOverride = 0,
            ForceUncompressed = 1,
            ForceFastCompressor = 2,
            ForceNoCrunchCompression = 3,
        }

        [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
        public enum Il2CppCodeGeneration
        {
            OptimizeSpeed = 0,
            OptimizeSize = 1
        }
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
        GP4Project = 3,
    }


    /// Target PS4 build Hardware Target.
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum PS4HardwareTarget
    {
        /// Target only Base hardware (works identically on Neo hardware)
        BaseOnly = 0,

        /// Obsolete.  Use PS4ProAndBase instead.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
    [Obsolete("The XDK Xbox One platform was removed in 2021.1", true)]
    public enum XboxOneDeployMethod
    {
        // copies files to the kit
        Push = 0,
        // PC network share loose files to the kit
        RunFromPC = 2,
        // Build Xbox One Package
        Package = 3,
        // Build Xbox One Package - if installed only install launch chunk for testing
        PackageStreaming = 4,
    }

    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    [Obsolete("The XDK Xbox One platform was removed in 2021.1", false)]
    public enum XboxOneDeployDrive
    {
        Default = 0,
        Retail = 1,
        Development = 2,
        Ext1 = 3,
        Ext2 = 4,
        Ext3 = 5,
        Ext4 = 6,
        Ext5 = 7,
        Ext6 = 8,
        Ext7 = 9
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

    [Obsolete("UnityEditor.AndroidCreateSymbols has been deprecated. Use UnityEditor.Android.DebugSymbols.level instead")]
    public enum AndroidCreateSymbols
    {
        Disabled,
        Public,
        Debugging
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
        [System.Obsolete("Texture compression format PVRTC has been deprecated and will be removed in a future release")]
        [EditorBrowsable(EditorBrowsableState.Never)]
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

    [Obsolete("AndroidETC2Fallback is obsolete and has no effect. It will be removed in a subsequent Unity release.")]
    public enum AndroidETC2Fallback
    {
        // 32-bit uncompressed
        Quality32Bit = 0,
        // 16-bit uncompressed
        Quality16Bit = 1,
        // 32-bit uncompressed, downscaled 2x
        Quality32BitDownscaled = 2,
    }

    // Target texture build platform.
    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    public enum WebGLTextureSubtarget
    {
        // Don't override texture compression.
        Generic = 0,
        // S3 texture compression, nonspecific to DXT variant, supported by desktop browsers
        DXT = 1,
        // ETC2/EAC texture compression, supported by mobile devices
        ETC2 = 3,
        // Adaptive Scalable Texture Compression, supported by mobile devices
        ASTC = 4,
    }

    // Client browser type
    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    public enum WebGLClientBrowserType
    {
        Default = 0,
        Edge = 1,
        Safari = 2,
        Firefox = 3,
        Chrome = 4,
        Chromium = 5
    }

     // Client browser type
     [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
     internal enum WebGLClientPlatform
     {
         Desktop = 0,
         Android = 1,
         iOS = 2
     }

    [NativeType(Header = "Runtime/Serialize/BuildTarget.h")]
    [Obsolete("WSASubtarget is obsolete and has no effect. It will be removed in a subsequent Unity release.")]
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
        XAML = 0,
        D3D = 1,
        ExecutableOnly = 2,
    }

    // *undocumented*
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum WSABuildAndRunDeployTarget
    {
        /// *undocumented*
        LocalMachine = 0,
        /// *undocumented*
        [System.Obsolete("UnityEditor.WSABuildAndRunDeployTarget.WindowsPhone is obsolete.", true)]
        WindowsPhone = 1,
        /// *undocumented*
        DevicePortal = 2
    }

    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum WindowsBuildAndRunDeployTarget
    {
        LocalMachine = 0,
        DevicePortal = 2
    }

    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum WSABuildType
    {
        Debug = 0,
        Release = 1,
        Master = 2
    }

    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum XcodeBuildConfig
    {
        Debug = 0,
        Release = 1,
    }

    [Obsolete("iOSBuildType is obsolete. Use XcodeBuildConfig instead (UnityUpgradable) -> XcodeBuildConfig", true)]
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
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Internal build system has been deprecated. Use Gradle instead (UnityUpgradable) -> UnityEditor.AndroidBuildSystem.Gradle", true)]
        Internal = 0,
        Gradle = 1,
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
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

    // We used this value to control if minification is enabled and what tool to use separately for release and debug builds.
    // With the Android Gradle Plugin changes in 3.4 the tool which will be used is the same for release and debug now
    [Obsolete("AndroidMinification enum is obsolete.", true)]
    public enum AndroidMinification
    {
        None = 0,
        Proguard = 1,
        Gradle = 2,
    }

    // *undocumented*
    [NativeType(Header = "Editor/Src/EditorOnlyPlayerSettings.h")]
    internal struct SwitchShaderCompilerConfig
    {
        internal int glslcDebugLevel;
        internal string debugInfoOutputPath;
        internal bool triggerGraphicsDebuggersConfigUpdate;
    }

    // *undocumented*
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum SwitchRomCompressionType
    {
        None = 0,
        Lz4 = 1,
    }

    [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
    [StaticAccessor("GetEditorUserBuildSettings()", StaticAccessorType.Dot)]
    public partial class EditorUserBuildSettings : Object
    {
        internal const string kSettingArchitecture = "Architecture";
        private EditorUserBuildSettings() {}

        internal static extern AppleBuildAndRunType appleBuildAndRunType { get; set; }
        internal static extern string appleDeviceId { get; set; }

        // The currently selected build target group.
        public static extern BuildTargetGroup selectedBuildTargetGroup { get; set; }

        [NativeMethod("GetSelectedSubTargetFor")]
        internal static extern int GetSelectedSubtargetFor(BuildTarget target);

        [NativeMethod("SetSelectedSubTargetFor")]
        internal static extern void SetSelectedSubtargetFor(BuildTarget target, int subtarget);

        [NativeMethod("GetActiveSubTargetFor")]
        internal static extern int GetActiveSubtargetFor(BuildTarget target);

        public static BuildTarget selectedStandaloneTarget
        {
            get { return internal_SelectedStandaloneTarget; }
            set
            {
                string platformName = BuildPipeline.GetBuildTargetName(value);
                var architecture = GetPlatformSettings(platformName, kSettingArchitecture).ToLower();
                switch (value)
                {
                    case BuildTarget.StandaloneWindows:
                        if (architecture != "x86")
                            SetPlatformSettings(platformName, kSettingArchitecture, OSArchitecture.x86.ToString());
                        break;
                    case BuildTarget.StandaloneWindows64:
                        if (architecture != "x64" && architecture != "arm64")
                            SetPlatformSettings(platformName, kSettingArchitecture, OSArchitecture.x64.ToString());
                        break;
                }

                internal_SelectedStandaloneTarget = value;
            }
        }

        private static extern BuildTarget internal_SelectedStandaloneTarget
        {
            [NativeMethod("GetSelectedStandaloneTarget")]
            get;
            [NativeMethod("SetSelectedStandaloneTargetFromBindings")]
            set;
        }

        internal static extern StandaloneBuildSubtarget selectedStandaloneBuildSubtarget
        {
            [NativeMethod("GetSelectedStandaloneBuildSubtarget")]
            get;
            [NativeMethod("SetSelectedStandaloneBuildSubtarget")]
            set;
        }

        public static extern StandaloneBuildSubtarget standaloneBuildSubtarget
        {
            [NativeMethod("GetActiveStandaloneBuildSubtarget")]
            get;
            [NativeMethod("SetActiveStandaloneBuildSubtarget")]
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

        [Obsolete("EditorUserBuildSettings.compressWithPsArc is obsolete and has no effect. It will be removed in a subsequent Unity release.")]
        public static bool compressWithPsArc { get => false; set {} }

        // Should we force an install on the build package, even if there are validation errors
        public static extern bool forceInstallation { get; set; }

        // Should we move the package to the Bluray disc outer edge (larger ISO, but faster loading)
        public static extern bool movePackageToDiscOuterEdge { get; set; }

        // Should we compress files added to the package file
        public static extern bool compressFilesInPackage { get; set; }

        // Headless Mode
        [Obsolete("Use EditorUserBuildSettings.standaloneBuildSubtarget instead.")]
        public static bool enableHeadlessMode
        {
            get => standaloneBuildSubtarget == StandaloneBuildSubtarget.Server;
            set => standaloneBuildSubtarget = value ? StandaloneBuildSubtarget.Server : StandaloneBuildSubtarget.Player;
        }


        // Scripts only build
        public static extern bool buildScriptsOnly { get; set; }

        // 0..X levels are all considered part of the launch set of streaming install chunks
        [Obsolete("The XDK Xbox One platform was removed in 2021.1", true)]
        public static extern int streamingInstallLaunchRange { get; set; }

        //XboxOne Build subtarget
        [Obsolete("The XDK Xbox One platform was removed in 2021.1", true)]
        public static extern XboxBuildSubtarget xboxBuildSubtarget
        {
            [NativeMethod("GetSelectedXboxBuildSubtarget")]
            get;
            [NativeMethod("SetSelectedXboxBuildSubtarget")]
            set;
        }

        //selected Xbox One Deploy Method
        [Obsolete("The XDK Xbox One platform was removed in 2021.1", true)]
        public static extern XboxOneDeployMethod xboxOneDeployMethod
        {
            [NativeMethod("GetSelectedXboxOneDeployMethod")]
            get;
            [NativeMethod("SetSelectedXboxOneDeployMethod")]
            set;
        }

        //selected Xbox One Deployment Drive
        [Obsolete("The XDK Xbox One platform was removed in 2021.1", false)]
        public static extern XboxOneDeployDrive xboxOneDeployDrive
        {
            [NativeMethod("GetSelectedXboxOneDeployDrive")]
            get;
            [NativeMethod("SetSelectedXboxOneDeployDrive")]
            set;
        }


        [Obsolete("xboxOneUsername is deprecated, it is unnecessary and non-functional.")]
        public static  string xboxOneUsername { get; set; }

        [Obsolete("xboxOneNetworkSharePath is deprecated, it is unnecessary and non-functional.")]
        public static  string xboxOneNetworkSharePath { get; set; }


        // Transitive property used when adding debug ports to the
        // manifest for our test systems. This is required to
        // allow the XboxOne to open the required ports in its
        // manifest.
        [Obsolete("The XDK Xbox One platform was removed in 2021.1", false)]
        public static string xboxOneAdditionalDebugPorts { get; set; }
        [Obsolete("The XDK Xbox One platform was removed in 2021.1", false)]
        public static bool xboxOneRebootIfDeployFailsAndRetry { get; set; }

        // Android platform options.
        public static extern MobileTextureSubtarget androidBuildSubtarget
        {
            [NativeMethod("GetSelectedAndroidBuildTextureSubtarget")]
            get;
            [NativeMethod("SetSelectedAndroidBuildSubtarget")]
            set;
        }

        // WebGL platform options.
        public static extern WebGLTextureSubtarget webGLBuildSubtarget
        {
            [NativeMethod("GetSelectedWebGLBuildTextureSubtarget")]
            get;
            [NativeMethod("SetSelectedWebGLBuildSubtarget")]
            set;
        }

        public static extern string webGLClientBrowserPath { get; set; }

        public static extern WebGLClientBrowserType webGLClientBrowserType
        {
            [NativeMethod("GetWebGLClientBrowserType")]
            get;
            [NativeMethod("SetWebGLClientBrowserType")]
            set;
        }
        internal static extern WebGLClientPlatform webGLClientPlatform
        {
            [NativeMethod("GetWebGLClientPlatform")]
            get;
            [NativeMethod("SetWebGLClientPlatform")]
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

        [Obsolete("androidETC2Fallback is obsolete and has no effect. It will be removed in a subsequent Unity release.")]
        public static AndroidETC2Fallback androidETC2Fallback
        {
            get { return AndroidETC2Fallback.Quality32Bit; }
            set { }
        }

        public static extern AndroidBuildSystem androidBuildSystem { get; set; }

        public static extern AndroidBuildType androidBuildType { get; set; }

        [Obsolete("androidUseLegacySdkTools has been deprecated. It does not have any effect.")]
        public static extern bool androidUseLegacySdkTools { get; set; }

        [Obsolete("androidCreateSymbolsZip has been deprecated. Use UnityEditor.Android.UserBuildSettings.DebugSymbols.level property")]
        public static bool androidCreateSymbolsZip
        {
            get => androidCreateSymbols != AndroidCreateSymbols.Disabled;
            set => androidCreateSymbols = value ? AndroidCreateSymbols.Public : AndroidCreateSymbols.Disabled;
        }

        [Obsolete("androidCreateSymbols has been deprecated. Use UnityEditor.Android.UserBuildSettings.DebugSymbols.level property")]
        public static extern AndroidCreateSymbols androidCreateSymbols { get; set; }

        // *undocumented*
        // NOTE: This setting should probably not be a part of the public API as is. Atm it is used by playmode tests
        //  and applied during build post-processing. We will however move towards separating building and launching
        //  which makes it unclear when connections should be attempted. In the future, the DeploymentTargets
        //  API will most likely support connecting to named external devices, which might make this setting obsolete.
        internal static extern string androidDeviceSocketAddress { get; set; }

        internal static extern string androidCurrentDeploymentTargetId { get; set; }

        [Obsolete("EditorUserBuildSettings.wsaSubtarget is obsolete and has no effect. It will be removed in a subsequent Unity release.")]
        public static WSASubtarget wsaSubtarget
        {
            get => WSASubtarget.AnyDevice;
            set {}
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

        public static extern string wsaMinUWPSDK
        {
            [NativeMethod("GetSelectedWSAMinUWPSDK")]
            get;
            [NativeMethod("SetSelectedWSAMinUWPSDK")]
            set;
        }

        public static extern string wsaArchitecture
        {
            [NativeMethod("GetSelectedWSAArchitecture")]
            get;
            [NativeMethod("SetSelectedWSAArchitecture")]
            set;
        }

        public static extern string wsaUWPVisualStudioVersion
        {
            [NativeMethod("GetSelectedWSAUWPVSVersion")]
            get;
            [NativeMethod("SetSelectedWSAUWPVSVersion")]
            set;
        }

        public static extern string windowsDevicePortalAddress
        {
            [NativeMethod("GetWindowsDevicePortalAddress")]
            get;
            [NativeMethod("SetWindowsDevicePortalAddress")]
            set;
        }

        internal static string EncodeBase64(string plainText)
        {
            if (plainText == null)
            {
                plainText = string.Empty;
            }

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        internal static string DecodeBase64(string base64Text)
        {
            if (base64Text == null)
            {
                base64Text = string.Empty;
            }

            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64Text);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }

        public static extern string windowsDevicePortalUsername
        {
            [NativeMethod("GetWindowsDevicePortalUsername")]
            get;
            [NativeMethod("SetWindowsDevicePortalUsername")]
            set;
        }

        // WDP password is not to be saved with other settings and only stored in memory until Editor is closed
        private static string internal_windowsDevicePortalPassword;

        public static string windowsDevicePortalPassword
        {
            get
            {
                var profile = BuildProfileContext.GetActiveOrClassicBuildProfile(BuildTarget.NoTarget, StandaloneBuildSubtarget.Default, SharedPlatformSettings.k_SettingWindowsDevicePortalPassword);
                if (profile != null)
                {
                    var settings = profile.platformBuildProfile;
                    return settings.GetSharedSetting(SharedPlatformSettings.k_SettingWindowsDevicePortalPassword);

                }
                return DecodeBase64(internal_windowsDevicePortalPassword);
            }

            set
            {
                var profile = BuildProfileContext.GetActiveOrClassicBuildProfile(BuildTarget.NoTarget, StandaloneBuildSubtarget.Default, SharedPlatformSettings.k_SettingWindowsDevicePortalPassword);
                if (profile == null)
                {
                    internal_windowsDevicePortalPassword = EncodeBase64(value);
                }
                else
                {
                    if (profile.buildTarget == BuildTarget.NoTarget)
                    {
                        var sharedPlatformSettings = profile.platformBuildProfile as SharedPlatformSettings;
                        // This will sync the value to applicable classic profiles through the shared profile.
                        sharedPlatformSettings.windowsDevicePortalPassword = value;

                    }
                    else
                    {
                        var settings = profile.platformBuildProfile;
                        // SetSharedSetting() is used to avoid having to cast the active custom profile to platform profiles.
                        // This only changes the value in the active custom profile and has no effect on syncing.
                        settings.SetSharedSetting(SharedPlatformSettings.k_SettingWindowsDevicePortalPassword, value);
                    }
                }
            }
        }

        // *undocumented*
        public static extern WSABuildAndRunDeployTarget wsaBuildAndRunDeployTarget
        {
            [NativeMethod("GetSelectedWSABuildAndRunDeployTarget")]
            get;
            [NativeMethod("SetSelectedWSABuildAndRunDeployTarget")]
            set;
        }

        public static extern WindowsBuildAndRunDeployTarget windowsBuildAndRunDeployTarget
        {
            [NativeMethod("GetSelectedWindowsBuildAndRunDeployTarget")]
            get;
            [NativeMethod("SetSelectedWindowsBuildAndRunDeployTarget")]
            set;
        }
        public static extern int overrideMaxTextureSize { get; set; }
        public static extern Build.OverrideTextureCompression overrideTextureCompression { get; set; }

        // The currently active build target.
        public static extern BuildTarget activeBuildTarget { get; }

        private static extern GUID GetInternalActivePlatformGuid();

        // Internal setter only to be used for testing.
        internal static extern void SetActivePlatformGuid(GUID platformId);

        internal static GUID activePlatformGuid
        {
            get
            {
                // The following code is addressing the fact that the actually selected build target
                // and the stored GUID might disagree. This can happen when a platform is not available
                // in the editor(for example a user selects a platform, checks in that setting, another
                // one pulls the project but does not have the platform installed).
                GUID activePlatformGuid = GetInternalActivePlatformGuid();
                GUID basePlatformGuid = BuildTargetDiscovery.GetBasePlatformGUIDFromBuildTarget(EditorUserBuildSettingsUtils.CalculateActiveNamedBuildTarget(), activeBuildTarget);

                if (activePlatformGuid.Empty())
                    return basePlatformGuid;

                // Account for derived platforms (the active platform is different from its base platform).
                // Jira https://jira.unity3d.com/browse/PLAT-9234
                if (activePlatformGuid != basePlatformGuid)
                {
                    // This logic will make sure that the base platform gets selected if the encountered platform
                    // is a derived one and the platform support for the derived platform is not available to
                    // align with the behavior of the editor before introducing derived platforms.
                    var module = ModuleManager.FindPlatformSupportModule(activePlatformGuid);
                    if (module is IDerivedBuildTargetProvider)
                        return activePlatformGuid;
                    return basePlatformGuid;
                }

                return activePlatformGuid;
            }
        }

        [NativeMethod("SwitchActiveBuildTargetGuid")]
        private static extern bool SwitchActiveBuildTargetAndSubTargetGuid(GUID platformGuid, BuildTarget target, int subtarget);
        internal static bool SwitchActiveBuildTargetGuid(BuildProfile profile)
        {
            var platformGuid = profile.platformGuid;
            // Account for derived platforms.
            // Jira https://jira.unity3d.com/browse/PLAT-9234
            // The editor triggers recompilation on a build target or subtarget change already. Both of these values
            // will not change by design when switching between a derived platform and its base platform or between
            // derived platforms of the same baseplatform so we need to trigger a rebuild if the GUID changes and
            // the support module stays the same.
            if (platformGuid != activePlatformGuid && ModuleManager.FindPlatformSupportModule(platformGuid) == ModuleManager.FindPlatformSupportModule(activePlatformGuid))
            {
                BuildProfileModuleUtil.RequestScriptCompilation(null);
            }

            var (buildTargetFromGuid, subTargetFromGuid) = BuildTargetDiscovery.GetBuildTargetAndSubtargetFromGUID(platformGuid);
            int activeSubtarget = (int)subTargetFromGuid;

            //This will either get the base or derived platform to cast to correct baseplatformsetting type.
            bool result = BuildTargetDiscovery.TryGetBuildTarget(profile.buildTarget, out var buildTarget);
            if (result && buildTarget != null)
            {
                var subtarget = (buildTarget.TextureSubtargetPlatformProperties == null) ? -1 :
                    buildTarget.TextureSubtargetPlatformProperties.GetSubtargetFromPlatformSettings(profile.platformBuildProfile);
                if (subtarget != -1) activeSubtarget = subtarget;
            }


            return SwitchActiveBuildTargetAndSubTargetGuid(platformGuid, buildTargetFromGuid, activeSubtarget);
        }

        // The currently active build target.
        internal static extern BuildTargetGroup activeBuildTargetGroup { get; }

        [NativeMethod("SwitchActiveBuildTargetSync")]
        internal static extern bool SwitchActiveBuildTargetAndSubtarget(BuildTarget target, int subtarget);
        public static bool SwitchActiveBuildTarget(BuildTargetGroup targetGroup, BuildTarget target)
            => SwitchActiveBuildTargetAndSubtarget(target, EditorUserBuildSettings.GetActiveSubtargetFor(target));

        [NativeMethod("SwitchActiveBuildTargetAsync")]
        internal static extern bool SwitchActiveBuildTargetAndSubtargetAsync(BuildTarget target, int subtarget);
        public static bool SwitchActiveBuildTargetAsync(BuildTargetGroup targetGroup, BuildTarget target)
            => SwitchActiveBuildTargetAndSubtargetAsync(target, EditorUserBuildSettings.GetActiveSubtargetFor(target));

        public static bool SwitchActiveBuildTarget(NamedBuildTarget namedBuildTarget, BuildTarget target)
            => BuildPlatforms.instance.BuildPlatformFromNamedBuildTarget(namedBuildTarget).SetActive(target);

        // This is used by tests -- note that it does tell the editor that current platform is X, without
        // validating if support for it is installed. However it does not do things like script recompile
        // or domain reload -- generally only useful for asset import testing.
        [NativeMethod("SwitchActiveBuildTargetSyncNoCheck")]
        internal static extern bool SwitchActiveBuildTargetAndSubtargetNoCheck(BuildTarget target, int subtarget);
        internal static bool SwitchActiveBuildTargetNoCheck(BuildTarget target)
            => SwitchActiveBuildTargetAndSubtargetNoCheck(target, EditorUserBuildSettings.GetActiveSubtargetFor(target));

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

        [Obsolete("Use PlayerSettings.SetIl2CppCodeGeneration and PlayerSettings.GetIl2CppCodeGeneration instead.", true)]
        public static Build.Il2CppCodeGeneration il2CppCodeGeneration
        {
            get { return Build.Il2CppCodeGeneration.OptimizeSpeed; }
            set { Debug.LogWarning("EditorUserBuildSettings.il2CppCodeGeneration is obsolete. Please use PlayerSettings.SetIl2CppCodeGeneration and PlayerSettings.GetIl2CppCodeGeneration instead." ); }
        }

        [Obsolete("Building with pre-built Engine option is no longer supported.", true)]
        public static bool webGLUsePreBuiltUnityEngine
        {
            get { return false; }
            set {}
        }

        // Start the player with a connection to the profiler.
        public static extern bool connectProfiler { get; set; }

        // Build the player with deep profiler support.
        public static extern bool buildWithDeepProfilingSupport { get; set; }

        // Enable source-level debuggers to connect.
        public static extern bool allowDebugging { get; set; }

        // Wait for player connection on start
        public static extern bool waitForPlayerConnection { get; set; }

        // Export as Android Google Project instead of building it
        public static extern bool exportAsGoogleAndroidProject { get; set; }

        // Build Google Play App Bundle
        public static extern bool buildAppBundle { get; set; }

        // Symlink runtime libraries with an iOS Xcode project.
        [Obsolete("EditorUserBuildSettings.symlinkLibraries is obsolete. Use EditorUserBuildSettings.symlinkSources instead (UnityUpgradable) -> [UnityEditor] EditorUserBuildSettings.symlinkSources", false)]

        public static bool symlinkLibraries
        {
            get => symlinkSources;
            set => symlinkSources = value;
        }

        public static extern bool symlinkSources { get; set; }

        // Symlink trampoline for iOS Xcode project.
        internal static extern bool symlinkTrampoline { get; set; }


        public static extern XcodeBuildConfig iOSXcodeBuildConfig
        {
            [NativeMethod("GetIOSXcodeBuildConfig")] get;
            [NativeMethod("SetIOSXcodeBuildConfig")] set;
        }
        public static extern XcodeBuildConfig macOSXcodeBuildConfig
        {
            [NativeMethod("GetMacOSXcodeBuildConfig")] get;
            [NativeMethod("SetMacOSXcodeBuildConfig")] set;
        }

        [Obsolete("iOSBuildConfigType is obsolete. Use iOSXcodeBuildConfig instead (UnityUpgradable) -> iOSXcodeBuildConfig", true)]
        public static iOSBuildType iOSBuildConfigType
        {
            // note that the actual values of iOSBuildType and XcodeBuildConfig agree
            get => (iOSBuildType)iOSXcodeBuildConfig;
            set => iOSXcodeBuildConfig = (XcodeBuildConfig)value;
        }

        // Create a .nsp ROM file out of the loose-files .nspd folder
        public static extern bool switchCreateRomFile
        {
            [NativeMethod("GetCreateRomFileForSwitch")]
            get;
            [NativeMethod("SetCreateRomFileForSwitch")]
            set;
        }

        public static extern bool switchEnableRomCompression
        {
            [NativeMethod("GetEnableRomCompressionForSwitch")]
            get;
            [NativeMethod("SetEnableRomCompressionForSwitch")]
            set;
        }

        public static extern bool switchSaveADF
        {
            [NativeMethod("GetSaveADFForSwitch")]
            get;
            [NativeMethod("SetSaveADFForSwitch")]
            set;
        }

        public static extern SwitchRomCompressionType switchRomCompressionType
        {
            [NativeMethod("GetRomCompressionTypeForSwitch")]
            get;
            [NativeMethod("SetRomCompressionTypeForSwitch")]
            set;
        }

        public static extern int switchRomCompressionLevel
        {
            [NativeMethod("GetRomCompressionLevelForSwitch")]
            get;
            [NativeMethod("SetRomCompressionLevelForSwitch")]
            set;
        }

        public static extern string switchRomCompressionConfig
        {
            [NativeMethod("GetRomCompressionConfigForSwitch")]
            get;
            [NativeMethod("SetRomCompressionConfigForSwitch")]
            set;
        }

        // Enable linkage of NVN Graphics Debugger for Nintendo Switch.
        public static extern bool switchNVNGraphicsDebugger
        {
            [NativeMethod("GetNVNGraphicsDebuggerForSwitch")]
            get;
            [NativeMethod("SetNVNGraphicsDebuggerForSwitch")]
            set;
        }

        // Generate Nintendo Switch shader info for shader source visualization and profiling in NVN Graphics Debugger or Low-Level Graphics Debugger (LLGD)
        public static extern bool generateNintendoSwitchShaderInfo
        {
            [NativeMethod("GetGenerateNintendoSwitchShaderInfo")]
            get;
            [NativeMethod("SetGenerateNintendoSwitchShaderInfo")]
            set;
        }

        // Enable shader debugging using NVN Graphics Debugger
        public static extern bool switchNVNShaderDebugging
        {
            [NativeMethod("GetNVNShaderDebugging")]
            get;
            [NativeMethod("SetNVNShaderDebugging")]
            set;
        }

        // Enable shader debugging using NVN Graphics Debugger
        public static extern bool switchNVNAftermath
        {
            [NativeMethod("GetNVNAftermath")]
            get;
            [NativeMethod("SetNVNAftermath")]
            set;
        }

        // Enable debug validation of NVN drawcalls
        [Obsolete("switchNVNDrawValidation is deprecated, use switchNVNDrawValidation_Heavy instead.")]
        public static bool switchNVNDrawValidation
        {
            get { return switchNVNDrawValidation_Heavy; }
            set { switchNVNDrawValidation_Heavy = value; }
        }

        public static extern bool switchNVNDrawValidation_Light
        {
            [NativeMethod("GetNVNDrawValidationLight")]
            get;
            [NativeMethod("SetNVNDrawValidationLight")]
            set;
        }

        public static extern bool switchNVNDrawValidation_Heavy
        {
            [NativeMethod("GetNVNDrawValidationHeavy")]
            get;
            [NativeMethod("SetNVNDrawValidationHeavy")]
            set;
        }

        // Enable linkage of the Memory Tracker tool for Nintendo Switch.
        public static extern bool switchEnableMemoryTracker
        {
            [NativeMethod("GetEnableMemoryTrackerForSwitch")]
            get;
            [NativeMethod("SetEnableMemoryTrackerForSwitch")]
            set;
        }

        // On startup the application waits for Memory Tracker to connect.
        public static extern bool switchWaitForMemoryTrackerOnStartup
        {
            [NativeMethod("GetWaitForSwitchMemoryTrackerOnStartup")]
            get;
            [NativeMethod("SetWaitForSwitchMemoryTrackerOnStartup")]
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

        [Obsolete("EditorUserBuildSettings.switchRedirectWritesToHostMount is obsolete. Use EditorUserBuildSettings.switchEnableHostIO instead (UnityUpgradable) -> switchEnableHostIO", false)]
        public static bool switchRedirectWritesToHostMount
        {
            get => switchEnableHostIO;
            set => switchEnableHostIO = value;
        }

        // Redirect attempts to write to "rom:" mount, to "host:" mount for Nintendo Switch (for debugging and tests only)
        public static extern bool switchEnableHostIO
        {
            [NativeMethod("GetEnableHostIOForSwitch")]
            get;
            [NativeMethod("SetEnableHostIOForSwitch")]
            set;
        }

        // Enable using the HTC devkit connection for script debugging
        public static extern bool switchHTCSScriptDebugging
        {
            [NativeMethod("GetHTCSScriptDebuggingForSwitch")]
            get;
            [NativeMethod("SetHTCSScriptDebuggingForSwitch")]
            set;
        }

        public static extern bool switchUseLegacyNvnPoolAllocator
        {
            [NativeMethod("GetUseLegacyNvnPoolAllocatorForSwitch")]
            get;
            [NativeMethod("SetUseLegacyNvnPoolAllocatorForSwitch")]
            set;
        }

        public static extern bool switchEnableUnpublishableErrors
        {
            [NativeMethod("GetEnableUnpublishableErrorsForSwitch")]
            get;
            [NativeMethod("SetEnableUnpublishableErrorsForSwitch")]
            set;
        }

        internal static extern SwitchShaderCompilerConfig switchShaderCompilerConfig
        {
            [NativeMethod("GetSwitchShaderCompilerConfig")]
            get;
            [NativeMethod("SetSwitchShaderCompilerConfig")]
            set;
        }

        // Place the built player in the build folder.
        public static extern bool installInBuildFolder { get; set; }

        public static bool waitForManagedDebugger
        {
            get
            {
                return GetPlatformSettings("Editor", BuildProfilePlatformSettingsBase.k_SettingWaitForManagedDebugger) == "true";
            }

            set
            {
                SetPlatformSettings("Editor", BuildProfilePlatformSettingsBase.k_SettingWaitForManagedDebugger, value.ToString().ToLower());
            }
        }

        public static int managedDebuggerFixedPort
        {
            get
            {
                if (Int32.TryParse(GetPlatformSettings("Editor", BuildProfilePlatformSettingsBase.k_SettingManagedDebuggerFixedPort), out int value)) {
                    if (0 < value && value <= 65535)
                    {
                        return value;
                    }
                }
                return 0;
            }

            set
            {
                SetPlatformSettings("Editor", BuildProfilePlatformSettingsBase.k_SettingManagedDebuggerFixedPort, value.ToString().ToLower());
            }
        }

        internal static extern bool isBuildProfileAvailable { get; set; }
        internal static BuildProfile activeBuildProfile
        {
            get => GetActiveBuildProfile() as BuildProfile;
            set => SetActiveBuildProfile(value);
        }

        private static extern ScriptableObject GetActiveBuildProfile();
        private static extern void SetActiveBuildProfile(ScriptableObject buildProfile);

        internal static extern void CopyFromBuildProfile(ScriptableObject buildProfile);
        internal static extern void CopyToBuildProfile(ScriptableObject buildProfile);

        internal static extern string[] GetActiveProfileScriptingDefines();
        internal static extern void SetActiveProfileScriptingDefines(string[] defines);
    }
}
