// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// View model for a <see cref="Item"/> in the library Tree View.
    /// </summary>
    /// <remarks>Basic implementation of <see cref="IItemView_Internal"/>.</remarks>
    class ItemView_Internal: IItemView_Internal
    {
        /// <summary>
        /// Name of the Item.
        /// </summary>
        public string Name => Item.Name;

        /// <summary>
        /// Parent of this item in the hierarchy.
        /// </summary>
        public ICategoryView_Internal Parent { get; }

        /// <summary>
        /// The <see cref="Item"/> represented by this view.
        /// </summary>
        public ItemLibraryItem Item { get; }

        /// <summary>
        /// Depth of this item in the hierarchy.
        /// </summary>
        public int Depth
        {
            get
            {
                if (m_Depth == -1)
                {
                    m_Depth = this.GetDepth();
                }
                return m_Depth;
            }
        }

        /// <summary>
        /// Path in the hierarchy of items.
        /// </summary>
        public string Path => Item.CategoryPath;

        /// <summary>
        /// Help content to display about this item.
        /// </summary>
        public string Help => Item.Help;

        /// <summary>
        /// Custom name used to generate USS styles when creating UI for this item.
        /// </summary>
        public string StyleName => Item.StyleName;

        int m_Depth = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemView_Internal"/> class.
        /// </summary>
        /// <param name="parent">Category under which to display this item. Can be <c>null</c>.</param>
        /// <param name="item">The Item represented by this view.</param>
        public ItemView_Internal(ICategoryView_Internal parent, ItemLibraryItem item)
        {
            Parent = parent;
            Item = item;
        }
    }
}
