// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for temporary wires.
    /// </summary>
    [UnityRestricted]
    internal abstract class AbstractGhostWireModel : WireModel, IGhostWireModel
    {
        /// <inheritdoc />
        public virtual Vector2 FromWorldPoint { get; set; } = Vector2.zero;

        /// <inheritdoc />
        public virtual Vector2 ToWorldPoint { get; set; } = Vector2.zero;
    }
}
