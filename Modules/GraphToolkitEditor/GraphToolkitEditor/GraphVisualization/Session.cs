// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Stores information about a graph visualization context, including its unique identifier, the associated authoring graph's guid, and the visualization store for that graph.
/// </summary>
class Session
{
    /// <summary>
    /// The unique identifier for the <see cref="Context"/>.
    /// </summary>
    public Hash128 VisualizationContextId { get; }

    /// <summary>
    /// The guid of the graph associated with the visualization session.
    /// </summary>
    /// <remarks>
    /// This is the same guid as the one accessible through <see cref="Graph.Guid"/> on the authoring graph.
    /// In the case of subgraphs, this guid must be the one of the root graph, not the subgraph being visualized.
    /// </remarks>
    public Hash128 GraphGuid { get; }

    /// <summary>
    /// The store for visualization data related to the graph visualization session.
    /// </summary>
    public Store Store { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Session"/> class.
    /// </summary>
    public Session(Hash128 graphGuid)
    {
        VisualizationContextId = Hash128Helpers.GenerateUnique();
        GraphGuid = graphGuid;
        Store = new Store();
    }
}
