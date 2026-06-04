// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An observer that automatically places the graph elements that were marked for automatic placement.
    /// </summary>
    [UnityRestricted]
    internal class AutoPlacementObserver : StateObserver
    {
        GraphView m_GraphView;
        AutoPlacementStateComponent m_AutoPlacementState;
        GraphModelStateComponent m_GraphModelState;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoPlacementObserver" /> class.
        /// </summary>
        /// <param name="graphView">The <see cref="GraphView"/> in which the graph elements are placed.</param>
        /// <param name="autoPlacementState">The automatic placement model state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        public AutoPlacementObserver(GraphView graphView, AutoPlacementStateComponent autoPlacementState, GraphModelStateComponent graphModelState)
            : base(new IStateComponent[] { autoPlacementState }, new IStateComponent[] { graphModelState })
        {
            m_GraphView = graphView;
            m_AutoPlacementState = autoPlacementState;
            m_GraphModelState = graphModelState;
        }

        /// <inheritdoc/>
        public override void Observe()
        {
            var graphModel = m_GraphModelState.GraphModel;
            if (graphModel == null)
                return;

            AutoPlacementStateComponent.Changeset changeset;

            using var disposeModelsToAlign = ListPool<GraphElementModel>.Get(out var modelsToAutoAlign);
            using var disposeModelsToReverseAlign = ListPool<GraphElementModel>.Get(out var modelsToReverseAutoAlign);
            using var disposeElementsToReposition =
                ListPool<AutoPlacementStateComponent.Changeset.ModelToReposition>.Get(out var elementsToReposition);

            modelsToAutoAlign.Clear();
            modelsToReverseAutoAlign.Clear();
            elementsToReposition.Clear();

            // Only peek first to see if we need to wait before processing the placement.
            // Auto placement relies on UI layout to compute node positions, so we need to
            // make sure all graph element are properly layed out. If the graph elements exist,
            // the layout pass has probably been done in the last frame. If they do not exist,
            // the next layout pass will be done this frame after they are created by the ModelViewUpdater.
            using (var observation = this.PeekAtState(m_AutoPlacementState))
            {
                if (observation.UpdateType == UpdateType.None)
                    return;

                if (observation.UpdateType == UpdateType.Partial)
                {
                    changeset = m_AutoPlacementState.GetAggregatedChangeset(observation.LastObservedVersion);

                    foreach (var modelHash in changeset.ModelsToAutoAlign)
                    {
                        var model = graphModel.GetModel(modelHash);
                        if (model != null)
                            modelsToAutoAlign.Add(model);
                    }

                    foreach (var modelHash in changeset.ModelsToReverseAutoAlign)
                    {
                        var model = graphModel.GetModel(modelHash);
                        if (model != null)
                            modelsToReverseAutoAlign.Add(model);
                    }

                    foreach (var modelToReposition in changeset.ModelsToRepositionAtCreation)
                    {
                        elementsToReposition.Add(modelToReposition);
                    }
                }
                else
                {
                    changeset = null;

                    foreach (var model in graphModel.NodeModels)
                        modelsToAutoAlign.Add(model);
                }

                // If any view is missing, then it will be created and layed out during this frame. So, wait until all relevant views are available.
                foreach (var model in modelsToAutoAlign)
                {
                    if (model != null && model.GetView<GraphElement>(m_GraphView) == null)
                        return;
                }

                foreach (var model in modelsToReverseAutoAlign)
                {
                    if (model != null && model.GetView<GraphElement>(m_GraphView) == null)
                        return;
                }

                foreach (var modelToReposition in elementsToReposition)
                {
                    var model = graphModel.GetModel(modelToReposition.Model);
                    if (model != null && model.GetView<GraphElement>(m_GraphView) == null)
                        return;

                    var wireModel = graphModel.GetModel(modelToReposition.WireModel);
                    if (wireModel != null && wireModel.GetView<GraphElement>(m_GraphView) == null)
                        return;
                }
            }

            using (this.ObserveState(m_AutoPlacementState))
            using (var graphUpdater = m_GraphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                if (modelsToAutoAlign.Count > 0)
                {
                    m_GraphView.PositionDependenciesManager.AlignNodes(true, false, modelsToAutoAlign);
                }

                if (modelsToReverseAutoAlign.Count > 0)
                {
                    // Reverse alignment is used on nodes owning a To port that need to reposition themselves relatively to the owner of
                    // the connected From port, which essentially happens when drag and dropping an output variable node on an output port.
                    // Follow is set to false because only the output variable node needs alignment, and it would be awkward if the other nodes
                    // currently connected to the owner of the output port got also realigned.
                    m_GraphView.PositionDependenciesManager.AlignNodes(false, true, modelsToReverseAutoAlign);
                }

                if (elementsToReposition.Count > 0)
                {
                    RepositionModelsAtCreation(elementsToReposition, graphModel, graphUpdater);
                }

                if (changeset != null && changeset.ModelsToHideDuringAutoPlacement.Count > 0)
                {
                    SetHiddenModelsToVisible(changeset.ModelsToHideDuringAutoPlacement);
                }

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }

        void RepositionModelsAtCreation(IEnumerable<AutoPlacementStateComponent.Changeset.ModelToReposition> modelsToReposition, GraphModel graphModel, GraphModelStateComponent.StateUpdater graphUpdater)
        {
            foreach (var modelToReposition in modelsToReposition)
            {
                graphModel.TryGetModelFromGuid(modelToReposition.Model, out AbstractNodeModel nodeModel);
                graphModel.TryGetModelFromGuid(modelToReposition.WireModel, out WireModel wireModel);

                var nodeUI = nodeModel?.GetView<NodeView>(m_GraphView);
                if (nodeUI == null)
                    continue;

                if (wireModel != null)
                {
                    var portModel = modelToReposition.WireSide == WireSide.From ? wireModel.FromPort : wireModel.ToPort;

                    switch (modelToReposition.RepositionType)
                    {
                        case AutoPlacementStateComponent.Changeset.RepositionType.FromPort:
                        {
                            // Nodes created from ports need to recompute their position after their creation to make
                            // sure that they are aligned to the port.
                            var dependency = new LinkedNodesDependency
                            {
                                DependentPort = portModel,
                                ParentPort = modelToReposition.WireSide == WireSide.From ? wireModel.ToPort : wireModel.FromPort
                            };

                            if (dependency.ParentPort != null)
                            {
                                var outChangedModels = new List<GraphElementModel>();
                                m_GraphView.PositionDependenciesManager.AlignDependency(dependency, dependency.ParentPort.NodeModel, outChangedModels);

                                // Make sure the changed model does not overlap with another element
                                foreach (var changedModel in outChangedModels)
                                {
                                    if (changedModel is not IMovable movableChangedModel)
                                        continue;

                                    var newPosition = movableChangedModel.Position;

                                    // Get elements in the region near the node
                                    const int regionLength = 300;
                                    var elementsInRegion = new List<GraphElement>();
                                    m_GraphView.GetGraphElementsInRegion(
                                        new Rect(newPosition.x - 100, newPosition.y - 100, regionLength, regionLength),
                                        elementsInRegion, GraphView.PartitioningMode.PlacematTitle);

                                    foreach (var element in elementsInRegion)
                                    {
                                        if (element.Model is not IMovable movable || element.Model.Guid == dependency.ParentPort.NodeModel.Guid || element.Model.Guid == dependency.DependentNode.Guid)
                                            continue;

                                        // If the other element has the same y, offset the changed model
                                        if (Mathf.Abs(movable.Position.y - newPosition.y) < Mathf.Epsilon)
                                        {
                                            const int offset = 10;
                                            newPosition +=
                                                new Vector2(
                                                    portModel.Direction == PortDirection.Output ? -offset : offset, // offset to the left when it's an output
                                                    offset);
                                        }
                                    }
                                    movableChangedModel.Position = newPosition;
                                }
                            }

                            break;
                        }
                        case AutoPlacementStateComponent.Changeset.RepositionType.FromWire:
                        {
                            // Nodes created from wires need to recompute their position after their creation to make sure that the
                            // last hovered position corresponds to the connected port or, in the case of an incompatible connection,
                            // to the nodes' middle height or width (depending on the orientation).

                            // Get the orientation of the connection
                            PortOrientation orientation;
                            if (portModel == null)
                                orientation = (modelToReposition.WireSide == WireSide.From ? wireModel.ToPort?.Orientation : wireModel.FromPort?.Orientation) ?? PortOrientation.Horizontal;
                            else
                                orientation = portModel.Orientation;

                            // If the node is created from an input, shift the position of a node width or height, depending on the orientation
                            var newPosX = nodeUI.layout.x - (orientation == PortOrientation.Horizontal && modelToReposition.WireSide == WireSide.From ? nodeUI.layout.width : 0);
                            var newPosY = nodeUI.layout.y - (orientation != PortOrientation.Horizontal && modelToReposition.WireSide == WireSide.From ? nodeUI.layout.height : 0);

                            var portUI = portModel?.GetView<Port>(m_GraphView);
                            var isCompatibleConnection = wireModel is IGhostWireModel ? portModel != null : (nodeModel as PortNodeModel)?.GetPortFitToConnectTo(modelToReposition.WireSide == WireSide.From ? wireModel.ToPort : wireModel.FromPort) != null;

                            if (isCompatibleConnection && portUI != null)
                            {
                                // If the connection to the port is compatible, we want the last hovered position to correspond to the port.
                                var portPos = portUI.parent.ChangeCoordinatesTo(nodeUI.parent, portUI.layout.center);

                                if (orientation == PortOrientation.Horizontal)
                                    newPosY += nodeModel.Position.y - portPos.y;
                                else
                                    newPosX += nodeModel.Position.x - portPos.x;
                            }
                            else
                            {
                                // If the connection to the port is not compatible, we want the last hovered position to correspond to the node's middle width or height, depending on the orientation.
                                if (orientation == PortOrientation.Horizontal)
                                    newPosY -= nodeUI.layout.height * 0.5f;
                                else
                                    newPosX -= nodeUI.layout.width * 0.5f;
                            }

                            nodeModel.Position = new Vector2(newPosX, newPosY);
                            break;
                        }
                    }

                    var wireUI = wireModel.GetView<Wire>(m_GraphView);
                    if (wireUI != null)
                        graphUpdater.MarkChanged(wireModel, ChangeHint.Layout);
                }
            }
        }

        void SetHiddenModelsToVisible(IEnumerable<Hash128> hiddenModels)
        {
            foreach (var hiddenModel in hiddenModels)
            {
                var modelUI = hiddenModel.GetView(m_GraphView);
                if (modelUI != null)
                    modelUI.visible = true;
            }
        }
    }
}
