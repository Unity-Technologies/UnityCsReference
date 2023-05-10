// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface to provide the data needed to create a node in a graph.
    /// </summary>
    interface IGraphNodeCreationData
    {
        /// <summary>
        /// Option used on node creation.
        /// </summary>
        SpawnFlags SpawnFlags { get; }
        /// <summary>
        /// Graph where to create the node.
        /// </summary>
        GraphModel GraphModel { get; }
        /// <summary>
        /// Position where to create the node.
        /// </summary>
        Vector2 Position { get; }
        /// <summary>
        /// Guid to give to the node on creation.
        /// </summary>
        Hash128 Guid { get; }
    }
}
