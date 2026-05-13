// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Profile;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    internal static class PlatformIconUtility
    {
        public static Texture2D GetPlatformIcon(BuildTarget platform)
        {
            var platformGuid = BuildTargetDiscovery.GetGUIDFromBuildTarget(platform);
            return BuildProfileModuleUtil.GetPlatformIcon(platformGuid);
        }
    }
}
