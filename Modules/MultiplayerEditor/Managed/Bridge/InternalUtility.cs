// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;

namespace Unity.DedicatedServer.Editor.Internal
{
    internal static class InternalUtility
    {
        // This is the only internal API we should access from the BuildProfile module
        public static void GetBuildProfileInternalData(BuildProfile buildProfile, out StandaloneBuildSubtarget buildSubtarget, out BuildTarget buildTarget, out string platformId)
        {
            buildSubtarget = buildProfile.subtarget;
            buildTarget = buildProfile.buildTarget;
            platformId = buildProfile.platformId;
        }

        public static bool IsStandalonePlatform(BuildTarget buildTarget)
            => BuildPipeline.GetBuildTargetGroup(buildTarget) == BuildTargetGroup.Standalone;

        public static bool IsServerProfile(BuildProfile buildProfile)
        {
            GetBuildProfileInternalData(buildProfile, out var buildSubtarget, out var buildTarget, out _);
            return buildSubtarget == StandaloneBuildSubtarget.Server && IsStandalonePlatform(buildTarget);
        }

        public static bool IsClassicProfile(BuildProfile buildProfile)
        {
            return (buildProfile.hideFlags & UnityEngine.HideFlags.DontSave) != 0;
        }

        public static NamedBuildTarget GetNamedBuildTarget(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            if (subtarget == StandaloneBuildSubtarget.Server && IsStandalonePlatform(buildTarget))
                return NamedBuildTarget.Server;

            return NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(buildTarget));
        }

        public static string GetUniqueKeyForClassicTarget(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            var platformGuid = (subtarget == StandaloneBuildSubtarget.Server && IsStandalonePlatform(buildTarget))
                ? BuildTargetDiscovery.GetGUIDFromBuildTarget(NamedBuildTarget.Server, buildTarget)
                : BuildTargetDiscovery.GetGUIDFromBuildTarget(buildTarget);

            return platformGuid.ToString();
        }
    }
}
