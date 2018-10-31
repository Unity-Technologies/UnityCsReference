// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;

namespace UnityEditor
{
    class SceneHierarchyStageHandling
    {
        static class Styles
        {
            public static GUIStyle prefabHeaderBg;
            public static GUIStyle leftArrow = "ArrowNavigationLeft";

            static Styles()
            {
                prefabHeaderBg = new GUIStyle("ProjectBrowserTopBarBg");
                prefabHeaderBg.fixedHeight = 0;
                prefabHeaderBg.border = new RectOffset(3, 3, 3, 3);
            }
        }

        SceneHierarchy m_SceneHierarchy;
        SceneHierarchyWindow m_SceneHierarchyWindow;
        StateCache<SceneHierarchyState> m_StateCache = new StateCache<SceneHierarchyState>("Library/StateCache/Hierarchy/");
        GUIContent m_PrefabHeaderContent;
        bool m_LastPrefabStageModifiedState;

        public SceneHierarchyStageHandling(SceneHierarchyWindow sceneHierarchyWindow)
        {
            m_SceneHierarchyWindow = sceneHierarchyWindow;
            m_SceneHierarchy = sceneHierarchyWindow.sceneHierarchy;
        }

        public void OnEnable()
        {
            StageNavigationManager.instance.stageChanged += OnStageChanged;
            StageNavigationManager.instance.prefabStageReloading += OnPrefabStageReloading;
            StageNavigationManager.instance.prefabStageReloaded += OnPrefabStageReloaded;
            StageNavigationManager.instance.prefabStageToBeDestroyed += OnPrefabStageBeingDestroyed;
            PrefabStage.prefabIconChanged += OnPrefabStageIconChanged;

            // To support expanded state of new unsaved GameObject in Prefab Mode across domain reloading we do not load the
            // last saved expanded state here but instead rely on the fact that the Hierarchy serializes its own expanded state already.
            SyncHierarchyToCurrentStage(StageNavigationManager.instance.currentItem, false);
        }

        public void OnDisable()
        {
            SaveHierarchyState(m_SceneHierarchyWindow, StageNavigationManager.instance.currentItem);

            StageNavigationManager.instance.stageChanged -= OnStageChanged;
            StageNavigationManager.instance.prefabStageReloading -= OnPrefabStageReloading;
            StageNavigationManager.instance.prefabStageReloaded -= OnPrefabStageReloaded;
            StageNavigationManager.instance.prefabStageToBeDestroyed -= OnPrefabStageBeingDestroyed;
            PrefabStage.prefabIconChanged -= OnPrefabStageIconChanged;
        }

        void OnPrefabStageBeingDestroyed(StageNavigationItem prefabStage)
        {
            SaveHierarchyState(m_SceneHierarchyWindow, prefabStage);
        }

        void OnPrefabStageReloading(StageNavigationItem prefabStage)
        {
            SaveHierarchyState(m_SceneHierarchyWindow, prefabStage); // Save hierarchy state so we can load it after the prefab have been reloaded (with new instanceIDs)
        }

        void OnPrefabStageReloaded(StageNavigationItem prefabStage)
        {
            LoadHierarchyState(m_SceneHierarchyWindow, prefabStage); // Load hierarchy state before reloading the tree so the correct rows are loaded
            m_SceneHierarchy.customParentForNewGameObjects = prefabStage.prefabStage.prefabContentsRoot.transform;
            m_SceneHierarchy.customScenes = new[] { prefabStage.prefabStage.scene}; // This will re-init the TreeView (new scenes to show)
        }

        void SyncHierarchyToCurrentStage(StageNavigationItem stage, bool loadExpandedState)
        {
            if (loadExpandedState)
                LoadHierarchyState(m_SceneHierarchyWindow, stage);

            if (stage.isMainStage)
            {
                m_SceneHierarchy.customParentForNewGameObjects = null;
                m_SceneHierarchy.SetCustomDragHandler(null);
                m_SceneHierarchy.customScenes = null;
            }
            else
            {
                m_SceneHierarchy.customParentForNewGameObjects = stage.prefabStage.prefabContentsRoot.transform;
                m_SceneHierarchy.SetCustomDragHandler(PrefabModeDraggingHandler);
                m_SceneHierarchy.customScenes = new[] { stage.prefabStage.scene};
                HandleFirstTimePrefabStageIsOpened(stage);
                m_SceneHierarchy.FrameObject(stage.prefabStage.prefabContentsRoot.GetInstanceID(), false);
            }
        }

