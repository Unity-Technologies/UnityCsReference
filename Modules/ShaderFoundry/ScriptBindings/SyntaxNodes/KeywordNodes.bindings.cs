// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Shaders;
using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct KeywordNode : IEquatable<KeywordNode>, ISyntaxNode<KeywordNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/KeywordNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal KeywordDescriptor.DefinitionType definition;
            internal KeywordDescriptor.ScopeType scope;
            internal ShaderStageFlags stage;
            internal bool allowsNone;
            internal NodeList opHandles; // List<IdentifierNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::KeywordNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is KeywordNode other && this.Equals(other);
        public bool Equals(KeywordNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(KeywordNode lhs, KeywordNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(KeywordNode lhs, KeywordNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal KeywordDescriptor.DefinitionType Definition => NodeRef.definition;
        internal KeywordDescriptor.ScopeType Scope => NodeRef.scope;
        internal ShaderStageFlags Stage => NodeRef.stage;
        internal bool AllowsNone => NodeRef.allowsNone;
        internal IEnumerable<IdentifierNode> Identifiers => syntaxTree.EnumerateSyntaxNodes<IdentifierNode>(NodeRef.opHandles);

        internal KeywordNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }
        public class Builder : BaseBuilder
        {
            KeywordDescriptor.ScopeType m_Scope = KeywordDescriptor.ScopeType.Local;
            ShaderStageFlags m_Stage = ShaderStageFlags.Any;
            KeywordDescriptor.DefinitionType m_Definition = KeywordDescriptor.DefinitionType.DynamicBranch;
            public TextRange SourceRange { get; set; }
            public KeywordDescriptor.ScopeType Scope
            {
                get => m_Scope;
                set => m_Scope = ValidateScope(value);
            }
            public ShaderStageFlags Stage
            {
                get => m_Stage;
                set => m_Stage = ValidateStage(value);
            }
            public KeywordDescriptor.DefinitionType Definition
            {
                get => m_Definition;
                set => m_Definition = ValidateDefinition(value);
            }
            public bool AllowsNone { get; set; } = true;

            List<IdentifierNode> m_Identifiers = new List<IdentifierNode>();
            public IEnumerable<IdentifierNode> Identifiers => m_Identifiers;
            public void Add(IdentifierNode node)
            {
                Validate(node);

                // The op name must not be all underscores
                bool nameIsValid = false;
                var opName = node.Text;
                for (int i = 0, n = opName.Length; i < n; ++i)
                {
                    if (opName[i] != '_')
                    {
                        nameIsValid = true;
                        break;
                    }
                }
                if (!nameIsValid)
                    throw new InvalidOperationException("Keyword name must not be all underscores.  Use the 'allowsNone' option to include a fully disabled variant.");

                m_Identifiers.Add(node);
            }
            KeywordDescriptor.ScopeType ValidateScope(KeywordDescriptor.ScopeType scope)
            {
                if (scope == KeywordDescriptor.ScopeType.Invalid)
                    throw new InvalidOperationException("Keyword scope must not be Invalid.");
                return scope;
            }
            ShaderStageFlags ValidateStage(ShaderStageFlags flags)
            {
                if (flags != ShaderStageFlags.Any && flags != ShaderStageFlags.Vertex
                    && flags != ShaderStageFlags.Fragment)
                {
                    throw new InvalidOperationException("Keyword stage must be either Vertex, Fragment, or Any.");
                }
                return flags;
            }
            KeywordDescriptor.DefinitionType ValidateDefinition(KeywordDescriptor.DefinitionType definition)
            {
                if (definition == KeywordDescriptor.DefinitionType.Invalid)
                    throw new InvalidOperationException("Keyword definition must not be Invalid.");
                return definition;
            }

            public Builder(SyntaxTree tree)
                : base(tree)
            {
            }

            public KeywordNode Build()
            {
                if (m_Identifiers.Count == 0)
                    throw new InvalidOperationException("A keyword node must contain at least one identifier.");

                var node = new KeywordNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.definition = Definition;
                nodeRef.scope = Scope;
                nodeRef.stage = Stage;
                nodeRef.allowsNone = AllowsNone;
                nodeRef.opHandles.AddRange(syntaxTree, Identifiers);
                return node;
            }
        }
    }

    internal struct KeywordsNode : IEquatable<KeywordsNode>, ISyntaxNode<KeywordsNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/KeywordNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList keywordHandles; // List<KeywordNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::KeywordsNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is KeywordsNode other && this.Equals(other);
        public bool Equals(KeywordsNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(KeywordsNode lhs, KeywordsNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(KeywordsNode lhs, KeywordsNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<KeywordNode> Keywords => syntaxTree.EnumerateSyntaxNodes<KeywordNode>(NodeRef.keywordHandles);

        internal KeywordsNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }
        public class Builder : BaseBuilder
        {
            public TextRange SourceRange { get; set; }
            List<KeywordNode> m_Keywords = new List<KeywordNode>();
            public IEnumerable<KeywordNode> Keywords => m_Keywords;
            public void Add(KeywordNode node) => m_Keywords.Add(Validate(node));

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }

            public KeywordsNode Build()
            {
                var node = new KeywordsNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.keywordHandles.AddRange(syntaxTree, Keywords);
                return node;
            }
        }
    }
}
