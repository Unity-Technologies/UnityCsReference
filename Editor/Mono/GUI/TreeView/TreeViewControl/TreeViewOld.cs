// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls;

[Obsolete("TreeView is now deprecated. You can likely now use TreeView<int> instead and not think more about it. But if you were using that identifier to store InstanceID data, you should instead opt to upgrade your TreeViews to use TreeView<InstanceID> to get the proper typing.")]
public abstract partial class TreeView : TreeViewInternal
{
    protected TreeView(TreeViewState state)
        : base(state) { }

    protected TreeView(TreeViewState state, MultiColumnHeader multiColumnHeader)
        : base(state, multiColumnHeader) { }

    protected new struct RowGUIArgs
    {
        public TreeViewItem item;
        public string label;
        public Rect rowRect;
        public int row;
        public bool selected;
        public bool focused;
        public bool isRenaming;
        internal TreeView<int>.RowGUIArgs.MultiColumnInfo columnInfo;

        public int GetNumVisibleColumns()=> ((TreeView<int>.RowGUIArgs)this).GetNumVisibleColumns();
        public int GetColumn(int visibleColumnIndex) => ((TreeView<int>.RowGUIArgs)this).GetColumn(visibleColumnIndex);
        public Rect GetCellRect(int visibleColumnIndex) => ((TreeView<int>.RowGUIArgs)this).GetCellRect(visibleColumnIndex);

        // a cast from RowGUIArgs to TreeView<int>.RowGUIArgs
        public static implicit operator TreeView<int>.RowGUIArgs(RowGUIArgs args) => new()
        {
            item = args.item,
            label = args.label,
            rowRect = args.rowRect,
            row = args.row,
            selected = args.selected,
            focused = args.focused,
            isRenaming = args.isRenaming,
            columnInfo = args.columnInfo
        };

        public static implicit operator RowGUIArgs(TreeView<int>.RowGUIArgs args) => new()
        {
            item = args.item as TreeViewItem,
            label = args.label,
            rowRect = args.rowRect,
            row = args.row,
            selected = args.selected,
            focused = args.focused,
            isRenaming = args.isRenaming,
            columnInfo = args.columnInfo
        };
    }

    protected new struct DragAndDropArgs
    {
        public DragAndDropPosition dragAndDropPosition;
        public TreeViewItem parentItem;
        public int insertAtIndex;
        public bool performDrop;

        // a cast from DragAndDropArgs to TreeView<int>.DragAndDropArgs
        public static implicit operator TreeView<int>.DragAndDropArgs(DragAndDropArgs args)
        {
            return new TreeView<int>.DragAndDropArgs
            {
                dragAndDropPosition = args.dragAndDropPosition,
                parentItem = args.parentItem,
                insertAtIndex = args.insertAtIndex,
                performDrop = args.performDrop
            };
        }
        public static implicit operator DragAndDropArgs(TreeView<int>.DragAndDropArgs args)
        {
            return new DragAndDropArgs
            {
                dragAndDropPosition = args.dragAndDropPosition,
                parentItem = args.parentItem as TreeViewItem,
                insertAtIndex = args.insertAtIndex,
                performDrop = args.performDrop
            };
        }
    }

    protected new struct CanStartDragArgs
    {
        public TreeViewItem draggedItem;
        public IList<int> draggedItemIDs;

        // a cast from CanStartDragArgs to TreeView<TIdentifier>.CanStartDragArgs
        public static implicit operator TreeView<int>.CanStartDragArgs(CanStartDragArgs args)
        {
            return new TreeView<int>.CanStartDragArgs
            {
                draggedItem = args.draggedItem,
                draggedItemIDs = args.draggedItemIDs
            };
        }

        public static implicit operator CanStartDragArgs(TreeView<int>.CanStartDragArgs args)
        {
            return new CanStartDragArgs
            {
                draggedItem = args.draggedItem as TreeViewItem,
                draggedItemIDs = args.draggedItemIDs
            };
        }
    }

    public new delegate List<int> GetNewSelectionFunction(TreeViewItem clickedItem, bool keepMultiSelection, bool useActionKeyAsShift);

    protected new GetNewSelectionFunction getNewSelectionOverride
    {
        set
        {
            m_TreeView.getNewSelectionOverride = (x, y, z) => value(x as TreeViewItem, y, z);
        }
    }

    protected new abstract TreeViewItem BuildRoot();

    internal override TreeViewItem BuildRootInternal() => BuildRoot();

