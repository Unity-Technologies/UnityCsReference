// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class GameObjectModuleReadWriteAnalyzer : GameObjectModuleAnalyzer
    {
        internal const string PAA6000 = nameof(PAA6000);
        internal const string PAA6001 = nameof(PAA6001);
        internal const string PAA6002 = nameof(PAA6002);

        internal static readonly Descriptor k_TextureNotReadWriteDescriptor = new Descriptor
            (
            PAA6000,
            "Texture requires Read/Write",
            Areas.Quality,
            "A GameObject requires access to the pixel data of this Texture on the CPU. Read/Write must be enabled on the Texture for this to work properly.",
            "Enable Read/Write in the Texture's import settings."
            )
        {
            MessageFormat = "Texture '{0}' used by '{1}' is not marked as Read/Write",
            Fixer = (issue, analysisParams) =>
            {
                if (InternalEditorUtility.IsReadOnlyAsset(issue.RelativePath, out _))
                {
                    Debug.LogWarning($"Cannot fix asset at '{issue.RelativePath}' because it's readonly.");
                    return false;
                }

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
            "Mesh requires Read/Write",
            Areas.Quality | Areas.Upgrade,
            "A GameObject requires access to the vertex data of this Mesh on the CPU. In future versions of Unity, the build process will no longer automatically enable Read/Write for Meshes referenced by components requiring it (Particle System, Terrain...).",
            "Enable Read/Write in the Mesh's import settings."
            )
        {
            MessageFormat = "Mesh '{0}' used by '{1}' is not marked as Read/Write",
            Fixer = (issue, analysisParams) =>
            {
                if (!InternalEditorUtility.CanMeshBeModifiedFromCode(issue.RelativePath))
                {
                    Debug.LogWarning($"Cannot modify Mesh located at '{issue.RelativePath}'. Please fix the Mesh manually or assign a different Mesh to the Game Object.");
                    return false;
                }

                return InternalEditorUtility.ImportMeshAsReadable(issue.RelativePath);
            }
        };

        internal static readonly Descriptor k_SceneMeshReadWriteEnabledDescriptor = new Descriptor(
            PAA6002,
            "Mesh: Read/Write enabled",
            Areas.Memory,
            "The <b>Read/Write Enabled</b> flag is enabled. This causes the mesh data to be duplicated in memory.",
            "If not required, disable the <b>Read/Write Enabled</b> option via script."
        )
        {
            MessageFormat = "Mesh '{0}' used by '{1}' is marked as Read/Write",
            DocumentationUrl = "https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html"
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_TextureNotReadWriteDescriptor);
            registerDescriptor(k_MeshNotReadWriteDescriptor);
            registerDescriptor(k_SceneMeshReadWriteEnabledDescriptor);
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
                    var psMeshFilter = shape.meshRenderer.GetComponent<MeshFilter>();
                    if ((psMeshFilter != null) && (psMeshFilter.sharedMesh != null) && (psMeshFilter.sharedMesh.isReadable == false))
                        yield return CreateMeshIssue(psMeshFilter.sharedMesh, context);
                }

                if (shape.skinnedMeshRenderer != null)
                {
                    var skinnedMesh = shape.skinnedMeshRenderer.sharedMesh;
                    if ((skinnedMesh != null) && (skinnedMesh.isReadable == false))
                        yield return CreateMeshIssue(skinnedMesh, context);
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
                            var terrainMeshFilter = prototype.prototype.GetComponent<MeshFilter>();
                            if ((terrainMeshFilter != null) && (terrainMeshFilter.sharedMesh != null) && (terrainMeshFilter.sharedMesh.isReadable == false))
                                yield return CreateMeshIssue(terrainMeshFilter.sharedMesh, context);

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

            // MeshFilter
            var meshFilter = context.GameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh != null)
                {
                    // Scene meshes, like those created by Polybrush, do not exist in the Assets folder
                    if (!AssetDatabase.Contains(mesh) || InSceneAssetUtility.IsInSceneAsset(mesh))
                    {
                        if (mesh.isReadable)
                            yield return CreateSceneMeshIssue(mesh, context);
                    }
                }
            }

            // SkinnedMeshRenderer
            var smr = context.GameObject.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                var mesh = smr.sharedMesh;
                if (mesh != null)
                {
                    // Scene meshes, like those created by Polybrush, do not exist in the Assets folder
                    if (!AssetDatabase.Contains(mesh) || InSceneAssetUtility.IsInSceneAsset(mesh))
                    {
                        if (mesh.isReadable)
                            yield return CreateSceneMeshIssue(mesh, context);
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

        ReportItemBuilder CreateSceneMeshIssue(Mesh mesh, GameObjectAnalysisContext context)
        {
            return context.CreateIssue
            (
                IssueCategory.GameObject,
                k_SceneMeshReadWriteEnabledDescriptor.Id,
                mesh.name,
                context.GameObject.name
            );
        }
    }
}
