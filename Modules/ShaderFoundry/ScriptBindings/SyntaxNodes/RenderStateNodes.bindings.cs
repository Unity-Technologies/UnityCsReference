// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

using RenderStateKind = UnityEditor.ShaderFoundry.RenderStateDescriptor.StateKind;

namespace UnityEditor.ShaderFoundry
{
    // TODO @ SHADERS SHADERS-1100: Convert node to use enums rather than strings and remove this class's translation tables
    internal struct RenderStateNode : IEquatable<RenderStateNode>, ISyntaxNode<RenderStateNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/RenderStateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal RenderStateKind kind;
            // Can be a property node, render target specifier, named value, or a primitive literal (e.g. integer,
            // identifier, string).
            internal NodeList arguments;
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::RenderStateNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        public enum OnOffState
        {
            On,
            Off,
        }

        internal static readonly string[] kOnOffStateStrings =
        {
            "on",
            "off",
        };

        public enum BlendFactor
        {
            One,
            Zero,
            SrcColor,
            SrcAlpha,
            SrcAlphaSaturate,
            DstColor,
            DstAlpha,
            OneMinusSrcColor,
            OneMinusSrcAlpha,
            OnMinusDstColor,
            OnMinusDstAlpha,
        }

        internal static readonly string[] kBlendFactorStrings =
        {
            "one",
            "zero",
            "src_color",
            "src_alpha",
            "src_alpha_saturate",
            "dst_color",
            "dst_alpha",
            "one_minus_src_color",
            "one_minus_src_alpha",
            "one_minus_dst_color",
            "one_minus_dst_alpha",
        };

        public enum BlendOp
        {
            Add,
            Sub,
            RevSub,
            Min,
            Max,
            LogicalClear,
            LogicalSet,
            LogicalCopy,
            LogicalCopyInverted,
            LogicalNoop,
            LogicalInvert,
            LogicalAnd,
            LogicalNand,
            LogicalOr,
            LogicalNor,
            LogicalXor,
            LogicalEquiv,
            LogicalAndReverse,
            LogicalAndInverted,
            LogicalOrReverse,
            LogicalOrInverted,
            Multiply,
            Screen,
            Overlay,
            Darken,
            Lighten,
            ColorDodge,
            ColorBurn,
            HardLight,
            SoftLight,
            Difference,
            Exclusion,
            HSLHue,
            HSLSaturation,
            HSLColor,
            HSLLuminosity,
        }

        internal static readonly string[] kBlendOpStrings =
        {
            "add",
            "sub",
            "rev_sub",
            "min",
            "max",
            "logical_clear",
            "logical_set",
            "logical_copy",
            "logical_copy_inverted",
            "logical_noop",
            "logical_invert",
            "logical_and",
            "logical_nand",
            "logical_or",
            "logical_nor",
            "logical_xor",
            "logical_equiv",
            "logical_and_reverse",
            "logical_and_inverted",
            "logical_or_reverse",
            "logical_or_inverted",
            "multiply",
            "screen",
            "overlay",
            "darken",
            "lighten",
            "color_dodge",
            "color_burn",
            "hard_light",
            "soft_light",
            "difference",
            "exclusion",
            "hsl_hue",
            "hsl_saturation",
            "hsl_color",
            "hsl_luminosity",
        };

        public enum ColorChannel
        {
            R,
            G,
            B,
            A
        }

        internal static readonly string[] kColorChannelStrings =
        {
            "r",
            "g",
            "b",
            "a",
        };

        public enum CullMode
        {
            Front,
            Back,
            Off,
        }

        internal static readonly string[] kCullModeStrings =
        {
            "front",
            "back",
            "off",
        };

        public enum ZTestOp
        {
            Less,
            LEqual,
            Equal,
            GEqual,
            Greater,
            NEqual,
            Always,
        }

        internal static readonly string[] kZTestOpStrings =
        {
            "less",
            "lequal",
            "equal",
            "gequal",
            "greater",
            "nequal",
            "always",
        };

