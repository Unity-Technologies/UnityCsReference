// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct IncludeNode : IEquatable<IncludeNode>, ISyntaxNode<IncludeNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/IncludeNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal UInt32 textIndex;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::IncludeNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is IncludeNode other && this.Equals(other);
        public bool Equals(IncludeNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(IncludeNode lhs, IncludeNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(IncludeNode lhs, IncludeNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal string Text => syntaxTree.GetString(NodeRef.textIndex);

        internal IncludeNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            private string m_text;
            public string Text
            {
                get => m_text;
                set { m_text = ValidateString(value, "IncludeNode.Builder.Text"); }
            }
            public Builder(SyntaxTree syntaxTree, string value)
                : base(syntaxTree)
            {
                Text = value;
            }
            public IncludeNode Build()
            {
                var node = new IncludeNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.textIndex = node.syntaxTree.AddString(Text);
                return node;
            }
        }
    }

    internal struct IncludesNode : IEquatable<IncludesNode>, ISyntaxNode<IncludesNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/IncludeNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList contentHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::IncludesNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is IncludesNode other && this.Equals(other);
        public bool Equals(IncludesNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(IncludesNode lhs, IncludesNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(IncludesNode lhs, IncludesNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<IncludeNode> Contents => syntaxTree.EnumerateSyntaxNodes<IncludeNode>(NodeRef.contentHandles);

        internal IncludesNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            private List<IncludeNode> m_contents = new List<IncludeNode>();

            public IEnumerable<IncludeNode> Contents => m_contents;
            public void AddContents(params IncludeNode[] contents) => AddContents(contents as IEnumerable<IncludeNode>);
            public void AddContents(IEnumerable<IncludeNode> contents)
            {
                foreach (var inc in contents)
                    m_contents.Add(Validate(inc));
            }

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }
            public IncludesNode Build()
            {
                var node = new IncludesNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.contentHandles.AddRange(syntaxTree, Contents);
                return node;
            }
        }
    }
}
