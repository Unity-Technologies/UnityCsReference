// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using System.Collections;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum PackageProperty
    {
        Name = 0,
        Version,
        Source,
        Num
    }

    class PackagesModule : ModuleWithAnalyzers<PackagesModuleAnalyzer>
    {
        static readonly IssueLayout k_PackageLayout = new IssueLayout
        {
            Category = IssueCategory.Package,
            Properties =
            [
                new PropertyDefinition { Type = PropertyType.Description, Name = "Package" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PackageProperty.Name), Format = PropertyFormat.String, Name = "Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PackageProperty.Version), Format = PropertyFormat.String, Name = "Version" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PackageProperty.Source), Format = PropertyFormat.String, Name = "Source", IsDefaultGroup = true },
                new PropertyDefinition { Type = PropertyType.Path, Format = PropertyFormat.String, Name = "Path" }
            ]
        };

        public override string Name => "Packages";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts =>
        [
            k_PackageLayout,
            SettingsModule.k_IssueLayout
        ];

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);
            if (analyzers.Length > 0)
            {
                var packages = PackageUtils.GetClientPackages();
                var packageCount = packages.Length;

                AsyncProgressState progressState = progress?.Start("Analyzing Packages", packageCount);

                yield return null;

                var context = new PackageAnalysisContext
                {
                    Params = analysisParams
                };

                foreach (var package in packages)
                {
                    if (AdvanceAsyncProgress(progress, progressState, package.displayName) == false)
                        break;
                    
                    context.PackageInfo = package;

                    analysisParams.OnIncomingIssues(EnumerateInstalledPackages(context));

                    foreach (var analyzer in analyzers)
                    {
                        analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                    }

                    yield return null;
                }

                progress?.Clear(progressState);
            }

            analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, 0);
        }

        IEnumerable<ReportItem> EnumerateInstalledPackages(PackageAnalysisContext context)
        {
            var package = context.PackageInfo;
            var dependencies = System.Array.ConvertAll(package.dependencies, d => d.name + " [" + d.version + "]");
            var displayName = string.IsNullOrEmpty(package.displayName) ? package.name : package.displayName;
            var node = new PackageDependencyNode(displayName, dependencies);
            yield return context.CreateInsight(IssueCategory.Package, displayName)
                .WithCustomProperties(
                [
                    package.name,
                    package.version,
                    package.source
                ])
                .WithDependencies(node)
                .WithLocation(package.assetPath);
        }
    }
}
