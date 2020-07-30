// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemRenderer.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemRendererScriptBindings.h")]
    [RequireComponent(typeof(Transform))]
    public sealed partial class ParticleSystemRenderer : Renderer
    {
        [NativeName("RenderAlignment")]
        extern public ParticleSystemRenderSpace alignment { get; set; }
        extern public ParticleSystemRenderMode renderMode { get; set; }
        extern public ParticleSystemSortMode sortMode { get; set; }

        extern public float lengthScale { get; set; }
        extern public float velocityScale { get; set; }
        extern public float cameraVelocityScale { get; set; }

        extern public float normalDirection { get; set; }
        extern public float shadowBias { get; set; }
        extern public float sortingFudge { get; set; }
        extern public float minParticleSize { get; set; }
        extern public float maxParticleSize { get; set; }
        extern public Vector3 pivot { get; set; }
        extern public Vector3 flip { get; set; }
        extern public SpriteMaskInteraction maskInteraction { get; set; }
        extern public Material trailMaterial { get; set; }
        extern public bool enableGPUInstancing { get; set; }
        extern public bool allowRoll { get; set; }

        // Mesh used as particle instead of billboarded texture.
        extern public Mesh mesh
        {
            [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetMesh", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetMesh", HasExplicitThis = true)]
            set;
        }

        [RequiredByNativeCode] // Added to any method to prevent stripping of the class
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetMeshes", HasExplicitThis = true)]
        extern public int GetMeshes([NotNull][Out] Mesh[] meshes);

        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetMeshes", HasExplicitThis = true)]
        extern public void SetMeshes([NotNull] Mesh[] meshes, int size);
        public void SetMeshes(Mesh[] meshes) { SetMeshes(meshes, meshes.Length); }

        extern public int meshCount { get; }

        public void BakeMesh(Mesh mesh, bool useTransform = false) { BakeMesh(mesh, Camera.main, useTransform); }
        extern public void BakeMesh([NotNull] Mesh mesh, [NotNull] Camera camera, bool useTransform = false);

        public void BakeTrailsMesh(Mesh mesh, bool useTransform = false) { BakeTrailsMesh(mesh, Camera.main, useTransform); }
        extern public void BakeTrailsMesh([NotNull] Mesh mesh, [NotNull] Camera camera, bool useTransform = false);

        // Vertex streams
        extern public int activeVertexStreamsCount { get; }
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetActiveVertexStreams", HasExplicitThis = true)]
        extern public void SetActiveVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetActiveVertexStreams", HasExplicitThis = true)]
        extern public void GetActiveVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);

        extern internal bool editorEnabled { get; set; }
        extern public bool supportsMeshInstancing { get; }
    }
}
