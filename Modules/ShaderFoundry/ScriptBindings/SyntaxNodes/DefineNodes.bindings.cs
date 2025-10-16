// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct DefineNode : IEquatable<DefineNode>, ISyntaxNode<DefineNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/DefineNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle identifierHandle;
            internal NodeHandle argumentHandle;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::DefineNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is DefineNode other && this.Equals(other);
        public bool Equals(DefineNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(DefineNode lhs, DefineNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(DefineNode lhs, DefineNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IdentifierNode Identifier => new IdentifierNode(syntaxTree, NodeRef.identifierHandle);
        internal ISyntaxNode Argument => syntaxTree.GetSyntaxNode(NodeRef.argumentHandle);

        internal DefineNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            IdentifierNode m_identifier;
            ISyntaxNode m_argument;
            public IdentifierNode Identifier
            {
                get => m_identifier;
                set => m_identifier = Validate(value);
            }
            public ISyntaxNode Argument
            {
                get => m_argument;
                private set => m_argument = Validate(value);
            }

            public void SetArgument(BooleanLiteralNode argument) => Argument = argument;
            public void SetArgument(CharacterLiteralNode argument) => Argument = argument;
            public void SetArgument(FloatLiteralNode argument) => Argument = argument;
            public void SetArgument(IdentifierNode argument) => Argument = argument;
            public void SetArgument(IntegerLiteralNode argument) => Argument = argument;
            public void SetArgument(StringLiteralNode argument) => Argument = argument;

            public Builder(SyntaxTree syntaxTree, IdentifierNode identifier)
                : base(syntaxTree)
            {
                Identifier = identifier;
            }

            public DefineNode Build()
            {
                var node = new DefineNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.identifierHandle = Identifier.handle;
                if(Argument != null)
                    nodeRef.argumentHandle = Argument.handle;
                return node;
            }
        }
    }

    internal struct DefinesNode : IEquatable<DefinesNode>, ISyntaxNode<DefinesNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/DefineNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList contentHandles;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::DefinesNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is DefinesNode other && this.Equals(other);
        public bool Equals(DefinesNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(DefinesNode lhs, DefinesNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(DefinesNode lhs, DefinesNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<DefineNode> Contents => syntaxTree.EnumerateSyntaxNodes<DefineNode>(NodeRef.contentHandles);

        internal DefinesNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            List<DefineNode> m_contents = new List<DefineNode>();
            public IEnumerable<DefineNode> Contents => m_contents;
            public void AddDefines(params DefineNode[] defineNodes) => InternalAddDefines(defineNodes);
            public void AddDefines(IEnumerable<DefineNode> defineNodes) => InternalAddDefines(defineNodes);
            void InternalAddDefines(IEnumerable<DefineNode> defineNodes)
            {
                foreach (var def in defineNodes)
                    m_contents.Add(Validate(def));
            }
            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }

            public DefinesNode Build()
            {
                var node = new DefinesNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.contentHandles.AddRange(syntaxTree, Contents);
                return node;
            }
        }
    }
}
