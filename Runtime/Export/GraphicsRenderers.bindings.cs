// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/Renderer.h")]
    public partial class Renderer : Component
    {
        extern public bool enabled   {[NativeMethod(Name = "GetEnabled")] get; [NativeMethod(Name = "SetEnabled")] set; }
        extern public bool isVisible {[NativeMethod(Name = "IsVisibleInScene")] get; }

        extern public ShadowCastingMode shadowCastingMode {[NativeMethod(Name = "GetShadowCastingMode")] get; [NativeMethod(Name = "SetShadowCastingMode")] set; }
        extern public bool receiveShadows {[NativeMethod(Name = "GetReceiveShadows")] get; [NativeMethod(Name = "SetReceiveShadows")] set; }
        extern public MotionVectorGenerationMode motionVectorGenerationMode {[NativeMethod(Name = "GetMotionVectorGenerationMode")] get; [NativeMethod(Name = "SetMotionVectorGenerationMode")] set; }
        extern public LightProbeUsage lightProbeUsage {[NativeMethod(Name = "GetLightProbeUsage")] get; [NativeMethod(Name = "SetLightProbeUsage")] set; }
        extern public ReflectionProbeUsage reflectionProbeUsage {[NativeMethod(Name = "GetReflectionProbeUsage")] get; [NativeMethod(Name = "SetReflectionProbeUsage")] set; }
        extern public string sortingLayerName {[NativeMethod(Name = "GetSortingLayerName")] get; [NativeMethod(Name = "SetSortingLayerName")] set; }
        extern public bool allowOcclusionWhenDynamic {[NativeMethod(Name = "GetIsDynamicOccludee")] get; [NativeMethod(Name = "SetIsDynamicOccludee")] set; }
        extern public int sortingLayerID {[NativeMethod(Name = "GetSortingLayerID")] get; [NativeMethod(Name = "SetSortingLayerID")] set; }
        extern public int sortingOrder {[NativeMethod(Name = "GetSortingOrder")] get; [NativeMethod(Name = "SetSortingOrder")] set; }


        [NativeMethod(Name = "GetLightmapIndexInt")] extern private int  GetLightmapIndex(UnityEngineInternal.LightmapType lt);
        [NativeMethod(Name = "SetLightmapIndexInt")] extern private void SetLightmapIndex(int index, UnityEngineInternal.LightmapType lt);

        public int lightmapIndex
        {
            get { return GetLightmapIndex(UnityEngineInternal.LightmapType.StaticLightmap); }
            set { SetLightmapIndex(value, UnityEngineInternal.LightmapType.StaticLightmap); }
        }
        public int realtimeLightmapIndex
        {
            get { return GetLightmapIndex(UnityEngineInternal.LightmapType.DynamicLightmap); }
            set { SetLightmapIndex(value, UnityEngineInternal.LightmapType.DynamicLightmap); }
        }
    }

    [NativeHeader("Runtime/Graphics/TrailRenderer.h")]
    public sealed partial class TrailRenderer : Renderer
    {
        extern public float time                {[NativeMethod(Name = "GetTime")]               get; [NativeMethod(Name = "SetTime")]               set; }
        extern public float startWidth          {[NativeMethod(Name = "GetStartWidth")]         get; [NativeMethod(Name = "SetStartWidth")]         set; }
        extern public float endWidth            {[NativeMethod(Name = "GetEndWidth")]           get; [NativeMethod(Name = "SetEndWidth")]           set; }
        extern public float widthMultiplier     {[NativeMethod(Name = "GetWidthMultiplier")]    get; [NativeMethod(Name = "SetWidthMultiplier")]    set; }
        extern public bool  autodestruct        {[NativeMethod(Name = "GetAutodestruct")]       get; [NativeMethod(Name = "SetAutodestruct")]       set; }
        extern public int   numCornerVertices   {[NativeMethod(Name = "GetNumCornerVertices")]  get; [NativeMethod(Name = "SetNumCornerVertices")]  set; }
        extern public int   numCapVertices      {[NativeMethod(Name = "GetNumCapVertices")]     get; [NativeMethod(Name = "SetNumCapVertices")]     set; }
        extern public float minVertexDistance   {[NativeMethod(Name = "GetMinVertexDistance")]  get; [NativeMethod(Name = "SetMinVertexDistance")]  set; }
        extern public int   positionCount       {[NativeMethod(Name = "GetPositionsCount")]     get; }

        extern public bool generateLightingData {[NativeMethod(Name = "GetGenerateLightingData")] get; [NativeMethod(Name = "SetGenerateLightingData")] set; }

        extern public LineTextureMode textureMode {[NativeMethod(Name = "GetTextureMode")] get; [NativeMethod(Name = "SetTextureMode")] set; }
        extern public LineAlignment   alignment   {[NativeMethod(Name = "GetAlignment")]   get; [NativeMethod(Name = "SetAlignment")]   set; }

        [NativeMethod(Name = "Clear")] extern public void Clear();
    }

    [NativeHeader("Runtime/Graphics/LineRenderer.h")]
    public sealed partial class LineRenderer : Renderer
    {
        extern public float startWidth          {[NativeMethod(Name = "GetStartWidth")]         get; [NativeMethod(Name = "SetStartWidth")]         set; }
        extern public float endWidth            {[NativeMethod(Name = "GetEndWidth")]           get; [NativeMethod(Name = "SetEndWidth")]           set; }
        extern public float widthMultiplier     {[NativeMethod(Name = "GetWidthMultiplier")]    get; [NativeMethod(Name = "SetWidthMultiplier")]    set; }
        extern public int   numCornerVertices   {[NativeMethod(Name = "GetNumCornerVertices")]  get; [NativeMethod(Name = "SetNumCornerVertices")]  set; }
        extern public int   numCapVertices      {[NativeMethod(Name = "GetNumCapVertices")]     get; [NativeMethod(Name = "SetNumCapVertices")]     set; }
        extern public int   positionCount       {[NativeMethod(Name = "GetPositionsCount")]     get; [NativeMethod(Name = "SetPositionsCount")]     set; }
        extern public bool  useWorldSpace       {[NativeMethod(Name = "GetUseWorldSpace")]      get; [NativeMethod(Name = "SetUseWorldSpace")]      set; }
        extern public bool  loop                {[NativeMethod(Name = "GetLoop")]               get; [NativeMethod(Name = "SetLoop")]               set; }

        extern public bool generateLightingData {[NativeMethod(Name = "GetGenerateLightingData")] get; [NativeMethod(Name = "SetGenerateLightingData")] set; }

        extern public LineTextureMode textureMode {[NativeMethod(Name = "GetTextureMode")] get; [NativeMethod(Name = "SetTextureMode")] set; }
        extern public LineAlignment   alignment   {[NativeMethod(Name = "GetAlignment")]   get; [NativeMethod(Name = "SetAlignment")]   set; }

        [NativeMethod(Name = "Simplify")] extern public void Simplify(float tolerance);
    }

    [NativeHeader("Runtime/Graphics/Mesh/SkinnedMeshRenderer.h")]
    public partial class SkinnedMeshRenderer : Renderer
    {
        extern public bool updateWhenOffscreen  {[NativeMethod(Name = "GetUpdateWhenOffscreen")]      get; [NativeMethod(Name = "SetUpdateWhenOffscreen")]      set; }
        extern public bool skinnedMotionVectors {[NativeMethod(Name = "GetSkinnedMeshMotionVectors")] get; [NativeMethod(Name = "SetSkinnedMeshMotionVectors")] set; }

        [NativeMethod(Name = "GetBlendShapeWeight")] extern public float GetBlendShapeWeight(int index);
        [NativeMethod(Name = "SetBlendShapeWeight")] extern public void  SetBlendShapeWeight(int index, float value);
    }
}
