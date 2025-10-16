// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct LinkOverrideAccessorNode : IEquatable<LinkOverrideAccessorNode>, ISyntaxNode<LinkOverrideAccessorNode>
    {
        // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN LinkOverrideNodes.h
        internal enum Kind
        {
            MemberAccess,
            ArrayAccess,
        };

        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/LinkOverrideNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            // Identifier or ArrayDimension
            internal NodeHandle accessorHandle;
            internal Kind kind;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::LinkOverrideAccessorNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is LinkOverrideAccessorNode other && this.Equals(other);
        public bool Equals(LinkOverrideAccessorNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(LinkOverrideAccessorNode lhs, LinkOverrideAccessorNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(LinkOverrideAccessorNode lhs, LinkOverrideAccessorNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal ISyntaxNode Accessor => syntaxTree.GetSyntaxNode(NodeRef.accessorHandle);

        internal LinkOverrideAccessorNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            ISyntaxNode m_Accessor;
            Kind m_Kind;
            public TextRange SourceRange { get; set; }
            public ISyntaxNode Accessor => m_Accessor;
            public void SetAccessor(IdentifierNode memberAccessor)
            {
                m_Accessor = Validate(memberAccessor);
                m_Kind = Kind.MemberAccess;
            }
            public void SetAccessor(ArrayDimensionNode arrayAccessor)
            {
                m_Accessor = Validate(arrayAccessor);
                m_Kind = Kind.ArrayAccess;
            }

            public Builder(SyntaxTree syntaxTree, IdentifierNode memberAccessor)
                : base(syntaxTree)
            {
                SetAccessor(memberAccessor);
            }
            public Builder(SyntaxTree syntaxTree, ArrayDimensionNode arrayAccessor)
                : base(syntaxTree)
            {
                SetAccessor(arrayAccessor);
            }

            public LinkOverrideAccessorNode Build()
            {
                var node = new LinkOverrideAccessorNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.accessorHandle = m_Accessor.handle;
                nodeRef.kind = m_Kind;
                return node;
            }
        }
    }
}