        void HandleFirstTimePrefabStageIsOpened(StageNavigationItem stage)
        {
            if (stage.isPrefabStage && GetStoredHierarchyState(m_SceneHierarchyWindow, stage) == null)
            {
                SetDefaultExpandedStateForOpenedPrefab(stage.prefabStage.prefabContentsRoot);
            }
        }

        void SetDefaultExpandedStateForOpenedPrefab(GameObject root)
        {
            var expandedIDs = new List<int>();
            AddParentsBelowButIgnoreNestedPrefabsRecursive(root.transform, expandedIDs);
            expandedIDs.Sort();
            m_SceneHierarchy.treeViewState.expandedIDs = expandedIDs;
        }

        void AddParentsBelowButIgnoreNestedPrefabsRecursive(Transform transform, List<int> gameObjectInstanceIDs)
        {
            gameObjectInstanceIDs.Add(transform.gameObject.GetInstanceID());

            int count = transform.childCount;
            for (int i = 0; i < count; ++i)
            {
                var child = transform.GetChild(i);
                if (child.childCount > 0 && !PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                {
                    AddParentsBelowButIgnoreNestedPrefabsRecursive(child, gameObjectInstanceIDs);
                }
            }
        }

        static DragAndDropVisualMode PrefabModeDraggingHandler(GameObjectTreeViewItem parentItem, GameObjectTreeViewItem targetItem, TreeViewDragging.DropPosition dropPos, bool perform)
        {
            var stage = StageNavigationManager.instance.currentItem;
            if (!stage.isPrefabStage)
                throw new InvalidOperationException("PrefabModeDraggingHandler should only be called in Prefab Mode");

            // Disallow dropping as sibling to the prefab instance root (In Prefab Mode we only want to show one root).
            if (parentItem != null && parentItem.parent == null && dropPos != TreeViewDragging.DropPosition.Upon)
                return DragAndDropVisualMode.Rejected;

            // Disallow dragging scenes into the hierarchy when it is in Prefab Mode (we do not support multi-scenes for prefabs yet)
            foreach (var dragged in DragAndDrop.objectReferences)
            {
                if (dragged is SceneAsset)
                    return DragAndDropVisualMode.Rejected;
            }

            // Check for cyclic nesting (only on perform since it is an expensive operation)
            if (perform)
            {
                var prefabAssetThatIsAddedTo = AssetDatabase.LoadMainAssetAtPath(stage.prefabAssetPath);
                foreach (var dragged in DragAndDrop.objectReferences)
                {
                    if (dragged is GameObject && EditorUtility.IsPersistent(dragged))
                    {
                        var prefabAssetThatWillBeAdded = dragged;
                        if (PrefabUtility.CheckIfAddingPrefabWouldResultInCyclicNesting(prefabAssetThatIsAddedTo, prefabAssetThatWillBeAdded))
                        {
                            PrefabUtility.ShowCyclicNestingWarningDialog();
                            return DragAndDropVisualMode.Rejected;
                        }
                    }
                }
            }

            return DragAndDropVisualMode.None;
        }

        void OnStageChanged(StageNavigationItem previousStage, StageNavigationItem newStage)
        {
            if (previousStage.isMainStage)
                SaveHierarchyState(m_SceneHierarchyWindow, previousStage); // prefab stage is saved before it is destroyed
            var stage = StageNavigationManager.instance.currentItem;
            SyncHierarchyToCurrentStage(newStage, true);
            CachePrefabHeaderText(stage);

            if (m_SceneHierarchyWindow.hasSearchFilter)
                m_SceneHierarchyWindow.SetSearchFilter(string.Empty, m_SceneHierarchyWindow.searchMode, true);
        }

        void SaveHierarchyState(SceneHierarchyWindow hierarchyWindow, StageNavigationItem stage)
        {
            if (stage == null)
                return;
            string key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.windowGUID, stage);
            var state = m_StateCache.GetState(key);
            if (state == null)
                state = new SceneHierarchyState();
            state.SaveStateFromHierarchy(hierarchyWindow, stage);
            m_StateCache.SetState(key, state);
        }

        SceneHierarchyState GetStoredHierarchyState(SceneHierarchyWindow hierarchyWindow, StageNavigationItem stage)
        {
            string key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.windowGUID, stage);
            return m_StateCache.GetState(key);
        }

        void LoadHierarchyState(SceneHierarchyWindow hierarchy, StageNavigationItem stage)
        {
            if (stage == null)
                return;

            var state = GetStoredHierarchyState(hierarchy, stage);
            if (state != null)
                state.LoadStateIntoHierarchy(hierarchy, stage);
        }

