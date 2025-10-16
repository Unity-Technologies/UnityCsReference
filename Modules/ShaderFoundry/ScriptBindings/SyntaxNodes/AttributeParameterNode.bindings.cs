// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct AttributeParameterNode : IEquatable<AttributeParameterNode>, ISyntaxNode<AttributeParameterNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/AttributeNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            // IdentifierNodeHandle
            internal NodeHandle nameHandle;
            // Any Literal, NamespacedIdentifier, ListValue
            internal NodeHandle valueHandle;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::AttributeParameterNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is AttributeParameterNode other && this.Equals(other);
        public bool Equals(AttributeParameterNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(AttributeParameterNode lhs, AttributeParameterNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(AttributeParameterNode lhs, AttributeParameterNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IdentifierNode Name => NodeRef.nameHandle.IsValid ? new IdentifierNode(syntaxTree, NodeRef.nameHandle) : new IdentifierNode();
        internal ISyntaxNode Value => syntaxTree.GetSyntaxNode(NodeRef.valueHandle);

        internal AttributeParameterNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            ISyntaxNode m_Value;
            public TextRange SourceRange { get; set; }
            public IdentifierNode Name { get; set; }
            public ISyntaxNode Value => m_Value;
            public void SetValue(BooleanLiteralNode node) => SetValueInternal(node);
            public void SetValue(IntegerLiteralNode node) => SetValueInternal(node);
            public void SetValue(StringLiteralNode node) => SetValueInternal(node);
            public void SetValue(CharacterLiteralNode node) => SetValueInternal(node);
            public void SetValue(FloatLiteralNode node) => SetValueInternal(node);
            public void SetValue(NamespacedIdentifierNode node) => SetValueInternal(node);
            public void SetValue(AttributeParameterListValueNode node) => SetValueInternal(node);
            void SetValueInternal(ISyntaxNode node) => m_Value = Validate(node);

            Builder(SyntaxTree tree, IdentifierNode name, ISyntaxNode value)
                : base(tree)
            {
                Name = name;
                SetValueInternal(value);
            }
            public Builder(SyntaxTree tree, IdentifierNode name, BooleanLiteralNode value) : this(tree, name, value as ISyntaxNode) { }
            public Builder(SyntaxTree tree, IdentifierNode name, IntegerLiteralNode value) : this(tree, name, value as ISyntaxNode) { }
            public Builder(SyntaxTree tree, IdentifierNode name, StringLiteralNode value) : this(tree, name, value as ISyntaxNode) { }
            public Builder(SyntaxTree tree, IdentifierNode name, CharacterLiteralNode value) : this(tree, name, value as ISyntaxNode) { }
            public Builder(SyntaxTree tree, IdentifierNode name, FloatLiteralNode value) : this(tree, name, value as ISyntaxNode) { }
            public Builder(SyntaxTree tree, IdentifierNode name, NamespacedIdentifierNode value) : this(tree, name, value as ISyntaxNode) { }
            public Builder(SyntaxTree tree, IdentifierNode name, AttributeParameterListValueNode value) : this(tree, name, value as ISyntaxNode) { }

            public AttributeParameterNode Build()
            {
                var node = new AttributeParameterNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                nodeRef.valueHandle = Value.handle;
                return node;
            }
        }
    }
}
