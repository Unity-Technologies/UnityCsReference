// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct BlockNode : IEquatable<BlockNode>, ISyntaxNode<BlockNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/BlockNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeHandle interfaceHandle; // InterfaceNodeHandle
            internal NodeList contentHandles; // List<GenericHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::BlockNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is BlockNode other && this.Equals(other);
        public bool Equals(BlockNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(BlockNode lhs, BlockNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(BlockNode lhs, BlockNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal InterfaceNode Interface => NodeRef.interfaceHandle.IsValid ? new InterfaceNode(syntaxTree, NodeRef.interfaceHandle) : new InterfaceNode();
        internal IEnumerable<ISyntaxNode> Contents => syntaxTree.EnumerateSyntaxNodes(NodeRef.contentHandles);

        internal BlockNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            IdentifierNode m_Name;
            List<AttributeNode> m_AttributeNodes = new List<AttributeNode>();
            List<ISyntaxNode> m_Contents = new List<ISyntaxNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<AttributeNode> Attributes => m_AttributeNodes;
            public void AddAttribute(AttributeNode node) => m_AttributeNodes.Add(Validate(node));
            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public InterfaceNode Interface { get; set; }
            public DefinesNode DefinesNode { get; set; }
            public IncludesNode IncludesNode { get; set; }
            public KeywordsNode KeywordsNode { get; set; }
            public PackageRequirementsNode PackageRequirementsNode { get; set; }
            public PragmasNode PragmasNode { get; set; }
            public RenderStatesNode RenderStatesNode { get; set; }
            public IEnumerable<ISyntaxNode> Contents => m_Contents;
            void AddInternal(ISyntaxNode node) => m_Contents.Add(Validate(node));
            public void Add(FunctionNode node) => AddInternal(node);
            public void Add(StructNode node) => AddInternal(node);

            public Builder(SyntaxTree tree, IdentifierNode name)
                : base(tree)
            {
                Name = name;
            }

            public BlockNode Build()
            {
                var node = new BlockNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, m_AttributeNodes);
                nodeRef.nameHandle = Name.handle;
                nodeRef.interfaceHandle = Interface.handle;
                nodeRef.contentHandles.TryAdd(syntaxTree, DefinesNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, IncludesNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, KeywordsNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, PackageRequirementsNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, PragmasNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, RenderStatesNode.handle);
                nodeRef.contentHandles.AddRange(syntaxTree, m_Contents);
                return node;
            }
        }
    }
}
