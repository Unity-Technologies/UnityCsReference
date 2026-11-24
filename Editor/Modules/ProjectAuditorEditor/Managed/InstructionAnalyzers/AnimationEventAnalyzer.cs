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
    class AnimationEventAnalyzer : CodeModuleInstructionAnalyzer
    {
        internal const string PAC2010 = nameof(PAC2010);

        static readonly Descriptor k_Descriptor = new Descriptor
            (
            PAC2010,
            "MonoBehaviour method uses 'AnimationEvent' parameter",
            Areas.Memory,
            "Using an AnimationEvent parameter for event functions causes allocations.",
            "If you require access to AnimationEvent.animationState, AnimationEvent.animatorClipInfo, or AnimationEvent.animatorStateInfo, prefer using AnimationEventInfo. Otherwise, prefer using float, int, string or an object reference."
            )
        {
            MessageFormat = "MonoBehaviour method '{0}' uses AnimationEvent parameter"
        };

        public override IReadOnlyList<OpCode> opCodes => Array.Empty<OpCode>();

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_Descriptor);
        }

        internal override ReportItemBuilder OnAnalyzeMethodBody(MethodAnalysisContext context)
        {
            if (!MonoBehaviourAnalysis.IsMonoBehaviour(context.MethodDefinition.DeclaringType))
                return null;

            var parameters = context.MethodDefinition.Parameters;
            if (parameters.Count == 1 && parameters[0].ParameterType.FullName == "UnityEngine.AnimationEvent")
                return context.CreateIssue(IssueCategory.Code, k_Descriptor.Id, context.MethodDefinition.Name);

            return null;
        }

        public override IEnumerable<ReportItemBuilder> Analyze(InstructionAnalysisContext context)
        {
            yield break;
        }
    }
}
