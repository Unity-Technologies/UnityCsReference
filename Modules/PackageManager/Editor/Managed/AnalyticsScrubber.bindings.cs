// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.PackageManager
{
    [NativeHeader("Modules/PackageManager/Editor/PackageManagerUtilities.h")]
    internal static class AnalyticsScrubber
    {
        public static string ScrubPackageId(string packageId)
        {
            return string.IsNullOrEmpty(packageId) ? packageId : ScrubPackageIdInternal(packageId);
        }

        public static string ScrubUserPaths(string text)
        {
            return string.IsNullOrEmpty(text) ? text : ScrubUserPathsInternal(text);
        }

        [FreeFunction("PackageManager::ScrubPackageId")]
        private static extern string ScrubPackageIdInternal(string packageId);

        [FreeFunction("PackageManager::ScrubUserPaths")]
        private static extern string ScrubUserPathsInternal(string text);
    }
}
