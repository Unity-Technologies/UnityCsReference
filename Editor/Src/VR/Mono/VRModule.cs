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
            return PlayerSettings.virtualRealitySupported && target == BuildTarget.iOS &&
                UnityEditorInternal.VR.VREditor.IsVRDeviceEnabledForBuildTarget(target, "cardboard");
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
            if (!PlayerSettings.virtualRealitySupported)
                return false;

            bool shouldInjectForTarget = false;
            var targetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(target);

            switch (targetGroup)
            {
                case BuildTargetGroup.iOS:
                    shouldInjectForTarget = VREditor.IsVRDeviceEnabledForBuildTarget(target, "cardboard");
                    break;

                default:
                    shouldInjectForTarget = false;
                    break;
            }

            return shouldInjectForTarget;
        }
    }
}
