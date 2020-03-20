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
        iPhone11        = 48,
        iPhone11Pro     = 49,
        iPhone11ProMax  = 50,

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
            [FreeFunction("systeminfo::GetDeviceSystemVersion")]
            get;
        }

        extern public static DeviceGeneration generation
        {
            [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
            [FreeFunction("UnityDeviceGeneration")]
            get;
        }

        extern public static string vendorIdentifier
        {
            [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
            [FreeFunction("UnityVendorIdentifier")]
            get;
        }

        // please note that we check both advertisingIdentifier/advertisingTrackingEnabled
        //   usage in scripts to decide if we should enable UNITY_USES_IAD macro (i.e. code that uses iAD and related things)
        // that's why it is VERY important that you use private extern functions instead of properties in internal/implementation code

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("UnityAdvertisingIdentifier")]
        extern private static string GetAdvertisingIdentifier();

        public static string advertisingIdentifier
        {
            get
            {
                string advertisingId = GetAdvertisingIdentifier();
                Application.InvokeOnAdvertisingIdentifierCallback(advertisingId, IsAdvertisingTrackingEnabled());
                return advertisingId;
            }
        }

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("IOSScripting::IsAdvertisingTrackingEnabled")]
        extern private static bool IsAdvertisingTrackingEnabled();

        public static bool advertisingTrackingEnabled
        {
            get
            {
                return IsAdvertisingTrackingEnabled();
            }
        }

        extern public static bool hideHomeButton
        {
            [NativeConditional("PLATFORM_IPHONE")]
            [FreeFunction("IOSScripting::GetHideHomeButton")] get;
            [NativeConditional("PLATFORM_IPHONE")]
            [FreeFunction("IOSScripting::SetHideHomeButton")] set;
        }

        extern private static int deferSystemGesturesModeInternal
        {
            [NativeConditional("PLATFORM_IPHONE")]
            [FreeFunction("IOSScripting::GetDeferSystemGesturesMode")] get;
            [NativeConditional("PLATFORM_IPHONE")]
            [FreeFunction("IOSScripting::SetDeferSystemGesturesMode")] set;
        }

        public static SystemGestureDeferMode deferSystemGesturesMode
        {
            get { return (SystemGestureDeferMode)deferSystemGesturesModeInternal; }
            set { deferSystemGesturesModeInternal = (int)value; }
        }

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [NativeMethod(Name = "IOSScripting::SetNoBackupFlag", IsFreeFunction = true, IsThreadSafe = true)]
        extern public static void SetNoBackupFlag(string path);

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [NativeMethod(Name = "IOSScripting::ResetNoBackupFlag", IsFreeFunction = true, IsThreadSafe = true)]
        extern public static void ResetNoBackupFlag(string path);

        [NativeConditional("PLATFORM_IPHONE")]
        [NativeMethod(Name = "IOSScripting::RequestStoreReview", IsFreeFunction = true, IsThreadSafe = true)]
        extern public static bool RequestStoreReview();
    }
}
