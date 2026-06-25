// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Burst.Editor
{
    internal partial class BurstDisassembler
    {
        internal class WasmAsmTokenKindProvider : AsmTokenKindProvider
        {
            private static readonly string[] Registers = new[]
            {
                "memory.",     // add . to avoid parsing instruction portion as directive
                "local.",
                "global.",
                "i32.",
                "i64.",
                "f32.",
                "f64."
            };

            private static readonly string[] Qualifiers = new[]
            {
                "offset",
                "align",

                "eqz",
                "eq",
                "ne",
                "lt_s",
                "lt_u",
                "gt_s",
                "gt_u",
                "le_s",
                "le_u",
                "ge_s",
                "ge_u",
                "lt",
                "gt",
                "le",
                "ge",
            };

            private static readonly string[] Instructions = new[]
            {
                "if",
                "end",
                "block",
                "end_block",
                "end_loop",
                "end_function",
                "loop",
                "unreachable",
                "nop",
                "call",
                "call_indirect",

                "drop",
                "select",
                "get",
                "set",
                "tee",

                "load",
                "load8_s",
                "load8_u",
                "load16_s",
                "load16_u",
                "load32_s",
                "load32_u",
                "store",
                "store8",
                "store16",
                "store32",
                "size",
                "grow",

                "const",
                "clz",
                "ctz",
                "popcnt",
                "add",
                "sub",
                "mul",
                "div_s",
                "div_u",
                "rem_s",
                "rem_u",
                "and",
                "or",
                "xor",
                "shl",
                "shr_s",
                "shr_u",
                "rotl",
                "rotr",
                "abs",
                "neg",
                "ceil",
                "floor",
                "trunc",
                "sqrt",
                "div",
                "min",
                "max",
                "copysign",

                "wrap_i64",
                "trunc_f32_s",
                "trunc_f32_u",
                "trunc_f64_s",
                "trunc_f64_u",
                "extend_i32_s",
                "extend_i32_u",
                "convert_i32_s",
                "convert_i32_u",
                "convert_i64_s",
                "convert_i64_u",
                "demote_f64",
                "promote_f32",
                "reinterpret_f32",
                "reinterpret_f64",
                "reinterpret_i32",
                "reinterpret_i64",
            };

            private static readonly string[] BranchInstructions = new string[]
            {
                "br_if",
            };

            private static readonly string[] JumpInstructions = new string[]
            {
                "br",
                "br_table"
            };

            private static readonly string[] ReturnInstructions = new string[]
            {
                "return",
            };

            private static readonly string[] SimdInstructions = Array.Empty<string>();

            private WasmAsmTokenKindProvider() : base(
                Registers.Length +
                Qualifiers.Length +
                Instructions.Length +
                BranchInstructions.Length +
                JumpInstructions.Length +
                ReturnInstructions.Length +
                SimdInstructions.Length)
            {
                foreach (var register in Registers)
                {
                    AddTokenKind(register, AsmTokenKind.Register);
                }

                foreach (var instruction in Qualifiers)
                {
                    AddTokenKind(instruction, AsmTokenKind.Qualifier);
                }

                foreach (var instruction in Instructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.Instruction);
                }

                foreach (var instruction in BranchInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.BranchInstruction);
                }

                foreach (var instruction in JumpInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.JumpInstruction);
                }

                foreach (var instruction in ReturnInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.ReturnInstruction);
                }

                foreach (var instruction in SimdInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.InstructionSIMD);
                }
            }

            public override bool AcceptsCharAsIdentifierOrRegisterEnd(char c)
            {
                return c == '.';
            }

            public override bool IsInstructionOrRegisterOrIdentifier(char c)
            {
                // Wasm should not take '.' with it as this will take register.instruction combo as one.
                return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' ||
                       c == '@';
            }

            public override SIMDkind SimdKind(StringSlice instruction)
            {
                throw new NotImplementedException("WASM does not contain any SIMD instruction.");
            }

            public static readonly WasmAsmTokenKindProvider Instance = new WasmAsmTokenKindProvider();
        }
    }
}
