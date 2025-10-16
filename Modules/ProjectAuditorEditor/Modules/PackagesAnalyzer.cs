// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class PackagesAnalyzer : PackagesModuleAnalyzer
    {
        internal const string PAP0001 = nameof(PAP0001);
        internal const string PAP0002 = nameof(PAP0002);

        static readonly Descriptor k_RecommendPackageUpgrade = new Descriptor(
            PAP0001,
            "Newer recommended package version",
            Areas.Quality,
            "A newer recommended version of this package is available.",
            "Update the package via Package Manager."
        )
        {
            MessageFormat = "Package '{0}' could be updated from version '{1}' to '{2}'",
            DefaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_RecommendPackagePreView = new Descriptor(
            PAP0002,
            "Experimental/Preview packages",
            Areas.Quality,
            "Experimental or Preview packages are in the early stages of development and not yet ready for production.",
            "Experimental packages should only be used for testing purposes and to give feedback to Unity."
        )
        {
            MessageFormat = "Package '{0}' version '{1}' is a preview/experimental version"
        };


        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_RecommendPackageUpgrade);
            registerDescriptor(k_RecommendPackagePreView);
        }

        public override IEnumerable<ReportItem> Analyze(PackageAnalysisContext context)
        {
            var package = context.PackageInfo;
            // first check if any package is preview or experimental
            if (package.version.Contains("pre") || package.version.Contains("exp"))
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_RecommendPackagePreView.Id, package.name, package.version)
                    .WithLocation(package.assetPath);
            }
            else
            {
                // if not preview or experimental, check anyway if there is a recommended version available
                var recommendedVersionString = PackageUtils.GetPackageRecommendedVersion(package);
                if (!string.IsNullOrEmpty(package.version) && !string.IsNullOrEmpty(recommendedVersionString))
                {
                    if (!recommendedVersionString.Equals(package.version))
                    {
                        yield return context.CreateIssue(IssueCategory.ProjectSetting, k_RecommendPackageUpgrade.Id, package.name, package.version, recommendedVersionString)
                            .WithLocation(package.assetPath);
                    }
                }
            }
        }
    }
}
