// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    // This node is only used internally by the RenderStateNode builders, so it has a minimal implementation with no
    // data validation.
    internal struct RenderStateNamedValueNode : IEquatable<RenderStateNamedValueNode>, ISyntaxNode<RenderStateNamedValueNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/RenderStateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeHandle valueHandle; // GenericHandle
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::RenderStateNamedValueNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is RenderStateNamedValueNode other && this.Equals(other);
        public bool Equals(RenderStateNamedValueNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(RenderStateNamedValueNode lhs, RenderStateNamedValueNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(RenderStateNamedValueNode lhs, RenderStateNamedValueNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        // IntegerLiteralNode, RenderStatePropertyNode, or IdentifierNode
        internal ISyntaxNode Value => syntaxTree.GetSyntaxNode(NodeRef.valueHandle);

        internal RenderStateNamedValueNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        internal class Builder : BaseBuilder
        {
            private IdentifierNode m_Name;
            private ISyntaxNode m_Value;
            public TextRange SourceRange { get; set; }
            public IdentifierNode Name => m_Name;
            public ISyntaxNode Value => m_Value;

            public Builder(SyntaxTree syntaxTree, IdentifierNode name, ISyntaxNode value)
                : base(syntaxTree)
            {
                m_Name = name;
                m_Value = value;
            }

            public RenderStateNamedValueNode Build()
            {
                var node = new RenderStateNamedValueNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                nodeRef.valueHandle = Value.handle;
                return node;
            }
        }
    }
}
