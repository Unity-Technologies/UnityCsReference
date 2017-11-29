// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

using LT = UnityEngineInternal.LightmapType;

namespace UnityEngine
{
    [RequireComponent(typeof(Transform))]
    public partial class Renderer : Component
    {
        // called when the object became visible by any camera.
        // void OnBecameVisible();

        // called when the object is no longer visible by any camera.
        // void OnBecameInvisible();
    }

    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public partial class Renderer : Component
    {
        extern public Bounds bounds {[FreeFunction(Name = "RendererScripting::GetBounds", HasExplicitThis = true)] get; }

        [FreeFunction(Name = "RendererScripting::SetStaticLightmapST", HasExplicitThis = true)] extern private void SetStaticLightmapST(Vector4 st);

        [FreeFunction(Name = "RendererScripting::GetMaterial", HasExplicitThis = true)] extern private Material GetMaterial();
        [FreeFunction(Name = "RendererScripting::GetSharedMaterial", HasExplicitThis = true)] extern private Material GetSharedMaterial();
        [FreeFunction(Name = "RendererScripting::SetMaterial", HasExplicitThis = true)] extern private void SetMaterial(Material m);

        [FreeFunction(Name = "RendererScripting::GetMaterialArray", HasExplicitThis = true)] extern private Material[] GetMaterialArray();
        [FreeFunction(Name = "RendererScripting::GetSharedMaterialArray", HasExplicitThis = true)] extern private Material[] GetSharedMaterialArray();
        [FreeFunction(Name = "RendererScripting::SetMaterialArray", HasExplicitThis = true)] extern private void SetMaterialArray([NotNull] Material[] m);

        [FreeFunction(Name = "RendererScripting::SetPropertyBlock", HasExplicitThis = true)] extern internal void Internal_SetPropertyBlock(MaterialPropertyBlock properties);
        [FreeFunction(Name = "RendererScripting::GetPropertyBlock", HasExplicitThis = true)] extern internal void Internal_GetPropertyBlock([NotNull] MaterialPropertyBlock dest);
        [FreeFunction(Name = "RendererScripting::SetPropertyBlockMaterialIndex", HasExplicitThis = true)] extern internal void Internal_SetPropertyBlockMaterialIndex(MaterialPropertyBlock properties, int materialIndex);
        [FreeFunction(Name = "RendererScripting::GetPropertyBlockMaterialIndex", HasExplicitThis = true)] extern internal void Internal_GetPropertyBlockMaterialIndex([NotNull] MaterialPropertyBlock dest, int materialIndex);
        [FreeFunction(Name = "RendererScripting::HasPropertyBlock", HasExplicitThis = true)] extern public bool HasPropertyBlock();

        public void SetPropertyBlock(MaterialPropertyBlock properties) { Internal_SetPropertyBlock(properties); }
        public void SetPropertyBlock(MaterialPropertyBlock properties, int materialIndex) { Internal_SetPropertyBlockMaterialIndex(properties, materialIndex); }
        public void GetPropertyBlock(MaterialPropertyBlock properties) { Internal_GetPropertyBlock(properties); }
        public void GetPropertyBlock(MaterialPropertyBlock properties, int materialIndex) { Internal_GetPropertyBlockMaterialIndex(properties, materialIndex); }

        [FreeFunction(Name = "RendererScripting::GetClosestReflectionProbes", HasExplicitThis = true)] extern private void GetClosestReflectionProbesInternal(object result);
    }

    [NativeHeader("Runtime/Graphics/Renderer.h")]
    public partial class Renderer : Component
    {
        extern public bool enabled   { get; set; }
        extern public bool isVisible {[NativeName("IsVisibleInScene")] get; }

        extern public ShadowCastingMode shadowCastingMode { get; set; }
        extern public bool              receiveShadows { get; set; }

        extern public MotionVectorGenerationMode motionVectorGenerationMode { get; set; }
        extern public LightProbeUsage            lightProbeUsage { get; set; }
        extern public ReflectionProbeUsage       reflectionProbeUsage { get; set; }
        extern public UInt32                     renderingLayerMask { get; set; }

        extern public   string sortingLayerName  { get; set; }
        extern public   int    sortingLayerID    { get; set; }
        extern public   int    sortingOrder      { get; set; }
        extern internal int    sortingGroupID    { get; set; }
        extern internal int    sortingGroupOrder { get; set; }

        [NativeProperty("IsDynamicOccludee")] extern public bool allowOcclusionWhenDynamic { get; set; }


        [NativeProperty("StaticBatchRoot")] extern internal Transform staticBatchRootTransform { get; set; }
        extern internal int staticBatchIndex { get; }
        extern internal void SetStaticBatchInfo(int firstSubMesh, int subMeshCount);
        extern public bool isPartOfStaticBatch {[NativeName("IsPartOfStaticBatch")] get; }