    internal override IList<TreeViewItem<int>> BuildRowsInternal(TreeViewItem<int> root) => BuildRows(root as TreeViewItem).ToGenericIList();
    protected virtual IList<TreeViewItem> BuildRows(TreeViewItem root) => base.BuildRowsInternal(root).ToNonGenericIList();

    public new TreeViewState state => base.state as TreeViewState;
    protected new TreeViewItem rootItem => base.rootItem as TreeViewItem;
    protected new TreeViewItem hoveredItem => base.hoveredItem as TreeViewItem;

    public new virtual IList<TreeViewItem> GetRows() => base.GetRowsInternal().ToNonGenericIList();
    internal override IList<TreeViewItem<int>> GetRowsInternal() => GetRows().ToGenericIList();

    protected new IList<TreeViewItem> FindRows(IList<int> ids) => base.FindRows(ids).ToNonGenericIList();
    protected TreeViewItem FindItem(int id, TreeViewItem searchFromThisItem) => base.FindItem(id, searchFromThisItem) as TreeViewItem;
    protected int FindRowOfItem(TreeViewItem item) => base.FindRowOfItem(item);
    protected void SelectionClick(TreeViewItem item, bool keepMultiSelection) => base.SelectionClick(item, keepMultiSelection);
    public bool BeginRename(TreeViewItem item) => base.BeginRename(item);
    public bool BeginRename(TreeViewItem item, float delay) => base.BeginRename(item, delay);
    protected float GetFoldoutIndent(TreeViewItem item) => base.GetFoldoutIndent(item);
    protected float GetContentIndent(TreeViewItem item) => base.GetContentIndent(item);
    protected void AddExpandedRows(TreeViewItem root, IList<TreeViewItem> rows) => base.AddExpandedRows(root, rows.ToGenericIList());

    protected virtual bool CanMultiSelect(TreeViewItem item) => base.CanMultiSelectInternal(item);
    internal override bool CanMultiSelectInternal(TreeViewItem<int> item) => CanMultiSelect(item as TreeViewItem);
    protected virtual bool CanRename(TreeViewItem item) => base.CanRenameInternal(item);
    internal override bool CanRenameInternal(TreeViewItem<int> item) => CanRename(item as TreeViewItem);

    protected virtual bool CanStartDrag(CanStartDragArgs args) => base.CanStartDragInternal(args);

    internal override bool CanStartDragInternal(TreeView<int>.CanStartDragArgs args) => CanStartDrag(args);
    protected virtual DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args) => base.HandleDragAndDropInternal(args);
    internal override DragAndDropVisualMode HandleDragAndDropInternal(TreeView<int>.DragAndDropArgs args) => HandleDragAndDrop(args);
    protected virtual bool CanBeParent(TreeViewItem item) => base.CanBeParentInternal(item);
    internal override bool CanBeParentInternal(TreeViewItem<int> item) => CanBeParent(item as TreeViewItem);
    protected virtual bool CanChangeExpandedState(TreeViewItem item) => base.CanChangeExpandedStateInternal(item);
    internal override bool CanChangeExpandedStateInternal(TreeViewItem<int> item) => CanChangeExpandedState(item as TreeViewItem);
    protected virtual bool DoesItemMatchSearch(TreeViewItem item, string search) => base.DoesItemMatchSearchInternal(item, search);
    internal override bool DoesItemMatchSearchInternal(TreeViewItem<int> item, string search) => DoesItemMatchSearch(item as TreeViewItem, search);
    protected virtual void RowGUI(RowGUIArgs args) => base.RowGUIInternal(args);
    internal override void RowGUIInternal(TreeView<int>.RowGUIArgs args) => RowGUI(args);
    protected virtual float GetCustomRowHeight(int row, TreeViewItem item) => base.GetCustomRowHeightInternal(row, item);
    internal override float GetCustomRowHeightInternal(int row, TreeViewItem<int> item) => GetCustomRowHeight(row, item as TreeViewItem);
    protected virtual Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item) => base.GetRenameRectInternal(rowRect, row, item);
    internal override Rect GetRenameRectInternal(Rect rowRect, int row, TreeViewItem<int> item) => GetRenameRect(rowRect, row, item as TreeViewItem);
    protected static void SetupParentsAndChildrenFromDepths(TreeViewItem root, IList<TreeViewItem> rows) => TreeView<int>.SetupParentsAndChildrenFromDepths(root, rows.ToGenericIList());
    protected static void SetupDepthsFromParentsAndChildren(TreeViewItem root) => TreeView<int>.SetupDepthsFromParentsAndChildren(root);
    protected new static List<TreeViewItem> CreateChildListForCollapsedParent() => TreeView<int>.CreateChildListForCollapsedParent().ToNonGenericList();
    protected static bool IsChildListForACollapsedParent(IList<TreeViewItem> childList) => TreeView<int>.IsChildListForACollapsedParent(childList.ToGenericIList());
}

