// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor
{
    class SceneViewStageHandling
    {
        SceneView m_SceneView;
        StateCache<SceneViewCameraState> m_StateCache = new StateCache<SceneViewCameraState>("Library/StateCache/SceneView/");
        StateCache<SceneViewSearchState> m_SearchStateCache = new StateCache<SceneViewSearchState>("Library/StateCache/SceneViewSearch/");

        BreadcrumbBar m_BreadcrumbBar = new BreadcrumbBar();
        bool m_BreadcrumbInitialized;
        Stage m_StageClickedFromBreadcrumb;

        public bool isShowingBreadcrumbBar
        {
            get { return StageNavigationManager.instance.stageHistory.Count > 1; }
        }

        public float breadcrumbHeight { get { return BreadcrumbBar.DefaultStyles.background.fixedHeight; }}

        Stage currentStage { get { return StageNavigationManager.instance.currentStage; } }

        public SceneViewStageHandling(SceneView sceneView)
        {
            m_SceneView = sceneView;
        }

        public void OnEnable()
        {
            StageNavigationManager.instance.stageChanged += OnStageChanged;
            AssetEvents.assetsChangedOnHDD += AssetsChangedOnHDD;

            // We need to sync to the current stage to ensure we have the correct SceneView settings: We could have closed Unity while a prefab scene was open
            // this means that the SceneView settings for that prefab is saved to the window layout. Opening Unity always opens the main scenes so here we ensure
            // we have e.g the corrct sky box and other settings.
            currentStage.SyncSceneViewToStage(m_SceneView);
            LoadCameraState(m_SceneView, currentStage);
        }

        public void OnDisable()
        {
            // Ensure saving current stage settings so we can reconstruct them on OnEnable
            SaveCameraState(m_SceneView, currentStage);

            StageNavigationManager.instance.stageChanged -= OnStageChanged;
            AssetEvents.assetsChangedOnHDD -= AssetsChangedOnHDD;
        }

        public void StartOnGUI()
        {
            currentStage.OnPreSceneViewRender(m_SceneView);
        }

        public void EndOnGUI()
        {
            currentStage.OnPostSceneViewRender(m_SceneView);
        }

        internal void RebuildBreadcrumbBar()
        {
            m_BreadcrumbInitialized = false;
        }

        void AssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            m_BreadcrumbInitialized = false;
        }

        void OnStageChanged(Stage previousStage, Stage newStage)
        {
            SaveCameraState(m_SceneView, previousStage);
            newStage.SyncSceneViewToStage(m_SceneView);

            var contextStage = newStage.GetContextStage();
            if (GetStoredCameraState(m_SceneView, contextStage) == null)
                newStage.OnFirstTimeOpenStageInSceneView(m_SceneView);
            else
                LoadCameraState(m_SceneView, contextStage);

            SwitchSearchFilter(previousStage, newStage);

            m_BreadcrumbInitialized = false;
            m_SceneView.OnStageChanged(previousStage, newStage);
        }

        // Unlike the camera state, the search is kept per stage and not per context stage
        void SwitchSearchFilter(Stage previousStage, Stage newStage)
        {
            // The legacy Hierarchy clears the search of all windows on stage changes
            if (EditorSettings.useLegacyHierarchy)
                return;

            if (previousStage != null)
            {
                var previousKey = StageUtility.CreateWindowAndStageIdentifier(m_SceneView.windowGUID, previousStage);
                m_SearchStateCache.SetState(previousKey, new SceneViewSearchState
                {
                    searchFilter = m_SceneView.m_SearchFilter,
                    searchMode = m_SceneView.m_SearchMode,
                });
            }

            // A newly opened stage starts with an empty search, only returning to a stage restores its search
            SceneViewSearchState state = null;
            if (newStage.setSelectionAndScrollWhenBecomingCurrentStage)
                state = m_SearchStateCache.GetState(StageUtility.CreateWindowAndStageIdentifier(m_SceneView.windowGUID, newStage));

            var searchFilter = state?.searchFilter ?? string.Empty;
            var searchMode = state?.searchMode ?? SearchableEditorWindow.SearchMode.All;
            m_SceneView.SetSearchFilter(searchFilter, searchMode, false, false);
        }

        void SaveCameraState(SceneView sceneView, Stage stage)
        {
            if (stage == null)
                return;

            // Allows stage to override which stage should be used for saving the state.
            // Useful for Prefab Mode in Context where we want to save the context scene state.
            Stage contextStage = stage.GetContextStage();

            if (contextStage == null)
                return;

            Hash128 key = StageUtility.CreateWindowAndStageIdentifier(sceneView.windowGUID, contextStage);
            var state = m_StateCache.GetState(key);
            if (state == null)
                state = new SceneViewCameraState();
            state.SaveStateFromSceneView(sceneView);
            m_StateCache.SetState(key, state);
        }

        SceneViewCameraState GetStoredCameraState(SceneView sceneView, Stage stage)
        {
            Hash128 key = StageUtility.CreateWindowAndStageIdentifier(sceneView.windowGUID, stage);
            return m_StateCache.GetState(key);
        }

        void LoadCameraState(SceneView sceneView, Stage stage)
        {
            if (stage == null)
                return;

            // Allows stage to override which stage should be used for saving the state.
            // Useful for Prefab Mode in Context where we want to save the context scene state.
            Stage contextStage = stage.GetContextStage();

            if (contextStage == null)
                return;

            var state = GetStoredCameraState(sceneView, contextStage);
            if (state != null)
                state.RestoreStateToSceneView(sceneView);
        }

        public void BreadcrumbGUI()
        {
            float breadcrumbsHeight = 17f;
            float verticalOffset = Mathf.Floor((BreadcrumbBar.DefaultStyles.background.fixedHeight - breadcrumbsHeight) / 2f);
            SetupBreadCrumbBarIfNeeded();
            using (new GUILayout.VerticalScope(BreadcrumbBar.DefaultStyles.background))
            {
                GUILayout.Space(verticalOffset - 1);
                using (new EditorGUILayout.HorizontalScope(GUILayout.Height(breadcrumbsHeight)))
                {
                    m_BreadcrumbBar.OnGUI();
                    GUILayout.Space(10f);
                    currentStage.OnControlsGUI(m_SceneView);
                }
            }
        }

        void SetupBreadCrumbBarIfNeeded()
        {
            if (m_BreadcrumbInitialized)
                return;

            var history = StageNavigationManager.instance.stageHistory;
            var crumbs = new List<BreadcrumbBar.Item>();
            foreach (var stage in history)
            {
                var breadcrumbItem = stage.CreateBreadcrumbItem();
                if (breadcrumbItem != null)
                {
                    breadcrumbItem.userdata = stage;
                    crumbs.Add(breadcrumbItem);
                }
            }
            m_BreadcrumbBar.SetBreadCrumbs(crumbs);
            m_BreadcrumbBar.onBreadCrumbClicked -= BreadCrumbItemClicked;
            m_BreadcrumbBar.onBreadCrumbClicked += BreadCrumbItemClicked;
            m_BreadcrumbInitialized = true;
        }

        void SwitchStageOnNextUpdate()
        {
            EditorApplication.update -= SwitchStageOnNextUpdate;
            var stageClicked = m_StageClickedFromBreadcrumb;
            m_StageClickedFromBreadcrumb = null;

            if (stageClicked != null && stageClicked.isValid)
                StageNavigationManager.instance.SwitchToStage(stageClicked, false, StageNavigationManager.Analytics.ChangeType.NavigateViaBreadcrumb);
        }

        void BreadCrumbItemClicked(BreadcrumbBar.Item item)
        {
            var stageClicked = (Stage)item.userdata;
            if (!stageClicked.isValid)
                return;

            if (StageNavigationManager.instance.currentStage == stageClicked)
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(stageClicked.assetPath));
            }
            else
            {
                // The BreadCrumbItemClicked event is called during the SceneView's OnGUI and if we switch stage directly we might delete the
                // target of the SceneView's activeEditors which the rest of the SceneView's OnGUI is not expecting.
                // E.g it will cause null ref exceptions in OnSceneGUI(). Instead we delay switching until the next Update.
                m_StageClickedFromBreadcrumb = stageClicked;
                EditorApplication.update += SwitchStageOnNextUpdate;
            }
        }
    }

    [Serializable]
    class SceneViewSearchState
    {
        public string searchFilter;
        public SearchableEditorWindow.SearchMode searchMode;
    }
}
