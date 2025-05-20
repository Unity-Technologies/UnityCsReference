// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using TreeViewGUI = UnityEditor.IMGUI.Controls.TreeViewGUI<int>;


namespace UnityEditor
{
    class SceneHierarchyStageHandling
    {
        static class Styles
        {
            public static GUIStyle stageHeaderBg;
            public static GUIStyle leftArrow = "ArrowNavigationLeft";

            static Styles()
            {
                stageHeaderBg = new GUIStyle("HeaderButton");
                stageHeaderBg.border = new RectOffset(3, 3, 3, 3);
            }
        }

        SceneHierarchyWindow m_SceneHierarchyWindow;

        GUIContent m_StageHeaderContent;
        bool m_LastStageUnsavedChangesState;

        Stage currentStage { get { return StageNavigationManager.instance.currentStage; } }

        public SceneHierarchyStageHandling(SceneHierarchyWindow sceneHierarchyWindow)
        {
            m_SceneHierarchyWindow = sceneHierarchyWindow;
        }

        public void OnEnable()
        {
            StageNavigationManager.instance.stageChanged += OnStageChanged;
            StageNavigationManager.instance.beforeSwitchingAwayFromStage += OnBeforeSwitchingAwayFromStage;

            // To support expanded state of new unsaved GameObject in stages across domain reloading we do not load the
            // last saved expanded state here but instead rely on the fact that the Hierarchy serializes its own expanded state already.
            currentStage.SyncSceneHierarchyToStage(m_SceneHierarchyWindow);
        }

        public void OnDisable()
        {
            StageNavigationManager.instance.currentStage.SaveHierarchyState(m_SceneHierarchyWindow);

            StageNavigationManager.instance.stageChanged -= OnStageChanged;
            StageNavigationManager.instance.beforeSwitchingAwayFromStage -= OnBeforeSwitchingAwayFromStage;
        }

        void OnBeforeSwitchingAwayFromStage(Stage stage)
        {
            // Clear parent object for the prefab stage if one was set
            if (stage is PrefabStage)
                SceneHierarchy.SetDefaultParentForSession(stage.GetSceneAt(stage.sceneCount - 1).guid, 0);

            stage.SaveHierarchyState(m_SceneHierarchyWindow);
        }

        void OnStageChanged(Stage previousStage, Stage newStage)
        {
            if (previousStage is MainStage)
                previousStage.SaveHierarchyState(m_SceneHierarchyWindow); // Non-main stages are saved before they are destroyed
            newStage.SyncSceneHierarchyToStage(m_SceneHierarchyWindow);

            newStage.LoadHierarchyState(m_SceneHierarchyWindow);

            m_StageHeaderContent = GUIContent.none; // Stage header content is being rebuild on demand in a OnGUI code path (required since it uses EditorStyles)

            if (m_SceneHierarchyWindow.hasSearchFilter)
            {
                m_SceneHierarchyWindow.SetSearchFilter(string.Empty, m_SceneHierarchyWindow.searchMode, true);
                m_SceneHierarchyWindow.UnfocusSearchField();
            }
        }

        internal void CacheStageHeaderContent()
        {
            Stage stage = currentStage;
            if (stage == null || !stage.isValid)
            {
                m_StageHeaderContent = GUIContent.none;
                return;
            }

            m_StageHeaderContent = stage.CreateHeaderContent();
            if (m_StageHeaderContent == null)
                m_StageHeaderContent = new GUIContent(stage.GetType().Name);

            // Make room for version control overlay icons.
            // GUIStyles don't allow controlling the space between icon and text.
            // We could add spacing by splitting text and icon into two rects and two draw operations,
            // but just adding a space character is a lot simpler and ends up amounting to the same thing.
            // This is cached text so there is minimal overhead.
            if (VersionControlUtils.isVersionControlConnected)
                m_StageHeaderContent.text = " " + m_StageHeaderContent.text;

            if (stage.hasUnsavedChanges)
                m_StageHeaderContent.text += "*";
        }

