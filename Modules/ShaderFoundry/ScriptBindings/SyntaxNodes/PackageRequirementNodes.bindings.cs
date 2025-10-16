// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct PackageRequirementNode : IEquatable<PackageRequirementNode>, ISyntaxNode<PackageRequirementNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/PackageRequirementsNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle nameHandle;
            internal NodeHandle versionHandle;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::PackageRequirementNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is PackageRequirementNode other && this.Equals(other);
        public bool Equals(PackageRequirementNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(PackageRequirementNode lhs, PackageRequirementNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(PackageRequirementNode lhs, PackageRequirementNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal StringLiteralNode Name => new StringLiteralNode(syntaxTree, NodeRef.nameHandle);
        internal StringLiteralNode Version => NodeRef.versionHandle.IsValid ? new StringLiteralNode(syntaxTree, NodeRef.versionHandle) : new StringLiteralNode();

        internal PackageRequirementNode(SyntaxTree syntaxTree, NodeHandle handle)
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
            public StringLiteralNode Version { get; set; }

            public Builder(SyntaxTree tree, StringLiteralNode name)
                : this(tree, name, new StringLiteralNode())
            {
            }
            public Builder(SyntaxTree tree, StringLiteralNode name, StringLiteralNode version)
                : base(tree)
            {
                Name = name;
                Version = version;
            }
            public PackageRequirementNode Build()
            {
                var node = new PackageRequirementNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                nodeRef.versionHandle = Version.handle;
                return node;
            }
        }
    }

    internal struct PackageRequirementsNode : IEquatable<PackageRequirementsNode>, ISyntaxNode<PackageRequirementsNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/PackageRequirementsNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList contentHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::PackageRequirementsNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is PackageRequirementsNode other && this.Equals(other);
        public bool Equals(PackageRequirementsNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(PackageRequirementsNode lhs, PackageRequirementsNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(PackageRequirementsNode lhs, PackageRequirementsNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<PackageRequirementNode> Contents => syntaxTree.EnumerateSyntaxNodes<PackageRequirementNode>(NodeRef.contentHandles);

        internal PackageRequirementsNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            private List<PackageRequirementNode> m_Contents = new List<PackageRequirementNode>();
            public IEnumerable<PackageRequirementNode> Contents => m_Contents;
            public void Add(params PackageRequirementNode[] nodes) => Add(nodes as IEnumerable<PackageRequirementNode>);
            public void Add(IEnumerable<PackageRequirementNode> nodes)
            {
                foreach (var node in nodes)
                    m_Contents.Add(Validate(node));
            }

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }
            public PackageRequirementsNode Build()
            {
                var node = new PackageRequirementsNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.contentHandles.AddRange(syntaxTree, Contents);
                return node;
            }
        }
    }
}
