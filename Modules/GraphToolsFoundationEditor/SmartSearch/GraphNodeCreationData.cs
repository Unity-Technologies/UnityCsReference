// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Graph node creation data used by the library.
    /// </summary>
    readonly struct GraphNodeCreationData : IGraphNodeCreationData
    {
        /// <summary>
        /// The interface to the graph where we want the node to be created in.
        /// </summary>
        public GraphModel GraphModel { get; }

        /// <summary>
        /// The position at which the node should be created.
        /// </summary>
        public Vector2 Position { get; }

        /// <summary>
        /// The flags specifying how the node is to be spawned.
        /// </summary>
        public SpawnFlags SpawnFlags { get; }

        /// <summary>
        /// The SerializableGUID to assign to the newly created item.
        /// </summary>
        public SerializableGUID Guid { get; }

        /// <summary>
        /// Initializes a new GraphNodeCreationData.
        /// </summary>
        /// <param name="graphModel">The interface to the graph where we want the node to be created in.</param>
        /// <param name="position">The position at which the node should be created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        public GraphNodeCreationData(GraphModel graphModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, SerializableGUID guid = default)
        {
            GraphModel = graphModel;
            Position = position;
            SpawnFlags = spawnFlags;
            Guid = guid;
        }
    }
}
