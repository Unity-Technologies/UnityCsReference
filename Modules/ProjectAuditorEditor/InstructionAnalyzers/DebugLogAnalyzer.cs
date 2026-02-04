// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class DebugLogAnalyzer : CodeModuleInstructionAnalyzer
    {
        static readonly int k_ModuleHashCode = "UnityEngine.CoreModule.dll".GetHashCode();
        static readonly int k_TypeHashCode = "UnityEngine.Debug".GetHashCode();
        static readonly int k_ConditionalAttributeHashCode = "System.Diagnostics.ConditionalAttribute".GetHashCode();

        internal const string PAC0192 = nameof(PAC0192);
        internal const string PAC0193 = nameof(PAC0193);

        static readonly Descriptor k_DebugLogIssueDescriptor = new Descriptor
            (
            PAC0192,
            "Debug.Log / Debug.LogFormat",
            Areas.CPU,
            "<b>Debug.Log</b> methods take a lot of CPU time, especially if used frequently.",
            "Remove logging code, or strip it from release builds by using scripting symbols for conditional compilation (#if ... #endif) or the <b>ConditionalAttribute</b> on a custom logging method that calls Debug.Log. Where logging is required in release builds, CPU times can be reduced by disabling stack traces in log messages. You can do this by setting <b>Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None)</b>."
            )
        {
            DocumentationUrl = "https://docs.unity3d.com/Manual/UnderstandingPerformanceGeneralOptimizations.html",
            MessageFormat = "Use of Debug.{0} in '{1}'",
            DefaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_DebugLogWarningIssueDescriptor = new Descriptor
            (
            PAC0193,
            "Debug.LogWarning / Debug.LogWarningFormat",
            Areas.CPU,
            "<b>Debug.LogWarning</b> methods take a lot of CPU time, especially if used frequently.",
            "Remove logging code, or strip it from release builds by using scripting symbols for conditional compilation (#if ... #endif) or the <b>ConditionalAttribute</b> on a custom logging method that calls Debug.LogWarning. Where logging is required in release builds, CPU times can be reduced by disabling stack traces in log messages. You can do this by setting <b>Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None)</b>."
            )
        {
            DocumentationUrl = "https://docs.unity3d.com/Manual/UnderstandingPerformanceGeneralOptimizations.html",
            MessageFormat = "Use of Debug.{0} in '{1}'",
            DefaultSeverity = Severity.Minor
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt
        };

        public override IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_DebugLogIssueDescriptor);
            registerDescriptor(k_DebugLogWarningIssueDescriptor);
        }

        public override ReportItemBuilder Analyze(InstructionAnalysisContext context)
        {
            var callee = (MethodReference)context.Instruction.Operand;
            var methodName = callee.Name;
            var declaringType = callee.DeclaringType;

            if (k_TypeHashCode != declaringType.FastFullName().GetHashCode())
                return null;

            // second check on module name which requires resolving the type
            try
            {
                var typeDefinition = declaringType.Resolve();
                if (typeDefinition == null)
                {
                    Debug.LogWarning(declaringType.FullName + " could not be resolved.");
                    return null;
                }

                if (k_ModuleHashCode != typeDefinition.Module.Name.GetHashCode())
                    return null;
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarningFormat("Could not resolve {0}: {1}", declaringType.Name, e.Message);
            }

            // If we find the ConditionalAttribute, we assume this is intended to be compiled out on release
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (context.MethodDefinition.HasCustomAttributes && context.MethodDefinition.CustomAttributes.Any(a =>
#pragma warning restore UA2001
                a.AttributeType.FullName.GetHashCode() == k_ConditionalAttributeHashCode))
            {
                return null;
            }

            switch (methodName)
            {
                case "Log":
                case "LogFormat":
                    return context.CreateIssue(IssueCategory.Code, k_DebugLogIssueDescriptor.Id, methodName, context.MethodDefinition.Name);
                case "LogWarning":
                case "LogWarningFormat":
                    return context.CreateIssue(IssueCategory.Code, k_DebugLogWarningIssueDescriptor.Id, methodName, context.MethodDefinition.Name);
                default:
                    return null;
            }
        }
    }
}
