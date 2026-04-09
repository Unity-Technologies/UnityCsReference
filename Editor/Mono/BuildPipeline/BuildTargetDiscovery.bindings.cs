// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Build;
using UnityEditor.PackageManager.UI.Internal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;
using UnityEditor.Modules;
using UnityEngine;

namespace UnityEditor
{
    [StaticAccessor("BuildTargetDiscovery::GetInstance()", StaticAccessorType.Dot)]
    [NativeHeader("Editor/Src/BuildPipeline/BuildTargetDiscovery.h")]
    [RequiredByNativeCode]
    [VisibleToOtherModules("UnityEditor.BuildProfileModule", "UnityEditor.BurstModule")]
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

        internal static string k_ResourcesSDKPlatformInfoPath => Path.Combine(EditorApplication.applicationContentsPath, "Resources/SDKPlatformInfo");

        static readonly string k_SDKPlatformInfoFailToParseError = L10n.Tr("Failed to parse SDK platform manifest '{0}': {1}.");
        static readonly string k_SDKPlatformInfoFailToParseUnknownError = L10n.Tr("Failed to parse SDK platform manifest '{0}'.");
        static readonly string k_SDKPlatformInfoMissingGuidError = L10n.Tr("SDK platform manifest '{0}' is missing a valid guid.");
        static readonly string k_SDKPlatformInfoInvalidVersionError = L10n.Tr("SDK platform manifest '{0}' has an invalid version ({1}).");
        static readonly string k_SDKPlatformGuidAlreadyUsedError = L10n.Tr("SDK platform guid '{0}' is already used by another platform.");
        static readonly string k_SDKPlatformMissingBaseGuidError = L10n.Tr("SDK platform '{0}' is a derived platform but is missing a valid base platform guid.");
        static readonly string k_SDKPlatformNoSupportedGuidsError = L10n.Tr("SDK platform '{0}' is a multi-target platform but has no supported platform guids.");
        static readonly string k_SDKPlatformMissingDisplayNameWarning = L10n.Tr("SDK platform '{0}' is missing a display name.");
        static readonly string k_SDKPlatformMissingPlatformGroupWarning = L10n.Tr("SDK platform '{0}' does not reference any platform group. The platform was registered but will not appear in any group.");
        static readonly string k_SDKPlatformUnknownPlatformGroupWarning = L10n.Tr("SDK platform '{0}' references unknown platform group '{1}'. The platform was registered but will not appear in any group.");
        internal static readonly string k_SDKProviderMissingPlatformInfoError = L10n.Tr("The SDK platform provider '{0}' does not reference a valid platform.");
        static readonly string k_SDKProviderNotMultiTargetError = L10n.Tr("The SDK platform provider '{0}' with guid '{1}' references a platform that is not marked as a multi-target platform.");
        internal static readonly string k_SDKProviderNotDerivedTargetError = L10n.Tr("The SDK platform provider '{0}' with guid '{1}' references a platform that is not marked as a derived platform.");
        static readonly string k_CreateIPlatformProviderFailedError = L10n.Tr("Failed to create IPlatformProvider instance for type '{0}'.");

        public static extern bool PlatformHasFlag(BuildTarget platform, TargetAttributes flag);

        public static extern bool PlatformGroupHasFlag(BuildTargetGroup group, TargetAttributes flag);

        public static extern DiscoveredTargetInfo[] GetBuildTargetInfoList();

        internal static DiscoveredTargetInfo[] GetDerivedBuildTargetInfoList(BuildTarget target)
        {
            List<DiscoveredTargetInfo> discoveredTargetInfos = new();
            var module = ModuleManager.FindPlatformSupportModule(ModuleManager.GetTargetStringFrom(target));
            if (module is IDerivedBuildTargetProvider derivedBuildTargetProvider)
            {
                var derivedBuildTargets = derivedBuildTargetProvider.GetDerivedBuildTargets();
                foreach (var derivedBuildTarget in derivedBuildTargets)
                {
                    if (derivedBuildTargetProvider.TryGetDiscoveredTargetInfo(derivedBuildTarget.Guid, out var discoveredTargetInfo))
                        discoveredTargetInfos.Add(discoveredTargetInfo);
                }
            }
            return discoveredTargetInfos.ToArray();
        }

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

        public static bool TryGetBuildTarget(GUID guid, out IBuildTarget buildTarget)
        {
            buildTarget = ModuleManager.GetIBuildTarget(guid);
            return buildTarget != null;
        }

        public static bool TryGetBuildTarget(BuildTarget platform, out IBuildTarget buildTarget)
        {
            buildTarget = Modules.ModuleManager.GetIBuildTarget(platform);
            return buildTarget != null;
        }

        internal static bool TryGetBuildTarget(NamedBuildTarget named, out IBuildTarget outTarget)
        {
            // For standalone, skip direct lookup and go straight to the detailed search
            if (named.TargetName != "Standalone")
            {
                var direct = BuildPipeline.GetBuildTargetByName(named.TargetName);
                if (TryGetBuildTarget(direct, out outTarget) && outTarget.IconPlatformProperties != null)
                    return true;
            }

            // look through every known platform
            foreach (var info in BuildTargetDiscovery.GetBuildTargetInfoList())
            {
                if (Array.IndexOf(info.nameList, named.TargetName) < 0)
                    continue;

                if (TryGetBuildTarget(info.buildTargetPlatformVal, out outTarget))
                    return true;
            }

            outTarget = null;
            return false;
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

        internal static bool IsStandalonePlatform(BuildTarget buildTarget)
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
        internal enum PlatformAttributes
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
            IsMultiTargetPlatform = (1 << 14),
            IsSDKPlatform = (1 << 15)
        }
        public record struct NameAndLink(string name, string linkUrl);

        internal struct PlatformInfo
        {
            public string displayName = String.Empty;
            public string downloadLinkName = String.Empty;
            public BuildTarget buildTarget = BuildTarget.NoTarget;
            public StandaloneBuildSubtarget subtarget = StandaloneBuildSubtarget.Default;
            public GUID[] supportedPlatformGuids = Array.Empty<GUID>();
            public PlatformAttributes flags = PlatformAttributes.None;

            /// <summary>
            /// To be used when fetching relevant platform specific colored icons or backgrounds.
            /// </summary>
            public string buildProfilePlatformBannerBgColorHex = "#00000000";

            /// <summary>
            /// List of Unity-maintained required and recommended packages for a platform.
            /// </summary>
            public PlatformPackageList internalPackages = new PlatformPackageList();

            /// <summary>
            /// List of Partner-maintained required and recommended packages for a platform.
            /// </summary>
            public PlatformPackageList partnerPackages = new PlatformPackageList();

            public string description = string.Empty;
            public string instructions = L10n.Tr("*standard install form hub");
            public string iconName = "BuildSettings.Editor";
            public string subtitle = string.Empty;
            public string settingsDocsLink = string.Empty;
            public List<NameAndLink> nameAndLinkToShowUnderTitle = null;
            public string keyFeatures = string.Empty;
            public string resources = string.Empty;

