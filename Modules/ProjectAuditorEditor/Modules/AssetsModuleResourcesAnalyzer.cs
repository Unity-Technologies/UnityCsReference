// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class AssetsModuleResourcesAnalyzer : AssetsModuleAnalyzer
    {
        internal const string PAA3000 = nameof(PAA3000);
        internal const string PAA3001 = nameof(PAA3001);

        static readonly Descriptor k_AssetInResourcesFolderDescriptor = new Descriptor
            (
            PAA3000,
            "Resources folder asset",
            Areas.BuildSize,
            "The <b>Resources folder</b> is a common source of many problems in Unity projects. Improper use of the Resources folder can bloat the size of a project’s build, lead to uncontrollable excessive memory utilization, and significantly increase application startup times.",
            "Use AssetBundles or Addressables when possible."
            )
        {
            DocumentationUrl = "https://docs.unity3d.com/Manual/LoadingResourcesatRuntime.html",
            MessageFormat = "Asset '{0}' is in a Resources folder"
        };

        static readonly Descriptor k_AssetInResourcesFolderDependencyDescriptor = new Descriptor
            (
            PAA3001,
            "Resources folder asset dependency",
            Areas.BuildSize,
            "The <b>Resources folder</b> is a common source of many problems in Unity projects. Improper use of the Resources folder can bloat the size of a project’s build, lead to uncontrollable excessive memory utilization, and significantly increase application startup times.",
            "Use AssetBundles or Addressables when possible."
            )
        {
            DocumentationUrl = "https://docs.unity3d.com/Manual/LoadingResourcesatRuntime.html",
            MessageFormat = "Asset '{0}' is a dependency of a Resources folder asset"
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_AssetInResourcesFolderDescriptor);
            registerDescriptor(k_AssetInResourcesFolderDependencyDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(AssetAnalysisContext context)
        {
            var issues = new List<ReportItem>();
            var assetPathsDict = new Dictionary<string, DependencyNode>();

            if (context.AssetPath.IndexOf("/resources/", StringComparison.OrdinalIgnoreCase) < 0)
                yield break;

            if ((File.GetAttributes(context.AssetPath) & FileAttributes.Directory) == FileAttributes.Directory)
                yield break;

            var root = ProcessResourceAsset(context, context.AssetPath, assetPathsDict, issues, null);
            var dependencies = AssetDatabase.GetDependencies(context.AssetPath, true);
            foreach (var depAssetPath in dependencies)
            {
                // skip self
                if (depAssetPath.Equals(context.AssetPath))
                    continue;

                ProcessResourceAsset(context, depAssetPath, assetPathsDict, issues, root);
            }

            foreach (var issue in issues)
                yield return issue;
        }

        static DependencyNode ProcessResourceAsset(AnalysisContext context,
            string assetPath, Dictionary<string, DependencyNode> assetPathsDict, IList<ReportItem> issues, DependencyNode parent)
        {
            // skip C# scripts
            if (Path.GetExtension(assetPath).Equals(".cs"))
                return null;

            if (assetPathsDict.ContainsKey(assetPath))
            {
                var dep = assetPathsDict[assetPath];
                if (parent != null)
                    dep.AddChild(parent);
                return dep;
            }

            var location = new Location(assetPath);
            var dependencyNode = new AssetDependencyNode
            {
                Location = new Location(assetPath)
            };
            if (parent != null)
                dependencyNode.AddChild(parent);

            var isInResources = assetPath.IndexOf("/resources/", StringComparison.OrdinalIgnoreCase) >= 0;

            issues.Add(context.CreateIssue
                (
                    IssueCategory.AssetIssue,
                    isInResources ? k_AssetInResourcesFolderDescriptor.Id : k_AssetInResourcesFolderDependencyDescriptor.Id,
                    Path.GetFileName(assetPath)
                )
                .WithDependencies(dependencyNode)
                .WithLocation(location));

            assetPathsDict.Add(assetPath, dependencyNode);

            return dependencyNode;
        }
    }
}
