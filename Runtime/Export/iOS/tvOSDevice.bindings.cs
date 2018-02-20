// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.tvOS
{

    // keep in sync with DeviceGeneration enum in trampoline.
    public enum DeviceGeneration
    {
        Unknown       = 0,
        AppleTV1Gen   = 1001,
        AppleTV2Gen   = 1002,
    }


    // Currently the bindings system does not support identically named class memeber in different namespaces.
    // To avoid conflicts with iOS.Device all properties have an internal extern implementation. These
    // should be removed when the bindings are fixed.
    [NativeHeader("PlatformDependent/iPhonePlayer/IOSScriptBindings.h")]
    public sealed partial class Device
    {
        extern private static string tvOSsystemVersion
        {
            [FreeFunction("systeminfo::GetDeviceSystemVersion")]
            get;
        }

        public static string systemVersion
        {
            get { return tvOSsystemVersion; }
        }

        extern private static DeviceGeneration tvOSGeneration
        {
            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("UnityDeviceGeneration")]
            get;
        }

        public static DeviceGeneration generation
        {
            get { return tvOSGeneration; }
        }

        extern private static string tvOSVendorIdentifier
        {
            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("UnityVendorIdentifier")]
            get;
        }

        public static string vendorIdentifier
        {
            get { return tvOSVendorIdentifier; }
        }

        [NativeConditional("PLATFORM_TVOS")]
        [FreeFunction("UnityAdvertisingIdentifier")]
        extern private static string GettvOSAdvertisingIdentifier();

        public static string advertisingIdentifier
        {
            get
            {
                string advertisingId = GettvOSAdvertisingIdentifier();
                Application.InvokeOnAdvertisingIdentifierCallback(advertisingId, advertisingTrackingEnabled);
                return advertisingId;
            }
        }

        extern private static bool tvOSadvertisingTrackingEnabled
        {
            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("IOSScripting::IsAdvertisingTrackingEnabled")] get;
        }

        public static bool advertisingTrackingEnabled
        {
            get { return tvOSadvertisingTrackingEnabled; }
        }


        [NativeConditional("PLATFORM_TVOS")]
        [NativeMethod(Name = "IOSScripting::SetNoBackupFlag", IsFreeFunction = true, IsThreadSafe = true)]
        extern private static void SettvOSNoBackupFlag(string path);

        public static void SetNoBackupFlag(string path)
        {
            SettvOSNoBackupFlag(path);
        }

        [NativeConditional("PLATFORM_TVOS")]
        [NativeMethod(Name = "IOSScripting::ResetNoBackupFlag", IsFreeFunction = true, IsThreadSafe = true)]
        extern private static void tvOSResetNoBackupFlag(string path);

        public static void ResetNoBackupFlag(string path)
        {
            tvOSResetNoBackupFlag(path);
        }
    }
}
