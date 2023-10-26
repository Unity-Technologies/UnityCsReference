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
    public class TreeView : BaseTreeView
    {
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
        public new class UxmlTraits : BaseTreeView.UxmlTraits {}

        Func<VisualElement> m_MakeItem;

        /// <summary>
        /// Callback for constructing the VisualElement that is the template for each recycled and re-bound element in the list.
        /// </summary>
        /// <remarks>
        /// This callback needs to call a function that constructs a blank <see cref="VisualElement"/> that is
        /// bound to an element from the list.
        ///
        /// The collection view automatically creates enough elements to fill the visible area, and adds more if the area
        /// is expanded. As the user scrolls, the collection view cycles elements in and out as they appear or disappear.
        ///
        /// If this property and <see cref="bindItem"/> are not set, Unity will either create a PropertyField if bound
        /// to a SerializedProperty, or create an empty label for any other case.
        /// </remarks>
        public new Func<VisualElement> makeItem
        {
            get => m_MakeItem;
            set
            {
                if (value != m_MakeItem)
                {
                    m_MakeItem = value;
                    Rebuild();
                }
            }
        }

        private Action<VisualElement, int> m_BindItem;

        /// <summary>
        /// Callback for binding a data item to the visual element.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to bind, and the index of the
        /// element to bind it to.
        ///
        /// If this property and <see cref="makeItem"/> are not set, Unity will try to bind to a SerializedProperty if
        /// bound, or simply set text in the created Label.
        ///
        /// **Note:**: Setting this callback without also setting <see cref="unbindItem"/> might result in unexpected behavior.
        /// This is because the default implementation of unbindItem expects the default implementation of bindItem.
        /// </remarks>
        public new Action<VisualElement, int> bindItem
        {
            get => m_BindItem;
            set
            {
                if (value != m_BindItem)
                {
                    m_BindItem = value;
                    RefreshItems();
                }

            }
        }

        /// <summary>
        /// Callback for unbinding a data item from the VisualElement.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to unbind, and the index of the
        /// element to unbind it from.
        ///
        /// **Note:**: Setting this callback without also setting <see cref="bindItem"/> might cause unexpected behavior.
        /// This is because the default implementation of bindItem expects the default implementation of unbindItem.
        /// </remarks>
        public new Action<VisualElement, int> unbindItem { get; set; }

        /// <summary>
        /// Callback invoked when a <see cref="VisualElement"/> created via <see cref="makeItem"/> is no longer needed and will be destroyed.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement that will be destroyed from the pool.
        /// </remarks>
        public new Action<VisualElement> destroyItem { get; set; }

        /// <summary>
        /// Sets the root items.
        /// </summary>
        /// <remarks>
        /// Root items can include their children directly.
        /// </remarks>
        /// <param name="rootItems">The TreeView root items.</param>
        internal override void SetRootItemsInternal<T>(IList<TreeViewItemData<T>> rootItems)
        {
            TreeViewHelpers<T, DefaultTreeViewController<T>>.SetRootItems(this, rootItems, () => new DefaultTreeViewController<T>());
        }

        internal override bool HasValidDataAndBindings()
        {
            return base.HasValidDataAndBindings() && !(makeItem != null ^ bindItem != null);
        }

        /// <summary>
        /// The view controller for this view, cast as a <see cref="TreeViewController"/>.
        /// </summary>
        public new TreeViewController viewController => base.viewController as TreeViewController;

        protected override CollectionViewController CreateViewController() => new DefaultTreeViewController<object>();

        /// <summary>
        /// Creates a <see cref="TreeView"/> with all default properties.
        /// </summary>
        /// <remarks>
        /// Use <see cref="BaseTreeView.SetRootItems{T}"/> to add content.
        /// </remarks>
        public TreeView()
            : this(null, null) { }

        /// <summary>
        /// Creates a <see cref="TreeView"/> with specified factory methods.
        /// </summary>
        /// <param name="makeItem">The factory method to call to create a display item. The method should return a
        /// VisualElement that can be bound to a data item.</param>
        /// <param name="bindItem">The method to call to bind a data item to a display item. The method
        /// receives as parameters the display item to bind, and the index of the data item to bind it to.</param>
        /// <remarks>
        /// Use <see cref="BaseTreeView.SetRootItems{T}"/> to add content.
        /// </remarks>
        public TreeView(Func<VisualElement> makeItem, Action<VisualElement, int> bindItem)
            : base((int)ItemHeightUnset)
        {
            this.makeItem = makeItem;
            this.bindItem = bindItem;
        }

        /// <summary>
        /// Creates a <see cref="TreeView"/> with specified factory methods using the fixed height virtualization method.
        /// </summary>
        /// <param name="itemHeight">The item height to use in FixedHeight virtualization mode.</param>
        /// <param name="makeItem">The factory method to call to create a display item. The method should return a
        /// VisualElement that can be bound to a data item.</param>
        /// <param name="bindItem">The method to call to bind a data item to a display item. The method
        /// receives as parameters the display item to bind, and the index of the data item to bind it to.</param>
        /// <remarks>
        /// Use <see cref="BaseTreeView.SetRootItems{T}"/> to add content.
        /// </remarks>
        public TreeView(int itemHeight, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem)
            : this(makeItem, bindItem)
        {
            fixedItemHeight = itemHeight;
        }

        private protected override IEnumerable<TreeViewItemData<T>> GetSelectedItemsInternal<T>()
        {
            return TreeViewHelpers<T, DefaultTreeViewController<T>>.GetSelectedItems(this);
        }

        private protected override T GetItemDataForIndexInternal<T>(int index)
        {
            return TreeViewHelpers<T, DefaultTreeViewController<T>>.GetItemDataForIndex(this, index);
        }

        private protected override T GetItemDataForIdInternal<T>(int id)
        {
            return TreeViewHelpers<T, DefaultTreeViewController<T>>.GetItemDataForId(this, id);
        }

        private protected override void AddItemInternal<T>(TreeViewItemData<T> item, int parentId, int childIndex, bool rebuildTree)
        {
            TreeViewHelpers<T, DefaultTreeViewController<T>>.AddItem(this, item, parentId, childIndex, rebuildTree);
        }
    }
}
