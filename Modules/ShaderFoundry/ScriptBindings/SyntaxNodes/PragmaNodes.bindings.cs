// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct PragmaNode : IEquatable<PragmaNode>, ISyntaxNode<PragmaNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/PragmaNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle identifierHandle;
            // Identifier or Literal
            internal NodeList argumentHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::PragmaNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is PragmaNode other && this.Equals(other);
        public bool Equals(PragmaNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(PragmaNode lhs, PragmaNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(PragmaNode lhs, PragmaNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IdentifierNode Identifier => new IdentifierNode(syntaxTree, NodeRef.identifierHandle);
        internal IEnumerable<ISyntaxNode> Arguments => syntaxTree.EnumerateSyntaxNodes(NodeRef.argumentHandles);

        internal PragmaNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            IdentifierNode m_Identifier;
            List<ISyntaxNode> m_Arguments = new List<ISyntaxNode>();
            public TextRange SourceRange { get; set; }
            public IdentifierNode Identifier
            {
                get => m_Identifier;
                set => m_Identifier = Validate(value);
            }
            public IEnumerable<ISyntaxNode> Arguments => m_Arguments;
            public void AddArgument(IdentifierNode node) => AddArgumentInternal(node);
            public void AddArgument(BooleanLiteralNode node) => AddArgumentInternal(node);
            public void AddArgument(IntegerLiteralNode node) => AddArgumentInternal(node);
            public void AddArgument(StringLiteralNode node) => AddArgumentInternal(node);
            public void AddArgument(CharacterLiteralNode node) => AddArgumentInternal(node);
            public void AddArgument(FloatLiteralNode node) => AddArgumentInternal(node);
            void AddArgumentInternal(ISyntaxNode node) => m_Arguments.Add(Validate(node));

            public Builder(SyntaxTree tree, IdentifierNode identifier)
                : base(tree)
            {
                Identifier = identifier;
            }
            public PragmaNode Build()
            {
                var node = new PragmaNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.identifierHandle = Identifier.handle;
                if (m_Arguments != null)
                    nodeRef.argumentHandles.AddRange(syntaxTree, m_Arguments);
                return node;
            }
        }
    }

    internal struct PragmasNode : IEquatable<PragmasNode>, ISyntaxNode<PragmasNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/PragmaNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList statementHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::PragmasNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is PragmasNode other && this.Equals(other);
        public bool Equals(PragmasNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(PragmasNode lhs, PragmasNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(PragmasNode lhs, PragmasNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<ISyntaxNode> Contents => syntaxTree.EnumerateSyntaxNodes<ISyntaxNode>(NodeRef.statementHandles);

        internal PragmasNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            private List<PragmaNode> m_Contents = new List<PragmaNode>();

            public IEnumerable<PragmaNode> Contents => m_Contents;
            public void Add(params PragmaNode[] nodes) => Add(nodes as IEnumerable<PragmaNode>);
            public void Add(IEnumerable<PragmaNode> nodes)
            {
                foreach (var node in nodes)
                    m_Contents.Add(Validate(node));
            }

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }
            public PragmasNode Build()
            {
                var node = new PragmasNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.statementHandles.AddRange(syntaxTree, Contents);
                return node;
            }
        }
    }
}
