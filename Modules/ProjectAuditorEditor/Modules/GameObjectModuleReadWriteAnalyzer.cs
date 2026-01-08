// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class GameObjectModuleReadWriteAnalyzer : GameObjectModuleAnalyzer
    {
        internal const string PAA6000 = nameof(PAA6000);
        internal const string PAA6001 = nameof(PAA6001);

        internal static readonly Descriptor k_TextureNotReadWriteDescriptor = new Descriptor
            (
            PAA6000,
            "Texture not Read/Write",
            Areas.Quality,
            "A GameObject requires access to the pixel data of this Texture on the CPU. Read/Write must be enabled on the Texture for this to work properly.",
            "Enable Read/Write in the Texture's import settings."
            )
        {
            MessageFormat = "Texture '{0}' used by '{1}' is not marked as Read/Write",
            Fixer = (issue, analysisParams) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.RelativePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.isReadable = true;
                    textureImporter.SaveAndReimport();
                    return true;
                }

                return false;
            }
        };

        internal static readonly Descriptor k_MeshNotReadWriteDescriptor = new Descriptor
            (
            PAA6001,
            "Mesh not Read/Write",
            Areas.Quality,
            "A GameObject requires access to the vertex data of this Mesh on the CPU. Read/Write must be enabled on the Mesh for this to work properly.",
            "Enable Read/Write in the Mesh's import settings."
            )
        {
            MessageFormat = "Mesh '{0}' used by '{1}' is not marked as Read/Write",
            Fixer = (issue, analysisParams) =>
            {
                var modelImporter = AssetImporter.GetAtPath(issue.RelativePath) as ModelImporter;
                if (modelImporter != null)
                {
                    modelImporter.isReadable = true;
                    modelImporter.SaveAndReimport();
                    return true;
                }

                return false;
            }
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_TextureNotReadWriteDescriptor);
            registerDescriptor(k_MeshNotReadWriteDescriptor);
        }

        public override IEnumerable<ReportItemBuilder> Analyze(GameObjectAnalysisContext context)
        {
            // ParticleSystem
            var ps = context.GameObject.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var shape = ps.shape;

                // Shape Texture
                if ((shape.texture != null) && (shape.texture.isReadable == false))
                    yield return CreateTextureIssue(shape.texture, context);

                // Shape Meshes
                if ((shape.mesh != null) && (shape.mesh.isReadable == false))
                    yield return CreateMeshIssue(shape.mesh, context);

                if (shape.meshRenderer != null)
                {
                    var meshFilter = shape.meshRenderer.GetComponent<MeshFilter>();
                    if ((meshFilter != null) && (meshFilter.sharedMesh != null) && (meshFilter.sharedMesh.isReadable == false))
                        yield return CreateMeshIssue(meshFilter.sharedMesh, context);
                }

                if (shape.skinnedMeshRenderer != null)
                {
                    var meshFilter = shape.skinnedMeshRenderer.GetComponent<MeshFilter>();
                    if ((meshFilter != null) && (meshFilter.sharedMesh != null) && (meshFilter.sharedMesh.isReadable == false))
                        yield return CreateMeshIssue(meshFilter.sharedMesh, context);
                }
            }

            // ParticleSystemRenderer
            var psr = context.GameObject.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                if (!psr.enableGPUInstancing)
                {
                    var meshes = new Mesh[psr.meshCount];
                    int meshCount = psr.GetMeshes(meshes);
                    for (int i = 0; i < meshCount; i++)
                    {
                        if ((meshes[i] != null) && (meshes[i].isReadable == false))
                            yield return CreateMeshIssue(meshes[i], context);
                    }
                }
            }

            // Terrain
            var terrain = context.GameObject.GetComponent<Terrain>();
            if (terrain != null)
            {
                var prototypeMeshTextures = new HashSet<Texture>();
                foreach (var prototype in terrain.terrainData.detailPrototypes)
                {
                    if (!prototype.useInstancing)
                    {
                        if (prototype.usePrototypeMesh)
                        {
                            var meshFilter = prototype.prototype.GetComponent<MeshFilter>();
                            if ((meshFilter != null) && (meshFilter.sharedMesh != null) && (meshFilter.sharedMesh.isReadable == false))
                                yield return CreateMeshIssue(meshFilter.sharedMesh, context);

                            if (prototype.prototype.TryGetComponent<MeshRenderer>(out var renderer) && renderer.sharedMaterial != null)
                            {
                                Material mat = renderer.sharedMaterial;
                                var propertyNameIds = mat.GetTexturePropertyNameIDs();
                                foreach (var propertyNameId in propertyNameIds)
                                {
                                    Texture tex = mat.GetTexture(propertyNameId);
                                    if (tex != null && !tex.isReadable && prototypeMeshTextures.Add(tex))
                                        yield return CreateTextureIssue(tex, context);
                                }
                            }
                        }
                        else
                        {
                            if ((prototype.prototypeTexture != null) && (prototype.prototypeTexture.isReadable == false))
                                yield return CreateTextureIssue(prototype.prototypeTexture, context);
                        }
                    }
                }
            }
        }

        ReportItemBuilder CreateTextureIssue(Texture texture, GameObjectAnalysisContext context)
        {
            return context.CreateIssue
            (
                IssueCategory.GameObject,
                k_TextureNotReadWriteDescriptor.Id,
                texture.name,
                context.GameObject.name
            )
            .WithSeverity(Severity.Major)
            .WithLocation(AssetDatabase.GetAssetPath(texture));
        }

        ReportItemBuilder CreateMeshIssue(Mesh mesh, GameObjectAnalysisContext context)
        {
            return context.CreateIssue
            (
                IssueCategory.GameObject,
                k_MeshNotReadWriteDescriptor.Id,
                mesh.name,
                context.GameObject.name
            )
            .WithSeverity(Severity.Major)
            .WithLocation(AssetDatabase.GetAssetPath(mesh));
        }
    }
}
