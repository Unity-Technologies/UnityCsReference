// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    public partial class PlayerSettings : UnityEngine.Object
    {
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        public class macOS
        {
            public static string buildNumber
            {
                get { return PlayerSettings.GetBuildNumber(BuildTargetGroup.Standalone); }
                set { PlayerSettings.SetBuildNumber(BuildTargetGroup.Standalone, value); }
            }

            [NativeProperty("MacAppStoreCategory")]
            public extern static string applicationCategoryType  { get; set; }

            // these three WERE internal because we are not yet sure if we want to have them back in general PlayerSettings
            // as we have several platforms that might want to use it
            [NativeProperty("CameraUsageDescription")]
            public extern static string cameraUsageDescription { get; set; }

            [NativeProperty("MicrophoneUsageDescription")]
            public extern static string microphoneUsageDescription { get; set; }

            [NativeProperty("BluetoothUsageDescription")]
            public extern static string bluetoothUsageDescription { get; set; }
        }
    }
}
