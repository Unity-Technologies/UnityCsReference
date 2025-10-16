// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Helper for the creation of a subgraph.
    /// </summary>
    [UnityRestricted]
    internal class SubgraphCreationHelper
    {
        /// <summary>
        /// The data needed to create a port on a subgraph node.
        /// </summary>
        protected struct SubgraphNodePortInfo
        {
            /// <summary>
            /// The unique name of the port on the subgraph node.
            /// </summary>
            public string PortUniqueName;
            /// <summary>
            /// Whether the port is an input on the subgraph node.
            /// </summary>
            public bool IsInput;
            /// <summary>
            /// The node in the main graph to which the port should be connected.
            /// </summary>
            public AbstractNodeModel NodeConnectedToSubgraphNode;
            /// <summary>
            /// The wires that were connected to the <see cref="NodeConnectedToSubgraphNode"/> in the main graph and to the source node that was transferred to the subgraph.
            /// </summary>
            public List<WireModel> SourceWires;
            /// <summary>
            /// Whether the port connections should be aligned.
            /// </summary>
            public bool AutoAlign;
        }

        /// <summary>
        /// Handles the creation of a subgraph node by transferring the selected graph elements to the subgraph and creating the connections to the subgraph node in the main graph.
        /// </summary>
        /// <param name="subgraphNodeModel">The newly created subgraph node.</param>
        /// <param name="selectedElements">The elements that were selected to be transferred to the subgraph.</param>
        /// <param name="elementsToDelete">Additional elements that need to be deleted from the main graph.</param>
        /// <param name="portIdsToAlign">The ids of the ports that need to have their connections aligned to them.</param>
        public void HandleSubgraphNodeCreation(SubgraphNodeModel subgraphNodeModel, List<GraphElementModel> selectedElements, List<GraphElementModel> elementsToDelete, List<string> portIdsToAlign)
        {
            if (subgraphNodeModel == null)
                return;

            var subgraphNodePortInfos = new List<SubgraphNodePortInfo>();
            var mainGraph = subgraphNodeModel.GraphModel;
            var subgraph = subgraphNodeModel.GetSubgraphModel();

            // 1. Get elements to transfer to subgraph.
            var elementsToTransfer = GetElementsToTransfer(selectedElements, subgraph);

            // 2. Populate the subgraph with the elements to transfer.
            PopulateSubgraph(subgraph, elementsToTransfer, subgraphNodePortInfos);

            // 3. Initialize the subgraph node in the current graph.
            subgraphNodeModel.DefineNode();
            InitializeSubgraphNode(subgraphNodeModel);

            // 4. Delete the elements that has been transferred to the subgraph.
            DeleteElementsInMainGraph(mainGraph, elementsToTransfer, elementsToDelete);

            // 5. Create wires to the subgraph node in the current graph.
            CreateWiresToSubgraphNode(mainGraph, subgraphNodeModel, subgraphNodePortInfos);

            // 6. Get all the ports that need to have their connected node aligned to it.
            GetPortsToAlignIds(subgraphNodePortInfos, portIdsToAlign);
        }

        /// <summary>
        /// Gets the elements to transfer to the created subgraph.
        /// </summary>
        /// <param name="sourceElements">The elements that were selected to be transferred.</param>
        /// <param name="newSubgraph">The subgraph model.</param>
        /// <returns>The elements that can be transferred to the subgraph.</returns>
        protected virtual List<GraphElementModel> GetElementsToTransfer(IEnumerable<GraphElementModel> sourceElements, GraphModel newSubgraph)
        {
            var elementsToTransfer = new List<GraphElementModel>();

            foreach (var element in sourceElements)
            {
                if (element is WirePortalModel && !newSubgraph.AllowPortalCreation)
                    continue;
                if (element is SubgraphNodeModel && !newSubgraph.AllowSubgraphCreation)
                    continue;
                if (element is NodeModel nodeModel && !newSubgraph.CanPasteNode(nodeModel))
                    continue;
                if (element is VariableDeclarationModelBase vdm && !newSubgraph.CanPasteVariable(vdm))
                    continue;

                elementsToTransfer.Add(element);

                // If the element is a container, recursively retrieve the contained elements.
                if (element is IGraphElementContainer container)
                    elementsToTransfer.AddRange(GetElementsToTransfer(container.GetGraphElementModels(), newSubgraph));
            }

            return elementsToTransfer;
        }

        /// <summary>
        /// Populates the subgraph with variable declarations and graph elements.
        /// </summary>
        /// <param name="newSubgraph">The created subgraph.</param>
        /// <param name="elementsToTransfer">The elements to transfer to the subgraph.</param>
        /// <param name="subgraphNodePortInfos">The information to create the ports on the associated subgraph node in the main graph.</param>
        protected virtual void PopulateSubgraph(GraphModel newSubgraph,
            List<GraphElementModel> elementsToTransfer, List<SubgraphNodePortInfo> subgraphNodePortInfos)
        {
            // 1. Transfer the elements to the subgraph.
            var nodeMapping = TransferElements(newSubgraph, elementsToTransfer);

            // We get the delta before adding the new inputs, outputs and portals.
            var repositionDelta = GetDeltaToCenter(newSubgraph);

            // 2. Create variable declarations in the subgraph.
            CreateVariableDeclarations(newSubgraph, elementsToTransfer, subgraphNodePortInfos);

            // 3. Create the variable nodes in the subgraph from the new declarations.
            CreateInputOutputVariableNodes(newSubgraph, elementsToTransfer, subgraphNodePortInfos, nodeMapping);

            // 4. Order the variable declarations properly.
            OrderVariableDeclarations(newSubgraph, subgraphNodePortInfos);

            // 5. Move all elements to the center of the subgraph.
            RepositionElements(newSubgraph, repositionDelta);
        }

        /// <summary>
        /// Initializes the subgraph node model after the subgraph has been created.
        /// </summary>
        /// <param name="subgraphNodeModel">The associated subgraph node in the main graph.</param>
        protected virtual void InitializeSubgraphNode(SubgraphNodeModel subgraphNodeModel) { }

        /// <summary>
        /// Deletes the graph elements in the main graph that were transferred to the subgraph.
        /// </summary>
        /// <param name="mainGraph">The main graph.</param>
        /// <param name="transferredElements">The transferred elements.</param>
        /// <param name="additionalElementsToDelete">Any additional element to delete.</param>
        protected virtual void DeleteElementsInMainGraph(GraphModel mainGraph, List<GraphElementModel> transferredElements, List<GraphElementModel> additionalElementsToDelete)
        {
            var elementsToDelete = new List<GraphElementModel>();

            if (additionalElementsToDelete != null)
                elementsToDelete.AddRange(additionalElementsToDelete);

            foreach (var element in transferredElements)
            {
                // Input, output and exposed variable nodes stay on the main graph, we do not delete them.
                if (element is not VariableNodeModel variableNode || (!variableNode.VariableDeclarationModel.IsInputOrOutput && variableNode.VariableDeclarationModel.Scope != VariableScope.Exposed))
                    elementsToDelete.Add(element);
            }

            mainGraph.DeleteElements(elementsToDelete);
        }

        /// <summary>
        /// Creates connections to the subgraph node in the main graph.
        /// </summary>
        /// <param name="mainGraph">The main graph.</param>
        /// <param name="subgraphNodeModel">The associated subgraph node.</param>
        /// <param name="subgraphNodePortInfos">The information on the ports of the subgraph node in the main graph.</param>
        protected virtual void CreateWiresToSubgraphNode(GraphModel mainGraph, SubgraphNodeModel subgraphNodeModel, List<SubgraphNodePortInfo> subgraphNodePortInfos)
        {
            // Get all the ports on the subgraph node.
            var subgraphNodeInputs = new List<PortModel>(subgraphNodeModel.InputPortToVariableDeclarationDictionary.Keys);
            var subgraphNodeOutputs = new List<PortModel>(subgraphNodeModel.OutputPortToVariableDeclarationDictionary.Keys);

            // Get the pairs of ports to create wires.
            var allPortPairsToCreateWires = new List<(PortModel, PortModel)>();
            foreach (var subgraphPortInfo in subgraphNodePortInfos)
            {
                var subgraphNodePorts = subgraphPortInfo.IsInput ? subgraphNodeInputs : subgraphNodeOutputs;
                PortModel subgraphNodePort = null;
                foreach (var otherSubgraphNodePort in subgraphNodePorts)
                {
                    if (subgraphPortInfo.PortUniqueName == otherSubgraphNodePort.UniqueName)
                    {
                        subgraphNodePort = otherSubgraphNodePort;
                        break;
                    }
                }

                if (subgraphNodePort == null)
                    continue;

                // Connect the subgraph node port to the node in the main graph

                // 1. Variable node connected to subgraph node
                if (subgraphPortInfo.NodeConnectedToSubgraphNode is VariableNodeModel)
                {
                    // Make sure the variable node is connected to the subgraph node.
                    var variableNodePort = ((ISingleInputPortNodeModel)subgraphPortInfo.NodeConnectedToSubgraphNode).InputPort ?? (subgraphPortInfo.NodeConnectedToSubgraphNode as ISingleOutputPortNodeModel).OutputPort;
                    if (variableNodePort == null)
                        continue;

                    allPortPairsToCreateWires.Add(subgraphPortInfo.IsInput ? (subgraphNodePort, variableNodePort) : (variableNodePort, subgraphNodePort));
                }
                // 2. Any node connected to the subgraph node with a wire
                else if (subgraphPortInfo.SourceWires != null)
                {
                    foreach (var oldWire in subgraphPortInfo.SourceWires)
                    {
                        var oldPort = subgraphPortInfo.IsInput ? oldWire.FromPort : oldWire.ToPort;
                        if (oldPort == null)
                            continue;
                        allPortPairsToCreateWires.Add(subgraphPortInfo.IsInput ? (subgraphNodePort, oldPort) : (oldPort, subgraphNodePort));
                    }
                }
                // 3. A portal that needs to be created and connected to the subgraph node
                else if (subgraphPortInfo.NodeConnectedToSubgraphNode is WirePortalModel portalModel && portalModel.CanCreateOppositePortal())
                {
                    // Portals that were transferred are replaced by 1 portal in the main graph per type (entry or exit)
                    var portalToCreate = mainGraph.CreateOppositePortal(portalModel);
                    switch (portalToCreate)
                    {
                        case ISingleInputPortNodeModel entryPortal:
                            allPortPairsToCreateWires.Add((entryPortal.InputPort, subgraphNodePort));
                            break;
                        case ISingleOutputPortNodeModel exitPortal:
                            allPortPairsToCreateWires.Add((subgraphNodePort, exitPortal.OutputPort));
                            break;
                    }
                }
            }

            // Create the new wires connected to the subgraph node.
            for (var i = 0; i < allPortPairsToCreateWires.Count; i++)
            {
                var toPort = allPortPairsToCreateWires[i].Item1;
                var fromPort = allPortPairsToCreateWires[i].Item2;

                if (toPort == null || fromPort == null)
                    continue;

                mainGraph.CreateWire(toPort, fromPort);
            }
        }

        /// <summary>
        /// Gets the ports that need to have their connection aligned in the subgraph node.
        /// </summary>
        /// <param name="subgraphNodePortInfos">The information on the ports of the subgraph node in the main graph.</param>
        /// <param name="portIdsToAlign">The ports to align.</param>
        protected virtual void GetPortsToAlignIds(List<SubgraphNodePortInfo> subgraphNodePortInfos, List<string> portIdsToAlign)
        {
            if (portIdsToAlign == null)
                return;

            foreach (var subgraphNodePortInfo in subgraphNodePortInfos)
            {
                if (subgraphNodePortInfo.AutoAlign)
                    portIdsToAlign.Add(subgraphNodePortInfo.PortUniqueName);
            }
        }

        /// <summary>
        /// Transfer the elements from the main graph to the subgraph.
        /// </summary>
        /// <param name="newSubgraph">The subgraph.</param>
        /// <param name="elementsToTransfer">The elements to transfer.</param>
        /// <returns>A mapping between the original and transferred elements.</returns>
        protected virtual Dictionary<Hash128, AbstractNodeModel> TransferElements(GraphModel newSubgraph, List<GraphElementModel> elementsToTransfer)
        {
            var listElements = new List<GraphElementModel>();

            foreach (var elementModel in elementsToTransfer)
            {
                listElements.Add(elementModel);

                if (elementModel is NodeModel nodeModel)
                {
                    foreach (var wireModel in nodeModel.GetConnectedWires())
                    {
                        listElements.Add(wireModel);
                    }
                }
            }

            var copyPasteData = new CopyPasteData(null, listElements);
            var nodeMapping = CopyPasteData.PasteSerializedData(
                PasteOperation.Duplicate, Vector2.zero, null, null, copyPasteData, newSubgraph, null, false, true);
            copyPasteData.Dispose();

            // Exposed variable nodes that have modifiers.none should be transferred to the subgraph as input or output
            foreach (var node in nodeMapping.Values)
            {
                if (node is VariableNodeModel variableNode && !variableNode.VariableDeclarationModel.IsInputOrOutput && variableNode.VariableDeclarationModel.Scope == VariableScope.Exposed)
                {
                    if (variableNode.OutputPort != null)
                    {
                        // Should be made an input
                        variableNode.VariableDeclarationModel.Modifiers = ModifierFlags.Read;
                    }
                    else if (variableNode.InputPort != null)
                    {
                        // Should be made an output
                        variableNode.VariableDeclarationModel.Modifiers = ModifierFlags.Write;
                    }
                }
            }

            return nodeMapping;
        }

        /// <summary>
        /// Creates a variable declaration for an input or output in the subgraph.
        /// </summary>
        /// <param name="newSubgraph">The subgraph.</param>
        /// <param name="transferredElements">The elements to transfer to the subgraph.</param>
        /// <param name="subgraphNodePortInfos">The information on the ports of the subgraph node in the main graph.</param>
        protected virtual void CreateVariableDeclarations(GraphModel newSubgraph, List<GraphElementModel> transferredElements,
            List<SubgraphNodePortInfo> subgraphNodePortInfos)
        {
            // Gets info from variable nodes to connect them to the subgraph node.
            GetInfosFromVariableNodes(transferredElements, subgraphNodePortInfos);

            // Create the variable declarations in the subgraph for empty ended wires.
            CreateVariableDeclarationsFromWires(newSubgraph, transferredElements, subgraphNodePortInfos);

            // Create the variable declarations in the subgraph for portals.
            CreateVariableDeclarationsFromPortals(newSubgraph, transferredElements, subgraphNodePortInfos);
        }

        /// <summary>
        /// Creates the input and output variable nodes in the subgraph.
        /// </summary>
        /// <param name="newSubgraph">The subgraph.</param>
        /// <param name="transferredElements">The transferred elements.</param>
        /// <param name="subgraphNodePortInfos">The information on the ports of the subgraph node in the main graph.</param>
        /// <param name="nodeMapping">The mapping of the transferred nodes.</param>
        protected virtual void CreateInputOutputVariableNodes(GraphModel newSubgraph, List<GraphElementModel> transferredElements,
            List<SubgraphNodePortInfo> subgraphNodePortInfos,
            Dictionary<Hash128, AbstractNodeModel> nodeMapping)
        {
            const float offset = 20;
            var existingPositions = new List<Vector2>();

            foreach (var subgraphNodePortInfo in subgraphNodePortInfos)
            {
                // 1. Handle variable nodes: Variable nodes are already added to the subgraph.
                if (subgraphNodePortInfo.NodeConnectedToSubgraphNode is VariableNodeModel sourceVariableNode && transferredElements.Contains(sourceVariableNode))
                    continue;

                // 2. Handle portals: When portals are transferred to the subgraph and not all portals of the same declaration are transferred;
                // - For ENTRY portals that weren't transferred: An input variable node and an entry portal node are created in the subgraph and connected by a wire;
                // - For EXIT portals that weren't transferred: An output variable node and an exit portal node are created in the subgraph and connected by a wire.
                if (TryCreateNodesForPortals(subgraphNodePortInfo, newSubgraph, nodeMapping, existingPositions, offset))
                    continue;

                // 3. Handle wires: Create a variable node for each wire with an empty end.
                CreateVariableNodesForWires(subgraphNodePortInfo, newSubgraph, nodeMapping, existingPositions, offset);
            }
        }

        /// <summary>
        /// Order the variable declarations in the subgraph.
        /// </summary>
        /// <param name="newSubgraph">The subgraph.</param>
        /// <param name="subgraphNodePortInfos">The information on the ports of the subgraph node in the main graph.</param>
        protected virtual void OrderVariableDeclarations(GraphModel newSubgraph, List<SubgraphNodePortInfo> subgraphNodePortInfos)
        {
            // If there is only 1 or no variable declaration, no need to sort them.
            if (newSubgraph.VariableDeclarations.Count < 2 || newSubgraph.VariableDeclarations[0].ParentGroup is not GroupModel parentGroup)
                return;

            // To create the subgraph node ports in order, the subgraph's variable declarations will be ordered based on the Y position of their source node.
            var portInfos = new List<(string variableGuidStr, float sourceNodePositionY)>();
            var outputPortInfos = new List<(string variableGuidStr, float sourceNodePositionY)>();
            foreach (var subgraphNodePortInfo in subgraphNodePortInfos)
            {
                if (subgraphNodePortInfo.IsInput)
                    portInfos.Add((subgraphNodePortInfo.PortUniqueName, subgraphNodePortInfo.NodeConnectedToSubgraphNode.Position.y));
                else
                    outputPortInfos.Add((subgraphNodePortInfo.PortUniqueName, subgraphNodePortInfo.NodeConnectedToSubgraphNode.Position.y));
            }

            // Sort inputs in vertical order
            portInfos.Sort((a, b) => a.sourceNodePositionY.CompareTo(b.sourceNodePositionY));

            // Sort outputs in vertical order
            outputPortInfos.Sort((a, b) => a.sourceNodePositionY.CompareTo(b.sourceNodePositionY));

            // Merge them together
            portInfos.AddRange(outputPortInfos);

            // Reinsert the variables in order.
            for (var i = 0; i < newSubgraph.VariableDeclarations.Count; i++)
            {
                var variableDeclaration = newSubgraph.VariableDeclarations[i];
                for (var j = 0; j < portInfos.Count; j++)
                {
                    if (variableDeclaration.Guid.ToString() == portInfos[j].variableGuidStr)
                    {
                        parentGroup.InsertItem(variableDeclaration, j);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Repositions the transferred elements in the subgraph. By default, they are repositioned to the center of the subgraph.
        /// </summary>
        /// <param name="newSubgraph">The subgraph.</param>
        /// <param name="delta">The delta to move the elements.</param>
        protected void RepositionElements(GraphModel newSubgraph, Vector2 delta)
        {
            foreach (var element in newSubgraph.GetGraphElementModels())
            {
                if (element is IMovable movable)
                    movable.Position += delta;
            }
        }

        /// <summary>
        /// Gets the delta to move the elements to the center of the subgraph.
        /// </summary>
        /// <param name="newSubgraph">The subgraph.</param>
        /// <returns>The delta.</returns>
        protected Vector2 GetDeltaToCenter(GraphModel newSubgraph)
        {
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;
            foreach (var element in newSubgraph.GetGraphElementModels())
            {
                if (element is not IMovable movable)
                    continue;

                if (movable.Position.x < minX)
                    minX = movable.Position.x;
                if (movable.Position.y < minY)
                    minY = movable.Position.y;
                if (movable.Position.x > maxX)
                    maxX = movable.Position.x;
                if (movable.Position.y > maxY)
                    maxY = movable.Position.y;
            }
            var bounds = Rect.MinMaxRect(minX, minY, maxX, maxY);

            return -bounds.center;
        }

        /// <summary>
        /// Generates a unique name for a local graph within its main graph model.
        /// </summary>
        /// <param name="localSubgraphs">The local subgraphs.</param>
        /// <param name="targetSubgraph">The subgraph for which the name is to be generated</param>
        /// <param name="wantedName">The wanted name for the local graph.</param>
        /// <returns>A unique local subgraph name.</returns>
        public static string GenerateLocalGraphUniqueName(IReadOnlyList<GraphModel> localSubgraphs, GraphModel targetSubgraph, string wantedName)
        {
            var foundSameName = false;
            for (var i = 0; i < localSubgraphs.Count; i++)
            {
                if (localSubgraphs[i] == targetSubgraph)
                    continue;
                if (localSubgraphs[i].Name == wantedName)
                {
                    foundSameName = true;
                    break;
                }
            }

            if (!foundSameName)
                return wantedName;

            var existingLocalGraphNames = new string[localSubgraphs.Count];

            for (var i = 0; i < existingLocalGraphNames.Length; i++)
            {
                existingLocalGraphNames[i] = localSubgraphs[i].Name;
            }

            return ObjectNames.GetUniqueName(existingLocalGraphNames, wantedName);
        }

        /// <summary>
        /// The default name for a local subgraph.
        /// </summary>
        public static string defaultLocalSubgraphName = "Local Subgraph";

        /// <summary>
        /// Computes the bounds of the subgraph.
        /// </summary>
        /// <param name="graphElementModels">The elements that will be part of the subgraph.</param>
        /// <param name="graphView">The graphview that contains the elements.</param>
        /// <param name="newSubgraph">The subgraph.</param>
        public static void ComputeSubgraphBounds(List<GraphElementModel> graphElementModels, GraphView graphView, GraphModel newSubgraph)
        {
            if (graphElementModels is null || graphElementModels.Count == 0)
                return;

            var bounds = Rect.zero;
            for (var i = 0; i < graphElementModels.Count; i++)
            {
                var element = graphElementModels[i];
                if (element is null || element is WireModel)
                    continue;

                var elementView = element.GetView(graphView);
                if (elementView is null)
                    continue;

                bounds = bounds == Rect.zero ? elementView.layout : RectUtils.Encompass(bounds, elementView.layout);
            }
            newSubgraph.LastKnownBounds = bounds;
        }

        /// <summary>
        /// Gets the position for a newly created variable node.
        /// </summary>
        /// <param name="existingPositions">The positions that are already taken by other nodes.</param>
        /// <param name="position">The original position of the node.</param>
        /// <param name="offset">The desired offset.</param>
        protected Vector2 GetNewVariablePosition(List<Vector2> existingPositions, Vector2 position, float offset)
        {
            while (IsOverlapping())
            {
                position.x += offset;
                position.y += offset;
            }
            existingPositions.Add(position);

            return position;

            bool IsOverlapping()
            {
                foreach (var existingPosition in existingPositions)
                {
                    if ((existingPosition - position).sqrMagnitude < offset * offset)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Creates input and output variable declarations from wires with empty ends.
        /// </summary>
        /// <param name="newSubgraph">The subgraph.</param>
        /// <param name="transferredElements">The transferred elements.</param>
        /// <param name="subgraphNodePortInfos">The information on the ports of the subgraph node in the main graph.</param>
        protected void CreateVariableDeclarationsFromWires(GraphModel newSubgraph, List<GraphElementModel> transferredElements, List<SubgraphNodePortInfo> subgraphNodePortInfos)
        {
            // Get the selected wires AND wires that weren't selected but are connected to selected nodes.
            var allWires = new List<WireModel>();
            foreach (var element in transferredElements)
            {
                if (element is WireModel wireModel)
                    allWires.Add(wireModel);
                if (element is NodeModel nodeModel)
                    allWires.AddRange(nodeModel.GetConnectedWires());
            }

            foreach (var wire in allWires)
            {
                var toPortNode = wire.ToPort.NodeModel;
                var fromPortNode = wire.FromPort.NodeModel;

                // If both nodes are part of the subgraph, no need to create a variable declaration.
                if (transferredElements.Contains(fromPortNode) && transferredElements.Contains(toPortNode))
                    continue;

                // If both nodes have the same direction, not possible to create a variable declaration.
                var isInput = !transferredElements.Contains(fromPortNode) && transferredElements.Contains(toPortNode);
                var isOutput = !transferredElements.Contains(toPortNode) && transferredElements.Contains(fromPortNode);
                if (isInput == isOutput)
                    continue;

                // Create the variable declaration.
                var portNotInSubgraph = isInput ? wire.FromPort : wire.ToPort;
                var portInSubgraph = isInput ? wire.ToPort : wire.FromPort;
                var variableGuid = portInSubgraph.Guid; // can be anything
                var title = GetVariableTitle(portInSubgraph, portNotInSubgraph);

                var newVariableDeclaration = CreateVariableDeclaration(newSubgraph, variableGuid, title, portNotInSubgraph.DataTypeHandle, isInput);
                if (newVariableDeclaration == null)
                    continue;

                // Keep the data to create the associated port on the subgraph node.
                subgraphNodePortInfos.Add(new SubgraphNodePortInfo
                {
                    IsInput = isInput,
                    PortUniqueName = variableGuid.ToString(),
                    NodeConnectedToSubgraphNode = portNotInSubgraph.NodeModel,
                    SourceWires = new List<WireModel> { wire }
                });
            }
        }

        /// <summary>
        /// Creates input and output variable declarations from portals.
        /// </summary>
        /// <param name="newSubgraph">The subgraph.</param>
        /// <param name="transferredElements">The transferred elements.</param>
        /// <param name="subgraphNodePortInfos">The information on the ports of the subgraph node in the main graph.</param>
        protected void CreateVariableDeclarationsFromPortals(GraphModel newSubgraph, List<GraphElementModel> transferredElements, List<SubgraphNodePortInfo> subgraphNodePortInfos)
        {
            // Get portal nodes
            var portalsToTransfer = new HashSet<WirePortalModel>();
            foreach (var element in transferredElements)
            {
                if (element is WirePortalModel portalModel)
                    portalsToTransfer.Add(portalModel);
            }

            if (portalsToTransfer.Count == 0)
                return;

            // We only create 1 variable declaration for the entry portals and 1 variable declaration for the exit portals of the same declaration.
            var alreadyHandled = new HashSet<DeclarationModel>();
            foreach (var portalModel in portalsToTransfer)
            {
                if (!alreadyHandled.Add(portalModel.DeclarationModel))
                    continue;

                // A declaration is handled only if it has both an entry and an exit portals
                var hasEntryPortal = false;
                var hasExitPortal = false;

                // A declaration is handled only if at least one portal is not transferred to the subgraph
                WirePortalModel notTransferredEntryPortal = null;
                WirePortalModel notTransferredExitPortal = null;

                // Iterate through all portals of the same declaration to verify those criteria.
                var portalsWithSameDeclaration = portalModel.GraphModel.FindReferencesInGraph<WirePortalModel>(portalModel.DeclarationModel);
                foreach (var otherPortal in portalsWithSameDeclaration)
                {
                    if (otherPortal is ISingleInputPortNodeModel)
                        hasEntryPortal = true;

                    if (otherPortal is ISingleOutputPortNodeModel)
                        hasExitPortal = true;

                    if (!portalsToTransfer.Contains(otherPortal))
                    {
                        if (otherPortal is ISingleInputPortNodeModel)
                            notTransferredEntryPortal = otherPortal;

                        if (otherPortal is ISingleOutputPortNodeModel)
                            notTransferredExitPortal = otherPortal;
                    }
                }

                // The declaration doesn't respect the criteria
                if (!hasEntryPortal || !hasExitPortal)
                    continue;

                // We only handle when not all portals are transferred to the subgraph.
                // Case 1: An exit portal wasn't transferred
                if (notTransferredExitPortal != null && (notTransferredEntryPortal == null || notTransferredEntryPortal.CanHaveAnotherPortalWithSameDirectionAndDeclaration()))
                {
                    CreateVariableDeclaration(newSubgraph, notTransferredExitPortal.Guid, notTransferredExitPortal.Title, notTransferredExitPortal.GetPortDataTypeHandle(), false);
                    subgraphNodePortInfos.Add(new SubgraphNodePortInfo
                    {
                        IsInput = false,
                        PortUniqueName = notTransferredExitPortal.Guid.ToString(),
                        NodeConnectedToSubgraphNode = notTransferredExitPortal,
                        AutoAlign = true
                    });
                }

                // Case 2: An entry portal wasn't transferred
                if (notTransferredEntryPortal != null && (notTransferredExitPortal == null || notTransferredExitPortal.CanHaveAnotherPortalWithSameDirectionAndDeclaration()))
                {
                    CreateVariableDeclaration(newSubgraph, notTransferredEntryPortal.Guid, notTransferredEntryPortal.Title, notTransferredEntryPortal.GetPortDataTypeHandle(), true);
                    subgraphNodePortInfos.Add(new SubgraphNodePortInfo
                    {
                        IsInput = true,
                        PortUniqueName = notTransferredEntryPortal.Guid.ToString(),
                        NodeConnectedToSubgraphNode = notTransferredEntryPortal,
                        AutoAlign = true
                    });
                }
            }
        }

        /// <summary>
        /// Gets <see cref="SubgraphNodePortInfo"/>'s from transferred variable nodes.
        /// </summary>
        /// <param name="transferredElements">The transferred elements.</param>
        /// <param name="subgraphNodePortInfos">The information on the ports of the subgraph node in the main graph.</param>
        protected void GetInfosFromVariableNodes(List<GraphElementModel> transferredElements, List<SubgraphNodePortInfo> subgraphNodePortInfos)
        {
            // Keep info on variable nodes that stay on the main graph (input, output, exposed variable nodes) and need to be connected to the subgraph node.
            foreach (var element in transferredElements)
            {
                if (element is not VariableNodeModel variableNode || (!variableNode.VariableDeclarationModel.IsInputOrOutput && variableNode.VariableDeclarationModel.Scope != VariableScope.Exposed))
                    continue;

                var connectedWires = new List<WireModel>(variableNode.GetConnectedWires());
                subgraphNodePortInfos.Add(new SubgraphNodePortInfo
                {
                    SourceWires = connectedWires,
                    IsInput = variableNode.OutputPort != null,
                    PortUniqueName = variableNode.VariableDeclarationModel.Guid.ToString(),
                    NodeConnectedToSubgraphNode = variableNode
                });
            }
        }

        static VariableDeclarationModelBase GetVariableDeclarationAssociatedWithPort(SubgraphNodePortInfo portInfo, IReadOnlyList<VariableDeclarationModelBase> variableDeclarationModels)
        {
            for (var i = 0; i < variableDeclarationModels.Count; i++)
            {
                if (portInfo.PortUniqueName != variableDeclarationModels[i].Guid.ToString())
                    continue;

                if (portInfo.IsInput && variableDeclarationModels[i].Modifiers == ModifierFlags.Read || !portInfo.IsInput && variableDeclarationModels[i].Modifiers == ModifierFlags.Write)
                    return variableDeclarationModels[i];
            }

            return null;
        }

        bool TryCreateNodesForPortals(SubgraphNodePortInfo portInfo, GraphModel newSubgraph, IReadOnlyDictionary<Hash128, AbstractNodeModel> nodeMapping, List<Vector2> existingPositions, float offset)
        {
            if (portInfo.NodeConnectedToSubgraphNode is not WirePortalModel sourcePortal)
                return false;

            // Get the variable declaration associated with the subgraph node port.
            var associatedVariableDeclaration = GetVariableDeclarationAssociatedWithPort(portInfo, newSubgraph.VariableDeclarations);
            if (associatedVariableDeclaration == null)
                return false;

            // Get the new portal declaration of the transferred portals in the subgraph.
            DeclarationModel newPortalDeclaration = null;
            foreach (var (oldGuidInMainGraph, newNodeInSubgraph) in nodeMapping)
            {
                if (newNodeInSubgraph is not WirePortalModel newPortal ||
                    !sourcePortal.GraphModel.TryGetModelFromGuid(oldGuidInMainGraph, out var oldNode) ||
                    oldNode is not WirePortalModel oldPortal)
                    continue;

                if (sourcePortal.DeclarationModel == oldPortal.DeclarationModel)
                    newPortalDeclaration = newPortal.DeclarationModel;
            }

            if (newPortalDeclaration == null)
                return false;

            // Create the variable node
            var variableNodeModel = newSubgraph.CreateVariableNode(associatedVariableDeclaration, GetNewVariablePosition(existingPositions, newSubgraph.LastKnownBounds.min - new Vector2(100, 100), offset));

            // Create the portal node
            var portalToCreate = variableNodeModel.VariableDeclarationModel.IsInput
                ? newSubgraph.CreateEntryPortalFromPort(variableNodeModel.OutputPort, variableNodeModel.Position + new Vector2(300, 0), 0, newPortalDeclaration, 0)
                : newSubgraph.CreateExitPortalToPort(variableNodeModel.InputPort, variableNodeModel.Position - new Vector2(300, 0), 0, newPortalDeclaration, 0);

            if (portalToCreate == null)
                return false;

            // Connect the variable node and the portal node
            if (portInfo.IsInput && portalToCreate is ISingleInputPortNodeModel newEntryPortal)
            {
                newSubgraph.CreateWire(newEntryPortal.InputPort, variableNodeModel.OutputPort);
                return true;
            }

            if (portalToCreate is ISingleOutputPortNodeModel newExitPortal)
            {
                newSubgraph.CreateWire(variableNodeModel.InputPort, newExitPortal.OutputPort);
                return true;
            }

            return false;
        }

        void CreateVariableNodesForWires(SubgraphNodePortInfo portInfo, GraphModel newSubgraph, IReadOnlyDictionary<Hash128, AbstractNodeModel> nodeMapping, List<Vector2> existingPositions, float offset)
        {
            if (portInfo.SourceWires == null)
                return;

            // Get the variable declaration associated with the subgraph node port.
            var associatedVariableDeclaration = GetVariableDeclarationAssociatedWithPort(portInfo, newSubgraph.VariableDeclarations);
            if (associatedVariableDeclaration == null)
                return;

            if (portInfo.SourceWires.Count == 0)
            {
                newSubgraph.CreateVariableNode(associatedVariableDeclaration, GetNewVariablePosition(existingPositions, portInfo.NodeConnectedToSubgraphNode.Position, offset));
                return;
            }

            // For each wire, check if a node is missing at one of its ends. If yes, create a variable node connected to it.
            foreach (var sourceWire in portInfo.SourceWires)
            {
                nodeMapping.TryGetValue(sourceWire.ToNodeGuid, out var toNode);
                nodeMapping.TryGetValue(sourceWire.FromNodeGuid, out var fromNode);

                if (toNode == null && fromNode == null)
                {
                    newSubgraph.CreateVariableNode(associatedVariableDeclaration, GetNewVariablePosition(existingPositions, portInfo.NodeConnectedToSubgraphNode.Position, offset));
                    continue;
                }

                var isInput = fromNode == null;
                if ((isInput ? toNode : fromNode) is not InputOutputPortsNodeModel otherNode) // Other node to which the wire is connected to and is not missing.
                    continue;

                // Get the other port to which the wire is connected to and is not null.
                var otherNodePortsById = isInput ? otherNode.InputsById : otherNode.OutputsById;
                var otherPortId = isInput ? sourceWire.ToPortId : sourceWire.FromPortId;
                if (!otherNodePortsById.TryGetValue(otherPortId, out var otherPort))
                    continue;

                // If the port is already connected to a variable node with the same declaration, do not create a new variable node.
                var alreadyConnected = false;
                foreach (var port in otherPort.GetConnectedPorts())
                {
                    if (port.NodeModel is VariableNodeModel variableNode && variableNode.VariableDeclarationModel.Guid == associatedVariableDeclaration.Guid)
                    {
                        alreadyConnected = true;
                        break;
                    }
                }

                if (!alreadyConnected)
                {
                    var variableNodeModel = newSubgraph.CreateVariableNode(associatedVariableDeclaration, GetNewVariablePosition(existingPositions, (isInput ? sourceWire.FromPort : sourceWire.ToPort).NodeModel.Position, offset));
                    if (isInput)
                        newSubgraph.CreateWire(otherPort, variableNodeModel.OutputPort);
                    else
                        newSubgraph.CreateWire(variableNodeModel.InputPort, otherPort);
                }
            }
        }

        static VariableDeclarationModelBase CreateVariableDeclaration(GraphModel newSubgraph, Hash128 variableGuid, string variableTitle, TypeHandle dataTypeHandle, bool isInput, VariableScope variableScope = VariableScope.Local)
        {
            // Check if the variable declaration already exists.
            for (var i = 0; i < newSubgraph.VariableDeclarations.Count; i++)
            {
                if (newSubgraph.VariableDeclarations[i].Guid == variableGuid)
                    return newSubgraph.VariableDeclarations[i];
            }
            // If the variable doesn't exist, create a new one.
            var newVariable = newSubgraph.CreateGraphVariableDeclaration(dataTypeHandle, variableTitle, isInput ? ModifierFlags.Read : ModifierFlags.Write, variableScope, indexInGroup: -1, guid: variableGuid);

            // Check if it can be pasted to the new subgraph.
            if (!newSubgraph.CanPasteVariable(newVariable))
            {
                newSubgraph.DeleteVariableDeclaration(newVariable);
                return null;
            }

            return newVariable;
        }

        static string GetVariableTitle(PortModel portInSubgraph, PortModel portNotInSubgraph)
        {
            // If the other node is a token node, its name is not informative (eg: MainPortName), use the variable node's name instead.
            if (portInSubgraph.NodeModel is ISingleInputPortNodeModel or ISingleOutputPortNodeModel)
            {
                return portNotInSubgraph.NodeModel is ISingleInputPortNodeModel or ISingleOutputPortNodeModel ? portNotInSubgraph.NodeModel.Title : portNotInSubgraph.Title;
            }

            return portInSubgraph.Title;
        }
    }
}
