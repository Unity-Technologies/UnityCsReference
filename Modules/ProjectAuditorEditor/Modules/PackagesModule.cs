// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.PackageManager;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;

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

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);
            if (analyzers.Length == 0)
                return AnalysisResult.Success;

            var packages = PackageUtils.GetClientPackages();
            var packageCount = packages.Length;

            progress?.Start("Finding Packages", "Search in Progress...", packageCount);

            var context = new PackageAnalysisContext
            {
                Params = analysisParams
            };

            foreach (var package in packages)
            {
                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                progress?.Advance(package.displayName);

                context.PackageInfo = package;

                analysisParams.OnIncomingIssues(EnumerateInstalledPackages(context));

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }
            }

            progress?.Clear();
            return AnalysisResult.Success;
        }

        IEnumerable<ReportItem> EnumerateInstalledPackages(PackageAnalysisContext context)
        {
            var package = context.PackageInfo;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var dependencies = package.dependencies.Select(d => d.name + " [" + d.version + "]").ToArray();
#pragma warning restore RS0030
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
