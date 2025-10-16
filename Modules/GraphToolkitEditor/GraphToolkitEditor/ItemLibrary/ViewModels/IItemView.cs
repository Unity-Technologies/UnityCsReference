// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.ItemLibrary.Editor
{
    /// <summary>
    /// View model for a <see cref="ItemLibraryItem"/> in the ItemLibrary Tree View.
    /// </summary>
    interface IItemView : ICategoryView
    {
        /// <summary>
        /// The <see cref="ItemLibraryItem"/> represented by this view.
        /// </summary>
        public ItemLibraryItem Item { get; }

        /// <summary>
        /// Get the port to which this item will be connected to.
        /// </summary>
        public PortModel PortToConnect { get; }

        /// <summary>
        /// Reference to the graph, if any.
        /// </summary>
        public GraphReference GraphReference { get; }
    }
}
