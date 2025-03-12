// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.iOS
{
    // keep in sync with DeviceGeneration enum in trampoline.
    public enum DeviceGeneration
    {
        Unknown         = 0,
        iPhone          = 1,
        iPhone3G        = 2,
        iPhone3GS       = 3,
        iPodTouch1Gen   = 4,
        iPodTouch2Gen   = 5,
        iPodTouch3Gen   = 6,
        iPad1Gen        = 7,
        iPhone4         = 8,
        iPodTouch4Gen   = 9,
        iPad2Gen        = 10,
        iPhone4S        = 11,
        iPad3Gen        = 12,
        iPhone5         = 13,
        iPodTouch5Gen   = 14,
        iPadMini1Gen    = 15,
        iPad4Gen        = 16,
        iPhone5C        = 17,
        iPhone5S        = 18,
        iPadAir1        = 19,
        iPadMini2Gen    = 20,
        iPhone6         = 21,
        iPhone6Plus     = 22,
        iPadMini3Gen    = 23,
        iPadAir2        = 24,
        iPhone6S        = 25,
        iPhone6SPlus    = 26,
        iPadPro1Gen     = 27,
        iPadMini4Gen    = 28,
        iPhoneSE1Gen    = 29,
        iPadPro10Inch1Gen = 30,
        iPhone7         = 31,
        iPhone7Plus     = 32,
        iPodTouch6Gen   = 33,
        iPad5Gen        = 34,
        iPadPro2Gen     = 35,
        iPadPro10Inch2Gen = 36,
        iPhone8         = 37,
        iPhone8Plus     = 38,
        iPhoneX         = 39,
        iPhoneXS        = 40,
        iPhoneXSMax     = 41,
        iPhoneXR        = 42,
        iPadPro11Inch   = 43,
        iPadPro3Gen     = 44,
        iPad6Gen        = 45,
        iPadAir3Gen     = 46,
        iPadMini5Gen    = 47,
        iPhone11        = 48,
        iPhone11Pro     = 49,
        iPhone11ProMax  = 50,
        iPodTouch7Gen   = 51,
        iPad7Gen        = 52,
        iPhoneSE2Gen    = 53,
        iPadPro11Inch2Gen = 54,
        iPadPro4Gen     = 55,
        iPhone12Mini    = 56,
        iPhone12        = 57,
        iPhone12Pro     = 58,
        iPhone12ProMax  = 59,
        iPad8Gen        = 60,
        iPadAir4Gen     = 61,
        iPad9Gen        = 62,
        iPadMini6Gen    = 63,
        iPhone13        = 64,
        iPhone13Mini    = 65,
        iPhone13Pro     = 66,
        iPhone13ProMax  = 67,
        iPadPro5Gen     = 68,
        iPadPro11Inch3Gen = 69,
        iPhoneSE3Gen    = 70,
        iPadAir5Gen     = 71,
        iPhone14        = 72,
        iPhone14Plus    = 73,
        iPhone14Pro     = 74,
        iPhone14ProMax  = 75,
        iPadPro6Gen     = 76,
        iPadPro11Inch4Gen = 77,
        iPad10Gen       = 78,
        iPhone15        = 79,
        iPhone15Plus    = 80,
        iPhone15Pro     = 81,
        iPhone15ProMax  = 82,
        iPhone16        = 83,
        iPhone16Plus    = 84,
        iPhone16Pro     = 85,
        iPhone16ProMax  = 86,
        iPhone16e       = 87,

        iPhoneUnknown       = 10001,
        iPadUnknown         = 10002,
        iPodTouchUnknown    = 10003,
    }

    public enum ActivityIndicatorStyle
    {
        DontShow = -1,  // Do not show ActivityIndicator
        WhiteLarge = 0, // The large white style of indicator (UIActivityIndicatorViewStyleWhiteLarge).
        White = 1,      // The standard white style of indicator (UIActivityIndicatorViewStyleWhite).
        Gray = 2,       // The standard gray style of indicator (UIActivityIndicatorViewStyleGray).
    }

    [NativeHeader("PlatformDependent/iPhonePlayer/IOSScriptBindings.h")]
    public sealed partial class Device
    {
        extern public static string systemVersion
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("systeminfo::GetDeviceSystemVersion")]
            get;
        }

        extern public static DeviceGeneration generation
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("UnityDeviceGeneration")]
            get;
        }

        extern public static string vendorIdentifier
        {
            [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
            [FreeFunction("UnityVendorIdentifier")]
            get;
        }

        // please note that we check both advertisingIdentifier/advertisingTrackingEnabled
        //   usage in scripts to decide if we should enable UNITY_USES_IAD macro (i.e. code that uses iAD and related things)
        // that's why it is VERY important that you use private extern functions instead of properties in internal/implementation code
        // as another caveat, apple seems to grep app strings naively when checking for usages of this api
        //   poterntially finding UnityAdvertisingIdentifier/IsAdvertisingTrackingEnabled
        // thats why we renamed these functions to be less like apple api

        [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
        [FreeFunction("UnityAdIdentifier")]
        extern private static string GetAdIdentifier();

        public static string advertisingIdentifier
        {
            get
            {
                string advertisingId = GetAdIdentifier();
                Application.InvokeOnAdvertisingIdentifierCallback(advertisingId, IsAdTrackingEnabled());
                return advertisingId;
            }
        }

        [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
        [FreeFunction("IOSScripting::IsAdTrackingEnabled")]
        extern private static bool IsAdTrackingEnabled();

        public static bool advertisingTrackingEnabled
        {
            get
            {
                return IsAdTrackingEnabled();
            }
        }

        extern public static bool hideHomeButton
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::GetHideHomeButton")] get;
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::SetHideHomeButton")] set;
        }

        extern public static bool lowPowerModeEnabled
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::GetLowPowerModeEnabled")] get;
        }

        extern public static bool wantsSoftwareDimming
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::GetWantsSoftwareDimming")] get;
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::SetWantsSoftwareDimming")] set;
        }

        extern public static bool iosAppOnMac
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::GetIosAppOnMac")] get;
        }

        extern private static int deferSystemGesturesModeInternal
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::GetDeferSystemGesturesMode")] get;
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::SetDeferSystemGesturesMode")] set;
        }

        public static SystemGestureDeferMode deferSystemGesturesMode
        {
            get { return (SystemGestureDeferMode)deferSystemGesturesModeInternal; }
            set { deferSystemGesturesModeInternal = (int)value; }
        }

        [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
        [NativeMethod(Name = "IOSScripting::SetNoBackupFlag", IsFreeFunction = true, IsThreadSafe = true)]
        extern public static void SetNoBackupFlag(string path);

        [NativeConditional("PLATFORM_APPLE_NONDESKTOP")]
        [NativeMethod(Name = "IOSScripting::ResetNoBackupFlag", IsFreeFunction = true, IsThreadSafe = true)]
        extern public static void ResetNoBackupFlag(string path);

        [NativeConditional("PLATFORM_IOS")]
        [NativeMethod(Name = "IOSScripting::RequestStoreReview", IsFreeFunction = true, IsThreadSafe = true)]
        extern public static bool RequestStoreReview();
    }
}
