// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ItemLibrary.Editor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface to provide different <see cref="ItemLibraryDatabaseBase"/> depending on context.
    /// </summary>
    interface IItemDatabaseProvider
    {
        /// <summary>
        /// Gets a database when searching for a graph element.
        /// </summary>
        /// <param name="graphModel">The graph in which to search for elements.</param>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing graph elements.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementsDatabases(GraphModel graphModel);

        /// <summary>
        /// Gets a database when searching for variable types.
        /// </summary>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing variable types.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetVariableTypesDatabases();

        /// <summary>
        /// Gets a database when searching for graph variables.
        /// </summary>
        /// <param name="graphModel">The graph in which to search for variables.</param>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing variable.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetGraphVariablesDatabases(GraphModel graphModel);

        /// <summary>
        /// Gets a database when searching for elements that can be linked to a port.
        /// </summary>
        /// <param name="portModel">The <see cref="PortModel"/> to link the search result to.</param>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing elements that can be linked to the port.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetDynamicDatabases(PortModel portModel);

        /// <summary>
        /// Gets a database when searching for elements that can be linked to certain ports.
        /// </summary>
        /// <param name="portModel">The ports to link the search result to.</param>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing elements that can be linked to the port.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetDynamicDatabases(IEnumerable<PortModel> portModel);

        /// <summary>
        /// Returns the <see cref="ItemLibraryDatabaseBase"/>s for a given <see cref="IGraphElementContainer"/>.
        /// </summary>
        /// <param name="graphModel">The <see cref="GraphModel"/> to use.</param>
        /// <param name="container">The <see cref="IGraphElementContainer"/> database to return.</param>
        /// <returns>The <see cref="ItemLibraryDatabaseBase"/>s for a given <see cref="IGraphElementContainer"/>.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementContainerDatabases(GraphModel graphModel,
            IGraphElementContainer container);
    }
}
