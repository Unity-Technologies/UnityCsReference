// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
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
            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(graphModelState);
                undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
            }

            SubgraphNodeModel subgraphNodeModel;
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
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

                // Save the graph asset, since it was populated after its creation.
                graphAsset.Dirty = true;
                graphAsset.Save();

                // Delete the graph elements that will be created in the local subgraph
                var graphModel = graphModelState.GraphModel;
                graphModel.DeletePlacemats(elementsToAddToSubgraph.PlacematModels);
                graphModel.DeleteStickyNotes(elementsToAddToSubgraph.StickyNoteModels);
                graphModel.DeleteNodes(elementsToAddToSubgraph.NodeModels, true);
                graphModel.DeleteWires(allWireModels);

                // Create the subgraph node
                var position = SubgraphNode.ComputeSubgraphNodePosition_Internal(command.ElementsToAddToSubgraph, command.GraphView);
                subgraphNodeModel = graphModel.CreateSubgraphNode(graphAsset.GraphModel, position, command.Guid);

                // Create new wires linking the subgraph node to other nodes
                SubgraphCreationHelpers_Internal.CreateWiresConnectedToSubgraphNode_Internal(graphModel, subgraphNodeModel, inputWireConnections, outputWireConnections);

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            if (subgraphNodeModel != null)
            {
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection();
                    selectionUpdaters.MainUpdateScope.SelectElement(subgraphNodeModel, true);
                }
            }
        }
    }

    /// <summary>
    /// Command to update a subgraph. This command is not undoable because it updates
    /// the subgraph nodes based on external asset modifications.
    /// </summary>
    class UpdateSubgraphCommand : ICommand
    {
        /// <summary>
        /// The guid of the graph referenced by subgraph nodes that need updating.
        /// </summary>
        public readonly string SubgraphGuid;

        /// <summary>
        /// Creates a new <see cref="UpdateSubgraphCommand"/>.
        /// </summary>
        /// <param name="subgraphGuid">The guid of the graph referenced by subgraph nodes that need updating.</param>
        public UpdateSubgraphCommand(string subgraphGuid)
        {
            SubgraphGuid = subgraphGuid;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphModelStateComponent graphModelState, UpdateSubgraphCommand command)
        {
            if (string.IsNullOrEmpty(command.SubgraphGuid))
                return;

            var graphModel = graphModelState.GraphModel;
            using (var updater = graphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                var subGraphNodeModels = graphModel.NodeModels.OfType<SubgraphNodeModel>().Where(nodeModel => nodeModel.SubgraphGuid == command.SubgraphGuid);

                foreach (var subgraphNodeModel in subGraphNodeModels)
                {
                    // The subgraph was changed or deleted. Update it.
                    subgraphNodeModel.Update();
                }

                updater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
