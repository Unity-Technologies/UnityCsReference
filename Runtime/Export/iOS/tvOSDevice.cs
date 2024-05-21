// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using AppleDevice = UnityEngine.Apple.Device;

namespace UnityEngine.tvOS
{
    // keep in sync with DeviceGeneration enum in trampoline.
    public enum DeviceGeneration
    {
        Unknown = 0,
        [System.Obsolete(@"AppleTV1Gen has been renamed. Use AppleTVHD instead (UnityUpgradable) -> AppleTVHD", false)] AppleTV1Gen = 1001,
        AppleTVHD = 1001,
        [System.Obsolete(@"AppleTV2Gen has been renamed. Use AppleTV4K instead (UnityUpgradable) -> AppleTV4K", false)] AppleTV2Gen = 1002,
        AppleTV4K = 1002,
        AppleTV4K2Gen = 1003,
        AppleTV4K3Gen = 1004,
    }


    public sealed partial class Device
    {
        public static string systemVersion => AppleDevice.systemVersion;
        public static DeviceGeneration generation => (DeviceGeneration)AppleDevice.generation;
        public static string vendorIdentifier => AppleDevice.vendorIdentifier;

        public static void SetNoBackupFlag(string path) => AppleDevice.SetNoBackupFlag(path);
        public static void ResetNoBackupFlag(string path) => AppleDevice.ResetNoBackupFlag(path);

        public static bool advertisingTrackingEnabled
        {
            get{ return AppleDevice.IsAdTrackingEnabled(); }
        }
        public static string advertisingIdentifier
        {
            get
            {
                string advertisingId = AppleDevice.GetAdIdentifier();
                Application.InvokeOnAdvertisingIdentifierCallback(advertisingId, AppleDevice.IsAdTrackingEnabled());
                return advertisingId;
            }
        }

        public static bool runsOnSimulator => AppleDevice.runsOnSimulator;
    }
}
