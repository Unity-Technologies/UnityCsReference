// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command to create an asset subgraph node and the corresponding subgraph asset.
    /// </summary>
    [UnityRestricted]
    internal class CreateSubgraphCommand : UndoableCommand
    {
        /// <summary>
        /// The <see cref="GraphView"/> that will contain the created subgraph node.
        /// </summary>
        public readonly GraphView GraphView;

        /// <summary>
        /// The selected graph elements that need to be recreated in the subgraph.
        /// </summary>
        public List<GraphElementModel> ElementsToAddToSubgraph;

        /// <summary>
        /// The guid to assign to the newly created subgraph node.
        /// </summary>
        public Hash128 Guid;

        /// <summary>
        /// The type of the asset.
        /// </summary>
        public Type AssetType;

        /// <summary>
        /// The template to create the subgraph.
        /// </summary>
        public GraphTemplate Template;

        /// <summary>
        /// Elements to delete after creating the subgraph node, if any.
        /// </summary>
        public List<GraphElementModel> ElementsToDelete;

        /// <summary>
        /// The path of the newly created asset when there is no prompt to create the subgraph.
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// The position where to create the subgraph node.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphCommand"/>.
        /// </summary>
        public CreateSubgraphCommand()
        {
            UndoString = "Create Asset Subgraph";
        }

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphCommand"/>.
        /// </summary>
        /// <remarks>This constructor will create the graph's default variable declaration.</remarks>
        /// <param name="assetType">The type of the asset.</param>
        /// <param name="elementsToCreate">The graph elements that need to be created in the subgraph.</param>
        /// <param name="template">The template of the subgraph.</param>
        /// <param name="graphView">The current graph view.</param>
        /// <param name="position">The position where to create the subgraph node.</param>
        /// <param name="elementsToDelete">Additional graph elements to delete, if any.</param>
        public CreateSubgraphCommand(Type assetType, List<GraphElementModel> elementsToCreate, GraphTemplate template, GraphView graphView, Vector2 position, List<GraphElementModel> elementsToDelete = null)
            : this()
        {
            AssetType = assetType;
            Guid = Hash128Helpers.GenerateUnique();
            GraphView = graphView;
            ElementsToAddToSubgraph = elementsToCreate;
            ElementsToDelete = elementsToDelete;
            Template = template;
            Position = position;
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
        /// <param name="position">The position where to create the subgraph node.</param>
        /// <param name="elementsToDelete">Additional graph elements to delete, if any.</param>
        public CreateSubgraphCommand(Type assetType, List<GraphElementModel> elementsToCreate, GraphTemplate template, GraphView graphView, string assetPath, Vector2 position, List<GraphElementModel> elementsToDelete = null)
            : this(assetType, elementsToCreate, template, graphView, position, elementsToDelete)
        {
            AssetPath = assetPath;
            Position = position;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreateSubgraphCommand command)
        {
            var graphModel = graphModelState.GraphModel;
            if (!graphModel.AllowSubgraphCreation)
                return;

            bool ValidatePath(string path)
            {
                var serializedAsset = graphModel.GraphObject;
                var valid = serializedAsset == null || path != serializedAsset.FilePath;

                if (!valid)
                {
                    Debug.LogWarning($"Could not create a subgraph at \"{path}\" because it would overwrite the main graph.");
                }

                return valid;
            }

            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(graphModelState);
                undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
            }

            SubgraphNodeModel subgraphNodeModel;
            GraphObject graphObject = null;
            if (command.AssetPath == null)
            {
                graphObject = GraphObjectCreationHelpers.PromptToCreateGraphObject(command.AssetType, command.Template, null, null, ValidatePath);
            }
            else if (ValidatePath(command.AssetPath))
            {
                graphObject = GraphObjectCreationHelpers.CreateGraphObject(command.AssetType, command.Template.GraphModelType, command.Template.NewAssetName, command.AssetPath, command.Template);
            }

            if (graphObject == null)
                return;

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                subgraphNodeModel = graphModel.CreateSubgraphNodeFromSelection(
                    graphObject.GraphModel,
                    command.ElementsToAddToSubgraph,
                    command.Position,
                    command.Guid,
                    command.ElementsToDelete);

                // Set the graph bounds. Necessary to know the bounds without opening the subgraph.
                SubgraphCreationHelper.ComputeSubgraphBounds(command.ElementsToAddToSubgraph, command.GraphView, graphObject.GraphModel);

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            graphObject.Save();

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
    /// Command to create a local subgraph node and the corresponding subgraph, a sub-asset of the current graph.
    /// </summary>
    [UnityRestricted]
    internal class CreateLocalSubgraphFromSelectionCommand : UndoableCommand
    {
        /// <summary>
        /// The GraphView.
        /// </summary>
        public readonly GraphView GraphView;

        /// <summary>
        /// The graph elements that need to be recreated in the local subgraph.
        /// </summary>
        public List<GraphElementModel> ElementsToAddToSubgraph;

        /// <summary>
        /// The Guid to assign to the newly local subgraph node.
        /// </summary>
        public Hash128 Guid;

        /// <summary>
        /// The default name for a local subgraph.
        /// </summary>
        public string DefaultName;

        /// <summary>
        /// The template to create the subgraph.
        /// </summary>
        public GraphTemplate Template;

        /// <summary>
        /// The position where to create the subgraph node.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Elements to delete after creating the subgraph node, if any.
        /// </summary>
        public List<GraphElementModel> ElementsToDelete;

        /// <summary>
        /// Initializes a new <see cref="CreateLocalSubgraphFromSelectionCommand"/>.
        /// </summary>
        public CreateLocalSubgraphFromSelectionCommand()
        {
            UndoString = "Create Local Subgraph";
        }

        /// <summary>
        /// Initializes a new <see cref="CreateLocalSubgraphFromSelectionCommand"/>.
        /// </summary>
        /// <remarks>This constructor will create the graph's default variable declaration.</remarks>
        /// <param name="elementsToCreate">The graph elements that need to be created in the local subgraph.</param>
        /// <param name="graphView">The current graph view.</param>
        /// <param name="position">The position where to create the subgraph node.</param>
        /// <param name="assetType">The type of the asset.</param>
        /// <param name="template">The template of the graph.</param>
        /// <param name="defaultName">The default name for the local subgraph.</param>
        /// <param name="elementsToDelete">Additional graph elements to delete, if any.</param>
        public CreateLocalSubgraphFromSelectionCommand(List<GraphElementModel> elementsToCreate, GraphView graphView, Vector2 position, Type assetType = null, GraphTemplate template = null, string defaultName = "", List<GraphElementModel> elementsToDelete = null)
            : this()
        {
            ElementsToAddToSubgraph = elementsToCreate;
            GraphView = graphView;
            Template = template;
            DefaultName = defaultName;
            Position = position;
            ElementsToDelete = elementsToDelete;

            Guid = Hash128Helpers.GenerateUnique();
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="autoPlacementState">The auto placement state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState,
            AutoPlacementStateComponent autoPlacementState, CreateLocalSubgraphFromSelectionCommand command)
        {
            var graphModel = graphModelState.GraphModel;
            if (!graphModel.AllowSubgraphCreation)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            // Create the local subgraph model.
            var localSubgraph = graphModel.CreateLocalSubgraph(
                command.Template?.GraphModelType ?? graphModel.GetType(),
                string.IsNullOrEmpty(command.DefaultName) ? SubgraphCreationHelper.defaultLocalSubgraphName : command.DefaultName,
                command.Template);

            if (localSubgraph is null)
                return;

            SubgraphNodeModel subgraphNodeModel;
            var portIdsToAlign = new List<string>();

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                // Create the subgraph node on the current graph.
                subgraphNodeModel = graphModel.CreateSubgraphNodeFromSelection(
                    localSubgraph,
                    command.ElementsToAddToSubgraph,
                    command.Position,
                    command.Guid,
                    command.ElementsToDelete,
                    portIdsToAlign);

                // Set the graph bounds. Necessary to know the bounds without opening the subgraph.
                SubgraphCreationHelper.ComputeSubgraphBounds(command.ElementsToAddToSubgraph, command.GraphView, localSubgraph);

                graphUpdater.MarkForRename(subgraphNodeModel);
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }

            if (subgraphNodeModel != null)
            {
                using (var autoPlacementUpdater = autoPlacementState.UpdateScope)
                {
                    foreach (var portId in portIdsToAlign)
                    {
                        foreach (var port in subgraphNodeModel.GetPorts())
                        {
                            if (portId != port.UniqueName)
                                continue;

                            foreach (var wire in port.GetConnectedWires())
                            {
                                var otherNode = port.Direction == PortDirection.Input ? wire.FromPort.NodeModel : wire.ToPort.NodeModel;
                                autoPlacementUpdater.MarkModelToRepositionAtCreation(
                                    (otherNode, wire, port.Direction == PortDirection.Input ? WireSide.From : WireSide.To),
                                    AutoPlacementStateComponent.Changeset.RepositionType.FromPort,
                                    new List<GraphElementModel> { otherNode });
                            }
                        }
                    }
                }

                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var undoStateUpdater = undoState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection();
                    selectionUpdaters.MainUpdateScope.SelectElement(subgraphNodeModel, true);
                }
            }
        }
    }

    /// <summary>
    /// Command to create a subgraph node from an existing graph object.
    /// </summary>
    [UnityRestricted]
    internal class CreateSubgraphNodeFromExistingGraphCommand : UndoableCommand
    {
        /// <summary>
        /// The guid to assign to the subgraph node.
        /// </summary>
        public Hash128 Guid;

        /// <summary>
        /// The graph referenced by the subgraph node.
        /// </summary>
        public GraphObject GraphObject;

        /// <summary>
        /// The position of the subgraph node.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphNodeFromExistingGraphCommand"/>.
        /// </summary>
        public CreateSubgraphNodeFromExistingGraphCommand()
        {
            UndoString = "Create Subgraph node from graph";
        }

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphNodeFromExistingGraphCommand"/>.
        /// </summary>
        /// <param name="graphObject">The graph referenced by the subgraph node.</param>
        /// <param name="position">The position of the subgraph node.</param>
        /// <param name="guid">The guid to assign to the newly subgraph node.</param>
        public CreateSubgraphNodeFromExistingGraphCommand(GraphObject graphObject, Vector2 position, Hash128 guid = default)
            : this()
        {
            GraphObject = graphObject;
            Position = position;
            Guid = guid == default ? Hash128Helpers.GenerateUnique() : guid;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreateSubgraphNodeFromExistingGraphCommand command)
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
                subgraphNodeModel = graphModelState.GraphModel.CreateSubgraphNode(command.GraphObject.GraphModel,
                    command.Position, command.Guid);

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
    /// Command to convert asset subgraph nodes to local subgraph nodes.
    /// </summary>
    /// <remarks>Creates a local copy that is detached from the original asset graph file, and will no longer update with the main file. Does not effect the original asset graph file.</remarks>
    [UnityRestricted]
    internal class ConvertAssetToLocalSubgraphCommand : UndoableCommand
    {
        /// <summary>
        /// The subgraph nodes to convert.
        /// </summary>
        public List<SubgraphNodeModel> SubgraphNodeModels;

        /// <summary>
        /// The template to create the subgraph.
        /// </summary>
        public GraphTemplate Template;

        /// <summary>
        /// Initializes a new <see cref="ConvertAssetToLocalSubgraphCommand"/>.
        /// </summary>
        public ConvertAssetToLocalSubgraphCommand()
        {
            UndoString = "Convert Asset To Local Subgraph";
        }

        /// <summary>
        /// Initializes a new <see cref="ConvertAssetToLocalSubgraphCommand"/>.
        /// </summary>
        /// <param name="subgraphNodeModels">The subgraph nodes to convert.</param>
        /// <param name="template">The template to create the subgraph.</param>
        public ConvertAssetToLocalSubgraphCommand(List<SubgraphNodeModel> subgraphNodeModels, GraphTemplate template)
            : this()
        {
            SubgraphNodeModels = subgraphNodeModels;
            Template = template;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, ConvertAssetToLocalSubgraphCommand command)
        {
            var graphModel = graphModelState.GraphModel;
            if (!graphModel.AllowSubgraphCreation)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            for (var i = 0; i < command.SubgraphNodeModels.Count; i++)
            {
                var subgraphNode = command.SubgraphNodeModels[i];

                // Create the local subgraph asset.
                var localSubgraph = GraphObjectCreationHelpers.ConvertAssetToLocalGraph(subgraphNode.GetSubgraphModel().GraphObject, subgraphNode.GraphModel, command.Template);
                if (localSubgraph is null)
                    continue;

                using (var graphUpdater = graphModelState.UpdateScope)
                using (var changeScope = graphModel.ChangeDescriptionScope)
                {
                    // Assign the local subgraph to the subgraph node.
                    subgraphNode.SetSubgraphModel(localSubgraph.GetGraphReference(true));
                    graphUpdater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// Command to convert local subgraph nodes to asset subgraph nodes.
    /// </summary>
    /// <remarks>The local subgraph will no longer exist inside of the parent graph.</remarks>
    [UnityRestricted]
    internal class ConvertLocalToAssetSubgraphCommand : UndoableCommand
    {
        const string k_PromptToCreateTitle = "Create {0}";
        const string k_PromptToCreate = "Create a new {0}";

        /// <summary>
        /// The subgraph nodes to convert.
        /// </summary>
        public List<SubgraphNodeModel> SubgraphNodeModels;

        /// <summary>
        /// The path of the newly created asset when there is no prompt to create the asset subgraph.
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// The template to create the subgraph.
        /// </summary>
        public GraphTemplate Template;

        /// <summary>
        /// Initializes a new <see cref="ConvertLocalToAssetSubgraphCommand"/>.
        /// </summary>
        public ConvertLocalToAssetSubgraphCommand()
        {
            UndoString = "Convert Local To Asset Subgraph";
        }

        /// <summary>
        /// Initializes a new <see cref="ConvertLocalToAssetSubgraphCommand"/>.
        /// </summary>
        /// <param name="subgraphNodeModels">The subgraph nodes to convert.</param>
        /// <param name="template">The template to create the subgraph.</param>
        public ConvertLocalToAssetSubgraphCommand(List<SubgraphNodeModel> subgraphNodeModels, GraphTemplate template)
            : this()
        {
            SubgraphNodeModels = subgraphNodeModels;
            Template = template;
        }

        /// <summary>
        /// Initializes a new <see cref="ConvertLocalToAssetSubgraphCommand"/>.
        /// </summary>
        /// <param name="subgraphNodeModels">The subgraph nodes to convert.</param>
        /// <param name="template">The template to create the subgraph.</param>
        /// <param name="assetPath">The path of the asset.</param>
        public ConvertLocalToAssetSubgraphCommand(List<SubgraphNodeModel> subgraphNodeModels, GraphTemplate template, string assetPath)
            : this(subgraphNodeModels, template)
        {
            AssetPath = assetPath;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, ConvertLocalToAssetSubgraphCommand command)
        {
            var graphModel = graphModelState.GraphModel;
            if (!graphModel.AllowSubgraphCreation)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            bool ValidatePath(string path)
            {
                var serializedAsset = graphModel.GraphObject;
                var valid = serializedAsset == null || path != serializedAsset.FilePath;

                if (!valid)
                {
                    Debug.LogWarning($"Could not create a subgraph at \"{path}\" because it would overwrite the main graph.");
                }

                return valid;
            }

            for (var i = 0; i < command.SubgraphNodeModels.Count; i++)
            {
                var subgraphNode = command.SubgraphNodeModels[i];

                if (!subgraphNode.IsReferencingLocalSubgraph)
                    continue;

                var promptTitle = string.Format(k_PromptToCreateTitle, subgraphNode.Title);
                var prompt = string.Format(k_PromptToCreate, subgraphNode.Title);

                var subgraphModel = subgraphNode.GetSubgraphModel();
                var assetGraph = GraphObjectCreationHelpers.ConvertLocalToAssetGraph(subgraphModel, promptTitle, prompt, command.AssetPath, command.Template, ValidatePath);
                if (assetGraph == null)
                    continue;

                using (var graphUpdater = graphModelState.UpdateScope)
                using (var changeScope = graphModel.ChangeDescriptionScope)
                {
                    // TODO part of GTF-1271: Undo the creation of the asset graph.

                    // Assign the asset subgraph to the subgraph node.
                    subgraphNode.SetSubgraphModel(assetGraph.GraphModel.GetGraphReference(true));
                    subgraphNode.GraphModel.RemoveLocalSubgraph(subgraphModel);
                    graphUpdater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// Command to expand the content of a subgraph node into a placemat.
    /// </summary>
    [UnityRestricted]
    internal class ExpandSubgraphCommand : UndoableCommand
    {
        /// <summary>
        /// The subgraph node to expand.
        /// </summary>
        public SubgraphNodeModel SubgraphNode;

        /// <summary>
        /// The graph model which will contain the placemat.
        /// </summary>
        public GraphModel TargetGraphModel;

        /// <summary>
        /// The position where to create the placemat.
        /// </summary>
        public Vector2 Position;

        static readonly string k_UndoString = "Expand Subgraph";

        /// <summary>
        /// Initializes a new <see cref="ExpandSubgraphCommand"/>.
        /// </summary>
        public ExpandSubgraphCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Initializes a new <see cref="ExpandSubgraphCommand"/>.
        /// </summary>
        /// <param name="targetGraphModel">The graph model which will contain the placemat.</param>
        /// <param name="subgraphNode">The subgraph node to expand.</param>
        /// <param name="position">The position where to create the placemat.</param>
        public ExpandSubgraphCommand(GraphModel targetGraphModel, SubgraphNodeModel subgraphNode, Vector2 position)
            : this()
        {
            TargetGraphModel = targetGraphModel;
            SubgraphNode = subgraphNode;
            Position = position;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(GraphModelStateComponent graphModelState, UndoStateComponent undoState, SelectionStateComponent selectionState, ExpandSubgraphCommand command)
        {
            var graphModel = graphModelState.GraphModel;
            if (!graphModel.AllowSubgraphCreation)
                return;

            var subgraphGraphModel = command.SubgraphNode.GetSubgraphModel();
            if (subgraphGraphModel == null)
                return;

            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(graphModelState);
                undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
            }

            var subgraphVariableNodes = new Dictionary<VariableDeclarationModelBase, HashSet<VariableNodeModel>>();
            var elementsToCopy = new List<Model>();

            // The subgraph top left position will be used to compute the copied elements' positions in the target graph.
            var subgraphTopLeft = new Vector2(float.MaxValue, float.MaxValue);
            var mostRightSourceNodeX = float.MinValue;
            InputOutputPortsNodeModel mostRightSourceNode = null;
            foreach (var element in subgraphGraphModel.GetGraphElementModels())
            {
                if (element is GroupModel)
                    continue;

                if (element is VariableDeclarationModel variableDeclaration)
                {
                    // Don't copy input or output variable declarations into the target graph.
                    // Don't copy variable declarations that are already part of the target graph.
                    if (variableDeclaration.IsInputOrOutput || command.TargetGraphModel.TryGetModelFromGuid(variableDeclaration.Guid, out _))
                        continue;

                    // If the variable is part of a group, copy the group, it will copy the variable as well.
                    if (variableDeclaration.ParentGroup is not null)
                    {
                        var parentGroup = variableDeclaration.ParentGroup;
                        while (parentGroup is not null)
                        {
                            // If the current group's parent group is a section model, it is the last group.
                            if (parentGroup.ParentGroup is SectionModel)
                            {
                                elementsToCopy.Add(parentGroup);
                                continue;
                            }
                            parentGroup = parentGroup.ParentGroup;
                        }
                    }
                }

                if (element is VariableNodeModel variableNode)
                {
                    // Keep track of the variable declarations and their associated variable nodes in the subgraph.
                    if (!subgraphVariableNodes.TryGetValue(variableNode.VariableDeclarationModel, out var nodes))
                    {
                        nodes = new HashSet<VariableNodeModel>();
                        subgraphVariableNodes[variableNode.VariableDeclarationModel] = nodes;
                    }
                    nodes.Add(variableNode);

                    // Don't copy input or output variable nodes.
                    if (variableNode.VariableDeclarationModel.IsInputOrOutput)
                        continue;
                }

                // Adjust the subgraph top left position.
                if (element is IMovable movable)
                {
                    if (movable.Position.x < subgraphTopLeft.x)
                        subgraphTopLeft.x = movable.Position.x;
                    if (movable.Position.y < subgraphTopLeft.y)
                        subgraphTopLeft.y = movable.Position.y;
                    if (element is InputOutputPortsNodeModel node && movable.Position.x > mostRightSourceNodeX)
                    {
                        mostRightSourceNodeX = movable.Position.x;
                        mostRightSourceNode = node;
                    }
                }

                // Get the elements from the subgraph that will be copied in the final placemat.
                elementsToCopy.Add(element);
            }

            using (var cpd = new CopyPasteData(null, elementsToCopy))
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            using (var graphUpdater = graphModelState.UpdateScope)
            using (var selectionUpdater = selectionState.UpdateScope)
            {
                // Create the placemat containing the expanded subgraph in the target graph.
                // Need to be created before pasting the content of the subgraph in case there are placemats in that content. Else, the copied placemats will be placed below the encompassing placemat.
                var subgraphBounds = subgraphGraphModel.LastKnownBounds;
                subgraphBounds.size -= subgraphTopLeft - subgraphBounds.position;
                subgraphBounds.position = command.Position + new Vector2(0, Placemat.k_SmartResizeMargin);

                selectionUpdater.ClearSelection();

                // Copy subgraph content.
                var nodeMapping = CopyPasteData.PasteSerializedData(PasteOperation.Duplicate,
                    command.Position + new Vector2(Placemat.k_SmartResizeMargin, Placemat.k_SmartResizeMargin * 2) -
                    subgraphTopLeft, null, selectionUpdater, cpd, command.TargetGraphModel, null,
                    false, true);

                var elementsToRemove = new List<GraphElementModel>();

                // Patch up wires in the target graph.
                var existingWiresConnectedToSubgraphNode = command.SubgraphNode.GetConnectedWires();
                foreach (var existingWire in existingWiresConnectedToSubgraphNode)
                {
                    elementsToRemove.Add(existingWire);
                    if (nodeMapping.Count == 0)
                        continue;

                    var isConnectedToSubgraphNodeInput = existingWire.ToPort.NodeModel == command.SubgraphNode;

                    var portOnSubgraphNode = isConnectedToSubgraphNodeInput ? existingWire.ToPort : existingWire.FromPort;
                    var portOnOtherNode = isConnectedToSubgraphNodeInput ? existingWire.FromPort : existingWire.ToPort;

                    if (portOnOtherNode.Capacity is PortCapacity.None || portOnSubgraphNode.Capacity is PortCapacity.None)
                        continue;

                    // Get the variable declaration in the subgraph associated to the port on the subgraph node.
                    var portsToVariables = isConnectedToSubgraphNodeInput ? command.SubgraphNode.InputPortToVariableDeclarationDictionary : command.SubgraphNode.OutputPortToVariableDeclarationDictionary;
                    portsToVariables.TryGetValue(portOnSubgraphNode, out var variableDeclaration);

                    if (variableDeclaration is null)
                        continue;

                    // Get all the instances of that variable declaration in the subgraph.
                    if (!subgraphVariableNodes.TryGetValue(variableDeclaration, out var variableNodes))
                        continue;

                    // For each instance, create a corresponding wire in the target graph.
                    foreach (var variableNode in variableNodes)
                    {
                        if (portOnOtherNode.Capacity != PortCapacity.Multi && portOnOtherNode.GetConnectedWires().Count > 1)
                            break;

                        var isInputVariableNode = variableNode.OutputPort is not null;
                        var wiresConnectedToVariableNode = variableNode.GetConnectedWires();
                        foreach (var wire in wiresConnectedToVariableNode)
                        {
                            if (isInputVariableNode)
                            {
                                if (nodeMapping[wire.ToPort.NodeModel.Guid] is not InputOutputPortsNodeModel matchingNode)
                                    continue;

                                var matchingPort = matchingNode.InputsById[wire.ToPort.UniqueName];
                                command.TargetGraphModel.CreateWire(matchingPort, portOnOtherNode);
                            }
                            else
                            {
                                if (nodeMapping[wire.FromPort.NodeModel.Guid] is not InputOutputPortsNodeModel matchingNode)
                                    continue;

                                var matchingPort = matchingNode.OutputsById[wire.FromPort.UniqueName];
                                command.TargetGraphModel.CreateWire(portOnOtherNode, matchingPort);
                            }
                        }
                    }
                }

                // It's possible for a source node (in the subgraph) to be smaller than the duplicated one (after the subgraph was expanded).
                // It happens if the source node's inputs are all connected while the duplicated node's aren't. In the latter, the constant field makes the node larger.
                // We should make sure the placemat still covers the most right node if it becomes larger.
                var placematExtraMargin = Vector2.one * (Placemat.k_SmartResizeMargin * 2);
                if (mostRightSourceNode != null)
                {
                    var sourceNodeHasConstantField = false;
                    for (var i = 0; i < mostRightSourceNode.InputsByDisplayOrder.Count; i++)
                    {
                        var input = mostRightSourceNode.InputsByDisplayOrder[i];
                        if ((input.Options & PortModelOptions.NoEmbeddedConstant) == 0 && input.GetConnectedWires().Count == 0)
                        {
                            sourceNodeHasConstantField = true;
                            break;
                        }
                    }

                    if (!sourceNodeHasConstantField)
                    {
                        var mostRightDuplicatedNodeX = float.MinValue;
                        InputOutputPortsNodeModel mostRightDuplicatedNode = null;
                        foreach (var(_, element) in nodeMapping)
                        {
                            if (element is IMovable movable and InputOutputPortsNodeModel node && movable.Position.x > mostRightDuplicatedNodeX)
                            {
                                mostRightDuplicatedNodeX = movable.Position.x;
                                mostRightDuplicatedNode = node;
                            }
                        }
                        var duplicatedNodeHasConstantField = false;
                        for (var i = 0; i < mostRightDuplicatedNode.InputsByDisplayOrder.Count; i++)
                        {
                            var input = mostRightDuplicatedNode.InputsByDisplayOrder[i];
                            if ((input.Options & PortModelOptions.NoEmbeddedConstant) == 0 && input.GetConnectedWires().Count == 0)
                            {
                                duplicatedNodeHasConstantField = true;
                                break;
                            }
                        }
                        // If the source node is smaller because it doesn't have a constant field and the duplicated node is larger because it has a constant field, we should add extra space to the placemat.
                        if (duplicatedNodeHasConstantField)
                            placematExtraMargin.x *= 2;
                    }
                }

                subgraphBounds.size += placematExtraMargin;
                var placemat = command.TargetGraphModel.CreatePlacemat(subgraphBounds);
                placemat.Title = command.SubgraphNode.Title;
                selectionUpdater.SelectElement(placemat, true);

                // Delete the subgraph node and obsolete wires.
                elementsToRemove.Add(command.SubgraphNode);
                command.TargetGraphModel.DeleteElements(elementsToRemove);

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
