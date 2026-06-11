// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace UnityEditorInternal.FrameDebuggerInternal
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct FrameDebuggerBlendState
    {
        public uint m_WriteMask;
        public BlendMode m_SrcBlend;
        public BlendMode m_DstBlend;
        public BlendMode m_SrcBlendAlpha;
        public BlendMode m_DstBlendAlpha;
        public BlendOp m_BlendOp;
        public BlendOp m_BlendOpAlpha;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FrameDebuggerRasterState
    {
        public CullMode m_CullMode;
        public int m_DepthBias;
        public float m_SlopeScaledDepthBias;
        public bool m_DepthClip;
        public bool m_Conservative;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FrameDebuggerDepthState
    {
        public int m_DepthWrite;
        public CompareFunction m_DepthFunc;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FrameDebuggerStencilState
    {
        public bool m_StencilEnable;
        public byte m_ReadMask;
        public byte m_WriteMask;
        public byte m_Padding;
        public CompareFunction m_StencilFuncFront;
        public StencilOp m_StencilPassOpFront;
        public StencilOp m_StencilFailOpFront;
        public StencilOp m_StencilZFailOpFront;
        public CompareFunction m_StencilFuncBack;
        public StencilOp m_StencilPassOpBack;
        public StencilOp m_StencilFailOpBack;
        public StencilOp m_StencilZFailOpBack;
    }

    internal struct ShaderKeywordInfo
    {
        public string m_Name;
        public int m_Flags;
        public bool m_IsGlobal;
        public bool m_IsDynamic;
    };

    internal struct ShaderFloatInfo
    {
        public string m_Name;
        public int m_Flags;
        public float m_Value;
    }

    internal struct ShaderIntInfo
    {
        public string m_Name;
        public int m_Flags;
        public int m_Value;
    }

    internal struct ShaderVectorInfo
    {
        public string m_Name;
        public int m_Flags;
        public Vector4 m_Value;
    }

    internal struct ShaderMatrixInfo
    {
        public string m_Name;
        public int m_Flags;
        public Matrix4x4 m_Value;
    }

    internal struct ShaderTextureInfo
    {
        public string m_Name;
        public int m_Flags;
        public string m_TextureName;
        public Texture m_Value;
    }

    internal struct ShaderBufferInfo
    {
        public string m_Name;
        public int m_Flags;
    }

    internal struct ShaderConstantBufferInfo
    {
        public string m_Name;
        public int m_Flags;
        public ShaderFloatInfo[] m_Floats;
        public ShaderIntInfo[] m_Ints;
        public ShaderVectorInfo[] m_Vectors;
        public ShaderMatrixInfo[] m_Matrices;
    }

    internal struct ShaderInfo
    {
        public ShaderKeywordInfo[] m_Keywords;
        public ShaderFloatInfo[] m_Floats;
        public ShaderIntInfo[] m_Ints;
        public ShaderVectorInfo[] m_Vectors;
        public ShaderMatrixInfo[] m_Matrices;
        public ShaderTextureInfo[] m_Textures;
        public ShaderBufferInfo[] m_Buffers;
        public ShaderConstantBufferInfo[] m_CBuffers;
    }

    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    internal class FrameDebuggerEventData
    {
        public int m_FrameEventIndex;
        public int m_RTDisplayIndex;
        public int m_VertexCount;
        public int m_IndexCount;
        public int m_InstanceCount;
        public int m_DrawCallCount;
        public string m_OriginalShaderName;
        public string m_RealShaderName;
        public string m_PassName;
        public string m_PassLightMode;
        public EntityId m_ShaderEntityId;
        public int m_SubShaderIndex;
        public int m_ShaderPassIndex;
        public string shaderKeywords;
        public EntityId m_ComponentEntityId;

        public Mesh m_Mesh;
        public EntityId m_MeshEntityId;
        public int m_MeshSubset;
        public EntityId[] m_MeshEntityIds;

        // state for compute shader dispatches
        public EntityId m_ComputeShaderEntityId;
        public string m_ComputeShaderName;
        public string m_ComputeShaderKernelName;
        public int m_ComputeShaderThreadGroupsX;
        public int m_ComputeShaderThreadGroupsY;
        public int m_ComputeShaderThreadGroupsZ;
        public int m_ComputeShaderGroupSizeX;
        public int m_ComputeShaderGroupSizeY;
        public int m_ComputeShaderGroupSizeZ;

        // state for ray tracing shader dispatches
        public EntityId m_RayTracingShaderEntityId;
        public bool m_RayTracingIndirectDispatch;
        public string m_RayTracingShaderName;
        public string m_RayTracingShaderPassName;
        public string m_RayTracingShaderRayGenName;
        public string m_RayTracingShaderAccelerationStructureName;
        public int m_RayTracingShaderAccelerationStructureSize;
        public int m_RayTracingShaderMaxRecursionDepth;
        public int m_RayTracingShaderWidth;
        public int m_RayTracingShaderHeight;
        public int m_RayTracingShaderDepth;
        public int m_RayTracingShaderMissShaderCount;
        public int m_RayTracingShaderCallableShaderCount;

        // active render target info
        public string m_RenderTargetName;
        public int m_RenderTargetWidth;
        public int m_RenderTargetHeight;
        public int m_RenderTargetFormat;
        public int m_RenderTargetDimension;
        public int m_RenderTargetCubemapFace;
        public int m_RenderTargetFoveatedRenderingMode;
        public int m_RenderTargetLoadAction;
        public int m_RenderTargetStoreAction;
        public int m_RenderTargetDepthLoadAction;
        public int m_RenderTargetDepthStoreAction;
        public float m_RenderTargetClearColorR;
        public float m_RenderTargetClearColorG;
        public float m_RenderTargetClearColorB;
        public float m_RenderTargetClearColorA;
        public float m_RenderTargetClearDepth;
        public uint m_RenderTargetClearStencil;
        public short m_RenderTargetCount;
        public sbyte m_RenderTargetHasDepthTexture;
        public sbyte m_RenderTargetHasStencilBits;
        public sbyte m_RenderTargetMemoryless;
        public bool m_RenderTargetIsBackBuffer;
        public RenderTexture m_RenderTargetRenderTexture;
        public int m_ShadingRateFragmentSize;
        public int m_ShadingRatePrimitiveCombiner;
        public int m_ShadingRateFragmentCombiner;
        public string m_ShadingRateImageName;
        public Texture m_ShadingRateImageTexture;

        // shader state
        public FrameDebuggerBlendState m_BlendState;
        public FrameDebuggerRasterState m_RasterState;
        public FrameDebuggerDepthState m_DepthState;
        public FrameDebuggerStencilState m_StencilState;
        public int m_StencilRef;

        // clear event data
        public float m_ClearColorR;
        public float m_ClearColorG;
        public float m_ClearColorB;
        public float m_ClearColorA;
        public float m_ClearDepth;
        public uint  m_ClearStencil;

        public int m_BatchBreakCause;
        public ShaderInfo m_ShaderInfo;
    }

    // match enum FrameEventType on C++ side!
    internal enum FrameEventType
    {
        // ReSharper disable InconsistentNaming
        ClearNone = 0,
        ClearColor,
        ClearDepth,
        ClearColorDepth,
        ClearStencil,
        ClearColorStencil,
        ClearDepthStencil,
        ClearAll,
        SetRenderTarget,
        ResolveRT,
        ResolveDepth,
        GrabIntoRT,
        StaticBatch,
        DynamicBatch,
        Mesh,
        DynamicGeometry,
        GLDraw,
        SkinOnGPU,
        DrawProcedural,
        DrawProceduralIndirect,
        DrawProceduralIndexed,
        DrawProceduralIndexedIndirect,
        ComputeDispatch,
        RayTracingDispatch,
        PluginEvent,
        InstancedMesh,
        BeginSubpass,
        SRPBatch,
        HierarchyLevelBreak,
        HybridBatch,
        ConfigureFoveatedRendering,
        SetShadingRateImage,
        // ReSharper restore InconsistentNaming
    }

    internal struct FrameDebuggerEvent
    {
        public FrameEventType m_Type;
        public Object m_Obj;
    }

    [NativeHeader("Runtime/Profiler/PerformanceTools/FrameDebugger.bindings.h")]
    [StaticAccessor("FrameDebugger", StaticAccessorType.DoubleColon)]
    internal sealed class FrameDebuggerUtility
    {
        public extern static void SetEnabled(bool enabled, int remotePlayerGUID);
        public extern static int GetRemotePlayerGUID();
        public extern static bool receivingRemoteFrameEventData { [NativeName("IsReceivingRemoteFrameEventData")] get; }
        public extern static bool locallySupported { [NativeName("IsSupported")] get; }
        [NativeName("FinalDrawCallCount")] public extern static int count { get; }
        [NativeName("DrawCallLimit")] public extern static int limit { get; set; }
        [NativeName("FrameEventsHash")] public extern static int eventsHash { get; }
        [NativeName("FrameEventDataHash")] public extern static uint eventDataHash { get; }
        public extern static void SetRenderTargetDisplayOptions(int rtIndex, Vector4 channels, float blackLevel, float whiteLevel);
        [NativeName("GetProfilerEventName")] public extern static string GetFrameEventInfoName(int index);
        public extern static Object GetFrameEventObject(int index);
        public extern static string[] GetBatchBreakCauseStrings();
        public extern static FrameDebuggerEvent[] GetFrameEvents();

        // Returns false, if frameEventData holds data from previous selected frame
        public extern static bool GetFrameEventData(int index, [Out][NotNull] FrameDebuggerEventData frameDebuggerEventData);

        // Surround a Camera.Render() call with these two methods to replicate the player-loop
        // capture scope (EnterOffscreenRendering / LeaveOffscreenRendering).  Without this
        // surrounding scope, Camera.Render() called outside the player loop does not set
        // m_InGameView, so IsCapturingFrameInfo() returns false and no events are recorded.
        // Intended for use in editor tests only.
        public extern static void EnterCapturingScope();
        public extern static void LeaveCapturingScope();
    }
}
