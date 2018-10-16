// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    class SceneViewStageHandling
    {
        SceneView m_SceneView;
        StateCache<SceneViewCameraState> m_StateCache = new StateCache<SceneViewCameraState>("Library/StateCache/SceneView/");

        public BreadcrumbBar m_BreadcrumbBar = new BreadcrumbBar();
        public bool m_BreadcrumbInitialized;

        public bool isShowingBreadcrumbBar
        {
            get { return PrefabStageUtility.GetCurrentPrefabStage() != null; }
        }

        public float breadcrumbHeight { get { return BreadcrumbBar.DefaultStyles.background.fixedHeight; }}

        static bool autoSave
        {
            get { return StageNavigationManager.instance.autoSave; }
            set { StageNavigationManager.instance.autoSave = value; }
        }

        static class Styles
        {
            public static GUIContent autoSaveGUIContent = EditorGUIUtility.TrTextContent("Auto Save", "When Auto Save is enabled, every change you make is automatically saved to the Prefab Asset. Disable Auto Save if you experience long import times.");
            public static GUIContent saveButtonContent = EditorGUIUtility.TrTextContent("Save");
            public static GUIContent checkoutButtonContent = EditorGUIUtility.TrTextContent("Check Out");
            public static GUIContent autoSavingBadgeContent = EditorGUIUtility.TrTextContent("Auto Saving...");
            public static GUIStyle saveToggle;
            public static GUIStyle button;
            public static GUIStyle savingBadge = "Badge";

            static Styles()
            {
                saveToggle = new GUIStyle(EditorStyles.toggle);
                saveToggle.font = EditorStyles.miniLabel.font;
                saveToggle.fontSize = EditorStyles.miniLabel.fontSize;
                saveToggle.fontStyle = EditorStyles.miniLabel.fontStyle;
                saveToggle.alignment = TextAnchor.MiddleLeft;
                saveToggle.padding.top = 3;
                saveToggle.padding.bottom = 0;
                saveToggle.margin.top = 0;
                saveToggle.margin.bottom = 2;

                button = new GUIStyle(EditorStyles.miniButton);
                button.padding.top = 3;
                button.margin.top = button.margin.bottom = 0;
            }
        }

        public SceneViewStageHandling(SceneView sceneView)
        {
            m_SceneView = sceneView;
        }

        public void OnEnable()
        {
            StageNavigationManager.instance.stageChanged += OnStageChanged;
            StageNavigationManager.instance.prefabStageReloaded += OnPrefabStageReloaded;
            StageNavigationManager.instance.prefabStageDirtinessChanged += OnPrefabStageDirtinessChanged;
            AssetEvents.assetsChangedOnHDD += AssetsChangedOnHDD;
            PrefabStage.prefabIconChanged += OnPrefabStageIconChanged;

            // We need to sync to the current stage to ensure we have the correct SceneView settings: We could have closed Unity while a prefab scene was open
            // this means that the SceneView settings for that prefab is saved to the window layout. Opening Unity always opens the main scenes so here we ensure
            // we have e.g the corrct sky box and other settings.
            SyncToCurrentStage();
        }

        public void OnDisable()
        {
            // Ensure saving current stage settings so we can reconstruct them on OnEnable
            SaveCameraState(m_SceneView, StageNavigationManager.instance.currentItem);

            StageNavigationManager.instance.stageChanged -= OnStageChanged;
            StageNavigationManager.instance.prefabStageReloaded -= OnPrefabStageReloaded;
            StageNavigationManager.instance.prefabStageDirtinessChanged -= OnPrefabStageDirtinessChanged;
            AssetEvents.assetsChangedOnHDD -= AssetsChangedOnHDD;
            PrefabStage.prefabIconChanged -= OnPrefabStageIconChanged;
        }

        void OnPrefabStageIconChanged(PrefabStage prefabStage)
        {
            m_BreadcrumbInitialized = false;
        }

        void AssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            m_BreadcrumbInitialized = false;
        }

        void OnPrefabStageDirtinessChanged(PrefabStage prefabStage)
        {
            if (m_SceneView.customScene == prefabStage.scene)
                m_SceneView.Repaint();
        }

        void OnPrefabStageReloaded(StageNavigationItem prefabStage)
        {
            m_SceneView.customScene = prefabStage.prefabStage.scene;
            m_SceneView.customParentForDraggedObjects = prefabStage.prefabStage.prefabContentsRoot.transform;
        }

        void OnStageChanged(StageNavigationItem previousStage, StageNavigationItem newStage)
        {
            SaveCameraState(m_SceneView, previousStage);
            SyncToCurrentStage();
            m_BreadcrumbInitialized = false;
            m_SceneView.OnStageChanged(previousStage, newStage);
        }

        void SyncToCurrentStage()
        {
            StageNavigationItem stage = StageNavigationManager.instance.currentItem;
            if (stage.isMainStage)
            {
                m_SceneView.customScene = new Scene();
                m_SceneView.customParentForDraggedObjects = null;
            }
            else
            {
                m_SceneView.customScene = PrefabStageUtility.GetCurrentPrefabStage().scene;
                m_SceneView.customParentForDraggedObjects = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot.transform;
            }

            LoadCameraState(m_SceneView, stage);
            HandleFirstTimePrefabStageIsOpened(stage);
        }

        static bool HasAnyActiveLights(Scene scene)
        {
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.GetComponentsInChildren<Light>(false).Length > 0)
                    return true;
            }

            return false;
        }

        void HandleFirstTimePrefabStageIsOpened(StageNavigationItem stage)
        {
            if (stage.isPrefabStage && GetStoredCameraState(m_SceneView, stage) == null)
            {
                // Default to scene view lighting if scene itself does not have any lights
                if (!HasAnyActiveLights(stage.prefabStage.scene))
                    m_SceneView.sceneLighting = false;

                // For UI to frame properly we need to delay one full Update for the layouting to have been processed
                EditorApplication.update += DelayedFraming;
            }
        }

        int m_DelayCounter;
        void DelayedFraming()
        {
            if (m_DelayCounter++ == 1)
            {
                EditorApplication.update -= DelayedFraming;
                m_DelayCounter = 0;

                // Frame the prefab if still available
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                {
                    Selection.activeGameObject = prefabStage.prefabContentsRoot;
                    m_SceneView.FrameSelected(false, true);
                }
            }
        }

        void SaveCameraState(SceneView sceneView, StageNavigationItem stage)
        {
            if (stage == null)
                return;
            string key = StageUtility.CreateWindowAndStageIdentifier(sceneView.windowGUID, stage);
            var state = m_StateCache.GetState(key);
            if (state == null)
                state = new SceneViewCameraState();
            state.SaveStateFromSceneView(sceneView);
            m_StateCache.SetState(key, state);
        }

        SceneViewCameraState GetStoredCameraState(SceneView sceneView, StageNavigationItem stage)
        {
            string key = StageUtility.CreateWindowAndStageIdentifier(sceneView.windowGUID, stage);
            return m_StateCache.GetState(key);
        }

        void LoadCameraState(SceneView sceneView, StageNavigationItem stage)
        {
            if (stage == null)
                return;

            var state = GetStoredCameraState(sceneView, stage);
            if (state != null)
                state.RestoreStateToSceneView(sceneView);
        }

        public void BreadcrumbGUI()
        {
            float breadcrumbsHeight = 18f;
            float verticalOffset = Mathf.Floor((BreadcrumbBar.DefaultStyles.background.fixedHeight - breadcrumbsHeight) / 2f);
            SetupBreadCrumbBarIfNeeded();
            using (new GUILayout.VerticalScope(BreadcrumbBar.DefaultStyles.background))
            {
                GUILayout.Space(verticalOffset);
                using (new EditorGUILayout.HorizontalScope(GUILayout.Height(breadcrumbsHeight)))
                {
                    m_BreadcrumbBar.OnGUI();
                    AutoSaveButtons();
                }
            }
        }

        void AutoSaveButtons()
        {
            StageNavigationItem item = StageNavigationManager.instance.currentItem;
            if (item.isPrefabStage)
            {
                StatusQueryOptions opts = EditorUserSettings.allowAsyncStatusUpdate ? StatusQueryOptions.UseCachedAsync : StatusQueryOptions.UseCachedIfPossible;
                bool openForEdit = AssetDatabase.IsOpenForEdit(item.prefabAssetPath, opts);

                PrefabStage stage = item.prefabStage;
                if (stage.showingSavingLabel)
                {
                    GUILayout.Label(Styles.autoSavingBadgeContent, Styles.savingBadge);
                    GUILayout.Space(4);
                }

                if (!stage.autoSave)
                {
                    using (new EditorGUI.DisabledScope(!openForEdit || !PrefabStageUtility.GetCurrentPrefabStage().HasSceneBeenModified()))
                    {
                        if (GUILayout.Button(Styles.saveButtonContent, Styles.button))
                            PrefabStageUtility.GetCurrentPrefabStage().SavePrefabWithVersionControlDialogAndRenameDialog();
                    }
                }

                using (new EditorGUI.DisabledScope(stage.temporarilyDisableAutoSave))
                {
                    bool autoSaveForScene = stage.autoSave;
                    EditorGUI.BeginChangeCheck();
                    autoSaveForScene = GUILayout.Toggle(autoSaveForScene, Styles.autoSaveGUIContent, Styles.saveToggle);
                    if (EditorGUI.EndChangeCheck())
                        stage.autoSave = autoSaveForScene;
                }

                if (!openForEdit)
                {
                    if (GUILayout.Button(Styles.checkoutButtonContent, Styles.button))
                    {
                        Task task = Provider.Checkout(AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabAssetPath), CheckoutMode.Both);
                        task.Wait();
                    }
                }
            }
        }

        void SetupBreadCrumbBarIfNeeded()
        {
            if (m_BreadcrumbInitialized)
                return;

            var history = StageNavigationManager.instance.stageHistory;
            var crumbs = new List<BreadcrumbBar.Item>();
            Texture sceneIcon = EditorGUIUtility.FindTexture("UnityEditor/SceneAsset Icon");
            foreach (var stage in history)
            {
                bool isLastCrumb = stage == history.Last();
                var label = stage.displayName;
                var icon = sceneIcon;
                var style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBold : BreadcrumbBar.DefaultStyles.label;
                var tooltip = "";
                if (stage.isPrefabStage)
                {
                    icon = isLastCrumb ? PrefabStageUtility.GetCurrentPrefabStage().prefabFileIcon : stage.prefabIcon;
                    if (!stage.prefabAssetExists)
                    {
                        style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBoldMissing : BreadcrumbBar.DefaultStyles.labelMissing;
                        tooltip = L10n.Tr("Prefab asset has been deleted");
                    }
                }

                crumbs.Add(new BreadcrumbBar.Item { content = new GUIContent(label, icon, tooltip), guistyle = style, userdata = stage });
            }
            m_BreadcrumbBar.SetBreadCrumbs(crumbs);
            m_BreadcrumbBar.onBreadCrumbClicked -= BreadCrumbItemClicked;
            m_BreadcrumbBar.onBreadCrumbClicked += BreadCrumbItemClicked;
            m_BreadcrumbInitialized = true;
        }

        static void BreadCrumbItemClicked(BreadcrumbBar.Item item)
        {
            var stageClicked = (StageNavigationItem)item.userdata;
            if (!stageClicked.valid)
                return;

            if (StageNavigationManager.instance.currentItem == stageClicked)
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(stageClicked.prefabAssetPath));
            }
            else
            {
                StageNavigationManager.instance.SwitchToStage(stageClicked, false, true, StageNavigationManager.Analytics.ChangeType.NavigateViaBreadcrumb);
            }
        }
    }
}
