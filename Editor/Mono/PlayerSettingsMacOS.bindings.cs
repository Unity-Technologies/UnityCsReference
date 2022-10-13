// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build;
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
                get { return PlayerSettings.GetBuildNumber(NamedBuildTarget.Standalone.TargetName); }
                set { PlayerSettings.SetBuildNumber(NamedBuildTarget.Standalone.TargetName, value); }
            }

            [NativeProperty("MacAppStoreCategory")]
            public extern static string applicationCategoryType  { get; set; }

            // these four WERE internal because we are not yet sure if we want to have them back in general PlayerSettings
            // as we have several platforms that might want to use it
            [NativeProperty("CameraUsageDescription")]
            public extern static string cameraUsageDescription { get; set; }

            [NativeProperty("MicrophoneUsageDescription")]
            public extern static string microphoneUsageDescription { get; set; }

            [NativeProperty("BluetoothUsageDescription")]
            public extern static string bluetoothUsageDescription { get; set; }

            [NativeProperty("macOSURLSchemes", false, TargetType.Function)]
            public extern static string[] urlSchemes
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("macOSTargetOSVersion")]
            public extern static string targetOSVersion { get; set; }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeMethod("GetMacOSMinimumVersionString")]
            internal static extern string GetMinimumVersionString();
            internal static readonly Version minimumOsVersion = new Version(GetMinimumVersionString());
        }
    }
}
