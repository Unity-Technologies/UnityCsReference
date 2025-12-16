// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Observer for <see cref="GraphViewCullingStateComponent"/>. Updates the culling state of
    /// <see cref="GraphElement"/>s based on the changes in the state component.
    /// </summary>
    [UnityRestricted]
    internal class GraphViewCullingObserver : StateObserver
    {
        GraphViewCullingStateComponent m_State;
        GraphView m_GraphView;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphViewCullingObserver"/> class.
        /// </summary>
        /// <param name="graphView">The <see cref="GraphView"/> containing the <see cref="GraphElement"/>s to be culled.</param>
        /// <param name="stateComponent">The <see cref="GraphViewCullingStateComponent"/>.</param>
        public GraphViewCullingObserver(GraphView graphView, GraphViewCullingStateComponent stateComponent)
            : base(new IStateComponent[] { stateComponent }, new IStateComponent[] { stateComponent })
        {
            m_GraphView = graphView;
            m_State = stateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            if (m_GraphView == null)
                return;
            const bool forceRevealAll = false;
            const bool alwaysCullRevealed = true;
            const int maxOperationCount = 25;

            int actuallyRevealedCount, actuallyCulledCount;
            List<KeyValuePair<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>>> elementsToCull;
            List<KeyValuePair<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>>> elementsToReveal;

            using var dispose = ListPool<KeyValuePair<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>>>.Get(out var elementsOnBothList);
            elementsOnBothList.Clear();
            using (var observation = this.ObserveState(m_State))
            {
                if (observation.UpdateType == UpdateType.None)
                    return;

                var changeset = m_State.GetAggregatedChangeset(observation.LastObservedVersion);

                if (changeset == null)
                {
                    ResetGraphViewCullingState();
                    return;
                }

                var elementsToCullDictionary = changeset.ElementsToCull;
                elementsToReveal = changeset.ElementsToReveal.ToList();

                int operationCount = forceRevealAll ? int.MaxValue : maxOperationCount;

                var elementThatNeedUpdate = new Dictionary<GraphElement, bool>();

                actuallyRevealedCount = SetCullingStateOnGraphElements(elementsToReveal, GraphViewCullingState.Disabled, ref operationCount, elementThatNeedUpdate);

                // immediately apply culling state on elements that have been revealed but are also in the culling list
                if (alwaysCullRevealed && actuallyRevealedCount > 0)
                {
                    var revealedElements = elementsToReveal.GetRange(0, actuallyRevealedCount);

                    foreach (var element in elementsToCullDictionary)
                    {
                        if (revealedElements.HasAny(r => r.Key.Equals(element.Key)))
                            elementsOnBothList.Add(element);
                    }

                    int max = int.MaxValue;

                    SetCullingStateOnGraphElements(elementsOnBothList, GraphViewCullingState.Enabled, ref max, elementThatNeedUpdate);
                }

                elementsToCull = elementsToCullDictionary.ToList();
                if (elementsOnBothList.Count > 0)
                {
                    elementsToCull.RemoveAll(t => elementsOnBothList.HasAny(u => u.Key.Equals(t.Key)));
                }

#pragma warning disable 0162
                if (forceRevealAll)
                    operationCount = actuallyRevealedCount >= maxOperationCount ? 0 : operationCount - actuallyRevealedCount;
#pragma warning restore 0162


                actuallyCulledCount = operationCount == 0 ? 0 : SetCullingStateOnGraphElements(elementsToCull, GraphViewCullingState.Enabled, ref operationCount, elementThatNeedUpdate);

                foreach (var kv in elementThatNeedUpdate)
                {
                    if (kv.Value != kv.Key.IsCulled())
                        kv.Key.UpdateCulling();
                }
            }

            if (elementsToCull.Count != actuallyCulledCount || elementsToReveal.Count != actuallyRevealedCount)
            {
                using var updater = m_State.UpdateScope;
                for (int i = actuallyCulledCount; i < elementsToCull.Count; i++)
                {
                    updater.MarkGraphElementAsCulled(elementsToCull[i].Key, elementsToCull[i].Value);
                }
                for (int i = actuallyRevealedCount; i < elementsToReveal.Count; i++)
                {
                    updater.MarkGraphElementAsRevealed(elementsToReveal[i].Key, elementsToReveal[i].Value);
                }
            }
        }

        void ResetGraphViewCullingState()
        {
            if (m_GraphView == null || m_GraphView.CullingState == GraphViewCullingState.Disabled || m_GraphView.GraphModel == null)
                return;

            var allCullingSources = m_GraphView.GetAllCullingSources();
            using var pooledChildViewList = ListPool<ChildView>.Get(out var childViews);
            m_GraphView.GraphModel.GetGraphElementModels().GetAllViewsRecursively(m_GraphView, view => view is GraphElement, childViews);
            using var pooledActiveCullingSourceList = ListPool<GraphViewCullingSource>.Get(out var activeCullingSources);

            var elementThatNeedUpdate = new Dictionary<GraphElement, bool>();
            for (var i = 0; i < childViews.Count; ++i)
            {
                var childView = childViews[i];
                if (childView is not GraphElement ge)
                    continue;
                activeCullingSources.Clear();
                if (m_GraphView.ShouldGraphElementBeCulled(ge, allCullingSources, activeCullingSources))
                    SetCullingStateOnGraphElement(ge, GraphViewCullingState.Enabled, activeCullingSources, elementThatNeedUpdate);
            }

            foreach (var kv in elementThatNeedUpdate)
            {
                if (kv.Value != kv.Key.IsCulled())
                    kv.Key.UpdateCulling();
            }
        }

        int SetCullingStateOnGraphElements(IReadOnlyList<KeyValuePair<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>>> graphElementKeys, GraphViewCullingState cullingState, ref int operationCount, Dictionary<GraphElement, bool> elementThatNeedUpdate)
        {
            int actuallyPerformed = 0;
            foreach (var (graphElementKey, cullingSources) in graphElementKeys)
            {
                var view = graphElementKey.ModelGuid.GetView<GraphElement>(m_GraphView, graphElementKey.ViewContext);
                SetCullingStateOnGraphElement(view, cullingState, cullingSources, elementThatNeedUpdate);
                actuallyPerformed++;
                if (--operationCount <= 0)
                    break;
            }

            return actuallyPerformed;
        }

        void SetCullingStateOnGraphElement(GraphElement graphElement, GraphViewCullingState state, IReadOnlyList<GraphViewCullingSource> cullingSources, Dictionary<GraphElement, bool> elementThatNeedUpdate)
        {
            if (graphElement == null)
                return;
            if (graphElement.SetCullingState(state, cullingSources))
            {
                if (!elementThatNeedUpdate.ContainsKey(graphElement))
                {
                    elementThatNeedUpdate.Add(graphElement, !graphElement.IsCulled());
                }
            }
            if (graphElement.Model is IGraphElementContainer container)
            {
                foreach (var childModel in container.GetGraphElementModels())
                {
                    var childView = childModel.GetView<GraphElement>(m_GraphView);
                    SetCullingStateOnGraphElement(childView, state, cullingSources, elementThatNeedUpdate);
                }
            }
        }
    }
}
