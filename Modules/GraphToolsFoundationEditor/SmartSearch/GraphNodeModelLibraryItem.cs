// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ItemLibrary.Editor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// <see cref="ItemLibraryItem"/> allowing to create a <see cref="GraphElementModel"/> in a Graph.
    /// </summary>
    class GraphNodeModelLibraryItem : ItemLibraryItem, IItemLibraryDataProvider
    {
        /// <inheritdoc />
        public override string Name => GetName != null ? GetName.Invoke() : base.Name;

        /// <summary>
        /// Function to create a <see cref="GraphElementModel"/> in the graph from this item.
        /// </summary>
        public Func<IGraphNodeCreationData, GraphElementModel> CreateElement { get; }

        /// <summary>
        /// Custom Data for the item.
        /// </summary>
        public IItemLibraryData Data { get; }

        /// <summary>
        /// Provides the <see cref="Name"/> dynamically.
        /// </summary>
        public Func<string> GetName { get; set; }

        /// <summary>
        /// Instantiates a <see cref="GraphNodeModelLibraryItem"/>.
        /// </summary>
        /// <param name="name">Name used to find the item in the library.</param>
        /// <param name="data">Custom data for the item.</param>
        /// <param name="createElement">Function to create the element in the graph.</param>
        public GraphNodeModelLibraryItem(
            string name,
            IItemLibraryData data,
            Func<IGraphNodeCreationData, GraphElementModel> createElement
        ) : base(name)
        {
            Data = data;
            CreateElement = createElement;
        }

        /// <summary>
        /// Instantiates a <see cref="GraphNodeModelLibraryItem"/>.
        /// </summary>
        /// <param name="data">Custom data for the item.</param>
        /// <param name="createElement">Function to create the element in the graph.</param>
        public GraphNodeModelLibraryItem(
            IItemLibraryData data,
            Func<IGraphNodeCreationData, GraphElementModel> createElement
        )
        {
            Data = data;
            CreateElement = createElement;
        }
    }
}
