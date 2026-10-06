// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

static class MsBuildProjectCacheSettings
{
    const string k_CacheFolderEnvironmentVariable = "UNITY_MSBUILD_PROJECT_CACHE_DIR";
    const string k_CacheFolderKey = "MSBuildProjectCacheFolder";

    public static string cacheFolder
    {
        get { return EditorPrefs.GetString(k_CacheFolderKey, string.Empty); }
        set
        {
            EditorPrefs.SetString(k_CacheFolderKey, value ?? string.Empty);
            UnityEditorMSBuildPropsTargetsGeneration.UpdateProjectCacheLocation();
        }
    }

    public static string defaultCacheFolder => Path.Combine(OSUtil.GetDefaultCachePath(), "msbuild");

    public static string effectiveCacheFolder
    {
        get
        {
            var folder = cacheFolder;
            return string.IsNullOrEmpty(folder) ? defaultCacheFolder : folder;
        }
    }

    public static bool isOverriddenByEnvironment =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(k_CacheFolderEnvironmentVariable));
}
