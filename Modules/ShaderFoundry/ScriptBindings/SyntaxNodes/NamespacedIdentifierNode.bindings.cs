// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct NamespacedIdentifierNode : IEquatable<NamespacedIdentifierNode>, ISyntaxNode<NamespacedIdentifierNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/IdentifierNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList namespaceIdentifierHandles;
            internal NodeHandle identifierHandle;
            internal bool isGlobalScoped;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::NamespacedIdentifierNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is NamespacedIdentifierNode other && this.Equals(other);
        public bool Equals(NamespacedIdentifierNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(NamespacedIdentifierNode lhs, NamespacedIdentifierNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(NamespacedIdentifierNode lhs, NamespacedIdentifierNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        // Can't just use syntaxTree.EnumerateSyntaxNodes since we need to skip the valid/null checking of the first element (when IsGlobalScope is true)
        internal IEnumerable<IdentifierNode> NamespaceIdentifiers
        {
            get
            {
                var enumerator = NodeRef.namespaceIdentifierHandles.GetEnumerable(syntaxTree).GetEnumerator();
                // We purposefully skip the first element if IsGlobalScope
                if (IsGlobalScoped)
                    enumerator.MoveNext();
                while (enumerator.MoveNext())
                {
                    yield return new IdentifierNode(syntaxTree, enumerator.Current);
                }
            }
        }
        internal IdentifierNode Identifier => (IdentifierNode)syntaxTree.GetSyntaxNode(NodeRef.identifierHandle);
        internal bool IsGlobalScoped => NodeRef.isGlobalScoped;

        internal NamespacedIdentifierNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }
        public class Builder : BaseBuilder
        {
            IdentifierNode m_identifier;
            List<IdentifierNode> m_namespaceIdentifiers = new List<IdentifierNode>();
            public TextRange SourceRange { get; set; }
            public IdentifierNode Identifier
            {
                get => m_identifier;
                set => m_identifier = Validate(value);
            }

            public IEnumerable<IdentifierNode> NamespaceIdentifiers => m_namespaceIdentifiers;
            public void AddNamespaceIdentifiers(params IdentifierNode[] namespaceIdentifiers) => AddNamespaceIdentifiersInternal(namespaceIdentifiers);
            public void AddNamespaceIdentifiers(IEnumerable<IdentifierNode> namespaceIdentifiers) => AddNamespaceIdentifiersInternal(namespaceIdentifiers);
            void AddNamespaceIdentifiersInternal(IEnumerable<IdentifierNode> namespaceIdentifiers)
            {
                foreach (var ident in namespaceIdentifiers)
                    m_namespaceIdentifiers.Add(Validate(ident));
            }

            public bool IsGlobalScoped { get; set; } = false;
            public Builder(SyntaxTree syntaxTree, IdentifierNode identifier)
                : base(syntaxTree)
            {
                Identifier = identifier;
            }
            public NamespacedIdentifierNode Build()
            {
                var node = new NamespacedIdentifierNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                // Part of the expectation of GlobalScoped namespaced identifiers is that the first entry is invalid. Match that expectation here.
                if (IsGlobalScoped)
                {
                    nodeRef.namespaceIdentifierHandles.Add(syntaxTree, new NodeHandle { m_NodeType = IdentifierNode.InternalNode.kNodeType});
                }
                nodeRef.namespaceIdentifierHandles.AddRange(syntaxTree, NamespaceIdentifiers);
                nodeRef.identifierHandle = Identifier.handle;
                nodeRef.isGlobalScoped = IsGlobalScoped;
                return node;
            }
        }
    }
}
