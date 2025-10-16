// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct IntegerLiteralNode : IEquatable<IntegerLiteralNode>, ISyntaxNode<IntegerLiteralNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/LiteralNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal UInt64 value;
            internal bool isSigned;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::IntegerLiteralNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is IntegerLiteralNode other && this.Equals(other);
        public bool Equals(IntegerLiteralNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(IntegerLiteralNode lhs, IntegerLiteralNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(IntegerLiteralNode lhs, IntegerLiteralNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        // TODO @ SHADERS: It might be worth revisiting how we handle signed/unsigned safety within the API. For now, IsSigned should be checked for values.
        internal UInt64 UnsignedValue => NodeRef.value;
        internal Int64 SignedValue => (Int64)NodeRef.value;
        internal bool IsSigned => NodeRef.isSigned;
        internal TextRange SourceRange => NodeRef.sourceRange;

        internal IntegerLiteralNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            UInt64? m_unsignedValue;
            Int64? m_signedValue;
            public UInt64? UnsignedValue
            {
                get => m_unsignedValue;
                set
                {
                    m_unsignedValue = value;
                    m_signedValue = null;
                }
            }

            public Int64? SignedValue
            {
                get => m_signedValue;
                set
                {
                    m_signedValue = value;
                    m_unsignedValue = null;
                }
            }

            public Builder(SyntaxTree syntaxTree, UInt64 value)
                : base(syntaxTree)
            {
                UnsignedValue = value;
            }
            public Builder(SyntaxTree syntaxTree, Int64 value)
                : base(syntaxTree)
            {
                SignedValue = value;
            }
            public IntegerLiteralNode Build()
            {
                var node = new IntegerLiteralNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                if(m_unsignedValue.HasValue)
                {
                    nodeRef.value = m_unsignedValue.Value;
                    nodeRef.isSigned = false;
                }
                else if(m_signedValue.HasValue)
                {
                    nodeRef.value = (UInt64)m_signedValue.Value;
                    // Only consider the value signed if it's negative. This is to maintain parity with the parser's
                    // handling of signed values.
                    nodeRef.isSigned = m_signedValue.Value < 0;
                }
                else
                {
                    throw new InvalidOperationException("Cannot build an IntegerLiteralNode with no integer values");
                }
                return node;
            }
        }
    }
}
