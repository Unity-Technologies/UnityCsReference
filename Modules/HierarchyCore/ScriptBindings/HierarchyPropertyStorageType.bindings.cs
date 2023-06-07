// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Describe how property values of a specific property are stored in memory.
    /// </summary>
    [NativeType("Modules/HierarchyCore/Public/HierarchyPropertyStorageType.h")]
    public enum HierarchyPropertyStorageType : int
    {
        /// <summary>
        ///  Property is stored in a sparse array.
        /// </summary>
        Sparse,
        /// <summary>
        /// Property is stored in a compact array of values.
        /// </summary>
        Dense,
        /// <summary>
        /// Property is stored as a binary blob.
        /// </summary>
        Blob,
        /// <summary>
        /// Default property storage: dense.
        /// </summary>
        Default = Dense
    }
}
