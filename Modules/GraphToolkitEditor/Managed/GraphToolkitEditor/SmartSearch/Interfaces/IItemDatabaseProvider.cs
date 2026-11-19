// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.ItemLibrary.Editor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface to provide different <see cref="ItemLibraryDatabaseBase"/> depending on context.
    /// </summary>
    [UnityRestricted]
    internal interface IItemDatabaseProvider
    {
        /// <summary>
        /// Gets a database when searching for a graph element.
        /// </summary>
        /// <param name="blackboardModel">The content model of the blackboard.</param>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing graph elements.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementsDatabases(BlackboardContentModel blackboardModel);

        /// <summary>
        /// Gets a database when searching for variables.
        /// </summary>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing variable types.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetVariableDatabases();

        /// <summary>
        /// Gets a database when searching for types compatible with a given variable.
        /// </summary>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing variable types.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetVariableCompatibleTypesDatabases(VariableDeclarationModelBase variable);

        /// <summary>
        /// Gets a database when searching for graph variables.
        /// </summary>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing variable.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetGraphVariablesDatabases();

        /// <summary>
        /// Gets a database when searching for elements that can be linked to a port.
        /// </summary>
        /// <param name="portModel">The <see cref="PortModel"/> to link the search result to.</param>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing elements that can be linked to the port.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetDynamicDatabases(PortModel portModel);

        /// <summary>
        /// Gets a database when searching for elements that can be linked to certain ports.
        /// </summary>
        /// <param name="portModels">The ports to link the search result to.</param>
        /// <returns>A <see cref="ItemLibraryDatabaseBase"/> containing elements that can be linked to the port.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetDynamicDatabases(IEnumerable<PortModel> portModels);

        /// <summary>
        /// Returns the <see cref="ItemLibraryDatabaseBase"/>s for a given <see cref="IGraphElementContainer"/>.
        /// </summary>
        /// <param name="container">The <see cref="IGraphElementContainer"/> database to return.</param>
        /// <returns>The <see cref="ItemLibraryDatabaseBase"/>s for a given <see cref="IGraphElementContainer"/>.</returns>
        IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementContainerDatabases(IGraphElementContainer container);
    }
}