[Obsolete("TreeView is now deprecated. You can likely now use TreeView<int> instead and not think more about it. But if you were using that identifier to store InstanceID data, you should instead opt to upgrade your TreeViews to use TreeView<InstanceID> to get the proper typing.")]
public abstract class TreeViewInternal : TreeView<int>
{
    protected TreeViewInternal(TreeViewState state)
        : base(state) { }

    protected TreeViewInternal(TreeViewState state, MultiColumnHeader multiColumnHeader)
        : base(state, multiColumnHeader) { }

    protected override TreeViewItem<int> BuildRoot() => BuildRootInternal();

    internal abstract TreeViewItem BuildRootInternal();
}

[Obsolete]
static class TreeViewItemConvertUtility {
    public static List<TreeViewItem> ToNonGenericList(this List<TreeViewItem<int>> items) => (GenericTreeViewItemToNonGenericTreeViewItemList)items;
    public static List<TreeViewItem<int>> ToGenericList(this List<TreeViewItem> items) => (GenericTreeViewItemToNonGenericTreeViewItemList)items;
    public static IList<TreeViewItem> ToNonGenericIList(this IList<TreeViewItem<int>> items) => new GenericTreeViewItemToNonGenericTreeViewItemIList(items).nonGeneric;
    public static IList<TreeViewItem<int>> ToGenericIList(this IList<TreeViewItem> items) => new GenericTreeViewItemToNonGenericTreeViewItemIList(items).generic;
}

[StructLayout(LayoutKind.Explicit)]
[Obsolete]
struct GenericTreeViewItemToNonGenericTreeViewItemList
{
    [FieldOffset(0)] public List<TreeViewItem> nonGeneric;
    [FieldOffset(0)] public List<TreeViewItem<int>> generic;

    public static implicit operator GenericTreeViewItemToNonGenericTreeViewItemList(List<TreeViewItem> items) => new() {nonGeneric=items};
    public static implicit operator GenericTreeViewItemToNonGenericTreeViewItemList(List<TreeViewItem<int>> items) => new() {generic = items};
    public static implicit operator List<TreeViewItem>(GenericTreeViewItemToNonGenericTreeViewItemList value) => value.nonGeneric;
    public static implicit operator List<TreeViewItem<int>>(GenericTreeViewItemToNonGenericTreeViewItemList value) => value.generic;
}

[StructLayout(LayoutKind.Explicit)]
[Obsolete]
struct GenericTreeViewItemToNonGenericTreeViewItemIList
{
    [FieldOffset(0)] public IList<TreeViewItem> nonGeneric;
    [FieldOffset(0)] public IList<TreeViewItem<int>> generic;

    public GenericTreeViewItemToNonGenericTreeViewItemIList(IList<TreeViewItem> items)
    {
        generic = null;
        nonGeneric=items;
    }

    public GenericTreeViewItemToNonGenericTreeViewItemIList(IList<TreeViewItem<int>> items)
    {
        nonGeneric = null;
        generic = items;
    }
}

[Obsolete("TreeViewItem is now deprecated. You can likely now use TreeViewItem<int> instead and not think more about it. But if you were using that identifier to store InstanceID data, you should instead opt to upgrade your TreeViews to use TreeViewItem<InstanceID> to get the proper typing.")]
public class TreeViewItem : TreeViewItem<int>
{
    public new virtual List<TreeViewItem> children
    {
        get => base.childrenInternal.ToNonGenericList();
        set => base.childrenInternal = value.ToGenericList();
    }
    internal override List<TreeViewItem<int>> childrenInternal
    {
        get => children.ToGenericList();
        set => children = value.ToNonGenericList();
    }


    internal override TreeViewItem<int> ParentInternal
    {
        get => parent;
        set => parent = value as TreeViewItem;
    }

    public new virtual TreeViewItem parent { get { return base.ParentInternal as TreeViewItem; } set { base.ParentInternal = value; } }

    public void AddChild(TreeViewItem child) => base.AddChild(child);


    internal override int CompareToInternal(TreeViewItem<int> other) => CompareTo(other as TreeViewItem);

    public virtual int CompareTo(TreeViewItem other) => base.CompareToInternal(other);

    public TreeViewItem() { }

