// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal enum MaterialProperty
    {
        Shader,
        Num
    }

    class MaterialModule : ModuleWithAnalyzers<MaterialModuleAnalyzer>
    {
        static readonly IssueLayout k_MaterialLayout = new IssueLayout
        {
            Category = IssueCategory.Material,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Name", LongName = "Material Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MaterialProperty.Shader), Format = PropertyFormat.String, Name = "Shader", IsDefaultGroup = true },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Source Asset", MaxAutoWidth = 500 }
            }
        };

        public override string Name => "Materials";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_MaterialLayout,
            AssetsModule.k_IssueLayout
        };

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);

            var platformString = analysisParams.PlatformAsString;

            var context = new MaterialAnalysisContext
            {
                // Importer set in loop
                // ImporterPlatformSettings set in loop
                // Material set in loop
                Params = analysisParams
            };

            var assetPaths = GetAssetPathsByFilter("t:material, a:assets", context);

            progress?.Start("Finding Materials", "Search in Progress...", assetPaths.Length);

            var issues = new List<ReportItem>();

            foreach (var assetPath in assetPaths)
            {
                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                context.Material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                if (string.IsNullOrEmpty(context.Material.name))
                    context.Name = Path.GetFileNameWithoutExtension(assetPath);
                else
                    context.Name = context.Material.name;

                issues.Add(context.CreateInsight(IssueCategory.Material, context.Material.name)
                    .WithCustomProperties(new object[(int)MaterialProperty.Num]
                    {
                        context.Material.shader.name
                    })
                    .WithLocation(new Location(assetPath)));

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            if (issues.Count > 0)
                context.Params.OnIncomingIssues(issues);

            progress?.Clear();

            return AnalysisResult.Success;
        }
    }
}
