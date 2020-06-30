// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditorInternal.VR
{
    [NativeHeader("Modules/VREditor/VREditor.bindings.h")]
    public sealed partial class VREditor
    {
        extern public static bool GetVREnabledOnTargetGroup(BuildTargetGroup targetGroup);

        extern public static void SetVREnabledOnTargetGroup(BuildTargetGroup targetGroup, bool value);


        [NativeMethod("SetVREnabledDevicesOnTargetGroup")]
        extern public static void NativeSetVREnabledDevicesOnTargetGroup(BuildTargetGroup targetGroup, string[] devices);
        public static void SetVREnabledDevicesOnTargetGroup(BuildTargetGroup targetGroup, string[] devices)
        {
            NativeSetVREnabledDevicesOnTargetGroup(targetGroup, devices);
        }
    }
}

// When Nested classes is supported for bindings, the above Internal only class should be removed and
// the below class should be updated with the proper PlayerSettings calls.
namespace UnityEditor
{
    partial class PlayerSettings
    {
        // TODO: Delete this once Windows MR XR Plugin package removes it's dependency on it.
        [Obsolete("This API is deprecated and will be removed in 2020.2.", false)]
        public static class VRWindowsMixedReality
        {
            [Obsolete("This API is deprecated and will be removed in 2020.2.", false)]
            public enum DepthBufferFormat
            {
                DepthBufferFormat16Bit = 0,
                DepthBufferFormat24Bit = 1
            }

            [Obsolete("This API is deprecated and will be removed in 2020.2.", false)]
            public static DepthBufferFormat depthBufferFormat { get; set; }

            [Obsolete("This API is deprecated and will be removed in 2020.2.", false)]
            public static bool depthBufferSharingEnabled { get; set; }
        }
    }
}
