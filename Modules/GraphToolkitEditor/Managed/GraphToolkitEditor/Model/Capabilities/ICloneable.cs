// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for cloneable graph element models.
    /// </summary>
    [UnityRestricted]
    internal interface ICloneable
    {
        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <returns>A clone of this graph element.</returns>
        Model Clone();
    }
}
