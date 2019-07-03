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

        internal static event Action currentStageIsolated;

        private readonly static List<GameObject> m_RootBuffer = new List<GameObject>();

        internal bool enableSceneVisibility
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
            instance.VisibilityChanged();
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
            instance.VisibilityChanged();
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
            instance.VisibilityChanged();
        }

        private static void UndoRedoPerformed()
        {
            instance.VisibilityChanged();
        }

        public void HideAll()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Hide All");
            HideAllNoUndo();
            VisibilityChanged();
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
                    Hide(SceneManager.GetSceneAt(i), false);
                }
            }
        }

        public void Show(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Show GameObject");
            SceneVisibilityState.SetGameObjectHidden(gameObject, false, includeDescendants);
            VisibilityChanged();
        }

        public void Hide(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Set GameObject Hidden");
            SceneVisibilityState.SetGameObjectHidden(gameObject, true, includeDescendants);
            VisibilityChanged();
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
            VisibilityChanged();
        }

        private void Show(Scene scene, bool sendContentChangedEvent)
        {
            if (!scene.IsValid())
                return;

            SceneVisibilityState.ShowScene(scene);

            if (sendContentChangedEvent)
            {
                VisibilityChanged();
            }
        }

        public void Show(Scene scene)
        {
            if (!scene.IsValid())
                return;

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Show Scene");
            Show(scene, true);
        }

        private void Hide(Scene scene, bool sendContentChangedEvent)
        {
            if (!scene.IsValid())
                return;

            SceneVisibilityState.ShowScene(scene);
            SceneVisibilityState.SetGameObjectsHidden(scene.GetRootGameObjects(), true, true);

            if (sendContentChangedEvent)
            {
                VisibilityChanged();
            }
        }

        public void Hide(Scene scene)
        {
            if (!scene.IsValid())
                return;

            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Hide Scene");
            Hide(scene, true);
        }

        public bool IsHidden(GameObject gameObject, bool includeDescendants = false)
        {
            if (includeDescendants)
                return SceneVisibilityState.IsHierarchyHidden(gameObject);
            else
                return SceneVisibilityState.IsGameObjectHidden(gameObject);
        }

        static bool IsIgnoredBySceneVisibility(GameObject go)
        {
            var hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

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

        public bool AreAnyDescendantsHidden(Scene scene)
        {
            return SceneVisibilityState.HasHiddenGameObjects(scene);
        }

        internal enum SceneState
        {
            AllHidden,
            AllVisible,
            Mixed
        }

        internal SceneState GetSceneState(Scene scene)
        {
            if (AreAllDescendantsHidden(scene))
                return SceneState.AllHidden;
            if (AreAnyDescendantsHidden(scene))
                return SceneState.Mixed;
            return SceneState.AllVisible;
        }

        public void Show(GameObject[] gameObjects, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Show GameObjects");
            SceneVisibilityState.SetGameObjectsHidden(gameObjects, false, includeDescendants);
            VisibilityChanged();
        }

        public void Hide(GameObject[] gameObjects, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Hide GameObjects");
            SceneVisibilityState.SetGameObjectsHidden(gameObjects, true, includeDescendants);
            VisibilityChanged();
        }

        public void Isolate(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Isolate GameObject");

            IsolateCurrentStage();
            HideAllNoUndo();
            SceneVisibilityState.SetGameObjectHidden(gameObject, false, includeDescendants);
            VisibilityChanged();
        }

        public void Isolate(GameObject[] gameObjects, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Isolate GameObjects");

            IsolateCurrentStage();
            HideAllNoUndo();
            SceneVisibilityState.SetGameObjectsHidden(gameObjects, false, includeDescendants);
            VisibilityChanged();
        }

        private void VisibilityChanged()
        {
            visibilityChanged?.Invoke();
        }

        public void ToggleVisibility(GameObject gameObject, bool includeDescendants)
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Visibility");
            SceneVisibilityState.SetGameObjectHidden(gameObject, !SceneVisibilityState.IsGameObjectHidden(gameObject), includeDescendants);
            VisibilityChanged();
        }

        public bool AreAllDescendantsHidden(GameObject gameObject)
        {
            return SceneVisibilityState.AreAllChildrenHidden(gameObject);
        }

        public bool AreAllDescendantsVisible(GameObject gameObject)
        {
            return SceneVisibilityState.AreAllChildrenVisible(gameObject);
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
                instance.VisibilityChanged();
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
                Undo.RecordObject(SceneVisibilityState.GetInstance(), "Toggle Visibility And Children");
                SceneVisibilityState.SetGameObjectsHidden(Selection.gameObjects, shouldHide, true);
                instance.VisibilityChanged();
            }
        }

        public bool IsCurrentStageIsolated()
        {
            return StageNavigationManager.instance.currentItem.isPrefabStage ? SceneVisibilityState.prefabStageIsolated : SceneVisibilityState.mainStageIsolated;
        }

        private void IsolateCurrentStage()
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

        public void ExitIsolation()
        {
            Undo.RecordObject(SceneVisibilityState.GetInstance(), "Exit Isolation");

            if (IsCurrentStageIsolated()) //already isolated
            {
                RevertIsolationCurrentStage();
                VisibilityChanged();
            }
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
        private static void ExitIsolationShortcut()
        {
            instance.ExitIsolation();
        }

        [Shortcut("Scene Visibility/Toggle Isolation On Selection And Descendants", typeof(ShortcutContext), KeyCode.H, ShortcutModifiers.Shift)]
        static void ToggleIsolateSelectionAndDescendantsShortcut()
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

                VisibilityChanged();
            }
            else
            {
                RevertIsolationCurrentStage();
                VisibilityChanged();
            }
        }

        [Shortcut("Scene Visibility/Toggle Isolation on Selection")]
        static void ToggleIsolateSelectionShortcut()
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

                VisibilityChanged();
            }
            else
            {
                RevertIsolationCurrentStage();
                VisibilityChanged();
            }
        }

        internal void ToggleScene(Scene scene, SceneState state)
        {
            if (state == SceneState.AllVisible || state == SceneState.Mixed)
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
