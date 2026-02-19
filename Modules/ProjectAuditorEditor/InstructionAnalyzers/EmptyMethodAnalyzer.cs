// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class EmptyMethodAnalyzer : CodeModuleInstructionAnalyzer
    {
        internal const string PAC2001 = nameof(PAC2001);

        static readonly Descriptor k_Descriptor = new Descriptor
            (
            PAC2001,
            "Empty MonoBehaviour Method",
            Areas.CPU,
            "Any empty MonoBehaviour message handling method (for example, Awake(), Start(), Update()) will be included in the build and executed even if it is empty. Every message handling method on every instance of a MonoBehaviour takes a small amount of CPU time.",
            "Remove any empty MonoBehaviour methods."
            )
        {
            MessageFormat = "MonoBehaviour method '{0}' is empty"
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Ret
        };

        public override IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_Descriptor);
        }

        public override ReportItemBuilder Analyze(InstructionAnalysisContext context)
        {
            // skip any no-op
            var previousIL = context.Instruction.Previous;
            while (previousIL != null && previousIL.OpCode == OpCodes.Nop)
                previousIL = previousIL.Previous;

            // if there is no instruction before OpCodes.Ret, then we know this method is empty
            if (previousIL != null)
                return null;

            var methodDefinition = context.MethodDefinition;
            if (!MonoBehaviourAnalysis.IsMonoBehaviourEvent(methodDefinition))
                return null;

            if (!MonoBehaviourAnalysis.IsMonoBehaviour(methodDefinition.DeclaringType))
                return null;

            return context.CreateIssue(IssueCategory.Code, k_Descriptor.Id, methodDefinition.Name);
        }

        internal static string GetDescriptorID()
        {
            return k_Descriptor.Id;
        }
    }
}
