// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.Collections;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Mono.BuildPipeline
{
    internal class DataBuildDirtyTracker
    {
        [Serializable]
        class BuildDataInputFile
        {
            public string path;
            public string contentHash;

            public BuildDataInputFile(NPath npath)
            {
                path = npath.ToString();
                if (npath.HasExtension("cs"))
                {
                    var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (monoScript != null)
                        contentHash = monoScript.GetPropertiesHashString();
                }
                else
                    contentHash = AssetDatabase.GetAssetDependencyHash(npath.ToString()).ToString();
            }
        }

        [Serializable]
        class BuildData
        {
            public BuildDataInputFile[] scenes;
            public BuildDataInputFile[] inputFiles;
            public BuildDataInputFile activeBuildProfile;
            public string activeBuildProfileHashWithoutPlatformSettings;
            public string[] enabledModules;
            public string[] resourcePaths;
            public BuildOptions buildOptions;
            public string unityVersion;
            public string sceneProcessorsHash;

            // These options could impact the cache data files.
            [NoAutoStaticsCleanup] // constant bitmask of BuildOptions enum flags (value type), never mutated; safe to persist
            public static BuildOptions BuildOptionsMask = BuildOptions.CompressWithLz4 |
                BuildOptions.ConnectToHost |
                BuildOptions.ConnectWithProfiler |
                BuildOptions.UncompressedAssetBundle |
                BuildOptions.CompressWithLz4HC;

            public string[] assemblyNames;

            // Session GUID of the full build that produced this data cache.
            // A scripts-only or incremental build that reuses this cache copies it onto its report.
            public GUID contentSourceBuildSessionGuid;
        }

        const string k_AssetDeletedReason = "deleted";
        const string k_AssetHashChangedReason = "hash";

        private BuildData buildData;
        private string[] scenes;
        private NPath activeBuildProfilePath;
        public BuildOptions buildOptions;
        private string[] assemblyNames;

        bool CheckAssetDirty(BuildDataInputFile file)
        {
            return CheckAssetDirty(file, GetAssetDirtyReason(file));
        }

        static bool CheckAssetDirty(BuildDataInputFile file, string dirtyReason)
        {
            if (dirtyReason == null)
                return false;

            Console.WriteLine($"Rebuilding Data files because {file.path.ToNPath()} is dirty ({dirtyReason})");
            return true;
        }

        // Returns null when the file hasn't changed
        static string GetAssetDirtyReason(BuildDataInputFile file)
        {
            NPath path = file.path;
            if (!path.Exists())
                return k_AssetDeletedReason;

            string contentHash = "";
            if (path.Extension == "cs")
            {
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path.ToString());
                if (monoScript != null)
                    contentHash = monoScript.GetPropertiesHashString();
            }
            else
                contentHash = AssetDatabase.GetAssetDependencyHash(file.path).ToString();

            return contentHash != file.contentHash ? k_AssetHashChangedReason : null;
        }

        bool DoCheckDirty()
        {
            if (Application.unityVersion != buildData.unityVersion)
            {
                Console.WriteLine($"Rebuilding Data files because they were built with a different Unity version {Application.unityVersion} vs {buildData.unityVersion}");
                return true;
            }

#pragma warning disable UAC2001, UAC2014 // Avoid Linq
            if (!scenes.SequenceEqual(buildData.scenes.Select(f => f.path)))
#pragma warning restore UAC2001, UAC2014
            {
                Console.WriteLine("Rebuilding Data files because the scene list is dirty");
                return true;
            }

            if ((buildOptions & BuildData.BuildOptionsMask) != buildData.buildOptions)
            {
                Console.WriteLine("Rebuilding Data files because the build options have changed");
                return true;
            }

            if (!assemblyNames.AsSpan().SequenceEqual(buildData.assemblyNames))
            {
                Console.WriteLine("Rebuilding Data files because the assembly list is dirty");
                return true;
            }

            if (Array.Exists(buildData.inputFiles, CheckAssetDirty))
                return true;

            var resourcePaths = ResourcesAPIInternal.GetAllPaths("");
            Array.Sort(resourcePaths);

            if (!resourcePaths.AsSpan().SequenceEqual(buildData.resourcePaths))
            {
                for (int i = 0; i < resourcePaths.Length || i < buildData.resourcePaths.Length; i++)
                {
                    string path;
                    if (i >= resourcePaths.Length)
                        path = buildData.resourcePaths[i];
                    else if (i >= buildData.resourcePaths.Length)
                        path = resourcePaths[i];
                    else if (buildData.resourcePaths[i] != resourcePaths[i])
                        path = resourcePaths[i];
                    else
                        continue;

                    Console.WriteLine($"Rebuilding Data files because {path} is dirty (Resource file added or removed)");
                    return true;
                }
            }

#pragma warning disable UAC2001 // Avoid Linq
            var enabledModules = ModuleMetadata.GetModuleNames()
#pragma warning restore UAC2001
                .Where(m => ModuleMetadata.GetModuleIncludeSettingForModule(m) != ModuleIncludeSetting.ForceExclude);
#pragma warning disable UAC2014 // Avoid Linq
            if (!enabledModules.SequenceEqual(buildData.enabledModules))
#pragma warning restore UAC2014
            {
                Console.WriteLine($"Rebuilding Data files because enabled modules have changed");
                return true;
            }

            if ((activeBuildProfilePath != buildData.activeBuildProfile.path) || (!string.IsNullOrEmpty(buildData.activeBuildProfile.path) && CheckActiveBuildProfileDirty()))
            {
                Console.WriteLine($"Rebuilding Data files because the active build profile have changed");
                return true;
            }

            if (ComputeSceneProcessorsHash() != buildData.sceneProcessorsHash)
            {
                Console.WriteLine("Rebuilding Data files because scene processing callbacks or their versions have changed");
                return true;
            }

            Console.WriteLine("Not rebuilding Data files -- no changes");
            return false;
        }

        static string ComputeSceneProcessorsHash()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            BuildPipelineInterfaces.OnSceneProcess_HashVersion(out var hash);
#pragma warning restore CS0618
            hash.Append(AssetPostprocessingInternal.GetSinglePassBuildSceneProcessorsHashString());
            return hash.ToString();
        }

        bool CheckActiveBuildProfileDirty()
        {
            var profile = buildData.activeBuildProfile;
            var dirtyReason = GetAssetDirtyReason(profile);

            // A changed file still reuses the data when only its platform settings changed
            if (dirtyReason == k_AssetHashChangedReason)
            {
                var hashWithoutPlatformSettings = ComputeBuildProfileHashWithoutPlatformSettings(profile.path);
                if (hashWithoutPlatformSettings != null && hashWithoutPlatformSettings == buildData.activeBuildProfileHashWithoutPlatformSettings)
                    return false;
            }

            return CheckAssetDirty(profile, dirtyReason);
        }

        // Returns null when the profile or a subasset has unsaved changes, because the hash must match the saved file
        static string ComputeBuildProfileHashWithoutPlatformSettings(string buildProfilePath)
        {
            if (string.IsNullOrEmpty(buildProfilePath))
                return null;

            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(buildProfilePath))
            {
                if (obj != null && EditorUtility.IsDirty(obj))
                    return null;
            }

            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(buildProfilePath);
            return profile != null ? profile.ComputeHashWithoutPlatformSettings() : null;
        }

        [RequiredByNativeCode]
        static public void WriteBuildData(string buildDataPath, BuildReport report, string[] scenes, string[] prefabs, string activeBuildProfilePath, string[] assemblyNames)
        {
            var inputScenes = new List<BuildDataInputFile>();
            foreach (var scene in scenes)
                inputScenes.Add(new BuildDataInputFile(scene));

            var inputFiles = new List<BuildDataInputFile>();
            foreach (var scene in scenes)
                inputFiles.Add(new BuildDataInputFile(scene));
            foreach (var prefab in prefabs)
                inputFiles.Add(new BuildDataInputFile(prefab));
#pragma warning disable UAC2001 // Avoid Linq
            foreach (var assetInfo in report.packedAssets.SelectMany(a => a.contents))
#pragma warning restore UAC2001
            {
                if (assetInfo.sourceAssetPath.ToNPath().FileExists() && !assetInfo.sourceAssetPath.StartsWith("."))
                    inputFiles.Add(new BuildDataInputFile(assetInfo.sourceAssetPath));
            }
            foreach (var projectSetting in new NPath("ProjectSettings").Files("*.asset", true))
                inputFiles.Add(new BuildDataInputFile(projectSetting));

            var resourcePaths = ResourcesAPIInternal.GetAllPaths("");
            Array.Sort(resourcePaths);

            var sortedAssemblyNames = (string[])assemblyNames.Clone();
            Array.Sort(sortedAssemblyNames);

            var buildData = new BuildData()
            {
                scenes = inputScenes.ToArray(),
                inputFiles = inputFiles.ToArray(),
                activeBuildProfile = new BuildDataInputFile(activeBuildProfilePath),
                activeBuildProfileHashWithoutPlatformSettings = ComputeBuildProfileHashWithoutPlatformSettings(activeBuildProfilePath),
                buildOptions = report.summary.options & BuildData.BuildOptionsMask,
                unityVersion = Application.unityVersion,
                sceneProcessorsHash = ComputeSceneProcessorsHash(),
                resourcePaths = resourcePaths,
#pragma warning disable UAC2001 // Avoid Linq
                enabledModules = ModuleMetadata.GetModuleNames()
                    .Where(m => ModuleMetadata.GetModuleIncludeSettingForModule(m) != ModuleIncludeSetting.ForceExclude)
                    .ToArray(),
#pragma warning restore UAC2001
                assemblyNames = sortedAssemblyNames,
                contentSourceBuildSessionGuid = report.summary.buildSessionGuid
            };
            buildDataPath.ToNPath().WriteAllText(JsonUtility.ToJson(buildData));
        }

        [RequiredByNativeCode]
        static public bool CheckDirty(string buildDataPath, BuildOptions buildOptions, string[] scenes, string activeBuildProfilePath, string[] assemblyNames)
        {
            NPath buildReportPath = buildDataPath;
            if (!buildReportPath.FileExists())
                return true;

            try
            {
                var sortedAssemblyNames = (string[])assemblyNames.Clone();
                Array.Sort(sortedAssemblyNames);

                DataBuildDirtyTracker tracker = new DataBuildDirtyTracker()
                {
                    buildData = JsonUtility.FromJson<BuildData>(buildReportPath.ReadAllText()),
                    scenes = scenes,
                    activeBuildProfilePath = activeBuildProfilePath,
                    buildOptions = buildOptions,
                    assemblyNames = sortedAssemblyNames
                };
                return tracker.DoCheckDirty();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Rebuilding Data files because the build data file is corrupt: {e}");
                return true;
            }
        }

        [RequiredByNativeCode]
        static public void ForceDirty(string buildDataPath)
        {
            NPath buildReportPath = buildDataPath;
            buildReportPath.DeleteIfExists();
        }

        // Called (from native BuildPlayer) when a build reuses the data cache instead of rebuilding it — both the
        // explicit scripts-only and the automatic (clean CheckDirty) reuse paths converge here. The reused BuildData
        // carries the session GUID of the full build that produced the cache; record it on this build's report so it
        // is written into BuildReportSummary and Build Analysis can borrow that exact source build's asset table.
        [RequiredByNativeCode]
        static public void SetContentSourceBuild(BuildReport report, string buildDataPath)
        {
            try
            {
                NPath path = buildDataPath;
                if (report == null || !path.FileExists())
                    return;

                var buildData = JsonUtility.FromJson<BuildData>(path.ReadAllText());
                if (buildData == null || buildData.contentSourceBuildSessionGuid.Empty())
                    return;

                report.SetContentSourceBuildSessionGUID(buildData.contentSourceBuildSessionGuid);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to record source content build metadata for this scripts-only build: {e.Message}");
            }
        }
    }
}
