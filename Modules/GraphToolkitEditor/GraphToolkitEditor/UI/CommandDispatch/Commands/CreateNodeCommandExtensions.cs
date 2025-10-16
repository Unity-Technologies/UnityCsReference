// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Utility/Factory methods to generate a <see cref="CreateNodeCommand"/>.
    /// </summary>
    [UnityRestricted]
    internal static class CreateNodeCommandExtensions
    {
        /// <summary>
        /// Adds a <see cref="GraphNodeModelLibraryItem"/> to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="item">The <see cref="GraphNodeModelLibraryItem"/> to create a node from.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        /// <remarks>
        /// Called in <see cref="CreateNodeCommand.OnGraph(GraphNodeModelLibraryItem, Vector2, Hash128)"/>.
        /// Used in the creation of a node from the item library that is not initially connected to any wire.
        /// </remarks>
        public static CreateNodeCommand WithNodeOnGraph(this CreateNodeCommand command, GraphNodeModelLibraryItem item, Vector2 position, Hash128 guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                NodeLibraryItem = item,
                Position = position,
                Guid = guid
            });
        }

        /// <summary>
        /// Adds a variable to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="item">The <see cref="VariableLibraryItem"/> representing the node to create.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="group">The group to insert the variable in.</param>
        /// <param name="indexInGroup">The index in the group where the variable will be inserted.</param>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnGraph(this CreateNodeCommand command, VariableLibraryItem item,
            Vector2 position = default,
            GroupModel group = null,
            int indexInGroup = -1,
            string variableName = "",
            Hash128 guid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                VariableCreationInfos = new VariableCreationInfos
                {
                    Name = !string.IsNullOrEmpty(variableName) ? variableName : "New Variable",
                    VariableType = item.VariableType,
                    TypeHandle = item.Type,
                    Scope = item.Scope,
                    ModifierFlags = item.ModifierFlags,
                    Group = group,
                    IndexInGroup = indexInGroup
                },
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
        public static CreateNodeCommand WithNodeOnGraph(this CreateNodeCommand command, VariableDeclarationModelBase variableDeclaration, Vector2 position, Hash128 guid = default)
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
                NodeLibraryItem = item,
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
        /// <param name="group">The group to insert the variable in.</param>
        /// <param name="indexInGroup">The index in the group where the variable will be inserted.</param>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnWires(this CreateNodeCommand command, VariableLibraryItem item,
            IEnumerable<(WireModel, WireSide)> wires,
            Vector2 position, GroupModel group = null,
            int indexInGroup = -1,
            string variableName = "",
            Hash128 guid = default)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                WireModel firstWire = null;
                var wireSide = WireSide.From;
                foreach (var (wire, side) in wires)
                {
                    firstWire = wire;
                    wireSide = side;
                    break;
                }

                var existingPort = firstWire?.GetOtherPort(wireSide);
                variableName = existingPort == null ? "" : !string.IsNullOrEmpty(existingPort.Title) ? existingPort.Title :
                    existingPort.Direction == PortDirection.Input ? "Input" : "Output";
            }

            return command.WithNode(new CreateNodeCommand.NodeData
            {
                VariableCreationInfos = new VariableCreationInfos
                {
                    Name = variableName,
                    VariableType = item.VariableType,
                    TypeHandle = item.Type,
                    Scope = item.Scope,
                    ModifierFlags = item.ModifierFlags,
                    Group = group,
                    IndexInGroup = indexInGroup
                },
                WiresToConnect = wires,
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
                NodeLibraryItem = item,
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
                NodeLibraryItem = item,
                PortModel = portModel,
                Position = position,
                Guid = guid,
                AutoAlign = autoAlign
            });
        }

        /// <summary>
        /// Adds a variable connected to a port to a <see cref="CreateNodeCommand"/>.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="variableDeclaration">The variable to create.</param>
        /// <param name="portModel">The port on which to connect the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="autoAlign">If true, the node will try to align automatically with the port after creation.</param>
        /// <param name="guid">The unique identifier for the node to create.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnPort(this CreateNodeCommand command, VariableDeclarationModelBase variableDeclaration, PortModel portModel, Vector2 position, bool autoAlign = false, Hash128 guid = default)
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

        /// <summary>
        /// Adds a variable node connected to a port to a <see cref="CreateNodeCommand"/>. Also creates the corresponding variable declaration.
        /// </summary>
        /// <param name="command">The command to alter.</param>
        /// <param name="portModel">The port to connect the new node to. The data type and name of the variable is taken from the port.</param>
        /// <param name="variableCreationInfos">The <see cref="VariableCreationInfos"/> to create the variable.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="autoAlign">If true, the created node will be automatically aligned after being created.</param>
        /// <param name="variableDeclarationGuid">The guid to assign to the newly created variable declaration. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <param name="variableNodeGuid">The guid to assign to the newly created variable node. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <returns>The command with an additional node to create.</returns>
        public static CreateNodeCommand WithNodeOnPort(this CreateNodeCommand command, PortModel portModel,
            VariableCreationInfos variableCreationInfos,
            Vector2 position = default,
            bool autoAlign = true,
            Hash128 variableDeclarationGuid = default,
            Hash128 variableNodeGuid = default)
        {
            return command.WithNode(new CreateNodeCommand.NodeData
            {
                PortModel = portModel,
                Position = position,
                Guid = variableNodeGuid,
                VariableDeclarationGuid = variableDeclarationGuid,
                AutoAlign = autoAlign,
                VariableCreationInfos = variableCreationInfos
            });
        }

        static CreateNodeCommand WithNode(this CreateNodeCommand command, CreateNodeCommand.NodeData data)
        {
            command.CreationData.Add(data);
            return command;
        }
    }
}
