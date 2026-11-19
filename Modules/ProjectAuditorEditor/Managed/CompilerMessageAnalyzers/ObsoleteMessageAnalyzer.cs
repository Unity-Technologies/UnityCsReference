// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CompilerMessageAnalyzers
{
    class ObsoleteMessageAnalyzer : CodeModuleCompilerMessageAnalyzer
    {
        //public override void Initialize(Action<Descriptor> registerDescriptor)
        //{
        //    registerDescriptor(k_Descriptor); // Registered by the ObsoleteAttributeAnalyzer
        //}

        public override IEnumerable<ReportItemBuilder> Analyze(CompilerMessageAnalysisContext context)
        {
            var message = context.Message;
            bool warning = message.Code == "CS0618";
            bool error = message.Code == "CS0619";

            if (warning || error)
            {
                yield return context.CreateIssue(IssueCategory.Code, ObsoleteAttributeAnalyzer.k_ObsoleteAttributeIssueDescriptor.Id)
                    .WithSeverity(error ? Severity.Major : Severity.Moderate)
                    .WithDescription(message.Message)
                    .WithUpgradeProperties(Application.unityVersion, null, message.Message);
            }
        }
    }
}
