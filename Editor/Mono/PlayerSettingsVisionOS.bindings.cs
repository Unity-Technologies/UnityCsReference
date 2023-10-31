// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    public enum VisionOSSdkVersion
    {
        Device = 0,
        Simulator = 1
    }

    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public partial class PlayerSettings : UnityEngine.Object
    {
        // VisionOS specific player settings
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
        [StaticAccessor("GetPlayerSettings()")]
        public sealed partial class VisionOS
        {
            private static extern int sdkVersionInt
            {
                [NativeMethod("GetVisionOSSdkVersion")]
                get;
                [NativeMethod("SetVisionOSSdkVersion")]
                set;
            }

            public static VisionOSSdkVersion sdkVersion
            {
                get { return (VisionOSSdkVersion)sdkVersionInt; }
                set { sdkVersionInt = (int)value; }
            }

            // visionos bundle build number
            public static string buildNumber
            {
                get { return PlayerSettings.GetBuildNumber(NamedBuildTarget.VisionOS.TargetName); }
                set { PlayerSettings.SetBuildNumber(NamedBuildTarget.VisionOS.TargetName, value); }
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeMethod("GetVisionOSMinimumVersionString")]
            static extern string GetMinimumVersionString();

            internal static readonly Version minimumOsVersion = new Version(GetMinimumVersionString());

            public static extern string targetOSVersionString
            {
                [NativeMethod("GetVisionOSTargetOSVersion")]
                get;
                [NativeMethod("SetVisionOSTargetOSVersion")]
                set;
            }
        }
    }
}
