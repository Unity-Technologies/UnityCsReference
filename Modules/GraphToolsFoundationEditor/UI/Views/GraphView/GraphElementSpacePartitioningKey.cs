// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Unique key representing a <see cref="GraphElement"/> used for space partitioning.
/// </summary>
readonly struct GraphElementSpacePartitioningKey : IEquatable<GraphElementSpacePartitioningKey>
{
    /// <summary>
    /// The guid of the <see cref="GraphElement"/>'s model.
    /// </summary>
    public readonly Hash128 ModelGuid;

    /// <summary>
    /// The <see cref="IViewContext"/> of the <see cref="GraphElement"/>.
    /// </summary>
    public readonly IViewContext ViewContext;

    /// <summary>
    /// Constructs a new <see cref="GraphElementSpacePartitioningKey"/>.
    /// </summary>
    /// <param name="modelGuid">The guid of the <see cref="GraphElement"/>'s model.</param>
    /// <param name="viewContext">The <see cref="IViewContext"/> of the <see cref="GraphElement"/>.</param>
    public GraphElementSpacePartitioningKey(Hash128 modelGuid, IViewContext viewContext)
    {
        ModelGuid = modelGuid;
        ViewContext = viewContext;
    }

    /// <summary>
    /// Constructs a new <see cref="GraphElementSpacePartitioningKey"/>.
    /// </summary>
    /// <param name="graphElement">The <see cref="GraphElement"/> to construct a key from.</param>
    public GraphElementSpacePartitioningKey(GraphElement graphElement)
        : this(graphElement.Model.Guid, graphElement.Context)
    {}

    /// <summary>
    /// Checks whether two <see cref="GraphElementSpacePartitioningKey"/> are equal.
    /// </summary>
    /// <param name="other">The other <see cref="GraphElementSpacePartitioningKey"/> to check equality against.</param>
    /// <returns>True if the two <see cref="GraphElementSpacePartitioningKey"/> are equal, false otherwise.</returns>
    public bool Equals(GraphElementSpacePartitioningKey other)
    {
        return ModelGuid == other.ModelGuid && ViewContext == other.ViewContext;
    }
}
