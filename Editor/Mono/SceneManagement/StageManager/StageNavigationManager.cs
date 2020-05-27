// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using System.Collections.ObjectModel;

namespace UnityEditor.SceneManagement
{
    internal class StageNavigationManager : ScriptableSingleton<StageNavigationManager>
    {
        StageNavigationHistory m_NavigationHistory;                                 // Not marked with SerializeField since it should be reset on every restart of Unity
        SavedBool m_AutoSave = new SavedBool("PrefabEditing.AutoSave", true);
        SavedInt m_ContextRenderMode = new SavedInt("PrefabEditing.ContextRenderMode", (int)StageUtility.ContextRenderMode.GreyedOut);
        Analytics m_Analytics = new Analytics();
        [NonSerialized]
        double m_NextUpdate;
        [NonSerialized]
        bool m_DebugLogging = false;

        public event Action<Stage, Stage> stageChanging;                             // previousStage, newStage
        public event Action<Stage, Stage> stageChanged;                              // previousStage, newStage
        public event Action<Stage> beforeSwitchingAwayFromStage;
        public event Action<Stage> afterSuccessfullySwitchedToStage;

        internal Stage currentStage
        {
            get { return m_NavigationHistory.currentStage; }
            // No setter since invoking code should explicitly specify desired effect on history.
        }

        internal MainStage mainStage
        {
            get { return m_NavigationHistory.GetMainStage(); }
        }

        internal ReadOnlyCollection<Stage> stageHistory
        {
            get { return m_NavigationHistory.GetHistory(); }
        }

        internal bool autoSave
        {
            get { return m_AutoSave.value; }
            set { m_AutoSave.value = value; }
        }
        internal StageUtility.ContextRenderMode contextRenderMode
        {
            get { return (StageUtility.ContextRenderMode)m_ContextRenderMode.value; }
            set { m_ContextRenderMode.value = (int)value; }
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
                m_NavigationHistory.Init();
            }

            EditorApplication.update += Update;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.newSceneCreated += OnNewSceneCreated;
            EditorApplication.editorApplicationQuit += OnQuit;
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
            EditorApplication.editorApplicationQuit -= OnQuit;
        }

        void OnQuit()
        {
            // Ensure we destroy the prefab stages so all hidden editor objects (environment objects are hidden)
            // are removed before closing down. Currently editor objects should be cleaned up if they have interest
            // in transform changes before shutting down.
            GoToMainStage(Analytics.ChangeType.GoToMainViaQuitApplication);
        }

