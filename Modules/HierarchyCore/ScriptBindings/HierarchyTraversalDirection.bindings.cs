// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Direction of traversal operation.
    /// </summary>
    [Flags, NativeHeader("Modules/HierarchyCore/Public/HierarchyTraversalDirection.h")]
    public enum HierarchyTraversalDirection : uint
    {
        /// <summary>
        /// Traverse all parents.
        /// </summary>
        Parents = 0,
        /// <summary>
        /// Traverse all children.
        /// </summary>
        Children = 1
    }
}
