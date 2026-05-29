// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Profile;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Resolves icons used throughout the Build Analysis UI.
    /// </summary>
    internal static class IconUtility
    {
        /// <summary>
        /// Returns the platform icon for a given <see cref="BuildTarget"/>.
        /// </summary>
        public static Texture2D GetPlatformIcon(BuildTarget platform)
        {
            var platformGuid = BuildTargetDiscovery.GetGUIDFromBuildTarget(platform);
            return BuildProfileModuleUtil.GetPlatformIcon(platformGuid);
        }

        /// <summary>
        /// Returns the icon for an asset path, mirroring the Project Browser's strategy
        /// (see Editor/Mono/ProjectBrowser/CachedFilteredHierachy.cs around line 48):
        /// prefer <see cref="AssetDatabase.GetCachedIcon"/> (handles custom icons, prefab
        /// variants, folder open/closed, and extension fallback internally), then fall
        /// back to <see cref="InternalEditorUtility.FindIconForFile"/> for paths that
        /// aren't in the current project — common for build-analysis since paths are
        /// captured at build time.
        /// </summary>
        public static Texture GetAssetIcon(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var icon = AssetDatabase.GetCachedIcon(path);
            if (icon != null)
                return icon;

            return InternalEditorUtility.FindIconForFile(path);
        }
    }
}
