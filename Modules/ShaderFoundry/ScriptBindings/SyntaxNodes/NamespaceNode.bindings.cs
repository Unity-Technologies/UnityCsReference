// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct NamespaceNode : IEquatable<NamespaceNode>, ISyntaxNode<NamespaceNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/NamespaceNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeList contentHandles; // List<GenericHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::NamespaceNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is NamespaceNode other && this.Equals(other);
        public bool Equals(NamespaceNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(NamespaceNode lhs, NamespaceNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(NamespaceNode lhs, NamespaceNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal IEnumerable<ISyntaxNode> Contents => syntaxTree.EnumerateSyntaxNodes(NodeRef.contentHandles);

        internal NamespaceNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            IdentifierNode m_Name;
            List<ISyntaxNode> m_Contents = new List<ISyntaxNode>();
            public TextRange SourceRange { get; set; }
            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public IEnumerable<ISyntaxNode> Contents => m_Contents;
            void AddInternal(ISyntaxNode node) => m_Contents.Add(Validate(node));
            public void Add(BlockNode node) => AddInternal(node);
            public void Add(BlockShaderNode node) => AddInternal(node);
            public void Add(BlockShaderInterfaceNode node) => AddInternal(node);
            public void Add(CustomizationPointNode node) => AddInternal(node);
            public void Add(NamespaceNode node) => AddInternal(node);
            public void Add(TemplateNode node) => AddInternal(node);
            public void Add(ResourceNode node) => AddInternal(node);
            public void Add(RegisterTemplatesWithInterfaceNode node) => AddInternal(node);
            public void Add(CustomAttributeNode node) => AddInternal(node);
            public void Add(BlockSequenceNode node) => AddInternal(node);
            // TODO @ SHADERS SHADERS-378: BlockInterfaceNode
            //public void Add(BlockInterfaceNode node) => AddInternal(node);

            public Builder(SyntaxTree tree, IdentifierNode name)
                : base(tree)
            {
                Name = name;
            }

            public NamespaceNode Build()
            {
                var node = new NamespaceNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                nodeRef.contentHandles.AddRange(syntaxTree, m_Contents);
                return node;
            }
        }
    }
}
