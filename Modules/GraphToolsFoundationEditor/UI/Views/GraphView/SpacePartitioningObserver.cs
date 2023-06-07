// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Observer that updates the space partitioning of a <see cref="GraphView"/>.
/// </summary>
class SpacePartitioningObserver : StateObserver
{
    GraphView m_GraphView;

    /// <summary>
    /// Creates a <see cref="SpacePartitioningObserver"/>.
    /// </summary>
    /// <param name="graphView">The <see cref="GraphView"/> to update the space partitioning.</param>
    /// <param name="spacePartitioningState">The <see cref="SpacePartitioningStateComponent"/> to monitor.</param>
    public SpacePartitioningObserver(GraphView graphView, SpacePartitioningStateComponent spacePartitioningState)
        : base(spacePartitioningState)
    {
        m_GraphView = graphView;
    }

    /// <inheritdoc />
    public override void Observe()
    {
        m_GraphView?.UpdateSpacePartitioning();
    }
}
