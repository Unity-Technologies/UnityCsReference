// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a hierarchy node.
    /// </summary>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyNode.h")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct HierarchyNode : IEquatable<HierarchyNode>
    {
        const int k_HierarchyNodeIdNull = 0;
        const int k_HierarchyNodeVersionNull = 0;

        static readonly HierarchyNode s_Null;
        readonly int m_Id;
        readonly int m_Version;

        /// <summary>
        /// Represents a hierarchy node that is null or invalid.
        /// </summary>
        public static ref readonly HierarchyNode Null => ref s_Null;

        /// <summary>
        /// The unique identification number of the hierarchy node.
        /// </summary>
        public int Id => m_Id;

        /// <summary>
        /// The version number of the hierarchy node.
        /// </summary>
        public int Version => m_Version;

        /// <summary>
        /// Creates a hierarchy node.
        /// </summary>
        public HierarchyNode()
        {
            m_Id = k_HierarchyNodeIdNull;
            m_Version = k_HierarchyNodeVersionNull;
        }

        [ExcludeFromDocs]
        public static bool operator ==(in HierarchyNode lhs, in HierarchyNode rhs) => lhs.Id == rhs.Id && lhs.Version == rhs.Version;
        [ExcludeFromDocs]
        public static bool operator !=(in HierarchyNode lhs, in HierarchyNode rhs) => !(lhs == rhs);
        [ExcludeFromDocs]
        public bool Equals(HierarchyNode other) => other.Id == Id && other.Version == Version;
        [ExcludeFromDocs]
        public override string ToString() => $"{nameof(HierarchyNode)}({(this == Null ? nameof(Null) : $"{Id}:{Version}")})";
        [ExcludeFromDocs]
        public override bool Equals(object obj) => obj is HierarchyNode node && Equals(node);
        [ExcludeFromDocs]
        public override int GetHashCode() => HashCode.Combine(Id, Version);
    }
}
