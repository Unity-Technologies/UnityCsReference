// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// An observer that automatically places the graph elements that were marked for automatic placement.
    /// </summary>
    class AutoPlacementObserver : StateObserver
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
            : base(new[] { autoPlacementState }, new[] { graphModelState })
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
            List<GraphElementModel> models;
            List<AutoPlacementStateComponent.Changeset.ModelToReposition> elementsToReposition;

            // Only peek first to see if we need to wait before processing the placement.
            // Auto placement relies on UI layout to compute node positions, so we need to
            // make sure all graph element are properly layed out. If the graph elements exist,
            // the layout pass has probably been done in the last frame. If they do not exist,
            // the next layout pass will be done this frame after they are created by the ModelViewUpdater.
            using (var observation = this.PeekAtState(m_AutoPlacementState))
            {
                if (observation.UpdateType == UpdateType.None)
                    return;

                changeset = m_AutoPlacementState.GetAggregatedChangeset(observation.LastObservedVersion);

                models = changeset.ModelsToAutoAlign.Select(graphModel.GetModel).Where(m => m != null).ToList();
                elementsToReposition = changeset.ModelsToRepositionAtCreation.ToList();
                var elementsToRepositionModels = elementsToReposition.SelectMany(mtr => new[] { graphModel.GetModel(mtr.Model), graphModel.GetModel(mtr.WireModel) });

                // If any view is missing, then it will be created and layed out during this frame. So, wait until all relevant views are available.
                var anyMissingView = models.Concat(elementsToRepositionModels).Any(model => model.GetView<GraphElement>(m_GraphView) == null);
                if (anyMissingView)
                    return;
            }

            using (this.ObserveState(m_AutoPlacementState))
            using (var graphUpdater = m_GraphModelState.UpdateScope)
            using (var changeScope = graphModel.ChangeDescriptionScope)
            {
                if (models.Any())
                {
                    m_GraphView.PositionDependenciesManager_Internal.AlignNodes(true, models);
                }

                // Nodes created from wires need to recompute their position after their creation to make sure that the
                // last hovered position corresponds to the connected port or, in the case of an incompatible connection,
                // to the nodes' middle height or width (depending on the orientation).
                if (elementsToReposition.Any())
                {
                    RepositionModelsAtCreation(elementsToReposition, graphModel, graphUpdater);
                }

                if (changeset.ModelsToHideDuringAutoPlacement.Any())
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

                var nodeUI = nodeModel?.GetView<Node>(m_GraphView);
                if (nodeUI == null)
                    continue;

                if (wireModel != null)
                {
                    var portModel = modelToReposition.WireSide == WireSide.From ? wireModel.FromPort : wireModel.ToPort;

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
                    var isCompatibleConnection = wireModel is IGhostWire ? portModel != null : (nodeModel as PortNodeModel)?.GetPortFitToConnectTo(modelToReposition.WireSide == WireSide.From ? wireModel.ToPort : wireModel.FromPort) != null;

                    if (isCompatibleConnection && portUI != null)
                    {
                        // If the connection to the port is compatible, we want the last hovered position to correspond to the port.
                        var portPos = portUI.parent.ChangeCoordinatesTo(nodeUI.parent, portUI.layout.center);

                        if (orientation == PortOrientation.Horizontal)
                            newPosY += nodeUI.layout.y - portPos.y;
                        else
                            newPosX += nodeUI.layout.x - portPos.x;
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

                    var wireUI = wireModel.GetView<Wire>(m_GraphView);
                    if (wireUI != null)
                        graphUpdater.MarkChanged(wireModel, ChangeHint.Layout);
                }
            }
        }

        void SetHiddenModelsToVisible(IEnumerable<SerializableGUID> hiddenModels)
        {
            foreach (var hiddenModel in hiddenModels)
            {
                var modelUI = hiddenModel.GetView_Internal(m_GraphView);
                if (modelUI != null)
                    modelUI.visible = true;
            }
        }
    }
}
