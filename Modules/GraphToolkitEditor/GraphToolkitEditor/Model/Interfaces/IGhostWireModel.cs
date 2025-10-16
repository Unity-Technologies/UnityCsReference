// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for temporary wires.
    /// </summary>
    [UnityRestricted]
    internal interface IGhostWireModel
    {
        /// <summary>
        /// The position of the start of the wire.
        /// </summary>
        Vector2 FromWorldPoint { get; set; }

        /// <summary>
        /// The position of the end of the wire.
        /// </summary>
        Vector2 ToWorldPoint { get; set; }
    }
}
