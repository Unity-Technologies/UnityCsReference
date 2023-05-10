// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Utility/Factory methods to generate a <see cref="CreateNodeCommand"/>.
    /// </summary>
    static class CreateNodeCommandExtensions
    {
        /// <summary>
        /// Adds a g<see cref="GraphNodeModelLibraryItem"/> to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> to create a node from.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnGraph(this CreateNodeCommand command, GraphNodeModelLibraryItem item, Vector2 position, Hash128 guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                LibraryItem = item,
                Position = position,
                Guid = guid
            });
        }

        /// <summary>
        /// Adds a variable to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="variableDeclaration">The variable to create.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnGraph(this CreateNodeCommand command, VariableDeclarationModel variableDeclaration, Vector2 position, Hash128 guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                VariableDeclaration = variableDeclaration,
                Position = position,
                Guid = guid
            });
        }

        /// <summary>
        /// Adds a node from a <see cref="GraphNodeModelLibraryItem"/> inserted in the middle of a wire to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> to create a node from.</param>
        /// <param name="wireModel">The wire on which to insert the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnWire(this CreateNodeCommand command, GraphNodeModelLibraryItem item, WireModel wireModel, Vector2 position, Hash128 guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                LibraryItem = item,
                WireToInsertOn = wireModel,
                Position = position,
                Guid = guid
            });
        }

        /// <summary>
        /// Adds a node from a <see cref="GraphNodeModelLibraryItem"/> inserted on a wire to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> to create a node from.</param>
        /// <param name="wires">The wires on which to insert the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnWires(this CreateNodeCommand command, GraphNodeModelLibraryItem item, IEnumerable<(WireModel, WireSide)> wires, Vector2 position, Hash128 guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                LibraryItem = item,
                WiresToConnect = wires,
                Position = position,
                Guid = guid
            });
        }

        /// <summary>
        /// Adds a node from a <see cref="GraphNodeModelLibraryItem"/> connected to a port to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> to create a node from.</param>
        /// <param name="portModel">The port on which to connect the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="autoAlign">If true, the node will try to align automatically with the port after creation.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnPort(this CreateNodeCommand command, GraphNodeModelLibraryItem item, PortModel portModel, Vector2 position, bool autoAlign = false, Hash128 guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                LibraryItem = item,
                PortModel = portModel,
                Position = position,
                Guid = guid,
                AutoAlign = autoAlign
            });
        }

        /// <summary>
        /// Adds variable connected to a port to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="variableDeclaration">The variable to create.</param>
        /// <param name="portModel">The port on which to connect the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="autoAlign">If true, the node will try to align automatically with the port after creation.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnPort(this CreateNodeCommand command, VariableDeclarationModel variableDeclaration, PortModel portModel, Vector2 position, bool autoAlign = false, Hash128 guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                VariableDeclaration = variableDeclaration,
                PortModel = portModel,
                Position = position,
                Guid = guid,
                AutoAlign = autoAlign
            });
        }

        static CreateNodeCommand WithNode(this CreateNodeCommand command, CreateNodeCommand.NodeData data)
        {
            command.CreationData.Add(data);
            return command;
        }
    }
}
