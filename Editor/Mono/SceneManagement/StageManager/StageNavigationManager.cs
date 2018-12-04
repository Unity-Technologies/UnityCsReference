// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace UnityEditor.SceneManagement
{
    internal class StageNavigationManager : ScriptableSingleton<StageNavigationManager>
    {
        List<PrefabStage> m_PrefabStages = new List<PrefabStage>();
        StageNavigationHistory m_NavigationHistory;                                 // Not marked with SerializeField since it should be reset on every restart of Unity
        SavedBool m_AutoSave = new SavedBool("PrefabEditing.AutoSave", true);
        Analytics m_Analytics = new Analytics();
        [NonSerialized]
        double m_NextUpdate;

        public event Action<StageNavigationItem, StageNavigationItem> stageChanging;                             // previousStage, newStage
        public event Action<StageNavigationItem, StageNavigationItem> stageChanged;                              // previousStage, newStage
        public event Action<StageNavigationItem> prefabStageReloading;
        public event Action<StageNavigationItem> prefabStageReloaded;
        public event Action<StageNavigationItem> prefabStageToBeDestroyed;
        public event Action<PrefabStage> prefabStageDirtinessChanged;

        internal StageNavigationItem currentItem
        {
            get { return m_NavigationHistory.currentItem; }
            // No setter since invoking code should explicitly specify desired effect on history.
        }

        internal StageNavigationItem[] stageHistory
        {
            get { return m_NavigationHistory.GetHistory(); }
        }

        internal bool autoSave
        {
            get { return m_AutoSave.value; }
            set { m_AutoSave.value = value; }
        }

        internal Analytics analytics
        {
            get { return m_Analytics; }
        }

        void OnEnable()
        {
            if (m_NavigationHistory == null)
            {
                m_NavigationHistory = new StageNavigationHistory();
                m_NavigationHistory.ClearForwardHistoryAndAddItem(m_NavigationHistory.GetOrCreateMainStage());
            }

            EditorApplication.update += Update;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.newSceneCreated += OnNewSceneCreated;
            AssetEvents.assetsChangedOnHDD += OnAssetsChangedOnHDD;
            PrefabUtility.savingPrefab += OnSavingPrefab;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.editorApplicationQuit += OnQuit;
            PrefabStage.prefabStageSavedAsNewPrefab += OnPrefabStageSavedAsNewPrefab;
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
            AssetEvents.assetsChangedOnHDD -= OnAssetsChangedOnHDD;
            PrefabUtility.savingPrefab -= OnSavingPrefab;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.editorApplicationQuit -= OnQuit;
            PrefabStage.prefabStageSavedAsNewPrefab -= OnPrefabStageSavedAsNewPrefab;
        }

        void OnQuit()
        {
            // Ensure we destroy the prefab scene so all hidden editor objects (environment objects are hidden)
            // are removed before closing down. Currently editor objects should be cleaned up if they have interest
            // in transform changes before shutting down.
            if (currentItem.isPrefabStage)
            {
                GoToMainStage(false, Analytics.ChangeType.GoToMainViaQuitApplication);
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange playmodeState)
        {
            if (playmodeState == PlayModeStateChange.ExitingEditMode)
            {
                if (GetCurrentPrefabStage() != null)
                {
                    bool blockPrefabModeInPlaymode = CheckIfAnyComponentShouldBlockPrefabModeInPlayMode(GetCurrentPrefabStage().prefabAssetPath);
                    if (blockPrefabModeInPlaymode)
                    {
                        GoToMainStage(true, Analytics.ChangeType.GoToMainViaPlayModeBlocking);
                    }
                }
            }
        }

        void OnSavingPrefab(GameObject gameObject, string path)
        {
            foreach (var prefabStage in m_PrefabStages)
                prefabStage.OnSavingPrefab(gameObject, path);
        }

        void OnPrefabStageSavedAsNewPrefab(PrefabStage prefabStage)
        {
            // Current open path has changed: update state in breadcrumbs
            currentItem.SetPrefabAssetPath(prefabStage.prefabAssetPath);
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!currentItem.isMainStage)
            {
                GoToMainStage(false, Analytics.ChangeType.GoToMainViaSceneOpened); // Do not set previous selection as this would e.g remove the selection from the Project Browser when double clicking a scene asset
            }
        }

        void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            if (!currentItem.isMainStage)
            {
                GoToMainStage(false, Analytics.ChangeType.GoToMainViaNewSceneCreated);
            }
        }

        void OnAssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            m_NavigationHistory.OnAssetsChangedOnHDD(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);

            foreach (var prefabStage in m_PrefabStages)
                prefabStage.OnAssetsChangedOnHDD(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        void Update()
        {
            var time = EditorApplication.timeSinceStartup;
            if (time > m_NextUpdate)
            {
                m_NextUpdate = time + 0.2;

                foreach (var prefabStage in m_PrefabStages)
                    prefabStage.Update();
            }
        }

        internal PrefabStage GetPrefabStage(string prefabAssetPath)
        {
            foreach (var prefabStage in m_PrefabStages)
                if (prefabStage.prefabAssetPath == prefabAssetPath)
                    return prefabStage;

            return null;
        }

        public bool IsPrefabStage(Scene scene)
        {
            foreach (var prefabStage in m_PrefabStages)
                if (prefabStage.scene == scene)
                    return true;

            return false;
        }

        public PrefabStage GetCurrentPrefabStage()
        {
            // We currently only support one prefab stage at a time
            if (m_PrefabStages.Count == 0)
                return null;

            return m_PrefabStages[0];
        }

        void DestroyPrefabStage(PrefabStage prefabStage)
        {
            if (prefabStage == null)
                return;

            prefabStage.CloseStage();
            m_PrefabStages.Remove(prefabStage);
        }

        // Returns false if the user clicked Cancel to save otherwise returns true
        internal bool AskUserToSaveModifiedPrefabStageBeforeDestroyingStage(PrefabStage prefabStage)
        {
            if (prefabStage != null)
                return prefabStage.AskUserToSaveDirtySceneBeforeDestroyingScene();

            return true;
        }

        // Returns true if we cleaned up (saved changes or user discarded changes), false if the user cancelled cleaning up.
        bool CleanupCurrentStageBeforeSwitchingStage()
        {
            var prefabStage = currentItem.prefabStage;
            if (prefabStage != null)
            {
                if (EditorApplication.isCompiling && prefabStage.HasSceneBeenModified())
                {
                    SceneView.ShowNotification("Compiling must finish before you can exit Prefab Mode");
                    SceneView.RepaintAll();
                    return false;
                }

                bool continueDestroyingScene = AskUserToSaveModifiedPrefabStageBeforeDestroyingStage(prefabStage);
                if (!continueDestroyingScene)
                    return false;
            }

            // We want to track the time from destroying a prefab stage and until the new stage is up and running, so we place the start
            m_Analytics.ChangingStageStarted(currentItem);

            if (prefabStage != null)
            {
                if (prefabStageToBeDestroyed != null)
                    prefabStageToBeDestroyed(currentItem);

                DestroyPrefabStage(prefabStage);
            }
            return true;
        }

        internal void PrefabStageDirtinessChanged(PrefabStage prefabStage)
        {
            if (prefabStageDirtinessChanged != null)
                prefabStageDirtinessChanged(prefabStage);
        }

        internal void PrefabStageReloading(PrefabStage prefabStage)
        {
            if (prefabStage == currentItem.prefabStage)
            {
                if (prefabStageReloading != null)
                    prefabStageReloading(currentItem);
            }
            else
            {
                Debug.LogError("Reloading a Prefab scene that is not the current stage is not supported currently.");
            }
        }

        internal void PrefabStageReloaded(PrefabStage prefabStage)
        {
            if (prefabStage == currentItem.prefabStage)
            {
                if (prefabStageReloaded != null)
                    prefabStageReloaded(currentItem);
            }
            else
            {
                Debug.LogError("Reloading a Prefab scene that is not the current stage is not supported currently.");
            }
        }

        internal void NavigateBack(Analytics.ChangeType stageChangeAnalytics)
        {
            if (m_NavigationHistory.CanGoBackward())
            {
                var previousStage = m_NavigationHistory.GetPrevious();
                SwitchToStage(previousStage, false, true, stageChangeAnalytics);
            }
        }

        internal void GoToMainStage(bool setPreviousSelection, Analytics.ChangeType stageChangeAnalytics)
        {
            if (currentItem.isMainStage)
                return;

            SwitchToStage(m_NavigationHistory.GetOrCreateMainStage(), false, setPreviousSelection, stageChangeAnalytics);
        }

        static void StopAnimationPlaybackAndPreviewing()
        {
            var animWindows = AnimationWindow.GetAllAnimationWindows();
            foreach (var animWindow in animWindows)
            {
                animWindow.state.StopPreview();
                animWindow.state.StopPlayback();
            }
        }

        internal bool SwitchToStage(StageNavigationItem newStage, bool setAsFirstItemAfterMainStage, bool setPreviousSelection, Analytics.ChangeType changeTypeAnalytics)
        {
            if (newStage.isPrefabStage && !newStage.prefabAssetExists)
            {
                Debug.LogError("Cannot switch to new stage! Prefab asset does not exist: " + (!string.IsNullOrEmpty(newStage.prefabAssetPath) ? newStage.prefabAssetPath : newStage.prefabAssetGUID));
                {
                    return false;
                }
            }

            StopAnimationPlaybackAndPreviewing();

            // Close current stage (returns false if user cancels closing prefab scene)
            if (!CleanupCurrentStageBeforeSwitchingStage())
            {
                return false;
            }

            newStage.setSelectionAndScrollWhenBecomingCurrentStage = setPreviousSelection;

            if (stageChanging != null)
                stageChanging(currentItem, newStage);

            // Switch to new stage.
            if (!newStage.isMainStage)
            {
                // Create prefab stage and add it to the list of Prefab stages before loading the prefab contents so the
                // user callbacks Awake and OnEnable are able to query the current prefab stage
                PrefabStage prefabStage = new PrefabStage();
                m_PrefabStages.Add(prefabStage);

                var success = prefabStage.OpenStage(newStage.prefabAssetPath);
                if (!success)
                {
                    m_PrefabStages.RemoveAt(m_PrefabStages.Count - 1);
                    return false;
                }
            }

            // Set new stage after we allowed the user to cancel changing stage
            OnStageSwitched(newStage, setAsFirstItemAfterMainStage, changeTypeAnalytics);

            return true;
        }

        void OnStageSwitched(StageNavigationItem newStage, bool setAsFirstItemAfterMainStage, Analytics.ChangeType changeTypeAnalytics)
        {
            var previousStage = currentItem;

            if (setAsFirstItemAfterMainStage)
            {
                m_NavigationHistory.ClearHistory();
                m_NavigationHistory.AddItem(newStage);
            }
            else
            {
                if (m_NavigationHistory.TrySetToIndexOfItem(newStage))
                    m_NavigationHistory.ClearForwardHistoryAfterCurrentStage();
                else
                    m_NavigationHistory.ClearForwardHistoryAndAddItem(newStage);
            }

            if (stageChanged != null)
                stageChanged(previousStage, newStage);

            m_Analytics.ChangingStageEnded(newStage, changeTypeAnalytics);
        }

        // Returns true any component in prefab is blocking Prefab Mode in Play Mode
        static bool CheckIfAnyComponentShouldBlockPrefabModeInPlayMode(string prefabAssetPath)
        {
            var assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
            var monoBehaviors = assetRoot.GetComponentsInChildren<MonoBehaviour>(true);  // also check the inactive since these can be made active while in play mode
            var warnList = new List<MonoBehaviour>();
            foreach (var m in monoBehaviors)
            {
                if (m != null && !m.allowPrefabModeInPlayMode)
                    warnList.Add(m);
            }

            if (warnList.Count > 0)
            {
                foreach (var m in warnList)
                {
                    var monoScript = MonoScript.FromMonoBehaviour(m);
                    Debug.LogWarningFormat(monoScript, "Prefab Mode in Play Mode was blocked by the script '{0}' to prevent the script accidentally affecting Play Mode. See the documentation for [ExecuteInEditMode] and [ExecuteAlways] for info on how to make scripts compatible with Prefab Mode during Play Mode.", monoScript.name);
                }
                return true;
            }
            return false;
        }

        internal PrefabStage OpenPrefabMode(string prefabAssetPath, GameObject instanceRoot, Analytics.ChangeType changeTypeAnalytics)
        {
            if (EditorApplication.isPlaying)
            {
                bool blockPrefabModeInPlaymode = CheckIfAnyComponentShouldBlockPrefabModeInPlayMode(prefabAssetPath);
                if (blockPrefabModeInPlaymode)
                    return null;
            }

            PrefabStage prefabStage = GetCurrentPrefabStage();
            bool setAsFirstItemAfterMainStage = prefabStage == null || !IsPartOfPrefabStage(instanceRoot, prefabStage);

            var previousSelection = Selection.activeGameObject;
            UInt64 previousFileID = (instanceRoot != null) ? GetFileIDForCorrespondingObjectFromSourceAtPath(previousSelection, prefabAssetPath) : 0;

            if (SwitchToStage(m_NavigationHistory.GetOrCreatePrefabStage(prefabAssetPath), setAsFirstItemAfterMainStage, false, changeTypeAnalytics))
            {
                // If selection did not change by switching stage (by us or user) then handle automatic selection in new prefab mode
                if (Selection.activeGameObject == previousSelection)
                {
                    HandleSelectionWhenSwithingToNewPrefabMode(GetCurrentPrefabStage().prefabContentsRoot, previousFileID);
                }

                var newPrefabStage = m_PrefabStages.Last();
                Assert.IsTrue(newPrefabStage.prefabAssetPath == prefabAssetPath);
                SceneView.RepaintAll();
                return newPrefabStage;
            }
            else
            {
                // Failed to switch to new stage
                return null;
            }
        }

        static UInt64 GetFileIDForCorrespondingObjectFromSourceAtPath(GameObject gameObject, string prefabAssetPath)
        {
            if (gameObject == null)
                return 0;

            if (EditorUtility.IsPersistent(gameObject))
                return 0;

            if (!PrefabUtility.IsPartOfNonAssetPrefabInstance(gameObject))
                return 0;

            GameObject assetGameObject = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(gameObject, prefabAssetPath);
            if (assetGameObject == null)
                return 0;

            return SceneHierarchyState.GetOrGenerateFileID(assetGameObject);
        }

        static void HandleSelectionWhenSwithingToNewPrefabMode(GameObject prefabContentsRoot, UInt64 previousFileID)
        {
            GameObject newSelection = null;

            if (previousFileID != 0)
                newSelection = SceneHierarchyState.FindFirstGameObjectThatMatchesFileID(prefabContentsRoot.transform, previousFileID);

            if (newSelection == null)
                newSelection = prefabContentsRoot;

            Selection.activeGameObject = newSelection;

            // For Prefab Mode we restore the last expanded tree view state for the opened Prefab. For usability
            // if a child GameObject on the Prefab Instance is selected when entering the Prefab Asset Mode we select the corresponding
            // child GameObject in the Asset. Here we ensure that selction is revealed and framed in the Scene hierarchy.
            if (newSelection != prefabContentsRoot)
            {
                foreach (SceneHierarchyWindow shw in SceneHierarchyWindow.GetAllSceneHierarchyWindows())
                    shw.FrameObject(newSelection.GetInstanceID(), false);
            }
        }

        static bool IsPartOfPrefabStage(GameObject gameObject, PrefabStage prefabStage)
        {
            if (gameObject == null)
                return false;
            return FindGameObjectRecursive(prefabStage.prefabContentsRoot.transform, gameObject);
        }

        static bool FindGameObjectRecursive(Transform transform, GameObject gameObject)
        {
            if (transform.gameObject == gameObject)
                return true;

            for (int i = 0; i < transform.childCount; ++i)
            {
                if (FindGameObjectRecursive(transform.GetChild(i), gameObject))
                    return true;
            }
            return false;
        }

        [OnOpenAsset]
        static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);

            if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                // The 'line' parameter can be used for passing an instanceID of a prefab instance
                GameObject instanceRoot = line == -1 ? null : EditorUtility.InstanceIDToObject(line) as GameObject;

                PrefabStageUtility.OpenPrefab(assetPath, instanceRoot, Analytics.ChangeType.EnterViaAssetOpened);
                return true;
            }
            return false;
        }

        public void PlaceGameObjectInCurrentStage(GameObject go)
        {
            // For prefab stages we want to ensure new root GameObjects are auto-parented under the prefab root if possible.
            // Note that users can get Awake and OnEnable callbacks while loading a Prefab into Prefab Mode, at this time
            // the PrefabStage is not fully initialized as it does not have a reference to the prefabContentsRoot yet. In this case
            // the go is not auto parented and the parenting must be handled by the client.
            var prefabStage = GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.initialized && go != null && go.transform.parent == null)
            {
                go.transform.SetParent(prefabStage.prefabContentsRoot.transform, true);
            }
        }

        [RequiredByNativeCode]
        internal static void Internal_PlaceGameObjectInCurrentStage(GameObject go)
        {
            instance.PlaceGameObjectInCurrentStage(go);
        }

        [RequiredByNativeCode]
        internal static bool Internal_HasCurrentPrefabStage()
        {
            return instance.GetCurrentPrefabStage() != null;
        }

        [RequiredByNativeCode]
        internal static void Internal_SaveCurrentPrefabStage()
        {
            var prefabStage = instance.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.HasSceneBeenModified())
                prefabStage.SavePrefabWithVersionControlDialogAndRenameDialog();
        }

        [RequiredByNativeCode]
        internal static void Internal_SaveCurrentPrefabStageWithSavePanel()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                prefabStage.SaveAsNewPrefabWithSavePanel();
            }
        }

        [Serializable]
        internal class Analytics
        {
            public enum ChangeType
            {
                Unknown,
                EnterViaInstanceHierarchyRightArrow,
                EnterViaInstanceHierarchyContextMenu,
                EnterViaInstanceInspectorOpenButton,
                EnterViaAssetInspectorOpenButton,
                EnterViaAssetOpened,
                EnterViaUnknown,
                GoToMainViaSceneOpened,
                GoToMainViaNewSceneCreated,
                GoToMainViaPlayModeBlocking,
                GoToMainViaQuitApplication,
                GoToMainViaAvatarSetup,
                GoToMainViaUnknown,
                NavigateBackViaHierarchyHeaderLeftArrow,
                NavigateBackViaUnknown,
                NavigateViaBreadcrumb,
            }

            public enum StageType
            {
                MainStage,
                PrefabStage,
                Unknown,
            }

            [Serializable]
            class EventData
            {
                public ChangeType changeType;
                public StageType existingStage;
                public StageType newStage;
                public int existingBreadcrumbCount;
                public int newBreadcrumbCount;
                public bool autoSaveEnabled;
                public bool didUserModify;
                public bool didUserSave;
            }
            EventData m_EventData;
            DateTime m_StartedTime;

            public void ChangingStageStarted(StageNavigationItem previousStageItem)
            {
                m_StartedTime = DateTime.UtcNow;

                m_EventData = new EventData();
                m_EventData.existingStage = GetStageType(previousStageItem);
                m_EventData.existingBreadcrumbCount = StageNavigationManager.instance.stageHistory.Length;
                if (previousStageItem.isPrefabStage)
                {
                    m_EventData.didUserModify = previousStageItem.prefabStage.analyticsDidUserModify;
                    m_EventData.didUserSave = previousStageItem.prefabStage.analyticsDidUserSave;
                }
            }

            public void ChangingStageEnded(StageNavigationItem newStageItem, Analytics.ChangeType changeTypeAnalytics)
            {
                m_EventData.changeType = changeTypeAnalytics;
                m_EventData.newStage = GetStageType(newStageItem);
                m_EventData.newBreadcrumbCount = StageNavigationManager.instance.stageHistory.Length;
                m_EventData.autoSaveEnabled = StageNavigationManager.instance.autoSave;
                var duration = DateTime.UtcNow.Subtract(m_StartedTime);

                UsabilityAnalytics.SendEvent("stageChange", m_StartedTime, duration, true, m_EventData);
            }

            static StageType GetStageType(StageNavigationItem item)
            {
                if (item.isMainStage)
                    return StageType.MainStage;
                if (item.isPrefabStage)
                    return StageType.PrefabStage;
                return StageType.Unknown;
            }
        }
    }
}
