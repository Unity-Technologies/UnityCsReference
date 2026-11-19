// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for models that are placeholders.
    /// </summary>
    /// <remarks>Placeholders are used to replace models when they have a missing type.</remarks>
    [UnityRestricted]
    internal interface IPlaceholder
    {
        /// <summary>
        /// The GUID of the placeholder.
        /// </summary>
        Hash128 Guid { get; }

        /// <summary>
        /// The reference ID of the model.
        /// </summary>
        long ReferenceId { get; }
    }
}