            // TODO: this is a workaround for onboarding instructions to fix EmbeddedLinux and QNX
            // needs to be removed when https://jira.unity3d.com/browse/PLAT-7721 is implemented
            public NameAndLink? temporaryLabelAndLinkForIndustrialOnboarding = null;

            public PlatformInfo() {}

            public bool HasFlag(PlatformAttributes flag) { return (flags & flag) == flag; }
        }

        [Serializable]
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal class PlatformPackageList
        {
            public int packageCount => requiredPackages.Length + recommendedPackages.Length;
            public PlatformPackageInfo[] requiredPackages = Array.Empty<PlatformPackageInfo>();
            public PlatformPackageInfo[] recommendedPackages = Array.Empty<PlatformPackageInfo>();
        }

        [Serializable]
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal class PlatformPackageInfo
        {
            public string displayName;
            public string qualifiedName;
            public string description;
            public string publisher;
            public bool hasThumbnail;

            public PlatformPackageInfo(string displayName, string qualifiedName, string description, string publisher = "", bool hasThumbnail = false)
            {
                this.displayName = displayName;
                this.qualifiedName = qualifiedName;
                this.description = description;
                this.publisher = publisher;
                this.hasThumbnail = hasThumbnail;
            }
        }

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal struct PlatformGroup
        {
            public string groupName;
            public GUID[] platforms;

            public PlatformGroup() { }
        }

        static GUID EmptyGuid = new GUID("");

