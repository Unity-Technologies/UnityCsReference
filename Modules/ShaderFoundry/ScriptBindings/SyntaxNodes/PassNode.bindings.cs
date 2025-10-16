// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct PassNode : IEquatable<PassNode>, ISyntaxNode<PassNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TemplateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle nameHandle; // StringLiteralNodeHandle
            internal NodeList contentHandles; // List<GenericHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::PassNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is PassNode other && this.Equals(other);
        public bool Equals(PassNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(PassNode lhs, PassNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(PassNode lhs, PassNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal StringLiteralNode Name => new StringLiteralNode(syntaxTree, NodeRef.nameHandle);
        internal IEnumerable<ISyntaxNode> Contents => syntaxTree.EnumerateSyntaxNodes(NodeRef.contentHandles);

        internal PassNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            StringLiteralNode m_Name;
            List<PassStageNode> m_Stages = new List<PassStageNode>();
            public TextRange SourceRange { get; set; }
            public TagsNode TagsNode { get; set; }
            public PragmasNode PragmasNode { get; set; }
            public PackageRequirementsNode PackageRequirementsNode { get; set; }
            public RenderStatesNode RenderStatesNode { get; set; }
            public StringLiteralNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public IEnumerable<PassStageNode> Stages => m_Stages;
            public void AddStage(PassStageNode stage) => m_Stages.Add(Validate(stage));

            public Builder(SyntaxTree syntaxTree, StringLiteralNode name)
                : base(syntaxTree)
            {
                Name = name;
            }

            public PassNode Build()
            {
                var node = new PassNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;

                // Add contents
                nodeRef.contentHandles.TryAdd(syntaxTree, TagsNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, PragmasNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, PackageRequirementsNode.handle);
                nodeRef.contentHandles.TryAdd(syntaxTree, RenderStatesNode.handle);
                nodeRef.contentHandles.AddRange(syntaxTree, Stages);

                return node;
            }
        }
    }
}
