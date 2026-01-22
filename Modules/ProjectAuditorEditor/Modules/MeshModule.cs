// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum MeshProperty
    {
        VertexCount,
        TriangleCount,
        MeshCompression,
        SizeOnDisk,
        Num
    }

    class MeshModule : ModuleWithAnalyzers<MeshModuleAnalyzer>
    {
        static readonly IssueLayout k_MeshLayout = new IssueLayout
        {
            Category = IssueCategory.Mesh,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Mesh Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.VertexCount), Format = PropertyFormat.String, Name = "Vertex Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.TriangleCount), Format = PropertyFormat.String, Name = "Triangle Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.MeshCompression), Format = PropertyFormat.String, Name = "Compression", LongName = "Mesh Compression" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.SizeOnDisk), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Mesh Size" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        public override string Name => "Meshes";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_MeshLayout,
            AssetsModule.k_IssueLayout
        };

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);

            var context = new MeshAnalysisContext()
            {
                // Importer is set in the loop
                Params = analysisParams
            };

            var assetPaths = GetAssetPathsByFilter("t:mesh, a:assets", context);

            AsyncProgressState progressState = progress?.Start("Analyzing Meshes", assetPaths.Length);

            yield return null;

            var issues = new List<ReportItem>();

            foreach (var assetPath in assetPaths)
            {
                if (AdvanceAsyncProgress(progress, progressState, Path.GetFileName(assetPath)) == false)
                    break;

                var assetImporter = AssetImporter.GetAtPath(assetPath);
                // Not all meshes use the ModelImporter, which is why we just pass the AssetImporter to the analyzers to figure out.
                var modelImporter = assetImporter as ModelImporter;
                context.Importer = assetImporter;

                var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

                foreach (var subAsset in subAssets)
                {
                    var mesh = subAsset as Mesh;
                    if (mesh == null)
                        continue;

                    var meshName = mesh.name;
                    if (string.IsNullOrEmpty(meshName))
                        meshName = Path.GetFileNameWithoutExtension(assetPath);

                    // TODO: the size returned by the profiler is not the exact size on the target platform. Needs to be fixed.
                    var size = Profiler.GetRuntimeMemorySizeLong(mesh);

                    context.Name = meshName;
                    context.Mesh = mesh;
                    context.Size = size;

                    issues.Add(context.CreateInsight(IssueCategory.Mesh, meshName)
                        .WithCustomProperties(
                            new object[((int)MeshProperty.Num)]
                            {
                                mesh.vertexCount,
                                mesh.triangles.Length / 3,
                                modelImporter != null
                                ? modelImporter.meshCompression
                                : ModelImporterMeshCompression.Off,
                                size
                            })
                        .WithLocation(new Location(assetPath)));

                    foreach (var analyzer in analyzers)
                    {
                        analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                    }
                }

                yield return null;
            }

            if (issues.Count > 0)
                context.Params.OnIncomingIssues(issues);

            progress?.Clear(progressState);
            analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, 0);
        }
    }
}
