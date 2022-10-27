// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface for views that can act as the source of a drag and drop operation.
    /// </summary>
    interface IDragSource
    {
        /// <summary>
        /// Get the currently selected graph element models.
        /// </summary>
        /// <returns>The currently selected graph element models.</returns>
        IReadOnlyList<GraphElementModel> GetSelection();
    }
}
