// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Holds the graph elements that need to be culled or revealed in the current view.
    /// </summary>
    [UnityRestricted]
    internal class GraphViewCullingStateComponent : StateComponent<GraphViewCullingStateComponent.StateUpdater>
    {
        /// <summary>
        /// State updater for <see cref="GraphViewCullingStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<GraphViewCullingStateComponent>
        {
            /// <summary>
            /// Marks a graph element as culled from a culling source.
            /// </summary>
            /// <param name="graphElement">The graph element</param>
            /// <param name="cullingSource">The culling source.</param>
            public void MarkGraphElementAsCulled(GraphElement graphElement, GraphViewCullingSource cullingSource)
            {
                MarkGraphElementAsCulled(new GraphElementSpacePartitioningKey(graphElement), cullingSource);
            }

            /// <summary>
            /// Marks a graph element as culled from a list of culling sources.
            /// </summary>
            /// <param name="graphElement">The graph element.</param>
            /// <param name="cullingSources">The culling sources.</param>
            public void MarkGraphElementAsCulled(GraphElement graphElement, IReadOnlyList<GraphViewCullingSource> cullingSources)
            {
                var graphElementKey = new GraphElementSpacePartitioningKey(graphElement);
                MarkGraphElementAsCulled(graphElementKey, cullingSources);
            }

            /// <summary>
            /// Marks a graph element as culled from a list of culling sources.
            /// </summary>
            /// <param name="graphElementKey">The graph element key.</param>
            /// <param name="cullingSources">The culling sources.</param>
            public void MarkGraphElementAsCulled(GraphElementSpacePartitioningKey graphElementKey, IReadOnlyList<GraphViewCullingSource> cullingSources)
            {
                for (var i = 0; i < cullingSources.Count; ++i)
                {
                    var cullingSource = cullingSources[i];
                    MarkGraphElementAsCulled(graphElementKey, cullingSource);
                }
            }

            /// <summary>
            /// Marks a graph element as culled from a culling source.
            /// </summary>
            /// <param name="graphElementKey">The graph element key.</param>
            /// <param name="cullingSource">The culling source.</param>
            public void MarkGraphElementAsCulled(GraphElementSpacePartitioningKey graphElementKey, GraphViewCullingSource cullingSource)
            {
                m_State.CurrentChangeset.AddElementToCulling(graphElementKey, cullingSource);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Marks a graph element as revealed from a culling source.
            /// </summary>
            /// <param name="graphElement">The graph element.</param>
            /// <param name="cullingSource">The culling source.</param>
            public void MarkGraphElementAsRevealed(GraphElement graphElement, GraphViewCullingSource cullingSource)
            {
                MarkGraphElementAsRevealed(new GraphElementSpacePartitioningKey(graphElement), cullingSource);
            }

            /// <summary>
            /// Marks a graph element as revealed from a list of culling sources.
            /// </summary>
            /// <param name="graphElement">The graph element.</param>
            /// <param name="cullingSources">The culling sources.</param>
            public void MarkGraphElementAsRevealed(GraphElement graphElement, IReadOnlyList<GraphViewCullingSource> cullingSources)
            {
                var graphElementKey = new GraphElementSpacePartitioningKey(graphElement);
                MarkGraphElementAsRevealed(graphElementKey, cullingSources);
            }

            /// <summary>
            /// Marks a graph element as revealed from a list of culling sources.
            /// </summary>
            /// <param name="graphElementKey">The graph element key.</param>
            /// <param name="cullingSources">The culling sources.</param>
            public void MarkGraphElementAsRevealed(GraphElementSpacePartitioningKey graphElementKey, IReadOnlyList<GraphViewCullingSource> cullingSources)
            {
                for (var i = 0; i < cullingSources.Count; ++i)
                {
                    var cullingSource = cullingSources[i];
                    MarkGraphElementAsRevealed(graphElementKey, cullingSource);
                }
            }

            /// <summary>
            /// Marks a graph element as revealed from a culling source.
            /// </summary>
            /// <param name="graphElementKey">The graph element key.</param>
            /// <param name="cullingSource">The culling source.</param>
            public void MarkGraphElementAsRevealed(GraphElementSpacePartitioningKey graphElementKey, GraphViewCullingSource cullingSource)
            {
                m_State.CurrentChangeset.RemoveElementFromCulling(graphElementKey, cullingSource);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Marks a graph element as culled or revealed from a culling source.
            /// </summary>
            /// <param name="graphElement">The graph element.</param>
            /// <param name="cullingSource">The culling source.</param>
            /// <param name="newState">The new culling state.</param>
            public void MarkGraphElementCullingChanged(GraphElement graphElement, GraphViewCullingSource cullingSource, GraphViewCullingState newState)
            {
                var graphElementKey = new GraphElementSpacePartitioningKey(graphElement);
                MarkGraphElementCullingChanged(graphElementKey, cullingSource, newState);
            }

            /// <summary>
            /// Marks a graph element as culled or revealed from a culling source.
            /// </summary>
            /// <param name="graphElementKey">The graph element key.</param>
            /// <param name="cullingSource">The culling source.</param>
            /// <param name="newState">The new culling state.</param>
            public void MarkGraphElementCullingChanged(GraphElementSpacePartitioningKey graphElementKey, GraphViewCullingSource cullingSource, GraphViewCullingState newState)
            {
                if (newState == GraphViewCullingState.Enabled)
                    MarkGraphElementAsCulled(graphElementKey, cullingSource);
                else
                    MarkGraphElementAsRevealed(graphElementKey, cullingSource);
            }
        }

        /// <summary>
        /// Class representing a changeset on the <see cref="GraphViewCullingStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class Changeset : IChangeset
        {
            Dictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>> m_ElementsToCull;
            Dictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>> m_ElementsToReveal;

            /// <summary>
            /// <see cref="GraphElement"/>s to cull.
            /// </summary>
            public IReadOnlyDictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>> ElementsToCull => m_ElementsToCull;

            /// <summary>
            /// <see cref="GraphElement"/>s to reveal.
            /// </summary>
            public IReadOnlyDictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>> ElementsToReveal => m_ElementsToReveal;

            /// <summary>
            /// Initializes a new instance of the <see cref="Changeset" /> class.
            /// </summary>
            public Changeset()
            {
                m_ElementsToCull = new Dictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>>();
                m_ElementsToReveal = new Dictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>>();
            }

            /// <inheritdoc/>
            public void Clear()
            {
                m_ElementsToCull.Clear();
                m_ElementsToReveal.Clear();
            }

            /// <inheritdoc/>
            public void AggregateFrom(IReadOnlyList<IChangeset> changesets)
            {
                Clear();
                foreach (var cs in changesets)
                {
                    if (cs is Changeset changeset)
                    {
                        UpdateCulledElements(m_ElementsToCull, m_ElementsToReveal, changeset.m_ElementsToCull);
                        UpdateCulledElements(m_ElementsToReveal, m_ElementsToCull, changeset.m_ElementsToReveal);
                    }
                }
            }

            /// <inheritdoc />
            public bool Reverse()
            {
                (m_ElementsToCull, m_ElementsToReveal) = (m_ElementsToReveal, m_ElementsToCull);
                return true;
            }

            /// <summary>
            /// Adds a <see cref="GraphElement"/> to the list of elements to cull.
            /// </summary>
            /// <param name="elementKey">The graph element key.</param>
            /// <param name="cullingSource">The culling source.</param>
            public void AddElementToCulling(GraphElementSpacePartitioningKey elementKey, GraphViewCullingSource cullingSource)
            {
                if (!RemoveCullingSource(m_ElementsToReveal, elementKey, cullingSource))
                    AddCullingSource(m_ElementsToCull, elementKey, cullingSource);
            }

            /// <summary>
            /// Adds a <see cref="GraphElement"/> to the list of elements to reveal.
            /// </summary>
            /// <param name="elementKey">The graph element key.</param>
            /// <param name="cullingSource">The culling source.</param>
            public void RemoveElementFromCulling(GraphElementSpacePartitioningKey elementKey, GraphViewCullingSource cullingSource)
            {
                if (!RemoveCullingSource(m_ElementsToCull, elementKey, cullingSource))
                    AddCullingSource(m_ElementsToReveal, elementKey, cullingSource);
            }

            static bool RemoveCullingSource(Dictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>> cullingElements, GraphElementSpacePartitioningKey graphElementKey, GraphViewCullingSource cullingSource)
            {
                if (!cullingElements.TryGetValue(graphElementKey, out var cullingSources))
                    return false;
                var writableList = cullingSources as List<GraphViewCullingSource>;
                return writableList?.Remove(cullingSource) ?? false;
            }

            static void AddCullingSource(Dictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>> cullingElements, GraphElementSpacePartitioningKey graphElementKey, GraphViewCullingSource cullingSource)
            {
                if (!cullingElements.TryGetValue(graphElementKey, out var cullingSources))
                {
                    cullingSources = new List<GraphViewCullingSource>();
                    cullingElements[graphElementKey] = cullingSources;
                }

                if (!cullingSources.Contains(cullingSource))
                {
                    var writableList = cullingSources as List<GraphViewCullingSource>;
                    writableList?.Add(cullingSource);
                }
            }

            static void UpdateCulledElements(Dictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>> elementsToUpdate, Dictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>> oppositeElements, Dictionary<GraphElementSpacePartitioningKey, IReadOnlyList<GraphViewCullingSource>> sourceElements)
            {
                foreach (var (graphElementKey, cullingSources) in sourceElements)
                {
                    for (var i = 0; i < cullingSources.Count; ++i)
                    {
                        var cullingSource = cullingSources[i];
                        RemoveCullingSource(oppositeElements, graphElementKey, cullingSource);
                        AddCullingSource(elementsToUpdate, graphElementKey, cullingSource);
                    }
                }
            }
        }

        ChangesetManager<Changeset> m_ChangesetManager = new();
        Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        /// <inheritdoc />
        public override IChangesetManager ChangesetManager => m_ChangesetManager;

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public Changeset GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is SpacePartitioningStateComponent)
            {
                SetUpdateType(UpdateType.Partial);
                CurrentChangeset.AggregateFrom(new[] { changeset });
            }
        }
    }
}
