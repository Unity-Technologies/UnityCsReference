// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Events;

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
        bool m_FocusSearchFilter;
        int m_ErrorCounter;
        int m_OriginalSelectedID;
        int m_UserData;
        int m_LastSelectedID = -1;
        string m_SelectedPath = "";
        const string kSearchFieldTag = "TreeSearchField";
        const float kBottomBarHeight = 17f;
        const float kTopBarHeight = 27f;
        SelectionEvent m_SelectionEvent;
        TreeViewNeededEvent m_TreeViewNeededEvent;
        DoubleClickedEvent m_DoubleClickedEvent;

        [Serializable] public class SelectionEvent : UnityEvent<TreeViewItem> {}
        [Serializable] public class TreeViewNeededEvent : UnityEvent<TreeSelectorData> {}
        [Serializable] public class DoubleClickedEvent : UnityEvent {}

        class Styles
        {
            public GUIStyle searchBg = new GUIStyle("ProjectBrowserTopBarBg");
            public GUIStyle bottomBarBg = new GUIStyle("ProjectBrowserBottomBarBg");
            public Styles()
            {
                searchBg.border = new RectOffset(0, 0, 2, 2);
                searchBg.fixedHeight = 0;

                bottomBarBg.alignment = TextAnchor.MiddleLeft;
                bottomBarBg.fontSize = EditorStyles.label.fontSize;
                bottomBarBg.padding = new RectOffset(5, 5, 0, 0);
            }
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
            int initialSelectedTreeViewItemID,
            int userData)
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

            m_FocusSearchFilter = true; // start by focusing search field

            // Initial setup
            EnsureTreeViewIsValid(GetTreeViewRect(position));
            if (m_TreeView != null)
            {
                m_TreeView.SetSelection(new[] { m_OriginalSelectedID }, true);
                // If nothing is selected we expand all to better start overview. If we have a selection it has been revealed in SetSelection above
                if (m_OriginalSelectedID == 0)
                    m_TreeView.data.SetExpandedWithChildren(m_TreeView.data.root, true);
            }
        }

        public void Clear()
        {
            m_Owner = null;
            m_TreeViewNeededEvent = null;
            m_SelectionEvent = null;

            m_DoubleClickedEvent = null;
            m_OriginalSelectedID = 0;
            m_UserData = 0;

            m_TreeView = null;
            m_TreeViewState = null;
            m_ErrorCounter = 0;
            m_FocusSearchFilter = false;
        }

        public int[] GetSelection()
        {
            if (m_TreeView != null)
                return m_TreeView.GetSelection();

            return new int[0];
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
            Rect toolbarRect = new Rect(rect.x, rect.y, rect.width, kTopBarHeight);
            Rect bottomRect = new Rect(rect.x, rect.yMax - kBottomBarHeight, rect.width, kBottomBarHeight);
            Rect treeViewRect = GetTreeViewRect(position);

            if (!EnsureTreeViewIsValid(treeViewRect))
                return;

            int treeViewControlID = GUIUtility.GetControlID("Tree".GetHashCode(), FocusType.Keyboard);

            HandleCommandEvents();
            HandleKeyboard(treeViewControlID);
            SearchArea(toolbarRect);
            TreeViewArea(treeViewRect, treeViewControlID);
            BottomBar(bottomRect);
        }

        void BottomBar(Rect bottomRect)
        {
            int currentID = m_TreeView.GetSelection().FirstOrDefault();   // 0 is none selected

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

        private void OnItemDoubleClicked(int id)
        {
            if (m_DoubleClickedEvent != null)
                m_DoubleClickedEvent.Invoke();
        }

        private void OnItemSelectionChanged(int[] selection)
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

        void HandleKeyboard(int treeViewControlID)
        {
            if (Event.current.type != EventType.KeyDown)
                return;

            switch (Event.current.keyCode)
            {
                case KeyCode.DownArrow:
                case KeyCode.UpArrow:
                {
                    // When searchfield has focus give keyboard focus to the tree view on Down/UpArrow
                    bool hasSearchFilterFocus = GUI.GetNameOfFocusedControl() == kSearchFieldTag;
                    if (hasSearchFilterFocus)
                    {
                        GUIUtility.keyboardControl = treeViewControlID;

                        // If nothing is selected ensure first item is selected, otherwise ensure current
                        // selection is visible (we just gave focus to the tree)
                        if (m_TreeView.IsLastClickedPartOfRows())
                            FrameSelectedTreeViewItem();
                        else
                            m_TreeView.OffsetSelection(1);      // Selects first item

                        Event.current.Use();
                    }
                }
                break;
                default:
                    return;
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

            if (evt.commandName == "FrameSelected")
            {
                if (evt.type == EventType.ExecuteCommand && m_TreeView.HasSelection())
                {
                    m_TreeView.searchString = string.Empty;
                    FrameSelectedTreeViewItem();
                }
                evt.Use();
                GUIUtility.ExitGUI();
            }
            if (evt.commandName == "Find")
            {
                if (evt.type == EventType.ExecuteCommand)
                {
                    FocusSearchField();
                }
                evt.Use();
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

        void SearchArea(Rect toolbarRect)
        {
            GUI.Label(toolbarRect, GUIContent.none, s_Styles.searchBg);

            // ESC clears search field and removes it's focus. But if we get an esc event we only want to clear search field.
            // So we need special handling afterwards.
            bool wasEscape = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;
            GUI.SetNextControlName(kSearchFieldTag);
            string newSearchFilter = EditorGUI.SearchField(new Rect(5, 5, toolbarRect.width - 10, 15), m_TreeView.searchString);

            if (wasEscape && Event.current.type == EventType.Used)
            {
                // If we hit esc and the string WAS empty, it's an actual cancel event.
                if (m_TreeView.searchString != string.Empty)
                    // Otherwise the string has been cleared and focus has been lost. We don't have anything else to recieve focus, so we want to refocus the search field.
                    m_FocusSearchFilter = true;
            }

            if (newSearchFilter != m_TreeView.searchString || m_FocusSearchFilter)
            {
                m_TreeView.searchString = newSearchFilter;
                HandleUtility.Repaint();
            }

            if (m_FocusSearchFilter)
            {
                EditorGUI.FocusTextInControl(kSearchFieldTag);
                if (Event.current.type == EventType.Repaint)
                    m_FocusSearchFilter = false;
            }
        }

        internal void FocusSearchField()
        {
            m_FocusSearchFilter = true;
        }
    }
} // namespace
