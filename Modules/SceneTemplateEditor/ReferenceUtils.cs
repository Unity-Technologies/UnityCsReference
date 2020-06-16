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
        public static void GetSceneDependencies(SceneAsset scene, List<Object> dependencies)
        {
            var path = AssetDatabase.GetAssetPath(scene);
            GetSceneDependencies(path, dependencies);
        }

        public static void GetSceneDependencies(Scene scene, List<Object> dependencies)
        {
            GetSceneDependencies(scene.path, dependencies);
        }

        public static void GetSceneDependencies(string scenePath, List<Object> dependencies)
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
                var typeInfo = SceneTemplateProjectSettings.Get().GetDependencyInfo(dependencyType);
                if (typeInfo.ignore)
                    continue;
                var obj = AssetDatabase.LoadAssetAtPath(dependencyPath, dependencyType);
                dependencies.Add(obj);
            }
        }
    }
}
