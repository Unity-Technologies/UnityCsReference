// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Build;
using UnityEditor.Utils;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;

namespace UnityEditor
{
    [StaticAccessor("BuildTargetDiscovery::GetInstance()", StaticAccessorType.Dot)]
    [NativeHeader("Editor/Src/BuildPipeline/BuildTargetDiscovery.h")]
    [RequiredByNativeCode]
    internal static class BuildTargetDiscovery
    {
        [Flags]
        public enum TargetAttributes
        {
            None                            = 0,
            IsDeprecated                    = (1 << 0),
            HasIntegratedGPU                = (1 << 1),
            IsConsole                       = (1 << 2),
            IsX64                           = (1 << 3),
            IsStandalonePlatform            = (1 << 4),
            DynamicBatchingDisabled         = (1 << 5),
            CompressedGPUSkinningDisabled   = (1 << 6),
            UseForsythOptimizedMeshData     = (1 << 7),
            DisableEnlighten                = (1 << 8),
            // unused in 2023LTS: ReflectionEmitDisabled          = (1 << 9),
            OSFontsDisabled                 = (1 << 10),
            NoDefaultUnityFonts             = (1 << 11),
            // removed in 2019.3: SupportsFacebook = (1 << 12),
            WarnForMouseEvents              = (1 << 13),
            HideInUI                        = (1 << 14),
            GPUSkinningNotSupported         = (1 << 15),
            StrippingNotSupported           = (1 << 16),
            DisableNativeHDRLightmaps       = (1 << 17),
            UsesNativeHDR                   = (1 << 18),
            // removed: ProtectedGraphicsMem = (1 << 19),
            IsMTRenderingDisabledByDefault  = (1 << 20),
            ConfigurableNormalMapEncoding   = (1 << 21),
            ConfigurableDefaultTextureCompressionFormat = (1 << 22)
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DiscoveredTargetInfo
        {
            public string moduleName;
            public string dirName;
            public string platformDefine;
            public string niceName;
            public string iconName;
            public string assemblyName;

            public BuildTarget buildTargetPlatformVal;

            // Build targets can have many names to identify them
            public string[] nameList;

            // Build targets can sometimes support more than one renderer
            public int[] rendererList;

            public TargetAttributes flags;

            public bool HasFlag(TargetAttributes flag) { return (flags & flag) == flag; }
        }

        public static extern bool PlatformHasFlag(BuildTarget platform, TargetAttributes flag);

        public static extern bool PlatformGroupHasFlag(BuildTargetGroup group, TargetAttributes flag);

        public static extern DiscoveredTargetInfo[] GetBuildTargetInfoList();

        public static extern BuildTarget GetBuildTargetByName(string name);

        public static extern int[] GetRenderList(BuildTarget platform);

        public static extern string GetModuleNameForBuildTarget(BuildTarget platform);

        public static extern string GetModuleNameForBuildTargetGroup(BuildTargetGroup group);

        public static string GetPlatformProfileSuffix(BuildTarget buildTarget)
        {
            if (BuildTargetDiscovery.TryGetBuildTarget(buildTarget, out var iBuildTarget))
            {
                return iBuildTarget.RootSystemType;
            }
            return string.Empty;
        }

        public static bool BuildTargetSupportsRenderer(BuildPlatform platform, GraphicsDeviceType type)
        {
            BuildTarget buildTarget = platform.defaultTarget;
            if (platform.namedBuildTarget == NamedBuildTarget.Standalone)
                buildTarget = DesktopStandaloneBuildWindowExtension.GetBestStandaloneTarget(buildTarget);

            foreach (int var in GetRenderList(buildTarget))
            {
                if ((GraphicsDeviceType)var == type)
                    return true;
            }

            return false;
        }

        public static string GetScriptAssemblyName(DiscoveredTargetInfo btInfo)
        {
            return !String.IsNullOrEmpty(btInfo.assemblyName) ? btInfo.assemblyName : btInfo.nameList[0];
        }

        public static bool TryGetBuildTarget(BuildTarget platform, out IBuildTarget buildTarget)
        {
            buildTarget = Modules.ModuleManager.GetIBuildTarget(platform);
            return buildTarget != null;
        }

        public static bool TryGetProperties<T>(BuildTarget platform, out T properties) where T : IPlatformProperties
        {
            if (TryGetBuildTarget(platform, out var buildTarget))
            {
                return buildTarget.TryGetProperties(out properties);
            }
            properties = default(T);
            return false;
        }

