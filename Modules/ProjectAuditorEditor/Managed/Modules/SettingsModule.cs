// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class SettingsModule : ModuleWithAnalyzers<SettingsModuleAnalyzer>
    {
        internal static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            Category = IssueCategory.ProjectSetting,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyType.Id, Name = "ID", LongName = "Rule ID"},
                new PropertyDefinition { Type = PropertyType.Severity, Format = PropertyFormat.String, Name = "Severity"},
                new PropertyDefinition { Type = PropertyType.Areas, Name = "Areas", LongName = "The areas the issue might have an impact on"},
                new PropertyDefinition { Type = PropertyType.Filename, Name = "System", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.Platform, Name = "Platform"},
                new PropertyDefinition { Type = PropertyType.IsIgnored, Name = "Ignored"},
            }
        };

        public override string Name => "Settings";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[] {k_IssueLayout};

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);
            var context = new SettingsAnalysisContext
            {
                Params = analysisParams
            };

            AsyncProgressState progressState = progress?.Start("Analyzing Settings", analyzers.Length);

            yield return null;

            foreach (var analyzer in analyzers)
            {
                if (AdvanceAsyncProgress(progress, progressState) == false)
                    break;

                #pragma warning disable UAC2001 // Avoid Linq
                var issues = analyzer.Analyze(context).ToArray();
#pragma warning restore UAC2001
                if (issues.Length > 0)
                    analysisParams.OnIncomingIssues(issues);

                yield return null;
            }

            progress?.Clear(progressState);
            analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, 0);
        }
    }
}
