// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build;

namespace UnityEditor
{
    internal static class EditorUserBuildSettingsUtils
    {
        public static BuildTarget CalculateSelectedBuildTarget()
        {
            NamedBuildTarget namedBuildTarget = CalculateSelectedNamedBuildTarget();
            return CalculateSelectedBuildTarget(namedBuildTarget);
        }

        internal static BuildTarget CalculateSelectedBuildTarget(NamedBuildTarget namedBuildTarget)
        {
            if (namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone)
            {
                BuildTarget target = EditorUserBuildSettings.selectedStandaloneTarget;
                if (target == BuildTarget.NoTarget)
                    target = EditorUserBuildSettings.activeBuildTarget;
                return DesktopStandaloneBuildWindowExtension.GetBestStandaloneTarget(target);
            }
            else
            {
                if (BuildPlatforms.instance == null)
                    throw new System.Exception("Build platforms are not initialized.");
                BuildPlatform platform = BuildPlatforms.instance.BuildPlatformFromNamedBuildTarget(namedBuildTarget);
                if (platform == null)
                    throw new System.Exception("Could not find build platform for target group " + namedBuildTarget.TargetName);
                return platform.defaultTarget;
            }
        }

        public static NamedBuildTarget CalculateSelectedNamedBuildTarget()
        {
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (targetGroup == BuildTargetGroup.Unknown)
                targetGroup = EditorUserBuildSettings.activeBuildTargetGroup;

            // TODO: Eventually NamedBuildTarget will replace completetly BuildTargetGroup, and we won't need this custom check
            if (targetGroup == BuildTargetGroup.Standalone)
            {
                if (EditorUserBuildSettings.selectedStandaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
                    return NamedBuildTarget.Server;

                return NamedBuildTarget.Standalone;
            }

            return NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        }

        public static NamedBuildTarget CalculateActiveNamedBuildTarget()
        {
            BuildTargetGroup targetGroup = EditorUserBuildSettings.activeBuildTargetGroup;

            // TODO: Eventually NamedBuildTarget will replace completetly BuildTargetGroup, and we won't need this custom check
            if (targetGroup == BuildTargetGroup.Standalone)
            {
                if (EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
                    return NamedBuildTarget.Server;

                return NamedBuildTarget.Standalone;
            }

            return NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        }

        public static bool IsSelected(this BuildPlatform buildPlatform)
        {
            return buildPlatform.namedBuildTarget == CalculateSelectedNamedBuildTarget();
        }

        public static bool IsActive(this BuildPlatform buildPlatform)
        {
            return buildPlatform.namedBuildTarget == CalculateActiveNamedBuildTarget();
        }

        public static void Select(this BuildPlatform buildPlatform)
        {
            if (buildPlatform is BuildPlatformWithSubtarget)
            {
                var target = CalculateSelectedBuildTarget(buildPlatform.namedBuildTarget);
                EditorUserBuildSettings.SetSelectedSubtargetFor(target, ((BuildPlatformWithSubtarget)buildPlatform).subtarget);
            }

            EditorUserBuildSettings.selectedBuildTargetGroup = buildPlatform.namedBuildTarget.ToBuildTargetGroup();
        }

        public static bool SetActive(this BuildPlatform buildPlatform)
        {
            var target = CalculateSelectedBuildTarget(buildPlatform.namedBuildTarget);
            return buildPlatform.SetActive(target);
        }

        public static bool SetActive(this BuildPlatform buildPlatform, BuildTarget target)
        {
            if (BuildPipeline.GetBuildTargetGroup(target) != buildPlatform.namedBuildTarget.ToBuildTargetGroup())
            {
                UnityEngine.Debug.LogError($"Build target {target} must be part of the {buildPlatform.namedBuildTarget.TargetName} group");
                return false;
            }

            if (buildPlatform is BuildPlatformWithSubtarget)
                EditorUserBuildSettings.SetActiveSubtargetFor(target, ((BuildPlatformWithSubtarget)buildPlatform).subtarget);

            return EditorUserBuildSettings.SwitchActiveBuildTarget(buildPlatform.namedBuildTarget.ToBuildTargetGroup(), target);
        }
    }
}
