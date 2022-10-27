// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// How to reorder in a collection ordered by Z-Order (first element in in the back).
    /// <example><see cref="ToFront"/> should place at the end of the collection.</example>
    /// </summary>
    enum ZOrderMove
    {
        /// <summary>
        /// Move in front of every other.
        /// </summary>
        ToFront = ReorderType.MoveLast,
        /// <summary>
        /// Move up one level, to swap order with the one above.
        /// </summary>
        Forward = ReorderType.MoveDown,
        /// <summary>
        /// Move down one level, to swap order with the one below.
        /// </summary>
        Backward = ReorderType.MoveUp,
        /// <summary>
        /// Move behind every other.
        /// </summary>
        ToBack = ReorderType.MoveFirst
    }
}
