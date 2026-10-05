// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.CompilerMessageAnalyzers
{
    class CodeQualityAnalyzer : CodeModuleCompilerMessageAnalyzer
    {
        internal const string PAC3000 = nameof(PAC3000);

        static readonly Descriptor k_Descriptor = new Descriptor
            (
            PAC3000,
            "Code quality issue",
            Areas.Quality,
            "This issue was reported by one of the .NET <b>code quality</b> analyzer rules (<b>CAxxxx</b>). These rules flag correctness, performance, security and maintainability problems that do not prevent compilation.",
            "Look up the reported rule ID in Microsoft's code quality rules documentation for an explanation of the rule and its suggested fix. Most rules also offer a code fix in your IDE."
            )
        {
            DefaultSeverity = Severity.Minor,
            DocumentationUrl = "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/"
        };

        static readonly Regex s_RegEx = new Regex(@"\bCA\d{4}\b");

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_Descriptor);
        }

        public override IEnumerable<ReportItemBuilder> Analyze(CompilerMessageAnalysisContext context)
        {
            var message = context.Message;
            if (s_RegEx.IsMatch(message.Code))
            {
                yield return context.CreateIssue(IssueCategory.Code, k_Descriptor.Id)
                    .WithDescription($"{message.Code}: {message.Message}");
            }
        }
    }
}
