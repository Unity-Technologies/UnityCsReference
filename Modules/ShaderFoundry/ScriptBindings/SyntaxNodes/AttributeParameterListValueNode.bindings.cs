// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct AttributeParameterListValueNode : IEquatable<AttributeParameterListValueNode>, ISyntaxNode<AttributeParameterListValueNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/AttributeNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            // List of: attributeParameterList | namespacedIdentifier | Integer | OtherNumber | Character | String | BooleanLiteral
            internal NodeList elementHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::AttributeParameterListValueNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is AttributeParameterListValueNode other && this.Equals(other);
        public bool Equals(AttributeParameterListValueNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(AttributeParameterListValueNode lhs, AttributeParameterListValueNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(AttributeParameterListValueNode lhs, AttributeParameterListValueNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<ISyntaxNode> Elements => syntaxTree.EnumerateSyntaxNodes(NodeRef.elementHandles);

        internal AttributeParameterListValueNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<ISyntaxNode> m_Elements = new List<ISyntaxNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<ISyntaxNode> Elements => m_Elements;
            public void Add(AttributeParameterListValueNode node) => AddInternal(node);
            public void Add(BooleanLiteralNode node) => AddInternal(node);
            public void Add(CharacterLiteralNode node) => AddInternal(node);
            public void Add(NamespacedIdentifierNode node) => AddInternal(node);
            public void Add(FloatLiteralNode node) => AddInternal(node);
            public void Add(IntegerLiteralNode node) => AddInternal(node);
            public void Add(StringLiteralNode node) => AddInternal(node);
            void AddInternal(ISyntaxNode node) => m_Elements.Add(Validate(node));

            Builder(SyntaxTree tree, ISyntaxNode node) : base(tree)
            {
                AddInternal(node);
            }
            public Builder(SyntaxTree tree, AttributeParameterListValueNode node) : this(tree, node as ISyntaxNode) { }
            public Builder(SyntaxTree tree, BooleanLiteralNode node) : this(tree, node as ISyntaxNode) { }
            public Builder(SyntaxTree tree, CharacterLiteralNode node) : this(tree, node as ISyntaxNode) { }
            public Builder(SyntaxTree tree, NamespacedIdentifierNode node) : this(tree, node as ISyntaxNode) { }
            public Builder(SyntaxTree tree, FloatLiteralNode node) : this(tree, node as ISyntaxNode) { }
            public Builder(SyntaxTree tree, IntegerLiteralNode node) : this(tree, node as ISyntaxNode) { }
            public Builder(SyntaxTree tree, StringLiteralNode node) : this(tree, node as ISyntaxNode) { }

            public AttributeParameterListValueNode Build()
            {
                var node = new AttributeParameterListValueNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.elementHandles.AddRange(syntaxTree, m_Elements);
                return node;
            }
        }
    }
}
