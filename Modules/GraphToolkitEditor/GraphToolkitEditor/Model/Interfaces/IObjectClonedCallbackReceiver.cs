// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for classes that need to receive callbacks when a graph object is cloned.
    /// </summary>
    [UnityRestricted]
    internal interface IObjectClonedCallbackReceiver
    {
        /// <summary>
        /// Callback to clone assets. The object should clone all its assets (including itself) and store the mapping between the original and the clone.
        /// </summary>
        /// <param name="clones">A list of cloned assets. The newly cloned assets should be added to this list.</param>
        /// <param name="originalToCloneMap">A dictionary to store the mapping between the original assets and their clone.</param>
        void CloneAssets(List<Object> clones, Dictionary<Object, Object> originalToCloneMap);

        /// <summary>
        /// Callback to notify cloned assets that the clone process is finished. The object should update its references to the cloned assets.
        /// </summary>
        /// <param name="originalToCloneMap">A dictionary containing the mapping between the original and the clone.</param>
        void OnAfterAssetClone(IReadOnlyDictionary<Object, Object> originalToCloneMap);
    }
}