        public enum StencilRefOrMask
        {
            Ref,
            ReadMask,
            WriteMask,
        }

        internal static readonly string[] kStencilRefOrMaskStrings =
        {
            "ref",
            "read_mask",
            "write_mask",
        };

        public enum StencilOp
        {
            Pass,
            Fail,
            ZFail,
            PassBack,
            FailBack,
            ZFailBack,
            PassFront,
            FailFront,
            ZFailFront,
        }

        internal static readonly string[] kStencilOpStrings =
        {
            "pass",
            "fail",
            "z_fail",
            "pass_back",
            "fail_back",
            "z_fail_back",
            "pass_front",
            "fail_front",
            "z_fail_front",
        };

        public enum StencilOpArgument
        {
            Keep,
            Zero,
            Replace,
            IncrSat,
            DecrSat,
            Invert,
            IncrWrap,
            DecrWrap,
        }

        internal static readonly string[] kStencilOpArgStrings =
        {
            "keep",
            "zero",
            "replace",
            "incr_sat",
            "decr_sat",
            "invert",
            "incr_wrap",
            "decr_wrap",
        };

        public enum StencilComparisonOp
        {
            Comp,
            CompBack,
            CompFront,
        }

        internal static readonly string[] kStencilComparisonOpStrings =
        {
            "comp",
            "comp_back",
            "comp_front",
        };

        public enum StencilComparisonOpArgument
        {
            Never,
            Less,
            Equal,
            LEqual,
            Greater,
            NEqual,
            GEqual,
            Always,
        }

        internal static readonly string[] kStencilComparisonOpArgStrings =
        {
            "never",
            "less",
            "equal",
            "lequal",
            "greater",
            "nequal",
            "gequal",
            "always",
        };

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is RenderStateNode other && this.Equals(other);
        public bool Equals(RenderStateNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(RenderStateNode lhs, RenderStateNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(RenderStateNode lhs, RenderStateNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal RenderStateKind Kind => NodeRef.kind;
        internal IEnumerable<ISyntaxNode> Arguments => syntaxTree.EnumerateSyntaxNodes(NodeRef.arguments);

        internal RenderStateNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        // The base class all builders below extend. Handles building a render state node from a list of arguments
        // and provides common helper functions to derived builders.
        public class RenderStateBaseBuilder : BaseBuilder
        {
            internal RenderStateKind m_Kind;
            public TextRange SourceRange { get; set; }

            internal RenderStateBaseBuilder(SyntaxTree syntaxTree, RenderStateKind kind)
                : base(syntaxTree)
            {
                m_Kind = kind;
            }

            protected IdentifierNode BuildKeywordNode<T>(T enumValue, string[] translationTable) where T : Enum
            {
                var translation = translationTable[Convert.ToInt32(enumValue)];
                return BuildIdentifierNode(translation);
            }

            protected IdentifierNode BuildIdentifierNode(string text)
            {
                var builder = new IdentifierNode.Builder(syntaxTree, text);
                builder.SourceRange = SourceRange;
                return builder.Build();
            }

            internal RenderStateNode Build(IEnumerable<ISyntaxNode> arguments)
            {
                var node = new RenderStateNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.kind = m_Kind;
                nodeRef.arguments.AddRange(syntaxTree, arguments);
                return node;
            }
        }

        // The base class for building a render state whose argument is either a property, 'On', or 'Off'.
        public class RenderStateOnOffPropertyBuilder : RenderStateBaseBuilder
        {
            internal RenderStateOnOffPropertyBuilder(SyntaxTree syntaxTree, RenderStateKind kind)
                : base(syntaxTree, kind)
            {
            }

            public RenderStateNode Build(OnOffState state)
            {
                var arguments = new List<ISyntaxNode>(){ BuildKeywordNode(state, kOnOffStateStrings) };
                return Build(arguments);
            }
            public RenderStateNode Build(RenderStatePropertyNode property)
            {
                var arguments = new List<ISyntaxNode>(){ Validate(property) };
                return Build(arguments);
            }
        }

        public class AlphaToMaskBuilder : RenderStateOnOffPropertyBuilder
        {
            public AlphaToMaskBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.AlphaToMask)
            {
            }
        }

        public class BlendBuilder : RenderStateBaseBuilder
        {
            public BlendBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.Blend)
            {
            }

            RenderStateNode BuildWithFactors(RenderStateTargetSpecifierNode target, params BlendFactor[] factors)
            {
                var arguments = new List<ISyntaxNode>();
                if (target.IsValid)
                    arguments.Add(target);
                foreach (var factor in factors)
                    arguments.Add(BuildKeywordNode(factor, kBlendFactorStrings));
                return Build(arguments);

            }
            // Builds the blend state 'Off' with an optional render target.
            public RenderStateNode Build(RenderStateTargetSpecifierNode targetSpecifier = new RenderStateTargetSpecifierNode())
            {
                var arguments = new List<ISyntaxNode>();
                if (targetSpecifier.IsValid)
                    arguments.Add(targetSpecifier);
                arguments.Add(BuildKeywordNode(OnOffState.Off, kOnOffStateStrings));
                return Build(arguments);
            }
            // Builds a blend state with the given factors and an optional render target.
            public RenderStateNode Build(BlendFactor source, BlendFactor dest,
                RenderStateTargetSpecifierNode targetSpecifier = new RenderStateTargetSpecifierNode())
            {
                return BuildWithFactors(targetSpecifier, source, dest);
            }
            // Builds a blend state with the given factors and an optional render target.
            public RenderStateNode Build(BlendFactor sourceRgb, BlendFactor destRgb,
                BlendFactor sourceAlpha, BlendFactor destAlpha,
                RenderStateTargetSpecifierNode targetSpecifier = new RenderStateTargetSpecifierNode())
            {
                return BuildWithFactors(targetSpecifier, sourceRgb, destRgb, sourceAlpha, destAlpha);
            }
        }

        public class BlendOpBuilder : RenderStateBaseBuilder
        {
            public BlendOpBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.BlendOp)
            {
            }

            public RenderStateNode Build(BlendOp op)
            {
                var arguments = new List<ISyntaxNode>(){ BuildKeywordNode(op, kBlendOpStrings) };
                return Build(arguments);
            }
            public RenderStateNode Build(RenderStatePropertyNode property)
            {
                var arguments = new List<ISyntaxNode>(){ Validate(property) };
                return Build(arguments);
            }
        }