        public static BuildTarget[] StandaloneBuildTargets { get; internal set; } = new BuildTarget[]
        {
            BuildTarget.StandaloneOSX,
            BuildTarget.StandaloneWindows,
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneLinux64,
        };

        static bool IsStandalonePlatform(BuildTarget buildTarget)
        {
            foreach (var target in StandaloneBuildTargets)
            {
                if (target == buildTarget)
                    return true;
            }

            return false;
        }

        [RequiredByNativeCode]
        internal static bool DoesBuildTargetSupportStereoInstancingRendering(BuildTarget buildTarget)
        {
            if (BuildTargetDiscovery.TryGetBuildTarget(buildTarget, out var iBuildTarget))
            {
                return iBuildTarget.VRPlatformProperties?.SupportStereoInstancingRendering ?? false;
            }
            return false;
        }

        [RequiredByNativeCode]
        internal static bool DoesBuildTargetSupportStereoMultiviewRendering(BuildTarget buildTarget)
        {
            if (BuildTargetDiscovery.TryGetBuildTarget(buildTarget, out var iBuildTarget))
            {
                return iBuildTarget.VRPlatformProperties?.SupportStereoMultiviewRendering ?? false;
            }
            return false;
        }

        [RequiredByNativeCode]
        internal static bool DoesBuildTargetSupportStereo360Capture(BuildTarget buildTarget)
        {
            if (BuildTargetDiscovery.TryGetBuildTarget(buildTarget, out var iBuildTarget))
            {
                return iBuildTarget.VRPlatformProperties?.SupportStereo360Capture ?? false;
            }
            return false;
        }

        [RequiredByNativeCode]
        internal static bool DoesBuildTargetSupportSinglePassStereoRendering(BuildTarget buildTarget)
        {
            if (BuildTargetDiscovery.TryGetBuildTarget(buildTarget, out var iBuildTarget))
            {
                return iBuildTarget.VRPlatformProperties?.SupportSinglePassStereoRendering ?? false;
            }
            return false;
        }

        [Flags]
        enum PlatformAttributes
        {
            None = 0,
            IsDeprecated = (1 << 0),
            IsWindowsBuildTarget = (1 << 1),
            IsWindowsArm64BuildTarget = (1 << 2),
            IsMacBuildTarget = (1 << 3),
            IsLinuxBuildTarget = (1 << 4),
            IsMobileBuildTarget = (1 << 5),
            IsNDAPlatform = (1 << 6),
            IsHidden = (1 << 7),
            ExternalDownloadForBuildTarget = (1 << 8),
            IsWindowsServerBuildTarget = (1 << 9),
            IsMacServerBuildTarget = (1 << 10),
            IsLinuxServerBuildTarget = (1 << 11),
            IsVisibleInPlatformBrowserOnly = (1 << 12),
            IsDerivedBuildTarget = (1 << 13),
        }

        struct PlatformInfo
        {
            public string displayName = String.Empty;
            public BuildTarget buildTarget = BuildTarget.NoTarget;
            public StandaloneBuildSubtarget subtarget = StandaloneBuildSubtarget.Default;
            public PlatformAttributes flags = PlatformAttributes.None;

            public string[] requiredPackage = new string[] {};
            public string[] recommendedPackage = new string[] {};
            public string description = L10n.Tr("");
            public string link = L10n.Tr("");
            public string instructions = L10n.Tr("*standard install form hub");
            public string iconName = "BuildSettings.Editor";
            public PlatformInfo(){}

            public bool HasFlag(PlatformAttributes flag) { return (flags & flag) == flag; }
        }


        static GUID EmptyGuid = new GUID("");

        static Dictionary<BuildTarget, GUID> s_PlatformGUIDDataQuickLookup = new Dictionary<BuildTarget, GUID>();

