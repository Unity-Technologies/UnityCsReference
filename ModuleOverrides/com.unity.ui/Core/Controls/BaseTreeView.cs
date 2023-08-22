// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base class for a tree view, a vertically scrollable area that links to, and displays, a list of items organized in a tree.
    /// </summary>
    public abstract class BaseTreeView : BaseVerticalCollectionView
    {
        /// <summary>
        /// The USS class name for TreeView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the TreeView element. Any styling applied to
        /// this class affects every TreeView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string ussClassName = "unity-tree-view";
        /// <summary>
        /// The USS class name for TreeView item elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every item element of the TreeView. Any styling applied to
        /// this class affects every item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string itemUssClassName = ussClassName + "__item";
        /// <summary>
        /// The USS class name for TreeView item toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every item toggle element of the TreeView. Any styling applied to
        /// this class affects every item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemToggleUssClassName = ussClassName + "__item-toggle";
        /// <summary>
        /// The USS class name for TreeView indent container elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every indent container element of the TreeView. Any styling applied to
        /// this class affects every item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemIndentsContainerUssClassName = ussClassName + "__item-indents";
        /// <summary>
        /// The USS class name for TreeView indent element.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every indent element of the TreeView. Any styling applied to
        /// this class affects every item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemIndentUssClassName = ussClassName + "__item-indent";
        /// <summary>
        /// The USS class name for TreeView item container elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every item container element of the TreeView. Any styling applied to
        /// this class affects every item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemContentContainerUssClassName = ussClassName + "__item-content";

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TreeView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the TreeView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        public new class UxmlTraits : BaseVerticalCollectionView.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription m_AutoExpand = new UxmlBoolAttributeDescription { name = "auto-expand", defaultValue = false };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var treeView = (BaseTreeView)ve;
                treeView.autoExpand = m_AutoExpand.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// Access to the itemsSource. For a TreeView, the source contains the items wrappers.
        /// </summary>
        /// <remarks>
        /// To set the items source, use <see cref="SetRootItems{T}"/> instead, which allows fully typed items.
        /// </remarks>
        public new IList itemsSource
        {
            get => viewController.itemsSource;
            internal set => GetOrCreateViewController().itemsSource = value;
        }

        /// <summary>
        /// Sets the root items to use with the default tree view controller.
        /// </summary>
        /// <remarks>
        /// Root items can include their children directly.
        /// This will force the use of a <see cref="DefaultTreeViewController{T}"/>.
        /// </remarks>
        /// <param name="rootItems">The TreeView root items.</param>
        public void SetRootItems<T>(IList<TreeViewItemData<T>> rootItems)
        {
            SetRootItemsInternal(rootItems);
        }

        internal abstract void SetRootItemsInternal<T>(IList<TreeViewItemData<T>> rootItems);

        /// <summary>
        /// Gets the root item identifiers.
        /// </summary>
        /// <returns>The root item identifiers.</returns>
        public IEnumerable<int> GetRootIds()
        {
            return viewController.GetRootItemIds();
        }

        /// <summary>
        /// Gets the TreeView's total number of items.
        /// </summary>
        /// <returns>The TreeView's total number of items.</returns>
        public int GetTreeCount()
        {
            return viewController.GetTreeItemsCount();
        }

        /// <summary>
        /// The view controller for this view, cast as a <see cref="BaseTreeViewController"/>.
        /// </summary>
        public new BaseTreeViewController viewController => base.viewController as BaseTreeViewController;

        private protected override void CreateVirtualizationController()
        {
            CreateVirtualizationController<ReusableTreeViewItem>();
        }

        /// <summary>
        /// Assigns the view controller for this view and registers all events required for it to function properly.
        /// </summary>
        /// <param name="controller">The controller to use with this view.</param>
        /// <remarks>The controller should implement <see cref="BaseTreeViewController"/>.</remarks>
        public override void SetViewController(CollectionViewController controller)
        {
            if (viewController != null)
            {
                viewController.itemIndexChanged -= OnItemIndexChanged;
            }

            base.SetViewController(controller);

            if (viewController != null)
            {
                viewController.itemIndexChanged += OnItemIndexChanged;
            }
        }

        void OnItemIndexChanged(int srcIndex, int dstIndex)
        {
            RefreshItems();
        }

        internal override ICollectionDragAndDropController CreateDragAndDropController() => new TreeViewReorderableDragAndDropController(this);

        bool m_AutoExpand;

        /// <summary>
        /// When true, items are automatically expanded when added to the TreeView.
        /// </summary>
        public bool autoExpand
        {
            get => m_AutoExpand;
            set
            {
                m_AutoExpand = value;
                viewController?.RegenerateWrappers();
                RefreshItems();
            }
        }

        [SerializeField]
        private List<int> m_ExpandedItemIds;

        internal List<int> expandedItemIds
        {
            get => m_ExpandedItemIds;
            set => m_ExpandedItemIds = value;
        }

        /// <summary>
        /// Creates a <see cref="TreeView"/> with all default properties.
        /// </summary>
        /// <remarks>
        /// Use <see cref="SetRootItems{T}"/> to add content.
        /// </remarks>
        public BaseTreeView() : this((int)ItemHeightUnset) {}

        /// <summary>
        /// Creates a <see cref="TreeView"/> with specified factory methods using the fixed height virtualization method.
        /// </summary>
        /// <param name="itemHeight">The item height to use in FixedHeight virtualization mode.</param>
        /// <remarks>
        /// Use <see cref="SetRootItems{T}"/> to add content.
        /// </remarks>
        public BaseTreeView(int itemHeight) : base(null, itemHeight)
        {
            m_ExpandedItemIds = new List<int>();

            viewDataKey = ussClassName;
            AddToClassList(ussClassName);

            RegisterCallback<PointerUpEvent>(OnTreeViewPointerUp, TrickleDown.TrickleDown);
        }

        /// <summary>
        /// Gets the specified TreeView item's identifier.
        /// </summary>
        /// <param name="index">The TreeView item index.</param>
        /// <returns>The TreeView item's identifier.</returns>
        public int GetIdForIndex(int index)
        {
            return viewController.GetIdForIndex(index);
        }

        /// <summary>
        /// Gets the specified TreeView item's parent identifier.
        /// </summary>
        /// <param name="index">The TreeView item index.</param>
        /// <returns>The TreeView item's parent identifier.</returns>
        public int GetParentIdForIndex(int index)
        {
            return viewController.GetParentId(GetIdForIndex(index));
        }

        /// <summary>
        /// Gets children identifiers for the specified TreeView item.
        /// </summary>
        /// <param name="index">The TreeView item index.</param>
        /// <returns>The children item identifiers.</returns>
        public IEnumerable<int> GetChildrenIdsForIndex(int index)
        {
            return viewController.GetChildrenIdsByIndex(index);
        }

        /// <summary>
        /// Gets tree data for the selected item indices.
        /// </summary>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The selected TreeViewItemData items.</returns>
        /// <exception cref="ArgumentException">Throws if the type does not match with the item source data type of the default controller.</exception>
        public IEnumerable<TreeViewItemData<T>> GetSelectedItems<T>()
        {
            return GetSelectedItemsInternal<T>();
        }

        private protected abstract IEnumerable<TreeViewItemData<T>> GetSelectedItemsInternal<T>();

        /// <summary>
        /// Gets data for the specified TreeView item index.
        /// </summary>
        /// <param name="index">The TreeView item index.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The TreeView item data.</returns>
        /// <exception cref="ArgumentException">Throws if the type does not match with the item source data type.</exception>
        public T GetItemDataForIndex<T>(int index)
        {
            return GetItemDataForIndexInternal<T>(index);
        }

        private protected abstract T GetItemDataForIndexInternal<T>(int index);

        /// <summary>
        /// Gets data for the specified TreeView item id.
        /// </summary>
        /// <param name="id">The TreeView item id.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The TreeView item data.</returns>
        /// <exception cref="ArgumentException">Throws if the type does not match with the item source data type.</exception>
        public T GetItemDataForId<T>(int id)
        {
            return GetItemDataForIdInternal<T>(id);
        }

        private protected abstract T GetItemDataForIdInternal<T>(int id);

        /// <summary>
        /// Adds an item to the existing tree.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <param name="parentId">The parent id for the item.</param>
        /// <param name="childIndex">The child index in the parent's children list.</param>
        /// <param name="rebuildTree">Whether we should call RebuildTree and RefreshItems or not. Set to false when doing multiple additions to save a few rebuilds.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <exception cref="ArgumentException">Throws if the type does not match with the item source data type.</exception>
        public void AddItem<T>(TreeViewItemData<T> item, int parentId = -1, int childIndex = -1, bool rebuildTree = true)
        {
            AddItemInternal(item, parentId, childIndex, rebuildTree);
        }

        private protected abstract void AddItemInternal<T>(TreeViewItemData<T> item, int parentId, int childIndex, bool rebuildTree);

        /// <summary>
        /// Removes an item of the tree if it can find it.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>If the item was removed from the tree.</returns>
        public bool TryRemoveItem(int id)
        {
            if (viewController.TryRemoveItem(id))
            {
                RefreshItems();
                return true;
            }

            return false;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            if (viewController != null)
            {
                viewController.RebuildTree();
                RefreshItems();
            }
        }

        private protected override bool HandleItemNavigation(bool moveIn, bool altPressed)
        {
            var index = selectedIndex;
            var hasChildren = viewController.HasChildrenByIndex(index);

            var selectionIncrement = 1;
            if (moveIn)
            {
                if (hasChildren && !IsExpandedByIndex(index))
                {
                    ExpandItemByIndex(index, altPressed);
                    return true;
                }
            }
            else
            {
                if (hasChildren && IsExpandedByIndex(index))
                {
                    CollapseItemByIndex(index, altPressed);
                    return true;
                }

                // Find the nearest ancestor with children in the tree and select it.
                // If no ancestor is found, find the closest item with children before the current one.
                var id = viewController.GetIdForIndex(index);
                var ancestorId = viewController.GetParentId(id);
                if (ancestorId != ReusableCollectionItem.UndefinedIndex)
                {
                    SetSelectionById(ancestorId);
                    ScrollToItemById(ancestorId);
                    return true;
                }

                selectionIncrement = -1;
            }

            // Find the next item with children in the tree and select it.
            var selectionIndex = index;
            do
            {
                selectionIndex += selectionIncrement;
                hasChildren = viewController.HasChildrenByIndex(selectionIndex);
            } while (!hasChildren && selectionIndex >= 0 && selectionIndex < itemsSource.Count);

            if (hasChildren)
            {
                SetSelection(selectionIndex);
                ScrollToItem(selectionIndex);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the currently selected item by id.
        /// </summary>
        /// <remarks>
        /// This will also expand the selected item if not expanded already.
        /// </remarks>
        /// <param name="id">The item id.</param>
        public void SetSelectionById(int id)
        {
            SetSelectionById(new[] { id });
        }

        /// <summary>
        /// Sets a collection of selected items by ids.
        /// </summary>
        /// <remarks>
        /// This will also expand the selected items if not expanded already.
        /// </remarks>
        /// <param name="ids">The item ids.</param>
        public void SetSelectionById(IEnumerable<int> ids)
        {
            SetSelectionInternalById(ids, true);
        }

        /// <summary>
        /// Sets a collection of selected items by id, without triggering a selection change callback.
        /// </summary>
        /// <remarks>
        /// This will also expand the selected items if not expanded already.
        /// </remarks>
        /// <param name="ids">The item ids.</param>
        public void SetSelectionByIdWithoutNotify(IEnumerable<int> ids)
        {
            SetSelectionInternalById(ids, false);
        }

        internal void SetSelectionInternalById(IEnumerable<int> ids, bool sendNotification)
        {
            if (ids == null)
                return;

            var selectedIndexes = ids.Select(id => GetItemIndex(id, true)).ToList();

            SetSelectionInternal(selectedIndexes, sendNotification);
        }

        /// <summary>
        /// Adds an item to the current selection by id.
        /// </summary>
        /// <remarks>
        /// This will also expand the selected item if not expanded already.
        /// </remarks>
        /// <param name="id">The item id.</param>
        public void AddToSelectionById(int id)
        {
            var index = GetItemIndex(id, true);
            Rebuild();
            AddToSelection(index);
        }

        /// <summary>
        /// Removes an item from the current selection by id.
        /// </summary>
        /// <param name="id">The item id.</param>
        public void RemoveFromSelectionById(int id)
        {
            var index = GetItemIndex(id);
            RemoveFromSelection(index);
        }

        int GetItemIndex(int id, bool expand = false)
        {
            if (expand)
            {
                var parentId = viewController.GetParentId(id);
                while (parentId != -1)
                {
                    if (!m_ExpandedItemIds.Contains(parentId))
                    {
                        viewController.ExpandItem(parentId, false);
                    }

                    parentId = viewController.GetParentId(parentId);
                }
            }

            return viewController.GetIndexForId(id);
        }

        internal void CopyExpandedStates(int sourceId, int targetId)
        {
            if (IsExpanded(sourceId))
            {
                ExpandItem(targetId);

                if (viewController.HasChildren(sourceId))
                {
                    if (viewController.GetChildrenIds(sourceId).Count() != viewController.GetChildrenIds(targetId).Count())
                    {
                        Debug.LogWarning("Source and target hierarchies are not the same");
                        return;
                    }

                    for (var i = 0; i < viewController.GetChildrenIds(sourceId).Count(); i++)
                    {
                        var sourceChild = viewController.GetChildrenIds(sourceId).ElementAt(i);
                        var targetChild = viewController.GetChildrenIds(targetId).ElementAt(i);
                        CopyExpandedStates(sourceChild, targetChild);
                    }
                }
            }
            else
            {
                CollapseItem(targetId);
            }
        }

        /// <summary>
        /// Returns true if the specified TreeView item is expanded, false otherwise.
        /// </summary>
        /// <param name="id">The TreeView item identifier.</param>
        public bool IsExpanded(int id)
        {
            return viewController.IsExpanded(id);
        }

        /// <summary>
        /// Collapses the specified TreeView item.
        /// </summary>
        /// <param name="id">The TreeView item identifier.</param>
        /// <param name="collapseAllChildren">When true, all children will also get collapsed. This is false by default.</param>
        public void CollapseItem(int id, bool collapseAllChildren = false)
        {
            viewController.CollapseItem(id, collapseAllChildren);
            RefreshItems();
        }

        /// <summary>
        /// Expands the specified TreeView item.
        /// </summary>
        /// <param name="id">The TreeView item identifier.</param>
        /// <param name="expandAllChildren">When true, all children will also get expanded. This is false by default.</param>
        public void ExpandItem(int id, bool expandAllChildren = false)
        {
            viewController.ExpandItem(id, expandAllChildren);
        }

        /// <summary>
        /// Expands all root TreeView items.
        /// </summary>
        public void ExpandRootItems()
        {
            foreach (var itemId in viewController.GetRootItemIds())
                viewController.ExpandItem(itemId, false, false);

            RefreshItems();
        }

        /// <summary>
        /// Expands all TreeView items, including children.
        /// </summary>
        public void ExpandAll()
        {
            viewController.ExpandAll();
        }

        /// <summary>
        /// Collapses all TreeView items, including children.
        /// </summary>
        public void CollapseAll()
        {
            viewController.CollapseAll();
        }

        private void OnTreeViewPointerUp(PointerUpEvent evt)
        {
            scrollView.contentContainer.Focus();
        }

        private bool IsExpandedByIndex(int index)
        {
            return viewController.IsExpandedByIndex(index);
        }

        private void CollapseItemByIndex(int index, bool collapseAll)
        {
            if (!viewController.HasChildrenByIndex(index))
                return;

            viewController.CollapseItemByIndex(index, collapseAll);
            RefreshItems();
            SaveViewData();
        }

        private void ExpandItemByIndex(int index, bool expandAll)
        {
            if (!viewController.HasChildrenByIndex(index))
                return;

            viewController.ExpandItemByIndex(index, expandAll);
            RefreshItems();
            SaveViewData();
        }
    }
}
