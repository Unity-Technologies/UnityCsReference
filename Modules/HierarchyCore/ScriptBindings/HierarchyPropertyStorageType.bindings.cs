// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Describes how property values of a specific property are stored in memory.
    /// </summary>
    [NativeType("Modules/HierarchyCore/Public/HierarchyPropertyStorageType.h")]
    public enum HierarchyPropertyStorageType : int
    {
        /// <summary>
        /// The property is stored in a sparse array. In a sparse array, memory is allocated for each node regardless if the node has a value for the property or not.
        /// </summary>
        Sparse,
        /// <summary>
        /// The property is stored in a compact array of values. In a dense array, memory is allocated for a node only if the node has a value for the property.
        /// </summary>
        Dense,
        /// <summary>
        /// The property is stored as a binary blob.
        /// </summary>
        Blob,
        /// <summary>
        /// The property is stored in the default storage type. Dense is the default storage type.
        /// </summary>
        Default = Dense
    }
}
