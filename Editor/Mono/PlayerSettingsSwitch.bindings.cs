// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor
{
    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public sealed partial class PlayerSettings
    {
        // Nintendo Switch specific player settings
        [NativeHeader("Editor/Mono/PlayerSettingsSwitch.bindings.h")]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
        public sealed partial class Switch
        {
            const string kPlayerSettingsAreObsoletedWarning = "NMETA Settings are deprecated in Unity 2023.2 and will be removed in 2023.3. Please use the Authoring Editor to create and edit NMETA files.";


            public enum ScreenResolutionBehavior
            {
                Manual = 0,
                OperationMode = 1,
                PerformanceMode = 2,
                Both = 3
            }

            // These language names should be match to the name descriptions where in an NMETA file and SwitchBuildUtils.Languages.
            // And, please notice that you have to increase numSwitchLanguages in EditorOnlyPlayerSettings.h when you add a new language here.
            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            public enum Languages
            {
                AmericanEnglish = 0,
                BritishEnglish = 1,
                Japanese = 2,
                French = 3,
                German = 4,
                LatinAmericanSpanish = 5,
                Spanish = 6,
                Italian = 7,
                Dutch = 8,
                CanadianFrench = 9,
                Portuguese = 10,
                Russian = 11,
                SimplifiedChinese = 12,
                TraditionalChinese = 13,
                Korean = 14,
                BrazilianPortuguese = 15,
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            public enum
            StartupUserAccount
            {
                None = 0,
                Required = 1,
                RequiredWithNetworkServiceAccountAvailable = 2
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            public enum LogoHandling
            {
                Auto = 0,
                Manual = 1
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            public enum LogoType
            {
                LicensedByNintendo = 0,
                [Obsolete("This attribute is no longer available as of NintendoSDK 4.3.", true)]
                DistributedByNintendo = 1,
                Nintendo = 2
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            public enum ApplicationAttribute
            {
                None = 0,
                Demo = 1
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            public enum RatingCategories
            {
                CERO = 0,
                GRACGCRB = 1,
                GSRMR = 2,
                ESRB = 3,
                ClassInd = 4,
                USK = 5,
                PEGI = 6,
                PEGIPortugal = 7,
                PEGIBBFC = 8,
                Russian = 9,
                ACB = 10,
                OFLC = 11,
                IARCGeneric = 12,
            }

            private enum SupportedNpadStyleBits
            {
                FullKey = 1,
                Handheld = 2,
                JoyDual = 4,
                JoyLeft = 8,
                JoyRight = 16,
            }

            [Flags]
            public enum SupportedNpadStyle
            {
                FullKey = (1 << SupportedNpadStyleBits.FullKey),
                Handheld = (1 << SupportedNpadStyleBits.Handheld),
                JoyDual = (1 << SupportedNpadStyleBits.JoyDual),
                JoyLeft = (1 << SupportedNpadStyleBits.JoyLeft),
                JoyRight = (1 << SupportedNpadStyleBits.JoyRight),
            }

            // Socket Memory Pool Size
            [NativeProperty("switchSocketMemoryPoolSize", TargetType.Field)]
            extern public static int socketMemoryPoolSize { get; set; }

            // Socket Allocator Pool Size
            [NativeProperty("switchSocketAllocatorPoolSize", TargetType.Field)]
            extern public static int socketAllocatorPoolSize { get; set; }

            // Socket Concurrency Limit
            [NativeProperty("switchSocketConcurrencyLimit", TargetType.Field)]
            extern public static int socketConcurrencyLimit { get; set; }

            // Whether to enable use of the Nintendo Switch CPU Profiler.
            [NativeProperty("switchUseCPUProfiler", TargetType.Field)]
            extern public static bool useSwitchCPUProfiler { get; set; }

            // What LTO setting to use on Switch.
            [NativeProperty("switchLTOSetting", TargetType.Field)]
            extern public static int switchLTOSetting { get; set; }

            // Whether to enable use of the old Nintendo GOLD linker.
            [NativeProperty("switchUseGOLDLinker", TargetType.Field)]
            extern public static bool useSwitchGOLDLinker { get; set; }

            // System Memory (used for virtual memory mapping).
            [NativeProperty("switchSystemResourceMemory", TargetType.Field)]
            extern public static int systemResourceMemory { get; set; }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int queueCommandMemory
            {
                [NativeMethod("GetSwitchQueueCommandMemory")]
                get;
                [NativeMethod("SetSwitchQueueCommandMemory")]
                set;
            }

            [StaticAccessor("PlayerSettings", StaticAccessorType.DoubleColon)]
            extern public static int defaultSwitchQueueCommandMemory { get; }

            [StaticAccessor("PlayerSettings", StaticAccessorType.DoubleColon)]
            extern public static int minimumSwitchQueueCommandMemory { get; }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int queueControlMemory
            {
                [NativeMethod("GetSwitchQueueControlMemory")]
                get;
                [NativeMethod("SetSwitchQueueControlMemory")]
                set;
            }

            [StaticAccessor("PlayerSettings", StaticAccessorType.DoubleColon)]
            extern public static int defaultSwitchQueueControlMemory { get; }

            [StaticAccessor("PlayerSettings", StaticAccessorType.DoubleColon)]
            extern public static int minimumSwitchQueueControlMemory { get; }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int queueComputeMemory
            {
                [NativeMethod("GetSwitchQueueComputeMemory")]
                get;
                [NativeMethod("SetSwitchQueueComputeMemory")]
                set;
            }

            [StaticAccessor("PlayerSettings", StaticAccessorType.DoubleColon)]
            extern public static int defaultSwitchQueueComputeMemory { get; }

            // GPU Pool information.
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int NVNShaderPoolsGranularity
            {
                [NativeMethod("GetSwitchNVNShaderPoolsGranularity")]
                get;
                [NativeMethod("SetSwitchNVNShaderPoolsGranularity")]
                set;
            }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int NVNDefaultPoolsGranularity
            {
                [NativeMethod("GetSwitchNVNDefaultPoolsGranularity")]
                get;
                [NativeMethod("SetSwitchNVNDefaultPoolsGranularity")]
                set;
            }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int NVNOtherPoolsGranularity
            {
                [NativeMethod("GetSwitchNVNOtherPoolsGranularity")]
                get;
                [NativeMethod("SetSwitchNVNOtherPoolsGranularity")]
                set;
            }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int GpuScratchPoolGranularity
            {
                [NativeMethod("GetSwitchGpuScratchPoolGranularity")]
                get;
                [NativeMethod("SetSwitchGpuScratchPoolGranularity")]
                set;
            }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static bool AllowGpuScratchShrinking
            {
                [NativeMethod("GetSwitchAllowGpuScratchShrinking")]
                get;
                [NativeMethod("SetSwitchAllowGpuScratchShrinking")]
                set;
            }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int NVNMaxPublicTextureIDCount
            {
                [NativeMethod("GetSwitchNVNMaxPublicTextureIDCount")]
                get;
                [NativeMethod("SetSwitchNVNMaxPublicTextureIDCount")]
                set;
            }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int NVNMaxPublicSamplerIDCount
            {
                [NativeMethod("GetSwitchNVNMaxPublicSamplerIDCount")]
                get;
                [NativeMethod("SetSwitchNVNMaxPublicSamplerIDCount")]
                set;
            }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int NVNGraphicsFirmwareMemory
            {
                [NativeMethod("GetSwitchNVNGraphicsFirmwareMemory")]
                get;
                [NativeMethod("SetSwitchNVNGraphicsFirmwareMemory")]
                set;
            }

            [StaticAccessor("PlayerSettings", StaticAccessorType.DoubleColon)]
            extern public static int defaultSwitchNVNGraphicsFirmwareMemory { get; }

            [StaticAccessor("PlayerSettings", StaticAccessorType.DoubleColon)]
            extern public static int minimumSwitchNVNGraphicsFirmwareMemory { get; }

            [StaticAccessor("PlayerSettings", StaticAccessorType.DoubleColon)]
            extern public static int maximumSwitchNVNGraphicsFirmwareMemory { get; }
			
			[StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int switchMaxWorkerMultiple
            {
                [NativeMethod("GetSwitchKMaxWorkerMultiple")]
                get;
                [NativeMethod("SetSwitchKMaxWorkerMultiple")]
                set;
            }

            // Controls the behavior of Switch's auto-changing screen resolution
            [NativeProperty("switchScreenResolutionBehavior", TargetType.Field)]
            extern public static ScreenResolutionBehavior screenResolutionBehavior { get; set; }

            [NativeProperty("switchNMETAOverride", TargetType.Function)]
            extern static private string NMETAOverrideInternal { get; set; }

            public static string NMETAOverride
            {
                get
                {
                    string path = NMETAOverrideInternal;

                    if (string.IsNullOrEmpty(path))
                        return "";

                    return path;
                }
                set
                {
                    NMETAOverrideInternal = value;
                }
            }

            public static string NMETAOverrideFullPath
            {
                get
                {
                    string path = NMETAOverrideInternal;

                    if (string.IsNullOrEmpty(path))
                        return "";

                    if (!Path.IsPathRooted(path))
                        path = Path.GetFullPath(path);

                    return path;
                }
            }

            public static string[] compilerFlags
            {
                get
                {
                    return compilerFlagsInternal.Split(new char[] {' '});
                }
                set
                {
                    compilerFlagsInternal = string.Join(" ", value);
                }
            }

            [NativeProperty("switchCompilerFlags", TargetType.Function)]
            extern private static string compilerFlagsInternal { get; set; }

            //Application ID (shows up in Application meta file)
            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchApplicationID", TargetType.Function)]
            extern public static string applicationID { get; set; }

            //Additional NSO Dependencies
            [NativeProperty("switchNSODependencies", TargetType.Function)]
            extern public static string nsoDependencies { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            extern public static string[] titleNames
            {
                [NativeMethod("GetSwitchTitleNames")]
                get;
                [NativeMethod("SetSwitchTitleNames")]
                set;
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            extern public static string[] publisherNames
            {
                [NativeMethod("GetSwitchPublisherNames")]
                get;
                [NativeMethod("SetSwitchPublisherNames")]
                set;
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            extern public static Texture2D[] icons
            {
                [NativeMethod("GetSwitchIcons")]
                get;
                [NativeMethod("SetSwitchIcons")]
                set;
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            extern public static Texture2D[] smallIcons
            {
                [NativeMethod("GetSwitchSmallIcons")]
                get;
                [NativeMethod("SetSwitchSmallIcons")]
                set;
            }

            public static string manualHTMLPath
            {
                get
                {
                    string path = manualHTMLPathInternal;

                    if (string.IsNullOrEmpty(path))
                        return "";

                    string fullPath = path;

                    if (!Path.IsPathRooted(fullPath))
                        fullPath = Path.GetFullPath(fullPath);

                    if (!Directory.Exists(fullPath))
                        return "";

                    return fullPath;
                }
                set
                {
                    manualHTMLPathInternal = string.IsNullOrEmpty(value) ? "" : value;
                }
            }

            public static string accessibleURLPath
            {
                get
                {
                    string path = accessibleURLPathInternal;

                    if (string.IsNullOrEmpty(path))
                        return "";

                    string fullPath = path;

                    if (!Path.IsPathRooted(fullPath))
                        fullPath = Path.GetFullPath(fullPath);

                    return fullPath;
                }
                set
                {
                    accessibleURLPathInternal = string.IsNullOrEmpty(value) ? "" : value;
                }
            }

            public static string legalInformationPath
            {
                get
                {
                    string path = legalInformationPathInternal;

                    if (string.IsNullOrEmpty(path))
                        return "";

                    if (!Path.IsPathRooted(path))
                        path = Path.GetFullPath(path);

                    return path;
                }
                set
                {
                    legalInformationPathInternal = string.IsNullOrEmpty(value) ? "" : value;
                }
            }

            [NativeProperty("switchManualHTML", TargetType.Function)]
            extern static private string manualHTMLPathInternal { get; set; }

            [NativeProperty("switchAccessibleURLs", TargetType.Function)]
            extern static private string accessibleURLPathInternal { get; set; }

            [NativeProperty("switchLegalInformation", TargetType.Function)]
            extern static private string legalInformationPathInternal { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchMainThreadStackSize", TargetType.Field)]
            extern public static int mainThreadStackSize { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchPresenceGroupId", TargetType.Function)]
            extern public static string presenceGroupId { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchLogoHandling", TargetType.Field)]
            extern public static LogoHandling logoHandling { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            extern public static string releaseVersion
            {
                [NativeMethod("GetSwitchReleaseVersion")]
                get;
                [NativeMethod("SetSwitchReleaseVersion")]
                set;
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchDisplayVersion", TargetType.Function)]
            extern public static string displayVersion { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchStartupUserAccount", TargetType.Field)]
            extern public static StartupUserAccount startupUserAccount { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchSupportedLanguagesMask", TargetType.Field)]
            extern public static int supportedLanguages { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchLogoType", TargetType.Field)]
            extern public static LogoType logoType { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchApplicationErrorCodeCategory", TargetType.Function)]
            extern public static string applicationErrorCodeCategory { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchUserAccountSaveDataSize", TargetType.Field)]
            extern public static int userAccountSaveDataSize { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchUserAccountSaveDataJournalSize", TargetType.Field)]
            extern public static int userAccountSaveDataJournalSize { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchApplicationAttribute", TargetType.Field)]
            extern public static ApplicationAttribute applicationAttribute { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            public static int cardSpecSize
            {
                get
                {
                    return cardSpecSizeInternal;
                }
                set
                {
                    cardSpecSizeInternal = (value > 0) ? value : -1;
                }
            }

            [NativeProperty("switchCardSpecSize", TargetType.Field)]
            extern private static int cardSpecSizeInternal { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            public static int cardSpecClock
            {
                get
                {
                    return cardSpecClockInternal;
                }
                set
                {
                    cardSpecClockInternal = (value > 0) ? value : -1;
                }
            }

            [NativeProperty("switchCardSpecClock", TargetType.Field)]
            extern private static int cardSpecClockInternal { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchRatingsMask", TargetType.Field)]
            extern public static int ratingsMask { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            extern public static string[] localCommunicationIds
            {
                [NativeMethod("GetSwitchLocalCommunicationIds")]
                get;
                [NativeMethod("SetSwitchLocalCommunicationIds")]
                set;
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchParentalControl", TargetType.Field)]
            extern public static bool isUnderParentalControl { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchAllowsScreenshot", TargetType.Field)]
            extern public static bool isScreenshotEnabled { get; set; }

            [Obsolete("isAllowsScreenshot was renamed to isScreenshotEnabled")]
            [NativeProperty("switchAllowsScreenshot", TargetType.Field)]
            extern public static bool isAllowsScreenshot { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchAllowsVideoCapturing", TargetType.Field)]
            extern public static bool isVideoCapturingEnabled { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchAllowsRuntimeAddOnContentInstall", TargetType.Field)]
            extern public static bool isRuntimeAddOnContentInstallEnabled { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchDataLossConfirmation", TargetType.Field)]
            extern public static bool isDataLossConfirmationEnabled { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            [NativeProperty("switchUserAccountLockEnabled", TargetType.Field)]
            extern public static bool isUserAccountLockEnabled { get; set; }

            [Obsolete("isDataLossConfirmation was renamed to isDataLossConfirmationEnabled")]
            [NativeProperty("switchDataLossConfirmation", TargetType.Field)]
            extern public static bool isDataLossConfirmation { get; set; }


            [NativeProperty("switchSupportedNpadStyles", TargetType.Field)]
            extern public static SupportedNpadStyle supportedNpadStyles { get; set; }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            public static int GetRatingAge(RatingCategories category)
            {
                return ratingAgeArray[(int)category];
            }

            [Obsolete(kPlayerSettingsAreObsoletedWarning)]
            extern public static int[] ratingAgeArray
            {
                [NativeMethod("GetSwitchRatingAges")]
                get;
                [NativeMethod("SetSwitchRatingAges")]
                set;
            }

            [NativeProperty("switchNativeFsCacheSize", TargetType.Field)]
            extern public static int nativeFsCacheSize { get; set; }

            [NativeProperty("switchIsHoldTypeHorizontal", TargetType.Field)]
            extern public static bool isHoldTypeHorizontal { get; set; }

            [NativeProperty("switchSupportedNpadCount", TargetType.Field)]
            extern public static int supportedNpadCount { get; set; }

            [NativeProperty("switchEnableTouchScreen", TargetType.Field)]
            extern public static bool enableTouchScreen { get; set; }

            // SocketConfigEnabled
            [NativeProperty("switchSocketConfigEnabled", TargetType.Field)]
            extern public static bool socketConfigEnabled { get; set; }

            // Tcp Initial Send Buffer Size
            [NativeProperty("switchTcpInitialSendBufferSize", TargetType.Field)]
            extern public static int tcpInitialSendBufferSize { get; set; }

            // Tcp Initial Receive Buffer Size
            [NativeProperty("switchTcpInitialReceiveBufferSize", TargetType.Field)]
            extern public static int tcpInitialReceiveBufferSize { get; set; }

            // Tcp Auto Send Buffer Size Max
            [NativeProperty("switchTcpAutoSendBufferSizeMax", TargetType.Field)]
            extern public static int tcpAutoSendBufferSizeMax { get; set; }

            // Tcp Auto Receive Buffer Size Max
            [NativeProperty("switchTcpAutoReceiveBufferSizeMax", TargetType.Field)]
            extern public static int tcpAutoReceiveBufferSizeMax { get; set; }

            // Udp Send Buffer Size
            [NativeProperty("switchUdpSendBufferSize", TargetType.Field)]
            extern public static int udpSendBufferSize { get; set; }

            // Udp Receive Buffer Size
            [NativeProperty("switchUdpReceiveBufferSize", TargetType.Field)]
            extern public static int udpReceiveBufferSize { get; set; }

            // Socket Buffer Efficiency
            [NativeProperty("switchSocketBufferEfficiency", TargetType.Field)]
            extern public static int socketBufferEfficiency { get; set; }

            // Socket Initialize Enabled
            [NativeProperty("switchSocketInitializeEnabled", TargetType.Field)]
            extern public static bool socketInitializeEnabled { get; set; }

            // Network Interface Manager Initialize Enabled
            [NativeProperty("switchNetworkInterfaceManagerInitializeEnabled", TargetType.Field)]
            extern public static bool networkInterfaceManagerInitializeEnabled { get; set; }

            // Player Connection Enabled
            [NativeProperty("switchPlayerConnectionEnabled", TargetType.Field)]
            extern public static bool playerConnectionEnabled { get; set; }

            // HTCS for player connection
            [NativeProperty("switchDisableHTCSPlayerConnection", TargetType.Field)]
            extern public static bool disableHTCSPlayerConnection { get; set; }

            // Using the new path style system
            [NativeProperty("switchUseNewStyleFilepaths", TargetType.Field)]
            extern public static bool useNewStyleFilepaths { get; set; }

            // Forces all FMOD threads to use nn::os::LowestThreadPriority
            [NativeProperty("switchUseLegacyFmodPriorities", TargetType.Field)]
            extern public static bool switchUseLegacyFmodPriorities { get; set; }

            // Controls if calls to nn::os::YieldThread are swapped with calls to nn::os::SleepThread({switchMicroSleepForYieldTime}us)
            [NativeProperty("switchUseMicroSleepForYield", TargetType.Field)]
            extern public static bool switchUseMicroSleepForYield { get; set; }

            // Number of micro seconds used by switchUseMicroSleepForYield
            [NativeProperty("switchMicroSleepForYieldTime", TargetType.Field)]
            extern public static int switchMicroSleepForYieldTime { get; set; }

            //Enable the RamDisk support
            [NativeProperty("switchEnableRamDiskSupport", TargetType.Field)]
            extern public static bool switchEnableRamDiskSupport { get; set; }

            //To specify how much space should be allocated for the ram disk
            [NativeProperty("switchRamDiskSpaceSize", TargetType.Field)]
            extern public static int switchRamDiskSpaceSize { get; set; }
        }
    }
}
