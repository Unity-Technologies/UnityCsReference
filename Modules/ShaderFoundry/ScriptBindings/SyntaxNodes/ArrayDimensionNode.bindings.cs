// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct ArrayDimensionNode : IEquatable<ArrayDimensionNode>, ISyntaxNode<ArrayDimensionNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TypeNodes.h")]
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
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::ArrayDimensionNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is ArrayDimensionNode other && this.Equals(other);
        public bool Equals(ArrayDimensionNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(ArrayDimensionNode lhs, ArrayDimensionNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(ArrayDimensionNode lhs, ArrayDimensionNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal UInt64 Value => NodeRef.value;

        internal ArrayDimensionNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            public UInt64 Value { get; set; }
            public Builder(SyntaxTree syntaxTree, UInt64 value)
                : base(syntaxTree)
            {
                Value = value;
            }

            public ArrayDimensionNode Build()
            {
                var node = new ArrayDimensionNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.isSigned = false;
                nodeRef.value = Value;
                return node;
            }
        }
    }
}
