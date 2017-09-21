// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;


namespace UnityEngine.Experimental.Rendering
{



[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct ShaderPassName
{
            private int m_NameIndex;
    
            public ShaderPassName(string name)
        {
            m_NameIndex = Init(name);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Init (string name) ;

    internal int nameIndex
        {
            get
            {
                return m_NameIndex;
            }
        }
    
    
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public unsafe partial struct DrawRendererSettings
{
    
            private const int kMaxShaderPasses = 16;
            public static readonly int maxShaderPasses = kMaxShaderPasses;
    
            public DrawRendererSortSettings sorting;
            private fixed int               shaderPassNames[kMaxShaderPasses];
            public RendererConfiguration    rendererConfiguration;
            public DrawRendererFlags        flags;
    
        #pragma warning disable 414
            private int                      m_OverrideMaterialInstanceId;
            private int                      m_OverrideMaterialPassIdx;
        #pragma warning restore 414
    
            public DrawRendererSettings(Camera camera, ShaderPassName shaderPassName)
        {
            rendererConfiguration = RendererConfiguration.None;
            flags = DrawRendererFlags.EnableInstancing;

            m_OverrideMaterialInstanceId = 0;
            m_OverrideMaterialPassIdx = 0;

            fixed(int* p = shaderPassNames)
            {
                for (int i = 0; i < maxShaderPasses; i++)
                {
                    p[i] = -1;
                }
            }

            fixed(int* p = shaderPassNames)
            {
                p[0] = shaderPassName.nameIndex;
            }

            rendererConfiguration = RendererConfiguration.None;
            flags = DrawRendererFlags.EnableInstancing;

            InitializeSortSettings(camera, out sorting);
        }
    
    public void SetOverrideMaterial(Material mat, int passIndex)
        {
            if (mat == null)
                m_OverrideMaterialInstanceId = 0;
            else
                m_OverrideMaterialInstanceId = mat.GetInstanceID();

            m_OverrideMaterialPassIdx = passIndex;
        }
    
    public void SetShaderPassName(int index, ShaderPassName shaderPassName)
        {
            if (index >= maxShaderPasses || index < 0)
                throw new ArgumentOutOfRangeException("index", string.Format("Index should range from 0 - DrawRendererSettings.maxShaderPasses ({0}), was {1}", maxShaderPasses, index));

            fixed(int* p = shaderPassNames)
            {
                p[index] = shaderPassName.nameIndex;
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InitializeSortSettings (Camera camera, out DrawRendererSortSettings sortSettings) ;

}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct ScriptableRenderContext
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Submit_Internal () ;

    private void DrawRenderers_Internal (FilterResults renderers, ref DrawRendererSettings drawSettings, FilterRenderersSettings filterSettings) {
        INTERNAL_CALL_DrawRenderers_Internal ( ref this, ref renderers, ref drawSettings, ref filterSettings );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawRenderers_Internal (ref ScriptableRenderContext self, ref FilterResults renderers, ref DrawRendererSettings drawSettings, ref FilterRenderersSettings filterSettings);
    private void DrawRenderers_StateBlock_Internal (FilterResults renderers, ref DrawRendererSettings drawSettings, FilterRenderersSettings filterSettings, RenderStateBlock stateBlock) {
        INTERNAL_CALL_DrawRenderers_StateBlock_Internal ( ref this, ref renderers, ref drawSettings, ref filterSettings, ref stateBlock );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawRenderers_StateBlock_Internal (ref ScriptableRenderContext self, ref FilterResults renderers, ref DrawRendererSettings drawSettings, ref FilterRenderersSettings filterSettings, ref RenderStateBlock stateBlock);
    private void DrawRenderers_StateMap_Internal (FilterResults renderers, ref DrawRendererSettings drawSettings, FilterRenderersSettings filterSettings, System.Array stateMap, int stateMapLength) {
        INTERNAL_CALL_DrawRenderers_StateMap_Internal ( ref this, ref renderers, ref drawSettings, ref filterSettings, stateMap, stateMapLength );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawRenderers_StateMap_Internal (ref ScriptableRenderContext self, ref FilterResults renderers, ref DrawRendererSettings drawSettings, ref FilterRenderersSettings filterSettings, System.Array stateMap, int stateMapLength);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void DrawShadows_Internal (ref DrawShadowsSettings settings) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void EmitWorldGeometryForSceneView (Camera cullingCamera) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void ExecuteCommandBuffer_Internal (CommandBuffer commandBuffer) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void ExecuteCommandBufferAsync_Internal (CommandBuffer commandBuffer, ComputeQueueType queueType) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetupCameraProperties_Internal (Camera camera, bool stereoSetup) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void StereoEndRender_Internal (Camera camera) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void StartMultiEye_Internal (Camera camera) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void StopMultiEye_Internal (Camera camera) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void DrawSkybox_Internal (Camera camera) ;

    internal IntPtr Internal_GetPtr()
        {
            return m_Ptr;
        }
    
    
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct VisibleLight
{
            public LightType            lightType;
            public Color                finalColor;
            public Rect                 screenRect;
            public Matrix4x4            localToWorld;
            public float                range;
            public float                spotAngle;
            private int                 instanceId;
            public VisibleLightFlags    flags;
    
    
    public Light          light { get { return GetLightObject(instanceId); } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Light GetLightObject (int instanceId) ;

}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct VisibleReflectionProbe
{
            public Bounds bounds;
            public Matrix4x4 localToWorld;
            public Vector4 hdr;
            public Vector3 center;
            public float blendDistance;
            public int importance;
            public int boxProjection;
            private int instanceId;
            private int textureId;
    public Texture texture { get { return GetTextureObject(textureId); } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Texture GetTextureObject (int textureId) ;

    public ReflectionProbe probe { get { return GetReflectionProbeObject(instanceId); } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  ReflectionProbe GetReflectionProbeObject (int instanceId) ;

}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct CullResults
{
    
            public List<VisibleLight>           visibleLights;
            public List<VisibleLight>           visibleOffscreenVertexLights;
            public List<VisibleReflectionProbe> visibleReflectionProbes;
            public FilterResults                visibleRenderers;
            internal IntPtr                 cullResults;
    
    
    private void Init()
        {
            visibleLights = new List<VisibleLight>();
            visibleOffscreenVertexLights = new List<VisibleLight>();
            visibleReflectionProbes = new List<VisibleReflectionProbe>();
            visibleRenderers = default(FilterResults);
            cullResults = IntPtr.Zero;
        }
    
    
    unsafe public static bool GetCullingParameters(Camera camera, out ScriptableCullingParameters cullingParameters)
        {
            return GetCullingParameters_Internal(camera, false, out cullingParameters, sizeof(ScriptableCullingParameters));
        }
    
    
    unsafe public static bool GetCullingParameters(Camera camera, bool stereoAware, out ScriptableCullingParameters cullingParameters)
        {
            return GetCullingParameters_Internal(camera, stereoAware, out cullingParameters, sizeof(ScriptableCullingParameters));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetCullingParameters_Internal (Camera camera, bool stereoAware, out ScriptableCullingParameters cullingParameters, int managedCullingParametersSize) ;

    internal static void Internal_Cull (ref ScriptableCullingParameters parameters, ScriptableRenderContext renderLoop, ref CullResults results) {
        INTERNAL_CALL_Internal_Cull ( ref parameters, ref renderLoop, ref results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_Cull (ref ScriptableCullingParameters parameters, ref ScriptableRenderContext renderLoop, ref CullResults results);
    public static CullResults Cull(ref ScriptableCullingParameters parameters, ScriptableRenderContext renderLoop)
        {
            CullResults res = new CullResults();
            Cull(ref parameters, renderLoop, ref res);
            return res;
        }
    
    
    public static void Cull(ref ScriptableCullingParameters parameters, ScriptableRenderContext renderLoop, ref CullResults results)
        {
            if (results.visibleLights == null
                || results.visibleOffscreenVertexLights == null
                || results.visibleReflectionProbes == null)
            {
                results.Init();
            }
            Internal_Cull(ref parameters, renderLoop, ref results);
        }
    
    
    public static bool Cull(Camera camera, ScriptableRenderContext renderLoop, out CullResults results)
        {
            results.cullResults = IntPtr.Zero;
            results.visibleLights = null;
            results.visibleOffscreenVertexLights = null;
            results.visibleReflectionProbes = null;
            results.visibleRenderers = default(FilterResults);

            ScriptableCullingParameters cullingParams;
            if (!GetCullingParameters(camera, out cullingParams))
                return false;

            results = Cull(ref cullingParams, renderLoop);
            return true;
        }
    
    
    public bool GetShadowCasterBounds(int lightIndex, out Bounds outBounds) { return GetShadowCasterBounds(cullResults, lightIndex, out outBounds); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetShadowCasterBounds (IntPtr cullResults, int lightIndex, out Bounds bounds) ;

    public int GetLightIndicesCount() { return GetLightIndicesCount(cullResults); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetLightIndicesCount (IntPtr cullResults) ;

    public void FillLightIndices(ComputeBuffer computeBuffer) { FillLightIndices(cullResults, computeBuffer); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void FillLightIndices (IntPtr cullResults, ComputeBuffer computeBuffer) ;

    public int[] GetLightIndexMap()
        {
            return GetLightIndexMap(cullResults);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int[] GetLightIndexMap (IntPtr cullResults) ;

    public void SetLightIndexMap(int[] mapping)
        {
            SetLightIndexMap(cullResults, mapping);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetLightIndexMap (IntPtr cullResults, int[] mapping) ;

    public bool ComputeSpotShadowMatricesAndCullingPrimitives(int activeLightIndex,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData)
        {
            return ComputeSpotShadowMatricesAndCullingPrimitives(cullResults, activeLightIndex,
                out viewMatrix, out projMatrix, out shadowSplitData);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool ComputeSpotShadowMatricesAndCullingPrimitives (IntPtr cullResults, int activeLightIndex,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData) ;

    public bool ComputePointShadowMatricesAndCullingPrimitives(int activeLightIndex,
            CubemapFace cubemapFace, float fovBias,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData)
        {
            return ComputePointShadowMatricesAndCullingPrimitives(cullResults, activeLightIndex,
                cubemapFace, fovBias,
                out viewMatrix, out projMatrix, out shadowSplitData);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool ComputePointShadowMatricesAndCullingPrimitives (IntPtr cullResults, int activeLightIndex,
            CubemapFace cubemapFace, float fovBias,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData) ;

    public bool ComputeDirectionalShadowMatricesAndCullingPrimitives(int activeLightIndex,
            int splitIndex, int splitCount, Vector3 splitRatio, int shadowResolution, float shadowNearPlaneOffset,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData)
        {
            return ComputeDirectionalShadowMatricesAndCullingPrimitives(cullResults, activeLightIndex,
                splitIndex, splitCount, splitRatio, shadowResolution, shadowNearPlaneOffset,
                out viewMatrix, out projMatrix, out shadowSplitData);
        }
    
    
    private static bool ComputeDirectionalShadowMatricesAndCullingPrimitives (IntPtr cullResults, int activeLightIndex,
            int splitIndex, int splitCount, Vector3 splitRatio, int shadowResolution, float shadowNearPlaneOffset,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData) {
        return INTERNAL_CALL_ComputeDirectionalShadowMatricesAndCullingPrimitives ( cullResults, activeLightIndex, splitIndex, splitCount, ref splitRatio, shadowResolution, shadowNearPlaneOffset, out viewMatrix, out projMatrix, out shadowSplitData );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ComputeDirectionalShadowMatricesAndCullingPrimitives (IntPtr cullResults, int activeLightIndex, int splitIndex, int splitCount, ref Vector3 splitRatio, int shadowResolution, float shadowNearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
}

}
