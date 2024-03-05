// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Scripting;

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
                {
                    contentHash = AssetDatabase.GetAssetDependencyHash(npath.ToString()).ToString();
                }
            }
        }

        [Serializable]
        class BuildData
        {
            public BuildDataInputFile[] scenes;
            public BuildDataInputFile[] inputFiles;
            public string[] enabledModules;
            public string[] resourcePaths;
            public BuildOptions buildOptions;
            public string unityVersion;

            // These options could impact the cache data files.
            public static BuildOptions BuildOptionsMask = BuildOptions.CompressWithLz4 |
                BuildOptions.ConnectToHost |
                BuildOptions.ConnectWithProfiler |
                BuildOptions.UncompressedAssetBundle |
                BuildOptions.ShaderLivelinkSupport |
                BuildOptions.CompressWithLz4HC;
        }

        private BuildData buildData;
        private string[] scenes;
        public BuildOptions buildOptions;

        bool CheckAssetDirty(BuildDataInputFile file)
        {
            NPath path = file.path;
            if (!path.Exists())
            {
                Console.WriteLine($"Rebuilding Data files because {path} is dirty (deleted)");
                return true;
            }

            string hash = "";
            if (path.Extension == "cs")
            {
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path.ToString());
                if (monoScript != null)
                    hash = monoScript.GetPropertiesHashString();
            }
            else
            {
                hash = AssetDatabase.GetAssetDependencyHash(path.ToString()).ToString();
            }

            if (hash != file.contentHash)
            {
                Console.WriteLine($"Rebuilding Data files because {path} is dirty (hash)");
                return true;
            }

            return false;
        }

        bool DoCheckDirty()
        {
            if (Application.unityVersion != buildData.unityVersion)
            {
                Console.WriteLine($"Rebuilding Data files because they were built with a different Unity version {Application.unityVersion} vs {buildData.unityVersion}");
                return true;
            }

            if (!scenes.SequenceEqual(buildData.scenes.Select(f => f.path)))
            {
                Console.WriteLine("Rebuilding Data files because the scene list is dirty");
                return true;
            }

            if (buildOptions != buildData.buildOptions)
            {
                Console.WriteLine("Rebuilding Data files because the build options have changed");
                return true;
            }

            if (buildData.inputFiles.Any(CheckAssetDirty))
                return true;

            var resourcePaths = ResourcesAPIInternal.GetAllPaths("").OrderBy(p => p).ToArray();
            if (!resourcePaths.SequenceEqual(buildData.resourcePaths))
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

            var enabledModules = ModuleMetadata.GetModuleNames()
                .Where(m => ModuleMetadata.GetModuleIncludeSettingForModule(m) != ModuleIncludeSetting.ForceExclude);
            if (!enabledModules.SequenceEqual(buildData.enabledModules))
            {
                Console.WriteLine($"Rebuilding Data files because enabled modules have changed");
                return true;
            }

            Console.WriteLine("Not rebuilding Data files -- no changes");
            return false;
        }

        [RequiredByNativeCode]
        static public void WriteBuildData(string buildDataPath, BuildReport report, string[] scenes, string[] prefabs)
        {
            var inputScenes = new List<BuildDataInputFile>();
            foreach (var scene in scenes)
                inputScenes.Add(new BuildDataInputFile(scene));

            var inputFiles = new List<BuildDataInputFile>();
            foreach (var scene in scenes)
                inputFiles.Add(new BuildDataInputFile(scene));
            foreach (var prefab in prefabs)
                inputFiles.Add(new BuildDataInputFile(prefab));
            foreach (var assetInfo in report.packedAssets.SelectMany(a => a.contents))
            {
                if (assetInfo.sourceAssetPath.ToNPath().FileExists() && !assetInfo.sourceAssetPath.StartsWith("."))
                    inputFiles.Add(new BuildDataInputFile(assetInfo.sourceAssetPath));
            }
            foreach (var projectSetting in new NPath("ProjectSettings").Files("*.asset"))
                inputFiles.Add(new BuildDataInputFile(projectSetting));

            var buildData = new BuildData()
            {
                scenes = inputScenes.ToArray(),
                inputFiles = inputFiles.ToArray(),
                buildOptions = report.summary.options & BuildData.BuildOptionsMask,
                unityVersion = Application.unityVersion,
                resourcePaths = ResourcesAPIInternal.GetAllPaths("").OrderBy(p => p).ToArray(),
                enabledModules = ModuleMetadata.GetModuleNames()
                    .Where(m => ModuleMetadata.GetModuleIncludeSettingForModule(m) != ModuleIncludeSetting.ForceExclude)
                    .ToArray()
            };
            buildDataPath.ToNPath().WriteAllText(JsonUtility.ToJson(buildData));
        }

        [RequiredByNativeCode]
        static public bool CheckDirty(string buildDataPath, BuildOptions buildOptions, string[] scenes)
        {
            NPath buildReportPath = buildDataPath;
            if (!buildReportPath.FileExists())
                return true;

            try
            {
                DataBuildDirtyTracker tracker = new DataBuildDirtyTracker()
                {
                    buildData = JsonUtility.FromJson<BuildData>(buildReportPath.ReadAllText()),
                    scenes = scenes,
                    buildOptions = buildOptions & BuildData.BuildOptionsMask
                };
                return tracker.DoCheckDirty();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Rebuilding Data files because the build data file is corrupt: {e}");
                return true;
            }
        }
    }
}
