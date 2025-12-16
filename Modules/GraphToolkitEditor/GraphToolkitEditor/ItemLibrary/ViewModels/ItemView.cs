// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.ItemLibrary.Editor
{
    /// <summary>
    /// View model for a <see cref="Item"/> in the library Tree View.
    /// </summary>
    /// <remarks>Basic implementation of <see cref="IItemView"/>.</remarks>
    class ItemView : CategoryView, IItemView
    {
        /// <summary>
        /// Name of the Item.
        /// </summary>
        public override string Name => Item.Name;

        /// <summary>
        /// The <see cref="Item"/> represented by this view.
        /// </summary>
        public ItemLibraryItem Item { get; }

        /// <inheritdoc />
        public override string GetPath() => Item.CategoryPath;

        /// <summary>
        /// Help content to display about this item.
        /// </summary>
        public override string Help => Item.Help;

        /// <summary>
        /// Custom name used to generate USS styles when creating UI for this item.
        /// </summary>
        public override string StyleName => Item.StyleName;

        /// <inheritdoc />
        public override string IconPath => Item.IconPath;

        /// <summary>
        /// Get the port to which this item will be connected to.
        /// </summary>
        public PortModel PortToConnect => Item is GraphNodeModelLibraryItem { Data: NodeItemLibraryData data } ? data.PortToConnect : null;

        /// <summary>
        /// Get the subgraph <see cref="GraphReference"/>, if any.
        /// </summary>
        public GraphReference GraphReference => Item is GraphNodeModelLibraryItem { Data: NodeItemLibraryData data } ? data.SubgraphReference : default;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemView"/> class.
        /// </summary>
        /// <param name="parent">Category under which to display this item. Can be <c>null</c>.</param>
        /// <param name="item">The Item represented by this view.</param>
        public ItemView(ICategoryView parent, ItemLibraryItem item)
            : base(item.Name, parent)
        {
            Item = item;
        }
    }
}
