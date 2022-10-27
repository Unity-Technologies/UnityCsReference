// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// How to reorder an item in a collection.
    /// </summary>
    enum ReorderType
    {
        /// <summary>
        /// Make the item the first.
        /// </summary>
        MoveFirst,
        /// <summary>
        /// Move the item one position towards the beginning.
        /// </summary>
        MoveUp,
        /// <summary>
        /// Move the item one position towards the end.
        /// </summary>
        MoveDown,
        /// <summary>
        /// Make the item the last.
        /// </summary>
        MoveLast,
    }
}
