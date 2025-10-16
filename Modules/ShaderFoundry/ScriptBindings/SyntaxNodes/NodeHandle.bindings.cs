// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Enable this here and in Handle.h to enable debugging for nodes.
//#define ENABLE_FOUNDRY_NODE_DEBUGGING
using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Parser/Handle.h")]
    [FoundryAPI]
    internal struct NodeHandle : IEquatable<NodeHandle>
    {
        internal UInt16 m_Offset;
        internal UInt16 m_Page;
        internal NodeType m_NodeType;


        internal extern bool IsValid { [NativeMethod(Name = "IsValid", IsThreadSafe = true)] get; }

        // IEquatable Interface
        public override bool Equals(object obj) => obj is NodeHandle other && this.Equals(other);
        public bool Equals(NodeHandle other) => m_Offset == other.m_Offset && m_Page == other.m_Page && m_NodeType == other.m_NodeType;
        public override int GetHashCode() => (m_Offset, m_Page, m_NodeType).GetHashCode();
        public static bool operator ==(NodeHandle lhs, NodeHandle rhs) => lhs.Equals(rhs);
        public static bool operator !=(NodeHandle lhs, NodeHandle rhs) => !lhs.Equals(rhs);
    }
}
