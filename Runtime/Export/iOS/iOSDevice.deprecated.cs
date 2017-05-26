// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [Obsolete("iPhoneGeneration enumeration is deprecated. Please use iOS.DeviceGeneration instead (UnityUpgradable) -> UnityEngine.iOS.DeviceGeneration", true)]
    public enum iPhoneGeneration
    {
        Unknown,
        iPhone,
        iPhone3G,
        iPhone3GS,
        iPodTouch1Gen,
        iPodTouch2Gen,
        iPodTouch3Gen,
        iPad1Gen,
        iPhone4,
        iPodTouch4Gen,
        iPad2Gen,
        iPhone4S,
        iPad3Gen,
        iPhone5,
        iPodTouch5Gen,
        iPadMini1Gen,
        iPad4Gen,
        iPhone5C,
        iPhone5S,
        iPhoneUnknown,
        iPadUnknown,
        iPodTouchUnknown
    }

    [Obsolete("iPhone class is deprecated. Please use iOS.Device instead (UnityUpgradable) -> UnityEngine.iOS.Device", true)]
    public sealed class iPhone
    {
        public static iPhoneGeneration generation { get { return default(iPhoneGeneration); } }
        public static string vendorIdentifier { get { return default(string); } }
        public static string advertisingIdentifier { get { return default(string); } }
        public static bool advertisingTrackingEnabled { get { return default(bool); } }
        public static void SetNoBackupFlag(string path) {}
        public static void ResetNoBackupFlag(string path) {}
    }

    [Obsolete("iOSActivityIndicatorStyle enumeration is deprecated. Please use iOS.ActivityIndicatorStyle instead (UnityUpgradable) -> UnityEngine.iOS.ActivityIndicatorStyle", true)]
    public enum iOSActivityIndicatorStyle
    {
        DontShow,
        WhiteLarge,
        White,
        Gray
    }
}

