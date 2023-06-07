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
    /// Type descriptor for a node in Hierarchy. Corresponds to the NodeType of the HierarchyNodeHandler.
    /// </summary>
    [NativeType(Header = "Modules/HierarchyCore/Public/HierarchyNodeType.h")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct HierarchyNodeType : IEquatable<HierarchyNodeType>
    {
        const int k_HierarchyNodeTypeNull = 0;

        static readonly HierarchyNodeType s_Null;
        readonly int m_Id;

        /// <summary>
        /// Represents an hierarchy node type that is null/invalid.
        /// </summary>
        public static ref readonly HierarchyNodeType Null => ref s_Null;

        /// <summary>
        /// Unique identification number of the hierarchy node type.
        /// </summary>
        public int Id => m_Id;

        /// <summary>
        /// Create a null node type.
        /// </summary>
        public HierarchyNodeType()
        {
            m_Id = k_HierarchyNodeTypeNull;
        }

        internal HierarchyNodeType(int id)
        {
            m_Id = id;
        }

        [ExcludeFromDocs]
        public static bool operator ==(in HierarchyNodeType lhs, in HierarchyNodeType rhs) => lhs.Id == rhs.Id;
        [ExcludeFromDocs]
        public static bool operator !=(in HierarchyNodeType lhs, in HierarchyNodeType rhs) => !(lhs == rhs);
        [ExcludeFromDocs]
        public bool Equals(HierarchyNodeType other) => other.Id == Id;
        [ExcludeFromDocs]
        public override string ToString() => $"{nameof(HierarchyNodeType)}({(this == Null ? nameof(Null) : Id)})";
        [ExcludeFromDocs]
        public override bool Equals(object obj) => obj is HierarchyNodeType node && Equals(node);
        [ExcludeFromDocs]
        public override int GetHashCode() => Id.GetHashCode();
    }
}
