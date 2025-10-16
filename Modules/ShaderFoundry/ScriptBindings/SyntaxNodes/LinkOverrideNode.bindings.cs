// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct LinkOverrideNode : IEquatable<LinkOverrideNode>, ISyntaxNode<LinkOverrideNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/LinkOverrideNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle interfaceFieldHandle; // LinkOverrideElementExpressionNode
            // LinkOverrideElementExpressionNode, LinkOverrideElementKeywordNode, LinkOverriderArgumentConstantExpressionNode
            internal NodeHandle argumentHandle;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::LinkOverrideNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is LinkOverrideNode other && this.Equals(other);
        public bool Equals(LinkOverrideNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(LinkOverrideNode lhs, LinkOverrideNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(LinkOverrideNode lhs, LinkOverrideNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal LinkOverrideElementExpressionNode InterfaceField => new LinkOverrideElementExpressionNode(syntaxTree, NodeRef.interfaceFieldHandle);
        internal ISyntaxNode Argument => syntaxTree.GetSyntaxNode(NodeRef.argumentHandle);

        internal LinkOverrideNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            LinkOverrideElementExpressionNode m_InterfaceField;
            ISyntaxNode m_Argument;
            public TextRange SourceRange { get; set; }
            public LinkOverrideElementExpressionNode InterfaceField
            {
                get => m_InterfaceField;
                set => m_InterfaceField = Validate(value);
            }
            public ISyntaxNode Argument => m_Argument;
            public void SetArgument(LinkOverrideElementExpressionNode node) => SetArgumentInternal(node);
            public void SetArgument(LinkOverrideElementKeywordNode node) => SetArgumentInternal(node);
            void SetArgumentInternal(ISyntaxNode node) => m_Argument = Validate(node);

            public Builder(SyntaxTree syntaxTree, LinkOverrideElementExpressionNode interfaceField, LinkOverrideElementExpressionNode argument)
                : base(syntaxTree)
            {
                InterfaceField = interfaceField;
                SetArgument(argument);
            }
            public Builder(SyntaxTree syntaxTree, LinkOverrideElementExpressionNode interfaceField, LinkOverrideElementKeywordNode argument)
               : base(syntaxTree)
            {
                InterfaceField = interfaceField;
                SetArgument(argument);
            }

            public LinkOverrideNode Build()
            {
                var node = new LinkOverrideNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.interfaceFieldHandle = InterfaceField.handle;
                nodeRef.argumentHandle = Argument.handle;
                return node;
            }
        }
    }
}