    public TreeViewItem(int id) : base(id){}
    public TreeViewItem(int id, int depth): base(id, depth) { }
    public TreeViewItem(int id, int depth, string displayName) : base(id, depth, displayName) { }
    internal TreeViewItem(int id, int depth, TreeViewItem parent, string displayName) : base(id, depth, parent, displayName) { }
}

[Obsolete("TreeViewState is now deprecated. You can likely now use TreeViewState<int> instead and not think more about it. But if you were using that identifier to store InstanceID data, you should instead opt to upgrade your TreeViews to use TreeViewState<InstanceID> to get the proper typing.")]
public class TreeViewState : TreeViewState<int> {}

[Obsolete]
internal class TreeViewController : TreeViewController<int>
{
    public TreeViewController(EditorWindow editorWindow, TreeViewState treeViewState) : base(editorWindow, treeViewState)
    {

    }

    public void Init(Rect rect, ITreeViewDataSource data, ITreeViewGUI gui, ITreeViewDragging<int> dragging) => base.Init(rect, data, new TreeViewGUIAbstract(gui), dragging);
    public new ITreeViewDataSource data { get =>base.data as ITreeViewDataSource; set => base.data = value; }
    public new TreeViewState state { get =>base.state as TreeViewState; set => base.state = value; }
}

[Obsolete]
internal abstract class TreeViewDataSource : TreeViewDataSource<int>, ITreeViewDataSource
{
    protected TreeViewDataSource(TreeViewController treeView)
        : base(treeView) { }

    public new TreeViewItem root => base.root as TreeViewItem;
    public new virtual IList<TreeViewItem> GetRows() => base.GetRowsInternal().ToNonGenericIList();
    public override IList<TreeViewItem<int>> GetRowsInternal() => GetRows().ToGenericIList();
    protected new TreeViewItem m_RootItem { get => base.m_RootItem as TreeViewItem; set => base.m_RootItem = value; }
    protected new TreeViewController m_TreeView { get => m_TreeViewInternal as TreeViewController; set => m_TreeViewInternal = value; }
    public virtual bool IsExpanded(TreeViewItem item) => base.IsExpandedInternal(item);
    public override bool IsExpandedInternal(TreeViewItem<int> item) => IsExpanded(item as TreeViewItem);
    public virtual bool IsExpandable(TreeViewItem item) => base.IsExpandableInternal(item);
    public override bool IsExpandableInternal(TreeViewItem<int> item) => IsExpandable(item as TreeViewItem);
    public virtual bool CanBeParent(TreeViewItem item) => base.CanBeParentInternal(item);
    public override bool CanBeParentInternal(TreeViewItem<int> item) => CanBeParent(item as TreeViewItem);
}

[Obsolete]
internal abstract class TreeViewDragging : TreeViewDragging<int>
{
    public TreeViewDragging(TreeViewController<int> treeView) : base(treeView)
    {
    }

    public virtual bool CanStartDrag(TreeViewItem targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition) => base.CanStartDragInternal(targetItem, draggedItemIDs, mouseDownPosition);
    public override bool CanStartDragInternal(TreeViewItem<int> targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition)=> CanStartDrag(targetItem as TreeViewItem, draggedItemIDs, mouseDownPosition);
    public override void StartDragInternal(TreeViewItem<int> draggedItem, List<int> draggedItemIDs) => StartDrag(draggedItem as TreeViewItem, draggedItemIDs);
    public virtual void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs) => base.StartDragInternal(draggedItem, draggedItemIDs);
    public virtual DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPosition) => base.DoDragInternal(parentItem, targetItem, perform, dropPosition);
    public override DragAndDropVisualMode DoDragInternal(TreeViewItem<int> parentItem, TreeViewItem<int> targetItem, bool perform, DropPosition dropPosition) => DoDrag(parentItem as TreeViewItem, targetItem as TreeViewItem, perform, dropPosition);
    public virtual bool DragElement(TreeViewItem targetItem, Rect targetItemRect, int row) => base.DragElementInternal(targetItem, targetItemRect, row);
    public override bool DragElementInternal(TreeViewItem<int> targetItem, Rect targetItemRect, int row) => DragElement(targetItem as TreeViewItem, targetItemRect, row);
}

