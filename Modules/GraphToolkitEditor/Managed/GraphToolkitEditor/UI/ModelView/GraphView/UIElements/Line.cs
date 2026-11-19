// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A line in 2D space.
    /// </summary>
    [UnityRestricted]
    internal readonly struct Line
    {
        /// <summary>
        /// The start of the line.
        /// </summary>
        public Vector2 Start { get; }

        /// <summary>
        /// The end of the line.
        /// </summary>
        public Vector2 End { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Line"/> class.
        /// </summary>
        public Line(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
    }
}
