// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct RegisterTemplateStatementNode : IEquatable<RegisterTemplateStatementNode>, ISyntaxNode<RegisterTemplateStatementNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/RegisterTemplatesWithInterfaceNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle nameHandle; // NamespacedIdentifierNodeHandle
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::RegisterTemplateStatementNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is RegisterTemplateStatementNode other && this.Equals(other);
        public bool Equals(RegisterTemplateStatementNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(RegisterTemplateStatementNode lhs, RegisterTemplateStatementNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(RegisterTemplateStatementNode lhs, RegisterTemplateStatementNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes =>
            syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal NamespacedIdentifierNode Name => new NamespacedIdentifierNode(syntaxTree, NodeRef.nameHandle);

        internal RegisterTemplateStatementNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<AttributeNode> m_Attributes = new List<AttributeNode>();
            NamespacedIdentifierNode m_Name;
            public TextRange SourceRange { get; set; }
            public NamespacedIdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public IEnumerable<AttributeNode> Attributes => m_Attributes;
            public void AddAttribute(AttributeNode node) => m_Attributes.Add(Validate(node));

            public Builder(SyntaxTree syntaxTree, NamespacedIdentifierNode name)
                : base(syntaxTree)
            {
                Name = name;
            }

            public RegisterTemplateStatementNode Build()
            {
                var node = new RegisterTemplateStatementNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, Attributes);
                nodeRef.nameHandle = Name.handle;

                return node;
            }
        }
    }
}
