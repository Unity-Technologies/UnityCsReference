// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct FunctionNode : IEquatable<FunctionNode>, ISyntaxNode<FunctionNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/FunctionNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle returnTypeNameHandle; // TypeNameNodeHandle
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeList argumentHandles; // List<FunctionParameterNodeHandle>
            internal NodeHandle bodyHandle; // TextNodeHandle
            internal bool isStatic;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::FunctionNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is FunctionNode other && this.Equals(other);
        public bool Equals(FunctionNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(FunctionNode lhs, FunctionNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(FunctionNode lhs, FunctionNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal TypeNameNode ReturnTypeName => new TypeNameNode(syntaxTree, NodeRef.returnTypeNameHandle);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal IEnumerable<FunctionParameterNode> Parameters => syntaxTree.EnumerateSyntaxNodes<FunctionParameterNode>(NodeRef.argumentHandles);
        internal TextNode Body => NodeRef.bodyHandle.IsValid ? new TextNode(syntaxTree, NodeRef.bodyHandle) : new TextNode();
        internal bool IsStatic => NodeRef.isStatic;

        internal FunctionNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<AttributeNode> m_Attributes = new List<AttributeNode>();
            TypeNameNode m_ReturnTypeName;
            IdentifierNode m_Name;
            List<FunctionParameterNode> m_Parameters = new List<FunctionParameterNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<AttributeNode> Attributes => m_Attributes;
            public void AddAttribute(AttributeNode node) => m_Attributes.Add(Validate(node));
            public TypeNameNode ReturnTypeName
            {
                get => m_ReturnTypeName;
                set => m_ReturnTypeName = Validate(value);
            }
            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            public IEnumerable<FunctionParameterNode> Parameters => m_Parameters;
            public void AddParameter(FunctionParameterNode node) => m_Parameters.Add(Validate(node));
            public TextNode Body { get; set; }
            public bool IsStatic { get; set; }

            public Builder(SyntaxTree syntaxTree, TypeNameNode returnTypeName, IdentifierNode name)
                : base(syntaxTree)
            {
                ReturnTypeName = returnTypeName;
                Name = name;
            }

            public FunctionNode Build()
            {
                var node = new FunctionNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, m_Attributes);
                nodeRef.returnTypeNameHandle = m_ReturnTypeName.handle;
                nodeRef.nameHandle = Name.handle;
                nodeRef.argumentHandles.AddRange(syntaxTree, m_Parameters);
                if (Body.IsValid)
                {
                    nodeRef.bodyHandle = Body.handle;
                }
                else
                {
                    // To maintain parity with the result of parsing a text asset, if no function body is provided,
                    // generate an empty TextNode.
                    var bodyBuilder = new TextNode.Builder(syntaxTree, string.Empty);
                    bodyBuilder.SourceRange = SourceRange; // Copy location from function location
                    var bodyNode = bodyBuilder.Build();
                    nodeRef.bodyHandle = bodyNode.handle;
                }
                nodeRef.isStatic = IsStatic;
                return node;
            }
        }
    }
}
