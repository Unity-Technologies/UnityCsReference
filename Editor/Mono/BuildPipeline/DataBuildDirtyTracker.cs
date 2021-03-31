// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
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
        }

        private BuildData buildData;
        private string[] scenes;
        bool CheckAssetDirty(BuildDataInputFile file)
        {
            NPath path = file.path;
            if (!path.Exists())
            {
                Console.WriteLine($"Rebuiding Data files because {path} is dirty (deleted)");
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
                Console.WriteLine($"Rebuiding Data files because {path} is dirty (hash)");
                return true;
            }

            return false;
        }

        bool DoCheckDirty()
        {
            if (!scenes.SequenceEqual(buildData.scenes.Select(f => f.path)))
            {
                Console.WriteLine($"Rebuiding Data files because the scene list is dirty");
                return true;
            }

            if (buildData.inputFiles.Any(CheckAssetDirty))
                return true;

            Console.WriteLine($"Not rebuiding Data files -- no changes");
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
            };
            buildDataPath.ToNPath().WriteAllText(JsonUtility.ToJson(buildData));
        }

        [RequiredByNativeCode]
        static public bool CheckDirty(string buildDataPath, string[] scenes)
        {
            NPath buildReportPath = buildDataPath;
            if (!buildReportPath.FileExists())
                return true;

            DataBuildDirtyTracker tracker = new DataBuildDirtyTracker()
            {
                buildData = JsonUtility.FromJson<BuildData>(buildReportPath.ReadAllText()),
                scenes = scenes
            };
            return tracker.DoCheckDirty();
        }
    }
}
