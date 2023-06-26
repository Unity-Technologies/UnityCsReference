// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneTemplate
{
    internal static class ReferenceUtils
    {
        public static void GetSceneDependencies(SceneAsset scene, List<Object> dependencies, HashSet<string> editorOnlyDependencies = null)
        {
            var path = AssetDatabase.GetAssetPath(scene);
            GetSceneDependencies(path, dependencies, editorOnlyDependencies);
        }

        public static void GetSceneDependencies(Scene scene, List<Object> dependencies, HashSet<string> editorOnlyDependencies = null)
        {
            GetSceneDependencies(scene.path, dependencies, editorOnlyDependencies);
        }

        public static void GetSceneDependencies(string scenePath, List<Object> dependencies, HashSet<string> editorOnlyDependencies = null)
        {
            var dependencyPaths = AssetDatabase.GetDependencies(scenePath);

            // Convert the dependency paths to assets
            // Remove scene from dependencies
            foreach (var dependencyPath in dependencyPaths)
            {
                if (dependencyPath.Equals(scenePath))
                    continue;

                var dependencyType = AssetDatabase.GetMainAssetTypeAtPath(dependencyPath);
                if (dependencyType == null)
                    continue;
                if (dependencyType == typeof(MonoScript))
                {
                    var scriptDependencies = AssetDatabase.GetDependencies(dependencyPath);
                    foreach(var scriptDepPath in scriptDependencies)
                    {
                        if (scriptDepPath != dependencyPath)
                            editorOnlyDependencies.Add(scriptDepPath);
                    }
                }

                var typeInfo = SceneTemplateProjectSettings.Get().GetDependencyInfo(dependencyType);
                if (typeInfo.ignore)
                    continue;
                var obj = AssetDatabase.LoadAssetAtPath(dependencyPath, dependencyType);
                dependencies.Add(obj);
            }
        }

        public static void RemapAssetReferences(Dictionary<string, string> pathMap, Dictionary<int, int> idMap = null)
        {
            var objects = new List<Object>();
            foreach (var dstPath in pathMap.Values)
            {
                // In case of subscene, we cannot call LoadAllAssetsAtPath on it. It creates loading error.
                if (dstPath.EndsWith(".unity"))
                    continue;
                var assetsInDstPath = AssetDatabase.LoadAllAssetsAtPath(dstPath);
                foreach (var o in assetsInDstPath)
                {
                    if (o == null)
                        continue;
                    objects.Add(o);
                }
            }

            EditorUtility.RemapAssetReferences(objects.ToArray(), pathMap, idMap);
        }
    }
}
