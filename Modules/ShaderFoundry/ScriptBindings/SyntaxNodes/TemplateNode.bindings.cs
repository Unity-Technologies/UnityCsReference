// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct TemplateNode : IEquatable<TemplateNode>, ISyntaxNode<TemplateNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TemplateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle nameHandle; // IdentifierNode
            internal NodeHandle extensionsHandle; // ExtensionsNodeHandle
            internal NodeList contentHandles; // List<GenericHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::TemplateNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is TemplateNode other && this.Equals(other);
        public bool Equals(TemplateNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(TemplateNode lhs, TemplateNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(TemplateNode lhs, TemplateNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes
            => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        // TODO @ SHADERS SHADERS-455: Add ExtensionsNode bindings
        //internal ExtensionsNodeHandle Extensions => new ExtensionsNode(syntaxTree, NodeRef.extensionsHandle);
        internal IEnumerable<ISyntaxNode> Contents
            => syntaxTree.EnumerateSyntaxNodes(NodeRef.contentHandles);

        internal TemplateNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<AttributeNode> m_Attributes = new List<AttributeNode>();
            IdentifierNode m_Name;
            List<ISyntaxNode> m_Contents = new List<ISyntaxNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<AttributeNode> Attributes => m_Attributes;
            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public IEnumerable<ISyntaxNode> Contents => m_Contents;
            public TagsNode TagsNode { get; set; }
            public PackageRequirementsNode PackageRequirementsNode { get; set; }
            public LodNode Lod { get; set; }
            public RenderStatesNode RenderStatesNode { get; set; }
            public FallbackNode Fallback { get; set; }
            // TODO @ SHADERS SHADERS-496: Add PropertyOrderNode bindings
            //public PropertyOrderNode PropertyOrder { get; set; }
            public void AddAttribute(AttributeNode node) => m_Attributes.Add(Validate(node));
            void AddInternal(ISyntaxNode node) => m_Contents.Add(Validate(node));
            public void Add(CustomizationPointImplementationNode node) => AddInternal(node);
            public void Add(PassNode node) => AddInternal(node);
            public void Add(DependencyNode node) => AddInternal(node);
            // TODO @ SHADERS SHADERS-423: Add CopyRuleNode bindings
            //public void Add(CopyRule node) => AddInternal(node);
            public void Add(NamespaceNode node) => m_Contents.Add(ValidateNamespace(node));
            public void Add(BlockNode node) => AddInternal(node);
            public void Add(CustomizationPointNode node) => AddInternal(node);
            public void Add(CustomAttributeNode node) => AddInternal(node);
            public void Add(BlockSequenceNode node) => AddInternal(node);

            NamespaceNode ValidateNamespace(NamespaceNode node)
            {
                Validate(node);
                foreach (var subNode in node.Contents)
                {
                    switch (subNode)
                    {
                        case NamespaceNode subNamespace:
                            ValidateNamespace(subNamespace);
                            break;
                        case BlockNode:
                            break;
                        case CustomizationPointNode:
                            break;
                        case CustomAttributeNode:
                            break;
                        case BlockSequenceNode:
                            break;
                        default:
                            var typeName = subNode.GetType().Name;
                            var message = $"Namespace contains an invalid entry of type {typeName}. "
                                + "Template namespaces can only contain BlockNode, BlockSequenceNode, CustomizationPointNode, and NamespaceNode.";
                            throw new InvalidOperationException(message);
                    }
                }
                return node;
            }

            public Builder(SyntaxTree syntaxTree, IdentifierNode name)
                : base(syntaxTree)
            {
                Name = name;
            }

            public TemplateNode Build()
            {
                var node = new TemplateNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, Attributes);
                nodeRef.nameHandle = Name.handle;

                // Add contents
                nodeRef.contentHandles.TryAdd(syntaxTree, TagsNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, PackageRequirementsNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, Lod.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, RenderStatesNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, Fallback.handle);
                nodeRef.contentHandles.AddRange(syntaxTree, Contents);

                return node;
            }
        }
    }
}
