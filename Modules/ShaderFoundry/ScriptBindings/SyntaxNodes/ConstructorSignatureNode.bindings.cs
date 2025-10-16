// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct ConstructorSignatureNode : IEquatable<ConstructorSignatureNode>, ISyntaxNode<ConstructorSignatureNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/ConstructorSignatureNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeList argumentHandles; // List<ConstructorSignatureParameterNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::ConstructorSignatureNode>")]
        private static extern void BlittabilityCheck(InternalNode node);
        
        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is ConstructorSignatureNode other && this.Equals(other);
        public bool Equals(ConstructorSignatureNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(ConstructorSignatureNode lhs, ConstructorSignatureNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(ConstructorSignatureNode lhs, ConstructorSignatureNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal IEnumerable<ConstructorSignatureParameterNode> Parameters => syntaxTree.EnumerateSyntaxNodes<ConstructorSignatureParameterNode>(NodeRef.argumentHandles);
        
        internal ConstructorSignatureNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            IdentifierNode m_Name;
            List<ConstructorSignatureParameterNode> m_Parameters = new List<ConstructorSignatureParameterNode>();
            public TextRange SourceRange { get; set; }

            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public IEnumerable<ConstructorSignatureParameterNode> Parameters => m_Parameters;
            public void AddParameter(ConstructorSignatureParameterNode node) => m_Parameters.Add(Validate(node));

            public Builder(SyntaxTree syntaxTree, IdentifierNode name)
                : base(syntaxTree)
            {
                Name = name;
            }

            public ConstructorSignatureNode Build()
            {
                var node = new ConstructorSignatureNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.nameHandle = Name.handle;
                nodeRef.argumentHandles.AddRange(syntaxTree, m_Parameters);
                return node;
            }
        }
    }
}
