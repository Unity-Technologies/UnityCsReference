// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace UnityEditorInternal
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
        HybridBatch
        // ReSharper restore InconsistentNaming
    }

    internal enum ShowAdditionalInfo
    {
        ShaderProperties = 0,
        Preview = 1,
    }

    // Match C++ ScriptingShaderFloatInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderFloatInfo : IComparable<ShaderFloatInfo>
    {
        public string name;
        public int flags;
        public float value;

        public int CompareTo(ShaderFloatInfo other)
        {
            return string.Compare(name, other.name);
        }
    }

    // Match C++ ScriptingShaderIntInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderIntInfo : IComparable<ShaderIntInfo>
    {
        public string name;
        public int flags;
        public int value;

        public int CompareTo(ShaderIntInfo other)
        {
            return string.Compare(name, other.name);
        }
    }

    // Match C++ ScriptingShaderVectorInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderVectorInfo : IComparable<ShaderVectorInfo>
    {
        public string name;
        public int flags;
        public Vector4 value;

        public int CompareTo(ShaderVectorInfo other)
        {
            return string.Compare(name, other.name);
        }
    }

    // Match C++ ScriptingShaderMatrixInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderMatrixInfo : IComparable<ShaderMatrixInfo>
    {
        public string name;
        public int flags;
        public Matrix4x4 value;

        public int CompareTo(ShaderMatrixInfo other)
        {
            return string.Compare(name, other.name);
        }
    }

    // Match C++ ScriptingShaderTextureInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderTextureInfo : IComparable<ShaderTextureInfo>
    {
        public string name;
        public int flags;
        public string textureName;
        public Texture value;

        public int CompareTo(ShaderTextureInfo other)
        {
            return string.Compare(name, other.name);
        }
    }

    // Match C++ ScriptingShaderBufferInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderBufferInfo : IComparable<ShaderBufferInfo>
    {
        public string name;
        public int flags;

        public int CompareTo(ShaderBufferInfo other)
        {
            return string.Compare(name, other.name);
        }
    }

    // Match C++ ScriptingShaderBufferInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderConstantBufferInfo : IComparable<ShaderConstantBufferInfo>
    {
        public string name;
        public int flags;

        public int CompareTo(ShaderConstantBufferInfo other)
        {
            return string.Compare(name, other.name);
        }
    }

    // Match C++ ScriptingShaderProperties memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderProperties
    {
        public ShaderFloatInfo[] floats;
        public ShaderIntInfo[] ints;
        public ShaderVectorInfo[] vectors;
        public ShaderMatrixInfo[] matrices;
        public ShaderTextureInfo[] textures;
        public ShaderBufferInfo[] buffers;
        public ShaderConstantBufferInfo[] cbuffers;
    }

    // Match C++ ScriptingFrameDebuggerEventData memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal class FrameDebuggerEventData
    {
        public int frameEventIndex;
        public int vertexCount;
        public int indexCount;
        public int instanceCount;
        public int drawCallCount;
        public string shaderName;
        public string passName;
        public string passLightMode;
        public int shaderInstanceID;
        public int subShaderIndex;
        public int shaderPassIndex;
        public string shaderKeywords;
        public int componentInstanceID;
        public Mesh mesh;
        public int meshInstanceID;
        public int meshSubset;
        public int[] meshInstanceIDs;

        // state for compute shader dispatches
        public int csInstanceID;
        public string csName;
        public string csKernel;
        public int csThreadGroupsX;
        public int csThreadGroupsY;
        public int csThreadGroupsZ;
        public int csGroupSizeX;
        public int csGroupSizeY;
        public int csGroupSizeZ;

        // state for ray tracing shader dispatches
        public int rtsInstanceID;
        public string rtsName;
        public string rtsShaderPassName;
        public string rtsRayGenShaderName;
        public string rtsAccelerationStructureName;
        public int rtsMaxRecursionDepth;
        public int rtsWidth;
        public int rtsHeight;
        public int rtsDepth;
        public int rtsMissShaderCount;
        public int rtsCallableShaderCount;

        // active render target info
        public string rtName;
        public int rtWidth;
        public int rtHeight;
        public int rtFormat;
        public int rtDim;
        public int rtFace;
        public int rtLoadAction;
        public int rtStoreAction;
        public int rtDepthLoadAction;
        public int rtDepthStoreAction;
        public float rtClearColorR;
        public float rtClearColorG;
        public float rtClearColorB;
        public float rtClearColorA;
        public float rtClearDepth;
        public uint rtClearStencil;
        public short rtCount;
        public sbyte rtHasDepthTexture;
        public sbyte rtMemoryless;
        public bool rtIsBackBuffer;
        public Texture rtOutput;

        // shader state
        public FrameDebuggerBlendState blendState;
        public FrameDebuggerRasterState rasterState;
        public FrameDebuggerDepthState depthState;
        public FrameDebuggerStencilState stencilState;
        public int stencilRef;

        // clear event data
        public float clearColorR;
        public float clearColorG;
        public float clearColorB;
        public float clearColorA;
        public float clearDepth;
        public uint clearStencil;

        // shader properties
        public int batchBreakCause;
        public ShaderProperties shaderProperties;
    }

    // Match C++ MonoFrameDebuggerEvent memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct FrameDebuggerEvent
    {
        public FrameEventType type;
        public UnityEngine.Object obj;
    }

    // Match C++ ScriptingFrameDebuggerBlendState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FrameDebuggerBlendState
    {
        public uint writeMask;
        public BlendMode srcBlend;
        public BlendMode dstBlend;
        public BlendMode srcBlendAlpha;
        public BlendMode dstBlendAlpha;
        public BlendOp blendOp;
        public BlendOp blendOpAlpha;
    }

    // Match C++ ScriptingFrameDebuggerRasterState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FrameDebuggerRasterState
    {
        public CullMode cullMode;
        public int depthBias;
        public float slopeScaledDepthBias;
        public bool depthClip;
        public bool conservative;
    }

    // Match C++ ScriptingFrameDebuggerDepthState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FrameDebuggerDepthState
    {
        public int depthWrite;
        public CompareFunction depthFunc;
    }

    // Match C++ ScriptingFrameDebuggerStencilState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FrameDebuggerStencilState
    {
        public bool stencilEnable;
        public byte readMask;
        public byte writeMask;
        public byte padding;
        public CompareFunction stencilFuncFront;
        public StencilOp stencilPassOpFront;
        public StencilOp stencilFailOpFront;
        public StencilOp stencilZFailOpFront;
        public CompareFunction stencilFuncBack;
        public StencilOp stencilPassOpBack;
        public StencilOp stencilFailOpBack;
        public StencilOp stencilZFailOpBack;
    }
}
