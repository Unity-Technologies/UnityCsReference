// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct BlockShaderNode : IEquatable<BlockShaderNode>, ISyntaxNode<BlockShaderNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/BlockShaderNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeList implementsHandles; // List<NamespacedIdentifierNodeHandle>
            internal NodeList contentHandles; // List<GenericHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::BlockShaderNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is BlockShaderNode other && this.Equals(other);
        public bool Equals(BlockShaderNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(BlockShaderNode lhs, BlockShaderNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(BlockShaderNode lhs, BlockShaderNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal IEnumerable<NamespacedIdentifierNode> Implements => syntaxTree.EnumerateSyntaxNodes<NamespacedIdentifierNode>(NodeRef.implementsHandles);
        internal IEnumerable<ISyntaxNode> Contents => syntaxTree.EnumerateSyntaxNodes(NodeRef.contentHandles);

        internal BlockShaderNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<AttributeNode> m_Attributes = new List<AttributeNode>();
            IdentifierNode m_Name;
            List<NamespacedIdentifierNode> m_Implements = new List<NamespacedIdentifierNode>();
            List<ISyntaxNode> m_Contents = new List<ISyntaxNode>();
            public TextRange SourceRange { get; set; }
            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }

            public IEnumerable<AttributeNode> Attributes => m_Attributes;
            public void AddAttribute(AttributeNode node) => m_Attributes.Add(Validate(node));
            public IEnumerable<NamespacedIdentifierNode> Implements => m_Implements;
            public void AddImplement(NamespacedIdentifierNode node) => m_Implements.Add(Validate(node));

            public IEnumerable<ISyntaxNode> Contents => m_Contents;
            void AddInternal(ISyntaxNode node) => m_Contents.Add(Validate(node));
            public void AddContent(BlockNode node) => AddInternal(node);
            public void AddContent(BlockSequenceNode node) => AddInternal(node);
            public void AddContent(CustomEditorNode node) => AddInternal(node);
            public void AddContent(CustomizationPointImplementationNode node) => AddInternal(node);
            public void AddContent(NamespaceNode node)
            {
                // We need to validate the node before we check the contents, otherwise we get a null exception.
                Validate(node);
                ValidateNamespaceContents(node);
                m_Contents.Add(node);
            }
            void ValidateNamespaceContents(NamespaceNode node)
            {
                foreach (var subNode in node.Contents)
                {
                    switch (subNode)
                    {
                        case BlockNode blockNode:
                            break;
                        case BlockSequenceNode blockSequenceNode:
                            break;
                        case NamespaceNode namespaceNode:
                            ValidateNamespaceContents(namespaceNode);
                            break;
                        default:
                            var nodeTypeName = subNode.GetType().Name;
                            throw new InvalidOperationException($"Namespace contains an invalid entry of type {nodeTypeName}. " +
                                "BlockShader namespaces can only contain BlockNode and NamespaceNode.");
                    }
                }
            }

            public Builder(SyntaxTree tree, IdentifierNode name, NamespacedIdentifierNode interfaceName)
                : base(tree)
            {
                Name = name;
                AddImplement(interfaceName);
            }

            public BlockShaderNode Build()
            {
                var node = new BlockShaderNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, m_Attributes);
                nodeRef.nameHandle = Name.handle;
                nodeRef.implementsHandles.AddRange(syntaxTree, m_Implements);
                nodeRef.contentHandles.AddRange(syntaxTree, m_Contents);
                return node;
            }
        }
    }
}
