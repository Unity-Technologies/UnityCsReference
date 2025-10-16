// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct CustomAttributeNode : IEquatable<CustomAttributeNode>, ISyntaxNode<CustomAttributeNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/CustomAttributeNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeList constructorSignatureHandles; // List<ConstructorSignatureNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::CustomAttributeNode>")]
        private static extern void BlittabilityCheck(InternalNode node);
        
        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is CustomAttributeNode other && this.Equals(other);
        public bool Equals(CustomAttributeNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(CustomAttributeNode lhs, CustomAttributeNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(CustomAttributeNode lhs, CustomAttributeNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal IEnumerable<ConstructorSignatureNode> ConstructorSignatures => syntaxTree.EnumerateSyntaxNodes<ConstructorSignatureNode>(NodeRef.constructorSignatureHandles);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
       
        internal CustomAttributeNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }
        
        public class Builder : BaseBuilder
        {
            IdentifierNode m_Name;
            List<AttributeNode> m_AttributeNodes = new List<AttributeNode>();
            List<ConstructorSignatureNode> m_ConstructorSignatureNodes = new List<ConstructorSignatureNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<AttributeNode> Attributes => m_AttributeNodes;
            public void AddAttribute(AttributeNode node) => m_AttributeNodes.Add(Validate(node));
            public IEnumerable<ConstructorSignatureNode> ConstructorSignatures => m_ConstructorSignatureNodes;
            public void AddSignature(ConstructorSignatureNode node) => m_ConstructorSignatureNodes.Add(Validate(node));
            
            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            
            public Builder(SyntaxTree syntaxTree, IdentifierNode name)
            : base(syntaxTree)
            {
                Name = name;
            }
            
            public CustomAttributeNode Build()
            {
                var node = new CustomAttributeNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                nodeRef.attributeHandles.AddRange(syntaxTree, m_AttributeNodes);
                nodeRef.constructorSignatureHandles.AddRange(syntaxTree, m_ConstructorSignatureNodes);
                return node;
            }
        }

    }
}
