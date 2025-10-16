// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct BlockSequenceNode : IEquatable<BlockSequenceNode>, ISyntaxNode<BlockSequenceNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/BlockSequenceNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeHandle interfaceHandle; // InterfaceNodeHandle
            internal NodeList elementHandles; // List<GenericHandle>
        }
        
        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::BlockSequenceNode>")]
        private static extern void BlittabilityCheck(InternalNode node);
        
        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;
        
        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;
        
        // IEquality Interface
        public override bool Equals(object obj) => obj is BlockSequenceNode other && this.Equals(other);
        public bool Equals(BlockSequenceNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(BlockSequenceNode lhs, BlockSequenceNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(BlockSequenceNode lhs, BlockSequenceNode rhs) => !lhs.Equals(rhs);
        
        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        public TextRange SourceRange => NodeRef.sourceRange;
        public IEnumerable<AttributeNode> Attributes => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        public IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        public InterfaceNode Interface => NodeRef.interfaceHandle.IsValid ?
            new InterfaceNode(syntaxTree, NodeRef.interfaceHandle) : new InterfaceNode();
        public IEnumerable<ISyntaxNode> Elements =>
            syntaxTree.EnumerateSyntaxNodes(NodeRef.elementHandles);
        
        internal BlockSequenceNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }
        
        public class Builder : BaseBuilder
        {
            List<AttributeNode> m_Attributes = new List<AttributeNode>();
            IdentifierNode m_Name;
            List<ISyntaxNode> m_Elements = new List<ISyntaxNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<AttributeNode> Attributes => m_Attributes;
            public void AddAttribute(AttributeNode node) => m_Attributes.Add(Validate(node));
            public InterfaceNode Interface { get; set; }
            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public OutputLinkerNode OutputLinker { get; set; }
            
            public IEnumerable<ISyntaxNode> Elements => m_Elements;
            public void Add(BlockSequenceElementNode node) => m_Elements.Add(Validate(node));

            public Builder(SyntaxTree syntaxTree, IdentifierNode name)
                : base(syntaxTree)
            {
                Name = name;
            }

            public BlockSequenceNode Build()
            {
                var node = new BlockSequenceNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, m_Attributes);
                nodeRef.nameHandle = Name.handle;
                nodeRef.interfaceHandle = Interface.handle;
                nodeRef.elementHandles.AddRange(syntaxTree, m_Elements);
                nodeRef.elementHandles.TryAdd(syntaxTree, OutputLinker.handle);
                return node;
            }
        }
    }
}
