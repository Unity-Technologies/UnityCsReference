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
    public class SceneVisibilityManager : ScriptableSingleton<SceneVisibilityManager>
    {
        internal class ShortcutContext : IShortcutToolContext
        {
            public bool active
            {
                get
                {
                    var focusedWindow = EditorWindow.focusedWindow;
                    if (focusedWindow != null)
                    {
                        return (focusedWindow.GetType() == typeof(SceneView) ||
                            focusedWindow.GetType() == typeof(SceneHierarchyWindow));
                    }

                    return false;
                }
            }
        }

        private static ShortcutContext s_ShortcutContext;

        public static event Action visibilityChanged;

        public static event Action pickingChanged;

        internal static event Action currentStageIsolated;

        private readonly static List<GameObject> m_RootBuffer = new List<GameObject>();

        internal bool enableSceneVisibility
        {
            get { return SceneVisibilityState.visibilityActive; }
            set { SceneVisibilityState.visibilityActive = value; }
        }

        internal bool enableScenePicking
        {
            get { return SceneVisibilityState.pickingActive; }
            set { SceneVisibilityState.pickingActive = value; }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            EditorSceneManager.newSceneCreated += EditorSceneManagerOnNewSceneCreated;
            EditorSceneManager.sceneSaving += EditorSceneManagerOnSceneSaving;
            EditorSceneManager.sceneSaved += EditorSceneManagerOnSceneSaved;
            EditorSceneManager.sceneOpening += EditorSceneManagerOnSceneOpening;
            EditorSceneManager.sceneOpened += EditorSceneManagerOnSceneOpened;
            EditorSceneManager.sceneClosing += EditorSceneManagerOnSceneClosing;
            EditorApplication.playModeStateChanged += EditorApplicationPlayModeStateChanged;
            StageNavigationManager.instance.stageChanged += StageNavigationManagerOnStageChanging;
            SceneVisibilityState.internalStructureChanged += InternalStructureChanged;
            PrefabStage stage = StageNavigationManager.instance.GetCurrentPrefabStage();
            SceneVisibilityState.SetPrefabStageScene(stage?.scene ?? default(Scene));

            s_ShortcutContext = new ShortcutContext();
            EditorApplication.delayCall += () => ShortcutIntegration.instance.contextManager.RegisterToolContext(s_ShortcutContext);
        }

        private static void InternalStructureChanged()
        {
            instance.VisibilityChanged();
            instance.PickableContentChanged();
        }

        private static void EditorSceneManagerOnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single)
            {
                SceneVisibilityState.isolation = false;
            }
            if (mode == OpenSceneMode.Additive)
            {
                //make sure added scenes are isolated when opened if main stage is isolated
                if (!StageNavigationManager.instance.currentItem.isPrefabStage)
                {
                    Undo.ClearUndo(SceneVisibilityState.GetInstance());
                }
            }
        }

        private static void StageNavigationManagerOnStageChanging(StageNavigationItem oldItem, StageNavigationItem newItem)
        {
            RevertIsolationCurrentStage();
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
            SceneVisibilityState.ForceDataUpdate();
        }

        public void HideAll()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Hide All");
            HideAllNoUndo();
        }

        private void HideAllNoUndo()
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
                    HideNoUndo(SceneManager.GetSceneAt(i));
                }
            }
        }

        public void DisableAllPicking()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Disable All Picking");
            DisableAllPickingNoUndo();
        }

        private void DisableAllPickingNoUndo()
        {
            if (StageNavigationManager.instance.currentItem.isPrefabStage)
            {
                var scene = StageNavigationManager.instance.GetCurrentPrefabStage().scene;
                SceneVisibilityState.EnablePicking(StageNavigationManager.instance.GetCurrentPrefabStage().scene);
                SceneVisibilityState.DisablePicking(scene);
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    DisablePicking(SceneManager.GetSceneAt(i));
                }
            }
        }

        public void Show(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Show GameObject");
            SceneVisibilityState.SetGameObjectHidden(gameObject, false, includeDescendants);
        }

        public void Hide(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Hide GameObject");
            SceneVisibilityState.SetGameObjectHidden(gameObject, true, includeDescendants);
        }

        public void DisablePicking(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Disable Picking GameObject");
            SceneVisibilityState.SetGameObjectPickingDisabled(gameObject, true, includeDescendants);
        }

        public void EnablePicking(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Enable Picking GameObject");
            SceneVisibilityState.SetGameObjectPickingDisabled(gameObject, false, includeDescendants);
        }

        [Shortcut("Scene Visibility/Show All")]
        internal static void ShowAllShortcut()
        {
            instance.ShowAll();
        }

        public void ShowAll()
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
                    Show(SceneManager.GetSceneAt(i), false);
                }
            }
        }

        public void EnableAllPicking()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Enable All Picking");
            if (StageNavigationManager.instance.currentItem.isPrefabStage)
            {
                SceneVisibilityState.EnablePicking(StageNavigationManager.instance.GetCurrentPrefabStage().scene);
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    EnablePickingNoUndo(SceneManager.GetSceneAt(i));
                }
            }
        }

        private void Show(Scene scene, bool sendContentChangedEvent)
        {
            if (!scene.IsValid())
                return;

            if (!scene.isLoaded)
                return;

            SceneVisibilityState.ShowScene(scene);

            if (sendContentChangedEvent)
            {
                VisibilityChanged();
            }
        }

        private void EnablePickingNoUndo(Scene scene)
        {
            if (!scene.IsValid())
                return;

            if (!scene.isLoaded)
                return;

            SceneVisibilityState.EnablePicking(scene);
        }

        public void Show(Scene scene)
        {
            if (!scene.IsValid())
                return;

            if (!scene.isLoaded)
                return;

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Show Scene");
            Show(scene, true);
        }

        public void EnablePicking(Scene scene)
        {
            if (!scene.IsValid())
                return;

            if (!scene.isLoaded)
                return;

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Enable Picking Scene");
            EnablePickingNoUndo(scene);
        }

        private void HideNoUndo(Scene scene)
        {
            if (!scene.IsValid())
                return;

            if (!scene.isLoaded)
                return;

            SceneVisibilityState.ShowScene(scene);
            SceneVisibilityState.SetGameObjectsHidden(scene.GetRootGameObjects(), true, true);
        }

        internal void DisablePickingNoUndo(Scene scene)
        {
            if (!scene.IsValid())
                return;

            if (!scene.isLoaded)
                return;

            SceneVisibilityState.EnablePicking(scene);
            SceneVisibilityState.SetGameObjectsPickingDisabled(scene.GetRootGameObjects(), true, true);
        }

        public void Hide(Scene scene)
        {
            if (!scene.IsValid())
                return;

            if (!scene.isLoaded)
                return;

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Hide Scene");
            HideNoUndo(scene);
        }

        public void DisablePicking(Scene scene)
        {
            if (!scene.IsValid())
                return;

            if (!scene.isLoaded)
                return;

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Disable Picking Scene");
            DisablePickingNoUndo(scene);
        }

        public bool IsHidden(GameObject gameObject, bool includeDescendants = false)
        {
            if (includeDescendants)
                return SceneVisibilityState.IsHierarchyHidden(gameObject);
            else
                return SceneVisibilityState.IsGameObjectHidden(gameObject);
        }

        public bool IsPickingDisabled(GameObject gameObject, bool includeDescendants = false)
        {
            if (includeDescendants)
                return SceneVisibilityState.IsHierarchyPickingDisabled(gameObject);
            else
                return SceneVisibilityState.IsGameObjectPickingDisabled(gameObject);
        }

        static bool IsIgnoredBySceneVisibility(GameObject go)
        {
            var hideFlags = HideFlags.HideInHierarchy;

            return (go.hideFlags & hideFlags) != 0;
        }

        public bool AreAllDescendantsHidden(Scene scene)
        {
            if (scene.rootCount == 0)
                return false;

            scene.GetRootGameObjects(m_RootBuffer);
            foreach (GameObject root in m_RootBuffer)
            {
                if (IsIgnoredBySceneVisibility(root))
                    continue;

                if (!SceneVisibilityState.IsHierarchyHidden(root))
                    return false;
            }

            return true;
        }

        public bool IsPickingDisabledOnAllDescendants(Scene scene)
        {
            if (scene.rootCount == 0)
                return false;

            scene.GetRootGameObjects(m_RootBuffer);
            foreach (GameObject root in m_RootBuffer)
            {
                if (IsIgnoredBySceneVisibility(root))
                    continue;

                if (!SceneVisibilityState.IsHierarchyPickingDisabled(root))
                    return false;
            }

            return true;
        }

        public bool AreAnyDescendantsHidden(Scene scene)
        {
            return SceneVisibilityState.HasHiddenGameObjects(scene);
        }

        public bool IsPickingDisabledOnAnyDescendant(Scene scene)
        {
            return SceneVisibilityState.ContainsGameObjectsWithPickingDisabled(scene);
        }

        internal enum SceneVisState
        {
            AllHidden,
            AllVisible,
            Mixed
        }

        internal enum ScenePickingState
        {
            PickingDisabledAll,
            PickingEnabledAll,
            Mixed
        }

        internal SceneVisState GetSceneVisibilityState(Scene scene)
        {
            if (AreAllDescendantsHidden(scene))
                return SceneVisState.AllHidden;
            if (AreAnyDescendantsHidden(scene))
                return SceneVisState.Mixed;
            return SceneVisState.AllVisible;
        }

        internal ScenePickingState GetScenePickingState(Scene scene)
        {
            if (IsPickingDisabledOnAllDescendants(scene))
                return ScenePickingState.PickingDisabledAll;
            if (IsPickingDisabledOnAnyDescendant(scene))
                return ScenePickingState.Mixed;
            return ScenePickingState.PickingEnabledAll;
        }

        public void Show(GameObject[] gameObjects, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Show GameObjects");
            SceneVisibilityState.SetGameObjectsHidden(gameObjects, false, includeDescendants);
        }

        public void Hide(GameObject[] gameObjects, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Hide GameObjects");
            SceneVisibilityState.SetGameObjectsHidden(gameObjects, true, includeDescendants);
        }

        public void DisablePicking(GameObject[] gameObjects, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Disable Picking GameObjects");
            SceneVisibilityState.SetGameObjectsPickingDisabled(gameObjects, true, includeDescendants);
        }

        public void EnablePicking(GameObject[] gameObjects, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Enable Picking GameObjects");
            SceneVisibilityState.SetGameObjectsPickingDisabled(gameObjects, false, includeDescendants);
        }

        public void Isolate(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Isolate GameObject");

            IsolateCurrentStage();
            HideAllNoUndo();
            SceneVisibilityState.SetGameObjectHidden(gameObject, false, includeDescendants);
        }

        public void Isolate(GameObject[] gameObjects, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Isolate GameObjects");

            IsolateCurrentStage();
            HideAllNoUndo();
            SceneVisibilityState.SetGameObjectsHidden(gameObjects, false, includeDescendants);
        }

        private void VisibilityChanged()
        {
            visibilityChanged?.Invoke();
        }

        private void PickableContentChanged()
        {
            pickingChanged?.Invoke();
        }

        public void ToggleVisibility(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Visibility");
            SceneVisibilityState.SetGameObjectHidden(gameObject, !SceneVisibilityState.IsGameObjectHidden(gameObject), includeDescendants);
        }

        public void TogglePicking(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Picking");
            SceneVisibilityState.SetGameObjectPickingDisabled(gameObject, !SceneVisibilityState.IsGameObjectPickingDisabled(gameObject), includeDescendants);
        }

        public bool AreAllDescendantsHidden(GameObject gameObject)
        {
            return SceneVisibilityState.AreAllChildrenHidden(gameObject);
        }

        public bool AreAllDescendantsVisible(GameObject gameObject)
        {
            return SceneVisibilityState.AreAllChildrenVisible(gameObject);
        }

        public bool IsPickingDisabledOnAllDescendants(GameObject gameObject)
        {
            return SceneVisibilityState.IsPickingDisabledOnAllChildren(gameObject);
        }

        public bool IsPickingEnabledOnAllDescendants(GameObject gameObject)
        {
            return SceneVisibilityState.IsPickingEnabledOnAllChildren(gameObject);
        }

        public bool IsCurrentStageIsolated()
        {
            return SceneVisibilityState.isolation;
        }

        private void IsolateCurrentStage()
        {
            SceneVisibilityState.isolation = true;
            currentStageIsolated?.Invoke();
        }

        public void ExitIsolation()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Exit Isolation");
            RevertIsolationCurrentStage();
        }

        private static void RevertIsolationCurrentStage()
        {
            SceneVisibilityState.isolation = false;
        }

        //SHORTCUTS
        [Shortcut("Scene Visibility/Toggle Selection Visibility")]
        private static void ToggleSelectionVisibility()
        {
            if (Selection.gameObjects.Length > 0)
            {
                bool shouldHide = true;
                foreach (var gameObject in Selection.gameObjects)
                {
                    if (!instance.IsHidden(gameObject))
                    {
                        break;
                    }

                    shouldHide = false;
                }
                Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Selection Visibility");
                SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, shouldHide, false);
            }
        }

        [Shortcut("Scene Visibility/Toggle Selection And Descendants Visibility", typeof(ShortcutContext), KeyCode.H)]
        private static void ToggleSelectionAndDescendantsVisibility()
        {
            if (Selection.gameObjects.Length > 0)
            {
                bool shouldHide = true;
                foreach (var gameObject in Selection.gameObjects)
                {
                    if (!instance.IsHidden(gameObject))
                    {
                        break;
                    }

                    shouldHide = false;
                }
                Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Selection And Descendants Visibility");
                SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, shouldHide, true);
            }
        }

        [Shortcut("Scene Picking/Toggle Picking On Selection And Descendants", typeof(ShortcutContext), KeyCode.L)]
        private static void ToggleSelectionAndDescendantsPicking()
        {
            if (Selection.gameObjects.Length > 0)
            {
                bool shouldDisablePicking = true;
                foreach (var gameObject in Selection.gameObjects)
                {
                    if (!instance.IsPickingDisabled(gameObject))
                    {
                        break;
                    }

                    shouldDisablePicking = false;
                }
                Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Selection And Descendants Picking");
                SceneVisibilityState.SetGameObjectsPickingDisabled(Selection.gameObjects, shouldDisablePicking, true);
            }
        }

        [Shortcut("Scene Picking/Toggle Picking On Selection")]
        internal static void ToggleSelectionPickable()
        {
            if (Selection.gameObjects.Length > 0)
            {
                bool shouldHide = true;
                foreach (var gameObject in Selection.gameObjects)
                {
                    if (!instance.IsPickingDisabled(gameObject))
                    {
                        break;
                    }

                    shouldHide = false;
                }
                Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Selection Pickable");
                SceneVisibilityState.SetGameObjectsPickingDisabled(Selection.gameObjects, shouldHide, false);
            }
        }

        [Shortcut("Scene Visibility/Exit Isolation")]
        private static void ExitIsolationShortcut()
        {
            instance.ExitIsolation();
        }

        [Shortcut("Scene Visibility/Toggle Isolation On Selection And Descendants", typeof(ShortcutContext), KeyCode.H, ShortcutModifiers.Shift)]
        private static void ToggleIsolateSelectionAndDescendantsShortcut()
        {
            instance.ToggleIsolateSelectionAndDescendants();
        }

        internal void ToggleIsolateSelectionAndDescendants()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Isolation on Selection And Children");

            if (!IsCurrentStageIsolated())
            {
                IsolateCurrentStage();
                HideAllNoUndo();

                if (Selection.gameObjects.Length > 0)
                {
                    SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, false, true);
                }
            }
            else
            {
                RevertIsolationCurrentStage();
            }
        }

        [Shortcut("Scene Visibility/Toggle Isolation on Selection")]
        private static void ToggleIsolateSelectionShortcut()
        {
            instance.ToggleIsolateSelection();
        }

        internal void ToggleIsolateSelection()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Isolation on Selection");

            if (!IsCurrentStageIsolated())
            {
                IsolateCurrentStage();
                HideAllNoUndo();

                if (Selection.gameObjects.Length > 0)
                {
                    SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, false, false);
                }
            }
            else
            {
                RevertIsolationCurrentStage();
            }
        }

        internal void ToggleScene(Scene scene, SceneVisState visibilityState)
        {
            if (visibilityState == SceneVisState.AllVisible || visibilityState == SceneVisState.Mixed)
            {
                Hide(scene);
            }
            else
            {
                Show(scene);
            }
        }
    }
}
