// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

namespace UnityEditor
{
    internal class SceneVisibilityManager : ScriptableSingleton<SceneVisibilityManager>
    {
        internal static event Action hiddenContentChanged;
        internal static event Action currentStageIsolated;

        private readonly static List<GameObject> m_RootBuffer = new List<GameObject>();

        public static bool active
        {
            get { return SceneVisibilityState.active; }
            set { SceneVisibilityState.active = value; }
        }

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

        private static void EditorSceneManagerOnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single)
            {
                //force out of isolation when loading single
                SceneVisibilityState.mainStageIsolated = false;
            }
            if (mode == OpenSceneMode.Additive)
            {
                //make sure added scenes are isolated when opened if main stage is isolated
                if (!StageNavigationManager.instance.currentItem.isPrefabStage)
                {
                    Undo.ClearUndo(SceneVisibilityState.GetInstance());
                    if (SceneVisibilityState.mainStageIsolated)
                        SceneVisibilityState.SetSceneIsolation(scene, true);
                }
            }
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
            if (!oldItem.isMainStage)
            {
                SceneVisibilityState.prefabStageIsolated = false;
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
            RevertIsolationCurrentStage();
            if (mode == OpenSceneMode.Single)
                SceneVisibilityState.GeneratePersistentDataForAllLoadedScenes();
        }

        private static void EditorSceneManagerOnSceneClosing(Scene scene, bool removingScene)
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
            HideAllNoUndo();
            HiddenContentChanged();
        }

        private static void HideAllNoUndo()
        {
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
        }

        internal static void SetGameObjectHidden(GameObject gameObject, bool isHidden, bool includeChildren)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Set GameObject Hidden");
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

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Show Scene");
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

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Hide Scene");
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
                if (!IsGameObjectHidden(root) || root.transform.childCount > 0 && !AreAllChildrenHidden(root))
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
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Set GameObjects Hidden");
            SceneVisibilityState.SetGameObjectsHidden(gameObjects, isHidden, includeChildren);
            HiddenContentChanged();
        }

        internal static void IsolateGameObject(GameObject gameObject, bool includeChildren)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Isolate GameObject");

            IsolateCurrentStage();
            HideAllNoUndo();
            SceneVisibilityState.SetGameObjectHidden(gameObject, false, includeChildren);
            HiddenContentChanged();
        }

        internal static void IsolateGameObjects(GameObject[] gameObjects, bool includeChildren)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Isolate GameObjects");

            if (gameObjects.Length > 0)
            {
                IsolateCurrentStage();
                HideAllNoUndo();
                SceneVisibilityState.SetGameObjectsHidden(gameObjects, false, includeChildren);
                HiddenContentChanged();
            }
        }

        private static void ToggleSelectionVisibility()
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

        [Shortcut("Scene Visibility/Toggle Visibility And Children")]
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
                Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Visibility And Children");
                SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, shouldHide, true);
                HiddenContentChanged();
            }
        }

        internal static bool IsCurrentStageIsolated()
        {
            return StageNavigationManager.instance.currentItem.isPrefabStage ? SceneVisibilityState.prefabStageIsolated : SceneVisibilityState.mainStageIsolated;
        }

        private static void IsolateCurrentStage()
        {
            if (StageNavigationManager.instance.currentItem.isPrefabStage)
            {
                SceneVisibilityState.prefabStageIsolated = true;
                SceneVisibilityState.SetSceneIsolation(StageNavigationManager.instance.GetCurrentPrefabStage().scene, true);
            }
            else
            {
                SceneVisibilityState.mainStageIsolated = true;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    SceneVisibilityState.SetSceneIsolation(scene, true);
                }
            }

            currentStageIsolated?.Invoke();
        }

        internal static void RevertIsolation()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Revert Isolation");
            RevertIsolationCurrentStage();
            HiddenContentChanged();
        }

        private static void RevertIsolationCurrentStage()
        {
            if (StageNavigationManager.instance.currentItem.isPrefabStage)
            {
                SceneVisibilityState.prefabStageIsolated = false;
                SceneVisibilityState.SetSceneIsolation(StageNavigationManager.instance.GetCurrentPrefabStage().scene, false);
            }
            else
            {
                SceneVisibilityState.mainStageIsolated = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    SceneVisibilityState.SetSceneIsolation(scene, false);
                }
            }

            //If no more isolation, ensure that every scenes in DB has it's isolation cleared (including unloaded scenes)
            if (!SceneVisibilityState.prefabStageIsolated && !SceneVisibilityState.mainStageIsolated)
            {
                SceneVisibilityState.ClearIsolation();
            }
        }

        [Shortcut("Scene Visibility/Exit Isolation")]
        internal static void ExitIsolation()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Exit Isolation");

            if (IsCurrentStageIsolated()) //already isolated
            {
                RevertIsolationCurrentStage();
                HiddenContentChanged();
            }
        }

        [Shortcut("Scene Visibility/Isolate Selection And Children")]
        internal static void SelectionHierarchyIsolation()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Isolate Selection And Children");

            if (!IsCurrentStageIsolated())
            {
                IsolateCurrentStage();
                HideAllNoUndo();

                if (Selection.gameObjects.Length > 0)
                {
                    SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, false, true);
                }

                HiddenContentChanged();
            }
            else
            {
                if (Selection.gameObjects.Length > 0)
                {
                    HideAllNoUndo();
                    SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, false, true);
                    HiddenContentChanged();
                }
                else
                {
                    RevertIsolationCurrentStage();
                    HiddenContentChanged();
                }
            }
        }

        [Shortcut("Scene Visibility/Isolate Selection")]
        internal static void SelectionGameObjectIsolation()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Isolate Selection");

            if (!IsCurrentStageIsolated())
            {
                IsolateCurrentStage();
                HideAllNoUndo();

                if (Selection.gameObjects.Length > 0)
                {
                    SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, false, false);
                }

                HiddenContentChanged();
            }
            else
            {
                if (Selection.gameObjects.Length > 0)
                {
                    HideAllNoUndo();
                    SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, false, false);
                    HiddenContentChanged();
                }
                else
                {
                    RevertIsolationCurrentStage();
                    HiddenContentChanged();
                }
            }
        }
    }
}
