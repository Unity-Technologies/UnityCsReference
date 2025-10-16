// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct RenderStatePropertyNode : IEquatable<RenderStatePropertyNode>, ISyntaxNode<RenderStatePropertyNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/RenderStateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle propertyNameHandle; // IdentifierNodeHandle
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::RenderStatePropertyNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is RenderStatePropertyNode other && this.Equals(other);
        public bool Equals(RenderStatePropertyNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(RenderStatePropertyNode lhs, RenderStatePropertyNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(RenderStatePropertyNode lhs, RenderStatePropertyNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IdentifierNode PropertyName => new IdentifierNode(syntaxTree, NodeRef.propertyNameHandle);

        internal RenderStatePropertyNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            IdentifierNode m_PropertyName;
            public TextRange SourceRange { get; set; }
            public IdentifierNode PropertyName
            {
                get => m_PropertyName;
                set => m_PropertyName = Validate(value);
            }

            public Builder(SyntaxTree syntaxTree, IdentifierNode propertyName)
                : base(syntaxTree)
            {
                PropertyName = propertyName;
            }

            public RenderStatePropertyNode Build()
            {
                var node = new RenderStatePropertyNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.propertyNameHandle = PropertyName.handle;
                return node;
            }
        }
    }
}
