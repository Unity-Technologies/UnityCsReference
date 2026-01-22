// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);
            if (analyzers.Length > 0)
            {
                var context = new AnalysisContext
                {
                    Params = analysisParams
                };

                var allAssetPaths = GetAssetPaths(context);

                AsyncProgressState progressState = progress?.Start("Analyzing Assets", allAssetPaths.Length);

                yield return null;

                foreach (var assetPath in allAssetPaths)
                {
                    if (AdvanceAsyncProgress(progress, progressState, Path.GetFileName(assetPath)) == false)
                        break;

                    if (assetPath.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    var assetAnalysisContext = new AssetAnalysisContext
                    {
                        AssetPath = assetPath,
                        Params = analysisParams
                    };

                    foreach (var analyzer in analyzers)
                    {
                        analysisParams.OnIncomingIssues(analyzer.Analyze(assetAnalysisContext));
                    }

                    yield return null;
                }

                progress?.Clear(progressState);
            }

            analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, 0);
        }
    }
}
