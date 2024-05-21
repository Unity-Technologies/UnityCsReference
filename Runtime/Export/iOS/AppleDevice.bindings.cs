// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

// we do have Device class for iOS and tvOS in separate namespaces
// but in reality their implementation (mostly) uses api shared across all apple platforms
// in here we extract common function bindings to have them in on place, so that c# implementation is nicer
namespace UnityEngine.Apple
{
    [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
    [NativeHeader("PlatformDependent/iPhonePlayer/IOSScriptBindings.h")]
    internal static partial class Device
    {
        extern internal static string systemVersion
        {
            [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
            [FreeFunction("systeminfo::GetDeviceSystemVersion")]
            get;
        }

        // please note that we per-platform DeviceGeneration enums in c# - we leave the casting to platform-specific Device class
        extern internal static int generation
        {
            [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
            [FreeFunction("UnityDeviceGeneration")]
            get;
        }

        extern internal static string vendorIdentifier
        {
            [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
            [FreeFunction("UnityVendorIdentifier")]
            get;
        }

        [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
        [NativeMethod(Name = "IOSScripting::SetNoBackupFlag", IsFreeFunction = true, IsThreadSafe = true)]
        extern internal static void SetNoBackupFlag(string path);

        [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
        [NativeMethod(Name = "IOSScripting::ResetNoBackupFlag", IsFreeFunction = true, IsThreadSafe = true)]
        extern internal static void ResetNoBackupFlag(string path);

        extern internal static bool lowPowerModeEnabled
        {
            [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
            [FreeFunction("IOSScripting::GetLowPowerModeEnabled")] get;
        }

        // advertisement

        // NOTE: for ads, we need to decide upfront if we are using them or not (this is very important due to AppStore restrictions and extra steps needed)
        // to do so we check for the usage of the properties "advertisingIdentifier" and "advertisingTrackingEnabled"
        // that means that we MUST use functions for bindings (they will be emitted in c# code, that is "used")
        // NOTE: there is no ASIdentifierManager on VisionOS, and tracking should be done through App Tracking Transparency

        [NativeConditional("PLATFORM_IOS || PLATFORM_TVOS")]
        [FreeFunction("UnityAdIdentifier")]
        extern internal static string GetAdIdentifier();

        [NativeConditional("PLATFORM_IOS || PLATFORM_TVOS")]
        [FreeFunction("IOSScripting::IsAdTrackingEnabled")]
        extern internal static bool IsAdTrackingEnabled();

        // host platform

        extern internal static bool iosAppOnMac
        {
            [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
            [FreeFunction("IOSScripting::IsIosAppOnMac")] get;
        }

        extern internal static bool runsOnSimulator
        {
            [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
            [FreeFunction("IOSScripting::IsRunningOnSimulator")] get;
        }
    }
}

