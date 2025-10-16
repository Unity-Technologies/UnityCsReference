// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct LinkOverridesNode : IEquatable<LinkOverridesNode>, ISyntaxNode<LinkOverridesNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/LinkOverrideNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList linkOverrideHandles; // List<LinkOverrideNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::LinkOverridesNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is LinkOverridesNode other && this.Equals(other);
        public bool Equals(LinkOverridesNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(LinkOverridesNode lhs, LinkOverridesNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(LinkOverridesNode lhs, LinkOverridesNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<LinkOverrideNode> LinkOverrides => syntaxTree.EnumerateSyntaxNodes<LinkOverrideNode>(NodeRef.linkOverrideHandles);

        internal LinkOverridesNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<LinkOverrideNode> m_LinkOverrides = new List<LinkOverrideNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<LinkOverrideNode> LinkOverrides => m_LinkOverrides;
            public void AddLinkOverride(LinkOverrideNode node) => m_LinkOverrides.Add(Validate(node));

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }

            public LinkOverridesNode Build()
            {
                var node = new LinkOverridesNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.linkOverrideHandles.AddRange(syntaxTree, LinkOverrides);
                return node;
            }
        }
    }
}
