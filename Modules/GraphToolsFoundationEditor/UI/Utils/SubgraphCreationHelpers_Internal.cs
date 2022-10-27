// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Helper methods to create a subgraph.
    /// </summary>
    static class SubgraphCreationHelpers_Internal
    {
        /// <summary>
        /// The data needed to recreate graph elements in a subgraph.
        /// </summary>
        internal struct GraphElementsToAddToSubgraph_Internal
        {
            public HashSet<StickyNoteModel> StickyNoteModels;
            public HashSet<PlacematModel> PlacematModels;
            public HashSet<WireModel> WireModels;
            public HashSet<AbstractNodeModel> NodeModels;

            internal static GraphElementsToAddToSubgraph_Internal ConvertToGraphElementsToAdd_Internal(IEnumerable<GraphElementModel> sourceElements)
            {
                GraphElementsToAddToSubgraph_Internal elementsToAdd;

                elementsToAdd.StickyNoteModels = new HashSet<StickyNoteModel>();
                elementsToAdd.PlacematModels = new HashSet<PlacematModel>();
                elementsToAdd.WireModels = new HashSet<WireModel>();
                elementsToAdd.NodeModels = new HashSet<AbstractNodeModel>();

                SortElementsByType(elementsToAdd, sourceElements);

                return elementsToAdd;
            }

            static void SortElementsByType(GraphElementsToAddToSubgraph_Internal elementsToAdd, IEnumerable<GraphElementModel> sourceElements)
            {
                foreach (var element in sourceElements)
                {
                    if (element is IGraphElementContainer container)
                        SortContainer(elementsToAdd, container);

                    switch (element)
                    {
                        case StickyNoteModel stickyNoteModel:
                            elementsToAdd.StickyNoteModels.Add(stickyNoteModel);
                            break;
                        case PlacematModel placematModel:
                            elementsToAdd.PlacematModels.Add(placematModel);
                            break;
                        case WireModel wireModel:
                            elementsToAdd.WireModels.Add(wireModel);
                            break;
                        case AbstractNodeModel nodeModel:
                            if (nodeModel.HasCapability(Capabilities.NeedsContainer))
                                SortContainer(elementsToAdd, nodeModel.Container);
                            elementsToAdd.NodeModels.Add(nodeModel);
                            break;
                    }
                }
            }

            static void SortContainer(GraphElementsToAddToSubgraph_Internal elementsToAdd, IGraphElementContainer container)
            {
                if (container is AbstractNodeModel containerNodeModel)
                {
                    if (elementsToAdd.NodeModels.Contains(containerNodeModel))
                        return;

                    elementsToAdd.NodeModels.Add(containerNodeModel);
                    SortElementsByType(elementsToAdd, container.GraphElementModels);
                }
                else
                {
                    // TODO: IGraphElementContainer might not be NodeModelBase in the future, it should be added to the right type HashSet
                    SortElementsByType(elementsToAdd, container.GraphElementModels);
                }
            }
        }

        /// <summary>
        /// Populates the subgraph with variable declarations and graph elements.
        /// </summary>
        /// <param name="graphModel">The subgraph.</param>
        /// <param name="sourceElementsToAdd">The selected graph elements to be recreated in the subgraph.</param>
        /// <param name="allWires">The wire models to be recreated in the subgraph.</param>
        /// <param name="inputWireConnections">A dictionary of input wire connections to their corresponding subgraph node port's unique name.</param>
        /// <param name="outputWireConnections">A dictionary of output wire connections to their corresponding subgraph node port's unique name.</param>
        internal static void PopulateSubgraph_Internal(GraphModel graphModel,
            GraphElementsToAddToSubgraph_Internal sourceElementsToAdd, IEnumerable<WireModel> allWires,
            Dictionary<WireModel, string> inputWireConnections, Dictionary<WireModel, string> outputWireConnections)
        {
            // Add input and output variable declarations to the subgraph
            CreateVariableDeclaration_Internal(graphModel, inputWireConnections, true);
            CreateVariableDeclaration_Internal(graphModel, outputWireConnections, false);

            // Add the graph elements to the subgraph
            AddGraphElementsToSubgraph_Internal(graphModel, sourceElementsToAdd, allWires, inputWireConnections, outputWireConnections);
        }

        /// <summary>
        /// Creates all the wires connected to the subgraph node.
        /// </summary>
        /// <param name="graphModel">The graph asset of the subgraph.</param>
        /// <param name="subgraphNode">The selected graph elements to be recreated in the subgraph.</param>
        /// <param name="inputWireConnections">A dictionary of input wire connections to their corresponding subgraph node port's unique name.</param>
        /// <param name="outputWireConnections">A dictionary of output wire connections to their corresponding subgraph node port's unique name.</param>
        internal static IEnumerable<WireModel> CreateWiresConnectedToSubgraphNode_Internal(GraphModel graphModel, SubgraphNodeModel subgraphNode, Dictionary<WireModel, string> inputWireConnections, Dictionary<WireModel, string> outputWireConnections)
        {
            var newWires = new List<WireModel>();

            var subgraphNodeInputPorts = subgraphNode.DataInputPortToVariableDeclarationDictionary.Keys.Concat(subgraphNode.ExecutionInputPortToVariableDeclarationDictionary.Keys).ToList();
            var subgraphNodeOutputPorts = subgraphNode.DataOutputPortToVariableDeclarationDictionary.Keys.Concat(subgraphNode.ExecutionOutputPortToVariableDeclarationDictionary.Keys).ToList();

            CreateWiresConnectedToSubgraphNode_Internal(newWires, graphModel, subgraphNodeInputPorts, inputWireConnections, true);
            CreateWiresConnectedToSubgraphNode_Internal(newWires, graphModel, subgraphNodeOutputPorts, outputWireConnections, false);

            return newWires;
        }

        internal static void CreateVariableDeclaration_Internal(GraphModel graphModel, Dictionary<WireModel, string> wireConnections, bool isInput)
        {
            foreach (var wire in wireConnections.Keys.ToList())
            {
                var portToSubgraph = isInput ? wire.ToPort : wire.FromPort;
                var newGuid = portToSubgraph.Guid;
                var variable = graphModel.VariableDeclarations.FirstOrDefault(v => v.Guid == newGuid);
                if (variable == null)
                {
                    var otherPort = isInput ? wire.FromPort : wire.ToPort;
                    variable = graphModel.CreateGraphVariableDeclaration(otherPort.DataTypeHandle, portToSubgraph.Title, isInput ? ModifierFlags.Read : ModifierFlags.Write, true, guid: newGuid);
                }

                // Used to keep track of the wire connections to the right subgraph node ports. The subgraph node ports will have the corresponding variable's guid as port id.
                wireConnections[wire] = variable.Guid.ToString();
            }
        }

        internal static void AddGraphElementsToSubgraph_Internal(GraphModel graphModel,
            GraphElementsToAddToSubgraph_Internal sourceGraphElementToAdd, IEnumerable<WireModel> allWires,
            Dictionary<WireModel, string> inputWireConnections,
            Dictionary<WireModel, string> outputWireConnections)
        {
            var elementMapping = new Dictionary<string, GraphElementModel>();

            if (sourceGraphElementToAdd.NodeModels != null)
            {
                foreach (var sourceNode in sourceGraphElementToAdd.NodeModels)
                {
                    // Ignore Blocks. They are duplicated when their Context is duplicated.
                    if (sourceNode.HasCapability(Capabilities.NeedsContainer))
                        continue;

                    var pastedNode = graphModel.DuplicateNode(sourceNode, Vector2.zero);
                    elementMapping.Add(sourceNode.Guid.ToString(), pastedNode);

                    if (sourceNode is IGraphElementContainer sourceContainer && pastedNode is IGraphElementContainer pastedContainer)
                    {
                        using (var pastedIter = pastedContainer.GraphElementModels.GetEnumerator())
                        using (var sourceIter = sourceContainer.GraphElementModels.GetEnumerator())
                        {
                            while (pastedIter.MoveNext() && sourceIter.MoveNext())
                            {
                                if (sourceIter.Current is AbstractNodeModel sourceElement && pastedIter.Current is AbstractNodeModel pastedElement)
                                {
                                    elementMapping.Add(sourceElement.Guid.ToString(), pastedElement);
                                }
                            }
                        }
                    }
                }
            }

            if (allWires != null)
            {
                const float offset = 20;
                var existingPositions = new List<Vector2>();

                foreach (var sourceWire in allWires)
                {
                    elementMapping.TryGetValue(sourceWire.ToNodeGuid.ToString(), out var newInput);
                    elementMapping.TryGetValue(sourceWire.FromNodeGuid.ToString(), out var newOutput);

                    if (newOutput == null)
                    {
                        var toPortId = inputWireConnections[sourceWire];

                        // input data
                        var declarationModel = graphModel.VariableDeclarations.FirstOrDefault(v => v.Guid.ToString() == toPortId);
                        if (declarationModel != null)
                        {
                            var inputPortModel = (newInput as InputOutputPortsNodeModel)?.InputsById[sourceWire.ToPortId];
                            // If the port is already connected to a variable node with the same declaration, do not create a new variable node
                            if (inputPortModel != null && !inputPortModel.GetConnectedPorts().Any(p => p.NodeModel is VariableNodeModel variableNode && variableNode.VariableDeclarationModel.Guid == declarationModel.Guid))
                            {
                                var variableNodeModel = graphModel.CreateVariableNode(declarationModel, GetNewVariablePosition_Internal(existingPositions, sourceWire.FromPort.NodeModel.Position, offset));
                                graphModel.CreateWire(inputPortModel, variableNodeModel.OutputPort);
                            }
                        }
                    }
                    else if (newInput == null)
                    {
                        var fromPortId = outputWireConnections[sourceWire];

                        // output data
                        var declarationModel = graphModel.VariableDeclarations.FirstOrDefault(v => v.Guid.ToString() == fromPortId);
                        if (declarationModel != null)
                        {
                            var outputPortModel = (newOutput as InputOutputPortsNodeModel)?.OutputsById[sourceWire.FromPortId];
                            // If the port is already connected to a variable node with the same declaration, do not create a new variable node
                            if (outputPortModel != null && !outputPortModel.GetConnectedPorts().Any(p => p.NodeModel is VariableNodeModel variableNode && variableNode.VariableDeclarationModel.Guid == declarationModel.Guid))
                            {
                                var variableNodeModel = graphModel.CreateVariableNode(declarationModel, GetNewVariablePosition_Internal(existingPositions, sourceWire.ToPort.NodeModel.Position, offset));
                                graphModel.CreateWire(variableNodeModel.InputPort, outputPortModel);
                            }
                        }
                    }
                    else
                    {
                        graphModel.DuplicateWire(sourceWire, newInput as AbstractNodeModel, newOutput as AbstractNodeModel);
                    }
                    elementMapping.Add(sourceWire.Guid.ToString(), sourceWire);
                }
            }

            if (sourceGraphElementToAdd.NodeModels != null)
            {
                foreach (var sourceVariableNode in sourceGraphElementToAdd.NodeModels.Where(model => model is VariableNodeModel))
                {
                    elementMapping.TryGetValue(sourceVariableNode.Guid.ToString(), out var newNode);
                    var variableDeclarationModel = graphModel.DuplicateGraphVariableDeclaration(((VariableNodeModel)sourceVariableNode).VariableDeclarationModel);

                    if (newNode != null)
                        ((VariableNodeModel)newNode).DeclarationModel = variableDeclarationModel;
                }
            }

            if (sourceGraphElementToAdd.StickyNoteModels != null)
            {
                foreach (var stickyNote in sourceGraphElementToAdd.StickyNoteModels)
                {
                    var newPosition = new Rect(stickyNote.PositionAndSize.position, stickyNote.PositionAndSize.size);
                    var pastedStickyNote = graphModel.CreateStickyNote(newPosition);
                    pastedStickyNote.Title = stickyNote.Title;
                    pastedStickyNote.Contents = stickyNote.Contents;
                    pastedStickyNote.Theme = stickyNote.Theme;
                    pastedStickyNote.TextSize = stickyNote.TextSize;
                    elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
                }
            }

            if (sourceGraphElementToAdd.PlacematModels != null)
            {
                var pastedPlacemats = new List<PlacematModel>();

                foreach (var placemat in sourceGraphElementToAdd.PlacematModels)
                {
                    var newPosition = new Rect(placemat.PositionAndSize.position, placemat.PositionAndSize.size);
                    var pastedPlacemat = graphModel.CreatePlacemat(newPosition);
                    pastedPlacemat.Title = placemat.Title;
                    pastedPlacemat.Color = placemat.Color;
                    pastedPlacemat.Collapsed = placemat.Collapsed;
                    pastedPlacemat.HiddenElements = (placemat).HiddenElements;
                    pastedPlacemats.Add(pastedPlacemat);
                    elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
                }
                // Update hidden content to new node ids.
                foreach (var pastedPlacemat in pastedPlacemats)
                {
                    if (pastedPlacemat.Collapsed)
                    {
                        foreach (var hiddenElement in pastedPlacemat.HiddenElements)
                        {
                            if (elementMapping.TryGetValue(hiddenElement.Guid.ToString(), out var pastedElement))
                            {
                                hiddenElement.SetGuid(pastedElement.Guid);
                            }
                        }
                    }
                }
            }
        }

        internal static Vector2 GetNewVariablePosition_Internal(List<Vector2> existingPositions, Vector2 position, float offset)
        {
            while (existingPositions.Any(p => (p - position).sqrMagnitude < offset * offset))
            {
                position.x += offset;
                position.y += offset;
            }
            existingPositions.Add(position);

            return position;
        }

        internal static void CreateWiresConnectedToSubgraphNode_Internal(List<WireModel> newWires, GraphModel graphModel, List<PortModel> portsOnSubgraphNode, Dictionary<WireModel, string> wireConnections, bool isInput)
        {
            foreach (var wireConnection in wireConnections)
            {
                var portOnSubgraphNode = portsOnSubgraphNode.FirstOrDefault(p => p.UniqueName == wireConnection.Value);
                var wire = wireConnection.Key;

                newWires.Add(isInput ? graphModel.CreateWire(portOnSubgraphNode, wire.FromPort) : graphModel.CreateWire(wire.ToPort, portOnSubgraphNode));
            }
        }
    }
}
