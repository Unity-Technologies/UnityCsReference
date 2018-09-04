// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    internal class GameObjectTreeViewGUI : TreeViewGUI
    {
        enum GameObjectColorType
        {
            Normal = 0,
            Prefab = 1,
            BrokenPrefab = 2,
            Count = 3,
        }

        internal static class GameObjectStyles
        {
            public static GUIStyle disabledLabel = "PR DisabledLabel";
            public static GUIStyle prefabLabel = "PR PrefabLabel";
            public static GUIStyle disabledPrefabLabel = "PR DisabledPrefabLabel";
            public static GUIStyle brokenPrefabLabel = "PR BrokenPrefabLabel";
            public static GUIStyle disabledBrokenPrefabLabel = "PR DisabledBrokenPrefabLabel";
            public static GUIContent loadSceneGUIContent = new GUIContent(EditorGUIUtility.FindTexture("SceneLoadIn"), "Load scene");
            public static GUIContent unloadSceneGUIContent = new GUIContent(EditorGUIUtility.FindTexture("SceneLoadOut"), "Unload scene");
            public static GUIContent saveSceneGUIContent = new GUIContent(EditorGUIUtility.FindTexture("SceneSave"), "Save scene");
            public static GUIStyle optionsButtonStyle = "PaneOptions";
            public static GUIStyle sceneHeaderBg = "ProjectBrowserTopBarBg";
            public static GUIStyle rightArrow = "ArrowNavigationRight";
            public static GUIStyle hoveredItemBackgroundStyle = "WhiteBackground";
            public static Color hoveredBackgroundColor =
                EditorResources.GetStyle("game-object-tree-view").GetColor("-unity-object-tree-hovered-color");

            public static Texture2D sceneAssetIcon = EditorGUIUtility.FindTexture(typeof(SceneAsset));
            public static Texture2D prefabIcon = EditorGUIUtility.FindTexture("Prefab Icon");

            public static readonly int kSceneHeaderIconsInterval = 2;
        }

        private float m_PrevScollPos;
        private float m_PrevTotalHeight;
        internal delegate void OnHeaderGUIDelegate(Rect availableRect, string scenePath);
        internal static OnHeaderGUIDelegate OnPostHeaderGUI = null;

        public GameObjectTreeViewGUI(TreeViewController treeView, bool useHorizontalScroll)
            : base(treeView, useHorizontalScroll)
        {
            k_TopRowMargin = 0f;
            m_TreeView.enableItemHovering = true;
        }

        public override void OnInitialize()
        {
            m_PrevScollPos = m_TreeView.state.scrollPos.y;
            m_PrevTotalHeight = m_TreeView.GetTotalRect().height;
        }

        public bool DetectUserInput()
        {
            if (DetectScrollChange())
                return true;
            if (DetectTotalRectChange())
                return true;
            if (DetectMouseDownInTreeViewRect())
                return true;

            return false;
        }

        bool DetectScrollChange()
        {
            bool changed = false;
            float curScroll = m_TreeView.state.scrollPos.y;
            if (!Mathf.Approximately(curScroll, m_PrevScollPos))
                changed = true;
            m_PrevScollPos = curScroll;
            return changed;
        }

        bool DetectTotalRectChange()
        {
            bool changed = false;
            float curHeight = m_TreeView.GetTotalRect().height;
            if (!Mathf.Approximately(curHeight, m_PrevTotalHeight))
                changed = true;
            m_PrevTotalHeight = curHeight;
            return changed;
        }

        bool DetectMouseDownInTreeViewRect()
        {
            var evt = Event.current;
            var mouseEvent = evt.type == EventType.MouseDown || evt.type == EventType.MouseUp;
            var keyboardEvent = evt.type == EventType.KeyDown || evt.type == EventType.KeyUp;
            if ((mouseEvent && m_TreeView.GetTotalRect().Contains(evt.mousePosition)) || keyboardEvent)
                return true;
            return false;
        }

        bool showingStickyHeaders { get { return SceneManager.sceneCount > 1; } }

        void DoStickySceneHeaders()
        {
            int firstRow, lastRow;
            GetFirstAndLastRowVisible(out firstRow, out lastRow);
            if (firstRow >= 0 && lastRow >= 0)
            {
                float scrollY = m_TreeView.state.scrollPos.y;
                if (firstRow == 0 && scrollY <= topRowMargin)
                    return; // Do nothing when first row is 0 since we do not need any sticky headers overlay

                var firstItem = (GameObjectTreeViewItem)m_TreeView.data.GetItem(firstRow);
                var nextItem = (GameObjectTreeViewItem)m_TreeView.data.GetItem(firstRow + 1);
                bool isFirstItemLastInScene = firstItem.scene != nextItem.scene;
                float rowWidth = GUIClip.visibleRect.width;
                Rect rect = GetRowRect(firstRow, rowWidth);

                // Do not do the sticky header if the scene is at top
                if (firstItem.isSceneHeader && Mathf.Approximately(scrollY, rect.y))
                    return;

                // Sticky header is achieved by ensuring the header never moves out of
                // scroll and is aligned with last item in scene list if needed
                if (!isFirstItemLastInScene)
                    rect.y = scrollY;

                var sceneHeaderItem = ((GameObjectTreeViewDataSource)m_TreeView.data).sceneHeaderItems.FirstOrDefault(p => p.scene == firstItem.scene);
                if (sceneHeaderItem != null)
                {
                    bool selected = m_TreeView.IsItemDragSelectedOrSelected(sceneHeaderItem);
                    bool focused = m_TreeView.HasFocus();
                    bool boldFont = sceneHeaderItem.scene == SceneManager.GetActiveScene();
                    DoItemGUI(rect, firstRow, sceneHeaderItem, selected, focused, boldFont);

                    // Frame the actual scene header row (by clicking left of scene icon)
                    if (GUI.Button(new Rect(rect.x, rect.y, rect.height, rect.height), GUIContent.none, GUIStyle.none))
                        m_TreeView.Frame(sceneHeaderItem.id, true, false);

                    m_TreeView.HandleUnusedMouseEventsForItem(rect, sceneHeaderItem, firstRow);

                    HandleStickyHeaderContextClick(rect, sceneHeaderItem);
                }
            }
        }

        void HandleStickyHeaderContextClick(Rect rect, GameObjectTreeViewItem sceneHeaderItem)
        {
            Event evt = Event.current;

            // On OSX manually handle context click for sticky headers here to prevent items beneath the sticky header to also handle the event
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                bool showContextMenu = (evt.type == EventType.MouseDown && evt.button == 1) || evt.type == EventType.ContextClick; // cmd+left click fires a context click event
                if (showContextMenu && rect.Contains(Event.current.mousePosition))
                {
                    evt.Use();
                    m_TreeView.contextClickItemCallback(sceneHeaderItem.id);
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                if (evt.type == EventType.MouseDown && evt.button == 1 && rect.Contains(Event.current.mousePosition))
                {
                    // On Windows prevent right mouse down to propagate to items beneath (which will select items below this item)
                    evt.Use();
                }
            }
        }

        public override void BeginRowGUI()
        {
            if (DetectUserInput())
            {
                var data = (GameObjectTreeViewDataSource)m_TreeView.data;
                data.EnsureFullyInitialized();
            }

            base.BeginRowGUI();

            // Sticky scene headers. Do non repaint events first to receive input first
            if (showingStickyHeaders && Event.current.type != EventType.Repaint)
            {
                DoStickySceneHeaders();
            }
        }

        public override void EndRowGUI()
        {
            base.EndRowGUI();

            // Sticky scene headers. Do repaint events last to render on top.
            if (showingStickyHeaders && Event.current.type == EventType.Repaint)
            {
                DoStickySceneHeaders();
            }
        }

        public override Rect GetRectForFraming(int row)
        {
            Rect rect = base.GetRectForFraming(row);
            if (showingStickyHeaders && row < m_TreeView.data.rowCount)
            {
                var item = m_TreeView.data.GetItem(row) as GameObjectTreeViewItem;
                if (item != null && !item.isSceneHeader)
                {
                    // For game objects under a sceneheader we create a larger frame rect to ensure row is shown
                    // beneath the sticky header (headers have same lineheight as rows)
                    rect.y -= k_LineHeight;
                    rect.height = 2 * k_LineHeight;
                }
            }

            return rect;
        }

        //-------------------
        // Create and Rename GameObject section

        override public bool BeginRename(TreeViewItem item, float delay)
        {
            GameObjectTreeViewItem goItem = item as GameObjectTreeViewItem;
            if (goItem == null)
                return false;
            if (goItem.isSceneHeader)
                return false;

            Object target = goItem.objectPPTR;
            if ((target.hideFlags & HideFlags.NotEditable) != 0)
            {
                Debug.LogWarning("Unable to rename a GameObject with HideFlags.NotEditable.");
                return false;
            }

            return base.BeginRename(item, delay);
        }

        override protected void RenameEnded()
        {
            string name = string.IsNullOrEmpty(GetRenameOverlay().name) ? GetRenameOverlay().originalName : GetRenameOverlay().name;
            int instanceID = GetRenameOverlay().userData;
            bool userAccepted = GetRenameOverlay().userAcceptedRename;

            if (userAccepted)
            {
                ObjectNames.SetNameSmartWithInstanceID(instanceID, name);

                // Manually set the name so no visual pop happens
                TreeViewItem item = m_TreeView.data.FindItem(instanceID);

                if (item != null)
                    item.displayName = name;

                EditorApplication.RepaintAnimationWindow();
            }
        }

        override protected void DrawItemBackground(Rect rect, int row, TreeViewItem item, bool selected, bool focused)
        {
            base.DrawItemBackground(rect, row, item, selected, focused);
            if (m_TreeView.hoveredItem != item)
                return;
            var color = GUI.backgroundColor;
            GUI.backgroundColor = GameObjectStyles.hoveredBackgroundColor;
            GUI.Label(rect, GUIContent.none, GameObjectStyles.hoveredItemBackgroundStyle);
            GUI.backgroundColor = color;
        }

        override protected void DoItemGUI(Rect rect, int row, TreeViewItem item, bool selected, bool focused, bool useBoldFont)
        {
            GameObjectTreeViewItem goItem = item as GameObjectTreeViewItem;
            if (goItem == null)
                return;

            EnsureLazyInitialization(goItem);

            // Scene header background (make it slightly transparent to hint it
            // is not the normal scene header)
            if (goItem.isSceneHeader)
            {
                Color oldColor = GUI.color;
                GUI.color = GUI.color * new Color(1, 1, 1, 0.9f);
                GUI.Label(rect, GUIContent.none, GameObjectStyles.sceneHeaderBg);
                GUI.color = oldColor;

                useBoldFont = (goItem.scene == SceneManager.GetActiveScene()) || IsPrefabStageHeader(goItem);
            }

            base.DoItemGUI(rect, row, item, selected, focused, useBoldFont);


            if (goItem.isSceneHeader)
                DoAdditionalSceneHeaderGUI(goItem, rect);
            else
                PrefabModeButton(goItem, rect);

            if (SceneHierarchy.s_Debug)
                GUI.Label(new Rect(rect.xMax - 70, rect.y, 70, rect.height), "" + row + " (" + goItem.id + ")", EditorStyles.boldLabel);
        }

        protected void DoAdditionalSceneHeaderGUI(GameObjectTreeViewItem goItem, Rect rect)
        {
            // Options button
            const float optionsButtonWidth = 16f;
            const float optionsButtonHeight = 6f;
            const float margin = 4f;
            Rect buttonRect = new Rect(rect.width - optionsButtonWidth - margin, rect.y + (rect.height - optionsButtonHeight) * 0.5f, optionsButtonWidth, rect.height);
            if (Event.current.type == EventType.Repaint)
                GameObjectStyles.optionsButtonStyle.Draw(buttonRect, false, false, false, false);

            // We want larger click area than the button icon
            buttonRect.y = rect.y;
            buttonRect.height = rect.height;
            buttonRect.width = 24f;
            if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Passive,  GUIStyle.none))
            {
                // Ensure item is selected before using context menu (menu logic is based on selection)
                m_TreeView.SelectionClick(goItem, true);
                m_TreeView.contextClickItemCallback(goItem.id);
            }

            if (null != OnPostHeaderGUI)
            {
                float optionsWidth = (rect.width - buttonRect.x);
                float width = (rect.width - optionsWidth - margin);
                float x = 0;
                float y = rect.y;
                float height = rect.height;
                Rect availableRect = new Rect(x, y, width, height);
                OnPostHeaderGUI(availableRect, goItem.scene.path);
            }
        }

        static bool IsPrefabStageHeader(GameObjectTreeViewItem item)
        {
            if (!item.isSceneHeader)
                return false;

            Scene scene = EditorSceneManager.GetSceneByHandle(item.id);
            if (!scene.IsValid())
                return false;

            return EditorSceneManager.IsPreviewScene(scene);
        }

        void EnsureLazyInitialization(GameObjectTreeViewItem item)
        {
            if (!item.lazyInitializationDone)
            {
                item.lazyInitializationDone = true;
                SetItemIcon(item);
                SetItemOverlayIcon(item);
                SetPrefabModeButtonVisibility(item);
            }
        }

        void SetItemIcon(GameObjectTreeViewItem item)
        {
            var go = item.objectPPTR as GameObject;
            if (go == null)
            {
                if (IsPrefabStageHeader(item))
                {
                    string prefabAssetPath = PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath;
                    item.icon = (Texture2D)AssetDatabase.GetCachedIcon(prefabAssetPath);
                }
                else
                {
                    item.icon = GameObjectStyles.sceneAssetIcon;
                }
            }
            else
            {
                item.icon = PrefabUtility.GetIconForGameObject(go);
            }
        }

        void SetItemOverlayIcon(GameObjectTreeViewItem item)
        {
            item.overlayIcon = null;

            var go = item.objectPPTR as GameObject;
            if (go == null)
                return;

            if (PrefabUtility.IsAddedGameObjectOverride(go))
                item.overlayIcon = EditorGUIUtility.LoadIcon("PrefabOverlayAdded Icon");
        }

        void SetPrefabModeButtonVisibility(GameObjectTreeViewItem item)
        {
            item.showPrefabModeButton = false;

            GameObject go = item.objectPPTR as GameObject;

            if (go == null)
                return;

            if (!PrefabUtility.IsPartOfAnyPrefab(go))
                return;

            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
                return;

            // Don't show button for disconnected prefab instances and if prefab asset is missing
            if (PrefabUtility.GetPrefabInstanceStatus(go) != PrefabInstanceStatus.Connected)
                return;

            // We can't simply check if the go is part of an immutable prefab, since that would check the asset of the
            // outermost prefab this go is part of. Instead we have to check original source or variant root
            // - the same one that would get opened if clicking the arrow.
            var source = PrefabUtility.GetOriginalSourceOrVariantRoot(go);
            if (source == null || PrefabUtility.IsPartOfImmutablePrefab(source))
                return;

            item.showPrefabModeButton = true;
        }

        protected override void OnContentGUI(Rect rect, int row, TreeViewItem item, string label, bool selected, bool focused,
            bool useBoldFont, bool isPinging)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            GameObjectTreeViewItem goItem = item as GameObjectTreeViewItem;
            if (goItem == null)
                return;

            if (goItem.isSceneHeader)
            {
                if (goItem.scene.isDirty)
                    label += "*";

                switch (goItem.scene.loadingState)
                {
                    case Scene.LoadingState.NotLoaded:
                        label += " (not loaded)";
                        break;
                    case Scene.LoadingState.Loading:
                        label += " (is loading)";
                        break;
                }

                // Render disabled if scene is unloaded
                using (new EditorGUI.DisabledScope(!goItem.scene.isLoaded))
                {
                    base.OnContentGUI(rect, row, item, label, selected, focused, useBoldFont, isPinging);
                }
                return;
            }

            if (!isPinging)
            {
                // The rect is assumed indented and sized after the content when pinging
                rect.xMin += GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
            }

            int colorCode = goItem.colorCode;

            if (string.IsNullOrEmpty(item.displayName))
            {
                /*
                 * We refresh data between Editor and Player changes to the hierarchy. But we repaint after both has happened.
                 * So there is a possibility that the Player deleted a gameobject that is still cached in our DataSource.
                 * Due to risk mitigation for 4.5 we are working around this here.
                 * A proper fix should be to make sure we also fetch data after Player changes as well.
                 * Look into: Application.cpp and look where GetSceneTracker().TickHierarchyWindowHasChanged() is being called.
                 * Likely the proper fix will be adding another call to TickHierarchyWindowHasChanged after the Player had a chance to run.
                 */
                if (goItem.objectPPTR != null)
                    goItem.displayName = goItem.objectPPTR.name;
                else
                    goItem.displayName = "deleted gameobject";
                label = goItem.displayName;
            }

            GUIStyle lineStyle = Styles.lineStyle;

            if (!goItem.shouldDisplay)
            {
                lineStyle = GameObjectStyles.disabledLabel; // TODO: THis need to be a better color then just the disabled.
            }
            else
            {
                if ((colorCode & 3) == (int)GameObjectColorType.Normal)
                    lineStyle = (colorCode < 4) ? Styles.lineStyle : GameObjectStyles.disabledLabel;
                else if ((colorCode & 3) == (int)GameObjectColorType.Prefab)
                    lineStyle = (colorCode < 4) ? GameObjectStyles.prefabLabel : GameObjectStyles.disabledPrefabLabel;
                else if ((colorCode & 3) == (int)GameObjectColorType.BrokenPrefab)
                    lineStyle = (colorCode < 4) ? GameObjectStyles.brokenPrefabLabel : GameObjectStyles.disabledBrokenPrefabLabel;
            }

            lineStyle.padding.left = 0;
            if (goItem.icon != null)
            {
                Rect iconRect = rect;
                iconRect.width = k_IconWidth;
                bool renderDisabled = colorCode >= 4;
                Color col = GUI.color;
                if (renderDisabled)
                    col = new Color(1f, 1f, 1f, 0.5f);
                GUI.DrawTexture(iconRect, goItem.icon, ScaleMode.ScaleToFit, true, 0, col, 0, 0);

                if (goItem.overlayIcon != null)
                    GUI.DrawTexture(iconRect, goItem.overlayIcon, ScaleMode.ScaleToFit, true, 0, col, 0, 0);

                rect.xMin += iconTotalPadding + k_IconWidth + k_SpaceBetweenIconAndText;
            }

            // Draw text
            lineStyle.Draw(rect, label, false, false, selected, focused);
        }

        public void PrefabModeButton(GameObjectTreeViewItem item, Rect selectionRect)
        {
            if (item.showPrefabModeButton)
            {
                float yOffset = (selectionRect.height - GameObjectStyles.rightArrow.fixedWidth) / 2;
                Rect buttonRect = new Rect(
                    selectionRect.xMax - GameObjectStyles.rightArrow.fixedWidth - GameObjectStyles.rightArrow.margin.right,
                    selectionRect.y + yOffset,
                    GameObjectStyles.rightArrow.fixedWidth,
                    GameObjectStyles.rightArrow.fixedHeight);

                int instanceID = item.id;
                GUIContent content = buttonRect.Contains(Event.current.mousePosition) ? GetPrefabButtonContent(instanceID) : GUIContent.none;
                if (GUI.Button(buttonRect, content, GameObjectStyles.rightArrow))
                {
                    GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                    string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    Object originalSource = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (originalSource != null)
                    {
                        PrefabStageUtility.OpenPrefab(assetPath, go, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceHierarchyRightArrow);
                    }
                }
            }
        }

        GUIContent GetPrefabButtonContent(int instanceID)
        {
            GUIContent result;
            if (m_PrefabButtonContents.TryGetValue(instanceID, out result))
            {
                return result;
            }

            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(EditorUtility.InstanceIDToObject(instanceID) as GameObject);
            string filename = System.IO.Path.GetFileNameWithoutExtension(path);
            result = new GUIContent("", null, "Open Prefab Asset '" + filename + "'");
            m_PrefabButtonContents[instanceID] = result;
            return result;
        }

        Dictionary<int, GUIContent> m_PrefabButtonContents = new Dictionary<int, GUIContent>();
    }
}
