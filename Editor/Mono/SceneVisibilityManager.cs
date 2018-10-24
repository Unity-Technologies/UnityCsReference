// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

namespace UnityEditor
{
    internal static class SceneVisibilityManager
    {
        internal static event Action hiddenContentChanged;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            EditorSceneManager.newSceneCreated += EditorSceneManagerOnNewSceneCreated;
            EditorSceneManager.sceneSaving += EditorSceneManagerOnSceneSaving;
            EditorSceneManager.sceneSaved += EditorSceneManagerOnSceneSaved;
            EditorSceneManager.sceneOpening += EditorSceneManagerOnSceneOpening;
            EditorSceneManager.sceneClosing += EditorSceneManagerOnSceneClosing;
            EditorApplication.playModeStateChanged += EditorApplicationPlayModeStateChanged;
        }

        private static void EditorApplicationPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SceneVisibilityState.GeneratePersistentDataForAllLoadedScenes();
            }
            HiddenContentChanged();
        }

        private static void EditorSceneManagerOnSceneSaved(Scene scene)
        {
            SceneVisibilityState.OnSceneSaved(scene);
        }

        private static void EditorSceneManagerOnSceneSaving(Scene scene, string path)
        {
            SceneVisibilityState.OnSceneSaving(scene, path);
        }

        private static void EditorSceneManagerOnSceneOpening(string path, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single)
                SceneVisibilityState.GeneratePersistentDataForAllLoadedScenes();
        }

        private static void EditorSceneManagerOnSceneClosing(Scene scene , bool removingScene)
        {
            SceneVisibilityState.GeneratePersistentDataForLoadedScene(scene);
        }

        private static void EditorSceneManagerOnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            if (mode == NewSceneMode.Single)
            {
                SceneVisibilityState.GeneratePersistentDataForAllLoadedScenes();
            }
            //need to clear scene on new scene since all new scenes use the same GUID
            SceneVisibilityState.ClearScene(scene);
        }

        private static void UndoRedoPerformed()
        {
            HiddenContentChanged();
        }

        internal static void HideAll()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Hide All");
            if (StageNavigationManager.instance.currentItem.isPrefabStage)
            {
                var scene = StageNavigationManager.instance.GetCurrentPrefabStage().scene;
                SceneVisibilityState.ShowScene(StageNavigationManager.instance.GetCurrentPrefabStage().scene);
                SceneVisibilityState.SetGameObjectsHidden(scene.GetRootGameObjects(), true, true);
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    SceneVisibilityState.ShowScene(scene);
                    SceneVisibilityState.SetGameObjectsHidden(scene.GetRootGameObjects(), true, true);
                }
            }
            HiddenContentChanged();
        }

        internal static void SetGameObjectHidden(GameObject gameObject, bool isHidden, bool includeChildren)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "SetGameObjectHidden");
            SceneVisibilityState.SetGameObjectHidden(gameObject, isHidden, includeChildren);
            HiddenContentChanged();
        }

        internal static void ShowAll()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Show All");
            if (StageNavigationManager.instance.currentItem.isPrefabStage)
            {
                SceneVisibilityState.ShowScene(StageNavigationManager.instance.GetCurrentPrefabStage().scene);
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    SceneVisibilityState.ShowScene(scene);
                }
            }

            HiddenContentChanged();
        }

        internal static bool IsGameObjectHidden(GameObject gameObject)
        {
            return SceneVisibilityState.IsGameObjectHidden(gameObject);
        }

        internal static bool IsHierarchyHidden(GameObject gameObject)
        {
            return SceneVisibilityState.IsHierarchyHidden(gameObject);
        }

        internal static bool HasHiddenGameObjects(Scene scene)
        {
            return SceneVisibilityState.HasHiddenGameObjects(scene);
        }

        internal static void SetGameObjectsHidden(GameObject[] gameObjects, bool isHidden, bool includeChildren)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "SetGameObjectsHidden");
            SceneVisibilityState.SetGameObjectsHidden(gameObjects, isHidden, includeChildren);
            HiddenContentChanged();
        }

        internal static void ToggleSelectionVisibility()
        {
            bool allHidden = true;
            foreach (var gameObject in Selection.gameObjects)
            {
                if (!SceneVisibilityState.IsGameObjectHidden(gameObject))
                {
                    allHidden = false;
                    break;
                }
            }

            if (allHidden)
            {
                SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, false, false);
            }
            else
            {
                SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, true, false);
            }

            HiddenContentChanged();
        }

        private static void HiddenContentChanged()
        {
            hiddenContentChanged?.Invoke();
        }

        internal static void ToggleGameObjectVisibility(GameObject gameObject)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle GameObject Visibility");
            SceneVisibilityState.SetGameObjectHidden(gameObject, !SceneVisibilityState.IsGameObjectHidden(gameObject), false);
            HiddenContentChanged();
        }

        internal static void ToggleHierarchyVisibility(GameObject gameObject)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Hierarchy Visibility");
            SceneVisibilityState.SetGameObjectHidden(gameObject, !SceneVisibilityState.IsGameObjectHidden(gameObject), true);
            HiddenContentChanged();
        }

        internal static bool AreAllChildrenHidden(GameObject gameObject)
        {
            return SceneVisibilityState.AreAllChildrenHidden(gameObject);
        }

        internal static bool AreAllChildrenVisible(GameObject gameObject)
        {
            return SceneVisibilityState.AreAllChildrenVisible(gameObject);
        }
    }
}
