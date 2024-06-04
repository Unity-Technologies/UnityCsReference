// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Bit flags used to describe the state of a hierarchy node.
    /// </summary>
    [Flags, NativeHeader("Modules/HierarchyCore/Public/HierarchyNodeFlags.h")]
    public enum HierarchyNodeFlags : uint
    {
        /// <summary>
        /// The hierarchy node has no flags.
        /// </summary>
        None = 0,
        /// <summary>
        /// The hierarchy node is expanded.
        /// </summary>
        Expanded = 1 << 0,
        /// <summary>
        /// The hierarchy node is selected.
        /// </summary>
        Selected = 1 << 1,
        /// <summary>
        /// The hierarchy node is cut.
        /// </summary>
        Cut = 1 << 2,
        /// <summary>
        /// The hierarchy node is hidden (also hides children).
        /// </summary>
        Hidden = 1 << 3,
    }
}
