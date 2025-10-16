// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct ResourceNode : IEquatable<ResourceNode>, ISyntaxNode<ResourceNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/ResourceNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeHandle includesHandle; // IncludesNodeHandle
            internal NodeList functionHandles; // List<FunctionNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::ResourceNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is ResourceNode other && this.Equals(other);
        public bool Equals(ResourceNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(ResourceNode lhs, ResourceNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(ResourceNode lhs, ResourceNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes =>
            syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal IncludesNode IncludesNode => NodeRef.includesHandle.IsValid ?
            new IncludesNode(syntaxTree, NodeRef.includesHandle) : new IncludesNode();
        internal IEnumerable<FunctionNode> Functions =>
            syntaxTree.EnumerateSyntaxNodes<FunctionNode>(NodeRef.functionHandles);

        internal ResourceNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<AttributeNode> m_Attributes = new List<AttributeNode>();
            IdentifierNode m_Name;
            List<FunctionNode> m_Functions = new List<FunctionNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<AttributeNode> Attributes => m_Attributes;
            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public IncludesNode IncludesNode { get; set; }
            public IEnumerable<FunctionNode> Functions => m_Functions;
            public void AddAttribute(AttributeNode node) => m_Attributes.Add(Validate(node));
            public void AddFunction(FunctionNode node) => m_Functions.Add(Validate(node));

            public Builder(SyntaxTree syntaxTree, IdentifierNode name)
                : base(syntaxTree)
            {
                Name = name;
            }

            public ResourceNode Build()
            {
                var node = new ResourceNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, Attributes);
                nodeRef.nameHandle = Name.handle;
                nodeRef.includesHandle = IncludesNode.handle;
                nodeRef.functionHandles.AddRange(syntaxTree, Functions);

                return node;
            }
        }
    }
}
