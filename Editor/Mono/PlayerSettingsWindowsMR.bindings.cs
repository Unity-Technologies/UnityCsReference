// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEditor
{
    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public sealed partial class PlayerSettings : UnityEngine.Object
    {
        [NativeHeader("Editor/Mono/PlayerSettingsWindowsMR.bindings.h")]
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        public static partial class VRWindowsMixedReality
        {
            [Obsolete("This enum is obsolete, and should no longer be used. Please update to the Unity Windows MR XR Plugin package.")]
            public enum DepthBufferFormat
            {
                DepthBufferFormat16Bit = 0,
                DepthBufferFormat24Bit = 1
            }

            [Obsolete("This API is obsolete, and should no longer be used. Please update to the Unity Windows MR XR Plugin package.")]
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            [NativeProperty("vrSettings.hololens.depthFormat", TargetType.Field)]
            public static extern DepthBufferFormat depthBufferFormat { get; set; }

            [Obsolete("This API is obsolete, and should no longer be used. Please update to the Unity Windows MR XR Plugin package.")]
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            [NativeProperty("vrSettings.hololens.depthBufferSharingEnabled", TargetType.Field)]
            public static extern bool depthBufferSharingEnabled { get; set; }
        }
    }
}
