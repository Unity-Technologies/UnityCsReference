// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class OpenSceneNode : ExecutionNode
    {
        [SerializeReference] public NodeInput<SceneAsset> Scene;

        public OpenSceneNode()
        {
            Scene = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // If the scene is already active (is EditMode-focused scene), there's nothing to do.
            var sceneAsset = GetInput(Scene);
            if (sceneAsset == null || IsCurrentOnlyScene(sceneAsset))
            {
                return Task.CompletedTask;
            }

            // Set the initial scene as the active one.
            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);

            // Scenario validation rejects an initial scene from a read-only package configuration before Deploy
            if (PackageUtils.IsAssetInReadOnlyPackage(scenePath, out var packageName))
            {
                throw new InvalidOperationException(
                    $"Cannot open scene '{scenePath}' because it is in the read-only package '{packageName}'.");
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return Task.CompletedTask;
        }

        bool IsCurrentOnlyScene(SceneAsset scene)
        {
            if (SceneManager.sceneCount != 1)
                return false;

            var currentScene = SceneManager.GetActiveScene();
            return AssetDatabase.GetAssetPath(scene) == currentScene.path;
        }
    }
}
