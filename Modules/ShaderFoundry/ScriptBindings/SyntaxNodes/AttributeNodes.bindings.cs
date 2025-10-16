// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct AttributeNode : IEquatable<AttributeNode>, ISyntaxNode<AttributeNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/AttributeNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle nameHandle; // NamespacedIdentifierNodeHandle
            internal NodeList parameterHandles; // List<AttributeParameterNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::AttributeNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is AttributeNode other && this.Equals(other);
        public bool Equals(AttributeNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(AttributeNode lhs, AttributeNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(AttributeNode lhs, AttributeNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal NamespacedIdentifierNode Name => new NamespacedIdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal IEnumerable<AttributeParameterNode> Parameters => syntaxTree.EnumerateSyntaxNodes<AttributeParameterNode>(NodeRef.parameterHandles);

        internal AttributeNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            NamespacedIdentifierNode m_Name;
            List<AttributeParameterNode> m_Parameters = new List<AttributeParameterNode>();
            public TextRange SourceRange { get; set; }
            public NamespacedIdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public IEnumerable<AttributeParameterNode> Parameters => m_Parameters;
            public void AddParameter(params AttributeParameterNode[] parameters)
                => AddParameter(parameters as IEnumerable<AttributeParameterNode>);
            public void AddParameter(IEnumerable<AttributeParameterNode> parameters)
            {
                foreach (var param in parameters)
                    m_Parameters.Add(Validate(param));
            }

            public Builder(SyntaxTree tree, NamespacedIdentifierNode name)
                : base(tree)
            {
                Name = name;
            }

            public AttributeNode Build()
            {
                var node = new AttributeNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                nodeRef.parameterHandles.AddRange(syntaxTree, m_Parameters);
                return node;
            }
        }
    }
}
