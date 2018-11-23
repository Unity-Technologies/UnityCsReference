// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

namespace UnityEditor
{
    internal static class SceneVisibilityManager
    {
        internal static event Action hiddenContentChanged;

        private readonly static List<GameObject> m_RootBuffer = new List<GameObject>();

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
            StageNavigationManager.instance.stageChanged += StageNavigationManagerOnStageChanging;

            PrefabStage stage = StageNavigationManager.instance.GetCurrentPrefabStage();
            SceneVisibilityState.SetPrefabStageScene(stage == null ? default(Scene) : stage.scene);
        }

        private static void StageNavigationManagerOnStageChanging(StageNavigationItem oldItem, StageNavigationItem newItem)
        {
            if (!newItem.isMainStage && newItem.prefabStage != null)
            {
                SceneVisibilityState.SetPrefabStageScene(newItem.prefabStage.scene);
            }
            else
            {
                SceneVisibilityState.SetPrefabStageScene(default(Scene));
            }
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
                    HideScene(SceneManager.GetSceneAt(i), false);
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
                    ShowScene(SceneManager.GetSceneAt(i), false);
                }
            }

            HiddenContentChanged();
        }

        private static void ShowScene(Scene scene, bool sendContentChangedEvent)
        {
            if (!scene.IsValid())
                return;

            SceneVisibilityState.ShowScene(scene);

            if (sendContentChangedEvent)
            {
                HiddenContentChanged();
            }
        }

        internal static void ShowScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "ShowScene");
            ShowScene(scene, true);
        }

        private static void HideScene(Scene scene, bool sendContentChangedEvent)
        {
            if (!scene.IsValid())
                return;

            SceneVisibilityState.ShowScene(scene);
            SceneVisibilityState.SetGameObjectsHidden(scene.GetRootGameObjects(), true, true);

            if (sendContentChangedEvent)
            {
                HiddenContentChanged();
            }
        }

        internal static void HideScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "HideScene");
            HideScene(scene, true);
        }

        internal static bool IsGameObjectHidden(GameObject gameObject)
        {
            return SceneVisibilityState.IsGameObjectHidden(gameObject);
        }

        internal static bool IsEntireSceneHidden(Scene scene)
        {
            if (scene.rootCount == 0)
                return false;

            scene.GetRootGameObjects(m_RootBuffer);
            foreach (GameObject root in m_RootBuffer)
            {
                if (!IsGameObjectHidden(root) || !AreAllChildrenHidden(root))
                    return false;
            }

            return true;
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

        //SHORTCUTS
        [Shortcut("Scene Visibility/Toggle Visibility")]
        internal static void ToggleSelectionGameObjectVisibility()
        {
            if (Selection.gameObjects.Length > 0)
            {
                bool shouldHide = true;
                foreach (var gameObject in Selection.gameObjects)
                {
                    if (!IsGameObjectHidden(gameObject))
                    {
                        break;
                    }

                    shouldHide = false;
                }
                Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Visibility");
                SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, shouldHide, false);
                HiddenContentChanged();
            }
        }

        [Shortcut("Scene Visibility/Toggle Visibility and children")]
        internal static void ToggleSelectionHierarchyVisibility()
        {
            if (Selection.gameObjects.Length > 0)
            {
                bool shouldHide = true;
                foreach (var gameObject in Selection.gameObjects)
                {
                    if (!IsGameObjectHidden(gameObject))
                    {
                        break;
                    }

                    shouldHide = false;
                }
                Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Visibility and children");
                SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, shouldHide, true);
                HiddenContentChanged();
            }
        }
    }
}
