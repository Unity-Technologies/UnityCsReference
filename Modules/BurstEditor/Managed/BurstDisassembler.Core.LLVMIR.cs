// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst.Editor
{
    internal partial class BurstDisassembler
    {
        /// <summary>
        /// <see cref="AsmTokenKind"/> provider for LLVM IR - intrinsics are not covered at this time
        /// </summary>
        private class LLVMIRAsmTokenKindProvider : AsmTokenKindProvider
        {

            private static readonly string[] Qualifiers = new[]
            {
                "to",
                "new",

                "float",
                "double",
                "i1",
                "i32",
                "i16",
                "i64",

                "eq",
                "ne",
                "ugt",
                "uge",
                "ult",
                "ule",
                "sgt",
                "sge",
                "slt",
                "sle",

                "false",
                "true",

                "oeq",
                "ogt",
                "oge",
                "olt",
                "ole",
                "one",
                "ord",
                "ueq",
                "une",
                "uno",
            };

            private static readonly string[] Instructions = new[]
            {
                "ret",
                "br",
                "switch",
                "indirectbr",
                "invoke",
                "callbr",
                "resume",
                "catchswitch",
                "catchret",
                "cleanupret",
                "unreachable",

                "add",
                "sub",
                "mul",
                "udiv",
                "sdiv",
                "urem",
                "srem",

                "shl",
                "lshr",
                "ashr",
                "and",
                "or",
                "xor",

                "extractvalue",
                "insertvalue",

                "alloca",
                "load",
                "store",
                "fence",
                "cmpxchg",
                "atomicrmw",
                "getelementptr",

                "trunc",
                "zext",
                "sext",
                "ptrtoint",
                "inttoptr",
                "bitcast",
                "addrspacecast",

                "icmp",
                "phi",
                "select",
                "freeze",
                "call",
                "va_arg",
                "landingpad",
                "catchpad",
                "cleanuppad",
            };

            private static readonly string[] FpuInstructions = new[]
            {
                "fneg",

                "fadd",
                "fsub",
                "fmul",
                "fdiv",
                "frem",

                "fptrunc",
                "fpext",
                "fptoui",
                "fptosi",
                "uitofp",
                "sitofp",

                "fcmp",
            };

            private static readonly string[] SimdInstructions = new[]
            {
                "extractelement",
                "insertelement",
                "shufflevector",
            };

            private LLVMIRAsmTokenKindProvider() : base(Qualifiers.Length + Instructions.Length + FpuInstructions.Length + SimdInstructions.Length)
            {
                foreach (var instruction in Qualifiers)
                {
                    AddTokenKind(instruction, AsmTokenKind.Qualifier);
                }

                foreach (var instruction in Instructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.Instruction);
                }

                foreach (var instruction in FpuInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.Instruction);
                }

                foreach (var instruction in SimdInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.InstructionSIMD);
                }
            }

            public override SIMDkind SimdKind(StringSlice instruction)
            {
                throw new System.NotImplementedException("Syntax Highlighting is not implemented for LLVM IR.");
            }

            public static readonly LLVMIRAsmTokenKindProvider Instance = new LLVMIRAsmTokenKindProvider();
        }
    }
}