        void OnPrefabStageIconChanged(PrefabStage prefabStage)
        {
            if (m_PrefabHeaderContent != null)
                m_PrefabHeaderContent.image = prefabStage.prefabFileIcon;
        }

        void CachePrefabHeaderText(StageNavigationItem stage)
        {
            if (!stage.isPrefabStage)
                return;

            var prefabStage = stage.prefabStage;
            if (prefabStage == null)
                return;

            var prefabAssetPath = prefabStage.prefabAssetPath;

            m_PrefabHeaderContent = new GUIContent();
            m_PrefabHeaderContent.text = System.IO.Path.GetFileNameWithoutExtension(prefabAssetPath);

            // Make room for version control overlay icons.
            // GUIStyles don't allow controlling the space between icon and text.
            // We could add spacing by splitting text and icon into two rects and two draw operations,
            // but just adding a space character is a lot simpler and ends up amounting to the same thing.
            // This is cached text so there is minimal overhead.
            if (VersionControl.Provider.isActive)
                m_PrefabHeaderContent.text = " " + m_PrefabHeaderContent.text;

            PrefabUtility.GetPrefabAssetType(prefabStage.prefabContentsRoot);
            m_PrefabHeaderContent.image = prefabStage.prefabFileIcon;
            if (!stage.prefabAssetExists)
                m_PrefabHeaderContent.tooltip = L10n.Tr("Prefab asset has been deleted");

            if (PrefabStageUtility.GetCurrentPrefabStage().HasSceneBeenModified())
                m_PrefabHeaderContent.text += "*";
        }

        public void PrefabStageHeaderGUI(Rect rect)
        {
            var currentItem = StageNavigationManager.instance.currentItem;
            if (currentItem.isMainStage)
            {
                Debug.LogError("Not a Prefab scene");
                return;
            }

            var prefabStage = currentItem.prefabStage;
            if (prefabStage == null)
                return;

            // Cache header text
            if (m_PrefabHeaderContent == null || m_LastPrefabStageModifiedState == prefabStage.HasSceneBeenModified())
                CachePrefabHeaderText(currentItem);
            m_LastPrefabStageModifiedState = prefabStage.HasSceneBeenModified();

            // Background
            GUI.Label(rect, GUIContent.none, Styles.prefabHeaderBg);

            // Back button
            if (Event.current.type == EventType.Repaint)
            {
                Rect renderRect = new Rect(
                    rect.x + Styles.leftArrow.margin.left,
                    rect.y + (rect.height - Styles.leftArrow.fixedHeight) / 2,
                    Styles.leftArrow.fixedWidth,
                    Styles.leftArrow.fixedHeight);
                Styles.leftArrow.Draw(renderRect, GUIContent.none, false, false, false, false);
            }
            Rect interactionRect = new Rect(
                rect.x,
                rect.y,
                Styles.leftArrow.fixedWidth + Styles.leftArrow.margin.horizontal,
                rect.height);
            if (GUI.Button(interactionRect, GUIContent.none, GUIStyle.none))
            {
                StageNavigationManager.instance.NavigateBack(StageNavigationManager.Analytics.ChangeType.NavigateBackViaHierarchyHeaderLeftArrow);
            }

            // Prefab icon and name
            EditorGUIUtility.SetIconSize(new Vector2(16, 16));
            float width = TreeViewGUI.Styles.lineBoldStyle.CalcSize(m_PrefabHeaderContent).x;
            float xStart = Styles.leftArrow.margin.left + Styles.leftArrow.fixedWidth;
            float space = rect.width;
            float offsetFromStart = Mathf.Max(xStart, (space - width) / 2);
            Rect labelRect = new Rect(offsetFromStart, rect.y + 2, rect.width - xStart, 20);
            if (GUI.Button(labelRect, m_PrefabHeaderContent, currentItem.valid ? BreadcrumbBar.DefaultStyles.labelBold : BreadcrumbBar.DefaultStyles.labelBoldMissing))
                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(prefabStage.prefabAssetPath));
            EditorGUIUtility.SetIconSize(Vector2.zero);

            // Version control overlay icons
            if (VersionControl.Provider.isActive)
            {
                Rect overlayRect = labelRect;
                overlayRect.width = 16;
                overlayRect.y += (overlayRect.height - 16) / 2;
                overlayRect.height = 16;
                AssetsTreeViewGUI.OnIconOverlayGUI(AssetDatabase.LoadMainAssetAtPath(currentItem.prefabAssetPath).GetInstanceID(), overlayRect, true);
            }
        }
    }
}
