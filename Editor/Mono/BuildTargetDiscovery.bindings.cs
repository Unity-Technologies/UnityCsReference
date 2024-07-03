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
            return "";
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

        static readonly GUID s_platform_02 = new("0d2129357eac403d8b359c2dcbf82502");
        static readonly GUID s_platform_05 = new("4e3c793746204150860bf175a9a41a05");
        static readonly GUID s_platform_09 = new("ad48d16a66894befa4d8181998c3cb09");
        static readonly GUID s_platform_13 = new("b9b35072a6f44c2e863f17467ea3dc13");
        static readonly GUID s_platform_20 = new("84a3bb9e7420477f885e98145999eb20");
        static readonly GUID s_platform_21 = new("32e92b6f4db44fadb869cafb8184d021");
        static readonly GUID s_platform_24 = new("cb423bfea44b4d658edb8bc5d91a3024");
        static readonly GUID s_platform_31 = new("5d4f9b64eeb74b18a2de0de6f0c36931");
        static readonly GUID s_platform_37 = new("81e4f4c492fd4311bbf5b0b88a28c737");
        static readonly GUID s_platform_38 = new("08d61a9cdfb840119d9bea5588e2f338");
        static readonly GUID s_platform_41 = new("f188349a68c441ec9e3eb4c6f59abd41");
        static readonly GUID s_platform_42 = new("c9f186cd3d594a1496bca1860359f842");
        static readonly GUID s_platform_43 = new("a6f1094111614cba85f0508bf9778843");
        static readonly GUID s_platform_44 = new("e30a9c34166844499a56eaa4ed115c44");
        static readonly GUID s_platform_45 = new("f1d7bec2fd7f42f481c66ef512f47845");
        static readonly GUID s_platform_46 = new("99ef95e1e9b048fa9628d7eed27a8646");
        static readonly GUID s_platform_47 = new("53916e6f1f7240d992977ffa2322b047");
        static readonly GUID s_platform_48 = new("25a09d2ed10c42f789b61d99b4d9bf83");
        static readonly GUID s_platform_100 = new("8d1e1bca926649cba89d37a4c66e8b3d");
        static readonly GUID s_platform_101 = new("91d938b35f6f4798811e41f2acf9377f");
        static readonly GUID s_platform_102 = new("8659dec1db6b4fac86149f99f2fa4291");

        static readonly Dictionary<GUID, bool> k_PlatformInstalledData = new();

        // This list is ordered by the order in which platforms are displayed in the build profiles window
        // Changes here should be synced with kBuildTargetUIOrder[] in BuildTargetDiscovery.cpp
        static GUID[] allPlatforms { get; } = new GUID[]
        {
            // first standalones and servers
            s_platform_05,
            s_platform_02,
            s_platform_24,
            s_platform_100,
            s_platform_102,
            s_platform_101,
            // then mobile
            s_platform_13,
            s_platform_09,
            // then consoles
            s_platform_31,
            s_platform_44,
            s_platform_42,
            s_platform_43,
            s_platform_38,
            s_platform_48,
            // then others
            s_platform_20,
            s_platform_21,
            s_platform_37,
            s_platform_47,
            s_platform_41,
            s_platform_45,
            s_platform_46,
        };

        static GUID[] WindowsBuildTargets { get; } = new GUID[]
        {
            s_platform_02,
            s_platform_05,
            s_platform_09,
            s_platform_13,
            s_platform_20,
            s_platform_21,
            s_platform_24,
            s_platform_31,
            s_platform_37,
            s_platform_38,
            s_platform_41,
            s_platform_42,
            s_platform_43,
            s_platform_44,
            s_platform_45,
            s_platform_46,
            s_platform_47,
            s_platform_48,
            s_platform_100,
            s_platform_101,
            s_platform_102,
        };

        static GUID[] WindowsARM64BuildTargets { get; } = new GUID[]
        {
            s_platform_02,
            s_platform_05,
            s_platform_09,
            s_platform_13,
            s_platform_20,
            s_platform_21,
            s_platform_24,
            s_platform_37,
            s_platform_41,
            s_platform_46,
            s_platform_47,
            s_platform_100,
            s_platform_101,
            s_platform_102,
        };

        static GUID[] MacBuildTargets { get; } = new GUID[]
        {
            s_platform_02,
            s_platform_05,
            s_platform_09,
            s_platform_13,
            s_platform_20,
            s_platform_24,
            s_platform_37,
            s_platform_41,
            s_platform_45,
            s_platform_46,
            s_platform_47,
            s_platform_100,
            s_platform_101,
            s_platform_102,
        };

        static GUID[] LinuxBuildTargets { get; } = new GUID[]
        {
            s_platform_02,
            s_platform_05,
            s_platform_09,
            s_platform_13,
            s_platform_20,
            s_platform_24,
            s_platform_37,
            s_platform_41,
            s_platform_45,
            s_platform_46,
            s_platform_47,
            s_platform_100,
            s_platform_101,
            s_platform_102,
        };

        static GUID[] NDABuildTargets { get; } = new GUID[]
        {
            s_platform_31,
            s_platform_44,
            s_platform_38,
            s_platform_48,
        };

        static GUID[] ExternalDownloadForBuildTarget { get; } = new GUID[]
        {
            s_platform_31,
            s_platform_43,
            s_platform_44,
            s_platform_48,
        };

        // Changes here should be synced with the usage of
        // BuildTargetDiscovery::HideInUI flag in [Platform]BuildTarget.cpp
        internal static GUID[] hiddenPlatforms { get; } = new GUID[]
        {
            s_platform_38,
            s_platform_41,
            s_platform_42,
            s_platform_43,
            s_platform_45,
            s_platform_46,
            s_platform_47,
            s_platform_48,
        };

        static Dictionary<BuildTarget, GUID> s_PlatformGUIDData = new()
        {
            { BuildTarget.StandaloneOSX, s_platform_02 },
            { BuildTarget.StandaloneWindows, s_platform_05 },
            { BuildTarget.StandaloneWindows64, s_platform_05 }, // return same build target GUID for Win and Win64 since we only have one
            { BuildTarget.iOS, s_platform_09 },
            { BuildTarget.Android, s_platform_13 },
            { BuildTarget.WebGL, s_platform_20 },
            { BuildTarget.WSAPlayer, s_platform_21 },
            { BuildTarget.StandaloneLinux64, s_platform_24 },
            { BuildTarget.PS4, s_platform_31 },
            { BuildTarget.tvOS, s_platform_37 },
            { BuildTarget.Switch, s_platform_38 },
            { BuildTarget.LinuxHeadlessSimulation, s_platform_41 },
            { BuildTarget.GameCoreXboxSeries, s_platform_42 },
            { BuildTarget.GameCoreXboxOne, s_platform_43 },
            { BuildTarget.PS5, s_platform_44 },
            { BuildTarget.EmbeddedLinux, s_platform_45 },
            { BuildTarget.QNX, s_platform_46 },
            { BuildTarget.VisionOS, s_platform_47 },
            { BuildTarget.ReservedCFE, s_platform_48 },
        };

        static readonly Dictionary<GUID, (BuildTarget, StandaloneBuildSubtarget)> k_PlatformBuildTargetAndSubtargetGUIDData = new()
        {
            { s_platform_02, (BuildTarget.StandaloneOSX, StandaloneBuildSubtarget.Player) },
            { s_platform_05, (BuildTarget.StandaloneWindows64, StandaloneBuildSubtarget.Player) },
            { s_platform_09, (BuildTarget.iOS, StandaloneBuildSubtarget.Default) },
            { s_platform_13, (BuildTarget.Android, StandaloneBuildSubtarget.Default) },
            { s_platform_20, (BuildTarget.WebGL, StandaloneBuildSubtarget.Default) },
            { s_platform_21, (BuildTarget.WSAPlayer, StandaloneBuildSubtarget.Default) },
            { s_platform_24, (BuildTarget.StandaloneLinux64, StandaloneBuildSubtarget.Player) },
            { s_platform_31, (BuildTarget.PS4, StandaloneBuildSubtarget.Default) },
            { s_platform_37, (BuildTarget.tvOS, StandaloneBuildSubtarget.Default) },
            { s_platform_38, (BuildTarget.Switch, StandaloneBuildSubtarget.Default) },
            { s_platform_41, (BuildTarget.LinuxHeadlessSimulation, StandaloneBuildSubtarget.Default) },
            { s_platform_42, (BuildTarget.GameCoreXboxSeries, StandaloneBuildSubtarget.Default) },
            { s_platform_43, (BuildTarget.GameCoreXboxOne, StandaloneBuildSubtarget.Default) },
            { s_platform_44, (BuildTarget.PS5, StandaloneBuildSubtarget.Default) },
            { s_platform_45, (BuildTarget.EmbeddedLinux, StandaloneBuildSubtarget.Default) },
            { s_platform_46, (BuildTarget.QNX, StandaloneBuildSubtarget.Default) },
            { s_platform_47, (BuildTarget.VisionOS, StandaloneBuildSubtarget.Default) },
            { s_platform_48, (BuildTarget.ReservedCFE, StandaloneBuildSubtarget.Default) },
            { s_platform_101, (BuildTarget.StandaloneLinux64, StandaloneBuildSubtarget.Server) },
            { s_platform_100, (BuildTarget.StandaloneWindows64, StandaloneBuildSubtarget.Server) },
            { s_platform_102, (BuildTarget.StandaloneOSX, StandaloneBuildSubtarget.Server) },
        };

        static Dictionary<GUID, string> s_PlatformRequiredPackages = new()
        {
            {  s_platform_45, L10n.Tr("") }, //https://github.cds.internal.unity3d.com/unity/unity/blob/690ff735df474658b18a6ce362b64384dd09a889/PlatformDependent/EmbeddedLinux/Extensions/Managed/EmbeddedLinuxToolchainPackageInstaller.cs#L71 and https://github.cds.internal.unity3d.com/unity/unity/blob/690ff735df474658b18a6ce362b64384dd09a889/PlatformDependent/LinuxStandalone/Extensions/Managed/LinuxStandaloneToolchainPackageInstaller.cs#L16
        };

        static Dictionary<GUID, string> s_PlatformRecommendedPackages = new()
        {
            {  s_platform_45, L10n.Tr("") },
        };

        static Dictionary<GUID, string> s_PlatformDescription = new()
        {
            {  s_platform_02, L10n.Tr("Take advantage of Unity’s support for the latest Mac devices with M series chips. The Mac Standalone platform also supports Intel-based Mac devices.") },
            {  s_platform_05, L10n.Tr("Access an ecosystem of Unity-supported game development solutions to reach the vast PC gamer audience around the world. Leverage DirectX 12 and inline ray tracing support for cutting edge visual fidelity. Use the Microsoft GDK packages to further unlock the Microsoft gaming ecosystem.") },
            {  s_platform_09, L10n.Tr("Benefit from Unity’s longstanding and wide-ranging resources for the entire development lifecycle for iOS games. This includes tools and services for rapid iteration, performance optimization, player engagement, and revenue growth.") },
            {  s_platform_13, L10n.Tr("Android is a large and varied device ecosystem with over 3bn active devices. Benefit from Unity’s longstanding and wide-ranging resources for the entire development lifecycle for Android games. This includes tools and services for rapid iteration, performance optimization, player engagement, and revenue growth.") },
            {  s_platform_20, L10n.Tr("Leverage Unity’s web solutions to offer your players near-instant access to their favorite games, no matter where they want to play. Our web platform includes support for desktop and mobile browsers.") },
            {  s_platform_21, L10n.Tr("Benefit from Unity’s runtime support for UWP, ensuring you’re able to reach as many users as possible in the Microsoft ecosystem. UWP is used for HoloLens and Windows 10 and 11 devices, among others.") },
            {  s_platform_24, L10n.Tr("Leverage Unity’s platform support for Linux, including an ecosystem of game development solutions for creators of all skill levels.") },
            {  s_platform_31, L10n.Tr("Create your game with a comprehensive development platform for PlayStation®4. Discover powerful creation tools to take your PlayStation game to the next level.") },
            {  s_platform_37, L10n.Tr("Choose tvOS if you’re planning to develop applications for Apple TVs. tvOS is based on the iOS operating system and has many similar frameworks, technologies, and concepts.") },
            {  s_platform_38, L10n.Tr("Bring your game to Nintendo Switch™ with Unity’s optimized platform support as well as a dedicated forum.") },
            {  s_platform_41, L10n.Tr("Utilize Unity’s headless Linux editor to deploy high-fidelity simulations at scale in cloud environments.") },
            {  s_platform_42, L10n.Tr("Attract players around the world on the latest generation of Xbox: Xbox Series X|S. Push the graphical fidelity of your games with inline ray tracing, all while maintaining performance with the latest optimizations for DirectX 12.") },
            {  s_platform_43, L10n.Tr("Attract and engage over 50 million players around the world on Xbox One.") },
            {  s_platform_44, L10n.Tr("Create your game with a comprehensive development platform for PlayStation®5. Discover powerful creation tools to take your PlayStation game to the next level.") },
            {  s_platform_45, L10n.Tr("Choose Embedded Linux, a compact version of Linux, if you are planning to build applications for embedded devices and appliances.") },
            {  s_platform_46, L10n.Tr("Deploy the Unity runtime to automotive and other embedded systems utilizing the Blackberry® QNX® real-time operating system.") },
            {  s_platform_47, L10n.Tr("Build for Apple Vision Pro today.\nBe among the first to create games, lifestyle experiences, and industry apps for Apple's all-new platform. Familiar frameworks and tools. Get ready to design and build an entirely new universe of apps and games for Apple Vision Pro.")},
            {  s_platform_48, L10n.Tr("Benefit from Unity’s support for developing games and applications on this platform") },
            {  s_platform_100, L10n.Tr("Benefit from Unity’s support for developing games and applications on the Dedicated Windows Server platform, including publishing multiplayer games.") },
            {  s_platform_101, L10n.Tr("Benefit from Unity’s support for developing games and applications on the Dedicated Linux Server platform, including publishing multiplayer games.") },
            {  s_platform_102, L10n.Tr("Benefit from Unity’s support for developing games and applications on the Dedicated Mac Server platform, including publishing multiplayer games.") },
        };

        static Dictionary<GUID, string> s_PlatformLink = new()
        {
            {  s_platform_13, L10n.Tr("Unity Android Manual / https://docs.unity3d.com/Manual/android.html") },
            {  s_platform_31, L10n.Tr("Register as a PlayStation developer / https://partners.playstation.net/ ") },
            {  s_platform_38, L10n.Tr("Register as a Nintendo developer / http://developer.nintendo.com ") },
            {  s_platform_42, L10n.Tr("Register as an Xbox developer / https://www.xbox.com/en-US/developers/id ") },
            {  s_platform_43, L10n.Tr("Register as an Xbox developer / https://www.xbox.com/en-US/developers/id ") },
            {  s_platform_44, L10n.Tr("Register as a PlayStation developer / https://partners.playstation.net/ ") },
            {  s_platform_48, L10n.Tr("More details coming soon") },
        };

        static Dictionary<GUID, string> s_PlatformInstructions = new()
        {
            {  s_platform_02, L10n.Tr("*standard install form hub") },
            {  s_platform_05, L10n.Tr("*standard install form hub") },
            {  s_platform_09, L10n.Tr("*standard install form hub") },
            {  s_platform_13, L10n.Tr("*standard install form hub") },
            {  s_platform_20, L10n.Tr("*standard install form hub") },
            {  s_platform_21, L10n.Tr("*standard install form hub") },
            {  s_platform_24, L10n.Tr("*standard install form hub") },
            {  s_platform_31, L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more.") },
            {  s_platform_37, L10n.Tr("*standard install form hub") },
            {  s_platform_38, L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more.") },
            {  s_platform_41, L10n.Tr("*standard install form hub") },
            {  s_platform_42, L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more.") },
            {  s_platform_43, L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more.") },
            {  s_platform_44, L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more.") },
            {  s_platform_45, L10n.Tr("As the Embedded Linux platform for Unity is not yet available to download from the Unity website, contact your Account Manager or the Unity Sales team to get access.") },
            {  s_platform_46, L10n.Tr("As the QNX platform for Unity is not yet available to download from the Unity website, contact your Account Manager or the Unity Sales team to get access.") },
            {  s_platform_47, L10n.Tr("*standard install form hub") },
            {  s_platform_48, L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more.") },
            {  s_platform_100, L10n.Tr("*standard install form hub") },
            {  s_platform_101, L10n.Tr("*standard install form hub") },
            {  s_platform_102, L10n.Tr("*standard install form hub") },
        };

        // Name changes here must be reflected in the platform [PLATFORM]BuildTarget.cs and [Platform]BuildTarget.cpp respective DisplayName and niceName
        static Dictionary<GUID, string> s_PlatformDisplayName = new()
        {
            {  s_platform_02, "macOS" },
            {  s_platform_05, "Windows" },
            {  s_platform_09, "iOS" },
            {  s_platform_13, "Android™" },
            {  s_platform_20, "Web" },
            {  s_platform_21, "Universal Windows Platform" },
            {  s_platform_24, "Linux" },
            {  s_platform_31, "PlayStation®4" },
            {  s_platform_37, "tvOS" },
            {  s_platform_38, "Nintendo Switch™" },
            {  s_platform_41, "Linux Headless Simulation" },
            {  s_platform_42, "Xbox Series X|S" },
            {  s_platform_43, "Xbox One" },
            {  s_platform_44, "PlayStation®5" },
            {  s_platform_45, "Embedded Linux" },
            {  s_platform_46, "QNX®" },
            {  s_platform_47, "visionOS" },
            {  s_platform_48, "ReservedCFE" },
            {  s_platform_100, "Windows Server" },
            {  s_platform_101, "Linux Server" },
            {  s_platform_102, "macOS Server" },
        };

        // Changes here should be synced with [Platform]BuildTarget.cpp respective iconName
        static readonly Dictionary<GUID, string> k_PlatformIconName = new()
        {
            { s_platform_02, "BuildSettings.OSX" },
            { s_platform_05, "BuildSettings.Windows" },
            { s_platform_09, "BuildSettings.iPhone" },
            { s_platform_13, "BuildSettings.Android" },
            { s_platform_20, "BuildSettings.WebGL" },
            { s_platform_21, "BuildSettings.Metro" },
            { s_platform_24, "BuildSettings.Linux" },
            { s_platform_31, "BuildSettings.PS4" },
            { s_platform_37, "BuildSettings.tvOS" },
            { s_platform_38, "BuildSettings.Switch" },
            { s_platform_41, "BuildSettings.LinuxHeadlessSimulation" },
            { s_platform_42, "BuildSettings.GameCoreScarlett" },
            { s_platform_43, "BuildSettings.GameCoreXboxOne" },
            { s_platform_44, "BuildSettings.PS5" },
            { s_platform_45, "BuildSettings.EmbeddedLinux" },
            { s_platform_46, "BuildSettings.QNX" },
            { s_platform_47, "BuildSettings.visionOS" },
            { s_platform_48, "BuildSettings.DedicatedServer" },
            { s_platform_100, "BuildSettings.DedicatedServer" },
            { s_platform_101, "BuildSettings.DedicatedServer" },
            { s_platform_102, "BuildSettings.DedicatedServer" },
        };

        static BuildTargetDiscovery()
        {
            PreloadBuildPlatformInstalledData();
        }

        public static GUID[] GetAllPlatforms() => allPlatforms;

        public static GUID GetGUIDFromBuildTarget(BuildTarget buildTarget)
        {
            if (s_PlatformGUIDData.TryGetValue(buildTarget, out GUID value))
                return value;

            return new GUID("");
        }

        public static GUID GetGUIDFromBuildTarget(NamedBuildTarget namedBuildTarget, BuildTarget buildTarget)
        {
            if (namedBuildTarget == NamedBuildTarget.Server)
            {
                if (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64)
                    return s_platform_100;
                else if (buildTarget == BuildTarget.StandaloneLinux64)
                    return s_platform_101;
                else if (buildTarget == BuildTarget.StandaloneOSX)
                    return s_platform_102;
            }

            return GetGUIDFromBuildTarget(buildTarget);
        }

        public static (BuildTarget, StandaloneBuildSubtarget) GetBuildTargetAndSubtargetFromGUID(GUID guid)
        {
            if (k_PlatformBuildTargetAndSubtargetGUIDData.TryGetValue(guid, out var value))
                return value;

            return (BuildTarget.NoTarget, StandaloneBuildSubtarget.Default);
        }

        static void PreloadBuildPlatformInstalledData()
        {
            foreach (var platform in allPlatforms)
            {
                var (buildTarget, _) = GetBuildTargetAndSubtargetFromGUID(platform);
                var playbackEngineDirectory = BuildPipeline.GetPlaybackEngineDirectory(buildTarget, BuildOptions.None, false);

                if (string.IsNullOrEmpty(playbackEngineDirectory))
                {
                    k_PlatformInstalledData.Add(platform, false);
                    continue;
                }

                if (!IsStandalonePlatform(buildTarget))
                {
                    k_PlatformInstalledData.Add(platform, true);
                    continue;
                }

                bool isInstalled;
                if (platform == s_platform_100)
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
                else if (platform == s_platform_101)
                {
                    var serverVariations = new[]
                    {
                        "linux64_server_development",
                        "linux64_server_nondevelopment",
                    };
                    isInstalled = VariationPresent(serverVariations, playbackEngineDirectory);
                }
                else if (platform == s_platform_102)
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

                k_PlatformInstalledData.Add(platform, isInstalled);
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
        public static bool BuildPlatformIsInstalled(BuildTarget platform) =>
            BuildPlatformIsInstalled(GetGUIDFromBuildTarget(platform));

        public static bool BuildPlatformIsInstalled(IBuildTarget platform) =>
            BuildPlatformIsInstalled(platform.Guid);

        public static bool BuildPlatformIsInstalled(GUID platformGuid)
        {
            if (k_PlatformInstalledData.TryGetValue(platformGuid, out bool isInstalled))
                return isInstalled;

            return false;
        }

        [System.Obsolete("BuildPlatformIsAvailableOnHostPlatform(BuildTarget) is obsolete. Use BuildPlatformIsAvailableOnHostPlatform(IBuildTarget) instead.", false)]
        public static bool BuildPlatformIsAvailableOnHostPlatform(BuildTarget platform, UnityEngine.OperatingSystemFamily hostPlatform) => BuildPlatformIsAvailableOnHostPlatform(GetGUIDFromBuildTarget(platform), hostPlatform);
        public static bool BuildPlatformIsAvailableOnHostPlatform(IBuildTarget platform, UnityEngine.OperatingSystemFamily hostPlatform) => BuildPlatformIsAvailableOnHostPlatform(platform.Guid, hostPlatform);

        public static bool BuildPlatformIsAvailableOnHostPlatform(GUID platformGuid, UnityEngine.OperatingSystemFamily hostPlatform)
        {
            var platformTargetArray = WindowsBuildTargets;

            if (hostPlatform == UnityEngine.OperatingSystemFamily.Windows && IsWindowsArm64Architecture())
                platformTargetArray = WindowsARM64BuildTargets;
            else if (hostPlatform == UnityEngine.OperatingSystemFamily.MacOSX)
                platformTargetArray = MacBuildTargets;
            else if (hostPlatform == UnityEngine.OperatingSystemFamily.Linux)
                platformTargetArray = LinuxBuildTargets;

            foreach (var platformTarget in platformTargetArray)
                if (platformTarget == platformGuid)
                    return true;

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
        public static bool BuildPlatformCanBeInstalledWithHub(BuildTarget platform)
        {
            foreach (var target in ExternalDownloadForBuildTarget)
                if (target == GetGUIDFromBuildTarget(platform))
                    return false;

            return true;
        }

        public static bool BuildPlatformCanBeInstalledWithHub(IBuildTarget platform)
        {
            foreach (var invisibleTarget in ExternalDownloadForBuildTarget)
                if (invisibleTarget == platform.Guid)
                    return false;

            return true;
        }

        [System.Obsolete("BuildPlatformIsUnderNDA(BuildTarget) is obsolete. Use BuildPlatformIsUnderNDA(IBuildTarget) instead.", false)]

        public static bool BuildPlatformIsUnderNDA(BuildTarget platform)
        {
            foreach (var ndaTarget in NDABuildTargets)
                if (ndaTarget == GetGUIDFromBuildTarget(platform))
                    return true;

            return false;
        }

        public static bool BuildPlatformIsUnderNDA(IBuildTarget platform)
        {
            foreach (var ndaTarget in NDABuildTargets)
                if (ndaTarget == platform.Guid)
                    return true;

            return false;
        }

        [System.Obsolete("BuildPlatformIsHiddenInUI(BuildTarget) is obsolete. Use BuildPlatformIsHiddenInUI(IBuildTarget) instead.", false)]
        public static bool BuildPlatformIsHiddenInUI(BuildTarget platform) =>
            BuildPlatformIsHiddenInUI(GetGUIDFromBuildTarget(platform));

        public static bool BuildPlatformIsHiddenInUI(IBuildTarget platform) =>
            BuildPlatformIsHiddenInUI(platform.Guid);

        public static bool BuildPlatformIsHiddenInUI(GUID platformGuid)
        {
            foreach (var hiddenPlatform in hiddenPlatforms)
                if (hiddenPlatform == platformGuid)
                    return true;

            return false;
        }

        [System.Obsolete("BuildPlatformRecommendeddPackages(BuildTarget) is obsolete. Use BuildPlatformRecommendeddPackages(IBuildTarget) instead.", false)]
        public static string BuildPlatformRecommendeddPackages(BuildTarget platform)
        {
            if (s_PlatformRecommendedPackages.TryGetValue(GetGUIDFromBuildTarget(platform), out string recommendedPackages))
                return recommendedPackages;

            return "";
        }

        public static string BuildPlatformRecommendeddPackages(IBuildTarget platform)
        {
            if (s_PlatformRecommendedPackages.TryGetValue(platform.Guid, out string recommendedPackages))
                return recommendedPackages;

            return "";
        }

        [System.Obsolete("BuildPlatformDescription(BuildTarget) is obsolete. Use BuildPlatformDescription(IBuildTarget) instead.", false)]

        public static string BuildPlatformDescription(BuildTarget platform)
        {
            if (s_PlatformDescription.TryGetValue(GetGUIDFromBuildTarget(platform), out string description))
                return description;

            return string.Empty;
        }

        public static string BuildPlatformDescription(IBuildTarget platform)
        {
            if (s_PlatformDescription.TryGetValue(platform.Guid, out string description))
                return description;

            return string.Empty;
        }

        public static string BuildPlatformDescription(GUID platformGuid)
        {
            if (s_PlatformDescription.TryGetValue(platformGuid, out string description))
                return description;

            return string.Empty;
        }

        [System.Obsolete("BuildPlatformDocumentationLink(BuildTarget) is obsolete. Use BuildPlatformDocumentationLink(IBuildTarget) instead.", false)]

        public static string BuildPlatformDocumentationLink(BuildTarget platform)
        {
            if (s_PlatformLink.TryGetValue(GetGUIDFromBuildTarget(platform), out string link))
                return link;

            return "";
        }

        public static string BuildPlatformDocumentationLink(IBuildTarget platform)
        {
            if (s_PlatformLink.TryGetValue(platform.Guid, out string link))
                return link;

            return "";
        }

        [System.Obsolete("BuildPlatformOnboardingInstructions(BuildTarget) is obsolete. Use BuildPlatformOnboardingInstructions(IBuildTarget) instead.", false)]

        public static string BuildPlatformOnboardingInstructions(BuildTarget platform)
        {
            if (s_PlatformInstructions.TryGetValue(GetGUIDFromBuildTarget(platform), out string instructions))
                return instructions;

            return "";
        }

        public static string BuildPlatformOnboardingInstructions(IBuildTarget platform)
        {
            if (s_PlatformInstructions.TryGetValue(platform.Guid, out string instructions))
                return instructions;

            return "";
        }

        [System.Obsolete("BuildPlatformDisplayName(BuildTarget) is obsolete. Use BuildPlatformDisplayName(IBuildTarget) instead.", false)]
        public static string BuildPlatformDisplayName(BuildTarget platform) =>
            BuildPlatformDisplayName(GetGUIDFromBuildTarget(platform));

        public static string BuildPlatformDisplayName(IBuildTarget platform) =>
            BuildPlatformDisplayName(platform.Guid);

        public static string BuildPlatformDisplayName(GUID platformGuid)
        {
            if (s_PlatformDisplayName.TryGetValue(platformGuid, out string displayName))
                return displayName;

            return string.Empty;
        }

        [System.Obsolete("BuildPlatformDisplayName(BuildTarget) is obsolete. Use BuildPlatformDisplayName(IBuildTarget) instead.", false)]
        public static string BuildPlatformDisplayName(NamedBuildTarget namedBuildTarget, BuildTarget buildTarget)
        {
            var guid = GetGUIDFromBuildTarget(buildTarget);

            if (namedBuildTarget == NamedBuildTarget.Server)
            {
                if (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64)
                    guid = s_platform_100;
                else if (buildTarget == BuildTarget.StandaloneLinux64)
                    guid = s_platform_101;
                else if (buildTarget == BuildTarget.StandaloneOSX)
                    guid = s_platform_102;
            }
            if (s_PlatformDisplayName.TryGetValue(guid, out string displayName))
                return displayName;

            return "";
        }

        [System.Obsolete("BuildPlatformIconName(BuildTarget) is obsolete. Use BuildPlatformIconName(IBuildTarget) instead.", false)]
        public static string BuildPlatformIconName(BuildTarget platform) =>
            BuildPlatformIconName(GetGUIDFromBuildTarget(platform));

        public static string BuildPlatformIconName(IBuildTarget platform) =>
            BuildPlatformIconName(platform.Guid);

        public static string BuildPlatformIconName(GUID platformGuid)
        {
            if (k_PlatformIconName.TryGetValue(platformGuid, out string iconName))
                return iconName;

            return "";
        }
    }
}
