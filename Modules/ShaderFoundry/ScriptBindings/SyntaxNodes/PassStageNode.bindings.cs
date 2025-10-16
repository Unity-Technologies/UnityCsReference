// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using ShaderStage = UnityEngine.Shaders.ShaderStage;
using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal struct PassStageNode : IEquatable<PassStageNode>, ISyntaxNode<PassStageNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/TemplateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeHandle setupHandle; // PassStageSetupNodeHandle
            internal NodeList sequenceHandles; // List<GenericHandle>
            internal ShaderStage stageType;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::PassStageNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is PassStageNode other && this.Equals(other);
        public bool Equals(PassStageNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(PassStageNode lhs, PassStageNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(PassStageNode lhs, PassStageNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal PassStageSetupNode Setup => new PassStageSetupNode(syntaxTree, NodeRef.setupHandle);
        internal IEnumerable<ISyntaxNode> Sequence =>
            syntaxTree.EnumerateSyntaxNodes<ISyntaxNode>(NodeRef.sequenceHandles);
        internal ShaderStage Stage => NodeRef.stageType;

        internal PassStageNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            PassStageSetupNode m_Setup;
            List<ISyntaxNode> m_Sequence = new List<ISyntaxNode>();
            public TextRange SourceRange { get; set; }
            public PassStageSetupNode Setup
            {
                get => m_Setup;
                set => m_Setup = Validate(value);
            }
            public IEnumerable<ISyntaxNode> Sequence => m_Sequence;
            public OutputLinkerNode OutputLinker { get; set; }
            public void AddSequenceElement(BlockSequenceElementNode element) => m_Sequence.Add(Validate(element));
            public ShaderStage Stage { get; set; }

            public Builder(SyntaxTree syntaxTree, ShaderStage stage, PassStageSetupNode setup)
                : base(syntaxTree)
            {
                Stage = stage;
                Setup = setup;
            }

            public PassStageNode Build()
            {
                var node = new PassStageNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.setupHandle = Setup.handle;
                nodeRef.sequenceHandles.AddRange(syntaxTree, m_Sequence);
                nodeRef.sequenceHandles.TryAdd(syntaxTree, OutputLinker.handle);
                nodeRef.stageType = Stage;
                return node;
            }
        }
    }
}
