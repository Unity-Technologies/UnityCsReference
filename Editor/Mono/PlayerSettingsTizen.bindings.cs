// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // Supported Tizen OS versions
    public enum TizenOSVersion
    {
        // Tizen 2.3
        //Version23 = 0, Removed in 5.5 for store compatibility

        // Tizen 2.4
        Version24 = 1
    }

    public enum TizenShowActivityIndicatorOnLoading
    {
        // Large == progressBarStyleLarge
        Large = 0,

        // Inversed Large == progressBarStyleLargeInverse
        InversedLarge = 1,

        // Small == progressBarStyleSmall
        Small = 2,

        // Inversed Small == progressBarStyleSmallInverse
        InversedSmall = 3,

        // Don't Show
        DontShow = -1
    }

    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public sealed partial class PlayerSettings : UnityEngine.Object
    {
        public enum TizenCapability
        {
            Location = 0,
            DataSharing = 1,
            NetworkGet = 2,
            WifiDirect = 3,
            CallHistoryRead = 4,
            Power = 5,
            ContactWrite = 6,
            MessageWrite = 7,
            ContentWrite = 8,
            Push = 9,
            AccountRead = 10,
            ExternalStorage = 11,
            Recorder = 12,
            PackageManagerInfo = 13,
            NFCCardEmulation = 14,
            CalendarWrite = 15,
            WindowPrioritySet = 16,
            VolumeSet = 17,
            CallHistoryWrite = 18,
            AlarmSet = 19,
            Call = 20,
            Email = 21,
            ContactRead = 22,
            Shortcut = 23,
            KeyManager = 24,
            LED = 25,
            NetworkProfile = 26,
            AlarmGet = 27,
            Display = 28,
            CalendarRead = 29,
            NFC = 30,
            AccountWrite = 31,
            Bluetooth = 32,
            Notification = 33,
            NetworkSet = 34,
            ExternalStorageAppData = 35,
            Download = 36,
            Telephony = 37,
            MessageRead = 38,
            MediaStorage = 39,
            Internet = 40,
            Camera = 41,
            Haptic = 42,
            AppManagerLaunch = 43,
            SystemSettings = 44
        }

        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [NativeHeader("Editor/Src/PlayerSettingsTizen.bindings.h")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        public sealed partial class Tizen
        {
            [NativeProperty("TizenProductDescription")] extern public static string productDescription { get; set; }
            [NativeProperty("TizenProductURL")] extern public static string productURL { get; set; }
            [NativeProperty("TizenSigningProfileName")] extern public static string signingProfileName { get; set; }
            [NativeProperty("TizenDeploymentTarget")] extern public static string deploymentTarget { get; set; }
            [NativeProperty("TizenDeploymentTargetType")] extern public static int deploymentTargetType { get; set; }
            [NativeProperty("TizenMinOSVersion")] extern public static TizenOSVersion minOSVersion { get; set; }
            [NativeProperty("TizenShowActivityIndicatorOnLoading")] extern public static TizenShowActivityIndicatorOnLoading showActivityIndicatorOnLoading { get; set; }

            [FreeFunction("PlayerSettingsTizenBindings::SetCapability")]
            private static extern void SetCapability(string name, string value);

            [FreeFunction("PlayerSettingsTizenBindings::GetCapability")]
            private static extern string GetCapability(string name);

            public static void SetCapability(TizenCapability capability, bool value)
            {
                SetCapability(capability.ToString(), value.ToString());
            }

            public static bool GetCapability(TizenCapability capability)
            {
                string stringValue = GetCapability(capability.ToString());

                if (string.IsNullOrEmpty(stringValue))
                    return false;

                try
                {
                    return (bool)System.ComponentModel.TypeDescriptor.GetConverter(typeof(bool)).ConvertFromString(stringValue);
                }
                catch
                {
                    Debug.LogError("Failed to parse value  ('" + capability.ToString() + "," + stringValue + "') to bool type.");
                    return false;
                }
            }
        }
    }
}
