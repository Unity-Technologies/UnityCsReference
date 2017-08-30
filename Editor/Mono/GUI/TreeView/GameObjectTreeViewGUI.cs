// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

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
            public static GUIStyle prefabLabel = new GUIStyle("PR PrefabLabel");
            public static GUIStyle disabledPrefabLabel = new GUIStyle("PR DisabledPrefabLabel");
            public static GUIStyle brokenPrefabLabel = new GUIStyle("PR BrokenPrefabLabel");
            public static GUIStyle disabledBrokenPrefabLabel = new GUIStyle("PR DisabledBrokenPrefabLabel");
            public static GUIContent loadSceneGUIContent = new GUIContent(EditorGUIUtility.FindTexture("SceneLoadIn"), "Load scene");
            public static GUIContent unloadSceneGUIContent = new GUIContent(EditorGUIUtility.FindTexture("SceneLoadOut"), "Unload scene");
            public static GUIContent saveSceneGUIContent = new GUIContent(EditorGUIUtility.FindTexture("SceneSave"), "Save scene");
            public static GUIStyle optionsButtonStyle = "PaneOptions";
            public static GUIStyle sceneHeaderBg = "ProjectBrowserTopBarBg";

            public static readonly int kSceneHeaderIconsInterval = 2;

            static GameObjectStyles()
            {
                disabledLabel.alignment = TextAnchor.MiddleLeft;
                prefabLabel.alignment = TextAnchor.MiddleLeft;
                disabledPrefabLabel.alignment = TextAnchor.MiddleLeft;
                brokenPrefabLabel.alignment = TextAnchor.MiddleLeft;
                disabledBrokenPrefabLabel.alignment = TextAnchor.MiddleLeft;

                ClearSelectionTexture(disabledLabel);
                ClearSelectionTexture(prefabLabel);
                ClearSelectionTexture(disabledPrefabLabel);
                ClearSelectionTexture(brokenPrefabLabel);
                ClearSelectionTexture(disabledBrokenPrefabLabel);
            }

            static void ClearSelectionTexture(GUIStyle style)
            {
                var transparent = style.hover.background;
                style.onNormal.background = transparent;
                style.onActive.background = transparent;
                style.onFocused.background = transparent;
            }
        }

        private float m_PrevScollPos;
        private float m_PrevTotalHeight;
        internal delegate void OnHeaderGUIDelegate(Rect availableRect, string scenePath);
        internal static OnHeaderGUIDelegate OnPostHeaderGUI = null;

        public GameObjectTreeViewGUI(TreeViewController treeView, bool useHorizontalScroll)
            : base(treeView, useHorizontalScroll)
        {
            k_TopRowMargin = 0f;
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

        override protected void DoItemGUI(Rect rect, int row, TreeViewItem item, bool selected, bool focused, bool useBoldFont)
        {
            GameObjectTreeViewItem goItem = item as GameObjectTreeViewItem;
            if (goItem == null)
                return;

            // Scene header background (make it slightly transparent to hint it
            // is not the normal scene header)
            if (goItem.isSceneHeader)
            {
                Color oldColor = GUI.color;
                GUI.color = GUI.color * new Color(1, 1, 1, 0.9f);
                GUI.Label(rect, GUIContent.none, GameObjectStyles.sceneHeaderBg);
                GUI.color = oldColor;
            }

            base.DoItemGUI(rect, row, item, selected, focused, useBoldFont);

            // Scene header extras
            if (goItem.isSceneHeader)
                DoAdditionalSceneHeaderGUI(goItem, rect);

            if (SceneHierarchyWindow.s_Debug)
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
                bool isBold = (goItem.scene == SceneManager.GetActiveScene());
                using (new EditorGUI.DisabledScope(!goItem.scene.isLoaded))
                {
                    base.OnContentGUI(rect, row, item, label, selected, focused, isBold, isPinging);
                }
                return;
            }

            if (!isPinging)
            {
                // The rect is assumed indented and sized after the content when pinging
                rect.xMin += GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
            }

            int i = goItem.colorCode;

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
                if ((i & 3) == (int)GameObjectColorType.Normal)
                    lineStyle = (i < 4) ? Styles.lineStyle : GameObjectStyles.disabledLabel;
                else if ((i & 3) == (int)GameObjectColorType.Prefab)
                    lineStyle = (i < 4) ? GameObjectStyles.prefabLabel : GameObjectStyles.disabledPrefabLabel;
                else if ((i & 3) == (int)GameObjectColorType.BrokenPrefab)
                    lineStyle = (i < 4) ? GameObjectStyles.brokenPrefabLabel : GameObjectStyles.disabledBrokenPrefabLabel;
            }

            Texture icon = GetIconForItem(item);

            lineStyle.padding.left = 0;
            if (icon != null)
            {
                Rect iconRect = rect;
                iconRect.width = k_IconWidth;
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                rect.xMin += iconTotalPadding + k_IconWidth + k_SpaceBetweenIconAndText;
            }

            // Draw text
            lineStyle.Draw(rect, label, false, false, selected, focused);
        }
    }
}
