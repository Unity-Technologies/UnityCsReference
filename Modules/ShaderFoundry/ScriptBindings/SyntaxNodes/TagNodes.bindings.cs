// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct TagNode : IEquatable<TagNode>, ISyntaxNode<TagNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TagNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle nameHandle;
            internal NodeHandle valueHandle;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::TagNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is TagNode other && this.Equals(other);
        public bool Equals(TagNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(TagNode lhs, TagNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(TagNode lhs, TagNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal StringLiteralNode Name => new StringLiteralNode(syntaxTree, NodeRef.nameHandle);
        internal StringLiteralNode Value => new StringLiteralNode(syntaxTree, NodeRef.valueHandle);

        internal TagNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            StringLiteralNode m_Name;
            StringLiteralNode m_Value;
            public TextRange SourceRange { get; set; }
            public StringLiteralNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public StringLiteralNode Value
            {
                get => m_Value;
                set => m_Value = Validate(value);
            }

            public Builder(SyntaxTree tree, StringLiteralNode name, StringLiteralNode value)
                : base(tree)
            {
                Name = name;
                Value = value;
            }

            public TagNode Build()
            {
                var node = new TagNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                nodeRef.valueHandle = Value.handle;
                return node;
            }
        }
    }

    internal struct TagsNode : IEquatable<TagsNode>, ISyntaxNode<TagsNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TagNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList tagHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::TagsNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is TagsNode other && this.Equals(other);
        public bool Equals(TagsNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(TagsNode lhs, TagsNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(TagsNode lhs, TagsNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<TagNode> Tags => syntaxTree.EnumerateSyntaxNodes<TagNode>(NodeRef.tagHandles);

        internal TagsNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            List<TagNode> m_Tags = new List<TagNode>();
            public IEnumerable<TagNode> Tags => m_Tags;
            public void Add(params TagNode[] tagNodes) => Add(tagNodes as IEnumerable<TagNode>);
            public void Add(IEnumerable<TagNode> tagNodes)
            {
                foreach (var def in tagNodes)
                    m_Tags.Add(Validate(def));
            }

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }

            public TagsNode Build()
            {
                var node = new TagsNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.tagHandles.AddRange(syntaxTree, Tags);
                return node;
            }
        }
    }
}
