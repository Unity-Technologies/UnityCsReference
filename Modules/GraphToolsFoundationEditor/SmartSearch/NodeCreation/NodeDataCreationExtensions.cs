// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extensions methods for <see cref="IGraphNodeCreationData"/>.
    /// </summary>
    static class NodeDataCreationExtensions
    {
        /// <summary>
        /// Creates a new node in the graph referenced in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Data containing some of the required information to create a node.</param>
        /// <param name="nodeTypeToCreate">The type of the new node to create.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <returns>The newly created node.</returns>
        public static AbstractNodeModel CreateNode(this IGraphNodeCreationData data, Type nodeTypeToCreate, string nodeName = null, Action<AbstractNodeModel> initializationCallback = null)
        {
            return data.GraphModel.CreateNode(nodeTypeToCreate, nodeName, data.Position, data.Guid, initializationCallback, data.SpawnFlags);
        }

        /// <summary>
        /// Creates a new block in a context referenced in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Data containing some of the required information to create a node.</param>
        /// <param name="nodeTypeToCreate">The type of the new node to create.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="contextTypeToCreate">The type of context to create when the block is created for display in the library. Must not ne null in this case.</param>
        /// <returns>The newly created node.</returns>
        public static AbstractNodeModel CreateBlock(this IGraphNodeCreationData data, Type nodeTypeToCreate, Action<AbstractNodeModel> initializationCallback = null, Type contextTypeToCreate = null)
        {
            if (data is GraphBlockCreationData blockData)
                return blockData.ContextNodeModel.CreateAndInsertBlock(
                    nodeTypeToCreate, blockData.OrderInContext, data.Guid, initializationCallback, data.SpawnFlags);

            //This code path is only meant to display the block in the Item Library
            if (data.SpawnFlags != SpawnFlags.Orphan)
                return null;

            if (contextTypeToCreate == null)
            {
                throw new ArgumentNullException(nameof(contextTypeToCreate));
            }

            if (!typeof(ContextNodeModel).IsAssignableFrom(contextTypeToCreate))
            {
                throw new ArgumentOutOfRangeException(nameof(contextTypeToCreate));
            }

            var context = data.GraphModel.CreateNode(contextTypeToCreate , "Dummy Context", data.Position, data.Guid,
                null, data.SpawnFlags);
            (context as ContextNodeModel)?.CreateAndInsertBlock(nodeTypeToCreate, -1, data.Guid, initializationCallback, data.SpawnFlags);

            return context;
        }

        /// <summary>
        /// Creates a new node in the graph referenced in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Data containing some of the required information to create a node.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <typeparam name="T">The type of the new node to create.</typeparam>
        /// <returns>The newly created node.</returns>
        public static T CreateNode<T>(this IGraphNodeCreationData data, string nodeName = null, Action<T> initializationCallback = null) where T : AbstractNodeModel
        {
            return data.GraphModel?.CreateNode(nodeName, data.Position, data.Guid, initializationCallback, data.SpawnFlags);
        }

        /// <summary>
        /// Creates a new variable node in the graph referenced in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Data containing some of the required information to create a node.</param>
        /// <param name="declarationModel">Declaration model of the variable to create.</param>
        /// <returns>The newly created variable node.</returns>
        public static AbstractNodeModel CreateVariableNode(this IGraphNodeCreationData data, VariableDeclarationModel declarationModel)
        {
            return data.GraphModel.CreateVariableNode(declarationModel, data.Position, data.Guid, data.SpawnFlags);
        }

        /// <summary>
        /// Creates a new constant node in the graph referenced in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Data containing some of the required information to create a node.</param>
        /// <param name="constantName">Name of the constant to create.</param>
        /// <param name="typeHandle">Type of the constant to create.</param>
        /// <returns>The newly created constant node.</returns>
        public static AbstractNodeModel CreateConstantNode(this IGraphNodeCreationData data, string constantName, TypeHandle typeHandle)
        {
            return data.GraphModel.CreateConstantNode(typeHandle, constantName, data.Position, data.Guid, spawnFlags: data.SpawnFlags);
        }

        /// <summary>
        /// Creates a new subgraph node in the graph referenced in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Data containing some of the required information to create a node.</param>
        /// <param name="referenceGraph">The Graph Model of the reference graph.</param>
        /// <returns>The newly created subgraph node.</returns>
        public static AbstractNodeModel CreateSubgraphNode(this IGraphNodeCreationData data, GraphModel referenceGraph)
        {
            return data.GraphModel.CreateSubgraphNode(referenceGraph, data.Position, data.Guid, data.SpawnFlags);
        }
    }
}
