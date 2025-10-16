// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct RenderStateTargetSpecifierNode : IEquatable<RenderStateTargetSpecifierNode>, ISyntaxNode<RenderStateTargetSpecifierNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/RenderStateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle targetIndexHandle; // GenericHandle
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::RenderStateTargetSpecifierNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is RenderStateTargetSpecifierNode other && this.Equals(other);
        public bool Equals(RenderStateTargetSpecifierNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(RenderStateTargetSpecifierNode lhs, RenderStateTargetSpecifierNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(RenderStateTargetSpecifierNode lhs, RenderStateTargetSpecifierNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal bool SpecifiesIndex => NodeRef.targetIndexHandle.m_NodeType == NodeType.IntegerLiteral;
        internal bool SpecifiesAll => !SpecifiesIndex;
        internal IntegerLiteralNode TargetIndex =>
            SpecifiesIndex ? new IntegerLiteralNode(syntaxTree, NodeRef.targetIndexHandle) : new IntegerLiteralNode();

        internal RenderStateTargetSpecifierNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            IntegerLiteralNode m_TargetIndex;
            public TextRange SourceRange { get; set; }
            // An invalid target index indicates that the node specifies all render targets
            public IntegerLiteralNode TargetIndex
            {
                get => m_TargetIndex;
                set => m_TargetIndex = ValidateTargetIndex(value);
            }

            IntegerLiteralNode ValidateTargetIndex(IntegerLiteralNode node)
            {
                if (node.IsValid)
                {
                    bool indexIsValid;
                    if (node.IsSigned)
                        indexIsValid = 0 <= node.SignedValue && node.SignedValue <= 7;
                    else
                        indexIsValid = node.UnsignedValue <= 7;
                    if (!indexIsValid)
                        throw new InvalidOperationException("Render target index must be in the range [0-7].");
                }
                return node;
            }

            // If a target index is not provided, the node specifies all render targets
            public Builder(SyntaxTree syntaxTree, IntegerLiteralNode targetIndex = new IntegerLiteralNode())
                : base(syntaxTree)
            {
                TargetIndex = targetIndex;
            }

            IdentifierNode CreateAllTargetsKeyword()
            {
                var builder = new IdentifierNode.Builder(syntaxTree, "all");
                builder.SourceRange = SourceRange;
                return builder.Build();
            }

            public RenderStateTargetSpecifierNode Build()
            {
                var node = new RenderStateTargetSpecifierNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                if (TargetIndex.IsValid)
                    nodeRef.targetIndexHandle = TargetIndex.handle;
                else
                    nodeRef.targetIndexHandle = CreateAllTargetsKeyword().handle;
                return node;
            }
        }
    }
}
