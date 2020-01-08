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
            public enum ScreenResolutionBehavior
            {
                Manual = 0,
                OperationMode = 1,
                PerformanceMode = 2,
                Both = 3
            }

            public enum Languages
            {
                AmericanEnglish,
                BritishEnglish,
                Japanese,
                French,
                German,
                LatinAmericanSpanish,
                Spanish,
                Italian,
                Dutch,
                CanadianFrench,
                Portuguese,
                Russian,
                SimplifiedChinese,
                TraditionalChinese,
                Korean,
            }

            public enum
            StartupUserAccount
            {
                None = 0,
                Required = 1,
                RequiredWithNetworkServiceAccountAvailable = 2
            }

            public enum TouchScreenUsage
            {
                Supported = 0,
                Required = 1,
                None = 2
            }

            public enum LogoHandling
            {
                Auto = 0,
                Manual = 1
            }

            public enum LogoType
            {
                LicensedByNintendo = 0,
                [Obsolete("This attribute is no longer available as of NintendoSDK 4.3.", true)]
                DistributedByNintendo = 1,
                Nintendo = 2
            }

            public enum ApplicationAttribute
            {
                None = 0,
                Demo = 1
            }

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

            //Application ID (shows up in Application meta file)
            [NativeProperty("switchApplicationID", TargetType.Function)]
            extern public static string applicationID { get; set; }

            //Additional NSO Dependencies
            [NativeProperty("switchNSODependencies", TargetType.Function)]
            extern public static string nsoDependencies { get; set; }

            extern public static string[] titleNames
            {
                [NativeMethod("GetSwitchTitleNames")]
                get;
                [NativeMethod("SetSwitchTitleNames")]
                set;
            }

            extern public static string[] publisherNames
            {
                [NativeMethod("GetSwitchPublisherNames")]
                get;
                [NativeMethod("SetSwitchPublisherNames")]
                set;
            }

            extern public static Texture2D[] icons
            {
                [NativeMethod("GetSwitchIcons")]
                get;
                [NativeMethod("SetSwitchIcons")]
                set;
            }

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

            [NativeProperty("switchMainThreadStackSize", TargetType.Field)]
            extern public static int mainThreadStackSize { get; set; }

            [NativeProperty("switchPresenceGroupId", TargetType.Function)]
            extern public static string presenceGroupId { get; set; }

            [NativeProperty("switchLogoHandling", TargetType.Field)]
            extern public static LogoHandling logoHandling { get; set; }

            extern public static string releaseVersion
            {
                [NativeMethod("GetSwitchReleaseVersion")]
                get;
                [NativeMethod("SetSwitchReleaseVersion")]
                set;
            }

            [NativeProperty("switchDisplayVersion", TargetType.Function)]
            extern public static string displayVersion { get; set; }

            [NativeProperty("switchStartupUserAccount", TargetType.Field)]
            extern public static StartupUserAccount startupUserAccount { get; set; }

            [NativeProperty("switchTouchScreenUsage", TargetType.Field)]
            extern public static TouchScreenUsage touchScreenUsage { get; set; }

            [NativeProperty("switchSupportedLanguagesMask", TargetType.Field)]
            extern public static int supportedLanguages { get; set; }

            [NativeProperty("switchLogoType", TargetType.Field)]
            extern public static LogoType logoType { get; set; }

            [NativeProperty("switchApplicationErrorCodeCategory", TargetType.Function)]
            extern public static string applicationErrorCodeCategory { get; set; }

            [NativeProperty("switchUserAccountSaveDataSize", TargetType.Field)]
            extern public static int userAccountSaveDataSize { get; set; }

            [NativeProperty("switchUserAccountSaveDataJournalSize", TargetType.Field)]
            extern public static int userAccountSaveDataJournalSize { get; set; }

            [NativeProperty("switchApplicationAttribute", TargetType.Field)]
            extern public static ApplicationAttribute applicationAttribute { get; set; }

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

            [NativeProperty("switchRatingsMask", TargetType.Field)]
            extern public static int ratingsMask { get; set; }

            extern public static string[] localCommunicationIds
            {
                [NativeMethod("GetSwitchLocalCommunicationIds")]
                get;
                [NativeMethod("SetSwitchLocalCommunicationIds")]
                set;
            }

            [NativeProperty("switchParentalControl", TargetType.Field)]
            extern public static bool isUnderParentalControl { get; set; }

            [NativeProperty("switchAllowsScreenshot", TargetType.Field)]
            extern public static bool isScreenshotEnabled { get; set; }

            [Obsolete("isAllowsScreenshot was renamed to isScreenshotEnabled")]
            [NativeProperty("switchAllowsScreenshot", TargetType.Field)]
            extern public static bool isAllowsScreenshot { get; set; }

            [NativeProperty("switchAllowsVideoCapturing", TargetType.Field)]
            extern public static bool isVideoCapturingEnabled { get; set; }

            [NativeProperty("switchAllowsRuntimeAddOnContentInstall", TargetType.Field)]
            extern public static bool isRuntimeAddOnContentInstallEnabled { get; set; }

            [NativeProperty("switchDataLossConfirmation", TargetType.Field)]
            extern public static bool isDataLossConfirmationEnabled { get; set; }

            [NativeProperty("switchUserAccountLockEnabled", TargetType.Field)]
            extern public static bool isUserAccountLockEnabled { get; set; }

            [Obsolete("isDataLossConfirmation was renamed to isDataLossConfirmationEnabled")]
            [NativeProperty("switchDataLossConfirmation", TargetType.Field)]
            extern public static bool isDataLossConfirmation { get; set; }


            [NativeProperty("switchSupportedNpadStyles", TargetType.Field)]
            extern public static SupportedNpadStyle supportedNpadStyles { get; set; }

            public static int GetRatingAge(RatingCategories category)
            {
                return ratingAgeArray[(int)category];
            }

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
        }
    }
}
