// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct ImportNode : IEquatable<ImportNode>, ISyntaxNode<ImportNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/ImportNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal UInt32 textIndex;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::ImportNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is ImportNode other && this.Equals(other);
        public bool Equals(ImportNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(ImportNode lhs, ImportNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(ImportNode lhs, ImportNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal string Text => syntaxTree.GetString(NodeRef.textIndex);

        internal ImportNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            string m_Text;
            public TextRange SourceRange { get; set; }
            public string Text
            {
                get => m_Text;
                set { m_Text = ValidateString(value, "ImportNode.Builder.Text"); }
            }

            public Builder(SyntaxTree syntaxTree, string value)
                : base(syntaxTree)
            {
                Text = value;
            }
            public ImportNode Build()
            {
                var node = new ImportNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.textIndex = node.syntaxTree.AddString(Text);
                return node;
            }
        }
    }

    internal struct ImportsNode : IEquatable<ImportsNode>, ISyntaxNode<ImportsNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/ImportNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList contentHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::ImportsNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is ImportsNode other && this.Equals(other);
        public bool Equals(ImportsNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(ImportsNode lhs, ImportsNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(ImportsNode lhs, ImportsNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<ImportNode> Contents => syntaxTree.EnumerateSyntaxNodes<ImportNode>(NodeRef.contentHandles);

        internal ImportsNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            private List<ImportNode> m_Contents = new List<ImportNode>();

            public IEnumerable<ImportNode> Contents => m_Contents;
            public void Add(params ImportNode[] nodes) => Add(nodes as IEnumerable<ImportNode>);
            public void Add(IEnumerable<ImportNode> nodes)
            {
                foreach (var node in nodes)
                    m_Contents.Add(Validate(node));
            }

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }
            public ImportsNode Build()
            {
                var node = new ImportsNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.contentHandles.AddRange(syntaxTree, Contents);
                return node;
            }
        }
    }
}
