// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal.VR
{
    public class VRModule
    {
        private static bool IsTargetingCardboardOnIOS(BuildTarget target)
        {
            return target == BuildTarget.iOS && VREditor.GetVREnabledOnTargetGroup(BuildPipeline.GetBuildTargetGroup(target)) &&
                VREditor.IsVRDeviceEnabledForBuildTarget(target, "cardboard");
        }

        public static void SetupBuildSettings(BuildTarget target, int osVerMajor)
        {
            if (IsTargetingCardboardOnIOS(target) && osVerMajor < 8)
            {
                Debug.LogWarning(string.Format("Deployment target version is set to {0}, but Cardboard supports only versions starting from 8.0.", osVerMajor));
            }
        }

        public static bool ShouldInjectVRDependenciesForBuildTarget(BuildTarget target)
        {
            if (!VREditor.GetVREnabledOnTargetGroup(BuildPipeline.GetBuildTargetGroup(target)))
                return false;

            VRDeviceInfoEditor[] enabledVRDevices = VREditor.GetEnabledVRDeviceInfo(target);

            return (enabledVRDevices.Length > 0);
        }
    }
}
