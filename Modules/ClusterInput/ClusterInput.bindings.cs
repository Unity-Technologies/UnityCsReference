// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [Obsolete("This type is deprecated and will be removed in Unity 7.", false)]
    public enum ClusterInputType
    {
        Button = 0,
        Axis = 1,
        Tracker = 2,
        CustomProvidedInput = 3
    }

    [NativeHeader("Modules/ClusterInput/ClusterInput.h")]
    [NativeConditional("ENABLE_CLUSTERINPUT")]
    [Obsolete("This type is deprecated and will be removed in Unity 7.", false)]
    public class ClusterInput
    {
        extern public static float GetAxis(string name);
        extern public static bool GetButton(string name);
        [NativeConditional("ENABLE_CLUSTERINPUT", "Vector3f(0.0f, 0.0f, 0.0f)")]
        extern public static Vector3 GetTrackerPosition(string name);
        [NativeConditional("ENABLE_CLUSTERINPUT", "Quartenion::identity")]
        extern public static Quaternion GetTrackerRotation(string name);

        extern public static void SetAxis(string name, float value);
        extern public static void SetButton(string name, bool value);
        extern public static void SetTrackerPosition(string name, Vector3 value);
        extern public static void SetTrackerRotation(string name, Quaternion value);

        extern public static bool AddInput(string name, string deviceName, string serverUrl, int index, ClusterInputType type);
        extern public static bool EditInput(string name, string deviceName, string serverUrl, int index, ClusterInputType type);
        extern public static bool CheckConnectionToServer(string name);
    }
}
