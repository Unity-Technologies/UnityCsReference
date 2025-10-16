// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct TypeNameNode : IEquatable<TypeNameNode>, ISyntaxNode<TypeNameNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TypeNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle nameHandle;
            internal NodeHandle templateSpecifierHandle;
            internal NodeList dimensionHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::TypeNameNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is TypeNameNode other && this.Equals(other);
        public bool Equals(TypeNameNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(TypeNameNode lhs, TypeNameNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(TypeNameNode lhs, TypeNameNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal NamespacedIdentifierNode Name => new NamespacedIdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal TemplateSpecifierNode TemplateSpecifier => NodeRef.templateSpecifierHandle.IsValid ? new TemplateSpecifierNode(syntaxTree, NodeRef.templateSpecifierHandle) : new TemplateSpecifierNode();
        internal IEnumerable<ArrayDimensionNode> Dimensions => syntaxTree.EnumerateSyntaxNodes<ArrayDimensionNode>(NodeRef.dimensionHandles);

        internal TypeNameNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            NamespacedIdentifierNode m_name;
            TemplateSpecifierNode m_templateSpecifier;
            List<ArrayDimensionNode> m_dimensions = new List<ArrayDimensionNode>();
            public NamespacedIdentifierNode Name
            {
                get => m_name;
                set => m_name = Validate(value);
            }
            public TemplateSpecifierNode TemplateSpecifier
            {
                get => m_templateSpecifier;
                set => m_templateSpecifier = value;
            }

            public IEnumerable<ArrayDimensionNode> Dimensions => m_dimensions;
            public void AddDimensions(params ArrayDimensionNode[] dimensions) => AddDimensions(dimensions as IEnumerable<ArrayDimensionNode>);
            public void AddDimensions(IEnumerable<ArrayDimensionNode> dimensions)
            {
                foreach (var dim in dimensions)
                    m_dimensions.Add(Validate(dim));
            }

            public Builder(SyntaxTree syntaxTree, NamespacedIdentifierNode name)
                : base(syntaxTree)
            {
                Name = name;
            }

            public TypeNameNode Build()
            {
                var node = new TypeNameNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                nodeRef.templateSpecifierHandle = TemplateSpecifier.handle;
                nodeRef.dimensionHandles.AddRange(syntaxTree, Dimensions);
                return node;
            }
        }
    }
}
