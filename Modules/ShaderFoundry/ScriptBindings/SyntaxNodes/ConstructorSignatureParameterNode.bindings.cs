// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct ConstructorSignatureParameterNode : IEquatable<ConstructorSignatureParameterNode>, ISyntaxNode<ConstructorSignatureParameterNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/ConstructorSignatureNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList attributeHandles; // List<AttributeNodeHandle>
            internal NodeHandle typeNameHandle; // TypeNameNodeHandle
            internal NodeHandle nameHandle; // IdentifierNodeHandle
            internal NodeHandle initializerHandle; // TextNodeHandle
            short padding0;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::ConstructorSignatureParameterNode>")]
        private static extern void BlittabilityCheck(InternalNode node);
        
        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is ConstructorSignatureParameterNode other && this.Equals(other);
        public bool Equals(ConstructorSignatureParameterNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(ConstructorSignatureParameterNode lhs, ConstructorSignatureParameterNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(ConstructorSignatureParameterNode lhs, ConstructorSignatureParameterNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal TypeNameNode TypeName => new TypeNameNode(syntaxTree, NodeRef.typeNameHandle);
        internal IdentifierNode Name => new IdentifierNode(syntaxTree, NodeRef.nameHandle);
        internal TextNode Initializer => NodeRef.initializerHandle.IsValid ? new TextNode(syntaxTree, NodeRef.initializerHandle) : new TextNode();

        internal ConstructorSignatureParameterNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseVariableBuilder
        {
            public TextNode Initializer { get; set; }

            public Builder(SyntaxTree syntaxTree, TypeNameNode typeName, IdentifierNode name)
                : base(syntaxTree, typeName, name)
            {
            }

            public ConstructorSignatureParameterNode Build()
            {
                var node = new ConstructorSignatureParameterNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.typeNameHandle = TypeName.handle;
                nodeRef.nameHandle = Name.handle;
                nodeRef.initializerHandle = Initializer.handle;
                return node;
            }
        }
    }
}
