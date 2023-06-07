// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Holds the graph elements that need to be partitioned or removed from the partitioning in the current view.
/// </summary>
class SpacePartitioningStateComponent : StateComponent<SpacePartitioningStateComponent.StateUpdater>
{
    /// <summary>
    /// Updater for the <see cref="SpacePartitioningStateComponent"/>
    /// </summary>
    public class StateUpdater : BaseUpdater<SpacePartitioningStateComponent>
    {
        /// <summary>
        /// Marks the <see cref="GraphElement"/> to be added or updated in the partitioning.
        /// </summary>
        /// <param name="graphElement">The <see cref="GraphElement"/> to partition.</param>
        public void MarkGraphElementForPartitioning(GraphElement graphElement)
        {
            MarkGraphElementForPartitioning(new GraphElementSpacePartitioningKey(graphElement));
        }

        /// <summary>
        /// Marks the <see cref="GraphElement"/> to be added or updated in the partitioning.
        /// </summary>
        /// <param name="graphElementPartitioningKey">The <see cref="GraphElementSpacePartitioningKey"/> of the <see cref="GraphElement"/> to partition.</param>
        public void MarkGraphElementForPartitioning(GraphElementSpacePartitioningKey graphElementPartitioningKey)
        {
            m_State.CurrentChangeset.ElementsToPartition.Add(graphElementPartitioningKey);
            m_State.SetUpdateType(UpdateType.Partial);
        }

        /// <summary>
        /// Marks the <see cref="GraphElement"/> to be removed from the partitioning.
        /// </summary>
        /// <param name="graphElement">The <see cref="GraphElement"/> to remove from the partitioning.</param>
        public void MarkGraphElementForRemoval(GraphElement graphElement)
        {
            MarkGraphElementForRemoval(new GraphElementSpacePartitioningKey(graphElement), graphElement.parent);
        }

        /// <summary>
        /// Marks the <see cref="GraphElement"/> to be removed from the partitioning.
        /// </summary>
        /// <param name="graphElementPartitioningKey">The <see cref="GraphElementSpacePartitioningKey"/> of the <see cref="GraphElement"/> to remove from the partitioning.</param>
        /// <param name="container">The container from where it is removed.</param>
        public void MarkGraphElementForRemoval(GraphElementSpacePartitioningKey graphElementPartitioningKey, VisualElement container)
        {
            m_State.CurrentChangeset.ElementsToRemoveFromPartitioning.Add((container, graphElementPartitioningKey));
            m_State.SetUpdateType(UpdateType.Partial);
        }

        /// <summary>
        /// Marks the <see cref="GraphElement"/> to change partitioning container. Partitioning is done by containers,
        /// so if a <see cref="GraphElement"/> changes container, it must be removed from its current partitioning
        /// and then partitioned in the new container.
        /// </summary>
        /// <param name="graphElement">The <see cref="GraphElement"/> that changed container.</param>
        /// <param name="oldContainer">The old container.</param>
        /// <param name="newContainer">The new container.</param>
        public void MarkGraphElementForChangingContainer(GraphElement graphElement, VisualElement oldContainer, VisualElement newContainer)
        {
            MarkGraphElementForChangingContainer(new GraphElementSpacePartitioningKey(graphElement), oldContainer, newContainer);
        }

        /// <summary>
        /// Marks the <see cref="GraphElement"/> to change partitioning container. Partitioning is done by containers,
        /// so if a <see cref="GraphElement"/> changes container, it must be removed from its current partitioning
        /// and then partitioned in the new container.
        /// </summary>
        /// <param name="graphElementPartitioningKey">The <see cref="GraphElementSpacePartitioningKey"/> of the <see cref="GraphElement"/> that changed container.</param>
        /// <param name="oldContainer">The old container.</param>
        /// <param name="newContainer">The new container.</param>
        public void MarkGraphElementForChangingContainer(GraphElementSpacePartitioningKey graphElementPartitioningKey, VisualElement oldContainer, VisualElement newContainer)
        {
            m_State.CurrentChangeset.ElementsToChangeContainer.Add((oldContainer, newContainer, graphElementPartitioningKey));
            m_State.SetUpdateType(UpdateType.Partial);
        }
    }

    public class Changeset : IChangeset
    {
        /// <summary>
        /// The partitioned elements.
        /// </summary>
        public HashSet<GraphElementSpacePartitioningKey> ElementsToPartition { get; private set; }

        /// <summary>
        /// The elements that are to be removed from the partitioning.
        /// </summary>
        public HashSet<(VisualElement, GraphElementSpacePartitioningKey)> ElementsToRemoveFromPartitioning { get; private set; }

        /// <summary>
        /// The elements that moved from one container to another and must be repartitioned.
        /// </summary>
        public HashSet<(VisualElement, VisualElement, GraphElementSpacePartitioningKey)> ElementsToChangeContainer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Changeset" /> class.
        /// </summary>
        public Changeset()
        {
            ElementsToPartition = new HashSet<GraphElementSpacePartitioningKey>();
            ElementsToRemoveFromPartitioning = new HashSet<(VisualElement, GraphElementSpacePartitioningKey)>();
            ElementsToChangeContainer = new HashSet<(VisualElement, VisualElement, GraphElementSpacePartitioningKey)>();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            ElementsToPartition.Clear();
            ElementsToRemoveFromPartitioning.Clear();
            ElementsToChangeContainer.Clear();
        }

        /// <inheritdoc/>
        public void AggregateFrom(IReadOnlyList<IChangeset> changesets)
        {
            Clear();
            foreach (var cs in changesets)
            {
                if (cs is Changeset changeset)
                {
                    ElementsToPartition.UnionWith(changeset.ElementsToPartition);
                    ElementsToRemoveFromPartitioning.UnionWith(changeset.ElementsToRemoveFromPartitioning);
                    ElementsToChangeContainer.UnionWith(changeset.ElementsToChangeContainer);
                }
            }
        }

        /// <inheritdoc />
        public bool Reverse()
        {
            var newElementsToPartition = new HashSet<GraphElementSpacePartitioningKey>(ElementsToRemoveFromPartitioning.Count);
            foreach (var elementToRemoveFromPartitioning in ElementsToRemoveFromPartitioning)
                newElementsToPartition.Add(elementToRemoveFromPartitioning.Item2);

            var newElementsToRemoveFromPartitioning = new HashSet<(VisualElement, GraphElementSpacePartitioningKey)>(ElementsToPartition.Count);
            foreach (var partitioningKey in ElementsToPartition)
            {
                newElementsToRemoveFromPartitioning.Add((null, partitioningKey));
            }

            var newElementsToChangeContainer = new HashSet<(VisualElement, VisualElement, GraphElementSpacePartitioningKey)>(ElementsToChangeContainer.Count);
            foreach (var elementToChangeContainer in ElementsToChangeContainer)
            {
                // Swap old and new container.
                newElementsToChangeContainer.Add((elementToChangeContainer.Item2, elementToChangeContainer.Item1, elementToChangeContainer.Item3));
            }

            ElementsToPartition = newElementsToPartition;
            ElementsToRemoveFromPartitioning = newElementsToRemoveFromPartitioning;
            ElementsToChangeContainer = newElementsToChangeContainer;

            return true;
        }
    }

    ChangesetManager<Changeset> m_ChangesetManager = new();
    Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

    /// <inheritdoc />
    public override ChangesetManager ChangesetManager => m_ChangesetManager;

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
