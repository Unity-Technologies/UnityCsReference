// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.StyleSheets;
using UnityEditor.VersionControl;
using UnityEditorInternal.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
            public static GUIStyle disabledLabel = new GUIStyle("PR DisabledLabel");
            public static GUIStyle prefabLabel = "PR PrefabLabel";
            public static GUIStyle disabledPrefabLabel = "PR DisabledPrefabLabel";
            public static GUIStyle brokenPrefabLabel = "PR BrokenPrefabLabel";
            public static GUIStyle disabledBrokenPrefabLabel = "PR DisabledBrokenPrefabLabel";
            public static GUIStyle optionsButtonStyle = "PaneOptions";
            public static GUIStyle sceneHeaderBg = "SceneTopBarBg";
            public static SVC<float> sceneHeaderWidth = new SVC<float>("SceneTopBarBg", "border-bottom-width", 1f);
            public static GUIStyle rightArrow = "ArrowNavigationRight";
            public static GUIStyle hoveredItemBackgroundStyle = "WhiteBackground";
            public static Color hoveredBackgroundColor =
                EditorResources.GetStyle("game-object-tree-view").GetColor("-unity-object-tree-hovered-color");

            public static Texture2D sceneAssetIcon = EditorGUIUtility.FindTexture(typeof(SceneAsset));
            public static Texture2D prefabIcon = EditorGUIUtility.FindTexture("Prefab Icon");

            static GameObjectStyles()
            {
                disabledLabel.fixedHeight = 0;
                disabledLabel.alignment = TextAnchor.UpperLeft;
                disabledLabel.padding = Styles.lineBoldStyle.padding;
            }

            public static readonly int kSceneHeaderIconsInterval = 2;
        }

        private float m_PrevScollPos;
        private float m_PrevTotalHeight;
        internal delegate float OnHeaderGUIDelegate(Rect availableRect, string scenePath);
        internal static OnHeaderGUIDelegate OnPostHeaderGUI = null;
        private Dictionary<int, Asset[]> m_HierarchyPrefabToAssetIDMap;

        private static Dictionary<string, int> s_ActiveParentObjectPerSceneGUID;

        internal static void UpdateActiveParentObjectValuesForScene(string sceneGUID, int instanceID)
        {
            if (instanceID == 0)
                s_ActiveParentObjectPerSceneGUID.Remove(sceneGUID);
            else
                s_ActiveParentObjectPerSceneGUID[sceneGUID] = instanceID;
        }

        internal void GetActiveParentObjectValuesFromSessionInfo()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var key = SceneManager.GetSceneAt(i).guid;
                var id = SceneHierarchy.GetDefaultParentForSession(SceneManager.GetSceneAt(i).guid);
                if (id != 0)
                    s_ActiveParentObjectPerSceneGUID.Add(key, id);
            }
        }

        static bool DetectSceneGuidMismatchInActiveParentState(KeyValuePair<string, int> activeParentObject)
        {
            var go = EditorUtility.InstanceIDToObject(activeParentObject.Value) as GameObject;
            if (go != null && go.scene.guid != activeParentObject.Key)
            {
                SceneHierarchy.SetDefaultParentForSession(activeParentObject.Key, 0);
                return true;
            }

            return false;
        }

        internal static void RemoveInvalidActiveParentObjects()
        {
            var itemsToRemove = s_ActiveParentObjectPerSceneGUID.Where(activeParent => DetectSceneGuidMismatchInActiveParentState(activeParent)).ToArray();
            foreach (var itemToRemove in itemsToRemove)
            {
                SceneHierarchy.UpdateSessionStateInfoAndActiveParentObjectValuesForScene(itemToRemove.Key, 0);
            }
        }

        GameObjectTreeViewDataSource dataSource
        {
            get { return (GameObjectTreeViewDataSource)m_TreeView.data; }
        }

        bool showingSearchResults
        {
            get { return !string.IsNullOrEmpty(dataSource.searchString); }
        }

        public GameObjectTreeViewGUI(TreeViewController treeView, bool useHorizontalScroll)
            : base(treeView, useHorizontalScroll)
        {
            k_TopRowMargin = 0f;
            m_TreeView.enableItemHovering = true;
        }

        public override void OnInitialize()
        {
            SceneVisibilityManager.visibilityChanged += SceneVisibilityManagerOnVisibilityChanged;
            dataSource.beforeReloading += SubSceneGUI.FetchSubSceneInfo;
            m_PrevScollPos = m_TreeView.state.scrollPos.y;
            m_PrevTotalHeight = m_TreeView.GetTotalRect().height;
            k_BaseIndent = SceneVisibilityHierarchyGUI.utilityBarWidth;
            s_ActiveParentObjectPerSceneGUID = new Dictionary<string, int>();
            GetActiveParentObjectValuesFromSessionInfo();

            m_HierarchyPrefabToAssetIDMap = new Dictionary<int, Asset[]>();
        }

        private void SceneVisibilityManagerOnVisibilityChanged()
        {
            m_TreeView.Repaint();
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

                var sceneHeaderItem = dataSource.sceneHeaderItems.FirstOrDefault(p => p.scene == firstItem.scene);
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
                dataSource.EnsureFullyInitialized();
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

            GameObject gameObject = (GameObject)goItem.objectPPTR;
            if ((gameObject.hideFlags & HideFlags.NotEditable) != 0)
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

        private bool isDragging
        {
            get
            {
                return m_TreeView.isDragging ||
                    (m_TreeView.dragging != null && m_TreeView.dragging.GetDropTargetControlID() != -1);
            }
        }

        override protected void DrawItemBackground(Rect rect, int row, TreeViewItem item, bool selected, bool focused)
        {
            var goItem = (GameObjectTreeViewItem)item;
            if (goItem.isSceneHeader)
            {
                GUI.Label(rect, GUIContent.none, GameObjectStyles.sceneHeaderBg);
            }
            else
            {
                // Don't show indented sub scene header backgrounds when searching (as the texts are not indented here)
                if (SubSceneGUI.IsUsingSubScenes() && !showingSearchResults)
                {
                    var gameObject = (GameObject)goItem.objectPPTR;
                    if (gameObject != null && SubSceneGUI.IsSubSceneHeader(gameObject))
                    {
                        SubSceneGUI.DrawSubSceneHeaderBackground(rect, k_BaseIndent, k_IndentWidth, gameObject);
                    }
                }
            }

            if (m_TreeView.hoveredItem != item)
                return;

            if (isDragging)
                return;

            using (new GUI.BackgroundColorScope(GameObjectStyles.hoveredBackgroundColor))
            {
                GUI.Label(rect, GUIContent.none, GameObjectStyles.hoveredItemBackgroundStyle);
            }
        }

        float m_ContentRectRight;

        override protected void DoItemGUI(Rect rect, int row, TreeViewItem item, bool selected, bool focused, bool useBoldFont)
        {
            GameObjectTreeViewItem goItem = item as GameObjectTreeViewItem;
            if (goItem == null)
                return;

            EnsureLazyInitialization(goItem);

            if (goItem.isSceneHeader)
            {
                useBoldFont = (goItem.scene == SceneManager.GetActiveScene());
            }

            base.DoItemGUI(rect, row, item, selected, focused, useBoldFont);
            SceneVisibilityHierarchyGUI.DoItemGUI(rect, goItem, selected && !IsRenaming(item.id), m_TreeView.hoveredItem == goItem, focused, isDragging);
        }

        protected override void OnAdditionalGUI(Rect rect, int row, TreeViewItem item, bool selected, bool focused)
        {
            GameObjectTreeViewItem goItem = item as GameObjectTreeViewItem;
            if (goItem == null)
                return;

            m_ContentRectRight = 0;

            if (goItem.isSceneHeader)
            {
                m_ContentRectRight = DoAdditionalSceneHeaderGUI(goItem, rect);
            }
            else
            {
                m_ContentRectRight = PrefabModeButton(goItem, rect);
                if (SubSceneGUI.IsUsingSubScenes() && !showingSearchResults)
                {
                    SubSceneGUI.DrawVerticalLine(rect, k_BaseIndent, k_IndentWidth, (GameObject)goItem.objectPPTR);
                }
            }

            if (SceneHierarchy.s_Debug)
                GUI.Label(new Rect(rect.xMax - 70, rect.y, 70, rect.height), "" + row + " (" + goItem.id + ")", EditorStyles.boldLabel);
        }

        protected override Rect GetDropTargetRect(Rect rect)
        {
            rect.xMin += SceneVisibilityHierarchyGUI.utilityBarWidth;

            return rect;
        }

        protected float DoAdditionalSceneHeaderGUI(GameObjectTreeViewItem goItem, Rect rect)
        {
            // Options button
            const float optionsButtonWidth = 16f;
            const float optionsButtonHeight = 16f;
            const float margin = 4f;

            Rect buttonRect = new Rect(rect.xMax - optionsButtonWidth - margin, rect.y + (rect.height - optionsButtonHeight) * 0.5f, optionsButtonWidth, rect.height);

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
            float availableRectLeft = buttonRect.xMin;
            if (null != OnPostHeaderGUI)
            {
                float optionsWidth = (rect.width - buttonRect.x);
                float width = (rect.width - optionsWidth - margin);
                float x = 0;
                float y = rect.y;
                float height = rect.height;
                Rect availableRect = new Rect(x, y, width, height);
                availableRectLeft = Math.Min(availableRectLeft, OnPostHeaderGUI(availableRect, goItem.scene.path));
            }

            return availableRectLeft;
        }

        void EnsureLazyInitialization(GameObjectTreeViewItem item)
        {
            if (!item.lazyInitializationDone)
            {
                item.lazyInitializationDone = true;

                SetItemIcon(item);
                SetItemSelectedIcon(item);
                SetItemOverlayIcon(item);
                SetPrefabModeButtonVisibility(item);
            }
        }

        void SetItemIcon(GameObjectTreeViewItem item)
        {
            var go = item.objectPPTR as GameObject;
            if (go == null)
            {
                item.icon = GameObjectStyles.sceneAssetIcon;
            }
            else
            {
                if (SubSceneGUI.IsSubSceneHeader(go))
                    item.icon = GameObjectStyles.sceneAssetIcon;
                else
                    item.icon = PrefabUtility.GetIconForGameObject(go);
            }
        }

        void SetItemSelectedIcon(GameObjectTreeViewItem item)
        {
            if (item.icon != null)
            {
                item.selectedIcon = EditorUtility.GetIconInActiveState(item.icon) as Texture2D;
            }
        }

        internal override Texture GetIconForSelectedItem(TreeViewItem item)
        {
            GameObjectTreeViewItem goItem = item as GameObjectTreeViewItem;

            if (goItem != null)
            {
                return goItem.selectedIcon;
            }

            return item.icon;
        }

        void SetItemOverlayIcon(GameObjectTreeViewItem item)
        {
            item.overlayIcon = null;

            var go = item.objectPPTR as GameObject;
            if (go == null)
                return;

            if (PrefabUtility.IsAddedGameObjectOverride(go))
                item.overlayIcon = EditorGUIUtility.LoadIcon("PrefabOverlayAdded Icon");


            if (!EditorApplication.isPlaying)
            {
                Asset asset = GetAsset(item);
                if (asset != null && !m_HierarchyPrefabToAssetIDMap.ContainsKey(item.id))
                {
                    string metaPath = asset.path.Trim('/') + ".meta";
                    Asset metaAsset = Provider.GetAssetByPath(metaPath);
                    Asset[] assets = new[] {asset, metaAsset};

                    m_HierarchyPrefabToAssetIDMap.Add(item.id, assets);
                }
            }
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

            rect.xMax = m_ContentRectRight;

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

            lineStyle = Styles.lineStyle;

            if (SubSceneGUI.IsUsingSubScenes())
                useBoldFont = SubSceneGUI.UseBoldFontForGameObject((GameObject)goItem.objectPPTR);

            if (useBoldFont)
            {
                lineStyle = Styles.lineBoldStyle;
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

            if (s_ActiveParentObjectPerSceneGUID.ContainsValue(goItem.id))
            {
                lineStyle = Styles.lineBoldStyle;
            }

            lineStyle.padding.left = 0;
            Texture icon = GetEffectiveIcon(goItem);

            if (icon != null)
            {
                Rect iconRect = rect;
                iconRect.width = k_IconWidth;
                bool renderDisabled = colorCode >= 4;
                Color col = GUI.color;
                if (renderDisabled || (CutBoard.hasCutboardData && CutBoard.IsGameObjectPartOfCutAndPaste((GameObject)goItem.objectPPTR)))
                    col = new Color(1f, 1f, 1f, 0.5f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 0, col, 0, 0);

                if (goItem.overlayIcon != null)
                    GUI.DrawTexture(iconRect, goItem.overlayIcon, ScaleMode.ScaleToFit, true, 0, col, 0, 0);

                if (!EditorApplication.isPlaying)
                {
                    Asset[] assets;
                    m_HierarchyPrefabToAssetIDMap.TryGetValue(item.id, out assets);
                    if (assets != null)
                    {
                        iconRect.x -= 10;
                        iconRect.width += 7 * 2;

                        Overlay.DrawHierarchyOverlay(assets[0], assets[1], iconRect);
                    }
                }

                rect.xMin += iconTotalPadding + k_IconWidth + k_SpaceBetweenIconAndText;
            }

            // Draw text
            lineStyle.Draw(rect, label, false, false, selected, focused);
        }

        private Asset GetAsset(GameObjectTreeViewItem item)
        {
            if (!Provider.isActive)
                return null;

            GameObject go = (GameObject)item.objectPPTR;

            if (!go || PrefabUtility.GetNearestPrefabInstanceRoot(go) != go)
                return null;

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            Asset vcAsset = string.IsNullOrEmpty(guid) ? null : Provider.GetAssetByGUID(guid);
            return vcAsset;
        }

        public float PrefabModeButton(GameObjectTreeViewItem item, Rect selectionRect)
        {
            float contentRectRight = selectionRect.xMax;
            if (item.showPrefabModeButton)
            {
                float yOffset = (selectionRect.height - GameObjectStyles.rightArrow.fixedWidth) / 2;
                Rect buttonRect = new Rect(
                    selectionRect.xMax - GameObjectStyles.rightArrow.fixedWidth - GameObjectStyles.rightArrow.margin.right,
                    selectionRect.y + yOffset,
                    GameObjectStyles.rightArrow.fixedWidth,
                    GameObjectStyles.rightArrow.fixedHeight);

                int instanceID = item.id;
                GUIContent content = buttonRect.Contains(Event.current.mousePosition) ? PrefabStageUtility.GetPrefabButtonContent(instanceID) : GUIContent.none;
                if (GUI.Button(buttonRect, content, GameObjectStyles.rightArrow))
                {
                    GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                    string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    Object originalSource = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (originalSource != null)
                    {
                        var prefabStageMode = PrefabStageUtility.GetPrefabStageModeFromModifierKeys();
                        PrefabStageUtility.OpenPrefab(assetPath, go, prefabStageMode, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceHierarchyRightArrow);
                    }
                }

                contentRectRight = buttonRect.xMin;
            }

            return contentRectRight;
        }
    }
}
