// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct StructFieldNode : IEquatable<StructFieldNode>, ISyntaxNode<StructFieldNode>
    {
        // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN StructNodes.h
        internal enum ModifierFlag : byte
        {
            None = 0,
            Static = 1 << 0,
            Const = 1 << 1,
        };
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/StructNodes.h")]
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
            internal ModifierFlag flags;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::StructFieldNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is StructFieldNode other && this.Equals(other);
        public bool Equals(StructFieldNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(StructFieldNode lhs, StructFieldNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(StructFieldNode lhs, StructFieldNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<AttributeNode> Attributes => syntaxTree.EnumerateSyntaxNodes<AttributeNode>(NodeRef.attributeHandles);
        internal TypeNameNode TypeName => new TypeNameNode(syntaxTree, NodeRef.typeNameHandle);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal TextNode Initializer => NodeRef.initializerHandle.IsValid ? new TextNode(syntaxTree, NodeRef.initializerHandle) : new TextNode();
        internal bool IsStatic => NodeRef.flags.HasFlag(ModifierFlag.Static);
        internal bool IsConst => NodeRef.flags.HasFlag(ModifierFlag.Const);

        internal StructFieldNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseVariableBuilder
        {
            public bool IsStatic { get; set; }
            public bool IsConst { get; set; }
            public TextNode Initializer { get; set; }

            public Builder(SyntaxTree syntaxTree, TypeNameNode typeName, IdentifierNode name)
                : base(syntaxTree, typeName, name)
            {
            }

            public StructFieldNode Build()
            {
                // According to the grammar, we only currently actually support static const with an initializer.
                // Technically hlsl allows static without const, but we don't have a way to parse
                // this now as the field has to be declared outside of the class for this to work.
                if (IsStatic && !(IsConst && Initializer.IsValid))
                    throw new InvalidOperationException("StructFieldNodes marked as static must be const and have a valid initializer.");
                if (IsConst && !(IsStatic && Initializer.IsValid))
                    throw new InvalidOperationException("StructFieldNodes marked as const must be static and have a valid initializer.");
                if (Initializer.IsValid && !(IsConst && IsStatic))
                    throw new InvalidOperationException("StructFieldNodes with an initializer must be marked as const and static.");

                var node = new StructFieldNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.attributeHandles.AddRange(syntaxTree, m_Attributes);
                nodeRef.typeNameHandle = TypeName.handle;
                nodeRef.nameHandle = Name.handle;
                nodeRef.initializerHandle = Initializer.handle;
                nodeRef.flags = ModifierFlag.None;
                if (IsStatic)
                    nodeRef.flags |= ModifierFlag.Static;
                if (IsConst)
                    nodeRef.flags |= ModifierFlag.Const;
                return node;
            }
        }
    }
}
