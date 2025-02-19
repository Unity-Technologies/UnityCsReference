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
        extern public ParticleSystemMeshDistribution meshDistribution { get; set; }
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
        extern internal Material oldTrailMaterial { set; }
        extern public bool enableGPUInstancing { get; set; }
        extern public bool allowRoll { get; set; }
        extern public bool freeformStretching { get; set; }
        extern public bool rotateWithStretchDirection { get; set; }
        extern public bool applyActiveColorSpace { get; set; }

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

        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetMeshWeightings", HasExplicitThis = true)]
        extern public int GetMeshWeightings([NotNull][Out] float[] weightings);

        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetMeshWeightings", HasExplicitThis = true)]
        extern public void SetMeshWeightings([NotNull] float[] weightings, int size);
        public void SetMeshWeightings(float[] weightings) { SetMeshWeightings(weightings, weightings.Length); }

        extern public int meshCount { get; }

        public void BakeMesh(Mesh mesh, ParticleSystemBakeMeshOptions options) { BakeMesh(mesh, Camera.main, options); }
        extern public void BakeMesh([NotNull] Mesh mesh, [NotNull] Camera camera, ParticleSystemBakeMeshOptions options);

        public void BakeTrailsMesh(Mesh mesh, ParticleSystemBakeMeshOptions options) { BakeTrailsMesh(mesh, Camera.main, options); }
        extern public void BakeTrailsMesh([NotNull] Mesh mesh, [NotNull] Camera camera, ParticleSystemBakeMeshOptions options);

        internal struct BakeTextureOutput
        {
            [NativeName("first")] internal Texture2D vertices;
            [NativeName("second")] internal Texture2D indices;
        }

        public int BakeTexture(ref Texture2D verticesTexture, ParticleSystemBakeTextureOptions options) { return BakeTexture(ref verticesTexture, Camera.main, options); }
        public int BakeTexture(ref Texture2D verticesTexture, Camera camera, ParticleSystemBakeTextureOptions options)
        {
            if (renderMode == ParticleSystemRenderMode.Mesh)
                throw new System.InvalidOperationException("Baking mesh particles to texture requires supplying an indices texture");

            int indexCount;
            verticesTexture = BakeTextureNoIndicesInternal(verticesTexture, camera, options, out indexCount);
            return indexCount;
        }

        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::BakeTextureNoIndices", HasExplicitThis = true)]
        extern private Texture2D BakeTextureNoIndicesInternal(Texture2D verticesTexture, [NotNull] Camera camera, ParticleSystemBakeTextureOptions options, out int indexCount);

        public int BakeTexture(ref Texture2D verticesTexture, ref Texture2D indicesTexture, ParticleSystemBakeTextureOptions options) { return BakeTexture(ref verticesTexture, ref indicesTexture, Camera.main, options); }
        public int BakeTexture(ref Texture2D verticesTexture, ref Texture2D indicesTexture, Camera camera, ParticleSystemBakeTextureOptions options)
        {
            int indexCount;
            var output = BakeTextureInternal(verticesTexture, indicesTexture, camera, options, out indexCount);
            verticesTexture = output.vertices;
            indicesTexture = output.indices;
            return indexCount;
        }

        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::BakeTexture", HasExplicitThis = true)]
        extern private BakeTextureOutput BakeTextureInternal(Texture2D verticesTexture, Texture2D indicesTexture, [NotNull] Camera camera, ParticleSystemBakeTextureOptions options, out int indexCount);

        public int BakeTrailsTexture(ref Texture2D verticesTexture, ref Texture2D indicesTexture, ParticleSystemBakeTextureOptions options) { return BakeTrailsTexture(ref verticesTexture, ref indicesTexture, Camera.main, options); }

        public int BakeTrailsTexture(ref Texture2D verticesTexture, ref Texture2D indicesTexture, Camera camera, ParticleSystemBakeTextureOptions options)
        {
            int indexCount;
            var output = BakeTrailsTextureInternal(verticesTexture, indicesTexture, camera, options, out indexCount);
            verticesTexture = output.vertices;
            indicesTexture = output.indices;
            return indexCount;
        }

        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::BakeTrailsTexture", HasExplicitThis = true)]
        extern private BakeTextureOutput BakeTrailsTextureInternal(Texture2D verticesTexture, Texture2D indicesTexture, [NotNull] Camera camera, ParticleSystemBakeTextureOptions options, out int indexCount);

        // Vertex streams
        extern public int activeVertexStreamsCount { get; }
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetActiveVertexStreams", HasExplicitThis = true)]
        extern public void SetActiveVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetActiveVertexStreams", HasExplicitThis = true)]
        extern public void GetActiveVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);
        extern public int activeTrailVertexStreamsCount { get; }
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetActiveTrailVertexStreams", HasExplicitThis = true)]
        extern public void SetActiveTrailVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetActiveTrailVertexStreams", HasExplicitThis = true)]
        extern public void GetActiveTrailVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);

        extern internal bool editorEnabled { get; set; }
        extern public bool supportsMeshInstancing { get; }
        extern internal void ConfigureTrailMaterialSlot(bool trailsEnabled);
    }
}
