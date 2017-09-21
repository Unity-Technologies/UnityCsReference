// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

using LT = UnityEngineInternal.LightmapType;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public partial class Renderer : Component
    {
        extern public Bounds bounds {[NativeMethod(Name = "RendererScripting::GetBounds", IsFreeFunction = true, HasExplicitThis = true)] get; }

        [FreeFunction(Name = "RendererScripting::SetStaticLightmapST", HasExplicitThis = true)] extern private void SetStaticLightmapST(Vector4 st);

        [FreeFunction(Name = "RendererScripting::GetMaterial", HasExplicitThis = true)] extern private Material GetMaterial();
        [FreeFunction(Name = "RendererScripting::GetSharedMaterial", HasExplicitThis = true)] extern private Material GetSharedMaterial();
        [FreeFunction(Name = "RendererScripting::SetMaterial", HasExplicitThis = true)] extern private void SetMaterial(Material m);

        [FreeFunction(Name = "RendererScripting::GetMaterialArray", HasExplicitThis = true)] extern private Material[] GetMaterialArray();
        [FreeFunction(Name = "RendererScripting::GetSharedMaterialArray", HasExplicitThis = true)] extern private Material[] GetSharedMaterialArray();
        [FreeFunction(Name = "RendererScripting::SetMaterialArray", HasExplicitThis = true)] extern private void SetMaterialArrayImpl(Material[] m);
    }

    [NativeHeader("Runtime/Graphics/Renderer.h")]
    public partial class Renderer : Component
    {
        extern public bool enabled   { get; set; }
        extern public bool isVisible {[NativeMethod(Name = "IsVisibleInScene")] get; }

        extern public ShadowCastingMode shadowCastingMode { get; set; }
        extern public bool              receiveShadows { get; set; }

        extern public MotionVectorGenerationMode motionVectorGenerationMode { get; set; }
        extern public LightProbeUsage            lightProbeUsage { get; set; }
        extern public ReflectionProbeUsage       reflectionProbeUsage { get; set; }

        extern public   string sortingLayerName  { get; set; }
        extern public   int    sortingLayerID    { get; set; }
        extern public   int    sortingOrder      { get; set; }
        extern internal int    sortingGroupID    { get; set; }
        extern internal int    sortingGroupOrder { get; set; }

        [NativeProperty("IsDynamicOccludee")] extern public bool allowOcclusionWhenDynamic { get; set; }


        [NativeProperty("StaticBatchRoot")] extern internal Transform staticBatchRootTransform { get; set; }
        extern internal int staticBatchIndex { get; }
        extern internal void SetStaticBatchInfo(int firstSubMesh, int subMeshCount);
        extern public bool isPartOfStaticBatch {[NativeMethod(Name = "IsPartOfStaticBatch")] get; }

        extern public Matrix4x4 worldToLocalMatrix { get; }
        extern public Matrix4x4 localToWorldMatrix { get; }


        extern public GameObject lightProbeProxyVolumeOverride { get; set; }
        extern public Transform  probeAnchor { get; set; }

        [NativeMethod(Name = "GetLightmapIndexInt")] extern private int  GetLightmapIndex(LT lt);
        [NativeMethod(Name = "SetLightmapIndexInt")] extern private void SetLightmapIndex(int index, LT lt);
        [NativeMethod(Name = "GetLightmapST")] extern private Vector4 GetLightmapST(LT lt);
        [NativeMethod(Name = "SetLightmapST")] extern private void    SetLightmapST(Vector4 st, LT lt);

        public int lightmapIndex         { get { return GetLightmapIndex(LT.StaticLightmap); }  set { SetLightmapIndex(value, LT.StaticLightmap); } }
        public int realtimeLightmapIndex { get { return GetLightmapIndex(LT.DynamicLightmap); } set { SetLightmapIndex(value, LT.DynamicLightmap); } }

        public Vector4 lightmapScaleOffset         { get { return GetLightmapST(LT.StaticLightmap); }  set { SetStaticLightmapST(value); } }
        public Vector4 realtimeLightmapScaleOffset { get { return GetLightmapST(LT.DynamicLightmap); } set { SetLightmapST(value, LT.DynamicLightmap); } }

        public Material material       { get { return GetMaterial(); }       set { SetMaterial(value); } }
        public Material sharedMaterial { get { return GetSharedMaterial(); } set { SetMaterial(value); } }

        private void SetMaterialArray(Material[] m)
        {
            if (m == null) throw new NullReferenceException("material array is null");
            SetMaterialArrayImpl(m);
        }

        public Material[] materials       { get { return GetMaterialArray(); }       set { SetMaterialArray(value); } }
        public Material[] sharedMaterials { get { return GetSharedMaterialArray(); } set { SetMaterialArray(value); } }
    }

    [NativeHeader("Runtime/Graphics/TrailRenderer.h")]
    public sealed partial class TrailRenderer : Renderer
    {
        extern public float time                { get; set; }
        extern public float startWidth          { get; set; }
        extern public float endWidth            { get; set; }
        extern public float widthMultiplier     { get; set; }
        extern public bool  autodestruct        { get; set; }
        extern public int   numCornerVertices   { get; set; }
        extern public int   numCapVertices      { get; set; }
        extern public float minVertexDistance   { get; set; }

        extern public Color startColor          { get; set; }
        extern public Color endColor            { get; set; }

        [NativeProperty("PositionsCount")] extern public int positionCount { get; }
        extern public Vector3 GetPosition(int index);

        extern public bool generateLightingData { get; set; }

        extern public LineTextureMode textureMode { get; set; }
        extern public LineAlignment   alignment   { get; set; }

        extern public void Clear();
    }

    [NativeHeader("Runtime/Graphics/LineRenderer.h")]
    public sealed partial class LineRenderer : Renderer
    {
        extern public float startWidth          { get; set; }
        extern public float endWidth            { get; set; }
        extern public float widthMultiplier     { get; set; }
        extern public int   numCornerVertices   { get; set; }
        extern public int   numCapVertices      { get; set; }
        extern public bool  useWorldSpace       { get; set; }
        extern public bool  loop                { get; set; }

        extern public Color startColor          { get; set; }
        extern public Color endColor            { get; set; }

        [NativeProperty("PositionsCount")] extern public int positionCount { get; set; }
        extern public void SetPosition(int index, Vector3 position);
        extern public Vector3 GetPosition(int index);

        extern public bool generateLightingData { get; set; }

        extern public LineTextureMode textureMode { get; set; }
        extern public LineAlignment   alignment   { get; set; }

        extern public void Simplify(float tolerance);
    }

    [NativeHeader("Runtime/Graphics/Mesh/SkinnedMeshRenderer.h")]
    public partial class SkinnedMeshRenderer : Renderer
    {
        extern public SkinQuality quality { get; set; }
        extern public bool updateWhenOffscreen  { get; set; }

        extern public Transform rootBone { get; set; }
        extern internal Transform actualRootBone { get; }

        [NativeProperty("Mesh")] extern public Mesh sharedMesh { get; set; }
        [NativeProperty("SkinnedMeshMotionVectors")]  extern public bool skinnedMotionVectors { get; set; }

        extern public float GetBlendShapeWeight(int index);
        extern public void  SetBlendShapeWeight(int index, float value);
        extern public void  BakeMesh(Mesh mesh);

        [FreeFunction(Name = "SkinnedMeshRendererScripting::GetLocalAABB", HasExplicitThis = true)]
        extern private Bounds GetLocalAABB();
        extern private void   SetLocalAABB(Bounds b);

        public Bounds localBounds { get { return GetLocalAABB(); } set { SetLocalAABB(value); } }
    }

    [NativeHeader("Runtime/Graphics/Mesh/MeshRenderer.h")]
    public partial class MeshRenderer : Renderer
    {
        extern public Mesh additionalVertexStreams { get; set; }
    }
}
