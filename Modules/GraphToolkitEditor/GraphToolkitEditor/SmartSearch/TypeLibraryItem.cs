// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.ItemLibrary.Editor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// <see cref="ItemLibraryItem"/> representing a Type.
    /// </summary>
    [UnityRestricted]
    internal class TypeLibraryItem : ItemLibraryItem, IItemLibraryDataProvider
    {
        /// <summary>
        /// <see cref="TypeHandle"/> of the item.
        /// </summary>
        public TypeHandle Type => ((TypeItemLibraryData)Data).Type;

        /// <summary>
        /// Custom data for the item.
        /// </summary>
        public IItemLibraryData Data { get; }

        /// <summary>
        /// The graph model for the item.
        /// </summary>
        public GraphModel GraphModel => ((TypeItemLibraryData)Data).GraphModel;

        /// <summary>
        /// Initializes a new instance of the TypeLibraryItem class.
        /// </summary>
        /// <param name="name">The name used to search the item.</param>
        /// <param name="type">The type represented by the item.</param>
        /// <param name="graphModel">The graph model associated with the item.</param>
        public TypeLibraryItem(string name, TypeHandle type, GraphModel graphModel)
            : base(name)
        {
            Data = new TypeItemLibraryData(type, graphModel);
        }
    }
}
