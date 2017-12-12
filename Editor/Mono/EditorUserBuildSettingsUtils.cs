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
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            switch (targetGroup)
            {
                case BuildTargetGroup.Standalone:
                    return DesktopStandaloneBuildWindowExtension.GetBestStandaloneTarget(EditorUserBuildSettings.selectedStandaloneTarget);
                case BuildTargetGroup.Facebook:
                    return EditorUserBuildSettings.selectedFacebookTarget;
                default:
                    if (BuildPlatforms.instance == null)
                        throw new System.Exception("Build platforms are not initialized.");
                    BuildPlatform platform = BuildPlatforms.instance.BuildPlatformFromTargetGroup(targetGroup);
                    if (platform == null)
                        throw new System.Exception("Could not find build platform for target group " + targetGroup);
                    return platform.defaultTarget;
            }
        }
    }
}
