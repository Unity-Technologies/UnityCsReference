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

            // Whether to enable file system trace on Nintendo Switch CPU Profiler.
            [NativeProperty("switchEnableFileSystemTrace", TargetType.Field)]
            extern public static bool enableFileSystemTrace { get; set; }

            // What LTO setting to use on Switch.
            [NativeProperty("switchLTOSetting", TargetType.Field)]
            extern public static int switchLTOSetting { get; set; }

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


            //Additional NSO Dependencies
            [NativeProperty("switchNSODependencies", TargetType.Function)]
            extern public static string nsoDependencies { get; set; }

            [NativeProperty("switchSupportedNpadStyles", TargetType.Field)]
            extern public static SupportedNpadStyle supportedNpadStyles { get; set; }

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
