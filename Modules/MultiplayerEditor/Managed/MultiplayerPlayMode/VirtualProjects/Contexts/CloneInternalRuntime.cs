// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class CloneInternalRuntime
    {
        public void HandleEvents(CloneContext vpContext)
        {
            vpContext.CloneSystems.AssetImportEvents.RequestImport += (didDomainReload, numAssetsChanged) =>
            {
                // AssetPostprocessingInternal::PostprocessAllAssets also does logic based off of containing no assets.
                // We need to pass this through to mirror the same logic
                // NOTE: This does mean this event fires more times than before!
                var containsAssets = numAssetsChanged > 0;
                if (containsAssets || didDomainReload)
                {
                    // If we have a domain reload from the main editor, call EditorUtility.RequestScriptReload() to request one for the clone, otherwise AssetDatabase.Refresh() as normal
                    if (didDomainReload && !EditorApplication.isPlaying)
                    {
                        MppmLog.Debug("Starting Script Reload");
                        MppmLog.Debug(EditorApplication.isPlaying);
                        EditorUtility.RequestScriptReload();
                        MppmLog.Debug("Script Reload Complete");
                    }
                    else
                    {
                        MppmLog.Debug("Starting Asset Import");
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                        MppmLog.Debug("Asset Import Complete");
                    }
                }
            };

            vpContext.CloneSystems.SceneEvents.SceneHierarchyChanged += newSceneHierarchy =>
            {
                var currentSceneHierarchy = SceneHierarchy.FromCurrentEditorSceneManager();

                // Active Scene Changed
                if (currentSceneHierarchy.ActiveScene != newSceneHierarchy.ActiveScene)
                {
                    HandleActiveSceneChanged(currentSceneHierarchy, newSceneHierarchy);
                }

                // Single scene is handled by the active scene changed at the same time
                if (newSceneHierarchy.LoadedScenes.Count > 1)
                {
                    HandleMultipleSceneLoaded(currentSceneHierarchy, newSceneHierarchy);
                }

                HandleSceneRemoval(currentSceneHierarchy, newSceneHierarchy);
            };
            vpContext.CloneSystems.SceneEvents.SceneSaved += sceneSaved =>
            {
                MppmLog.Debug($"Reloading scene {sceneSaved}");
                var sceneHierarchy = SceneHierarchy.FromCurrentEditorSceneManager();

                // Single Scene only, there is no scene manipulation required
                if (sceneHierarchy.LoadedScenes.Count == 1 && sceneHierarchy.UnloadedScenes.Count == 0)
                {
                    EditorSceneManager.OpenScene(sceneSaved, OpenSceneMode.Single);
                }
                else
                {
                    ReloadTheScene(sceneSaved, sceneHierarchy);
                }

                // Force an asset refresh after the scene is saved
                MppmLog.Debug("Starting Asset Import");
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                MppmLog.Debug("Asset Import Complete");
            };
        }

        static void ReloadTheScene(string sceneSaved, SceneHierarchy sceneHierarchy)
        {
            var scene = EditorSceneManager.GetSceneByPath(sceneSaved);
            EditorSceneManager.CloseScene(scene, false);

            var contains = ContainsString(sceneHierarchy.LoadedScenes, sceneSaved);
            var openSceneMode = contains
                ? OpenSceneMode.Additive
                : OpenSceneMode.AdditiveWithoutLoading;

            EditorSceneManager.OpenScene(sceneSaved, openSceneMode);
            if (sceneHierarchy.ActiveScene == sceneSaved)
            {
                EditorSceneManager.SetActiveScene(scene);
            }
        }

        static bool ContainsString(IEnumerable<string> sceneHierarchyLoadedScenes, string sceneSaved)
        {
            foreach (var loadedScene in sceneHierarchyLoadedScenes)
            {
                if (Equals(loadedScene, sceneSaved)) return true;
            }
            return false;
        }

        static void HandleMultipleSceneLoaded(SceneHierarchy currentSceneHierarchy, SceneHierarchy newSceneHierarchy)
        {
            var hierarchyScenes = newSceneHierarchy.LoadedScenes;
            var scenesToLoad = new List<string>();
            foreach (var scene in hierarchyScenes)
            {
                var contains = ContainsString(currentSceneHierarchy.LoadedScenes, scene);
                if (!contains && scene != newSceneHierarchy.ActiveScene)
                {
                    scenesToLoad.Add(scene);
                }
            }

            MppmLog.Debug($"Syncing scene hierarchy from main editor: {scenesToLoad.Count} scenes to load");
            foreach (var scene in scenesToLoad)
            {
                MppmLog.Debug($"Opening scene at path {scene}");
                EditorSceneManager.OpenScene(scene, GetOpenSceneMode(newSceneHierarchy));
            }
        }

        static void HandleActiveSceneChanged(SceneHierarchy currentSceneHierarchy, SceneHierarchy newSceneHierarchy)
        {
            MppmLog.Debug($"Active scene changed from {currentSceneHierarchy.ActiveScene} to {newSceneHierarchy.ActiveScene}");
            //If currently loaded, we can just make it the active without loading it
            if (ContainsString(currentSceneHierarchy.LoadedScenes, newSceneHierarchy.ActiveScene))
            {
                MppmLog.Debug("The new active scene is loaded, no need to reload");
                var scene = EditorSceneManager.GetSceneByPath(newSceneHierarchy.ActiveScene);
                EditorSceneManager.SetActiveScene(scene);
            }
            else
            {
                MppmLog.Debug("The new active scene is not loaded, opening now");
                if (!EditorApplication.isPlaying)
                {
                    var scene = EditorSceneManager.OpenScene(newSceneHierarchy.ActiveScene, GetOpenSceneMode(newSceneHierarchy));
                    EditorSceneManager.SetActiveScene(scene);
                }
            }
        }

        static void HandleSceneRemoval(SceneHierarchy currentSceneHierarchy, SceneHierarchy newSceneHierarchy)
        {
            var scenesToUnload = new List<string>();
            foreach (var scene in currentSceneHierarchy.LoadedScenes)
            {
                if (ContainsString(currentSceneHierarchy.UnloadedScenes, scene))
                {
                    scenesToUnload.Add(scene);
                }
            }

            var scenesToRemove = new List<string>();
            var concat = new List<string>(currentSceneHierarchy.LoadedScenes);
            concat.AddRange(currentSceneHierarchy.UnloadedScenes);
            foreach (var scene in concat)
            {
                if (!ContainsString(newSceneHierarchy.LoadedScenes, scene) && !ContainsString(newSceneHierarchy.UnloadedScenes, scene))
                {
                    scenesToRemove.Add(scene);
                }
            }

            if (!EditorApplication.isPlaying)
            {
                MppmLog.Debug($"Syncing scene hierarchy from main editor: {scenesToUnload.Count} scenes to unload");
                foreach (var scenePath in scenesToUnload)
                {
                    MppmLog.Debug($"Unloading scene {scenePath}");
                    var scene = EditorSceneManager.GetSceneByPath(scenePath);
                    EditorSceneManager.CloseScene(scene, false);
                }

                MppmLog.Debug($"Syncing scene hierarchy from main editor: {scenesToRemove.Count} scenes to close");
                foreach (var scenePath in scenesToRemove)
                {
                    MppmLog.Debug($"Closing scene {scenePath}");
                    var scene = EditorSceneManager.GetSceneByPath(scenePath);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static OpenSceneMode GetOpenSceneMode(SceneHierarchy sceneHierarchy)
        {
            return sceneHierarchy.LoadedScenes.Count > 1 || sceneHierarchy.UnloadedScenes.Count > 0
                ? OpenSceneMode.Additive
                : OpenSceneMode.Single;
        }
    }
}
