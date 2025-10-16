// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct CharacterLiteralNode : IEquatable<CharacterLiteralNode>, ISyntaxNode<CharacterLiteralNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/LiteralNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal UInt32 textIndex;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::CharacterLiteralNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is CharacterLiteralNode other && this.Equals(other);
        public bool Equals(CharacterLiteralNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(CharacterLiteralNode lhs, CharacterLiteralNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(CharacterLiteralNode lhs, CharacterLiteralNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal char Character => syntaxTree.GetString(NodeRef.textIndex)[0];
        internal TextRange SourceRange => NodeRef.sourceRange;

        internal CharacterLiteralNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            private char m_character;
            public char Character
            {
                get => m_character;
                set
                {
                    if(!IsAscii(value))
                    {
                        throw new ArgumentException("Builder.Value char does not allow unicode characters");
                    }
                    m_character = value;
                }
            }
            public Builder(SyntaxTree syntaxTree, char value)
                : base(syntaxTree)
            {
                Character = value;
            }
            public CharacterLiteralNode Build()
            {
                var node = new CharacterLiteralNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.textIndex = node.syntaxTree.AddString(Character.ToString());
                return node;
            }
        }
    }
}
