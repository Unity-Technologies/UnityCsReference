// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct CustomizationPointNode : IEquatable<CustomizationPointNode>, ISyntaxNode<CustomizationPointNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/CustomizationPointNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeHandle extensionsHandle; // ExtensionsNodeHandle
            internal NodeHandle interfaceHandle; // InterfaceNodeHandle
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::CustomizationPointNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is CustomizationPointNode other && this.Equals(other);
        public bool Equals(CustomizationPointNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(CustomizationPointNode lhs, CustomizationPointNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(CustomizationPointNode lhs, CustomizationPointNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        // TODO @ SHADERS SHADERS-455: Implement extensions
        // internal ExtensionsNode Extensions => new ExtensionsNode(syntaxTree, NodeRef.extensionsHandle);
        internal InterfaceNode Interface => new InterfaceNode(syntaxTree, NodeRef.interfaceHandle);
        internal IEnumerable<InterfaceFieldNode> InterfaceFields => Interface.Fields;

        internal CustomizationPointNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<AttributeNode> m_Attributes = new List<AttributeNode>();
            IdentifierNode m_Name;
            // TODO @ SHADERS SHADERS-455: Implement extensions
            //ExtensionsNode m_Extensions;
            InterfaceNode.Builder m_InterfaceBuilder;
            public TextRange SourceRange { get; set; }
            public IEnumerable<AttributeNode> Attributes => m_Attributes;
            public void AddAttribute(AttributeNode node) => m_Attributes.Add(Validate(node));
            public IEnumerable<InterfaceFieldNode> InterfaceFields => m_InterfaceBuilder.Fields;
            public void AddInterfaceField(InterfaceFieldNode node) => m_InterfaceBuilder.Add(Validate(node));
            public IdentifierNode Name
            {
                get => m_Name;
                set => m_Name = Validate(value);
            }
            // TODO @ SHADERS SHADERS-455: Implement extensions
            //public ExtensionsNode Extensions
            //{
            //    get => m_Extensions;
            //    set => m_Extensions = Validate(value);
            //}

            public Builder(SyntaxTree syntaxTree, IdentifierNode name)
                : base(syntaxTree)
            {
                Name = name;
                m_InterfaceBuilder = new InterfaceNode.Builder(syntaxTree);
            }

            public CustomizationPointNode Build()
            {
                var node = new CustomizationPointNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, m_Attributes);
                nodeRef.nameHandle = Name.handle;
                // TODO @ SHADERS SHADERS-455: Implement extensions
                //nodeRef.extensionsHandle = Extensions.handle;
                nodeRef.interfaceHandle = m_InterfaceBuilder.Build().handle;
                return node;
            }
        }
    }
}
