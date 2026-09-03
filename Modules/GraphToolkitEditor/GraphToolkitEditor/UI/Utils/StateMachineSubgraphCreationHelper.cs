// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Helper for creating subgraph states from a selection of states in a state machine graph.
    /// Transitions that cross the selection boundary are preserved and reconnected to the resulting subgraph state.
    /// </summary>
    [UnityRestricted]
    internal class StateMachineSubgraphCreationHelper : SubgraphCreationHelper
    {
        struct CrossingTransitionInfo
        {
            /// <summary>Whether the transition goes from an external state into the selection.</summary>
            public bool IsIncoming;
            /// <summary>The state outside the selection that the transition connects to.</summary>
            public StateModel ExternalState;
            /// <summary>
            /// Reference to the original wire before it is deleted along with the transferred states.
            /// Kept alive so we can copy its transition conditions after deletion.
            /// </summary>
            public TransitionSupportModel OriginalTransition;
        }

        // Both CollectCrossingTransitions and TransferElements need the same set; compute it once in PopulateSubgraph.
        HashSet<AbstractNodeModel> m_SelectedNodes;
        List<CrossingTransitionInfo> m_CrossingTransitions;

        /// <inheritdoc />
        protected override bool ShouldCreateVariableDeclarations => false;

        /// <inheritdoc />
        protected override void PopulateSubgraph(GraphModel newSubgraph,
            List<GraphElementModel> elementsToTransfer, List<SubgraphNodePortInfo> subgraphNodePortInfos)
        {
            m_SelectedNodes = BuildSelectedNodes(elementsToTransfer);
            // Capture crossing transition info before the base call transfers and deletes elements.
            CollectCrossingTransitions(elementsToTransfer);
            base.PopulateSubgraph(newSubgraph, elementsToTransfer, subgraphNodePortInfos);
        }

        static HashSet<AbstractNodeModel> BuildSelectedNodes(List<GraphElementModel> elements)
        {
            var set = new HashSet<AbstractNodeModel>();
            foreach (var element in elements)
            {
                if (element is AbstractNodeModel node)
                    set.Add(node);
            }
            return set;
        }

        void CollectCrossingTransitions(List<GraphElementModel> elementsToTransfer)
        {
            m_CrossingTransitions = new List<CrossingTransitionInfo>();

            // Dedup: the same crossing transition can be found via both of its endpoint states.
            using var _ = HashSetPool<TransitionSupportModel>.Get(out var seenTransitions);

            foreach (var element in elementsToTransfer)
            {
                if (element is not StateModel state)
                    continue;

                // Transitions targeting this state from an external state (incoming).
                var inPort = state.GetInPort();
                if (inPort != null)
                {
                    foreach (var wire in inPort.GetConnectedWires())
                    {
                        if (wire is not TransitionSupportModel transition || !seenTransitions.Add(transition))
                            continue;

                        var fromNode = transition.FromPort?.NodeModel;
                        if (fromNode != null && !m_SelectedNodes.Contains(fromNode) && fromNode is StateModel externalFrom)
                        {
                            m_CrossingTransitions.Add(new CrossingTransitionInfo
                            {
                                IsIncoming = true,
                                ExternalState = externalFrom,
                                OriginalTransition = transition
                            });
                        }
                    }
                }

                // Transitions originating from this state to an external state (outgoing).
                var outPort = state.GetOutPort();
                if (outPort != null)
                {
                    foreach (var wire in outPort.GetConnectedWires())
                    {
                        if (wire is not TransitionSupportModel transition || !seenTransitions.Add(transition))
                            continue;

                        var toNode = transition.ToPort?.NodeModel;
                        if (toNode != null && !m_SelectedNodes.Contains(toNode) && toNode is StateModel externalTo)
                        {
                            m_CrossingTransitions.Add(new CrossingTransitionInfo
                            {
                                IsIncoming = false,
                                ExternalState = externalTo,
                                OriginalTransition = transition
                            });
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override Dictionary<Hash128, AbstractNodeModel> TransferElements(GraphModel newSubgraph,
            List<GraphElementModel> elementsToTransfer)
        {
            // Only transfer transitions where both endpoints are inside the selection.
            // Crossing transitions are excluded here; they will be re-created as new transitions
            // connecting the external states to the subgraph state in CreateWiresToSubgraphNode.
            using var disposeAddedWires = HashSetPool<WireModel>.Get(out var addedWires);
            using var disposeListElements = ListPool<GraphElementModel>.Get(out var listElements);

            foreach (var element in elementsToTransfer)
            {
                listElements.Add(element);

                if (element is StateModel state)
                {
                    // StateModel : PortNodeModel : AbstractNodeModel (not NodeModel), so GetConnectedWires()
                    // is not available on the node. Iterate the hidden in/out ports directly instead.
                    AddInternalWires(state.GetOutPort(), m_SelectedNodes, addedWires, listElements);
                    AddInternalWires(state.GetInPort(), m_SelectedNodes, addedWires, listElements);
                }
                else if (element is NodeModel nodeModel)
                {
                    foreach (var wire in nodeModel.GetConnectedWires())
                    {
                        if (!addedWires.Add(wire))
                            continue;

                        var fromNode = wire.FromPort?.NodeModel;
                        var toNode = wire.ToPort?.NodeModel;
                        if (fromNode != null && m_SelectedNodes.Contains(fromNode) &&
                            toNode != null && m_SelectedNodes.Contains(toNode))
                            listElements.Add(wire);
                    }
                }
            }

            var copyPasteData = new CopyPasteData(null, listElements);
            var nodeMapping = CopyPasteData.PasteSerializedData(
                PasteOperation.Duplicate, Vector2.zero, null, null, copyPasteData, newSubgraph, null, false, true);
            copyPasteData.Dispose();

            return nodeMapping;
        }

        static void AddInternalWires(PortModel port, HashSet<AbstractNodeModel> selectedNodes,
            HashSet<WireModel> addedWires, List<GraphElementModel> listElements)
        {
            if (port == null)
                return;

            foreach (var wire in port.GetConnectedWires())
            {
                if (!addedWires.Add(wire))
                    continue;

                var fromNode = wire.FromPort?.NodeModel;
                var toNode = wire.ToPort?.NodeModel;
                if (fromNode != null && selectedNodes.Contains(fromNode) &&
                    toNode != null && selectedNodes.Contains(toNode))
                    listElements.Add(wire);
            }
        }

        /// <inheritdoc />
        protected override void CreateWiresToSubgraphNode(GraphModel mainGraph, ISubgraphNodeInternal subgraphNodeModel,
            List<SubgraphNodePortInfo> subgraphNodePortInfos)
        {
            if (subgraphNodeModel is not SubgraphStateModel subgraphState ||
                m_CrossingTransitions == null || m_CrossingTransitions.Count == 0)
                return;

            var inPort = subgraphState.GetInPort();
            var outPort = subgraphState.GetOutPort();

            foreach (var info in m_CrossingTransitions)
            {
                if (info.ExternalState == null)
                    continue;

                TransitionSupportModel newTransitionSupport;

                if (info.IsIncoming)
                {
                    // ExternalState ──▶ SubgraphState
                    // Preserve FromAnchor for the external state; reuse the original ToAnchor for the subgraph state.
                    newTransitionSupport = mainGraph.CreateTransitionSupport(
                        inPort,
                        info.OriginalTransition.ToNodeAnchorSide,
                        info.OriginalTransition.ToNodeAnchorOffset,
                        info.ExternalState.GetOutPort(),
                        info.OriginalTransition.FromNodeAnchorSide,
                        info.OriginalTransition.FromNodeAnchorOffset,
                        typeof(StateToStateTransitionModel));
                }
                else
                {
                    // SubgraphState ──▶ ExternalState
                    // Preserve ToAnchor for the external state; reuse the original FromAnchor for the subgraph state.
                    newTransitionSupport = mainGraph.CreateTransitionSupport(
                        info.ExternalState.GetInPort(),
                        info.OriginalTransition.ToNodeAnchorSide,
                        info.OriginalTransition.ToNodeAnchorOffset,
                        outPort,
                        info.OriginalTransition.FromNodeAnchorSide,
                        info.OriginalTransition.FromNodeAnchorOffset,
                        typeof(StateToStateTransitionModel));
                }

                if (newTransitionSupport == null)
                    continue;

                // Replace the auto-created empty transition with copies of the original conditions.
                newTransitionSupport.RemoveAllTransitionRules();
                newTransitionSupport.CopyTransitions(info.OriginalTransition);
            }
        }
    }
}
