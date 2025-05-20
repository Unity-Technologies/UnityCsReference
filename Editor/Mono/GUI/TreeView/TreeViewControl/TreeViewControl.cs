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

    public abstract partial class TreeView<TIdentifier> where TIdentifier : unmanaged, System.IEquatable<TIdentifier>
    {
        public delegate bool DoFoldoutCallback(Rect position, bool expandedState, GUIStyle style);
        public delegate List<TIdentifier> GetNewSelectionFunction(TreeViewItem<TIdentifier> clickedItem, bool keepMultiSelection, bool useActionKeyAsShift);

        protected GetNewSelectionFunction getNewSelectionOverride
        {
            set { m_TreeView.getNewSelectionOverride = (x, y, z) => value(x, y, z); }
        }

        internal bool deselectOnUnhandledMouseDown
        {
            set
            {
                if (m_TreeView != null)
                    m_TreeView.deselectOnUnhandledMouseDown = value;
            }
        }

        internal TreeViewController<TIdentifier> m_TreeView;
        TreeViewControlDataSource m_DataSource;
        TreeViewControlGUI m_GUI;
        TreeViewControlDragging m_Dragging;
        MultiColumnHeader m_MultiColumnHeader;
        List<TreeViewItem<TIdentifier>> m_DefaultRows;
        int m_TreeViewKeyControlID;

        OverriddenMethods m_OverriddenMethods;
        protected DoFoldoutCallback foldoutOverride { get; set; }

        public TreeView(TreeViewState<TIdentifier> state)
        {
            Init(state);
        }

        public TreeView(TreeViewState<TIdentifier> state, MultiColumnHeader multiColumnHeader)
        {
            m_MultiColumnHeader = multiColumnHeader;
            Init(state);
        }

        void Init(TreeViewState<TIdentifier> state)
        {
            if (state == null)
                throw new ArgumentNullException("state", "Invalid TreeViewState: it is null");

            m_TreeView = new TreeViewController<TIdentifier>(null, state);
            m_DataSource = new TreeViewControlDataSource(m_TreeView, this);
            m_GUI = new TreeViewControlGUI(m_TreeView, this);
            m_Dragging = new TreeViewControlDragging(m_TreeView, this);
            m_TreeView.Init(new Rect(), m_DataSource, m_GUI, m_Dragging);

            m_TreeView.searchChanged += SearchChanged;
            m_TreeView.selectionChangedCallback += SelectionChanged;
            m_TreeView.itemSingleClickedCallback += SingleClickedItem;
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
        protected abstract TreeViewItem<TIdentifier> BuildRoot();


        // Default implementation of BuildRows assumes full tree was built in BuildRoot. With full tree we can also support search out of the box
        protected virtual IList<TreeViewItem<TIdentifier>> BuildRows(TreeViewItem<TIdentifier> root) => BuildRowsInternal(root);
        internal virtual IList<TreeViewItem<TIdentifier>> BuildRowsInternal(TreeViewItem<TIdentifier> root)
        {
            // Reuse cached list (for capacity)
            if (m_DefaultRows == null)
                m_DefaultRows = new List<TreeViewItem<TIdentifier>>(100);
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

        public TreeViewState<TIdentifier> state { get { return m_TreeView.state; } }
        public MultiColumnHeader multiColumnHeader { get { return m_MultiColumnHeader; } set { m_MultiColumnHeader = value; }}

        // internal for testing
        internal TreeViewController<TIdentifier> controller => m_TreeView;

        protected TreeViewItem<TIdentifier> rootItem
        {
            get { return m_TreeView.data.root; }
        }

        protected TreeViewItem<TIdentifier> hoveredItem
        {
            get { return m_TreeView.hoveredItem; }
        }

        protected bool enableItemHovering
        {
            get { return m_TreeView.enableItemHovering; }
            set { m_TreeView.enableItemHovering = value; }
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

        internal bool drawSelection
        {
            get => m_GUI.drawSelection;
            set => m_GUI.drawSelection = value;
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

        protected bool useScrollView
        {
            get { return m_TreeView.useScrollView; }
            set { m_TreeView.useScrollView = value; }
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
            set { m_GUI.k_IndentWidth = value; }
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

        public virtual IList<TreeViewItem<TIdentifier>> GetRows() => GetRowsInternal();

        internal virtual IList<TreeViewItem<TIdentifier>> GetRowsInternal()
        {
            if (!isInitialized)
                return null;

            return m_TreeView.data.GetRows();
        }

        protected IList<TreeViewItem<TIdentifier>> FindRows(IList<TIdentifier> ids)
        {
            return GetRows().Where(item => ids.Contains(item.id)).ToList();
        }

        protected TreeViewItem<TIdentifier> FindItem(TIdentifier id, TreeViewItem<TIdentifier> searchFromThisItem)
        {
            return TreeViewUtility<TIdentifier>.FindItem(id, searchFromThisItem);
        }

        protected int FindRowOfItem(TreeViewItem<TIdentifier> item)
        {
            return GetRows().IndexOf(item);
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
            SetExpanded(new TIdentifier[0]);
        }

        public void SetExpandedRecursive(TIdentifier id, bool expanded)
        {
            m_DataSource.SetExpandedWithChildren(id, expanded);
        }

        public bool SetExpanded(TIdentifier id, bool expanded)
        {
            return m_DataSource.SetExpanded(id, expanded);
        }

        public void SetExpanded(IList<TIdentifier> ids)
        {
            m_DataSource.SetExpandedIDs(ids.ToArray());
        }

        public IList<TIdentifier> GetExpanded()
        {
            return m_DataSource.GetExpandedIDs();
        }

        public bool IsExpanded(TIdentifier id)
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
        public IList<TIdentifier> GetSelection()
        {
            return m_TreeView.GetSelection();
        }

        public void SetSelection(IList<TIdentifier> selectedIDs)
        {
            SetSelection(selectedIDs, TreeViewSelectionOptions.None);
        }

        public void SetSelection(IList<TIdentifier> selectedIDs, TreeViewSelectionOptions options)
        {
            bool fireSelectionChanged = (options & TreeViewSelectionOptions.FireSelectionChanged) != 0;
            bool revealSelectionAndFrameLastSelected = (options & TreeViewSelectionOptions.RevealAndFrame) != 0;
            bool animatedFraming = false;

            m_TreeView.SetSelection(selectedIDs.ToArray(), revealSelectionAndFrameLastSelected, animatedFraming);
            if (fireSelectionChanged)
                m_TreeView.NotifyListenersThatSelectionChanged();
        }

        public bool IsSelected(TIdentifier id)
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

        protected void SelectionClick(TreeViewItem<TIdentifier> item, bool keepMultiSelection)
        {
            m_TreeView.SelectionClick(item, keepMultiSelection);
        }

        // Rename interface
        public bool BeginRename(TreeViewItem<TIdentifier> item)
        {
            return BeginRename(item, 0f);
        }

        public bool BeginRename(TreeViewItem<TIdentifier> item, float delay)
        {
            return m_GUI.BeginRename(item, delay);
        }

        public void EndRename()
        {
            m_GUI.EndRename();
        }

        // Frame interface
        public void FrameItem(TIdentifier id)
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

        protected float GetFoldoutIndent(TreeViewItem<TIdentifier> item)
        {
            return m_GUI.GetFoldoutIndent(item);
        }

        protected float GetContentIndent(TreeViewItem<TIdentifier> item)
        {
            return m_GUI.GetContentIndent(item);
        }

        protected IList<TIdentifier> SortItemIDsInRowOrder(IList<TIdentifier> ids)
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

        protected void AddExpandedRows(TreeViewItem<TIdentifier> root, IList<TreeViewItem<TIdentifier>> rows)
        {
            if (root == null)
                throw new ArgumentNullException("root", "root is null");

            if (rows == null)
                throw new ArgumentNullException("rows", "rows is null");

            if (root.hasChildren)
                foreach (TreeViewItem<TIdentifier> child in root.children)
                    GetExpandedRowsRecursive(child, rows);
        }

        void GetExpandedRowsRecursive(TreeViewItem<TIdentifier> item, IList<TreeViewItem<TIdentifier>> expandedRows)
        {
            if (item == null)
                Debug.LogError("Found a TreeViewItem<TIdentifier> that is null. Invalid use of AddExpandedRows(): This method is only valid to call if you have built the full tree of TreeViewItems.");

            expandedRows.Add(item);

            if (item.hasChildren && IsExpanded(item.id))
                foreach (TreeViewItem<TIdentifier> child in item.children)
                    GetExpandedRowsRecursive(child, expandedRows);
        }

        protected virtual void SelectionChanged(IList<TIdentifier> selectedIds)
        {
        }

        protected virtual void SingleClickedItem(TIdentifier id)
        {
        }

        protected virtual void DoubleClickedItem(TIdentifier id)
        {
        }

        protected virtual void ContextClickedItem(TIdentifier id)
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
        protected virtual IList<TIdentifier> GetAncestors(TIdentifier id)
        {
            var item = FindItem(id);
            if (item == null)
                return new List<TIdentifier>();

            // Default behavior assumes complete tree
            HashSet<TIdentifier> parentsAbove = new HashSet<TIdentifier>();
            TreeViewUtility<TIdentifier>.GetParentsAboveItem(item, parentsAbove);
            return parentsAbove.ToArray();
        }

        // Used to expand children recursively below an item
        protected virtual IList<TIdentifier> GetDescendantsThatHaveChildren(TIdentifier id)
        {
            // Default behavior assumes complete tree
            HashSet<TIdentifier> parentsBelow = new HashSet<TIdentifier>();
            TreeViewUtility<TIdentifier>.GetParentsBelowItem(FindItem(id), parentsBelow);
            return parentsBelow.ToArray();
        }

        TreeViewItem<TIdentifier> FindItem(TIdentifier id)
        {
            if (rootItem == null)
                throw new InvalidOperationException("FindItem failed: root item has not been created yet");

            return TreeViewUtility<TIdentifier>.FindItem(id, rootItem);
        }

        // Selection
        protected virtual bool CanMultiSelect(TreeViewItem<TIdentifier> item) => CanMultiSelectInternal(item);

        internal virtual bool CanMultiSelectInternal(TreeViewItem<TIdentifier> item)
        {
            return true; // default behavior
        }

        // Renaming
        protected virtual bool CanRename(TreeViewItem<TIdentifier> item) => CanRenameInternal(item);

        internal virtual bool CanRenameInternal(TreeViewItem<TIdentifier> item)
        {
            return false; // Default to false so user have to enable renaming if wanted
        }

        protected virtual void RenameEnded(RenameEndedArgs args)
        {
        }

        // Dragging
        protected virtual bool CanStartDrag(CanStartDragArgs args) => CanStartDragInternal(args);

        internal virtual bool CanStartDragInternal(CanStartDragArgs args)
        {
            return false;
        }

        protected virtual void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
        }

        protected virtual DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args) => HandleDragAndDropInternal(args);
        internal virtual DragAndDropVisualMode HandleDragAndDropInternal(DragAndDropArgs args)
        {
            return DragAndDropVisualMode.None;
        }

        protected virtual bool CanBeParent(TreeViewItem<TIdentifier> item) => CanBeParentInternal(item);

        internal virtual bool CanBeParentInternal(TreeViewItem<TIdentifier> item)
        {
            return true; // default behavior
        }

        protected virtual bool CanChangeExpandedState(TreeViewItem<TIdentifier> item) => CanChangeExpandedStateInternal(item);

        internal virtual bool CanChangeExpandedStateInternal(TreeViewItem<TIdentifier> item)
        {
            // Default: Ignore expansion (foldout arrow) when showing search results
            if (m_TreeView.isSearching)
                return false;
            return item.hasChildren;
        }

        protected virtual bool DoesItemMatchSearch(TreeViewItem<TIdentifier> item, string search) => DoesItemMatchSearchInternal(item, search);

        internal virtual bool DoesItemMatchSearchInternal(TreeViewItem<TIdentifier> item, string search)
        {
            return item.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Custom GUI
        protected virtual void RowGUI(RowGUIArgs args) => RowGUIInternal(args);

        internal virtual void RowGUIInternal(RowGUIArgs args)
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

        protected virtual float GetCustomRowHeight(int row, TreeViewItem<TIdentifier> item) => GetCustomRowHeightInternal(row, item);

        internal virtual float GetCustomRowHeightInternal(int row, TreeViewItem<TIdentifier> item)
        {
            return rowHeight;
        }

        protected virtual Rect GetRenameRect(Rect rowRect, int row, TreeViewItem<TIdentifier> item) => GetRenameRectInternal(rowRect, row, item);

        internal virtual Rect GetRenameRectInternal(Rect rowRect, int row, TreeViewItem<TIdentifier> item)
        {
            return m_GUI.DefaultRenameRect(rowRect, row, item);
        }

        protected virtual void CommandEventHandling()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
                return;

            bool execute = evt.type == EventType.ExecuteCommand;

            if (HasFocus() && evt.commandName == EventCommandNames.SelectAll)
            {
                if (execute)
                    SelectAllRows();
                evt.Use();
                GUIUtility.ExitGUI();
            }

            if (evt.commandName == EventCommandNames.FrameSelected)
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

        protected static void SetupParentsAndChildrenFromDepths(TreeViewItem<TIdentifier> root, IList<TreeViewItem<TIdentifier>> rows)
        {
            TreeViewUtility<TIdentifier>.SetChildParentReferences(rows, root);
        }

        protected static void SetupDepthsFromParentsAndChildren(TreeViewItem<TIdentifier> root)
        {
            TreeViewUtility<TIdentifier>.SetDepthValuesForItems(root);
        }

        protected static List<TreeViewItem<TIdentifier>> CreateChildListForCollapsedParent()
        {
            return LazyTreeViewDataSource<TIdentifier>.CreateChildListForCollapsedParent();
        }

        protected static bool IsChildListForACollapsedParent(IList<TreeViewItem<TIdentifier>> childList)
        {
            return LazyTreeViewDataSource<TIdentifier>.IsChildListForACollapsedParent(childList);
        }

        class OverriddenMethods
        {
            public readonly bool hasRowGUI;
            public readonly bool hasHandleDragAndDrop;
            public readonly bool hasGetRenameRect;
            public readonly bool hasBuildRows;
            public readonly bool hasGetCustomRowHeight;

            public OverriddenMethods(TreeView<TIdentifier> treeView)
            {
                Type type = treeView.GetType();
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
                bool hasCanRename = false, hasRenameEnded = false;
                bool hasCanStartDragAndDrop = false, hasSetupDragAndDrop = false;

                foreach (var method in methods)
                {
                    if (method.GetBaseDefinition().DeclaringType == method.DeclaringType)
                        continue;

                    switch (method.Name)
                    {
                        case "RowGUI":
                        {
                            hasRowGUI = true;
                            break;
                        }
                        case "HandleDragAndDrop":
                        {
                            hasHandleDragAndDrop = true;
                            break;
                        }
                        case "GetRenameRect":
                        {
                            hasGetRenameRect = true;
                            break;
                        }
                        case "BuildRows":
                        {
                            hasBuildRows = true;
                            break;
                        }
                        case "GetCustomRowHeight":
                        {
                            hasGetCustomRowHeight = true;
                            break;
                        }
                        case "CanRename":
                        {
                            hasCanRename = true;
                            break;
                        }
                        case "RenameEnded":
                        {
                            hasRenameEnded = true;
                            break;
                        }
                        case "CanStartDrag":
                        {
                            hasCanStartDragAndDrop = true;
                            break;
                        }
                        case "SetupDragAndDrop":
                        {
                            hasSetupDragAndDrop = true;
                            break;
                        }
                    }
                }

                if (hasRenameEnded != hasCanRename)
                {
                    if (hasCanRename)
                        Debug.LogError(type.Name + ": If you are overriding CanRename you should also override RenameEnded (to handle the renaming).");

                    if (hasRenameEnded)
                        Debug.LogError(type.Name + ": If you are overriding RenameEnded you should also override CanRename (to allow renaming).");
                }

                if (hasCanStartDragAndDrop != hasSetupDragAndDrop)
                {
                    if (hasCanStartDragAndDrop)
                        Debug.LogError(type.Name + ": If you are overriding CanStartDrag you should also override SetupDragAndDrop (to setup the drag).");

                    if (hasSetupDragAndDrop)
                        Debug.LogError(type.Name + ": If you are overriding SetupDragAndDrop you should also override CanStartDrag (to allow dragging).");
                }
            }
        }

        // Nested because they are used by protected methods
        protected internal struct RowGUIArgs
        {
            public TreeViewItem<TIdentifier> item;
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

        protected internal struct DragAndDropArgs
        {
            public DragAndDropPosition dragAndDropPosition;
            public TreeViewItem<TIdentifier> parentItem;
            public int insertAtIndex;
            public bool performDrop;
        }

        protected struct SetupDragAndDropArgs
        {
            public IList<TIdentifier> draggedItemIDs;
        }

        protected internal struct CanStartDragArgs
        {
            public TreeViewItem<TIdentifier> draggedItem;
            public IList<TIdentifier> draggedItemIDs;
        }

        protected struct RenameEndedArgs
        {
            public bool acceptedRename;
            public TIdentifier itemID;
            public string originalName;
            public string newName;
        }

        protected internal enum DragAndDropPosition
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
