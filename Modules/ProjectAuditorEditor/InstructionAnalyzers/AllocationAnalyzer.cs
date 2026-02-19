// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Collections;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class AllocationAnalyzer : CodeModuleInstructionAnalyzer
    {
        internal const string PAC2002 = nameof(PAC2002);
        internal const string PAC2003 = nameof(PAC2003);
        internal const string PAC2004 = nameof(PAC2004);
        internal const string PAC2005 = nameof(PAC2005);
        internal const string PAC2006 = nameof(PAC2006);

        static readonly Descriptor k_ObjectAllocationDescriptor = new Descriptor
            (
            PAC2002,
            "Object Allocation",
            Areas.Memory,
            "An object is allocated in managed memory.",
            "Try to avoid allocating objects in frequently-updated code."
            )
        {
            DocumentationUrl = "https://learn.microsoft.com/en-us/dotnet/standard/automatic-memory-management",
            MessageFormat = "'{0}' allocation",
            DefaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_ClosureAllocationDescriptor = new Descriptor
            (
            PAC2003,
            "Closure Allocation",
            Areas.Memory,
            "A closure is allocating managed memory. A closure occurs when a variable's state is captured by an in-line delegate, anonymous method or lambda which accesses that variable.",
            "Try to avoid allocating objects in frequently-updated code."
            )
        {
            DocumentationUrl = "https://learn.microsoft.com/en-us/dotnet/standard/automatic-memory-management",
            MessageFormat = "Closure allocation in '{0}.{1}'",
            DefaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_ArrayAllocationDescriptor = new Descriptor
            (
            PAC2004,
            "Array Allocation",
            Areas.Memory,
            "An array is allocated in managed memory.",
            "Try to avoid allocating arrays in frequently-updated code."
            )
        {
            DocumentationUrl = "https://learn.microsoft.com/en-us/dotnet/standard/automatic-memory-management",
            MessageFormat = "'{0}' array allocation",
            DefaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_ParamArrayAllocationDescriptor = new Descriptor
            (
            PAC2005,
            "Param Object Allocation",
            Areas.Memory,
            "A parameters array is allocated in managed memory.",
            "Try to avoid calling this method in frequently-updated code."
            )
        {
            DocumentationUrl = "https://learn.microsoft.com/en-us/dotnet/standard/automatic-memory-management",
            MessageFormat = "Parameters array '{0} {1}' allocation"
        };

        static readonly Descriptor k_StringFormatArrayAllocationDescriptor = new Descriptor
            (
            PAC2006,
            "Array Allocation",
            Areas.Memory,
            "An array is allocated in managed memory. There is a call to string.Format (or an interpolated string) in this method that is using more than 3 format parameters. That may be causing this issue, because it requires an array allocation.",
            "Try to avoid allocating arrays in frequently-updated code."
            )
        {
            DocumentationUrl = "https://learn.microsoft.com/en-us/dotnet/standard/automatic-memory-management",
            MessageFormat = "'{0}' array allocation",
            DefaultSeverity = Severity.Minor
        };

        static readonly int k_ParamArrayAtributeHashCode = "System.ParamArrayAttribute".GetHashCode();

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt,
            OpCodes.Newobj,
            OpCodes.Newarr
        };

        public override IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_ObjectAllocationDescriptor);
            registerDescriptor(k_ClosureAllocationDescriptor);
            registerDescriptor(k_ArrayAllocationDescriptor);
            registerDescriptor(k_ParamArrayAllocationDescriptor);
            registerDescriptor(k_StringFormatArrayAllocationDescriptor);
        }

        public override ReportItemBuilder Analyze(InstructionAnalysisContext context)
        {
            if (context.Instruction.OpCode == OpCodes.Call || context.Instruction.OpCode == OpCodes.Callvirt)
            {
                var callee = (MethodReference)context.Instruction.Operand;
                if (callee.HasParameters)
                {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var lastParam = callee.Parameters.Last();
#pragma warning restore UA2001
                    if (lastParam.HasCustomAttributes && lastParam.CustomAttributes.Exists(a => a.AttributeType.FastFullName().GetHashCode() == k_ParamArrayAtributeHashCode))
                    {
                        // If the previous instruction is loading Array.Empty<T>, we know that the method is being called with no `params`, and we are not actually allocating a managed array for the params array
                        // This looks like:
                        // context.Instruction.Previous: "IL_0001: call !!0[] System.Array::Empty<System.Object>()"
                        bool actuallyPassesArray = true;
                        var lastParamValue = context.Instruction.Previous;
                        if (lastParamValue.OpCode == OpCodes.Call)
                        {
                            var lastParamMethodReference = (MethodReference)lastParamValue.Operand;
                            if (lastParamMethodReference.DeclaringType.FullName == "System.Array" && lastParamMethodReference.Name == "Empty")
                                actuallyPassesArray = false;
                        }
                        if (actuallyPassesArray)
                            return context.CreateIssue(IssueCategory.Code, k_ParamArrayAllocationDescriptor.Id, lastParam.ParameterType.Name, lastParam.Name);
                    }
                }
                return null;
            }

            if (context.Instruction.OpCode == OpCodes.Newobj)
            {
                var methodReference = (MethodReference)context.Instruction.Operand;
                var typeReference = methodReference.DeclaringType;
                if (typeReference.IsValueType)
                    return null;

                var isClosure = typeReference.Name.StartsWith("<>c__DisplayClass", StringComparison.Ordinal);
                if (isClosure)
                {
                    return context.CreateIssue(IssueCategory.Code, k_ClosureAllocationDescriptor.Id, context.MethodDefinition.DeclaringType.Name, context.MethodDefinition.Name);
                }
                else
                {
                    return context.CreateIssue(IssueCategory.Code, k_ObjectAllocationDescriptor.Id, typeReference.FastFullName());
                }
            }
            else // OpCodes.Newarr
            {
                var typeReference = (TypeReference)context.Instruction.Operand;

                // Search for a future call, and see if it is string.Format with an object[] for its second parameter. We can provide a more specific message to the user in this case.
                var next = context.Instruction.Next;
                while (next != null)
                {
                    if (next.OpCode == OpCodes.Call)
                    {
                        var callee = (MethodReference)next.Operand;
                        if (callee.Name == "Format" && callee.DeclaringType.FullName == "System.String")
                        {
                            if (callee.HasParameters && callee.Parameters.Count == 2 && callee.Parameters[1].ParameterType.FullName == "System.Object[]") // Check if the second parameter is the parameter array
                                return context.CreateIssue(IssueCategory.Code, k_StringFormatArrayAllocationDescriptor.Id, typeReference.Name);
                        }
                    }

                    next = next.Next;
                }

                // Object[] allocation
                return context.CreateIssue(IssueCategory.Code, k_ArrayAllocationDescriptor.Id, typeReference.Name);
            }
        }
    }
}
