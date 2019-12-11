// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.XR.Daydream
{
    public enum SupportedHeadTracking
    {
        ThreeDoF = 0,
        SixDoF = 1
    }
}

namespace UnityEditor
{
    public sealed partial class PlayerSettings : UnityEngine.Object
    {
        [NativeHeader("Editor/Mono/PlayerSettingsVRGoogle.bindings.h")]
        public static class VRCardboard
        {
            [Obsolete("This API is obsolete, and should no longer be used. Existing projects should continue to use 2018.4 LTS.")]
            [NativeProperty("depthFormat", TargetType.Field)]
            public extern static int depthFormat
            {
                [StaticAccessor("GetGoogleCardboardSettings()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetGoogleCardboardSettingsForUpdate()", StaticAccessorType.Dot)] set;
            }
        }

        [NativeHeader("Runtime/Graphics/Texture2D.h")]
        public static class VRDaydream
        {
            [Obsolete("This API is obsolete, and should no longer be used. More information with the latest recommendation can be found here <https://developers.google.com/vr/develop/unity/get-started-android>.")]
            [NativeProperty("daydreamIconForeground")]
            public extern static Texture2D daydreamIcon
            {
                [StaticAccessor("GetGoogleVREditorOnlySettings()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetGoogleVREditorOnlySettingsForUpdate()", StaticAccessorType.Dot)] set;
            }

            [Obsolete("This API is obsolete, and should no longer be used. More information with the latest recommendation can be found here <https://developers.google.com/vr/develop/unity/get-started-android>.")]
            [NativeProperty("daydreamIconBackground")]
            public extern static Texture2D daydreamIconBackground
            {
                [StaticAccessor("GetGoogleVREditorOnlySettings()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetGoogleVREditorOnlySettingsForUpdate()", StaticAccessorType.Dot)] set;
            }

            [Obsolete("This API is obsolete, and should no longer be used. More information with the latest recommendation can be found here <https://developers.google.com/vr/develop/unity/get-started-android>.")]
            [NativeProperty("depthFormat", TargetType.Field)]
            public extern static int depthFormat
            {
                [StaticAccessor("GetGoogleVRSettings()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetGoogleVRSettingsForUpdate()", StaticAccessorType.Dot)] set;
            }

            [Obsolete("This API is obsolete, and should no longer be used. More information with the latest recommendation can be found here <https://developers.google.com/vr/develop/unity/get-started-android>.")]
            [NativeProperty("minimumSupportedHeadTracking", TargetType.Field)]
            public extern static XR.Daydream.SupportedHeadTracking minimumSupportedHeadTracking
            {
                [StaticAccessor("GetGoogleVRSettings()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetGoogleVRSettingsForUpdate()", StaticAccessorType.Dot)] set;
            }

            [Obsolete("This API is obsolete, and should no longer be used. More information with the latest recommendation can be found here <https://developers.google.com/vr/develop/unity/get-started-android>.")]
            [NativeProperty("maximumSupportedHeadTracking", TargetType.Field)]
            public extern static XR.Daydream.SupportedHeadTracking maximumSupportedHeadTracking
            {
                [StaticAccessor("GetGoogleVRSettings()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetGoogleVRSettingsForUpdate()", StaticAccessorType.Dot)] set;
            }

            [Obsolete("This API is obsolete, and should no longer be used. More information with the latest recommendation can be found here <https://developers.google.com/vr/develop/unity/get-started-android>.")]
            [NativeProperty("enableVideoLayer", TargetType.Field)]
            public extern static bool enableVideoSurface
            {
                [StaticAccessor("GetGoogleVRSettings()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetGoogleVRSettingsForUpdate()", StaticAccessorType.Dot)] set;
            }

            [Obsolete("This API is obsolete, and should no longer be used. More information with the latest recommendation can be found here <https://developers.google.com/vr/develop/unity/get-started-android>.")]
            [NativeProperty("useProtectedVideoMemory", TargetType.Field)]
            public extern static bool enableVideoSurfaceProtectedMemory
            {
                [StaticAccessor("GetGoogleVRSettings()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetGoogleVRSettingsForUpdate()", StaticAccessorType.Dot)] set;
            }
        }
    }
}
