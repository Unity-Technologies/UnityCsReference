// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct InterfaceFieldNode : IEquatable<InterfaceFieldNode>, ISyntaxNode<InterfaceFieldNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/InterfaceNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle typeNameHandle; // TypeNameNodeHandle
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeHandle initializerHandle; // TextNodeHandle
            // This padding is added to make sure flags aligns with the next word boundary
            short padding0;
            internal InoutModifierFlags flags;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::InterfaceFieldNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is InterfaceFieldNode other && this.Equals(other);
        public bool Equals(InterfaceFieldNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(InterfaceFieldNode lhs, InterfaceFieldNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(InterfaceFieldNode lhs, InterfaceFieldNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal TypeNameNode TypeName => new TypeNameNode(syntaxTree, NodeRef.typeNameHandle);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal bool IsInput => NodeRef.flags.HasFlag(InoutModifierFlags.In);
        internal bool IsOutput => NodeRef.flags.HasFlag(InoutModifierFlags.Out);

        internal InterfaceFieldNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseVariableBuilder
        {
            InoutModifierFlags m_Flags = InoutModifierFlags.In;

            public bool IsInput
            {
                get => m_Flags.HasFlag(InoutModifierFlags.In);
                set => InoutModifierFlagsUtils.Set(ref m_Flags, InoutModifierFlags.In, value);
            }
            public bool IsOutput
            {
                get => m_Flags.HasFlag(InoutModifierFlags.Out);
                set => InoutModifierFlagsUtils.Set(ref m_Flags, InoutModifierFlags.Out, value);
            }

            public Builder(SyntaxTree syntaxTree, TypeNameNode typeName, IdentifierNode name)
                : base(syntaxTree, typeName, name)
            {
            }

            public InterfaceFieldNode Build()
            {
                if (!(m_Flags.HasFlag(InoutModifierFlags.In) || m_Flags.HasFlag(InoutModifierFlags.Out)))
                    throw new InvalidOperationException("InterfaceFieldNode is not marked as an in or out parameter.");

                var node = new InterfaceFieldNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, m_Attributes);
                nodeRef.typeNameHandle = TypeName.handle;
                nodeRef.nameHandle = Name.handle;
                nodeRef.initializerHandle = new NodeHandle();
                nodeRef.flags = m_Flags;
                return node;
            }
        }
    }

    internal struct InterfaceNode : IEquatable<InterfaceNode>, ISyntaxNode<InterfaceNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/InterfaceNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList fieldHandles; // List<InterfaceFieldNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::InterfaceNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is InterfaceNode other && this.Equals(other);
        public bool Equals(InterfaceNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(InterfaceNode lhs, InterfaceNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(InterfaceNode lhs, InterfaceNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<InterfaceFieldNode> Fields => syntaxTree.EnumerateSyntaxNodes<InterfaceFieldNode>(NodeRef.fieldHandles);

        internal InterfaceNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            List<InterfaceFieldNode> m_Fields = new List<InterfaceFieldNode>();
            public IEnumerable<InterfaceFieldNode> Fields => m_Fields;
            public void Add(params InterfaceFieldNode[] fields) => Add(fields as IEnumerable<InterfaceFieldNode>);
            public void Add(IEnumerable<InterfaceFieldNode> fields)
            {
                foreach (var node in fields)
                    m_Fields.Add(Validate(node));
            }

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }

            public InterfaceNode Build()
            {
                var node = new InterfaceNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.fieldHandles.AddRange(syntaxTree, Fields);
                return node;
            }
        }
    }
}