[Obsolete]
internal interface ITreeViewGUI
{
    void OnInitialize();
    Vector2 GetTotalSize();
    void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible);
    Rect GetRowRect(int row, float rowWidth);
    Rect GetRectForFraming(int row);

    void BeginPingItem(TreeViewItem item, float topPixelOfRow, float availableWidth);
    void EndPingItem();
    bool BeginRename(TreeViewItem item, float delay);
    void EndRename();
    float GetContentIndent(TreeViewItem item);
    int GetNumRowsOnPageUpDown(TreeViewItem fromItem, bool pageUp, float heightOfTreeView);
    Rect GetRenameRect(Rect rect, int row, TreeViewItem item);
    void OnRowGUI(Rect rect, TreeViewItem item, int row, bool selected, bool focused);
    void BeginRowGUI();                                                                     // use for e.g clearing state before OnRowGUI calls
    void EndRowGUI();                                                                       // use for handling stuff after all rows have had their OnRowGUI
    float halfDropBetweenHeight { get; }
    float topRowMargin { get; }
    float bottomRowMargin { get; }
}

[Obsolete]
internal class TreeViewGUIAbstract : ITreeViewGUI<int>
{
    ITreeViewGUI treeViewGUI;

    public TreeViewGUIAbstract(ITreeViewGUI treeViewGUI)
    {
        this.treeViewGUI = treeViewGUI;
    }

    public void OnInitialize() => treeViewGUI.OnInitialize();
    public Vector2 GetTotalSize() => treeViewGUI.GetTotalSize();
    public void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible) => treeViewGUI.GetFirstAndLastRowVisible(out firstRowVisible, out lastRowVisible);
    public Rect GetRowRect(int row, float rowWidth) => treeViewGUI.GetRowRect(row, rowWidth);
    public Rect GetRectForFraming(int row) => treeViewGUI.GetRectForFraming(row);
    public int GetNumRowsOnPageUpDown(TreeViewItem<int> fromItem, bool pageUp, float heightOfTreeView) => treeViewGUI.GetNumRowsOnPageUpDown(fromItem as TreeViewItem, pageUp, heightOfTreeView);
    public void OnRowGUI(Rect rowRect, TreeViewItem<int> item, int row, bool selected, bool focused) => treeViewGUI.OnRowGUI(rowRect, item as TreeViewItem, row, selected, focused);
    public void BeginRowGUI() => treeViewGUI.BeginRowGUI();
    public void EndRowGUI() => treeViewGUI.EndRowGUI();
    public void BeginPingItem(TreeViewItem<int> item, float topPixelOfRow, float availableWidth) => treeViewGUI.BeginPingItem(item as TreeViewItem, topPixelOfRow, availableWidth);
    public void EndPingItem() => treeViewGUI.EndPingItem();
    public bool BeginRename(TreeViewItem<int> item, float delay) => treeViewGUI.BeginRename(item as TreeViewItem, delay);
    public void EndRename() => treeViewGUI.EndRename();
    public Rect GetRenameRect(Rect rowRect, int row, TreeViewItem<int> item) => treeViewGUI.GetRenameRect(rowRect, row, item as TreeViewItem);
    public float GetContentIndent(TreeViewItem<int> item) => treeViewGUI.GetContentIndent(item as TreeViewItem);
    public float halfDropBetweenHeight => treeViewGUI.halfDropBetweenHeight;
    public float topRowMargin => treeViewGUI.topRowMargin;
    public float bottomRowMargin => treeViewGUI.bottomRowMargin;
}

[Obsolete]
internal abstract class TreeViewGUI : TreeViewGUI<int>
{
    protected TreeViewGUI(TreeViewController<int> treeView)
        : base(treeView) { }

    protected TreeViewGUI(TreeViewController<int> treeView, bool useHorizontalScroll)
        : base(treeView, useHorizontalScroll) { }

    public new System.Action<TreeViewItem, Rect> iconOverlayGUI { get; set; }

    virtual public void OnRowGUI(Rect rowRect, TreeViewItem  item, int row, bool selected, bool focused) => base.OnRowGUIInternal(rowRect, item, row, selected, focused);
    override public void OnRowGUIInternal(Rect rowRect, TreeViewItem<int>  item, int row, bool selected, bool focused) => OnRowGUI(rowRect, item as TreeViewItem, row, selected, focused);
    virtual public bool BeginRename(TreeViewItem  item, float delay) => base.BeginRenameInternal(item, delay);
    override public bool BeginRenameInternal(TreeViewItem<int>  item, float delay) => BeginRename(item as TreeViewItem, delay);
    protected virtual Texture GetIconForItem(TreeViewItem  item) => base.GetIconForItemInternal(item);
    protected override Texture GetIconForItemInternal(TreeViewItem<int>  item) => GetIconForItem(item as TreeViewItem);
}

[Obsolete]
internal interface ITreeViewDataSource : ITreeViewDataSource<int>
{
    new TreeViewItem root { get; }
    new IList<TreeViewItem> GetRows();
}
