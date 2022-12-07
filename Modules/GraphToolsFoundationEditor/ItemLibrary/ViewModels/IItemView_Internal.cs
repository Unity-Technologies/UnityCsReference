// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolsFoundation.Editor;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// View model for a <see cref="ItemLibraryItem"/> in the ItemLibrary Tree View.
    /// </summary>
    interface IItemView_Internal : ICategoryView_Internal
    {
        /// <summary>
        /// The <see cref="ItemLibraryItem"/> represented by this view.
        /// </summary>
        public ItemLibraryItem Item { get; }

        /// <summary>
        /// Get the port to which this item will be connected to.
        /// </summary>
        public PortModel GetPortToConnect();
    }
}