        internal Stage GetStage(Scene scene)
        {
            var inputStageHandle = StageHandle.GetStageHandle(scene);
            var result = stageHistory.FirstOrDefault(stage => stage.stageHandle == inputStageHandle);
            return result;
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!(currentStage is MainStage))
            {
                GoToMainStage(Analytics.ChangeType.GoToMainViaSceneOpened); // Do not set previous selection as this would e.g remove the selection from the Project Browser when double clicking a scene asset
            }
        }

        void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            if (!(currentStage is MainStage))
            {
                GoToMainStage(Analytics.ChangeType.GoToMainViaNewSceneCreated);
            }
        }

        void Update()
        {
            var time = EditorApplication.timeSinceStartup;
            if (time > m_NextUpdate)
            {
                m_NextUpdate = time + 0.2;
                ValidateAndTickStages(true);
            }
        }

        // internal for testing
        internal void ValidateAndTickStages(bool showDialogIfInvalid)
        {
            var stageHistory = m_NavigationHistory.GetHistory();
            foreach (var stage in stageHistory)
            {
                if (stage == null)
                {
                    if (showDialogIfInvalid)
                        EditorUtility.DisplayDialog("Stage Error", "A stage has been destroyed unexpectedly.\n\nReturning to the MainStage.", "OK");
                    GoToMainStage(Analytics.ChangeType.Unknown);
                    return;
                }
            }

            if (!currentStage.isValid)
            {
                var errorMsg = currentStage.GetErrorMessage();
                if (showDialogIfInvalid)
                    EditorUtility.DisplayDialog("Stage Error", errorMsg, "OK");
                GoToMainStage(Analytics.ChangeType.Unknown);
                return;
            }

            foreach (var stage in stageHistory)
            {
                stage.Tick();
            }
        }

        internal void NavigateBack(Analytics.ChangeType stageChangeAnalytics)
        {
            if (m_NavigationHistory.CanGoBackward())
            {
                var previousStage = m_NavigationHistory.GetPrevious();
                SwitchToStage(previousStage, false, stageChangeAnalytics);
            }
        }

        internal void GoToMainStage(Analytics.ChangeType stageChangeAnalytics)
        {
            if (currentStage is MainStage)
                return;

            SwitchToStage(m_NavigationHistory.GetMainStage(), false, stageChangeAnalytics);
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

        internal bool SwitchToStage(Stage stage, bool setAsFirstItemAfterMainStage, Analytics.ChangeType changeTypeAnalytics)
        {
            if (stage == null)
            {
                Debug.LogError("Cannot switch to new stage. Input stage is null");
                return false;
            }

            bool setPreviousSelection = stage.opened;

            StopAnimationPlaybackAndPreviewing();

            Stage previousStage = currentStage;
            if (!previousStage.AskUserToSaveModifiedStageBeforeSwitchingStage())
            {
                // User canceled switching stage.
                // If the history contains the new stage we do not destroy it (the user
                // can have clicked a previous stage in the breadcrumb bar).
                if (!stageHistory.Contains(stage))
                    DestroyImmediate(stage);
                return false;
            }

            // User accepted to switch stage (and lose any data if not saved)
            // Here we save current Hierarchy and SceneView stage state
            beforeSwitchingAwayFromStage?.Invoke(previousStage);

            // Used by the Avatar Editor to exit its editing mode before opening new stage
            stageChanging?.Invoke(previousStage, stage);

            // Set/Add stage in m_NavigationHistory (and detect what stages should be removed)
            if (m_DebugLogging)
                Debug.Log("Set Navigation History (setAsFirstItemAfterMainStage " + setAsFirstItemAfterMainStage);

            var deleteStages = new List<Stage>();
            if (setAsFirstItemAfterMainStage)
            {
                deleteStages = m_NavigationHistory.ClearHistory();
                m_NavigationHistory.AddStage(stage);
            }
            else
            {
                if (m_NavigationHistory.TrySetToIndexOfItem(stage))
                    deleteStages = m_NavigationHistory.ClearForwardHistoryAfterCurrentStage();
                else
                    deleteStages = m_NavigationHistory.ClearForwardHistoryAndAddItem(stage);
            }

            m_Analytics.ChangingStageStarted(previousStage);

            if (m_DebugLogging)
                Debug.Log("Activate new stage");

            // Activate stage after setting up the history above so objects loaded during ActivateStage can query current stage
            bool success;
            try
            {
                if (!stage.opened)
                {
                    stage.opened = true;
                    success = stage.OnOpenStage();
                }
                else
                {
                    stage.OnReturnToStage();
                    success = stage.isValid;
                }

                if (success)
                {
                    if (m_DebugLogging)
                        Debug.Log("Deactivate previous stage");

                    // Here the Hierarchy and SceneView sync's up to the new stage
                    stage.setSelectionAndScrollWhenBecomingCurrentStage = setPreviousSelection;
                    stageChanged?.Invoke(previousStage, stage);
                }
            }
            catch (Exception e)
            {
                success = false;
                Debug.LogError("Error while changing Stage: " + e);
            }

            if (success)
            {
                // Activation and changing stage succeeded. Now destroy removed stages
                if (m_DebugLogging)
                    Debug.Log("Destroying " + deleteStages.Count + " stages");

                // A previous existing stage can have been requested to set as the current so don't delete that
                deleteStages.Remove(stage);

                DeleteStagesInReverseOrder(deleteStages);

                // Here we update state that relies on old scenes having been destroyed entirely.
                afterSuccessfullySwitchedToStage?.Invoke(stage);

                m_Analytics.ChangingStageEnded(stage, changeTypeAnalytics);
            }
            else
            {
                if (m_DebugLogging)
                    Debug.Log("Switching stage failed (" + stage + ")");

                RecoverFromStageChangeError(previousStage, deleteStages);
            }

            return success;
        }

        struct PreviewSceneLeakDetection
        {
            Type m_StageType;
            Scene m_Scene;

            internal void Init(PreviewSceneStage previewSceneStage)
            {
                m_StageType = null;
                m_Scene = default(Scene);

                if (previewSceneStage != null)
                {
                    m_StageType = previewSceneStage.GetType();
                    m_Scene = previewSceneStage.scene;
                }
            }

            internal void LogErrorIfPreviewSceneWasNotDestroyed()
            {
                if (m_StageType != null && m_Scene.IsValid())
                    Debug.LogError($"Stage type '{m_StageType}' did not clean up properly: A PreviewScene was leaked. Ensure to call 'base.OnCloseStage()' from your implementation of OnCloseStage().");
            }
        }

        static void DeleteStagesInReverseOrder(List<Stage> stagesToDelete)
        {
            var previewSceneLeakDetection = new PreviewSceneLeakDetection();

            // Remove in reverse order of added (simulates going back one stage at a time)
            for (int i = stagesToDelete.Count - 1; i >= 0; --i)
            {
                var removeStage = stagesToDelete[i];
                if (removeStage != null)
                {
                    previewSceneLeakDetection.Init(removeStage as PreviewSceneStage);
                    DestroyImmediate(removeStage);
                    previewSceneLeakDetection.LogErrorIfPreviewSceneWasNotDestroyed();
                }
            }
        }

        void RecoverFromStageChangeError(Stage previousStage, List<Stage> deleteStages)
        {
            // Recover by going back to main stage
            if (m_NavigationHistory.TrySetToIndexOfItem(m_NavigationHistory.GetMainStage()))
            {
                var lastStages = m_NavigationHistory.ClearForwardHistoryAfterCurrentStage();
                if (lastStages.Count > 0)
                    deleteStages.InsertRange(0, lastStages); // Insert at start so we ensure the same order as in history

                try
                {
                    // Here the Hierarchy and SceneView sync's up to the new stage
                    stageChanged?.Invoke(previousStage, m_NavigationHistory.GetMainStage());
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while recovering from Stage change error: " + e);
                }

                DeleteStagesInReverseOrder(deleteStages);
            }
            else
                Debug.LogError("Could not set MainStage to recover from error");
        }

        public void PlaceGameObjectInCurrentStage(GameObject gameObject)
        {
            if (gameObject != null && gameObject.transform.parent == null)
                currentStage.PlaceGameObjectInStage(gameObject);
        }

        [RequiredByNativeCode]
        internal static void Internal_PlaceGameObjectInCurrentStage(GameObject go)
        {
            instance.PlaceGameObjectInCurrentStage(go);
        }

        [RequiredByNativeCode]
        internal static bool Internal_HasCurrentNonMainStage()
        {
            return !(instance.currentStage is MainStage);
        }

        [RequiredByNativeCode]
        internal static bool Internal_CurrentNonMainStageSupportsSaving()
        {
            return instance.currentStage.SupportsSaving();
        }

        [RequiredByNativeCode]
        internal static void Internal_SaveCurrentStage()
        {
            var stage = instance.currentStage;
            if (stage != null && stage.SupportsSaving() && stage.hasUnsavedChanges)
                stage.Save();
        }

        [RequiredByNativeCode]
        internal static void Internal_SaveCurrentStageAsNew()
        {
            var stage = instance.currentStage;
            if (stage != null && stage.SupportsSaving())
                stage.SaveAsNew();
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

            public void ChangingStageStarted(Stage previousStage)
            {
                m_StartedTime = DateTime.UtcNow;

                m_EventData = new EventData();
                m_EventData.existingStage = GetStageType(previousStage);
                m_EventData.existingBreadcrumbCount = StageNavigationManager.instance.stageHistory.Count;
                if (previousStage is PrefabStage)
                {
                    var prefabStage = (PrefabStage)previousStage;
                    m_EventData.didUserModify = prefabStage.analyticsDidUserModify;
                    m_EventData.didUserSave = prefabStage.analyticsDidUserSave;
                }
            }

            public void ChangingStageEnded(Stage newStage, Analytics.ChangeType changeTypeAnalytics)
            {
                m_EventData.changeType = changeTypeAnalytics;
                m_EventData.newStage = GetStageType(newStage);
                m_EventData.newBreadcrumbCount = StageNavigationManager.instance.stageHistory.Count;
                m_EventData.autoSaveEnabled = StageNavigationManager.instance.autoSave;
                var duration = DateTime.UtcNow.Subtract(m_StartedTime);

                UsabilityAnalytics.SendEvent("stageChange", m_StartedTime, duration, true, m_EventData);
            }

            static StageType GetStageType(Stage stage)
            {
                if (stage is MainStage)
                    return StageType.MainStage;
                if (stage is PrefabStage)
                    return StageType.PrefabStage;
                return StageType.Unknown;
            }
        }
    }
}