        static Dictionary<BuildTarget, GUID> s_BuildTargetToPlatformGUID = new Dictionary<BuildTarget, GUID>();

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
                    downloadLinkName = "Windows-Mono",
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
                    downloadLinkName = "Mac-Mono",
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
                    downloadLinkName = "Linux-Mono",
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
                    downloadLinkName = "Windows-Server",
                    description = L10n.Tr("Benefit from Unity’s support for developing games and applications on the Dedicated Windows Server platform, including publishing multiplayer games."),
                    buildTarget = BuildTarget.StandaloneWindows64,
                    subtarget = StandaloneBuildSubtarget.Server,
                    iconName = "BuildSettings.DedicatedServer",
                    internalPackages = new PlatformPackageList
                    {
                        requiredPackages = new[]
                        {
                            new PlatformPackageInfo(L10n.Tr("Dedicated Server"), "com.unity.dedicated-server", L10n.Tr("The Dedicated Server package contains optimizations and workflow improvements for developing Dedicated Server platform.")),
                        }
                    },
                    flags = PlatformAttributes.IsWindowsServerBuildTarget | PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("8659dec1db6b4fac86149f99f2fa4291"),
                new PlatformInfo
                {
                    displayName = "macOS Server",
                    downloadLinkName = "Mac-Server",
                    description = L10n.Tr("Benefit from Unity’s support for developing games and applications on the Dedicated Mac Server platform, including publishing multiplayer games."),
                    buildTarget = BuildTarget.StandaloneOSX,
                    subtarget = StandaloneBuildSubtarget.Server,
                    iconName = "BuildSettings.DedicatedServer",
                    internalPackages = new PlatformPackageList
                    {
                        requiredPackages = new[]
                        {
                            new PlatformPackageInfo(L10n.Tr("Dedicated Server"), "com.unity.dedicated-server", L10n.Tr("The Dedicated Server package contains optimizations and workflow improvements for developing Dedicated Server platform.")),
                        }
                    },
                    flags = PlatformAttributes.IsMacServerBuildTarget | PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("91d938b35f6f4798811e41f2acf9377f"),
                new PlatformInfo
                {
                    displayName = "Linux Server",
                    downloadLinkName = "Linux-Server",
                    description =  L10n.Tr("Benefit from Unity’s support for developing games and applications on the Dedicated Linux Server platform, including publishing multiplayer games."),
                    buildTarget = BuildTarget.StandaloneLinux64,
                    subtarget = StandaloneBuildSubtarget.Server,
                    iconName = "BuildSettings.DedicatedServer",
                    internalPackages = new PlatformPackageList
                    {
                        requiredPackages = new[]
                        {
                            new PlatformPackageInfo(L10n.Tr("Dedicated Server"), "com.unity.dedicated-server", L10n.Tr("The Dedicated Server package contains optimizations and workflow improvements for developing Dedicated Server platform.")),
                        }
                    },
                    flags = PlatformAttributes.IsLinuxServerBuildTarget | PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            // then mobile
            {
                new("b9b35072a6f44c2e863f17467ea3dc13"),
                new PlatformInfo
                {
                    displayName = "Android™",
                    downloadLinkName = "Android",
                    description = L10n.Tr("Android is a large and varied device ecosystem with over 3bn active devices. Benefit from Unity’s longstanding and wide-ranging resources for the entire development lifecycle for Android games. This includes tools and services for rapid iteration, performance optimization, player engagement, and revenue growth."),
                    buildTarget = BuildTarget.Android,
                    nameAndLinkToShowUnderTitle = new List<NameAndLink>
                    {
                        new NameAndLink{name = L10n.Tr("Unity Android Manual"),  linkUrl = $"https://docs.unity3d.com/{Help.GetShortReleaseVersion()}/Documentation/Manual/android.html"},
                    },
                    iconName = "BuildSettings.Android",
                    internalPackages = new PlatformPackageList
                    {
                        recommendedPackages = new[]
                        {
                            new PlatformPackageInfo(L10n.Tr("In-App Purchasing"), "com.unity.purchasing",
                                L10n.Tr("With Unity IAP, setting up in-app purchases for your game across multiple app stores has never been easier."))
                        }
                    },
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("ad48d16a66894befa4d8181998c3cb09"),
                new PlatformInfo
                {
                    displayName = "iOS",
                    downloadLinkName = "iOS",
                    description =  L10n.Tr("Benefit from Unity’s longstanding and wide-ranging resources for the entire development lifecycle for iOS games. This includes tools and services for rapid iteration, performance optimization, player engagement, and revenue growth."),
                    buildTarget = BuildTarget.iOS,
                    iconName = "BuildSettings.iPhone",
                    internalPackages = new PlatformPackageList
                    {
                        recommendedPackages = new[]
                        {
                            new PlatformPackageInfo(L10n.Tr("In-App Purchasing"), "com.unity.purchasing",
                                L10n.Tr("With Unity IAP, setting up in-app purchases for your game across multiple app stores has never been easier."))
                        }
                    },
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            // then consoles
            {
                new("5d4f9b64eeb74b18a2de0de6f0c36931"),
                new PlatformInfo
                {
                    displayName = "PlayStation®4",
                    downloadLinkName = "PS4",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Create your game with a comprehensive development platform for PlayStation®4. Discover powerful creation tools to take your PlayStation game to the next level."),
                    buildTarget = BuildTarget.PS4,
                    nameAndLinkToShowUnderTitle = new List<NameAndLink>
                    {
                        new NameAndLink{ name = L10n.Tr("Register as a PlayStation developer"), linkUrl = "https://partners.playstation.net/"}
                    },
                    iconName = "BuildSettings.PS4",
                    flags = PlatformAttributes.ExternalDownloadForBuildTarget | PlatformAttributes.IsNDAPlatform | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            {
                new("e30a9c34166844499a56eaa4ed115c44"),
                new PlatformInfo
                {
                    displayName = "PlayStation®5",
                    downloadLinkName = "PS5",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Create your game with a comprehensive development platform for PlayStation®5. Discover powerful creation tools to take your PlayStation game to the next level."),
                    buildTarget = BuildTarget.PS5,
                    nameAndLinkToShowUnderTitle = new List<NameAndLink>
                    {
                        new NameAndLink{name = L10n.Tr("Register as a PlayStation developer"), linkUrl = "https://partners.playstation.net/"}
                    },
                    iconName = "BuildSettings.PS5",
                    flags = PlatformAttributes.ExternalDownloadForBuildTarget | PlatformAttributes.IsNDAPlatform | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            {
                new("c9f186cd3d594a1496bca1860359f842"),
                new PlatformInfo
                {
                    displayName = "Xbox Series X|S",
                    downloadLinkName = "GameCoreScarlett",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Attract players around the world on the latest generation of Xbox: Xbox Series X|S. Push the graphical fidelity of your games with inline ray tracing, all while maintaining performance with the latest optimizations for DirectX 12."),
                    buildTarget = BuildTarget.GameCoreXboxSeries,
                    nameAndLinkToShowUnderTitle = new List<NameAndLink>
                    {
                         new NameAndLink{name = L10n.Tr("Register as an Xbox developer"), linkUrl = "https://www.xbox.com/en-US/developers/id"}
                    },
                    iconName = "BuildSettings.GameCoreScarlett",
                    flags = PlatformAttributes.IsHidden | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            {
                new("a6f1094111614cba85f0508bf9778843"),
                new PlatformInfo
                {
                    displayName = "Xbox One",
                    downloadLinkName = "GameCoreXboxOne",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Attract and engage over 50 million players around the world on Xbox One."),
                    buildTarget = BuildTarget.GameCoreXboxOne,
                    nameAndLinkToShowUnderTitle = new List<NameAndLink>
                    {
                        new NameAndLink{name = L10n.Tr("Register as an Xbox developer"), linkUrl ="https://www.xbox.com/en-US/developers/id"}
                    },
                    iconName = "BuildSettings.GameCoreXboxOne",
                    flags = PlatformAttributes.ExternalDownloadForBuildTarget | PlatformAttributes.IsHidden | PlatformAttributes.IsWindowsBuildTarget
                }
            },
            {
                new("08d61a9cdfb840119d9bea5588e2f338"),
                new PlatformInfo
                {
                    displayName = "Nintendo Switch™",
                    downloadLinkName = "Switch",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description = L10n.Tr("Bring your game to Nintendo Switch™ with Unity’s optimized platform support as well as a dedicated forum."),
                    buildTarget = BuildTarget.Switch,
                    nameAndLinkToShowUnderTitle = new List<NameAndLink>
                    {
                        new NameAndLink{name = L10n.Tr("Register as a Nintendo developer"), linkUrl = "http://developer.nintendo.com"}
                    },
                    iconName = "BuildSettings.Switch",
                    flags = PlatformAttributes.IsHidden | PlatformAttributes.IsNDAPlatform | PlatformAttributes.IsWindowsBuildTarget,
                    buildProfilePlatformBannerBgColorHex = "#E60012"
                }
            },
            {
               new("25a09d2ed10c42f789b61d99b4d9bf83"),
               new PlatformInfo
               {
                    displayName = "Nintendo Switch™ 2",
                    instructions = L10n.Tr("This platform is not available to download from the Unity website, contact the platform holder directly to learn more."),
                    description =  L10n.Tr("Benefit from Unity’s support for developing games and applications on this platform"),
                    buildTarget = BuildTarget.Switch2,
                    iconName = "BuildSettings.Switch2",
                    flags = PlatformAttributes.ExternalDownloadForBuildTarget | PlatformAttributes.IsHidden | PlatformAttributes.IsNDAPlatform | PlatformAttributes.IsWindowsBuildTarget,
                    buildProfilePlatformBannerBgColorHex = "#E60012"
                }
            },
            // then others
            {
                new("84a3bb9e7420477f885e98145999eb20"),
                new PlatformInfo
                {
                    displayName = "Web",
                    downloadLinkName = "WebGL",
                    description =  L10n.Tr("Leverage Unity’s web solutions to offer your players near-instant access to their favorite games, no matter where they want to play. Our web platform includes support for desktop and mobile browsers."),
                    buildTarget = BuildTarget.WebGL,
                    iconName = "BuildSettings.WebGL",
                    settingsDocsLink = $"https://docs.unity3d.com/{Help.GetShortReleaseVersion()}/Documentation/Manual/web-setting-configurations.html",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
               new("80657fe557de4d17822398b3a01b8c9e"),
               new PlatformInfo
               {
                    displayName = "Meta Quest",
                    downloadLinkName = "Android",
                    description = L10n.Tr(
                        "Take advantage of Unity's support for Meta Quest platforms and leverage Unity's extensive XR tools and frameworks. " +
                        "This platform offers default project settings and configurations for a streamlined publishing process."
                    ),
                    buildTarget = BuildTarget.Android,
                    iconName = "BuildSettings.Meta",
                    internalPackages = new PlatformPackageList
                    {
                        requiredPackages = new[]
                        {
                            new PlatformPackageInfo(L10n.Tr("OpenXR Plugin"), "com.unity.xr.openxr",
                                L10n.Tr("OpenXR is an open, royalty-free standard developed by Khronos that aims to simplify AR/VR development by allowing developers to target a wide range of AR/VR devices. Use this plug-in to enable OpenXR in XR Plug-in Management.")),
                        }
                    },
                    partnerPackages = new PlatformPackageList
                    {
                        recommendedPackages = new[]
                        {
                            new PlatformPackageInfo(L10n.Tr("Meta XR All-in-one SDK"), "com.meta.xr.sdk.all",
                                L10n.Tr("Meta XR All-in-One SDK is a wrapper package that depends on the latest version of all Meta XR SDKs, making it easy to get started with VR development."),
                                L10n.Tr("Meta"), true),
                            new PlatformPackageInfo(L10n.Tr("Meta XR Core SDK"), "com.meta.xr.sdk.core",
                                L10n.Tr("Meta XR Core SDK package provides the latest features to create immersive experiences for Meta XR devices."),
                                L10n.Tr("Meta"), true),
                            new PlatformPackageInfo(L10n.Tr("Meta XR Audio SDK"), "com.meta.xr.sdk.audio",
                                L10n.Tr("Comprehensive spatial audio features for immersive applications, including a spatial audio renderer with HRTF, ambisonic audio playback, and room acoustics for creating immersive environments."),
                                L10n.Tr("Meta"), true),
                            new PlatformPackageInfo(L10n.Tr("Meta XR Haptics SDK"), "com.meta.xr.sdk.haptics",
                                L10n.Tr("SDK for playback of haptics created in Meta Haptics Studio on Quest devices."),
                                L10n.Tr("Meta"), true),
                            new PlatformPackageInfo(L10n.Tr("Meta XR Interaction SDK Essentials"), "com.meta.xr.sdk.interaction",
                                L10n.Tr("Provides the core implementations of all the provided interaction models along with necessary shaders, materials, and prefabs."),
                                L10n.Tr("Meta"), true),
                            new PlatformPackageInfo(L10n.Tr("Meta XR Interaction SDK"), "com.meta.xr.sdk.interaction.ovr",
                                L10n.Tr("This package allows Interaction SDK to interface with OVRPlugin. Use this package if you are using OVRPlugin or the Core SDK package."),
                                L10n.Tr("Meta"), true),
                            new PlatformPackageInfo(L10n.Tr("Meta XR Platform SDK"), "com.meta.xr.sdk.platform",
                                L10n.Tr("Use the Platform SDK to create social VR applications. Add Matchmaking, DLC,In-App Purchases, Cloud Storage, and more to your experience using the individual components of the SDK."),
                                L10n.Tr("Meta"), true),
                            new PlatformPackageInfo(L10n.Tr("Meta XR Voice SDK"), "com.meta.xr.sdk.voice",
                                L10n.Tr("Voice SDK enables natural voice interactions for AR/VR apps, powered by Wit.ai."),
                                L10n.Tr("Meta"), true),
                            new PlatformPackageInfo(L10n.Tr("Meta XR Simulator"), "com.meta.xr.simulator",
                                L10n.Tr("Meta XR Simulator allows developers to preview their VR changes without needing a physical device."),
                                L10n.Tr("Meta"), true),
                            new PlatformPackageInfo(L10n.Tr("Meta XR MR Utility Kit"), "com.meta.xr.mrutilitykit",
                                L10n.Tr("Helper tools & functions to simplify development with Scene API. Works on Mac & PC, without a device attached."),
                                L10n.Tr("Meta"), true),
                        }
                    },
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget | PlatformAttributes.IsDerivedBuildTarget,
                    buildProfilePlatformBannerBgColorHex = "#FFFFFF"
                }
            },
            {
                new("a71389c8cc8e4edc99d30db86d62ee8f"),
                new PlatformInfo
                {
                    displayName = "Android XR",
                    downloadLinkName = "Android",
                    description = L10n.Tr("Android XR provides a strong foundation for high-performance, immersive experiences. OpenXR conformance ensures support for widely used XR extensions, while cross-platform tools provide a familiar development experience. The Android XR build profile offers pre-configured settings tailored for Android XR development, making getting started fast and easy."),
                    buildTarget = BuildTarget.Android,
                    iconName = "BuildSettings.Android",
                    internalPackages = new PlatformPackageList
                    {
                        requiredPackages = new[]
                        {
                            new PlatformPackageInfo(L10n.Tr("Unity OpenXR Android XR"), "com.unity.xr.androidxr-openxr", L10n.Tr("Enables you to build mixed reality apps for Android XR devices by implementing support for Android XR extensions.")),
                        }
                    },
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget | PlatformAttributes.IsDerivedBuildTarget
                }
            },
            {
                new("32e92b6f4db44fadb869cafb8184d021"),
                new PlatformInfo
                {
                    displayName = "Universal Windows Platform",
                    downloadLinkName = "Universal-Windows-Platform",
                    description = L10n.Tr("Benefit from Unity’s runtime support for UWP, ensuring you’re able to reach as many users as possible in the Microsoft ecosystem. UWP is used for HoloLens and Windows 10 and 11 devices, among others."),
                    buildTarget = BuildTarget.WSAPlayer,
                    iconName = "BuildSettings.Metro",
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget
                }
            },
            {
                new("1e09bd9b55c8d45e9a11b4727bf18e88"),
                new PlatformInfo
                {
                    displayName = "Unity Render Service for Android™",
                    buildTarget = BuildTarget.Android,
                    iconName = "BuildSettings.Android",
                    internalPackages = new PlatformPackageList
                    {
                        requiredPackages = new[]
                        {
                            new PlatformPackageInfo(L10n.Tr("Unity Render Service for Android™ Support"), "com.unity.android.render-service", L10n.Tr("Enables building the Unity Render Service for Android™. Contact your sales representative for access."))
                        }
                    },
                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget | PlatformAttributes.IsDerivedBuildTarget | PlatformAttributes.IsHidden | PlatformAttributes.IsVisibleInPlatformBrowserOnly
                }
            },
            {
                new("81e4f4c492fd4311bbf5b0b88a28c737"),
                new PlatformInfo
                {
                    displayName = "tvOS",
                    downloadLinkName = "AppleTV",
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
                    downloadLinkName = "VisionOS",
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
                    downloadLinkName = "CloudRendering",
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
                    downloadLinkName = "EmbeddedLinux",
                    instructions = L10n.Tr("As the Embedded Linux platform for Unity is not yet available to download from the Unity website, contact your Account Manager or the Unity Sales team to get access."),
                    description = L10n.Tr("Choose Embedded Linux, a compact version of Linux, if you are planning to build applications for embedded devices and appliances."),
                    buildTarget = BuildTarget.EmbeddedLinux,
                    iconName = "BuildSettings.EmbeddedLinux",

                    // TODO: this is a workaround for onboarding instructions to fix EmbeddedLinux and QNX
                    // needs to be removed when https://jira.unity3d.com/browse/PLAT-7721 is implemented
                    temporaryLabelAndLinkForIndustrialOnboarding = new NameAndLink{name = L10n.Tr("No Embedded Linux module loaded. If you are a current Embedded Platforms customer, contact your Account Manager for download instructions.\nFor information on access and licensing, contact the Unity Sales team."), linkUrl = "https://create.unity.com/unity-for-industries?sfcid=7015G000000KFqdQAG&sflsa=2023-04-na-dg-hmi-solutions-contact-us"},

                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
                new("99ef95e1e9b048fa9628d7eed27a8646"),
                new PlatformInfo
                {
                    displayName = "QNX®",
                    downloadLinkName = "QNX",
                    instructions = L10n.Tr("As the QNX platform for Unity is not yet available to download from the Unity website, contact your Account Manager or the Unity Sales team to get access."),
                    description = L10n.Tr("Deploy the Unity runtime to automotive and other embedded systems utilizing the Blackberry® QNX® real-time operating system."),
                    buildTarget = BuildTarget.QNX,
                    iconName = "BuildSettings.QNX",

                    // TODO: this is a workaround for onboarding instructions to fix EmbeddedLinux and QNX
                    // needs to be removed when https://jira.unity3d.com/browse/PLAT-7721 is implemented
                    temporaryLabelAndLinkForIndustrialOnboarding = new NameAndLink{name = L10n.Tr("No QNX® module loaded. If you are a current Embedded Platforms customer, contact your Account Manager for download instructions.\nFor information on access and licensing, contact the Unity Sales team."), linkUrl = "https://create.unity.com/unity-for-industries?sfcid=7015G000000KFqdQAG&sflsa=2023-04-na-dg-hmi-solutions-contact-us"},

                    flags = PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget | PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget
                }
            },
            {
               new("f8c7649c24f344129a97cf9854e2d582"),
               new PlatformInfo
               {
                    displayName = "Kepler",
                    buildTarget = BuildTarget.Kepler,
                    iconName = "BuildSettings.EmbeddedLinux",
                    flags = PlatformAttributes.IsMacBuildTarget | PlatformAttributes.IsNDAPlatform | PlatformAttributes.IsHidden
                }
            }
        };

        static readonly Dictionary<GUID, bool> k_PlatformInstalledData = new();
        static readonly Dictionary<GUID, ISDKPlatformExtension> k_SDKPlatformExtensions = new();

        static readonly PlatformGroup[] allPlatformGroups =
        {
            new PlatformGroup
            {
                groupName = L10n.Tr("Mobile"),
                platforms = new GUID []
                {
                    new("b9b35072a6f44c2e863f17467ea3dc13"), // Android
                    new("ad48d16a66894befa4d8181998c3cb09"), // iOS
                }
            },
             new PlatformGroup
            {
                groupName = L10n.Tr("Desktop"),
                platforms = new GUID []
                {
                    new("4e3c793746204150860bf175a9a41a05"), // Windows
                    new("0d2129357eac403d8b359c2dcbf82502"), // MacOS
                    new("cb423bfea44b4d658edb8bc5d91a3024"), // Linux
                    new("32e92b6f4db44fadb869cafb8184d021"), // UWP
                }
            },
            new PlatformGroup
            {
                groupName = L10n.Tr("XR"),
                platforms = new GUID []
                {
                    new("80657fe557de4d17822398b3a01b8c9e"), // Meta Quest
                    new("a71389c8cc8e4edc99d30db86d62ee8f"), // Android XR
                    new("53916e6f1f7240d992977ffa2322b047"), // visionOS
                }
            },
            new PlatformGroup
            {
                groupName = L10n.Tr("Web"),
                platforms = new GUID []
                {
                    new("84a3bb9e7420477f885e98145999eb20"), // Web
                }
            },
            new PlatformGroup
            {
                groupName = L10n.Tr("Console"),
                platforms = new GUID []
                {
                    new("c9f186cd3d594a1496bca1860359f842"), // Xbox X|S
                    new("a6f1094111614cba85f0508bf9778843"), // Xbox One
                    new("08d61a9cdfb840119d9bea5588e2f338"), // Switch
                    new("25a09d2ed10c42f789b61d99b4d9bf83"), // Switch 2
                    new("5d4f9b64eeb74b18a2de0de6f0c36931"), // PS4
                    new("e30a9c34166844499a56eaa4ed115c44"), // PS5
                    new("81e4f4c492fd4311bbf5b0b88a28c737"), // tvOS
                }
            },
            new PlatformGroup
            {
                groupName = L10n.Tr("Industry"),
                platforms = new GUID []
                {
                    new("f1d7bec2fd7f42f481c66ef512f47845"), // EmbeddedLinux
                    new("f188349a68c441ec9e3eb4c6f59abd41"), // LinuxHeadlessSimulation
                    new("99ef95e1e9b048fa9628d7eed27a8646"), // QNX
                    // new("1e09bd9b55c8d45e9a11b4727bf18e88"), // Android Render Service will come here, but is hidden until entitlements are available.
                    new("f8c7649c24f344129a97cf9854e2d582"), // Kepler
                }
            },
            new PlatformGroup
            {
                groupName = L10n.Tr("Servers"),
                platforms = new GUID []
                {
                    new("8d1e1bca926649cba89d37a4c66e8b3d"), // Windows server
                    new("8659dec1db6b4fac86149f99f2fa4291"), // MacOS Server
                    new("91d938b35f6f4798811e41f2acf9377f"), // Linux Server
                }
            },
        };

        static BuildTargetDiscovery()
        {
            LoadSDKPlatforms();
            PreloadBuildPlatformInstalledData();
            LoadSDKMultiTargetPlatformExtensions();
        }

        public static IEnumerable<GUID> GetAllPlatforms() => allPlatforms.Keys;

        /// <summary>
        /// Get the platform GUID corresponding to the NamedBuildTarget and BuildTarget.
        /// </summary>
        /// <param name="namedBuildTarget">The NamedBuildTarget to get the platform GUID for.</param>
        /// <param name="buildTarget">The BuildTarget to get the platform GUID for.</param>
        /// <returns>The platform GUID. Derived platform GUID when the active platform is a derived platform. Base platform GUID otherwise.</returns>
        public static GUID GetGUIDFromBuildTarget(NamedBuildTarget namedBuildTarget, BuildTarget buildTarget)
        {
            if (TryGetServerGUIDFromBuildTarget(namedBuildTarget, buildTarget, out var value))
                return value;

            return GetGUIDFromBuildTarget(buildTarget);
        }

        /// <summary>
        /// Get the platform GUID corresponding to the BuildTarget. Note this method does not work with the server platforms.
        /// Use the namedBuildTarget overload of <see cref="BuildTargetDiscovery.GetGUIDFromBuildTarget"/> for server platforms.
        /// </summary>
        /// <param name="buildTarget">The BuildTarget to get the platform GUID for.</param>
        /// <returns>The platform GUID. Derived platform GUID when the active platform is a derived platform. Base platform GUID otherwise.</returns>
        public static GUID GetGUIDFromBuildTarget(BuildTarget buildTarget)
        {
            if (s_BuildTargetToPlatformGUID.TryGetValue(buildTarget, out GUID value))
            {
                var module = ModuleManager.FindPlatformSupportModule(value);
                if (module != null && module is IDerivedBuildTargetProvider)
                    return module.PlatformBuildTarget.Guid;
                else
                    return value;
            }

            return EmptyGuid;
        }

        internal static bool TryGetServerGUIDFromBuildTarget(NamedBuildTarget namedBuildTarget, BuildTarget buildTarget, out GUID result)
        {
            result = EmptyGuid;

            if (namedBuildTarget == NamedBuildTarget.Server)
            {
                foreach (var platform in allPlatforms)
                {
                    if (platform.Value.subtarget == StandaloneBuildSubtarget.Server)
                    {
                        if ((buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64) && platform.Value.HasFlag(PlatformAttributes.IsWindowsServerBuildTarget))
                            result = platform.Key;
                        else if (buildTarget == BuildTarget.StandaloneLinux64 && platform.Value.HasFlag(PlatformAttributes.IsLinuxServerBuildTarget))
                            result = platform.Key;
                        else if (buildTarget == BuildTarget.StandaloneOSX && platform.Value.HasFlag(PlatformAttributes.IsMacServerBuildTarget))
                            result = platform.Key;
                    }
                }
            }
            return !result.Empty();
        }

        internal static GUID GetBasePlatformGUIDFromBuildTarget(NamedBuildTarget namedBuildTarget, BuildTarget buildTarget)
        {
            if (TryGetServerGUIDFromBuildTarget(namedBuildTarget, buildTarget, out var value))
                return value;

            if (s_BuildTargetToPlatformGUID.TryGetValue(buildTarget, out GUID guid))
                return guid;

            return EmptyGuid;
        }

        /// <summary>
        /// Get the base platform GUID given a platform GUID.
        /// </summary>
        /// <param name="platformGuid">The platform GUID to get the base platform GUID for.</param>
        /// <returns>The base platform GUID. If the platform is not a derived platform, the same GUID is returned.</returns>
        internal static GUID GetBasePlatformGUID(GUID platformGuid)
        {
            if (!allPlatforms.TryGetValue(platformGuid, out PlatformInfo platformInfo))
                return EmptyGuid;

            if (!platformInfo.HasFlag(PlatformAttributes.IsDerivedBuildTarget))
                return platformGuid;

            if (s_BuildTargetToPlatformGUID.TryGetValue(platformInfo.buildTarget, out GUID basePlatformGuid))
                return basePlatformGuid;

            return EmptyGuid;
        }

        public static (BuildTarget, StandaloneBuildSubtarget) GetBuildTargetAndSubtargetFromGUID(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return (platformInfo.buildTarget, platformInfo.subtarget);

            return (BuildTarget.NoTarget, StandaloneBuildSubtarget.Default);
        }

        static void LoadSDKPlatforms()
        {
            if (!Directory.Exists(k_ResourcesSDKPlatformInfoPath))
                return;

            var sdkPlatformFiles = Directory.GetFiles(k_ResourcesSDKPlatformInfoPath, "*.SDKPlatform.json");
            foreach (var sdkPlatformFile in sdkPlatformFiles)
            {
                var text = File.ReadAllText(sdkPlatformFile);
                SDKPlatformInfo sdkPlatformInfo;
                try
                {
                    sdkPlatformInfo = JsonUtility.FromJson(text, typeof(SDKPlatformInfo)) as SDKPlatformInfo;
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format(k_SDKPlatformInfoFailToParseError, sdkPlatformFile, e.Message));
                    continue;
                }

                if (sdkPlatformInfo == null)
                {
                    Debug.LogError(string.Format(k_SDKPlatformInfoFailToParseUnknownError, sdkPlatformFile));
                    continue;
                }

                var sdkPlatformGuid = new GUID(sdkPlatformInfo.guid);
                if (sdkPlatformGuid.Empty())
                {
                    Debug.LogError(string.Format(k_SDKPlatformInfoMissingGuidError, sdkPlatformFile));
                    continue;
                }

                var version = sdkPlatformInfo.version;
                if (version <= 0)
                {
                    Debug.LogError(string.Format(k_SDKPlatformInfoInvalidVersionError, sdkPlatformFile, version));
                    continue;
                }

                if (allPlatforms.ContainsKey(sdkPlatformGuid))
                {
                    Debug.LogError(string.Format(k_SDKPlatformGuidAlreadyUsedError, sdkPlatformGuid));
                    continue;
                }

                var basePlatformGuid = new GUID(sdkPlatformInfo.basePlatformGuid);
                var (baseBuildTarget, _) = GetBuildTargetAndSubtargetFromGUID(basePlatformGuid);

                var flags = PlatformAttributes.None;
                switch (sdkPlatformInfo.flags.platformType)
                {
                    case SDKPlatformType.Derived:
                        flags |= PlatformAttributes.IsDerivedBuildTarget;
                        if (baseBuildTarget <= 0)
                        {
                            Debug.LogError(string.Format(k_SDKPlatformMissingBaseGuidError, sdkPlatformGuid));
                            continue;
                        }
                        break;
                    case SDKPlatformType.MultiTarget:
                        flags |= PlatformAttributes.IsMultiTargetPlatform;
                        if (sdkPlatformInfo.supportedPlatformGuids == null || sdkPlatformInfo.supportedPlatformGuids.Length <= 0)
                        {
                            Debug.LogError(string.Format(k_SDKPlatformNoSupportedGuidsError, sdkPlatformGuid));
                            continue;
                        }
                        break;
                }

                var displayName = sdkPlatformInfo.displayName ?? string.Empty;
                if (string.IsNullOrEmpty(displayName))
                    Debug.LogWarning(string.Format(k_SDKPlatformMissingDisplayNameWarning, sdkPlatformGuid));

                PlatformInfo platformInfo = new()
                {
                    supportedPlatformGuids = sdkPlatformInfo.flags.platformType == SDKPlatformType.MultiTarget ?
                        Array.ConvertAll(sdkPlatformInfo.supportedPlatformGuids, s => new GUID(s)) : Array.Empty<GUID>(),
                    buildTarget = baseBuildTarget,
                    displayName = displayName,
                    description = sdkPlatformInfo.description ?? string.Empty,
                    instructions = sdkPlatformInfo.instructions ?? string.Empty,
                    keyFeatures = sdkPlatformInfo.keyFeatures ?? string.Empty,
                    resources = sdkPlatformInfo.resources ?? string.Empty,
                    iconName = sdkPlatformInfo.iconName,
                    buildProfilePlatformBannerBgColorHex = sdkPlatformInfo.bannerBackgroundColorHex ?? "#00000000",
                    internalPackages = sdkPlatformInfo.internalPackages,
                    partnerPackages = sdkPlatformInfo.partnerPackages,
                    flags = flags | PlatformAttributes.IsSDKPlatform | PlatformAttributes.IsVisibleInPlatformBrowserOnly |
                        PlatformAttributes.IsWindowsBuildTarget | PlatformAttributes.IsWindowsArm64BuildTarget |
                        PlatformAttributes.IsLinuxBuildTarget | PlatformAttributes.IsMacBuildTarget,
                };
                allPlatforms.Add(sdkPlatformGuid, platformInfo);

                var targetGroupName = sdkPlatformInfo.platformGroupName;
                if (string.IsNullOrEmpty(targetGroupName))
                {
                    Debug.LogWarning(string.Format(k_SDKPlatformMissingPlatformGroupWarning, sdkPlatformGuid));
                    continue;
                }

                var groupIndex = Array.FindIndex(allPlatformGroups, g => g.groupName == targetGroupName);
                if (groupIndex < 0)
                {
                    Debug.LogWarning(string.Format(k_SDKPlatformUnknownPlatformGroupWarning, sdkPlatformGuid, targetGroupName));
                    continue;
                }

                var platformsList = new List<GUID>(allPlatformGroups[groupIndex].platforms);
                platformsList.Add(sdkPlatformGuid);
                allPlatformGroups[groupIndex].platforms = platformsList.ToArray();
            }
        }

        static void PreloadBuildPlatformInstalledData()
        {
            foreach (var platform in allPlatforms)
            {
                // SDK platforms will be set as installed when their corresponding platform module is
                // loaded and their SDKPlatformProvider is valid.
                if (platform.Value.HasFlag(PlatformAttributes.IsSDKPlatform))
                {
                    k_PlatformInstalledData.Add(platform.Key, false);
                    continue;
                }

                // Capture BuildTarget to GUID mapping for all platforms.
                // Considers that StandaloneWindows and StandaloneWindows64 are the same platform.
                if (platform.Value.buildTarget != BuildTarget.StandaloneWindows
                    && platform.Value.subtarget != StandaloneBuildSubtarget.Server
                    && !platform.Value.HasFlag(PlatformAttributes.IsDerivedBuildTarget)
                    && !platform.Value.HasFlag(PlatformAttributes.IsMultiTargetPlatform))
                {
                    s_BuildTargetToPlatformGUID.Add(platform.Value.buildTarget, platform.Key);
                    if (platform.Value.buildTarget == BuildTarget.StandaloneWindows64)
                        s_BuildTargetToPlatformGUID.Add(BuildTarget.StandaloneWindows, platform.Key);
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
                if (platform.Value.HasFlag(PlatformAttributes.IsWindowsServerBuildTarget) ||
                    platform.Value.HasFlag(PlatformAttributes.IsLinuxServerBuildTarget) ||
                    platform.Value.HasFlag(PlatformAttributes.IsMacServerBuildTarget))
                    isInstalled = BuildPipeline.IsServerBuildPlatformSupported(platform.Value.buildTarget);
                else
                    isInstalled = true;

                k_PlatformInstalledData.Add(platform.Key, isInstalled);
            }
        }

        static void LoadSDKMultiTargetPlatformExtensions()
        {
            var types = TypeCache.GetTypesDerivedFrom<IPlatformProvider>();
            foreach (var type in types)
            {
                if (!TryCreateIPlatformProvider(type, out var provider))
                    continue;

                var sdkPlatformProvider = SDKPlatformProvider.TryCreateMultiTargetPlatformProvider(provider);
                if (sdkPlatformProvider == null)
                    continue;

                if (!allPlatforms.TryGetValue(sdkPlatformProvider.guid, out PlatformInfo platformInfo))
                {
                    Debug.LogError(string.Format(k_SDKProviderMissingPlatformInfoError, sdkPlatformProvider.providerType.FullName));
                    continue;
                }

                if (!platformInfo.HasFlag(PlatformAttributes.IsMultiTargetPlatform))
                {
                    Debug.LogError(string.Format(k_SDKProviderNotMultiTargetError, sdkPlatformProvider.providerType.FullName, sdkPlatformProvider.guid));
                    continue;
                }

                var supportedBuildTargets = new List<IBuildTarget>();
                foreach (var supportedGuid in platformInfo.supportedPlatformGuids)
                {
                    if (TryGetBuildTarget(supportedGuid, out var buildTarget))
                        supportedBuildTargets.Add(buildTarget);
                }

                if (supportedBuildTargets.Count <= 0)
                    continue;

                var sdkBuildTarget = new ConfigurableMultiTargetBuildTarget(sdkPlatformProvider, platformInfo, supportedBuildTargets.ToArray());
                var sdkPlatformExtension = new ConfigurableSDKPlatformExtension(sdkPlatformProvider, sdkBuildTarget);

                k_SDKPlatformExtensions.Add(sdkPlatformProvider.guid, sdkPlatformExtension);

                // A multi-target platform is considered as installed if at least one of its supported platforms is installed.
                foreach (var supportedGuid in platformInfo.supportedPlatformGuids)
                {
                    if (k_PlatformInstalledData.TryGetValue(supportedGuid, out var isPlatformInstalled) && isPlatformInstalled)
                        k_PlatformInstalledData[sdkPlatformProvider.guid] = true;
                }
            }
        }

        internal static bool TryCreateIPlatformProvider(Type type, out IPlatformProvider provider)
        {
            try
            {
                provider = Activator.CreateInstance(type) as IPlatformProvider;
                if (provider != null)
                    return true;

                Debug.LogError(string.Format(k_CreateIPlatformProviderFailedError, type.FullName));
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                provider = null;
                return false;
            }
        }

        /// <summary>
        /// Validates the SDK platforms by checking if their providers are coming from a unity
        /// registered package. If an SDK platform is found to be invalid, it is removed from 
        /// the list of available platforms.
        /// </summary>
        public static void ValidateSDKPlatformProviders()
        {
            var invalidSDKPlatforms = new List<GUID>();
            foreach (var extension in k_SDKPlatformExtensions)
            {   
                var provider = extension.Value.sdkPlatformProvider;
                if (!IsSDKPlatformValid(provider))
                    invalidSDKPlatforms.Add(provider.guid);
            }

            foreach (var invalidGuid in invalidSDKPlatforms)
            {
                k_SDKPlatformExtensions.Remove(invalidGuid);
                ModuleManager.RemoveDerivedBuildTargetExtension(invalidGuid);
                k_PlatformInstalledData[invalidGuid] = false;
            }
        }

        static bool IsSDKPlatformValid(SDKPlatformProvider provider)
        {
            var packageInfo = PackageManager.PackageInfo.FindForAssembly(provider.providerType.Assembly);
            if (packageInfo == null)
                return false;

            if (!IsFromUnityPackageSource(packageInfo))
                return false;

            return true;
        }

        public static bool IsFromUnityPackageSource(PackageManager.PackageInfo packageInfo)
        {
            if (Unsupported.IsSourceBuild())
                return true;
            return packageInfo.GetAvailableRegistryType() == RegistryType.UnityRegistry;
        }

        public static Type[] TryGetSDKRequiredComponents()
        {
            var extensions = k_SDKPlatformExtensions.Values;
            List<Type> types_list = new List<Type>();

            foreach (var ext in extensions)
                types_list.AddRange(ext.requiredComponents);

            return types_list.ToArray();
        } 

        public static bool TryGetSDKPlatformExtension(GUID guid, out ISDKPlatformExtension sdkPlatformExtension)
        {
            return k_SDKPlatformExtensions.TryGetValue(guid, out sdkPlatformExtension);
        }

        public static Dictionary<GUID, ISDKPlatformExtension> GetAllSDKPlatformExtensions()
        {
            return k_SDKPlatformExtensions;
        }

        internal static bool TryGetPlatformInfo(GUID guid, out PlatformInfo platformInfo)
        {
            return allPlatforms.TryGetValue(guid, out platformInfo);
        }

        internal static void RegisterSDKPlatformExtension(GUID guid, ISDKPlatformExtension sdkPlatformExtension)
        {
            if (!k_SDKPlatformExtensions.ContainsKey(guid))
                k_SDKPlatformExtensions.Add(guid, sdkPlatformExtension);
        }

        internal static void SetSDKPlatformInstalledStatus(GUID guid, bool isInstalled)
        {
            k_PlatformInstalledData[guid] = isInstalled;
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

        /// <summary>
        /// Checks if the platform module corresponding to the given platform GUID is installed.
        /// For multi-target platforms, it checks if at least one of the supported platform modules is installed.
        /// </summary>
        public static bool BuildPlatformModuleIsInstalled(GUID platformGuid)
        {
            if (TryGetSupportedPlatformGuids(platformGuid, out var supportedPlatformGuids))
            {
                foreach (var supportedGuid in supportedPlatformGuids)
                {
                    if (BuildPlatformIsInstalled(GetBasePlatformGUID(supportedGuid)))
                        return true;
                }
                return false;
            }

            return BuildPlatformIsInstalled(GetBasePlatformGUID(platformGuid));
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

        public static bool BuildPlatformIsSDKPlatform(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo) && platformInfo.HasFlag(PlatformAttributes.IsSDKPlatform))
                return true;

            return false;
        }

        internal static bool BuildPlatformIsDerivedPlatform(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo) && platformInfo.HasFlag(PlatformAttributes.IsDerivedBuildTarget))
                return true;

            return false;
        }

        public static bool BuildPlatformIsMultiTargetPlatform(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo) && platformInfo.HasFlag(PlatformAttributes.IsMultiTargetPlatform))
                return true;

            return false;
        }

        public static bool TryGetSupportedPlatformGuids(GUID guid, out GUID[] supportedPlatformGuids)
        {
            supportedPlatformGuids = Array.Empty<GUID>();
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo) && platformInfo.HasFlag(PlatformAttributes.IsMultiTargetPlatform))
            {
                supportedPlatformGuids = platformInfo.supportedPlatformGuids;
                if (supportedPlatformGuids != null && supportedPlatformGuids.Length > 0)
                    return true;
            }

            return false;
        }

        public static PlatformPackageList BuildPlatformInternalPackages(IBuildTarget platform) => BuildPlatformInternalPackages(platform.Guid);
        public static PlatformPackageList BuildPlatformInternalPackages(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.internalPackages;

            return new PlatformPackageList();
        }

        public static PlatformPackageList BuildPlatformPartnerPackages(IBuildTarget platform) => BuildPlatformPartnerPackages(platform.Guid);
        public static PlatformPackageList BuildPlatformPartnerPackages(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.partnerPackages;

            return new PlatformPackageList();
        }

        public static string[] GetAllPlatformPackageNames()
        {
            var allPackageNames = new HashSet<string>();
            foreach (var platform in allPlatforms)
            {
                var internalPackages = platform.Value.internalPackages;
                var partnerPackages = platform.Value.partnerPackages;
                if (internalPackages.packageCount == 0 && partnerPackages.packageCount == 0)
                    continue;

                foreach (var package in internalPackages.requiredPackages)
                    allPackageNames.Add(package.qualifiedName);
                foreach (var package in internalPackages.recommendedPackages)
                    allPackageNames.Add(package.qualifiedName);
                foreach (var package in partnerPackages.requiredPackages)
                    allPackageNames.Add(package.qualifiedName);
                foreach (var package in partnerPackages.recommendedPackages)
                    allPackageNames.Add(package.qualifiedName);
            }
            var result = new string[allPackageNames.Count];
            allPackageNames.CopyTo(result);
            return result;
        }

        /// <summary>
        /// Returns all required package names for a given platform. A required package
        /// is any package (internal or partner) that must be installed for a build to succeed.
        /// </summary>
        /// <param name="platformId">Platform identifier.</param>
        /// <returns>Set of required package names. </returns>
        public static string[] GetAllMissingRequiredPlatformPackageNames(GUID platformId)
        {
            var allPackageNames = new HashSet<string>();
            var internalPackages = BuildTargetDiscovery.BuildPlatformInternalPackages(platformId);
            var partnerPackages = BuildTargetDiscovery.BuildPlatformPartnerPackages(platformId);

            foreach (var package in internalPackages.requiredPackages)
            {
                if (!PackageManager.PackageInfo.IsPackageRegistered(package.qualifiedName))
                     allPackageNames.Add(package.qualifiedName);
            }

            foreach (var package in partnerPackages.requiredPackages)
            {
                if (!PackageManager.PackageInfo.IsPackageRegistered(package.qualifiedName))
                     allPackageNames.Add(package.qualifiedName);
            }

            var result = new string[allPackageNames.Count];
            allPackageNames.CopyTo(result);
            return result;
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

        public static string BuildPlatformKeyFeatures(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.keyFeatures;

            return string.Empty;
        }

        public static string BuildPlatformResources(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.resources;

            return string.Empty;
        }

        public static string GetPlatformColorString(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.buildProfilePlatformBannerBgColorHex;

            return "#FFFFFF";
        }

        public static List<NameAndLink> BuildPlatformNameLinkList(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.nameAndLinkToShowUnderTitle;

            return null;
        }

        public static string BuildPlatformSettingsDocsLink(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.settingsDocsLink;

            return null;
        }

        // TODO: this is a workaround for onboarding instructions to fix EmbeddedLinux and QNX
        // needs to be removed when https://jira.unity3d.com/browse/PLAT-7721 is implemented
        public static bool BuildPlatformTryGetCustomInstallLinkAndText(GUID guid, out string url, out string text)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
            {
                var nameAndLink = platformInfo.temporaryLabelAndLinkForIndustrialOnboarding;
                if (nameAndLink != null)
                {
                    url = nameAndLink.Value.linkUrl;
                    text = nameAndLink.Value.name;
                    return true;
                }
            }
            url = string.Empty;
            text = string.Empty;
            return false;
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

        public static string BuildPlatformDownloadLinkName(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.downloadLinkName;

            return string.Empty;
        }

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

        public static string BuildPlatformSubtitle(GUID guid)
        {
            if (allPlatforms.TryGetValue(guid, out PlatformInfo platformInfo))
                return platformInfo.subtitle;

            return string.Empty;
        }

        public static PlatformGroup[] GetPlatformGroups()
        {
            return allPlatformGroups;
        }
    }
}
