// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using TreeViewController = UnityEditor.IMGUI.Controls.TreeViewController<UnityEngine.EntityId>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<UnityEngine.EntityId>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<UnityEngine.EntityId>;


namespace UnityEditor
{
    // Description: Since TreeView is not serialized the client should set the tree view when requested
    // by ObjectTreeSelector.  Requested by calling treeViewNeededCallback provided by the client,
    // use SetTreeView() to set tree view.

    [Serializable]
    internal class ObjectTreeForSelector
    {
        internal class TreeSelectorData
        {
            public ObjectTreeForSelector objectTreeForSelector;
            public EditorWindow editorWindow;
            public TreeViewState state;
            public Rect treeViewRect;
            public int userData;
        }

        EditorWindow m_Owner;
        TreeViewController m_TreeView;
        TreeViewState m_TreeViewState;
        int m_ErrorCounter;
        EntityId m_OriginalSelectedID;
        int m_UserData;
        EntityId m_LastSelectedID = EntityId.None;
        string m_SelectedPath = "";
        const string kSearchFieldTag = "TreeSearchField";
        const float kBottomBarHeight = 17f;
        const float kTopBarHeight = 0; // top bar rendered with UI-Toolkit
        SelectionEvent m_SelectionEvent;
        TreeViewNeededEvent m_TreeViewNeededEvent;
        DoubleClickedEvent m_DoubleClickedEvent;
        Delayer m_Debounce;
        string m_SearchString;
        HashSet<Event> m_PriorityKeyboardEvents;

        [Serializable] public class SelectionEvent : UnityEvent<TreeViewItem> {}
        [Serializable] public class TreeViewNeededEvent : UnityEvent<TreeSelectorData> {}
        [Serializable] public class DoubleClickedEvent : UnityEvent {}

        class Styles
        {
            public GUIStyle searchBg = "OT TopBar";
            public GUIStyle bottomBarBg = "OT BottomBar";
        }
        static Styles s_Styles;

        public bool IsInitialized()
        {
            return m_Owner != null;
        }

        public void Init(
            Rect position,
            EditorWindow owner,
            UnityAction<TreeSelectorData> treeViewNeededCallback,
            UnityAction<TreeViewItem> selectionCallback,
            UnityAction doubleClickedCallback,
            EntityId initialSelectedTreeViewItemID,
            int userData,
            HashSet<Event> priorityKeyboardEvents)
        {
            Clear();

            m_Owner = owner;

            m_TreeViewNeededEvent = new TreeViewNeededEvent();
            m_TreeViewNeededEvent.AddPersistentListener(treeViewNeededCallback, UnityEventCallState.EditorAndRuntime);

            m_SelectionEvent = new SelectionEvent();
            m_SelectionEvent.AddPersistentListener(selectionCallback, UnityEventCallState.EditorAndRuntime);

            m_DoubleClickedEvent = new DoubleClickedEvent();
            m_DoubleClickedEvent.AddPersistentListener(doubleClickedCallback, UnityEventCallState.EditorAndRuntime);

            m_OriginalSelectedID = initialSelectedTreeViewItemID;
            m_UserData = userData;
            m_PriorityKeyboardEvents = priorityKeyboardEvents;

            // Initial setup
            EnsureTreeViewIsValid(GetTreeViewRect(position));
            if (m_TreeView != null)
            {
                m_TreeView.SetSelection(new[] { m_OriginalSelectedID }, true);
                // If nothing is selected we expand all to better start overview. If we have a selection it has been revealed in SetSelection above
                if (m_OriginalSelectedID == EntityId.None)
                    m_TreeView.data.SetExpandedWithChildren(m_TreeView.data.root, true);
            }

            m_Debounce = Delayer.Debounce(context =>
            {
                DoSearchFilter();
                m_Owner.Repaint();
            });
        }

        public void Clear()
        {
            m_Owner = null;
            m_TreeViewNeededEvent = null;
            m_SelectionEvent = null;

            m_DoubleClickedEvent = null;
            m_OriginalSelectedID = EntityId.None;
            m_UserData = 0;

            m_TreeView = null;
            m_TreeViewState = null;
            m_ErrorCounter = 0;

            m_Debounce?.Dispose();
            m_Debounce = null;
        }

        public EntityId[] GetSelection()
        {
            if (m_TreeView != null)
                return m_TreeView.GetSelection();

            return new EntityId[0];
        }

        public void SetSearchFilter(string searchString)
        {
            m_SearchString = searchString;
            m_Debounce.Execute();
        }

        public void SetSelection(EntityId[] selectedIDs)
        {
            m_TreeView.SetSelection(selectedIDs, false);
        }

        // Call this when requested by ObjectTreeSelector (it calls treeViewNeededCallback)
        public void SetTreeView(TreeViewController treeView)
        {
            m_TreeView = treeView;

            // Hook up to tree view events
            m_TreeView.selectionChangedCallback -= OnItemSelectionChanged;
            m_TreeView.selectionChangedCallback += OnItemSelectionChanged;
            m_TreeView.itemDoubleClickedCallback -= OnItemDoubleClicked;
            m_TreeView.itemDoubleClickedCallback += OnItemDoubleClicked;
        }

