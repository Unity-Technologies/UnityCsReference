// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct BlockSequenceElementNode : IEquatable<BlockSequenceElementNode>, ISyntaxNode<BlockSequenceElementNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/BlockSequenceNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle> 
            internal NodeHandle typeNameHandle; // NamespacedIdentifierNodeHandle 
            internal NodeHandle instanceNameHandle; // IdentifierNodeHandle 
            internal NodeHandle linkOverridesHandle; // LinkOverridesNodeHandle 
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::BlockSequenceElementNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is BlockSequenceElementNode other && this.Equals(other);
        public bool Equals(BlockSequenceElementNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(BlockSequenceElementNode lhs, BlockSequenceElementNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(BlockSequenceElementNode lhs, BlockSequenceElementNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal NamespacedIdentifierNode TypeName => new NamespacedIdentifierNode(syntaxTree, NodeRef.typeNameHandle);
        internal IdentifierNode InstanceName
            => NodeRef.instanceNameHandle.IsValid ? new IdentifierNode(syntaxTree, NodeRef.instanceNameHandle) : new IdentifierNode();
        internal LinkOverridesNode LinkOverrides
            => NodeRef.linkOverridesHandle.IsValid ? new LinkOverridesNode(syntaxTree, NodeRef.linkOverridesHandle) : new LinkOverridesNode();

        internal BlockSequenceElementNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<AttributeNode> m_Attributes = new List<AttributeNode>();
            NamespacedIdentifierNode m_TypeName;
            IdentifierNode m_InstanceName;

            public TextRange SourceRange { get; set; }
            public IEnumerable<AttributeNode> Attributes => m_Attributes;
            public void AddAttribute(AttributeNode node) => m_Attributes.Add(Validate(node));
            public NamespacedIdentifierNode TypeName
            {
                get => m_TypeName;
                set => m_TypeName = Validate(value);
            }
            public IdentifierNode InstanceName
            {
                get => m_InstanceName;
                set => m_InstanceName = Validate(value);
            }
            public LinkOverridesNode LinkOverrides { get; set; }

            public Builder(SyntaxTree syntaxTree, NamespacedIdentifierNode typeName, IdentifierNode instanceName)
                : base(syntaxTree)
            {
                TypeName = typeName;
                InstanceName = instanceName;
            }

            public BlockSequenceElementNode Build()
            {
                var node = new BlockSequenceElementNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, m_Attributes);
                nodeRef.typeNameHandle = TypeName.handle;
                nodeRef.instanceNameHandle = InstanceName.handle;
                nodeRef.linkOverridesHandle = LinkOverrides.handle;
                return node;
            }
        }
    }
}
