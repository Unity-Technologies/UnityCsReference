// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct LinkOverrideElementExpressionNode : IEquatable<LinkOverrideElementExpressionNode>, ISyntaxNode<LinkOverrideElementExpressionNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/LinkOverrideNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle castHandle; // TypeNameNodeHandle
            internal NodeHandle nameHandle; // NamespacedIdentifierNodeHandle
            internal NodeList accessChainHandles; // List<LinkOverrideAccessorNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::LinkOverrideElementExpressionNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is LinkOverrideElementExpressionNode other && this.Equals(other);
        public bool Equals(LinkOverrideElementExpressionNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(LinkOverrideElementExpressionNode lhs, LinkOverrideElementExpressionNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(LinkOverrideElementExpressionNode lhs, LinkOverrideElementExpressionNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal TypeNameNode Cast => NodeRef.castHandle.IsValid ? new TypeNameNode(syntaxTree, NodeRef.castHandle) : new TypeNameNode();
        internal NamespacedIdentifierNode Name => NodeRef.nameHandle.IsValid ? new NamespacedIdentifierNode(syntaxTree, NodeRef.nameHandle) : new NamespacedIdentifierNode();
        internal IEnumerable<LinkOverrideAccessorNode> AccessChain => syntaxTree.EnumerateSyntaxNodes<LinkOverrideAccessorNode>(NodeRef.accessChainHandles);

        internal LinkOverrideElementExpressionNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            TypeNameNode m_Cast;
            NamespacedIdentifierNode m_Name;
            List<LinkOverrideAccessorNode> m_AccessChain = new List<LinkOverrideAccessorNode>();
            public TextRange SourceRange { get; set; }
            public TypeNameNode Cast
            {
                get => m_Cast;
                set => m_Cast = value;
            }
            public NamespacedIdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public IEnumerable<LinkOverrideAccessorNode> AccessChain => m_AccessChain;
            public void AddAccessor(LinkOverrideAccessorNode node) => m_AccessChain.Add(Validate(node));
            public Builder(SyntaxTree syntaxTree, NamespacedIdentifierNode name)
                : base(syntaxTree)
            {
                Name = name;
            }

            public LinkOverrideElementExpressionNode Build()
            {
                var node = new LinkOverrideElementExpressionNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.castHandle = Cast.handle;
                nodeRef.nameHandle = Name.handle;
                nodeRef.accessChainHandles.AddRange(syntaxTree, m_AccessChain);
                return node;
            }
        }
    }
}
