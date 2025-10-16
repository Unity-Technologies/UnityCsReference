// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct LinkOverrideElementKeywordNode : IEquatable<LinkOverrideElementKeywordNode>, ISyntaxNode<LinkOverrideElementKeywordNode>
    {
        // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN LinkOverrideNodes.h
        public enum Keywords
        {
            Default,
        };
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/LinkOverrideNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal Keywords keyword;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::LinkOverrideElementKeywordNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is LinkOverrideElementKeywordNode other && this.Equals(other);
        public bool Equals(LinkOverrideElementKeywordNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(LinkOverrideElementKeywordNode lhs, LinkOverrideElementKeywordNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(LinkOverrideElementKeywordNode lhs, LinkOverrideElementKeywordNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal Keywords Keyword => NodeRef.keyword;

        internal LinkOverrideElementKeywordNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            public Keywords Keyword { get; set; }
            public Builder(SyntaxTree syntaxTree, Keywords keyword)
                : base(syntaxTree)
            {
                Keyword = keyword;
            }

            public LinkOverrideElementKeywordNode Build()
            {
                var node = new LinkOverrideElementKeywordNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.keyword = Keyword;
                return node;
            }
        }
    }
}
