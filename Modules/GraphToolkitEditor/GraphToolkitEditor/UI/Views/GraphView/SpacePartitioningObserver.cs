// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Observer that updates the space partitioning of a <see cref="GraphView"/>.
    /// </summary>
    [UnityRestricted]
    internal class SpacePartitioningObserver : StateObserver
    {
        GraphView m_GraphView;

        /// <summary>
        /// Creates a <see cref="SpacePartitioningObserver"/>.
        /// </summary>
        /// <param name="graphView">The <see cref="GraphView"/> to update the space partitioning.</param>
        /// <param name="spacePartitioningState">The <see cref="SpacePartitioningStateComponent"/> to monitor.</param>
        /// <param name="cullingState">The <see cref="GraphViewCullingStateComponent"/> that is modified.</param>
        public SpacePartitioningObserver(GraphView graphView, SpacePartitioningStateComponent spacePartitioningState, GraphViewCullingStateComponent cullingState)
            : base(new IStateComponent[] { spacePartitioningState }, new IStateComponent[] { cullingState })
        {
            m_GraphView = graphView;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            m_GraphView?.UpdateSpacePartitioning();
        }
    }
}
