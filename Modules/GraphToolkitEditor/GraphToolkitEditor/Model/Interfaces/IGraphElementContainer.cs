// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A container for graph elements.
    /// </summary>
    [UnityRestricted]
    internal interface IGraphElementContainer
    {
        /// <summary>
        /// Gets the contained element models.
        /// </summary>
        /// <returns>The element models.</returns>
        IEnumerable<GraphElementModel> GetGraphElementModels();

        /// <summary>
        /// Removes graph element models from the container.
        /// </summary>
        /// <param name="elementModels">The elements to remove.</param>
        void RemoveContainerElements(IReadOnlyCollection<GraphElementModel> elementModels);

        /// <summary>
        /// Repair the container by removing invalid or null references.
        /// </summary>
        bool Repair();
    }
}
