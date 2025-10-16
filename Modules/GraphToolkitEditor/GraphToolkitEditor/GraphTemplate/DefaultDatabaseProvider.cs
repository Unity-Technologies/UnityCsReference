// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.ItemLibrary.Editor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The default implementation of <see cref="IItemDatabaseProvider"/>.
    /// </summary>
    [UnityRestricted]
    internal class DefaultDatabaseProvider : IItemDatabaseProvider
    {
        /// <summary>
        /// An empty list of <see cref="ItemLibraryDatabaseBase"/>.
        /// </summary>
        /// <remarks>'k_NoDatabase' is a static, read-only list of <see cref="ItemLibraryDatabaseBase"/> used to represent an empty database collection. It helps
        /// optimize performance by reusing a predefined empty list instead of creating new instances when no databases are available, which prevents memory allocations.</remarks>
        protected static readonly IReadOnlyList<ItemLibraryDatabaseBase> k_NoDatabase = new List<ItemLibraryDatabaseBase>();

        /// <summary>
        /// An empty list of <see cref="Type"/>.
        /// </summary>
        /// <remarks>'k_NoTypeList' is a static, read-only list of <see cref="Type"/> used to represent an empty type collection. It helps optimize
        /// performance by reusing a predefined empty list instead of creating new instances when no types are available, which prevents memory allocations.
        /// </remarks>
        protected static readonly IReadOnlyList<Type> k_NoTypeList = Array.Empty<Type>();

        /// <summary>
        /// List of types supported for variables and constants.
        /// </summary>
        /// <remarks>
        /// Will populate the default implementation of <see cref="GetVariableDatabases"/>.
        /// </remarks>
        public virtual IReadOnlyList<Type> SupportedTypes => k_NoTypeList;

        List<ItemLibraryDatabaseBase> m_GraphElementsDatabases;
        List<ItemLibraryDatabaseBase> m_GraphVariablesDatabases;
        List<ItemLibraryDatabaseBase> m_TypeDatabases;

        // Used to track whether m_GraphVariablesDatabases needs to be recreated when variables have changed in the graph
        HashSet<Hash128> m_ExistingVariableGuids;

        /// <summary>
        /// The <see cref="GraphModel"/> associated with the provider.
        /// </summary>
        protected GraphModel GraphModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDatabaseProvider"/> class.
        /// </summary>
        /// <param name="graphModel">The graph model.</param>
        public DefaultDatabaseProvider(GraphModel graphModel)
        {
            GraphModel = graphModel;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementsDatabases(BlackboardContentModel blackboardModel = null)
        {
            return m_GraphElementsDatabases ??= new List<ItemLibraryDatabaseBase>
                {
                    InitialGraphElementDatabase().Build()
                };
        }

        /// <summary>
        /// Creates the initial database used for graph elements.
        /// </summary>
        /// <returns>A database containing <see cref="ItemLibraryItem"/>s for graph elements.</returns>
        protected virtual GraphElementItemDatabase InitialGraphElementDatabase()
        {
            return new GraphElementItemDatabase(GraphModel)
                .AddNodesWithLibraryItemAttribute();
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementContainerDatabases(IGraphElementContainer container)
        {
            if (container is ContextNodeModel)
                return new List<ItemLibraryDatabaseBase>
                    {
                        new ContextDatabase(GraphModel, container.GetType())
                            .Build()
                    };

            return null;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetVariableDatabases()
        {
            return m_TypeDatabases ??= new List<ItemLibraryDatabaseBase>
                {
                    SupportedTypes.ToDatabase()
                };
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetVariableCompatibleTypesDatabases(VariableDeclarationModelBase variable)
        {
            return GetVariableDatabases();
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetGraphVariablesDatabases()
        {
            // If there are no variable databases, we create them.
            if (m_GraphVariablesDatabases == null)
            {
                // Add all variables in the graph to m_ExistingVariableGuids.
                m_ExistingVariableGuids = new HashSet<Hash128>(GraphModel.VariableDeclarations.Count);
                for (var i = 0; i < GraphModel.VariableDeclarations.Count; i++)
                {
                    m_ExistingVariableGuids.Add(GraphModel.VariableDeclarations[i].Guid);
                }

                return m_GraphVariablesDatabases = new List<ItemLibraryDatabaseBase>
                {
                    InitialGraphVariablesDatabase().Build()
                };
            }

            // If there are already variable databases, we check if they need to be recreated because the graph's variables have changed.
            var shouldRecreateVariableDatabases = false;
            var existingVariableGuidsToRemove = new HashSet<Hash128>(m_ExistingVariableGuids);
            for (var i = 0; i < GraphModel.VariableDeclarations.Count; i++)
            {
                if (m_ExistingVariableGuids.Add(GraphModel.VariableDeclarations[i].Guid))
                {
                    // A new variable was added, we must recreate the variable databases.
                    shouldRecreateVariableDatabases = true;
                }
                else
                {
                    // The variable is part of the graph and already in m_ExistingVariableGuids. We don't remove its guid from m_ExistingVariableGuids.
                    existingVariableGuidsToRemove.Remove(GraphModel.VariableDeclarations[i].Guid);
                }
            }

            // Remove guids of variables that aren't in the graph anymore from m_ExistingVariableGuids.
            foreach (var guidToRemove in existingVariableGuidsToRemove)
            {
                if (m_ExistingVariableGuids.Remove(guidToRemove))
                {
                    // A variable was removed, we must recreate the variable databases.
                    shouldRecreateVariableDatabases = true;
                }
            }

            if (shouldRecreateVariableDatabases)
            {
                m_GraphVariablesDatabases = new List<ItemLibraryDatabaseBase>
                {
                    InitialGraphVariablesDatabase().Build()
                };
            }

            return m_GraphVariablesDatabases;
        }

        /// <summary>
        /// Creates the initial database used for graph variables.
        /// </summary>
        /// <returns>A database containing the items for variables.</returns>
        protected virtual GraphElementItemDatabase InitialGraphVariablesDatabase()
        {
            return new GraphElementItemDatabase(GraphModel)
                .AddGraphVariables(GraphModel);
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetDynamicDatabases(PortModel portModel)
        {
            return portModel != null && portModel.DataTypeHandle != TypeHandle.Automatic ? GetVariableFromPortDatabase(portModel) : k_NoDatabase;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ItemLibraryDatabaseBase> GetDynamicDatabases(
            IEnumerable<PortModel> portModels)
        {
            // Provide "new variable of port type" item only if all port models have the same type and direction
            PortModel firstPort = null;
            var areSame = true;

            foreach (var portModel in portModels)
            {
                if (firstPort == null)
                {
                    firstPort = portModel;
                    continue;
                }

                if (portModel.DataTypeHandle != firstPort.DataTypeHandle ||
                    portModel.Direction != firstPort.Direction ||
                    portModel.PortType != firstPort.PortType)
                {
                    areSame = false;
                    break;
                }
            }

            return firstPort != null && firstPort.DataTypeHandle != TypeHandle.Automatic && areSame ? GetVariableFromPortDatabase(firstPort) : k_NoDatabase;
        }

        /// <summary>
        /// Creates the database used for a variable to create from a port.
        /// </summary>
        /// <param name="portModel">The port from which the variable is created.</param>
        /// <returns>A database containing a <see cref="ItemLibraryItem"/> for a variable to create from a port.</returns>
        protected virtual List<ItemLibraryDatabaseBase> GetVariableFromPortDatabase(PortModel portModel)
        {
            var variableInfos = new VariableCreationInfos
            {
                ModifierFlags = portModel.Direction == PortDirection.Input ? ModifierFlags.None : ModifierFlags.Write,
                TypeHandle = portModel.DataTypeHandle
            };

            return new List<ItemLibraryDatabaseBase>
            {
                new GraphElementItemDatabase(portModel.GraphModel).AddVariableFromPort(portModel, variableInfos).Build()
            };
        }

        /// <summary>
        /// For tests. Resets Graph Elements Databases to force invalidating the cached version.
        /// </summary>
        internal void ResetGraphElementsDatabases()
        {
            m_GraphElementsDatabases = null;
        }
    }
}
