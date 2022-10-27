// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command to create a subgraph.
    /// </summary>
    class CreateSubgraphCommand : UndoableCommand
    {
        const string k_PromptToCreateTitle = "Create {0}";
        const string k_PromptToCreate = "Create a new {0}";

        /// <summary>
        /// The GraphView in charge of aligning the nodes.
        /// </summary>
        public readonly GraphView GraphView;

        /// <summary>
        /// The graph elements that need to be recreated in the subgraph.
        /// </summary>
        public List<GraphElementModel> ElementsToAddToSubgraph;

        /// <summary>
        /// The SerializableGUID to assign to the newly subgraph node.
        /// </summary>
        public SerializableGUID Guid;

        /// <summary>
        /// The type of the asset.
        /// </summary>
        public Type AssetType;

        /// <summary>
        /// The template to create the subgraph.
        /// </summary>
        public GraphTemplate Template;

        /// <summary>
        /// The path of the newly created asset when there is no prompt to create the subgraph.
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphCommand"/>.
        /// </summary>
        public CreateSubgraphCommand()
        {
            UndoString = "Create Subgraph";
        }

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphCommand"/>.
        /// </summary>
        /// <remarks>This constructor will create the graph's default variable declaration.</remarks>
        /// <param name="assetType">The type of the asset.</param>
        /// <param name="elementsToCreate">The graph elements that need to be created in the subgraph.</param>
        /// <param name="template">The template of the subgraph.</param>
        /// <param name="graphView">The current graph view.</param>
        public CreateSubgraphCommand(Type assetType, List<GraphElementModel> elementsToCreate, GraphTemplate template, GraphView graphView)
            : this()
        {
            AssetType = assetType;
            Guid = SerializableGUID.Generate();
            GraphView = graphView;
            ElementsToAddToSubgraph = elementsToCreate;
            Template = template;
        }

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphCommand"/>.
        /// </summary>
        /// <remarks>This constructor will create the graph's default variable declaration.</remarks>
        /// <param name="assetType">The type of the asset.</param>
        /// <param name="elementsToCreate">The graph elements that need to be created in the subgraph.</param>
        /// <param name="template">The template of the subgraph.</param>
        /// <param name="graphView">The current graph view.</param>
        /// <param name="assetPath">The path of the asset.</param>
        public CreateSubgraphCommand(Type assetType, List<GraphElementModel> elementsToCreate, GraphTemplate template, GraphView graphView, string assetPath)
            : this(assetType, elementsToCreate, template, graphView)
        {
            AssetType = assetType;
            Guid = SerializableGUID.Generate();
            GraphView = graphView;
            ElementsToAddToSubgraph = elementsToCreate;
            Template = template;
            AssetPath = assetPath;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreateSubgraphCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            SubgraphNodeModel subgraphNodeModel;
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var elementsToAddToSubgraph = SubgraphCreationHelpers_Internal.GraphElementsToAddToSubgraph_Internal.ConvertToGraphElementsToAdd_Internal(command.ElementsToAddToSubgraph);

                // Get source wire models AND wire models that aren't source wire models but are connected to source nodes.
                var allWireModels = elementsToAddToSubgraph.WireModels;
                foreach (var nodeModel in elementsToAddToSubgraph.NodeModels)
                    allWireModels.UnionWith(nodeModel.GetConnectedWires());

                // Get wire connections (wire to subgraph node port unique name) to keep after the subgraph node creation
                var inputWireConnections = new Dictionary<WireModel, string>();
                var outputWireConnections = new Dictionary<WireModel, string>();
                foreach (var wireModel in allWireModels)
                {
                    if (!elementsToAddToSubgraph.NodeModels.Contains(wireModel.FromPort.NodeModel))
                        inputWireConnections.Add(wireModel, "");
                    else if (!elementsToAddToSubgraph.NodeModels.Contains(wireModel.ToPort.NodeModel))
                        outputWireConnections.Add(wireModel, "");
                }

                bool ValidatePath(string path)
                {
                    var serializedAsset = graphModelState.GraphModel.Asset;
                    var valid = serializedAsset == null || path != serializedAsset.FilePath;

                    if (!valid)
                    {
                        Debug.LogWarning($"Could not create a subgraph at \"{path}\" because it would overwrite the main graph.");
                    }

                    return valid;
                }

                // Create the subgraph
                GraphAsset graphAsset = null;
                if (command.AssetPath == null)
                {
                    var promptTitle = string.Format(k_PromptToCreateTitle, command.Template.GraphTypeName);
                    var prompt = string.Format(k_PromptToCreate, command.Template.GraphTypeName);
                    graphAsset = GraphAssetCreationHelpers.PromptToCreateGraphAsset(command.AssetType, command.Template, promptTitle, prompt, ValidatePath);
                }
                else if (ValidatePath(command.AssetPath))
                {
                    graphAsset = GraphAssetCreationHelpers.CreateGraphAsset(command.AssetType, command.Template.StencilType, command.Template.GraphTypeName, command.AssetPath, command.Template);
                }

                if (graphAsset == null)
                {
                    return;
                }

                SubgraphCreationHelpers_Internal.PopulateSubgraph_Internal(graphAsset.GraphModel, elementsToAddToSubgraph, allWireModels, inputWireConnections, outputWireConnections);

                // Delete the graph elements that will be created in the local subgraph
                var deletedModels = new List<GraphElementModel>();

                var graphModel = graphModelState.GraphModel;
                deletedModels.AddRange(graphModel.DeletePlacemats(elementsToAddToSubgraph.PlacematModels));
                deletedModels.AddRange(graphModel.DeleteStickyNotes(elementsToAddToSubgraph.StickyNoteModels));
                deletedModels.AddRange(graphModel.DeleteNodes(elementsToAddToSubgraph.NodeModels, true));
                deletedModels.AddRange(graphModel.DeleteWires(allWireModels));

                graphUpdater.MarkDeleted(deletedModels);

                // Create the subgraph node
                var position = SubgraphNode.ComputeSubgraphNodePosition_Internal(command.ElementsToAddToSubgraph, command.GraphView);
                subgraphNodeModel = graphModel.CreateSubgraphNode(graphAsset.GraphModel, position, command.Guid);
                graphUpdater.MarkNew(subgraphNodeModel);

                // Create new wires linking the subgraph node to other nodes
                graphUpdater.MarkNew(SubgraphCreationHelpers_Internal.CreateWiresConnectedToSubgraphNode_Internal(graphModel, subgraphNodeModel, inputWireConnections, outputWireConnections));
            }

            if (subgraphNodeModel != null)
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection();
                    selectionUpdaters.MainUpdateScope.SelectElement(subgraphNodeModel, true);
                }
            }
        }
    }
}
