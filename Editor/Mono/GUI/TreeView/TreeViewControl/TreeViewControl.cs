// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.IMGUI.Controls
{
    // The TreeView is a wrapper for the internal treeview system (wraps the TreeViewController and its 3
    // interfaces: ITreeViewDataSource, ITreeViewGUI, ITreeViewDragging)
    // - It is intended for traditional treeviews and listviews (and supports multiple columns)
    // - Usage:
    //   - Inherit from TreeView and implement the abtract methods.
    //   - Override the virtual methods for further customization (rendering, dragging, renaming etc)
    //   - Adjust layout properties

    public abstract partial class TreeView
    {
        TreeViewController m_TreeView;
        TreeViewControlDataSource m_DataSource;
        TreeViewControlGUI m_GUI;
        TreeViewControlDragging m_Dragging;
        MultiColumnHeader m_MultiColumnHeader;
        List<TreeViewItem> m_DefaultRows;
        int m_TreeViewKeyControlID;

        OverriddenMethods m_OverriddenMethods;

        public TreeView(TreeViewState state)
        {
            Init(state);
        }

        public TreeView(TreeViewState state, MultiColumnHeader multiColumnHeader)
        {
            m_MultiColumnHeader = multiColumnHeader;
            Init(state);
        }

        void Init(TreeViewState state)
        {
            if (state == null)
                throw new ArgumentNullException("state", "Invalid TreeViewState: it is null");

            m_TreeView = new TreeViewController(null, state);
            m_DataSource = new TreeViewControlDataSource(m_TreeView, this);
            m_GUI = new TreeViewControlGUI(m_TreeView, this);
            m_Dragging = new TreeViewControlDragging(m_TreeView, this);
            m_TreeView.Init(new Rect(), m_DataSource, m_GUI, m_Dragging);

            m_TreeView.searchChanged += SearchChanged;
            m_TreeView.selectionChangedCallback += SelectionChanged;
            m_TreeView.itemDoubleClickedCallback += DoubleClickedItem;
            m_TreeView.contextClickItemCallback += ContextClickedItem;
            m_TreeView.contextClickOutsideItemsCallback += ContextClicked;
            m_TreeView.expandedStateChanged += ExpandedStateChanged;
            m_TreeView.keyboardInputCallback += KeyEvent;

            m_TreeViewKeyControlID = GUIUtility.GetPermanentControlID();
        }

        // The TreeView can be initialized in two ways:
        // 1) Full tree: simplest conceptually and requires the least code. Just create the full tree
        //    in BuildRoot and return the root of tree.
        // 2) For very large trees that often needs rebuilding of the tree structure it is more
        //    performant to just create the expanded rows and omitting all items under collapsed items.
        //    This can be achieved by:
        //    a) Just create the root item in BuildRoot (no descendants)
        //    b) Override BuildRows to handle how the rows are generated: Create the rows list
        //       only visiting expanded items. Also override GetAncestors and GetDescendantsWithChildren
        //       to use the backend data to fetch this information (since it might not be available in the TreeView)
        protected abstract TreeViewItem BuildRoot();


        // Default implementation of BuildRows assumes full tree was built in BuildRoot. With full tree we can also support search out of the box
        protected virtual IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            // Reuse cached list (for capacity)
            if (m_DefaultRows == null)
                m_DefaultRows = new List<TreeViewItem>(100);
            m_DefaultRows.Clear();

            if (hasSearch)
                m_DataSource.SearchFullTree(searchString, m_DefaultRows);
            else
                AddExpandedRows(root, m_DefaultRows);
            return m_DefaultRows;
        }

        public void Reload()
        {
            if (m_OverriddenMethods == null)
                m_OverriddenMethods = new OverriddenMethods(this);
            m_TreeView.ReloadData();
        }

        public void Repaint()
        {
            m_TreeView.Repaint();
        }

        public TreeViewState state { get { return m_TreeView.state; } }
        public MultiColumnHeader multiColumnHeader { get { return m_MultiColumnHeader; } set { m_MultiColumnHeader = value; }}

        protected TreeViewItem rootItem
        {
            get { return m_TreeView.data.root; }
        }

        protected bool isInitialized
        {
            get { return m_DataSource.isInitialized; }
        }

        // Layout variables
        protected Rect treeViewRect
        {
            get { return m_TreeView.GetTotalRect(); }
            set { m_TreeView.SetTotalRect(value); }
        }

        protected float baseIndent
        {
            get { return m_GUI.k_BaseIndent; }
            set { m_GUI.k_BaseIndent = value; }
        }

        protected float foldoutWidth
        {
            get { return m_GUI.foldoutWidth; }
        }

        protected float extraSpaceBeforeIconAndLabel
        {
            get { return m_GUI.extraSpaceBeforeIconAndLabel; }
            set { m_GUI.extraSpaceBeforeIconAndLabel = value; }
        }

        protected float customFoldoutYOffset
        {
            get { return m_GUI.customFoldoutYOffset; }
            set { m_GUI.customFoldoutYOffset = value; }
        }

        protected int columnIndexForTreeFoldouts
        {
            get { return m_GUI.columnIndexForTreeFoldouts; }
            set
            {
                if (multiColumnHeader == null)
                    throw new InvalidOperationException("Setting columnIndexForTreeFoldouts can only be set when using TreeView with a MultiColumnHeader");

                if (value < 0 || value >= multiColumnHeader.state.columns.Length)
                    throw new ArgumentOutOfRangeException("value", string.Format("Invalid index for columnIndexForTreeFoldouts: {0}. Number of available columns: {1}", value, multiColumnHeader.state.columns.Length));

                m_GUI.columnIndexForTreeFoldouts = value;
            }
        }

        protected Rect GetCellRectForTreeFoldouts(Rect rowRect)
        {
            if (multiColumnHeader == null)
                throw new InvalidOperationException("GetCellRect can only be called when 'multiColumnHeader' has been set");

            int columnIndex = columnIndexForTreeFoldouts;
            int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex);
            return multiColumnHeader.GetCellRect(visibleColumnIndex, rowRect);
        }

        protected float depthIndentWidth
        {
            get { return m_GUI.k_IndentWidth; }
        }

        protected bool showAlternatingRowBackgrounds { get; set; }
        protected bool showBorder { get; set; }
        protected bool showingHorizontalScrollBar { get { return m_TreeView.showingHorizontalScrollBar; }}
        protected bool showingVerticalScrollBar { get { return m_TreeView.showingVerticalScrollBar; } }

        protected float cellMargin
        {
            get { return m_GUI.cellMargin; }
            set { m_GUI.cellMargin = value; }
        }

        public float totalHeight
        {
            get { return m_GUI.totalHeight + (showBorder ? m_GUI.borderWidth * 2 : 0f); }
        }

        protected float rowHeight
        {
            get { return m_GUI.k_LineHeight; }
            set { m_GUI.k_LineHeight = Mathf.Max(value, EditorGUIUtility.singleLineHeight); }
        }

        public int treeViewControlID
        {
            get { return m_TreeViewKeyControlID; }
            set { m_TreeViewKeyControlID = value; }
        }

        protected bool isDragging
        {
            get { return m_TreeView.isDragging; }
        }

        protected Rect GetRowRect(int row)
        {
            return m_TreeView.gui.GetRowRect(row, GUIClip.visibleRect.width);
        }

        public virtual IList<TreeViewItem> GetRows()
        {
            if (!isInitialized)
                return null;

            return m_TreeView.data.GetRows();
        }

        protected IList<TreeViewItem> FindRows(IList<int> ids)
        {
            return GetRows().Where(item => ids.Contains(item.id)).ToList();
        }

        protected TreeViewItem FindItem(int id, TreeViewItem searchFromThisItem)
        {
            return TreeViewUtility.FindItem(id, searchFromThisItem);
        }

        protected void GetFirstAndLastVisibleRows(out int firstRowVisible, out int lastRowVisible)
        {
            m_GUI.GetFirstAndLastRowVisible(out firstRowVisible, out lastRowVisible);
        }

        // Expanded interface
        public void ExpandAll()
        {
            SetExpandedRecursive(rootItem.id, true);
        }

        public void CollapseAll()
        {
            SetExpanded(new int[0]);
        }

        public void SetExpandedRecursive(int id, bool expanded)
        {
            m_DataSource.SetExpandedWithChildren(id, expanded);
        }

        public bool SetExpanded(int id, bool expanded)
        {
            return m_DataSource.SetExpanded(id, expanded);
        }

        public void SetExpanded(IList<int> ids)
        {
            m_DataSource.SetExpandedIDs(ids.ToArray());
        }

        public IList<int> GetExpanded()
        {
            return m_DataSource.GetExpandedIDs();
        }

        public bool IsExpanded(int id)
        {
            return m_DataSource.IsExpanded(id);
        }

        // Search interface
        public bool hasSearch
        {
            get { return !string.IsNullOrEmpty(searchString); }
        }

        public string searchString
        {
            get { return m_TreeView.searchString; }
            set { m_TreeView.searchString = value; }
        }

        // Selection interface
        public IList<int> GetSelection()
        {
            return m_TreeView.GetSelection();
        }

        public void SetSelection(IList<int> selectedIDs)
        {
            SetSelection(selectedIDs, TreeViewSelectionOptions.None);
        }

        public void SetSelection(IList<int> selectedIDs, TreeViewSelectionOptions options)
        {
            bool fireSelectionChanged = (options & TreeViewSelectionOptions.FireSelectionChanged) != 0;
            bool revealSelectionAndFrameLastSelected = (options & TreeViewSelectionOptions.RevealAndFrame) != 0;
            bool animatedFraming = false;

            m_TreeView.SetSelection(selectedIDs.ToArray(), revealSelectionAndFrameLastSelected, animatedFraming);
            if (fireSelectionChanged)
                m_TreeView.NotifyListenersThatSelectionChanged();
        }

        public bool IsSelected(int id)
        {
            return m_TreeView.IsSelected(id);
        }

        public bool HasSelection()
        {
            return m_TreeView.HasSelection();
        }

        public bool HasFocus()
        {
            return m_TreeView.HasFocus();
        }

        public void SetFocus()
        {
            GUIUtility.keyboardControl = m_TreeViewKeyControlID;
            EditorGUIUtility.editingTextField = false;
        }

        public void SetFocusAndEnsureSelectedItem()
        {
            SetFocus();

            if (GetRows().Count > 0)
            {
                if (m_TreeView.IsLastClickedPartOfRows())
                    FrameItem(state.lastClickedID);
                else
                    SetSelection(new[] { GetRows()[0].id }, TreeViewSelectionOptions.FireSelectionChanged | TreeViewSelectionOptions.RevealAndFrame);
            }
        }

        protected void SelectionClick(TreeViewItem item, bool keepMultiSelection)
        {
            m_TreeView.SelectionClick(item, keepMultiSelection);
        }

        // Rename interface
        public bool BeginRename(TreeViewItem item)
        {
            return BeginRename(item, 0f);
        }

        public bool BeginRename(TreeViewItem item, float delay)
        {
            return m_GUI.BeginRename(item, delay);
        }

        public void EndRename()
        {
            m_GUI.EndRename();
        }

        // Frame interface
        public void FrameItem(int id)
        {
            bool animated = false;
            m_TreeView.Frame(id, true, false, animated);
        }

        bool m_WarnedUser;
        bool ValidTreeView()
        {
            if (isInitialized)
                return true;

            if (!m_WarnedUser)
            {
                Debug.LogError("TreeView has not been properly intialized yet. Ensure to call Reload() before using the tree view.");
                m_WarnedUser = true;
            }
            return false;
        }

        // OnGUI entry
        public virtual void OnGUI(Rect rect)
        {
            if (!ValidTreeView())
                return;

            m_TreeView.OnEvent();

            if (showBorder)
                rect = m_GUI.DoBorder(rect);

            if (m_MultiColumnHeader != null)
                TreeViewWithMultiColumnHeader(rect);
            else
                m_TreeView.OnGUI(rect, m_TreeViewKeyControlID);

            CommandEventHandling();
        }

        public void SelectAllRows()
        {
            var rows = GetRows();
            var allowedSelection = (from treeViewItem in rows where CanMultiSelect(treeViewItem) select treeViewItem.id).ToList();
            SetSelection(allowedSelection, TreeViewSelectionOptions.FireSelectionChanged);
        }

        void TreeViewWithMultiColumnHeader(Rect rect)
        {
            Rect columnHeaderRect = new Rect(rect.x, rect.y, rect.width, m_MultiColumnHeader.height);
            Rect treeRect = new Rect(rect.x, columnHeaderRect.yMax, rect.width, rect.height - columnHeaderRect.height);

            float scrollX = Mathf.Max(m_TreeView.state.scrollPos.x, 0f); // Ensure positive scroll values (work around an issue in the scroll view that when using mousescroll it returns invalid negative values)

            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && columnHeaderRect.Contains(evt.mousePosition))
                GUIUtility.keyboardControl = m_TreeViewKeyControlID;

            m_MultiColumnHeader.OnGUI(columnHeaderRect, scrollX);
            m_TreeView.OnGUI(treeRect, m_TreeViewKeyControlID);
        }

        // GUI helper methods (for custon drawing)

        protected float GetFoldoutIndent(TreeViewItem item)
        {
            return m_GUI.GetFoldoutIndent(item);
        }

        protected float GetContentIndent(TreeViewItem item)
        {
            return m_GUI.GetContentIndent(item);
        }

        protected IList<int> SortItemIDsInRowOrder(IList<int> ids)
        {
            return m_TreeView.SortIDsInVisiblityOrder(ids);
        }

        protected void CenterRectUsingSingleLineHeight(ref Rect rect)
        {
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            if (rect.height > singleLineHeight)
            {
                rect.y += (rect.height - singleLineHeight) * 0.5f;
                rect.height = singleLineHeight;
            }
        }

        protected void AddExpandedRows(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (root == null)
                throw new ArgumentNullException("root", "root is null");

            if (rows == null)
                throw new ArgumentNullException("rows", "rows is null");

            if (root.hasChildren)
                foreach (TreeViewItem child in root.children)
                    GetExpandedRowsRecursive(child, rows);
        }

        void GetExpandedRowsRecursive(TreeViewItem item, IList<TreeViewItem> expandedRows)
        {
            if (item == null)
                Debug.LogError("Found a TreeViewItem that is null. Invalid use of AddExpandedRows(): This method is only valid to call if you have built the full tree of TreeViewItems.");

            expandedRows.Add(item);

            if (item.hasChildren && IsExpanded(item.id))
                foreach (TreeViewItem child in item.children)
                    GetExpandedRowsRecursive(child, expandedRows);
        }

        protected virtual void SelectionChanged(IList<int> selectedIds)
        {
        }

        protected virtual void DoubleClickedItem(int id)
        {
        }

        protected virtual void ContextClickedItem(int id)
        {
        }

        protected virtual void ContextClicked()
        {
        }

        protected virtual void ExpandedStateChanged()
        {
        }

        protected virtual void SearchChanged(string newSearch)
        {
        }

        protected virtual void KeyEvent()
        {
        }

        // Used to reveal an item
        protected virtual IList<int> GetAncestors(int id)
        {
            // Default behavior assumes complete tree
            return TreeViewUtility.GetParentsAboveItem(FindItem(id)).ToList();
        }

        // Used to expand children recursively below an item
        protected virtual IList<int> GetDescendantsThatHaveChildren(int id)
        {
            // Default behavior assumes complete tree
            return TreeViewUtility.GetParentsBelowItem(FindItem(id)).ToList();
        }

        TreeViewItem FindItem(int id)
        {
            if (rootItem == null)
                throw new InvalidOperationException("FindItem failed: root item has not been created yet");

            var item = TreeViewUtility.FindItem(id, rootItem);
            if (item == null)
                throw new ArgumentException(string.Format("Could not find item with id: {0}. FindItem assumes complete tree is built. Most likely the item is not allocated because it is hidden under a collapsed item. Check if GetAncestors are overriden for the tree view.", id));
            return item;
        }

        // Selection
        protected virtual bool CanMultiSelect(TreeViewItem item)
        {
            return true; // default behavior
        }

        // Renaming
        protected virtual bool CanRename(TreeViewItem item)
        {
            return false; // Default to false so user have to enable renaming if wanted
        }

        protected virtual void RenameEnded(RenameEndedArgs args)
        {
        }

        // Dragging
        protected virtual bool CanStartDrag(CanStartDragArgs args)
        {
            return false;
        }

        protected virtual void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
        }

        protected virtual DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            return DragAndDropVisualMode.None;
        }

        protected virtual bool CanBeParent(TreeViewItem item)
        {
            return true; // default behavior
        }

        protected virtual bool CanChangeExpandedState(TreeViewItem item)
        {
            // Default: Ignore expansion (foldout arrow) when showing search results
            if (m_TreeView.isSearching)
                return false;
            return item.hasChildren;
        }

        protected virtual bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            return item.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Custom GUI
        protected virtual void RowGUI(RowGUIArgs args)
        {
            m_GUI.DefaultRowGUI(args);
        }

        protected virtual void BeforeRowsGUI()
        {
            if (showAlternatingRowBackgrounds)
                m_GUI.DrawAlternatingRowBackgrounds();
        }

        protected virtual void AfterRowsGUI()
        {
        }

        protected virtual void RefreshCustomRowHeights()
        {
            if (!m_OverriddenMethods.hasGetCustomRowHeight)
                throw new InvalidOperationException("Only call RefreshCustomRowHeights if you have overridden GetCustomRowHeight to customize the height of each row.");
            m_GUI.RefreshRowRects(GetRows());
        }

        protected virtual float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return rowHeight;
        }

        protected virtual Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            return m_GUI.DefaultRenameRect(rowRect, row, item);
        }

        protected virtual void CommandEventHandling()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
                return;

            bool execute = evt.type == EventType.ExecuteCommand;

            if (HasFocus() && evt.commandName == "SelectAll")
            {
                if (execute)
                    SelectAllRows();
                evt.Use();
                GUIUtility.ExitGUI();
            }

            if (evt.commandName == "FrameSelected")
            {
                if (execute)
                {
                    if (hasSearch)
                        searchString = string.Empty;

                    if (HasSelection())
                        FrameItem(GetSelection()[0]);
                }
                evt.Use();
                GUIUtility.ExitGUI();
            }
        }

        protected static void SetupParentsAndChildrenFromDepths(TreeViewItem root, IList<TreeViewItem> rows)
        {
            TreeViewUtility.SetChildParentReferences(rows, root);
        }

        protected static void SetupDepthsFromParentsAndChildren(TreeViewItem root)
        {
            TreeViewUtility.SetDepthValuesForItems(root);
        }

        protected static List<TreeViewItem> CreateChildListForCollapsedParent()
        {
            return LazyTreeViewDataSource.CreateChildListForCollapsedParent();
        }

        protected static bool IsChildListForACollapsedParent(IList<TreeViewItem> childList)
        {
            return LazyTreeViewDataSource.IsChildListForACollapsedParent(childList);
        }

        class OverriddenMethods
        {
            public readonly bool hasRowGUI;
            public readonly bool hasHandleDragAndDrop;
            public readonly bool hasGetRenameRect;
            public readonly bool hasBuildRows;
            public readonly bool hasGetCustomRowHeight;

            public OverriddenMethods(TreeView treeView)
            {
                Type type = treeView.GetType();
                hasRowGUI = IsOverridden(type, "RowGUI");
                hasHandleDragAndDrop = IsOverridden(type, "HandleDragAndDrop");
                hasGetRenameRect = IsOverridden(type, "GetRenameRect");
                hasBuildRows = IsOverridden(type, "BuildRows");
                hasGetCustomRowHeight = IsOverridden(type, "GetCustomRowHeight");
                ValidateOverriddenMethods(treeView);
            }

            static bool IsOverridden(Type type, string methodName)
            {
                var methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo != null)
                    return methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType;

                Debug.LogError("IsOverridden: method name not found: " + methodName + " (check spelling against method declaration)");
                return false;
            }

            void ValidateOverriddenMethods(TreeView treeView)
            {
                Type type = treeView.GetType();

                bool hasCanRename = IsOverridden(type, "CanRename");
                bool hasRenameEnded = IsOverridden(type, "RenameEnded");
                if (hasRenameEnded != hasCanRename)
                {
                    if (hasCanRename)
                        Debug.LogError(type.Name + ": If you are overriding CanRename you should also override RenameEnded (to handle the renaming).");

                    if (hasRenameEnded)
                        Debug.LogError(type.Name + ": If you are overriding RenameEnded you should also override CanRename (to allow renaming).");
                }

                bool hasCanStartDragAndDrop = IsOverridden(type, "CanStartDrag");
                bool hasSetupDragAndDrop = IsOverridden(type, "SetupDragAndDrop");
                if (hasCanStartDragAndDrop != hasSetupDragAndDrop)
                {
                    if (hasCanStartDragAndDrop)
                        Debug.LogError(type.Name + ": If you are overriding CanStartDrag you should also override SetupDragAndDrop (to setup the drag).");

                    if (hasSetupDragAndDrop)
                        Debug.LogError(type.Name + ": If you are overriding SetupDragAndDrop you should also override CanStartDrag (to allow dragging).");
                }
            }
        };

        // Nested because they are used by protected methods
        protected struct RowGUIArgs
        {
            public TreeViewItem item;
            public string label;
            public Rect rowRect;
            public int row;
            public bool selected;
            public bool focused;
            public bool isRenaming;
            internal MultiColumnInfo columnInfo { get; set; }

            public int GetNumVisibleColumns()
            {
                if (!HasMultiColumnInfo())
                    throw new NotSupportedException("Only call this method if you are using a MultiColumnHeader with the TreeView.");

                return columnInfo.multiColumnHeaderState.visibleColumns.Length;
            }

            public int GetColumn(int visibleColumnIndex)
            {
                if (!HasMultiColumnInfo())
                    throw new NotSupportedException("Only call this method if you are using a MultiColumnHeader with the TreeView.");

                return columnInfo.multiColumnHeaderState.visibleColumns[visibleColumnIndex];
            }

            public Rect GetCellRect(int visibleColumnIndex)
            {
                if (!HasMultiColumnInfo())
                    throw new NotSupportedException("Only call this method if you are using a MultiColumnHeader with the TreeView.");

                return columnInfo.cellRects[visibleColumnIndex];
            }

            internal bool HasMultiColumnInfo()
            {
                return columnInfo.multiColumnHeaderState != null;
            }

            internal struct MultiColumnInfo
            {
                internal MultiColumnInfo(MultiColumnHeaderState multiColumnHeaderState, Rect[] cellRects)
                {
                    this.multiColumnHeaderState = multiColumnHeaderState;
                    this.cellRects = cellRects;
                }

                public MultiColumnHeaderState multiColumnHeaderState;
                public Rect[] cellRects;
            }
        }

        protected struct DragAndDropArgs
        {
            public DragAndDropPosition dragAndDropPosition;
            public TreeViewItem parentItem;
            public int insertAtIndex;
            public bool performDrop;
        }

        protected struct SetupDragAndDropArgs
        {
            public IList<int> draggedItemIDs;
        }

        protected struct CanStartDragArgs
        {
            public TreeViewItem draggedItem;
            public IList<int> draggedItemIDs;
        }

        protected struct RenameEndedArgs
        {
            public bool acceptedRename;
            public int itemID;
            public string originalName;
            public string newName;
        }

        protected enum DragAndDropPosition
        {
            UponItem,
            BetweenItems,
            OutsideItems
        }
    }

    [Flags]
    public enum TreeViewSelectionOptions
    {
        None = 0,
        FireSelectionChanged = 1,
        RevealAndFrame = 2
    }
} // UnityEditor
