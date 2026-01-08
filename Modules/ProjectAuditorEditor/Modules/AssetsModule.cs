// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class AssetsModule : ModuleWithAnalyzers<AssetsModuleAnalyzer>
    {
        internal static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            Category = IssueCategory.AssetIssue,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyType.Severity, Format = PropertyFormat.String, Name = "Severity"},
                new PropertyDefinition { Type = PropertyType.Areas, Format = PropertyFormat.String, Name = "Areas", LongName = "Impacted Areas" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyType.Descriptor, Name = "Descriptor", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.IsIgnored, Name = "Ignored"},
            }
        };

        public override string Name => "Assets";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[] {k_IssueLayout};

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var context = new AnalysisContext
            {
                Params = analysisParams
            };

            var analyzers = GetCompatibleAnalyzers(analysisParams);
            if (analyzers.Length == 0)
                return AnalysisResult.Success;

            var allAssetPaths = GetAssetPaths(context);

            progress?.Start("Finding Assets", "Search in Progress...", allAssetPaths.Length);

            foreach (var assetPath in allAssetPaths)
            {
                progress?.Advance();

                if (assetPath.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                var assetAnalysisContext = new AssetAnalysisContext
                {
                    AssetPath = assetPath,
                    Params = analysisParams
                };

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(assetAnalysisContext));
                }
            }

            progress?.Clear();

            return AnalysisResult.Success;
        }
    }
}
