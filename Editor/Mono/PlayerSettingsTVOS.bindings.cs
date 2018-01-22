// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    public enum tvOSSdkVersion
    {
        Device = 0,
        Simulator = 1
    }

    [System.Obsolete("targetOSVersion is obsolete. Use targetOSVersionString instead.", false)]
    public enum tvOSTargetOSVersion
    {
        Unknown = 0,
        tvOS_9_0 = 900,
        tvOS_9_1 = 901,
    }

    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public partial class PlayerSettings : UnityEngine.Object
    {
        // tvOS specific player settings
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
        [StaticAccessor("GetPlayerSettings()")]
        public partial class tvOS
        {
            private static extern int sdkVersionInt
            {
                [NativeMethod("GettvOSSdkVersion")]
                get;
                [NativeMethod("SettvOSSdkVersion")]
                set;
            }

            public static tvOSSdkVersion sdkVersion
            {
                get { return (tvOSSdkVersion)sdkVersionInt; }
                set { sdkVersionInt = (int)value; }
            }

            // tvOS bundle build number
            public static string buildNumber
            {
                get { return PlayerSettings.GetBuildNumber(BuildTargetGroup.tvOS); }
                set { PlayerSettings.SetBuildNumber(BuildTargetGroup.tvOS, value); }
            }

            [System.Obsolete("targetOSVersion is obsolete. Use targetOSVersionString instead.", false)]
            public static tvOSTargetOSVersion targetOSVersion
            {
                get
                {
                    string version = targetOSVersionString;
                    if (version == "9.0")
                        return tvOSTargetOSVersion.tvOS_9_0;
                    else if (version == "9.1")
                        return tvOSTargetOSVersion.tvOS_9_1;
                    return tvOSTargetOSVersion.Unknown;
                }
                set
                {
                    string version = "";
                    if (value == tvOSTargetOSVersion.tvOS_9_0)
                        version = "9.0";
                    else if (value == tvOSTargetOSVersion.tvOS_9_1)
                        version = "9.1";

                    targetOSVersionString = version;
                }
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeMethod("GettvOSMinimumVersionString")]
            static extern string GetMinimumVersionString();

            internal static readonly Version minimumOsVersion = new Version(GetMinimumVersionString());

            public static extern string targetOSVersionString
            {
                [NativeMethod("GettvOSTargetOSVersion")]
                get;
                [NativeMethod("SettvOSTargetOSVersion")]
                set;
            }

            [NativeProperty("tvOSSmallIconLayers", TargetType.Field)]
            private static extern Texture2D[] smallIconLayers
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal static Texture2D[] GetSmallIconLayers() { return smallIconLayers; }
            internal static void SetSmallIconLayers(Texture2D[] layers) { smallIconLayers = layers; }


            [NativeProperty("tvOSSmallIconLayers2x", TargetType.Field)]
            private static extern Texture2D[] smallIconLayers2x
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal static Texture2D[] GetSmallIconLayers2x() { return smallIconLayers2x; }
            internal static void SetSmallIconLayers2x(Texture2D[] layers) { smallIconLayers2x = layers; }


            [NativeProperty("tvOSLargeIconLayers", TargetType.Field)]
            private static extern Texture2D[] largeIconLayers
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal static Texture2D[] GetLargeIconLayers() { return largeIconLayers; }
            internal static void SetLargeIconLayers(Texture2D[] layers) { largeIconLayers = layers; }


            [NativeProperty("tvOSLargeIconLayers2x", TargetType.Field)]
            private static extern Texture2D[] largeIconLayers2x
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal static Texture2D[] GetLargeIconLayers2x() { return largeIconLayers2x; }
            internal static void SetLargeIconLayers2x(Texture2D[] layers) { largeIconLayers2x = layers; }


            [NativeProperty("tvOSTopShelfImageLayers", TargetType.Field)]
            private static extern Texture2D[] topShelfImageLayers
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal static Texture2D[] GetTopShelfImageLayers() { return topShelfImageLayers; }
            internal static void SetTopShelfImageLayers(Texture2D[] layers) { topShelfImageLayers = layers; }


            [NativeProperty("tvOSTopShelfImageLayers2x", TargetType.Field)]
            private static extern Texture2D[] topShelfImageLayers2x
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal static Texture2D[] GetTopShelfImageLayers2x() { return topShelfImageLayers2x; }
            internal static void SetTopShelfImageLayers2x(Texture2D[] layers) { topShelfImageLayers2x = layers; }

            [NativeProperty("tvOSTopShelfImageWideLayers", TargetType.Field)]
            private static extern Texture2D[] topShelfImageWideLayers
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal static Texture2D[] GetTopShelfImageWideLayers() { return topShelfImageWideLayers; }
            internal static void SetTopShelfImageWideLayers(Texture2D[] layers) { topShelfImageWideLayers = layers; }


            [NativeProperty("tvOSTopShelfImageWideLayers2x", TargetType.Field)]
            private static extern Texture2D[] topShelfImageWideLayers2x
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal static Texture2D[] GetTopShelfImageWideLayers2x() { return topShelfImageWideLayers2x; }
            internal static void SetTopShelfImageWideLayers2x(Texture2D[] layers) { topShelfImageWideLayers2x = layers; }

            // AppleTV Enable extended game controller
            public static extern bool requireExtendedGameController
            {
                [NativeMethod("GettvOSRequireExtendedGameController")]
                get;
                [NativeMethod("SettvOSRequireExtendedGameController")]
                set;
            }
        }
    }
}
