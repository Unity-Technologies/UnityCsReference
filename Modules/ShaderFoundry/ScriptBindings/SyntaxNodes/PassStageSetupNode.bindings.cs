// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct PassStageSetupNode : IEquatable<PassStageSetupNode>, ISyntaxNode<PassStageSetupNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TemplateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList statementHandles; // List<GenericHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::PassStageSetupNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is PassStageSetupNode other && this.Equals(other);
        public bool Equals(PassStageSetupNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(PassStageSetupNode lhs, PassStageSetupNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(PassStageSetupNode lhs, PassStageSetupNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<ISyntaxNode> Statements => syntaxTree.EnumerateSyntaxNodes(NodeRef.statementHandles);

        internal PassStageSetupNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<ISyntaxNode> m_Statements = new List<ISyntaxNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<ISyntaxNode> Statements => m_Statements;
            void AddInternal(ISyntaxNode node) => m_Statements.Add(Validate(node));
            public void Add(InterfaceFieldNode field) => AddInternal(field);
            // TODO @ SHADERS SHADERS-350: Add PassStageSettingNode bindings
            //public void Add(PassStageSettingNode setting) => AddInternal(setting);

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }

            public PassStageSetupNode Build()
            {
                var node = new PassStageSetupNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.statementHandles.AddRange(syntaxTree, m_Statements);
                return node;
            }
        }
    }
}