        // This list should not be exposed ouside of BuildTargetDiscovery to avoid NDA spillage, provide a access function for data here instead.
        // Changes here should be synced with the usage of
        // BuildTargetDiscovery::HideInUI flag in [Platform]BuildTarget.cpp
        // This list is ordered by the order in which platforms are displayed in the build profiles window (Do not change!)
        // Changes here should be synced with kBuildTargetUIOrder[] in BuildTargetDiscovery.cpp
        // Name changes here must be reflected in the platform [PLATFORM]BuildTarget.cs and [Platform]BuildTarget.cpp respective DisplayName, niceName and iconName
        static readonly Dictionary<GUID, PlatformInfo> allPlatforms = new Dictionary<GUID, PlatformInfo>
        {
            // first standalones and servers
            {
                new("4e3c793746204150860bf175a9a41a05"),
                new PlatformInfo
                {
                    displayName = "Windows",
                    description = L10n.Tr("Access an ecosystem of Unity-supported game development solutions to reach the vast PC gamer audience around the world. Leverage DirectX 12 and inline ray tracing support for cutting edge visual fidelity. Use the Microsoft GDK packages to further unlock the Microsoft gaming ecosystem."),
                    subtarget = StandaloneBuildSubtarget.Player,
                    buildTarget = BuildTarget.StandaloneWindows64,
                    iconName = "BuildSettings.Windows",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("0d2129357eac403d8b359c2dcbf82502"),
                new PlatformInfo
                {
                    displayName = "macOS",
                    description = L10n.Tr("Take advantage of Unity’s support for the latest Mac devices with M series chips. The Mac Standalone platform also supports Intel-based Mac devices."),
                    subtarget = StandaloneBuildSubtarget.Player,
                    buildTarget = BuildTarget.StandaloneOSX,
                    iconName = "BuildSettings.OSX",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("cb423bfea44b4d658edb8bc5d91a3024"),
                new PlatformInfo
                {
                    displayName = "Linux",
                    description = L10n.Tr("Leverage Unity’s platform support for Linux, including an ecosystem of game development solutions for creators of all skill levels."),
                    subtarget = StandaloneBuildSubtarget.Player,
                    buildTarget = BuildTarget.StandaloneLinux64,
                    iconName = "BuildSettings.Linux",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            // Server platforms
            {
                new("8d1e1bca926649cba89d37a4c66e8b3d"),
                new PlatformInfo
                {
                    displayName = "Windows Server",
                    description = L10n.Tr("Benefit from Unity’s support for developing games and applications on the Dedicated Windows Server platform, including publishing multiplayer games."),
                    buildTarget = BuildTarget.StandaloneWindows64,
                    subtarget = StandaloneBuildSubtarget.Server,
                    iconName = "BuildSettings.DedicatedServer",
                    requiredPackage = new string[]{L10n.Tr("com.unity.dedicated-server") },
                    flags = PlatformAttributes.IsWindowsServerBuildTarget | PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("8659dec1db6b4fac86149f99f2fa4291"),
                new PlatformInfo
                {
                    displayName = "macOS Server",
                    description = L10n.Tr("Benefit from Unity’s support for developing games and applications on the Dedicated Mac Server platform, including publishing multiplayer games."),
                    buildTarget = BuildTarget.StandaloneOSX,
                    subtarget = StandaloneBuildSubtarget.Server,
                    iconName = "BuildSettings.DedicatedServer",
                    requiredPackage = new string[]{L10n.Tr("com.unity.dedicated-server") },
                    flags = PlatformAttributes.IsMacServerBuildTarget | PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("91d938b35f6f4798811e41f2acf9377f"),
                new PlatformInfo
                {
                    displayName = "Linux Server",
                    description =  L10n.Tr("Benefit from Unity’s support for developing games and applications on the Dedicated Linux Server platform, including publishing multiplayer games."),
                    buildTarget = BuildTarget.StandaloneLinux64,
                    subtarget = StandaloneBuildSubtarget.Server,
                    iconName = "BuildSettings.DedicatedServer",
                    requiredPackage = new string[]{L10n.Tr("com.unity.dedicated-server") },
                    flags = PlatformAttributes.IsLinuxServerBuildTarget | PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            // then mobile
            {
                new("b9b35072a6f44c2e863f17467ea3dc13"),
                new PlatformInfo
                {
                    displayName = "Android™",
                    description = L10n.Tr("Android is a large and varied device ecosystem with over 3bn active devices. Benefit from Unity’s longstanding and wide-ranging resources for the entire development lifecycle for Android games. This includes tools and services for rapid iteration, performance optimization, player engagement, and revenue growth."),
                    buildTarget = BuildTarget.Android,
                    link = L10n.Tr("Unity Android Manual / https://docs.unity3d.com/Manual/android.html"),
                    iconName = "BuildSettings.Android",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("ad48d16a66894befa4d8181998c3cb09"),
                new PlatformInfo
                {
                    displayName = "iOS",
                    description =  L10n.Tr("Benefit from Unity’s longstanding and wide-ranging resources for the entire development lifecycle for iOS games. This includes tools and services for rapid iteration, performance optimization, player engagement, and revenue growth."),
                    buildTarget = BuildTarget.iOS,
                    iconName = "BuildSettings.iPhone",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            // then consoles
            {
                new("5d4f9b64eeb74b18a2de0de6f0c36931"),
                new PlatformInfo
                {
                    displayName = "PlayStation®4",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Create your game with a comprehensive development platform for PlayStation®4. Discover powerful creation tools to take your PlayStation game to the next level."),
                    buildTarget = BuildTarget.PS4,
                    link = L10n.Tr("Register as a PlayStation developer / https://partners.playstation.net/ "),
                    iconName = "BuildSettings.PS4",
                    flags = PlatformAttributes.ExternalDownloadForBuildTarget | PlatformAttributes.IsNDAPlatform | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            {
                new("e30a9c34166844499a56eaa4ed115c44"),
                new PlatformInfo
                {
                    displayName = "PlayStation®5",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Create your game with a comprehensive development platform for PlayStation®5. Discover powerful creation tools to take your PlayStation game to the next level."),
                    buildTarget = BuildTarget.PS5,
                    link = L10n.Tr("Register as a PlayStation developer / https://partners.playstation.net/ "),
                    iconName = "BuildSettings.PS5",
                    flags = PlatformAttributes.ExternalDownloadForBuildTarget | PlatformAttributes.IsNDAPlatform | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            {
                new("c9f186cd3d594a1496bca1860359f842"),
                new PlatformInfo
                {
                    displayName = "Xbox Series X|S",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Attract players around the world on the latest generation of Xbox: Xbox Series X|S. Push the graphical fidelity of your games with inline ray tracing, all while maintaining performance with the latest optimizations for DirectX 12."),
                    buildTarget = BuildTarget.GameCoreXboxSeries,
                    link = L10n.Tr("Register as an Xbox developer / https://www.xbox.com/en-US/developers/id "),
                    iconName = "BuildSettings.GameCoreScarlett",
                    flags = PlatformAttributes.IsHidden | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            {
                new("a6f1094111614cba85f0508bf9778843"),
                new PlatformInfo
                {
                    displayName = "Xbox One",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Attract and engage over 50 million players around the world on Xbox One."),
                    buildTarget = BuildTarget.GameCoreXboxOne,
                    link = L10n.Tr("Register as an Xbox developer / https://www.xbox.com/en-US/developers/id "),
                    iconName = "BuildSettings.GameCoreXboxOne",
                    flags = PlatformAttributes.ExternalDownloadForBuildTarget | PlatformAttributes.IsHidden | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            {
                new("08d61a9cdfb840119d9bea5588e2f338"),
                new PlatformInfo
                {
                    displayName = "Nintendo Switch™",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Bring your game to Nintendo Switch™ with Unity’s optimized platform support as well as a dedicated forum."),
                    buildTarget = BuildTarget.Switch,
                    link = L10n.Tr("Register as a Nintendo developer / http://developer.nintendo.com "),
                    iconName = "BuildSettings.Switch",
                    flags = PlatformAttributes.IsHidden | PlatformAttributes.IsNDAPlatform | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            {
               new("25a09d2ed10c42f789b61d99b4d9bf83"),
               new PlatformInfo
               {
                    displayName = "ReservedCFE",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description =  L10n.Tr("Benefit from Unity’s support for developing games and applications on this platform"),
                    buildTarget = BuildTarget.ReservedCFE,
                    link = L10n.Tr("More details coming soon"),
                    flags = PlatformAttributes.ExternalDownloadForBuildTarget | PlatformAttributes.IsHidden | PlatformAttributes.IsNDAPlatform | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            // then others
            {
                new("84a3bb9e7420477f885e98145999eb20"),
                new PlatformInfo
                {
                    displayName = "Web",
                    description =  L10n.Tr("Leverage Unity’s web solutions to offer your players near-instant access to their favorite games, no matter where they want to play. Our web platform includes support for desktop and mobile browsers."),
                    buildTarget = BuildTarget.WebGL,
                    iconName = "BuildSettings.WebGL",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("32e92b6f4db44fadb869cafb8184d021"),
                new PlatformInfo
                {
                    displayName = "Universal Windows Platform",
                    description = L10n.Tr("Benefit from Unity’s runtime support for UWP, ensuring you’re able to reach as many users as possible in the Microsoft ecosystem. UWP is used for HoloLens and Windows 10 and 11 devices, among others."),
                    buildTarget = BuildTarget.WSAPlayer,
                    iconName = "BuildSettings.Metro",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget
                }
            },
            {
                new("81e4f4c492fd4311bbf5b0b88a28c737"),
                new PlatformInfo
                {
                    displayName = "tvOS",
                    description = L10n.Tr("Choose tvOS if you’re planning to develop applications for Apple TVs. tvOS is based on the iOS operating system and has many similar frameworks, technologies, and concepts."),
                    buildTarget = BuildTarget.tvOS,
                    iconName = "BuildSettings.tvOS",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("53916e6f1f7240d992977ffa2322b047"),
                new PlatformInfo
                {
                    displayName = "visionOS",
                    description = L10n.Tr("Build for Apple Vision Pro today.\nBe among the first to create games, lifestyle experiences, and industry apps for Apple's all-new platform. Familiar frameworks and tools. Get ready to design and build an entirely new universe of apps and games for Apple Vision Pro."),
                    buildTarget = BuildTarget.VisionOS,
                    iconName = "BuildSettings.visionOS",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("f188349a68c441ec9e3eb4c6f59abd41"),
                new PlatformInfo
                {
                    displayName = "Linux Headless Simulation",
                    description = L10n.Tr("Utilize Unity’s headless Linux editor to deploy high-fidelity simulations at scale in cloud environments."),
                    buildTarget = BuildTarget.LinuxHeadlessSimulation,
                    iconName = "BuildSettings.LinuxHeadlessSimulation",
                    flags = PlatformAttributes.IsHidden | PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("f1d7bec2fd7f42f481c66ef512f47845"),
                new PlatformInfo
                {
                    displayName = "Embedded Linux",
                    instructions = L10n.Tr("As the Embedded Linux platform for Unity is not yet available to download from the Unity website, contact your Account Manager or the Unity Sales team to get access."),
                    description = L10n.Tr("Choose Embedded Linux, a compact version of Linux, if you are planning to build applications for embedded devices and appliances."),
                    buildTarget = BuildTarget.EmbeddedLinux,
                    iconName = "BuildSettings.EmbeddedLinux",
                    flags = PlatformAttributes.IsHidden | PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("99ef95e1e9b048fa9628d7eed27a8646"),
                new PlatformInfo
                {
                    displayName = "QNX®",
                    instructions = L10n.Tr("As the QNX platform for Unity is not yet available to download from the Unity website, contact your Account Manager or the Unity Sales team to get access."),
                    description = L10n.Tr("Deploy the Unity runtime to automotive and other embedded systems utilizing the Blackberry® QNX® real-time operating system."),
                    buildTarget = BuildTarget.QNX,
                    iconName = "BuildSettings.QNX",
                    flags = PlatformAttributes.IsHidden | PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            }
        };

        static readonly Dictionary<GUID, bool> k_PlatformInstalledData = new();


        static BuildTargetDiscovery()
        {
            PreloadBuildPlatformInstalledData();
        }
        public static IEnumerable<GUID> GetAllPlatforms() => allPlatforms.Keys;

        public static GUID GetGUIDFromBuildTarget(BuildTarget buildTarget)
        {
            if (buildTarget == BuildTarget.StandaloneWindows) //workaround for win64 and win having the same guid in new Build Target system
                buildTarget = BuildTarget.StandaloneWindows64;

             if (s_PlatformGUIDDataQuickLookup.TryGetValue(buildTarget, out GUID value))
                return value;

            return EmptyGuid;
        }

        public static GUID GetGUIDFromBuildTarget(NamedBuildTarget namedBuildTarget, BuildTarget buildTarget)
        {
            if (namedBuildTarget == NamedBuildTarget.Server)
            {
                foreach (var platform in allPlatforms)
                {
                    if(platform.Value.subtarget == StandaloneBuildSubtarget.Server)
                    {
                        if((buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64) && platform.Value.HasFlag(PlatformAttributes.IsWindowsServerBuildTarget))
                            return platform.Key;
                        else if(buildTarget == BuildTarget.StandaloneLinux64 && platform.Value.HasFlag(PlatformAttributes.IsLinuxServerBuildTarget))
                            return platform.Key;
                        else if(buildTarget == BuildTarget.StandaloneOSX && platform.Value.HasFlag(PlatformAttributes.IsMacServerBuildTarget))
                            return platform.Key;
                    }
                }
            }
            return GetGUIDFromBuildTarget(buildTarget);
        }

        public static (BuildTarget, StandaloneBuildSubtarget) GetBuildTargetAndSubtargetFromGUID(GUID guid)
        {
            if(allPlatforms.TryGetValue(guid,out PlatformInfo platformInfo))
                return (platformInfo.buildTarget, platformInfo.subtarget);

            return (BuildTarget.NoTarget, StandaloneBuildSubtarget.Default);
        }

        static void PreloadBuildPlatformInstalledData()
        {
            foreach (var platform in allPlatforms)
            {
                if (platform.Value.buildTarget != BuildTarget.StandaloneWindows
                    && platform.Value.subtarget != StandaloneBuildSubtarget.Server
                    && !platform.Value.HasFlag(PlatformAttributes.IsDerivedBuildTarget)
                    ) //workaround for win64 and win having the same guid in new Build Target system and for derived build targets
                {
                    s_PlatformGUIDDataQuickLookup.Add(platform.Value.buildTarget, platform.Key);
                }

                var playbackEngineDirectory = BuildPipeline.GetPlaybackEngineDirectory(platform.Value.buildTarget, BuildOptions.None, false);

                if (string.IsNullOrEmpty(playbackEngineDirectory))
                {
                    k_PlatformInstalledData.Add(platform.Key, false);
                    continue;
                }

                if (!IsStandalonePlatform(platform.Value.buildTarget))
                {
                    k_PlatformInstalledData.Add(platform.Key, true);
                    continue;
                }

                bool isInstalled = false;
                if (platform.Value.HasFlag(PlatformAttributes.IsWindowsServerBuildTarget))
                {
                    var serverVariations = new[]
                    {
                        "win32_server_development",
                        "win32_server_nondevelopment",
                        "win64_server_development",
                        "win64_server_nondevelopment",
                        "win_arm64_server_development",
                        "win_arm64_server_nondevelopment",
                    };
                    isInstalled = VariationPresent(serverVariations, playbackEngineDirectory);
                }
                else if (platform.Value.HasFlag(PlatformAttributes.IsLinuxServerBuildTarget))
                {
                    var serverVariations = new[]
                    {
                        "linux64_server_development",
                        "linux64_server_nondevelopment",
                    };
                    isInstalled = VariationPresent(serverVariations, playbackEngineDirectory);
                }
                else if (platform.Value.HasFlag(PlatformAttributes.IsMacServerBuildTarget))
                {
                    var serverVariations = new[]
                    {
                        "macos_x64_server_development",
                        "macos_x64_server_nondevelopment",
                        "macos_arm64_server_development",
                        "macos_arm64_server_nondevelopment",
                        "macos_x64arm64_server_development",
                        "macos_x64arm64_server_nondevelopment",
                    };
                    isInstalled = VariationPresent(serverVariations, playbackEngineDirectory);
                }
                else
                    isInstalled = true;

                k_PlatformInstalledData.Add(platform.Key, isInstalled);
            }
        }

        static bool VariationPresent(string[] variations, string playbackEngineDirectory)
        {
            if (string.IsNullOrEmpty(playbackEngineDirectory))
                return false;

            var scriptingBackends = new[] { "mono", "il2cpp", "coreclr" };
            foreach (var variation in variations)
            {
                foreach (var backend in scriptingBackends)
                {
                    if (Directory.Exists(Paths.Combine(playbackEngineDirectory, "Variations", variation + "_" + backend)))
                        return true;
                }
            }

            return false;
        }

        [System.Obsolete("BuildPlatformIsInstalled(BuildTarget) is obsolete. Use BuildPlatformIsInstalled(IBuildTarget) instead.", false)]
        public static bool BuildPlatformIsInstalled(BuildTarget platform) => BuildPlatformIsInstalled(GetGUIDFromBuildTarget(platform));

        public static bool BuildPlatformIsInstalled(IBuildTarget platform) => BuildPlatformIsInstalled(platform.Guid);

        public static bool BuildPlatformIsInstalled(GUID platformGuid)
        {
            if (k_PlatformInstalledData.TryGetValue(platformGuid, out bool isInstalled))
                return isInstalled;

            return false;
        }

        [System.Obsolete("BuildPlatformIsAvailableOnHostPlatform(BuildTarget) is obsolete. Use BuildPlatformIsAvailableOnHostPlatform(IBuildTarget) instead.", false)]
        public static bool BuildPlatformIsAvailableOnHostPlatform(BuildTarget platform, UnityEngine.OperatingSystemFamily hostPlatform) => BuildPlatformIsAvailableOnHostPlatform(GetGUIDFromBuildTarget(platform), hostPlatform);
        public static bool BuildPlatformIsAvailableOnHostPlatform(IBuildTarget platform, UnityEngine.OperatingSystemFamily hostPlatform) => BuildPlatformIsAvailableOnHostPlatform(platform.Guid, hostPlatform);

        public static bool BuildPlatformIsAvailableOnHostPlatform(GUID guid, UnityEngine.OperatingSystemFamily hostPlatform)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
            {
                if (hostPlatform == UnityEngine.OperatingSystemFamily.Windows && IsWindowsArm64Architecture() && platformInfo.HasFlag(PlatformAttributes.IsWindowsArm64BuildTarget))
                    return true;
                if (hostPlatform == UnityEngine.OperatingSystemFamily.Windows && platformInfo.HasFlag(PlatformAttributes.IsWindowsBuildTarget))
                    return true;
                else if (hostPlatform == UnityEngine.OperatingSystemFamily.MacOSX && platformInfo.HasFlag(PlatformAttributes.IsMacBuildTarget))
                    return true;
                else if (hostPlatform == UnityEngine.OperatingSystemFamily.Linux && platformInfo.HasFlag(PlatformAttributes.IsLinuxBuildTarget))
                    return true;
            }

            return false;
        }

        static bool IsWindowsArm64Architecture()
        {
            // Based on WindowsUtility.GetHostOSArchitecture() in platform dependent code
            // We can't use RuntimeInformation.OSArchitecture because it doesn't work on emulations
            var architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE", EnvironmentVariableTarget.Machine);
            if (string.IsNullOrEmpty(architecture))
                return false;

            architecture = architecture.ToLowerInvariant();
            return architecture.Contains("arm64") || architecture.Contains("aarch64");
        }

        [System.Obsolete("BuildPlatformCanBeInstalledWithHub(BuildTarget) is obsolete. Use BuildPlatformCanBeInstalledWithHub(IBuildTarget) instead.", false)]
        public static bool BuildPlatformCanBeInstalledWithHub(BuildTarget platform) => BuildPlatformCanBeInstalledWithHub(GetGUIDFromBuildTarget(platform));

        public static bool BuildPlatformCanBeInstalledWithHub(IBuildTarget platform) => BuildPlatformCanBeInstalledWithHub(platform.Guid);

        public static bool BuildPlatformCanBeInstalledWithHub(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo) && platformInfo.HasFlag(PlatformAttributes.ExternalDownloadForBuildTarget))
                return false;

            return true;
        }

        [System.Obsolete("BuildPlatformIsUnderNDA(BuildTarget) is obsolete. Use BuildPlatformIsUnderNDA(IBuildTarget) instead.", false)]

        public static bool BuildPlatformIsUnderNDA(BuildTarget platform) => BuildPlatformIsUnderNDA(GetGUIDFromBuildTarget(platform));

        public static bool BuildPlatformIsUnderNDA(IBuildTarget platform) => BuildPlatformIsUnderNDA(platform.Guid);

        public static bool BuildPlatformIsUnderNDA(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo) && platformInfo.HasFlag(PlatformAttributes.IsNDAPlatform))
                return true;

            return false;
        }

        [System.Obsolete("BuildPlatformIsHiddenInUI(BuildTarget) is obsolete. Use BuildPlatformIsHiddenInUI(IBuildTarget) instead.", false)]
        public static bool BuildPlatformIsHiddenInUI(BuildTarget platform) => BuildPlatformIsHiddenInUI(GetGUIDFromBuildTarget(platform));

        public static bool BuildPlatformIsHiddenInUI(IBuildTarget platform) => BuildPlatformIsHiddenInUI(platform.Guid);

        public static bool BuildPlatformIsHiddenInUI(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo) && platformInfo.HasFlag(PlatformAttributes.IsHidden))
                return true;

            return false;
        }

        [System.Obsolete("BuildPlatformRecommendeddPackages(BuildTarget) is obsolete. Use BuildPlatformRecommendeddPackages(IBuildTarget) instead.", false)]
        public static string[] BuildPlatformRecommendedPackages(BuildTarget platform) => BuildPlatformRecommendedPackages(GetGUIDFromBuildTarget(platform));

        public static string[] BuildPlatformRecommendedPackages(IBuildTarget platform) => BuildPlatformRecommendedPackages(platform.Guid);

        public static string[] BuildPlatformRecommendedPackages(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                    return platformInfo.recommendedPackage;

            return Array.Empty<string>();
        }

        [System.Obsolete("BuildPlatformDescription(BuildTarget) is obsolete. Use BuildPlatformDescription(IBuildTarget) instead.", false)]

        public static string BuildPlatformDescription(BuildTarget platform) => BuildPlatformDescription(GetGUIDFromBuildTarget(platform));

        public static string BuildPlatformDescription(IBuildTarget platform) => BuildPlatformDescription(platform.Guid);

        public static string BuildPlatformDescription(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.description;

            return string.Empty;
        }

        [System.Obsolete("BuildPlatformDocumentationLink(BuildTarget) is obsolete. Use BuildPlatformDocumentationLink(IBuildTarget) instead.", false)]
        public static string BuildPlatformDocumentationLink(BuildTarget platform) => BuildPlatformDocumentationLink(GetGUIDFromBuildTarget(platform));

        public static string BuildPlatformDocumentationLink(IBuildTarget platform) => BuildPlatformDocumentationLink(platform.Guid);

        public static string BuildPlatformDocumentationLink(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.link;

            return string.Empty;
        }

        [System.Obsolete("BuildPlatformOnboardingInstructions(BuildTarget) is obsolete. Use BuildPlatformOnboardingInstructions(IBuildTarget) instead.", false)]
        public static string BuildPlatformOnboardingInstructions(BuildTarget platform) => BuildPlatformOnboardingInstructions(GetGUIDFromBuildTarget(platform));

        public static string BuildPlatformOnboardingInstructions(IBuildTarget platform) => BuildPlatformOnboardingInstructions(platform.Guid);

        public static string BuildPlatformOnboardingInstructions(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.instructions;

            return string.Empty;
        }

        [System.Obsolete("BuildPlatformDisplayName(BuildTarget) is obsolete. Use BuildPlatformDisplayName(IBuildTarget) instead.", false)]
        public static string BuildPlatformDisplayName(BuildTarget platform) => BuildPlatformDisplayName(GetGUIDFromBuildTarget(platform));

        public static string BuildPlatformDisplayName(IBuildTarget platform) => BuildPlatformDisplayName(platform.Guid);

        public static string BuildPlatformDisplayName(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.displayName;

            return string.Empty;
        }

        [System.Obsolete("BuildPlatformDisplayName(NamedBuildTarget, BuildTarget) is obsolete. Use BuildPlatformDisplayName(IBuildTarget) instead.", false)]
        public static string BuildPlatformDisplayName(NamedBuildTarget namedBuildTarget, BuildTarget buildTarget) => BuildPlatformDisplayName(GetGUIDFromBuildTarget(namedBuildTarget, buildTarget));

        [System.Obsolete("BuildPlatformIconName(BuildTarget) is obsolete. Use BuildPlatformIconName(IBuildTarget) instead.", false)]
        public static string BuildPlatformIconName(BuildTarget platform) => BuildPlatformIconName(GetGUIDFromBuildTarget(platform));

        public static string BuildPlatformIconName(IBuildTarget platform) => BuildPlatformIconName(platform.Guid);

        public static string BuildPlatformIconName(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.iconName;

            return string.Empty;
        }

        public static bool BuildPlatformIsVisibleInPlatformBrowserOnly(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo) && platformInfo.HasFlag(PlatformAttributes.IsVisibleInPlatformBrowserOnly))
                return true;

            return false;
        }
    }
}
