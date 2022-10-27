// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A container for graph elements.
    /// </summary>
    interface IGraphElementContainer
    {
        /// <summary>
        /// The contained element models.
        /// </summary>
        IEnumerable<GraphElementModel> GraphElementModels { get; }

        /// <summary>
        /// Removes graph element models from the container.
        /// </summary>
        /// <param name="elementModels">The elements to remove.</param>
        void RemoveElements(IReadOnlyCollection<GraphElementModel> elementModels);

        /// <summary>
        /// Repair the container by removing invalid or null references.
        /// </summary>
        void Repair();
    }
}
