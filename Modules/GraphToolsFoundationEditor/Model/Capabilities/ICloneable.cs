// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface for cloneable graph element models.
    /// </summary>
    interface ICloneable
    {
        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <remarks>Note that it does not add the instance to a <see cref="GraphModel"/>.</remarks>
        /// <returns>A clone of this graph element.</returns>
        GraphElementModel Clone();
    }
}
