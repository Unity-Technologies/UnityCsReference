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
    /// Represents a flattened hierarchy node.
    /// </summary>
    [NativeType(Header = "Modules/HierarchyCore/Public/HierarchyFlattenedNode.h")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct HierarchyFlattenedNode : IEquatable<HierarchyFlattenedNode>
    {
        static readonly HierarchyFlattenedNode s_Null;
        readonly HierarchyNode m_Node;
        readonly int m_ParentOffset;
        readonly int m_NextSiblingOffset;
        readonly int m_ChildrenCount;
        readonly int m_Depth;

        /// <summary>
        /// Represents a flattened hierarchy node that is null or invalid.
        /// </summary>
        public static ref readonly HierarchyFlattenedNode Null => ref s_Null;

        /// <summary>
        /// The hierarchy node referenced.
        /// </summary>
        public HierarchyNode Node => m_Node;

        /// <summary>
        /// The offset of the parent of the node.
        /// </summary>
        public int ParentOffset => m_ParentOffset;

        /// <summary>
        /// The offset of the next sibling of the node.
        /// </summary>
        public int NextSiblingOffset => m_NextSiblingOffset;

        /// <summary>
        /// The number of children nodes that the node has.
        /// </summary>
        public int ChildrenCount => m_ChildrenCount;

        /// <summary>
        /// The depth of the node.
        /// </summary>
        public int Depth => m_Depth;

        /// <summary>
        /// Creates a flattened node.
        /// </summary>
        public HierarchyFlattenedNode()
        {
            m_Node = HierarchyNode.Null;
            m_ParentOffset = 0;
            m_NextSiblingOffset = 0;
            m_ChildrenCount = 0;
            m_Depth = 0;
        }

        [ExcludeFromDocs]
        public static bool operator ==(in HierarchyFlattenedNode lhs, in HierarchyFlattenedNode rhs) => lhs.Node == rhs.Node;
        [ExcludeFromDocs]
        public static bool operator !=(in HierarchyFlattenedNode lhs, in HierarchyFlattenedNode rhs) => !(lhs == rhs);
        [ExcludeFromDocs]
        public bool Equals(HierarchyFlattenedNode other) => other.Node == Node;
        [ExcludeFromDocs]
        public override string ToString() => $"{nameof(HierarchyFlattenedNode)}({(this == Null ? nameof(Null) : $"{Node.Id}:{Node.Version}")})";
        [ExcludeFromDocs]
        public override bool Equals(object obj) => obj is HierarchyFlattenedNode node && Equals(node);
        [ExcludeFromDocs]
        public override int GetHashCode() => Node.GetHashCode();

        #region Obsolete code to remove in 2024.1
        [Obsolete("ChildCount has been renamed to ChildrenCount. (UnityUpgradable) -> ChildrenCount")]
        public int ChildCount => m_ChildrenCount;
        #endregion
    }
}
