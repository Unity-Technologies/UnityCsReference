// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct FallbackNode : IEquatable<FallbackNode>, ISyntaxNode<FallbackNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TemplateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle nameHandle; // StringLiteralNodeHandle
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::FallbackNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is FallbackNode other && this.Equals(other);
        public bool Equals(FallbackNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(FallbackNode lhs, FallbackNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(FallbackNode lhs, FallbackNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal StringLiteralNode Name => new StringLiteralNode(syntaxTree, NodeRef.nameHandle);

        internal FallbackNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            StringLiteralNode m_Name;
            public TextRange SourceRange { get; set; }
            public StringLiteralNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }

            public Builder(SyntaxTree syntaxTree, StringLiteralNode name)
                : base(syntaxTree)
            {
                Name = name;
            }

            public FallbackNode Build()
            {
                var node = new FallbackNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                return node;
            }
        }
    }
}
