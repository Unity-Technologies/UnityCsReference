// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.ItemLibrary.Editor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// <see cref="ItemLibraryItem"/> allowing to create a <see cref="GraphElementModel"/> in a Graph.
    /// </summary>
    [UnityRestricted]
    internal class GraphNodeModelLibraryItem : ItemLibraryItem, IItemLibraryDataProvider
    {
        readonly Func<IGraphNodeCreationData, GraphElementModel> m_CreateElement;

        /// <inheritdoc />
        public override string Name => GetName != null ? GetName.Invoke() : base.Name;

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
            m_CreateElement = createElement;
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
            m_CreateElement = createElement;
        }

        internal GraphNodeModelLibraryItem(
            string name,
            IItemLibraryData data,
            GraphNodeModelLibraryItem createElement)
            : this(name, data, createElement.m_CreateElement) { }

        /// <summary>
        /// Creates a <see cref="GraphElementModel"/> from the given data.
        /// </summary>
        /// <param name="data">The data to create the element from.</param>
        /// <returns>A new <see cref="GraphElementModel"/> instance.</returns>
        public virtual GraphElementModel CreateElement(IGraphNodeCreationData data)
        {
            return m_CreateElement(data);
        }
    }
}
