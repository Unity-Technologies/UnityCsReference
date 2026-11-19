// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        PrimitiveCount,
        PrimitiveType,
        SubmeshCount,
        LodCount,
        Volume,
        VertexDensity,
        MeshCompression,
        SizeOnDisk,
        Readable,
        Num
    }

    class MeshModule : ModuleWithAnalyzers<MeshModuleAnalyzer>
    {
        static readonly IssueLayout k_MeshLayout = new IssueLayout
        {
            Category = IssueCategory.Mesh,
            Properties =
            [
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Mesh Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.VertexCount), Format = PropertyFormat.Integer, Name = "Vertices", LongName = "Vertex Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.PrimitiveCount), Format = PropertyFormat.Integer, Name = "Primitives", LongName = "Primitive Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.PrimitiveType), Format = PropertyFormat.String, Name = "Topology", LongName = "Primitive Type" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.SubmeshCount), Format = PropertyFormat.Integer, Name = "Submeshes", LongName = "Submesh Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.LodCount), Format = PropertyFormat.Integer, Name = "LODs", LongName = "LOD Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.Volume), Format = PropertyFormat.Float, DecimalPlaces = 2, Name = "Volume", LongName = "Volume (Unit³, cubic units)" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.VertexDensity), Format = PropertyFormat.Float, DecimalPlaces = 2, Name = "Density", LongName = "Vertex Density (Verts/Unit³, Verts per cubic unit)" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.MeshCompression), Format = PropertyFormat.String, Name = "Compression", LongName = "Mesh Compression" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.SizeOnDisk), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Mesh Size" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.Readable), Format = PropertyFormat.Bool, Name = "Readable", LongName = "Readable" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            ]
        };

        public override string Name => "Meshes";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts =>
        [
            k_MeshLayout,
            AssetsModule.k_IssueLayout
        ];

        public override void Initialize()
        {
            base.Initialize();

            AddNumericComparer(MeshProperty.Volume);
            AddNumericComparer(MeshProperty.VertexDensity);
        }

        static void AddNumericComparer(MeshProperty property)
        {
            var propertyType = PropertyTypeUtil.FromCustom(property);
            ProjectIssueExtensions.AddCustomComparer(IssueCategory.Mesh, propertyType,
                (a, b) => ProjectIssueExtensions.StringCompareWithDoubleSupport(
                    a.GetProperty(propertyType), b.GetProperty(propertyType)));
        }

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

                    int primitiveCount = 0;
                    string topologyString = "None";
                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        var indexCount = mesh.GetSubMesh(i).indexCount;
                        var topology = mesh.GetTopology(i);
                        switch (topology)
                        {
                            case MeshTopology.Triangles:
                                primitiveCount += (indexCount / 3);
                                break;
                            case MeshTopology.Quads:
                                primitiveCount += (indexCount / 4);
                                break;
                            case MeshTopology.Lines:
                                primitiveCount += (indexCount / 2);
                                break;
                            case MeshTopology.LineStrip:
                                primitiveCount += System.Math.Max(0, indexCount - 1);
                                break;
                            case MeshTopology.Points:
                            default:
                                primitiveCount += indexCount;
                                break;
                        }

                        if (topologyString == "None")
                            topologyString = topology.ToString();
                        else if (topologyString != "Mixed" && topologyString != topology.ToString())
                            topologyString = "Mixed";
                    }

                    float vol = CalcVolume3DOr2D(mesh.bounds.size);
                    float density = IsZero(vol) ? float.NaN : mesh.vertexCount / vol;
                    issues.Add(context.CreateInsight(IssueCategory.Mesh, meshName)
                        .WithCustomProperties(
                            [
                                mesh.vertexCount,
                                primitiveCount,
                                topologyString,
                                mesh.subMeshCount,
                                mesh.lodCount,
                                vol,
                                density,
                                modelImporter?.meshCompression ?? ModelImporterMeshCompression.Off,
                                size,
                                modelImporter?.isReadable ?? mesh.isReadable
                            ])
                        .WithLocation(assetPath));

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

        private static float CalcVolume3DOr2D(Vector3 size)
        {
            if (IsZero(size.x)) return size.y * size.z;
            if (IsZero(size.y)) return size.x * size.z;
            if (IsZero(size.z)) return size.x * size.y;

            return size.x * size.y * size.z;
        }

        private static bool IsZero(float val) => Mathf.Approximately(val, 0f);

    }
}
