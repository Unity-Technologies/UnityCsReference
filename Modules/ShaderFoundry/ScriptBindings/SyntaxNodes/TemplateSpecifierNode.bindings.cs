// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct TemplateSpecifierNode : IEquatable<TemplateSpecifierNode>, ISyntaxNode<TemplateSpecifierNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TypeNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList argumentHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::TemplateSpecifierNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is TemplateSpecifierNode other && this.Equals(other);
        public bool Equals(TemplateSpecifierNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(TemplateSpecifierNode lhs, TemplateSpecifierNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(TemplateSpecifierNode lhs, TemplateSpecifierNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<TypeNameNode> Arguments => syntaxTree.EnumerateSyntaxNodes<TypeNameNode>(NodeRef.argumentHandles);

        internal TemplateSpecifierNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<TypeNameNode> m_arguments = new List<TypeNameNode>();
            public TextRange SourceRange { get; set; }

            public IEnumerable<TypeNameNode> Arguments => m_arguments;
            public void AddArguments(params TypeNameNode[] arguments) => AddArguments(arguments as IEnumerable<TypeNameNode>);
            public void AddArguments(IEnumerable<TypeNameNode> arguments)
            {
                foreach (var arg in arguments)
                    m_arguments.Add(Validate(arg));
            }

            public Builder(SyntaxTree syntaxTree, TypeNameNode typeName)
                : base(syntaxTree)
            {
                AddArguments(typeName);
            }

            public TemplateSpecifierNode Build()
            {
                var node = new TemplateSpecifierNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.argumentHandles.AddRange(syntaxTree, Arguments);
                return node;
            }
        }
    }
}
