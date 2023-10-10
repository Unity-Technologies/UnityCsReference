// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.Utils;

namespace UnityEditor
{
    internal class LocalCacheServer
    {
        public const string PathKey = "LocalCacheServerPath";
        public const string CustomPathKey = "LocalCacheServerCustomPath";

        public static string GetCacheLocation()
        {
            var cachePath = EditorPrefs.GetString(PathKey);
            var enableCustomPath = EditorPrefs.GetBool(CustomPathKey);
            var result = cachePath;
            if (!enableCustomPath || string.IsNullOrEmpty(cachePath))
                result = Paths.Combine(OSUtil.GetDefaultCachePath(), "CacheServer");
            return result;
        }

        public static void Setup()
        {
            var mode = (AssetPipelinePreferences.CacheServerMode)EditorPrefs.GetInt("CacheServerMode");

            if (mode == AssetPipelinePreferences.CacheServerMode.Local)
            {
                EditorGUILayout.HelpBox("Local CacheServer is no longer supported", MessageType.Info, true);
            }
        }

        public static void Clear()
        {
            string cacheDirectoryPath = GetCacheLocation();
            if (Directory.Exists(cacheDirectoryPath))
                Directory.Delete(cacheDirectoryPath, true);
        }
    }
}
