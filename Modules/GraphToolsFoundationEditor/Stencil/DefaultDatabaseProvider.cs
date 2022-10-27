// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ItemLibrary.Editor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The default implementation of <see cref="IItemDatabaseProvider"/>.
    /// </summary>
    class DefaultDatabaseProvider : IItemDatabaseProvider
    {
        protected static readonly IReadOnlyList<ItemLibraryDatabaseBase> k_NoDatabase = new List<ItemLibraryDatabaseBase>();
        protected static readonly IReadOnlyList<Type> k_NoTypeList = new List<Type>();

        /// <summary>
        /// List of types supported for variables and constants.
        /// <remarks>Will populate the default implementation of <see cref="GetVariableTypesDatabases"/>.</remarks>
        /// </summary>
        protected virtual IReadOnlyList<Type> SupportedTypes => k_NoTypeList;

        List<ItemLibraryDatabaseBase> m_GraphElementsDatabases;
        List<ItemLibraryDatabaseBase> m_GraphVariablesDatabases;
        List<ItemLibraryDatabaseBase> m_TypeDatabases;

        protected Dictionary<Type, List<ItemLibraryDatabaseBase>> m_GraphElementContainersDatabases;

        /// <summary>
        /// The graph stencil.
        /// </summary>
        public Stencil Stencil { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDatabaseProvider"/> class.
        /// </summary>
        /// <param name="stencil">The stencil.</param>
        public DefaultDatabaseProvider(Stencil stencil)
        {
            Stencil = stencil;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementsDatabases(GraphModel graphModel)
        {
            return m_GraphElementsDatabases ??= new List<ItemLibraryDatabaseBase>
            {
                InitialGraphElementDatabase(graphModel).Build_Internal()
            };
        }

        /// <summary>
        /// Creates the initial database used for graph elements.
        /// </summary>
        /// <param name="graphModel">The graph in which to search for elements.</param>
        /// <returns>A database containing <see cref="ItemLibraryItem"/>s for graph elements.</returns>
        public virtual GraphElementItemDatabase InitialGraphElementDatabase(GraphModel graphModel)
        {
            return new GraphElementItemDatabase(Stencil, graphModel)
                .AddNodesWithLibraryItemAttribute()
                .AddStickyNote();
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementContainerDatabases(
            GraphModel graphModel, IGraphElementContainer container)
        {
            List<ItemLibraryDatabaseBase> databases;
            if (m_GraphElementContainersDatabases != null)
            {
                m_GraphElementContainersDatabases.TryGetValue(container.GetType(), out databases);

                if (databases != null)
                    return databases;
            }

            if (container is ContextNodeModel)
                return m_GraphElementsDatabases ??= new List<ItemLibraryDatabaseBase>
                {
                    new ContextDatabase(Stencil, container.GetType())
                        .Build_Internal()
                };

            return null;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetVariableTypesDatabases()
        {
            return m_TypeDatabases ??= new List<ItemLibraryDatabaseBase>
            {
                SupportedTypes.ToDatabase()
            };
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetGraphVariablesDatabases(GraphModel graphModel)
        {
            return m_GraphVariablesDatabases ??= new List<ItemLibraryDatabaseBase>
            {
                InitialGraphVariablesDatabase(graphModel).Build_Internal()
            };
        }

        /// <summary>
        /// Creates the initial database used for graph variables.
        /// </summary>
        /// <param name="graphModel">The graph in which to search for variables.</param>
        /// <returns>A database containing the items for variables.</returns>
        public virtual GraphElementItemDatabase InitialGraphVariablesDatabase(GraphModel graphModel)
        {
            return new GraphElementItemDatabase(Stencil, graphModel)
                .AddGraphVariables(graphModel);
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetDynamicDatabases(PortModel portModel)
        {
            return k_NoDatabase;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetDynamicDatabases(
            IEnumerable<PortModel> portModel)
        {
            return k_NoDatabase;
        }

        /// <summary>
        /// Resets Graph Elements Databases to force invalidating the cached version.
        /// </summary>
        protected void ResetGraphElementsDatabases()
        {
            m_GraphElementsDatabases = null;
        }

        /// <summary>
        /// Resets Graph Variable Databases to force invalidating the cached version.
        /// </summary>
        protected void ResetGraphVariablesDatabases()
        {
            m_GraphVariablesDatabases = null;
        }
    }
}