        bool EnsureTreeViewIsValid(Rect treeViewRect)
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            if (m_TreeView == null)
            {
                var input = new TreeSelectorData()
                {
                    state = m_TreeViewState,
                    treeViewRect = treeViewRect,
                    userData = m_UserData,
                    objectTreeForSelector = this,
                    editorWindow = m_Owner
                };

                m_TreeViewNeededEvent.Invoke(input);
                if (m_TreeView != null)
                {
                    if (m_TreeView.data.root == null)
                    {
                        m_TreeView.ReloadData();
                    }
                }

                if (m_TreeView == null)
                {
                    if (m_ErrorCounter == 0)
                    {
                        Debug.LogError("ObjectTreeSelector is missing its tree view. Ensure to call 'SetTreeView()' when the treeViewNeededCallback is invoked!");
                        m_ErrorCounter++;
                    }
                    return false;
                }
            }
            return true;
        }

        Rect GetTreeViewRect(Rect position)
        {
            return new Rect(0, kTopBarHeight, position.width, position.height - kBottomBarHeight - kTopBarHeight);
        }

        public void OnGUI(Rect position)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            Rect rect = new Rect(0, 0, position.width, position.height);
            Rect bottomRect = new Rect(rect.x, rect.yMax - kBottomBarHeight, rect.width, kBottomBarHeight);
            Rect treeViewRect = GetTreeViewRect(position);

            if (!EnsureTreeViewIsValid(treeViewRect))
                return;

            int treeViewControlID = GUIUtility.GetControlID("Tree".GetHashCode(), FocusType.Keyboard);

            HandleCommandEvents();
            HandlePriorityKeyboardEvents();
            TreeViewArea(treeViewRect, treeViewControlID);
            BottomBar(bottomRect);
        }

        void BottomBar(Rect bottomRect)
        {
            EntityId currentID = m_TreeView.GetSelection().FirstOrDefault();   // 0 is none selected

            // Refresh cached string
            if (currentID != m_LastSelectedID)
            {
                m_LastSelectedID = currentID;
                m_SelectedPath = "";
                var selected = m_TreeView.FindItem(currentID);
                if (selected != null)
                {
                    StringBuilder sb = new StringBuilder();
                    var item = selected;
                    while (item != null && item != m_TreeView.data.root)
                    {
                        if (item != selected)
                            sb.Insert(0, "/");
                        sb.Insert(0, item.displayName);
                        item = item.parent;
                    }
                    m_SelectedPath = sb.ToString();
                }
            }

            GUI.Label(bottomRect, GUIContent.none, s_Styles.bottomBarBg);
            if (!string.IsNullOrEmpty(m_SelectedPath))
                GUI.Label(bottomRect, GUIContent.Temp(m_SelectedPath), EditorStyles.miniLabel);
        }

        private void OnItemDoubleClicked(EntityId id)
        {
            if (m_DoubleClickedEvent != null)
                m_DoubleClickedEvent.Invoke();
        }

        private void OnItemSelectionChanged(EntityId[] selection)
        {
            if (m_SelectionEvent != null)
            {
                TreeViewItem item = null;
                if (selection.Length > 0)
                {
                    item = m_TreeView.FindItem(selection[0]);
                }
                FireSelectionEvent(item);
            }
        }

        void HandlePriorityKeyboardEvents()
        {
            if (Event.current.type != EventType.KeyDown)
                return;

            if (m_PriorityKeyboardEvents?.Contains(Event.current) ?? false)
            {
                // For these priority events to work when called from the SearchField, we need to
                // call "m_TreeView.KeyboardGUI(false);" to bypass the keyboard control check.
                // We also cannot give keyboard control to the TreeView, otherwise the SearchField
                // will lose keyboard focus. When the focus is on the TreeView, this call also works
                // because it will be handled once here and not in the m_TreeView.OnGUI.
                m_TreeView.KeyboardGUI(false);
            }
        }

        void FrameSelectedTreeViewItem()
        {
            m_TreeView.Frame(m_TreeView.state.lastClickedID, true, false);
        }

        void HandleCommandEvents()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
                return;

            if (evt.commandName == EventCommandNames.FrameSelected)
            {
                if (evt.type == EventType.ExecuteCommand && m_TreeView.HasSelection())
                {
                    m_SearchString = string.Empty;
                    DoSearchFilter();
                    FrameSelectedTreeViewItem();
                }
                evt.Use();
                GUIUtility.ExitGUI();
            }
        }

        void FireSelectionEvent(TreeViewItem item)
        {
            if (m_SelectionEvent != null)
                m_SelectionEvent.Invoke(item);
        }

        void TreeViewArea(Rect treeViewRect, int treeViewControlID)
        {
            bool hasRows = m_TreeView.data.rowCount > 0;
            if (hasRows)
            {
                m_TreeView.OnGUI(treeViewRect, treeViewControlID);
            }
        }

        private void DoSearchFilter()
        {
            m_TreeView.searchString = m_SearchString;
        }

        internal void ChangeExpandedState(EntityId id, bool expand, bool includeChildren)
        {
            var item = m_TreeView.FindItem(id);
            if (item != null)
            {
                m_TreeView.ChangeExpandedState(item, expand, includeChildren);
            }
        }

        internal bool IsExpanded(EntityId id)
        {
            var item = m_TreeView.FindItem(id);
            if (item != null)
            {
                return m_TreeView.data.IsExpanded(item);
            }
            return false;
        }

        internal void GrabKeyboardFocus()
        {
            m_TreeView.GrabKeyboardFocus();
        }
    }
} // namespace
