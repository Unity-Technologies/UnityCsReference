// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct OutputLinkerNode : IEquatable<OutputLinkerNode>, ISyntaxNode<OutputLinkerNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/OutputLinkerNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle linkOverridesHandle; // LinkOverridesNodeHandle 
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::OutputLinkerNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is OutputLinkerNode other && this.Equals(other);
        public bool Equals(OutputLinkerNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(OutputLinkerNode lhs, OutputLinkerNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(OutputLinkerNode lhs, OutputLinkerNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal LinkOverridesNode LinkOverrides
            => NodeRef.linkOverridesHandle.IsValid ? new LinkOverridesNode(syntaxTree, NodeRef.linkOverridesHandle) : new LinkOverridesNode();

        internal OutputLinkerNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            public LinkOverridesNode LinkOverrides { get; set; }

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }

            public OutputLinkerNode Build()
            {
                var node = new OutputLinkerNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.linkOverridesHandle = LinkOverrides.handle;
                return node;
            }
        }
    }
}
