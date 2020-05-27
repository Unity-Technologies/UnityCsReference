// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;

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
                m_SceneHierarchyWindow.SetSearchFilter(string.Empty, m_SceneHierarchyWindow.searchMode, true);
        }

        internal void CacheStageHeaderContent()
        {
            Stage stage = currentStage;
            if (stage == null || !stage.isValid)
                return;

            m_StageHeaderContent = stage.CreateHeaderContent();

            // Make room for version control overlay icons.
            // GUIStyles don't allow controlling the space between icon and text.
            // We could add spacing by splitting text and icon into two rects and two draw operations,
            // but just adding a space character is a lot simpler and ends up amounting to the same thing.
            // This is cached text so there is minimal overhead.
            if (VersionControl.Provider.isActive)
                m_StageHeaderContent.text = " " + m_StageHeaderContent.text;

            if (stage.hasUnsavedChanges)
                m_StageHeaderContent.text += "*";
        }

        public void StageHeaderGUI(Rect rect)
        {
            var stage = currentStage;
            if (stage == null || !stage.isValid)
                return;

            if (m_StageHeaderContent == GUIContent.none || m_LastStageUnsavedChangesState == stage.hasUnsavedChanges)
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

            // Icon and name
            EditorGUIUtility.SetIconSize(new Vector2(16, 16));
            float width = TreeViewGUI.Styles.lineBoldStyle.CalcSize(m_StageHeaderContent).x;
            float xStart = Styles.leftArrow.margin.left + Styles.leftArrow.fixedWidth;
            float space = rect.width;
            float offsetFromStart = Mathf.Max(xStart, (space - width) / 2);
            Rect labelRect = new Rect(offsetFromStart, rect.y + 2, rect.width - xStart, 20);
            if (GUI.Button(labelRect, m_StageHeaderContent, stage.isAssetMissing ? BreadcrumbBar.DefaultStyles.labelBoldMissing : BreadcrumbBar.DefaultStyles.labelBold))
                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(stage.assetPath));
            EditorGUIUtility.SetIconSize(Vector2.zero);

            // Version control overlay icons
            if (VersionControl.Provider.isActive && EditorUserSettings.hierarchyOverlayIcons)
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
