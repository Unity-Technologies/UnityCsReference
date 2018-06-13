// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.Experimental.Rendering
{
    [NativeHeader("Runtime/Export/ScriptableRenderLoop/ScriptableRenderLoop.bindings.h")]
    public struct ShaderPassName
    {
        private int m_NameIndex;

        public ShaderPassName(string name)
        {
            m_NameIndex = Init(name);
        }

        [FreeFunction("ScriptableRenderLoop_Bindings::InitShaderPassName")]
        extern static private int Init(string name);

        internal int nameIndex
        {
            get
            {
                return m_NameIndex;
            }
        }
    }

    // match layout of DrawRendererSettings on C++ side
    [NativeHeader("Runtime/Graphics/ScriptableRenderLoop/ScriptableDrawRenderersUtility.h")]
    public unsafe struct DrawRendererSettings
    {
        private const int kMaxShaderPasses = 16;
        // externals bind to this to avoid recompile
        // as precompiled assemblies inline the const
        public static readonly int maxShaderPasses = kMaxShaderPasses;

        public DrawRendererSortSettings sorting;
        // can't make fixed types private, because then the compiler generates different code which BindinsgGenerator does not handle yet.
        internal fixed int               shaderPassNames[kMaxShaderPasses];
        public RendererConfiguration    rendererConfiguration;
        public DrawRendererFlags        flags;

    #pragma warning disable 414
        private int                      m_OverrideMaterialInstanceId;
        private int                      m_OverrideMaterialPassIdx;
        private int                      useSRPBatcher; // only needed to match native struct
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
            useSRPBatcher = 0;
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

        [FreeFunction("InitializeSortSettings")]
        extern private static void InitializeSortSettings(Camera camera, out DrawRendererSortSettings sortSettings);
    }

    [UsedByNativeCode]
    public struct VisibleLight
    {
        public LightType            lightType;
        public Color                finalColor;
        public Rect                 screenRect;
        public Matrix4x4            localToWorld;
        public float                range;
        public float                spotAngle;
    #pragma warning disable 649
        private int                 instanceId;
    #pragma warning restore 649
        public VisibleLightFlags    flags;

        public Light          light { get { return GetLightObject(instanceId); } }

        [FreeFunction("(Light*)Object::IDToPointer")]
        extern private static Light GetLightObject(int instanceId);
    }

    [UsedByNativeCode]
    public struct VisibleReflectionProbe
    {
        public Bounds bounds;
        public Matrix4x4 localToWorld;
        public Vector4 hdr;
        public Vector3 center;
        public float blendDistance;
        public int importance;
        public int boxProjection;
    #pragma warning disable 649
        private int instanceId;
        private int textureId;
    #pragma warning restore 649
        public Texture texture { get { return GetTextureObject(textureId); } }

        [FreeFunction("(Texture*)Object::IDToPointer")]
        extern private static Texture GetTextureObject(int textureId);

        public ReflectionProbe probe { get { return GetReflectionProbeObject(instanceId); } }

        [FreeFunction("(ReflectionProbe*)Object::IDToPointer")]
        extern private static ReflectionProbe GetReflectionProbeObject(int instanceId);
    }

    [UsedByNativeCode]
    public struct CullResults
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

        [FreeFunction("ScriptableRenderLoop_Bindings::GetCullingParameters_Internal")]
        extern private static bool GetCullingParameters_Internal(Camera camera, bool stereoAware, out ScriptableCullingParameters cullingParameters, int managedCullingParametersSize);

        [FreeFunction("ScriptableRenderLoop_Bindings::Internal_Cull")]
        extern static internal void Internal_Cull(ref ScriptableCullingParameters parameters, ScriptableRenderContext renderLoop, ref CullResults results);

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

        [FreeFunction("ScriptableRenderLoop_Bindings::GetShadowCasterBounds")]
        extern private static bool GetShadowCasterBounds(IntPtr cullResults, int lightIndex, out Bounds bounds);

        public int GetLightIndicesCount() { return GetLightIndicesCount(cullResults); }

        [FreeFunction("ScriptableRenderLoop_Bindings::GetLightIndicesCount")]
        extern private static int GetLightIndicesCount(IntPtr cullResults);

        public void FillLightIndices(ComputeBuffer computeBuffer) { FillLightIndices(cullResults, computeBuffer); }

        [FreeFunction("ScriptableRenderLoop_Bindings::FillLightIndices")]
        extern private static void FillLightIndices(IntPtr cullResults, ComputeBuffer computeBuffer);

        public int[] GetLightIndexMap()
        {
            return GetLightIndexMap(cullResults);
        }

        [FreeFunction("ScriptableRenderLoop_Bindings::GetLightIndexMap")]
        extern private static int[] GetLightIndexMap(IntPtr cullResults);

        public void SetLightIndexMap(int[] mapping)
        {
            SetLightIndexMap(cullResults, mapping);
        }

        [FreeFunction("ScriptableRenderLoop_Bindings::SetLightIndexMap")]
        extern private static void SetLightIndexMap(IntPtr cullResults, int[] mapping);

        public bool ComputeSpotShadowMatricesAndCullingPrimitives(int activeLightIndex,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData)
        {
            return ComputeSpotShadowMatricesAndCullingPrimitives(cullResults, activeLightIndex,
                out viewMatrix, out projMatrix, out shadowSplitData);
        }

        [FreeFunction("ScriptableRenderLoop_Bindings::ComputeSpotShadowMatricesAndCullingPrimitives")]
        extern private static bool ComputeSpotShadowMatricesAndCullingPrimitives(IntPtr cullResults, int activeLightIndex,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);

        public bool ComputePointShadowMatricesAndCullingPrimitives(int activeLightIndex,
            CubemapFace cubemapFace, float fovBias,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData)
        {
            return ComputePointShadowMatricesAndCullingPrimitives(cullResults, activeLightIndex,
                cubemapFace, fovBias,
                out viewMatrix, out projMatrix, out shadowSplitData);
        }

        [FreeFunction("ScriptableRenderLoop_Bindings::ComputePointShadowMatricesAndCullingPrimitives")]
        extern private static bool ComputePointShadowMatricesAndCullingPrimitives(IntPtr cullResults, int activeLightIndex,
            CubemapFace cubemapFace, float fovBias,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);

        public bool ComputeDirectionalShadowMatricesAndCullingPrimitives(int activeLightIndex,
            int splitIndex, int splitCount, Vector3 splitRatio, int shadowResolution, float shadowNearPlaneOffset,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData)
        {
            return ComputeDirectionalShadowMatricesAndCullingPrimitives(cullResults, activeLightIndex,
                splitIndex, splitCount, splitRatio, shadowResolution, shadowNearPlaneOffset,
                out viewMatrix, out projMatrix, out shadowSplitData);
        }

        [FreeFunction("ScriptableRenderLoop_Bindings::ComputeDirectionalShadowMatricesAndCullingPrimitives")]
        extern private static bool ComputeDirectionalShadowMatricesAndCullingPrimitives(IntPtr cullResults, int activeLightIndex,
            int splitIndex, int splitCount, Vector3 splitRatio, int shadowResolution, float shadowNearPlaneOffset,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
    }
}
