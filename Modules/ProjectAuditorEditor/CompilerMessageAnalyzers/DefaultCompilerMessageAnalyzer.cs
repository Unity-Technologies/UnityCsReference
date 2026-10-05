// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CompilerMessageAnalyzers
{
    class DefaultCompilerMessageAnalyzer : CodeModuleCompilerMessageAnalyzer
    {
        static readonly Regex s_RegEx = new Regex(@"\b(?:UAL|PAR|UAC)\d{4}\b");

        public override IEnumerable<ReportItemBuilder> Analyze(CompilerMessageAnalysisContext context)
        {
            var message = context.Message;
            if (s_RegEx.IsMatch(message.Code))
            {
                var descriptorId = new DescriptorId(message.Code);
                if (!DescriptorLibrary.TryGetDescriptor(descriptorId, out var descriptor)) // Not all messages are handled, eg some UAC codes
                    yield break;

                var issue = context.CreateIssue(IssueCategory.Code, descriptor.Id);

                // Use contextual message if one exists
                if (!string.IsNullOrEmpty(descriptor.MessageFormat))
                    issue = issue.WithDescription(message.Message);

                if ((descriptor.Areas & Areas.Upgrade) != 0)
                    issue = issue.WithUpgradeProperties(Application.unityVersion, null, descriptor.Recommendation); // TODO - extract version info from custom tags

                yield return issue;
            }
        }
    }
}
