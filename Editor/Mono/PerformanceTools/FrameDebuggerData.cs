// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditorInternal.FrameDebuggerInternal
{
    // match enum FrameEventType on C++ side!
    // match kFrameEventTypeNames names array!
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
        // ReSharper restore InconsistentNaming
    }

    // Match C++ ScriptingShaderKeywordInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderKeywordInfo
    {
        public string m_Name;
        public int m_Flags;
        public bool m_IsGlobal;
        public bool m_IsDynamic;
    };

    // Match C++ ScriptingShaderFloatInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderFloatInfo
    {
        public string m_Name;
        public int m_Flags;
        public float m_Value;
    }

    // Match C++ ScriptingShaderIntInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderIntInfo
    {
        public string m_Name;
        public int m_Flags;
        public int m_Value;
    }

    // Match C++ ScriptingShaderVectorInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderVectorInfo
    {
        public string m_Name;
        public int m_Flags;
        public Vector4 m_Value;
    }

    // Match C++ ScriptingShaderMatrixInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderMatrixInfo
    {
        public string m_Name;
        public int m_Flags;
        public Matrix4x4 m_Value;
    }

    // Match C++ ScriptingShaderTextureInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderTextureInfo
    {
        public string m_Name;
        public int m_Flags;
        public string m_TextureName;
        public Texture m_Value;
    }

    // Match C++ ScriptingShaderBufferInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderBufferInfo
    {
        public string m_Name;
        public int m_Flags;
    }

    // Match C++ ScriptingShaderBufferInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderConstantBufferInfo
    {
        public string m_Name;
        public int m_Flags;
    }

    // Match C++ ScriptingShaderInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
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

    // Match C++ ScriptingFrameDebuggerEventData memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal class FrameDebuggerEventData
    {
        public int m_FrameEventIndex;
        public int m_VertexCount;
        public int m_IndexCount;
        public int m_InstanceCount;
        public int m_DrawCallCount;
        public string m_OriginalShaderName;
        public string m_RealShaderName;
        public string m_PassName;
        public string m_PassLightMode;
        public int m_ShaderInstanceID;
        public int m_SubShaderIndex;
        public int m_ShaderPassIndex;
        public string shaderKeywords;
        public int m_ComponentInstanceID;
        public Mesh m_Mesh;
        public int m_MeshInstanceID;
        public int m_MeshSubset;
        public int[] m_MeshInstanceIDs;

        // state for compute shader dispatches
        public int m_ComputeShaderInstanceID;
        public string m_ComputeShaderName;
        public string m_ComputeShaderKernelName;
        public int m_ComputeShaderThreadGroupsX;
        public int m_ComputeShaderThreadGroupsY;
        public int m_ComputeShaderThreadGroupsZ;
        public int m_ComputeShaderGroupSizeX;
        public int m_ComputeShaderGroupSizeY;
        public int m_ComputeShaderGroupSizeZ;

        // state for ray tracing shader dispatches
        public int m_RayTracingShaderInstanceID;
        public string m_RayTracingShaderName;
        public string m_RayTracingShaderPassName;
        public string m_RayTracingShaderRayGenShaderName;
        public string m_RayTracingShaderAccelerationStructureName;
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
        public sbyte m_RenderTargetMemoryless;
        public bool m_RenderTargetIsBackBuffer;
        public RenderTexture m_RenderTargetRenderTexture;

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

    // Match C++ MonoFrameDebuggerEvent memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct FrameDebuggerEvent
    {
        public FrameEventType m_Type;
        public UnityEngine.Object m_Obj;
    }

    // Match C++ ScriptingFrameDebuggerBlendState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
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

    // Match C++ ScriptingFrameDebuggerRasterState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FrameDebuggerRasterState
    {
        public CullMode m_CullMode;
        public int m_DepthBias;
        public float m_SlopeScaledDepthBias;
        public bool m_DepthClip;
        public bool m_Conservative;
    }

    // Match C++ ScriptingFrameDebuggerDepthState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FrameDebuggerDepthState
    {
        public int m_DepthWrite;
        public CompareFunction m_DepthFunc;
    }

    // Match C++ ScriptingFrameDebuggerStencilState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
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
}
