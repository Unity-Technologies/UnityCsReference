// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class BoxingAnalyzer : CodeModuleInstructionAnalyzer
    {
        internal const string PAC2000 = nameof(PAC2000);

        static readonly Descriptor k_Descriptor = new Descriptor
            (
            PAC2000,
            "Boxing Allocation",
            Areas.Memory,
            "Boxing happens where a value type, such as an integer, is converted into an object of reference type. This causes an allocation on the managed heap.",
            "Try to avoid boxing when possible. Create methods and APIs that can accept value types."
            )
        {
            DocumentationUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing",
            MessageFormat = "Conversion from value type '{0}' to ref type"
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Box
        };

        public override IReadOnlyList<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_Descriptor);
        }

        public override ReportItemBuilder Analyze(InstructionAnalysisContext context)
        {
            var type = (TypeReference)context.Instruction.Operand;
            if (type.IsGenericParameter)
            {
                var isValueType = true; // assume it's value type
                var genericType = (GenericParameter)type;
                if (genericType.HasReferenceTypeConstraint)
                    isValueType = false;
                else
                    foreach (var constraint in genericType.Constraints)
                        if (!constraint.ConstraintType.IsValueType)
                            isValueType = false;
                if (!isValueType)
                    // boxing on ref types are no-ops, so not a problem
                    return null;
            }

            var typeName = type.Name;
            if (type.FullName.Equals("System.Single"))
                typeName = "float";
            else if (type.FullName.Equals("System.Double"))
                typeName = "double";

            return context.CreateIssue(IssueCategory.Code, k_Descriptor.Id, typeName);
        }
    }
}