        static void ShowStageContextMenu(Stage stage)
        {
            GenericMenu menu = new GenericMenu();
            stage.BuildContextMenuForStageHeader(menu);
            if (menu.menuItems.Count > 0)
                menu.ShowAsContext();
        }

        public void StageHeaderGUI(Rect rect)
        {
            var stage = currentStage;
            if (stage == null || !stage.isValid)
                return;

            if (m_StageHeaderContent == null || m_StageHeaderContent == GUIContent.none || m_LastStageUnsavedChangesState == stage.hasUnsavedChanges)
                CacheStageHeaderContent();
            m_LastStageUnsavedChangesState = stage.hasUnsavedChanges;

            // Background
            GUI.Box(rect, GUIContent.none, Styles.stageHeaderBg);

            Rect interactionRect = new Rect(
                rect.x,
                rect.y,
                Styles.leftArrow.fixedWidth + Styles.leftArrow.margin.horizontal,
                rect.height - 1); /*bottom borer*/

            // Back button
            if (Event.current.type == EventType.Repaint)
            {
                // Resets the fixed size to stretch the button
                float oldW = Styles.leftArrow.fixedWidth, oldH = Styles.leftArrow.fixedHeight;

                Styles.leftArrow.fixedWidth = 0; Styles.leftArrow.fixedHeight = 0;
                Styles.leftArrow.Draw(interactionRect, GUIContent.none, interactionRect.Contains(Event.current.mousePosition), false, false, false);
                Styles.leftArrow.fixedWidth = oldW; Styles.leftArrow.fixedHeight = oldH;
            }

            if (GUI.Button(interactionRect, GUIContent.none, GUIStyle.none))
            {
                StageNavigationManager.instance.NavigateBack(StageNavigationManager.Analytics.ChangeType.NavigateBackViaHierarchyHeaderLeftArrow);
            }

            // Options button
            Rect optionsButtonRect = new Rect();
            if (stage.showOptionsButton)
            {
                if (GameObjectTreeViewGUI.DoOptionsButton(rect, out optionsButtonRect))
                {
                    ShowStageContextMenu(stage);
                }
            }

            // Icon and name (and context click on background)
            EditorGUIUtility.SetIconSize(new Vector2(16, 16));
            float contentWidth = TreeViewGUI.Styles.lineBoldStyle.CalcSize(m_StageHeaderContent).x;
            float xStart = Styles.leftArrow.margin.left + Styles.leftArrow.fixedWidth;
            float space = (optionsButtonRect.width > 0 ? optionsButtonRect.xMin : rect.width) - xStart;
            float offsetFromStart = xStart + Mathf.Max(0, (space - contentWidth) / 2);
            Rect labelRect = new Rect(offsetFromStart, rect.y + 2, space, 20);

            if (GUI.Button(labelRect, m_StageHeaderContent, stage.isAssetMissing ? BreadcrumbBar.DefaultStyles.labelBoldMissing : BreadcrumbBar.DefaultStyles.labelBold))
            {
                Event evt = Event.current;
                if (evt.button == 0)
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(stage.assetPath));
                }
                else if (evt.button == 1)
                {
                    ShowStageContextMenu(stage);
                }
            }
            EditorGUIUtility.SetIconSize(Vector2.zero);

            // Version control overlay icons
            if (VersionControlUtils.isVersionControlConnected && EditorUserSettings.hierarchyOverlayIcons)
            {
                Rect overlayRect = labelRect;
                overlayRect.width = 16;
                overlayRect.y += (overlayRect.height - 16) / 2;
                overlayRect.height = 16;

                // The source asset could have been deleted while open inside the stage so the library object might be null here (case 1086613)
                var asset = AssetDatabase.LoadMainAssetAtPath(stage.assetPath);
                if (asset != null)
                    AssetsTreeViewGUI.OnIconOverlayGUI(asset.GetInstanceID(), overlayRect, true);
            }
        }
    }
}
