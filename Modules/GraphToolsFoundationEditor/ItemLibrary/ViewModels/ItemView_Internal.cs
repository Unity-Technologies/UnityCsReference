// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolsFoundation.Editor;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// View model for a <see cref="Item"/> in the library Tree View.
    /// </summary>
    /// <remarks>Basic implementation of <see cref="IItemView_Internal"/>.</remarks>
    class ItemView_Internal: CategoryView_Internal, IItemView_Internal
    {
        /// <summary>
        /// Name of the Item.
        /// </summary>
        public override string Name => Item.Name;

        /// <summary>
        /// The <see cref="Item"/> represented by this view.
        /// </summary>
        public ItemLibraryItem Item { get; }

        /// <summary>
        /// Path in the hierarchy of items.
        /// </summary>
        public override string Path => Item.CategoryPath;

        /// <summary>
        /// Help content to display about this item.
        /// </summary>
        public override string Help => Item.Help;

        /// <summary>
        /// Custom name used to generate USS styles when creating UI for this item.
        /// </summary>
        public override string StyleName => Item.StyleName;

        /// <summary>
        /// Get the port to which this item will be connected to.
        /// </summary>
        public PortModel GetPortToConnect()
        {
            if (Item is GraphNodeModelLibraryItem item && item.Data is NodeItemLibraryData data)
                return data.PortToConnect;

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemView_Internal"/> class.
        /// </summary>
        /// <param name="parent">Category under which to display this item. Can be <c>null</c>.</param>
        /// <param name="item">The Item represented by this view.</param>
        public ItemView_Internal(ICategoryView_Internal parent, ItemLibraryItem item)
            : base(item.Name, parent)
        {
            Item = item;
        }
    }
}