        extern public Matrix4x4 worldToLocalMatrix { get; }
        extern public Matrix4x4 localToWorldMatrix { get; }


        extern public GameObject lightProbeProxyVolumeOverride { get; set; }
        extern public Transform  probeAnchor { get; set; }

        [NativeName("GetLightmapIndexInt")] extern private int  GetLightmapIndex(LT lt);
        [NativeName("SetLightmapIndexInt")] extern private void SetLightmapIndex(int index, LT lt);
        [NativeName("GetLightmapST")] extern private Vector4 GetLightmapST(LT lt);
        [NativeName("SetLightmapST")] extern private void    SetLightmapST(Vector4 st, LT lt);

        public int lightmapIndex         { get { return GetLightmapIndex(LT.StaticLightmap); }  set { SetLightmapIndex(value, LT.StaticLightmap); } }
        public int realtimeLightmapIndex { get { return GetLightmapIndex(LT.DynamicLightmap); } set { SetLightmapIndex(value, LT.DynamicLightmap); } }

        public Vector4 lightmapScaleOffset         { get { return GetLightmapST(LT.StaticLightmap); }  set { SetStaticLightmapST(value); } }
        public Vector4 realtimeLightmapScaleOffset { get { return GetLightmapST(LT.DynamicLightmap); } set { SetLightmapST(value, LT.DynamicLightmap); } }

        // this is needed to extract check for persistent from cpp to cs
        extern internal bool IsPersistent();

        public Material[] materials
        {
            get
            {
                if (IsPersistent())
                {
                    Debug.LogError("Not allowed to access Renderer.materials on prefab object. Use Renderer.sharedMaterials instead", this);
                    return null;
                }
                return GetMaterialArray();
            }
            set { SetMaterialArray(value); }
        }

        public Material material
        {
            get
            {
                if (IsPersistent())
                {
                    Debug.LogError("Not allowed to access Renderer.material on prefab object. Use Renderer.sharedMaterial instead", this);
                    return null;
                }
                return GetMaterial();
            }
            set { SetMaterial(value); }
        }

        public Material sharedMaterial { get { return GetSharedMaterial(); } set { SetMaterial(value); } }
        public Material[] sharedMaterials { get { return GetSharedMaterialArray(); } set { SetMaterialArray(value); } }

        public void GetClosestReflectionProbes(List<ReflectionProbeBlendInfo> result)
        {
            GetClosestReflectionProbesInternal(result);
        }
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

        public AnimationCurve widthCurve    { get { return GetWidthCurveCopy(); }    set { SetWidthCurve(value); } }
        public Gradient       colorGradient { get { return GetColorGradientCopy(); } set { SetColorGradient(value); } }

        // these are direct glue to TrailRenderer methods to simplify properties code (and have null checks generated)

        extern private AnimationCurve GetWidthCurveCopy();
        extern private void SetWidthCurve([NotNull] AnimationCurve curve);

        extern private Gradient GetColorGradientCopy();
        extern private void SetColorGradient([NotNull] Gradient curve);
    }

    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public sealed partial class TrailRenderer : Renderer
    {
        [FreeFunction(Name = "TrailRendererScripting::GetPositions", HasExplicitThis = true)]
        extern public int GetPositions([NotNull][Out] Vector3[] positions);
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

        public AnimationCurve widthCurve    { get { return GetWidthCurveCopy(); }    set { SetWidthCurve(value); } }
        public Gradient       colorGradient { get { return GetColorGradientCopy(); } set { SetColorGradient(value); } }

        // these are direct glue to TrailRenderer methods to simplify properties code (and have null checks generated)

        extern private AnimationCurve GetWidthCurveCopy();
        extern private void SetWidthCurve([NotNull] AnimationCurve curve);

        extern private Gradient GetColorGradientCopy();
        extern private void SetColorGradient([NotNull] Gradient curve);
    }

    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public sealed partial class LineRenderer : Renderer
    {
        [FreeFunction(Name = "LineRendererScripting::GetPositions", HasExplicitThis = true)]
        extern public int GetPositions([NotNull][Out] Vector3[] positions);

        [FreeFunction(Name = "LineRendererScripting::SetPositions", HasExplicitThis = true)]
        extern public void SetPositions([NotNull] Vector3[] positions);
    }

    [NativeHeader("Runtime/Graphics/Mesh/SkinnedMeshRenderer.h")]
    public partial class SkinnedMeshRenderer : Renderer
    {
        extern public SkinQuality quality { get; set; }
        extern public bool updateWhenOffscreen  { get; set; }

        extern public Transform rootBone { get; set; }
        extern internal Transform actualRootBone { get; }
        extern public Transform[] bones { get; set; }

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
        extern public int subMeshStartIndex {[NativeName("GetSubMeshStartIndex")] get; }
    }
}
