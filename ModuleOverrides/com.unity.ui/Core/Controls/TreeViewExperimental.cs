// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements.Experimental
{
    /// <summary>
    /// A TreeView is a vertically scrollable area that links to, and displays, a list of items organized in a tree.
    /// </summary>
    /// <remarks>
    /// A <see cref="TreeView"/> is a <see cref="ScrollView"/> with additional logic to display a tree of vertically-arranged
    /// VisualElements. Each VisualElement in the tree is bound to a corresponding element in a data-source list. The
    /// data-source list can contain elements of any type. <see cref="TreeViewItemData{T}"/>\\
    /// \\
    /// The logic required to create VisualElements, and to bind them to or unbind them from the data source, varies depending
    /// on the intended result. It's up to you to implement logic that is appropriate to your use case. For the ListView to function
    /// correctly, you must supply at least the following:
    ///
    ///- <see cref="BaseVerticalCollectionView.fixedItemHeight"/>
    ///
    /// It is also recommended to supply the following for more complex items:
    ///
    ///- <see cref="TreeView.makeItem"/>
    ///- <see cref="TreeView.bindItem"/>
    ///- <see cref="BaseVerticalCollectionView.fixedItemHeight"/>, in the case of <c>FixedHeight</c> ListView
    ///
    /// The TreeView creates VisualElements for the visible items, and supports binding many more. As the user scrolls, the TreeView
    /// recycles VisualElements and re-binds them to new data items.
    /// </remarks>
    internal class TreeView : BaseVerticalCollectionView
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
        /// The USS class name for TreeView indent elements.
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
        /// Instantiates a <see cref="TreeView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<TreeView, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TreeView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the TreeView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlIntAttributeDescription m_FixedItemHeight = new UxmlIntAttributeDescription { name = "fixed-item-height", obsoleteNames = new[] { "item-height" }, defaultValue = s_DefaultItemHeight };
            private readonly UxmlEnumAttributeDescription<CollectionVirtualizationMethod> m_VirtualizationMethod = new UxmlEnumAttributeDescription<CollectionVirtualizationMethod> { name = "virtualization-method", defaultValue = CollectionVirtualizationMethod.FixedHeight };
            private readonly UxmlBoolAttributeDescription m_ShowBorder = new UxmlBoolAttributeDescription { name = "show-border", defaultValue = false };
            private readonly UxmlBoolAttributeDescription m_AutoExpand = new UxmlBoolAttributeDescription { name = "auto-expand", defaultValue = false };
            private readonly UxmlEnumAttributeDescription<SelectionType> m_SelectionType = new UxmlEnumAttributeDescription<SelectionType> { name = "selection-type", defaultValue = SelectionType.Single };
            private readonly UxmlEnumAttributeDescription<AlternatingRowBackground> m_ShowAlternatingRowBackgrounds = new UxmlEnumAttributeDescription<AlternatingRowBackground> { name = "show-alternating-row-backgrounds", defaultValue = AlternatingRowBackground.None };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                int itemHeight = 0;
                if (m_FixedItemHeight.TryGetValueFromBag(bag, cc, ref itemHeight))
                {
                    ((TreeView)ve).fixedItemHeight = itemHeight;
                }

                ((TreeView)ve).virtualizationMethod = m_VirtualizationMethod.GetValueFromBag(bag, cc);
                ((TreeView)ve).autoExpand = m_AutoExpand.GetValueFromBag(bag, cc);
                ((TreeView)ve).showBorder = m_ShowBorder.GetValueFromBag(bag, cc);
                ((TreeView)ve).selectionType = m_SelectionType.GetValueFromBag(bag, cc);
                ((TreeView)ve).showAlternatingRowBackgrounds = m_ShowAlternatingRowBackgrounds.GetValueFromBag(bag, cc);
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
        /// Sets the root items.
        /// </summary>
        /// <remarks>
        /// Root items can include their children directly.
        /// </remarks>
        /// <param name="rootItems">The TreeView root items.</param>
        public void SetRootItems<T>(IList<TreeViewItemData<T>> rootItems)
        {
            if (base.viewController is DefaultTreeViewController<T> defaultController)
            {
                defaultController.SetRootItems(rootItems);
            }
            else
            {
                var defaultTreeViewController = new DefaultTreeViewController<T>();
                SetViewController(defaultTreeViewController);
                defaultTreeViewController.SetRootItems(rootItems);
            }
        }

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
            return viewController.GetTreeCount();
        }

        internal new TreeViewController viewController => base.viewController as TreeViewController;

        private protected override void CreateVirtualizationController()
        {
            CreateVirtualizationController<ReusableTreeViewItem>();
        }

        private protected override void CreateViewController()
        {
            SetViewController(new DefaultTreeViewController<object>());
        }

        internal void SetViewController(TreeViewController controller)
        {
            if (viewController != null)
            {
                controller.itemIndexChanged -= OnItemIndexChanged;
            }

            base.SetViewController(controller);
            RefreshItems();

            if (controller != null)
            {
                controller.itemIndexChanged += OnItemIndexChanged;
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
        /// You can also define
        /// </remarks>
        public TreeView()
        {
            m_ExpandedItemIds = new List<int>();

            name = ussClassName;
            viewDataKey = ussClassName;
            AddToClassList(ussClassName);

            scrollView.contentContainer.RegisterCallback<KeyDownEvent>(OnScrollViewKeyDown);

            RegisterCallback<MouseUpEvent>(OnTreeViewMouseUp, TrickleDown.TrickleDown);
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
            return viewController.GetChildrenIdsByIndex(GetIdForIndex(index));
        }

        /// <summary>
        /// Gets data for the specified TreeView item index.
        /// </summary>
        /// <param name="index">The TreeView item index.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The TreeView item data.</returns>
        /// <exception cref="ArgumentException">Throws if the type does not match with the item source data type.</exception>
        public T GetItemDataForIndex<T>(int index)
        {
            // Support default case
            if (viewController is DefaultTreeViewController<T> defaultController)
                return defaultController.GetDataForIndex(index);

            // Support user-defined controller case.
            var obj = viewController?.GetItemForIndex(index);
            var objectType = obj?.GetType();
            if (objectType == typeof(T))
                return (T)obj;

            if (objectType == null && viewController?.GetType().GetGenericTypeDefinition() == typeof(DefaultTreeViewController<>))
            {
                objectType = viewController.GetType().GetGenericArguments()[0];
            }

            throw new ArgumentException($"Type parameter ({typeof(T)}) differs from data source ({objectType}) and is not recognized by the controller.");
        }

        /// <summary>
        /// Gets data for the specified TreeView item id.
        /// </summary>
        /// <param name="id">The TreeView item id.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The TreeView item data.</returns>
        /// <exception cref="ArgumentException">Throws if the type does not match with the item source data type.</exception>
        public T GetItemDataForId<T>(int id)
        {
            // Support default case
            if (viewController is DefaultTreeViewController<T> defaultController)
                return defaultController.GetDataForId(id);

            // Support user-defined controller case.
            var obj = viewController?.GetItemForIndex(viewController.GetIndexForId(id));
            var objectType = obj?.GetType();
            if (objectType == typeof(T))
                return (T)obj;

            if (objectType == null && viewController?.GetType().GetGenericTypeDefinition() == typeof(DefaultTreeViewController<>))
            {
                objectType = viewController.GetType().GetGenericArguments()[0];
            }

            throw new ArgumentException($"Type parameter ({typeof(T)}) differs from data source ({objectType}) and is not recognized by the controller.");
        }

        /// <summary>
        /// Adds an item to the existing tree.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <param name="parentId">The parent id for the item.</param>
        /// <param name="childIndex">The child index in the parent's children list.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <exception cref="ArgumentException">Throws if the type does not match with the item source data type.</exception>
        public void AddItem<T>(TreeViewItemData<T> item, int parentId = -1, int childIndex = -1)
        {
            if (viewController is DefaultTreeViewController<T> defaultController)
            {
                defaultController.AddItem(item, parentId, childIndex);
                RefreshItems();
            }

            Type dataSourceType = null;
            if (viewController?.GetType().GetGenericTypeDefinition() == typeof(DefaultTreeViewController<>))
            {
                dataSourceType = viewController.GetType().GetGenericArguments()[0];
            }

            throw new ArgumentException($"Type parameter ({typeof(T)}) differs from data source ({dataSourceType})and is not recognized by the controller.");
        }

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
            viewController.RebuildTree();
            RefreshItems();
        }

        private void OnScrollViewKeyDown(KeyDownEvent evt)
        {
            var index = selectedIndex;

            bool shouldStopPropagation = true;

            switch (evt.keyCode)
            {
                case KeyCode.RightArrow:
                    if (evt.altKey || !IsExpandedByIndex(index))
                        ExpandItemByIndex(index, evt.altKey);
                    break;
                case KeyCode.LeftArrow:
                    if (evt.altKey || IsExpandedByIndex(index))
                        CollapseItemByIndex(index, evt.altKey);
                    break;
                default:
                    shouldStopPropagation = false;
                    break;
            }

            if (shouldStopPropagation)
                evt.StopPropagation();
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

            var selectedIndexes = ids.Select(id =>
            {
                viewController.ExpandItem(id, false);
                return viewController.GetIndexForId(id);
            }).ToList();

            SetSelectionInternal(selectedIndexes, sendNotification);
        }

        internal void CopyExpandedStates(ITreeViewItem source, ITreeViewItem target)
        {
            if (IsExpanded(source.id))
            {
                ExpandItem(target.id);

                if (source.children != null && source.children.Count() > 0)
                {
                    if (target.children == null || source.children.Count() != target.children.Count())
                    {
                        Debug.LogWarning("Source and target hierarchies are not the same");
                        return;
                    }

                    for (int i = 0; i < source.children.Count(); i++)
                    {
                        var sourceChild = source.children.ElementAt(i);
                        var targetchild = target.children.ElementAt(i);
                        CopyExpandedStates(sourceChild, targetchild);
                    }
                }
            }
            else
            {
                CollapseItem(target.id);
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
            RefreshItems();
        }

        /// <summary>
        /// Expands all root TreeView items.
        /// </summary>
        public void ExpandRootItems()
        {
            foreach (var itemId in viewController.GetRootItemIds())
                viewController.ExpandItem(itemId, false);

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

        private void OnTreeViewMouseUp(MouseUpEvent evt)
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