        public class ColorMaskBuilder : RenderStateBaseBuilder
        {
            public ColorMaskBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.ColorMask)
            {
            }

            RenderStateNode BuildInternal(RenderStateTargetSpecifierNode target, ISyntaxNode argument)
            {
                var arguments = new List<ISyntaxNode>();
                if (target.IsValid)
                    arguments.Add(target);
                arguments.Add(argument);
                return Build(arguments);

            }
            // Builds a color mask in the 'Off' state with an optional render target.
            public RenderStateNode Build(RenderStateTargetSpecifierNode targetSpecifier = new RenderStateTargetSpecifierNode())
            {
                return BuildInternal(targetSpecifier, BuildKeywordNode(OnOffState.Off, kOnOffStateStrings));
            }
            // Builds a color mask from the given flags and an optional render target.
            public RenderStateNode Build(IEnumerable<ColorChannel> mask,
                RenderStateTargetSpecifierNode targetSpecifier = new RenderStateTargetSpecifierNode())
            {
                // Validate color mask
                UInt32 bitMask = 0;
                string stringMask = "";
                foreach (var channel in mask)
                {
                    var bitFlag = 1u << (Int32)channel;
                    if ((bitMask & bitFlag) > 0)
                        throw new InvalidOperationException("A color mask may not contain the same channel more than once.");
                    bitMask |= bitFlag;

                    stringMask += kColorChannelStrings[Convert.ToInt32(channel)];
                }
                if (bitMask == 0)
                    throw new InvalidOperationException("Color mask must be non-empty.");

                return BuildInternal(targetSpecifier, BuildIdentifierNode(stringMask));
            }
            // Builds a color mask from the given property and an optional render target.
            public RenderStateNode Build(RenderStatePropertyNode property,
                RenderStateTargetSpecifierNode targetSpecifier = new RenderStateTargetSpecifierNode())
            {
                return BuildInternal(targetSpecifier, Validate(property));
            }
        }

        public class ConservativeBuilder : RenderStateOnOffPropertyBuilder
        {
            public ConservativeBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.Conservative)
            {
            }
        }

        public class CullBuilder : RenderStateBaseBuilder
        {
            public CullBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.Cull)
            {
            }

            public RenderStateNode Build(CullMode mode)
            {
                var arguments = new List<ISyntaxNode> { BuildKeywordNode(mode, kCullModeStrings) };
                return Build(arguments);
            }
            public RenderStateNode Build(RenderStatePropertyNode property)
            {
                var arguments = new List<ISyntaxNode> { Validate(property) };
                return Build(arguments);
            }
        }

        public class ZClipBuilder : RenderStateOnOffPropertyBuilder
        {
            public ZClipBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.ZClip)
            {
            }
        }

        public class ZTestBuilder : RenderStateOnOffPropertyBuilder
        {
            public ZTestBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.ZTest)
            {
            }

            public RenderStateNode Build(ZTestOp op)
            {
                var arguments = new List<ISyntaxNode>() { BuildKeywordNode(op, kZTestOpStrings) };
                return Build(arguments);
            }
        }

        public class ZWriteBuilder : RenderStateOnOffPropertyBuilder
        {
            public ZWriteBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.ZWrite)
            {
            }
        }

        public class OffsetBuilder : RenderStateBaseBuilder
        {
            public OffsetBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.Offset)
            {
            }

            RenderStateNode BuildInternal(ISyntaxNode factor, ISyntaxNode units)
            {
                var arguments = new List<ISyntaxNode>()
                {
                    Validate(factor),
                    Validate(units),
                };
                return Build(arguments);
            }
            public RenderStateNode Build(FloatLiteralNode factor, FloatLiteralNode units)
                => BuildInternal(factor, units);
            public RenderStateNode Build(FloatLiteralNode factor, RenderStatePropertyNode units)
                => BuildInternal(factor, units);
            public RenderStateNode Build(RenderStatePropertyNode factor, FloatLiteralNode units)
                => BuildInternal(factor, units);
            public RenderStateNode Build(RenderStatePropertyNode factor, RenderStatePropertyNode units)
                => BuildInternal(factor, units);
        }

        public class StencilBuilder : RenderStateBaseBuilder
        {
            List<ISyntaxNode> m_Arguments = new List<ISyntaxNode>();
            public IEnumerable<ISyntaxNode> Arguments => m_Arguments;
            public void AddRefOrMask(StencilRefOrMask target, IntegerLiteralNode value)
                => AddNamedValue(target, kStencilRefOrMaskStrings, ValidateBufferValue(value));
            public void AddRefOrMask(StencilRefOrMask target, RenderStatePropertyNode value)
                => AddNamedValue(target, kStencilRefOrMaskStrings, Validate(value));
            public void AddStencilOp(StencilOp op, StencilOpArgument arg)
                => AddNamedValue(op, kStencilOpStrings, BuildKeywordNode(arg, kStencilOpArgStrings));
            public void AddStencilOp(StencilOp op, RenderStatePropertyNode arg)
                => AddNamedValue(op, kStencilOpStrings, Validate(arg));
            public void AddComparisonOp(StencilComparisonOp op, StencilComparisonOpArgument arg)
                => AddNamedValue(op, kStencilComparisonOpStrings, BuildKeywordNode(arg, kStencilComparisonOpArgStrings));
            public void AddComparisonOp(StencilComparisonOp op, RenderStatePropertyNode arg)
                => AddNamedValue(op, kStencilComparisonOpStrings, Validate(arg));

            void AddNamedValue<T>(T name, string[] translationTable, ISyntaxNode value) where T : Enum
            {
                var nameNode = BuildKeywordNode(name, translationTable);
                var builder = new RenderStateNamedValueNode.Builder(syntaxTree, nameNode, value);
                var namedValueNode = builder.Build();
                m_Arguments.Add(namedValueNode);
            }

            IntegerLiteralNode ValidateBufferValue(IntegerLiteralNode node)
            {
                Validate(node);

                bool isValid;
                if (node.IsSigned)
                    isValid = 0 <= node.SignedValue && node.SignedValue <= 255;
                else
                    isValid = node.UnsignedValue <= 255;
                if (!isValid)
                    throw new InvalidOperationException("Stencil buffer value must be in the range [0, 255].");
                return node;
            }

            public StencilBuilder(SyntaxTree syntaxTree)
                : base(syntaxTree, RenderStateKind.Stencil)
            {
            }

            public RenderStateNode Build()
            {
                return Build(Arguments);
            }
        }
    }

    internal struct RenderStatesNode : IEquatable<RenderStatesNode>, ISyntaxNode<RenderStatesNode>
    {
        [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/RenderStateNodes.h")]
        internal struct InternalNode
        {
            [NativeProperty("kNodeType", false, TargetType.Field, true)]
            internal extern static NodeType kNodeType { get; }
            internal TextRange sourceRange;
            internal NodeList statementHandles; // List<RenderStateNodeHandle>
        }

        // This function must be defined for a static size check between native and managed types to be generated.
        [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
        [FreeFunction("ShaderFoundry::BlockParser::BlittabilityCheck<ShaderFoundry::BlockParser::RenderStatesNode>")]
        private static extern void BlittabilityCheck(InternalNode node);

        internal readonly SyntaxTree syntaxTree;
        internal readonly NodeHandle handle;
        internal readonly IntPtr nodePtr;

        // ISyntaxNode Interface
        SyntaxTree ISyntaxNode.syntaxTree => syntaxTree;
        NodeHandle ISyntaxNode.handle => handle;

        // IEquality Interface
        public override bool Equals(object obj) => obj is RenderStatesNode other && this.Equals(other);
        public bool Equals(RenderStatesNode other) => syntaxTree == other.syntaxTree && handle == other.handle;
        public override int GetHashCode() => (syntaxTree, handle).GetHashCode();
        public static bool operator ==(RenderStatesNode lhs, RenderStatesNode rhs) => lhs.Equals(rhs);
        public static bool operator !=(RenderStatesNode lhs, RenderStatesNode rhs) => !lhs.Equals(rhs);

        // Node Interface
        internal bool IsValid => handle.IsValid;
        ref InternalNode NodeRef => ref syntaxTree.GetNodeRef<InternalNode>(nodePtr);
        internal TextRange SourceRange => NodeRef.sourceRange;
        internal IEnumerable<RenderStateNode> Statements
            => syntaxTree.EnumerateSyntaxNodes<RenderStateNode>(NodeRef.statementHandles);

        internal RenderStatesNode(SyntaxTree syntaxTree, NodeHandle handle)
        {
            this.syntaxTree = syntaxTree;
            this.handle = handle;
            this.nodePtr = syntaxTree.GetNodeIntPtr(handle);
        }

        public class Builder : BaseBuilder
        {
            List<RenderStateNode> m_Statements = new List<RenderStateNode>();
            public TextRange SourceRange { get; set; }
            public IEnumerable<RenderStateNode> Statements => m_Statements;
            public void AddStatement(RenderStateNode statement) => m_Statements.Add(Validate(statement));

            public Builder(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }

            public RenderStatesNode Build()
            {
                var node = new RenderStatesNode(syntaxTree, syntaxTree.AllocateByNodeType(InternalNode.kNodeType));
                ref var nodeRef = ref node.NodeRef;
                nodeRef.sourceRange = SourceRange;
                nodeRef.statementHandles.AddRange(syntaxTree, Statements);
                return node;
            }
        }
    }
}
