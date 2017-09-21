// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{


public enum ComputeQueueType
{
    Default = 0,
    Background = 1,
    Urgent = 2
}

[UsedByNativeCode]
public sealed partial class CommandBuffer : IDisposable
{
    #pragma warning disable 414
    internal IntPtr m_Ptr;
    #pragma warning restore 414
    
    
            ~CommandBuffer()
        {
            Dispose(false);
        }
    
    
    public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    
    private void Dispose(bool disposing)
        {

            ReleaseBuffer();
            m_Ptr = IntPtr.Zero;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InitBuffer (CommandBuffer buf) ;

    private IntPtr CreateGPUFence_Internal (SynchronisationStage stage) {
        IntPtr result;
        INTERNAL_CALL_CreateGPUFence_Internal ( this, stage, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CreateGPUFence_Internal (CommandBuffer self, SynchronisationStage stage, out IntPtr value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void WaitOnGPUFence_Internal (IntPtr fencePtr, SynchronisationStage stage) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void ReleaseBuffer () ;

    public CommandBuffer()
        {
            m_Ptr = IntPtr.Zero;
            InitBuffer(this);
        }
    
    
    public void Release()
        {
            Dispose();
        }
    
    
    [uei.ExcludeFromDocs]
public GPUFence CreateGPUFence () {
    SynchronisationStage stage = SynchronisationStage.PixelProcessing;
    return CreateGPUFence ( stage );
}

public GPUFence CreateGPUFence( [uei.DefaultValue("SynchronisationStage.PixelProcessing")] SynchronisationStage stage )
        {
            GPUFence newFence = new GPUFence();
            newFence.m_Ptr = CreateGPUFence_Internal(stage);
            newFence.InitPostAllocation();
            newFence.Validate();
            return newFence;
        }

    
    
    [uei.ExcludeFromDocs]
public void WaitOnGPUFence (GPUFence fence) {
    SynchronisationStage stage = SynchronisationStage.VertexProcessing;
    WaitOnGPUFence ( fence, stage );
}

public void WaitOnGPUFence(GPUFence fence, [uei.DefaultValue("SynchronisationStage.VertexProcessing")]  SynchronisationStage stage )
        {
            fence.Validate();

            if (fence.IsFencePending())
                WaitOnGPUFence_Internal(fence.m_Ptr, stage);
        }

    
    
    public void SetComputeFloatParam(ComputeShader computeShader, string name, float val)
        {
            SetComputeFloatParam(computeShader, Shader.PropertyToID(name), val);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetComputeFloatParam (ComputeShader computeShader, int nameID, float val) ;

    public void SetComputeIntParam(ComputeShader computeShader, string name, int val)
        {
            SetComputeIntParam(computeShader, Shader.PropertyToID(name), val);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetComputeIntParam (ComputeShader computeShader, int nameID, int val) ;

    public void SetComputeVectorParam(ComputeShader computeShader, string name, Vector4 val)
        {
            SetComputeVectorParam(computeShader, Shader.PropertyToID(name), val);
        }
    
    
    public void SetComputeVectorParam (ComputeShader computeShader, int nameID, Vector4 val) {
        INTERNAL_CALL_SetComputeVectorParam ( this, computeShader, nameID, ref val );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetComputeVectorParam (CommandBuffer self, ComputeShader computeShader, int nameID, ref Vector4 val);
    public void SetComputeVectorArrayParam(ComputeShader computeShader, string name, Vector4[] values)
        {
            SetComputeVectorArrayParam(computeShader, Shader.PropertyToID(name), values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetComputeVectorArrayParam (ComputeShader computeShader, int nameID, Vector4[] values) ;

    public void SetComputeMatrixParam(ComputeShader computeShader, string name, Matrix4x4 val)
        {
            SetComputeMatrixParam(computeShader, Shader.PropertyToID(name), val);
        }
    
    
    public void SetComputeMatrixParam (ComputeShader computeShader, int nameID, Matrix4x4 val) {
        INTERNAL_CALL_SetComputeMatrixParam ( this, computeShader, nameID, ref val );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetComputeMatrixParam (CommandBuffer self, ComputeShader computeShader, int nameID, ref Matrix4x4 val);
    public void SetComputeMatrixArrayParam(ComputeShader computeShader, string name, Matrix4x4[] values)
        {
            SetComputeMatrixArrayParam(computeShader, Shader.PropertyToID(name), values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetComputeMatrixArrayParam (ComputeShader computeShader, int nameID, Matrix4x4[] values) ;

    public void SetComputeFloatParams(ComputeShader computeShader, string name, params float[] values)
        {
            Internal_SetComputeFloats(computeShader, Shader.PropertyToID(name), values);
        }
    
    
    public void SetComputeFloatParams(ComputeShader computeShader, int nameID, params float[] values)
        {
            Internal_SetComputeFloats(computeShader, nameID, values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetComputeFloats (ComputeShader computeShader, int nameID, float[] values) ;

    public void SetComputeIntParams(ComputeShader computeShader, string name, params int[] values)
        {
            Internal_SetComputeInts(computeShader, Shader.PropertyToID(name), values);
        }
    
    
    public void SetComputeIntParams(ComputeShader computeShader, int nameID, params int[] values)
        {
            Internal_SetComputeInts(computeShader, nameID, values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetComputeInts (ComputeShader computeShader, int nameID, int[] values) ;

    public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, string name, RenderTargetIdentifier rt)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, Shader.PropertyToID(name), ref rt);
        }
    
    
    public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, int nameID, RenderTargetIdentifier rt)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, nameID, ref rt);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetComputeTextureParam (ComputeShader computeShader, int kernelIndex, int nameID, ref UnityEngine.Rendering.RenderTargetIdentifier rt) ;

    public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, string name, ComputeBuffer buffer)
        {
            SetComputeBufferParam(computeShader, kernelIndex, Shader.PropertyToID(name), buffer);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetComputeBufferParam (ComputeShader computeShader, int kernelIndex, int nameID, ComputeBuffer buffer) ;

    public void DispatchCompute(ComputeShader computeShader, int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ)
        {
            Internal_DispatchCompute(computeShader, kernelIndex, threadGroupsX, threadGroupsY, threadGroupsZ);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_DispatchCompute (ComputeShader computeShader, int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ) ;

    public void DispatchCompute(ComputeShader computeShader, int kernelIndex, ComputeBuffer indirectBuffer, uint argsOffset)
        {
            Internal_DispatchComputeIndirect(computeShader, kernelIndex, indirectBuffer, argsOffset);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_DispatchComputeIndirect (ComputeShader computeShader, int kernelIndex, ComputeBuffer indirectBuffer, uint argsOffset) ;

    public void GenerateMips(RenderTexture rt)
        {
            if (rt == null)
                throw new ArgumentNullException("rt");

            Internal_GenerateMips(rt);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_GenerateMips (RenderTexture rt) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void CopyCounterValue (ComputeBuffer src, ComputeBuffer dst, uint dstOffsetBytes) ;

    public extern  string name
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  int sizeInBytes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Clear () ;

    [uei.ExcludeFromDocs]
public void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex , int shaderPass ) {
    MaterialPropertyBlock properties = null;
    DrawMesh ( mesh, matrix, material, submeshIndex, shaderPass, properties );
}

[uei.ExcludeFromDocs]
public void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex ) {
    MaterialPropertyBlock properties = null;
    int shaderPass = -1;
    DrawMesh ( mesh, matrix, material, submeshIndex, shaderPass, properties );
}

[uei.ExcludeFromDocs]
public void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material) {
    MaterialPropertyBlock properties = null;
    int shaderPass = -1;
    int submeshIndex = 0;
    DrawMesh ( mesh, matrix, material, submeshIndex, shaderPass, properties );
}

public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, [uei.DefaultValue("0")]  int submeshIndex , [uei.DefaultValue("-1")]  int shaderPass , [uei.DefaultValue("null")]  MaterialPropertyBlock properties )
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
            {
                submeshIndex = Mathf.Clamp(submeshIndex, 0, mesh.subMeshCount - 1);
                Debug.LogWarning(String.Format("submeshIndex out of range. Clampped to {0}.", submeshIndex));
            }
            if (material == null)
                throw new ArgumentNullException("material");
            Internal_DrawMesh(mesh, matrix, material, submeshIndex, shaderPass, properties);
        }

    
    
    private void Internal_DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass, MaterialPropertyBlock properties) {
        INTERNAL_CALL_Internal_DrawMesh ( this, mesh, ref matrix, material, submeshIndex, shaderPass, properties );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawMesh (CommandBuffer self, Mesh mesh, ref Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass, MaterialPropertyBlock properties);
    [uei.ExcludeFromDocs]
public void DrawRenderer (Renderer renderer, Material material, int submeshIndex ) {
    int shaderPass = -1;
    DrawRenderer ( renderer, material, submeshIndex, shaderPass );
}

[uei.ExcludeFromDocs]
public void DrawRenderer (Renderer renderer, Material material) {
    int shaderPass = -1;
    int submeshIndex = 0;
    DrawRenderer ( renderer, material, submeshIndex, shaderPass );
}

public void DrawRenderer(Renderer renderer, Material material, [uei.DefaultValue("0")]  int submeshIndex , [uei.DefaultValue("-1")]  int shaderPass )
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            if (submeshIndex < 0)
            {
                submeshIndex = Mathf.Max(submeshIndex, 0);
                Debug.LogWarning(String.Format("submeshIndex out of range. Clampped to {0}.", submeshIndex));
            }
            if (material == null)
                throw new ArgumentNullException("material");
            Internal_DrawRenderer(renderer, material, submeshIndex, shaderPass);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_DrawRenderer (Renderer renderer, Material material, [uei.DefaultValue("0")]  int submeshIndex , [uei.DefaultValue("-1")]  int shaderPass ) ;

    [uei.ExcludeFromDocs]
    private void Internal_DrawRenderer (Renderer renderer, Material material, int submeshIndex ) {
        int shaderPass = -1;
        Internal_DrawRenderer ( renderer, material, submeshIndex, shaderPass );
    }

    [uei.ExcludeFromDocs]
    private void Internal_DrawRenderer (Renderer renderer, Material material) {
        int shaderPass = -1;
        int submeshIndex = 0;
        Internal_DrawRenderer ( renderer, material, submeshIndex, shaderPass );
    }

    [uei.ExcludeFromDocs]
public void DrawProcedural (Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount ) {
    MaterialPropertyBlock properties = null;
    DrawProcedural ( matrix, material, shaderPass, topology, vertexCount, instanceCount, properties );
}

[uei.ExcludeFromDocs]
public void DrawProcedural (Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount) {
    MaterialPropertyBlock properties = null;
    int instanceCount = 1;
    DrawProcedural ( matrix, material, shaderPass, topology, vertexCount, instanceCount, properties );
}

public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, [uei.DefaultValue("1")]  int instanceCount , [uei.DefaultValue("null")]  MaterialPropertyBlock properties )
        {
            if (material == null)
                throw new ArgumentNullException("material");
            Internal_DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
        }

    
    
    private void Internal_DrawProcedural (Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount, MaterialPropertyBlock properties) {
        INTERNAL_CALL_Internal_DrawProcedural ( this, ref matrix, material, shaderPass, topology, vertexCount, instanceCount, properties );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawProcedural (CommandBuffer self, ref Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount, MaterialPropertyBlock properties);
    [uei.ExcludeFromDocs]
public void DrawProceduralIndirect (Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset ) {
    MaterialPropertyBlock properties = null;
    DrawProceduralIndirect ( matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties );
}

[uei.ExcludeFromDocs]
public void DrawProceduralIndirect (Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs) {
    MaterialPropertyBlock properties = null;
    int argsOffset = 0;
    DrawProceduralIndirect ( matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties );
}

public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, [uei.DefaultValue("0")]  int argsOffset , [uei.DefaultValue("null")]  MaterialPropertyBlock properties )
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");
            Internal_DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
        }

    
    
    private void Internal_DrawProceduralIndirect (Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties) {
        INTERNAL_CALL_Internal_DrawProceduralIndirect ( this, ref matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawProceduralIndirect (CommandBuffer self, ref Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties);
    [uei.ExcludeFromDocs]
public void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count ) {
    MaterialPropertyBlock properties = null;
    DrawMeshInstanced ( mesh, submeshIndex, material, shaderPass, matrices, count, properties );
}

[uei.ExcludeFromDocs]
public void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices) {
    MaterialPropertyBlock properties = null;
    int count = matrices.Length;
    DrawMeshInstanced ( mesh, submeshIndex, material, shaderPass, matrices, count, properties );
}

public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, [uei.DefaultValue("matrices.Length")]  int count , [uei.DefaultValue("null")]  MaterialPropertyBlock properties )
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("DrawMeshInstanced is not supported.");
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            if (material == null)
                throw new ArgumentNullException("material");
            if (matrices == null)
                throw new ArgumentNullException("matrices");
            if (count < 0 || count > Mathf.Min(Graphics.kMaxDrawMeshInstanceCount, matrices.Length))
                throw new ArgumentOutOfRangeException("count", String.Format("Count must be in the range of 0 to {0}.", Mathf.Min(Graphics.kMaxDrawMeshInstanceCount, matrices.Length)));
            if (count > 0)
                Internal_DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, count, properties);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties) ;

    [uei.ExcludeFromDocs]
public void DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, int argsOffset ) {
    MaterialPropertyBlock properties = null;
    DrawMeshInstancedIndirect ( mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, properties );
}

[uei.ExcludeFromDocs]
public void DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs) {
    MaterialPropertyBlock properties = null;
    int argsOffset = 0;
    DrawMeshInstancedIndirect ( mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, properties );
}

public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, [uei.DefaultValue("0")]  int argsOffset , [uei.DefaultValue("null")]  MaterialPropertyBlock properties )
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("Instancing is not supported.");
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            if (material == null)
                throw new ArgumentNullException("material");
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");
            Internal_DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, properties);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties) ;

    public void SetRenderTarget(RenderTargetIdentifier rt)
        {
            SetRenderTarget_Single(ref rt, 0, CubemapFace.Unknown, 0);
        }
    
    
    public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel)
        {
            SetRenderTarget_Single(ref rt, mipLevel, CubemapFace.Unknown, 0);
        }
    
    
    public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace)
        {
            SetRenderTarget_Single(ref rt, mipLevel, cubemapFace, 0);
        }
    
    
    public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        {
            SetRenderTarget_Single(ref rt, mipLevel, cubemapFace, depthSlice);
        }
    
    
    public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth)
        {
            SetRenderTarget_ColDepth(ref color, ref depth, 0, CubemapFace.Unknown, 0);
        }
    
    
    public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth, int mipLevel)
        {
            SetRenderTarget_ColDepth(ref color, ref depth, mipLevel, CubemapFace.Unknown, 0);
        }
    
    
    public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace)
        {
            SetRenderTarget_ColDepth(ref color, ref depth, mipLevel, cubemapFace, 0);
        }
    
    
    public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        {
            SetRenderTarget_ColDepth(ref color, ref depth, mipLevel, cubemapFace, depthSlice);
        }
    
    
    public void SetRenderTarget(RenderTargetIdentifier[] colors, RenderTargetIdentifier depth)
        {
            SetRenderTarget_Multiple(colors, ref depth);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetRenderTarget_Single (ref UnityEngine.Rendering.RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace, int depthSlice) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetRenderTarget_ColDepth (ref UnityEngine.Rendering.RenderTargetIdentifier color, ref UnityEngine.Rendering.RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace, int depthSlice) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetRenderTarget_Multiple (UnityEngine.Rendering.RenderTargetIdentifier[] color, ref UnityEngine.Rendering.RenderTargetIdentifier depth) ;

    public void SetRandomWriteTarget(int index, RenderTargetIdentifier rt)
        {
            SetRandomWriteTarget_Texture(index, ref rt);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetRandomWriteTarget_Texture (int index, ref UnityEngine.Rendering.RenderTargetIdentifier rt) ;

    [uei.ExcludeFromDocs]
public void SetRandomWriteTarget (int index, ComputeBuffer buffer) {
    bool preserveCounterValue = false;
    SetRandomWriteTarget ( index, buffer, preserveCounterValue );
}

public void SetRandomWriteTarget(int index, ComputeBuffer buffer, [uei.DefaultValue("false")]  bool preserveCounterValue )
        {
            SetRandomWriteTarget_Buffer(index, buffer, preserveCounterValue);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetRandomWriteTarget_Buffer (int index, ComputeBuffer uav, bool preserveCounterValue) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void ClearRandomWriteTargets () ;

    public void CopyTexture(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            CopyTexture_Internal(ref src, -1, -1, -1, -1, -1, -1,
                ref dst, -1, -1, -1, -1, 0x1);
        }
    
    
    public void CopyTexture(RenderTargetIdentifier src, int srcElement,
            RenderTargetIdentifier dst, int dstElement)
        {
            CopyTexture_Internal(ref src, srcElement, -1, -1, -1, -1, -1,
                ref dst, dstElement, -1, -1, -1, 0x2);
        }
    
    
    public void CopyTexture(RenderTargetIdentifier src, int srcElement, int srcMip,
            RenderTargetIdentifier dst, int dstElement, int dstMip)
        {
            CopyTexture_Internal(ref src, srcElement, srcMip, -1, -1, -1, -1,
                ref dst, dstElement, dstMip, -1, -1, 0x3);
        }
    
    
    public void CopyTexture(RenderTargetIdentifier src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight,
            RenderTargetIdentifier dst, int dstElement, int dstMip, int dstX, int dstY)
        {
            CopyTexture_Internal(ref src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight,
                ref dst, dstElement, dstMip, dstX, dstY, 0x4);
        }
    
    
    public void SetViewport (Rect pixelRect) {
        INTERNAL_CALL_SetViewport ( this, ref pixelRect );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetViewport (CommandBuffer self, ref Rect pixelRect);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void CopyTexture_Internal (ref UnityEngine.Rendering.RenderTargetIdentifier src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight,
            ref UnityEngine.Rendering.RenderTargetIdentifier dst, int dstElement, int dstMip, int dstX, int dstY, int mode) ;

    public void Blit(Texture source, RenderTargetIdentifier dest)
        {

            Blit_Texture(source, ref dest, null, -1, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
        }
    
    
    public void Blit(Texture source, RenderTargetIdentifier dest, Vector2 scale, Vector2 offset)
        {
            Blit_Texture(source, ref dest, null, -1, scale, offset);
        }
    
    
    public void Blit(Texture source, RenderTargetIdentifier dest, Material mat)
        {
            Blit_Texture(source, ref dest, mat, -1, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
        }
    
    
    public void Blit(Texture source, RenderTargetIdentifier dest, Material mat, int pass)
        {
            Blit_Texture(source, ref dest, mat, pass, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
        }
    
    
    private void Blit_Texture (Texture source, ref UnityEngine.Rendering.RenderTargetIdentifier dest, Material mat, int pass, Vector2 scale, Vector2 offset) {
        INTERNAL_CALL_Blit_Texture ( this, source, ref dest, mat, pass, ref scale, ref offset );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Blit_Texture (CommandBuffer self, Texture source, ref UnityEngine.Rendering.RenderTargetIdentifier dest, Material mat, int pass, ref Vector2 scale, ref Vector2 offset);
    public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest)
        {
            Blit_Identifier(ref source, ref dest, null, -1, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
        }
    
    
    public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Vector2 scale, Vector2 offset)
        {
            Blit_Identifier(ref source, ref dest, null, -1, scale, offset);
        }
    
    
    public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Material mat)
        {
            Blit_Identifier(ref source, ref dest, mat, -1, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
        }
    
    
    public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Material mat, int pass)
        {
            Blit_Identifier(ref source, ref dest, mat, pass, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
        }
    
    
    private void Blit_Identifier (ref UnityEngine.Rendering.RenderTargetIdentifier source, ref UnityEngine.Rendering.RenderTargetIdentifier dest, Material mat, int pass, Vector2 scale, Vector2 offset) {
        INTERNAL_CALL_Blit_Identifier ( this, ref source, ref dest, mat, pass, ref scale, ref offset );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Blit_Identifier (CommandBuffer self, ref UnityEngine.Rendering.RenderTargetIdentifier source, ref UnityEngine.Rendering.RenderTargetIdentifier dest, Material mat, int pass, ref Vector2 scale, ref Vector2 offset);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void GetTemporaryRT (int nameID, int width, int height, [uei.DefaultValue("0")]  int depthBuffer , [uei.DefaultValue("FilterMode.Point")]  FilterMode filter , [uei.DefaultValue("RenderTextureFormat.Default")]  RenderTextureFormat format , [uei.DefaultValue("RenderTextureReadWrite.Default")]  RenderTextureReadWrite readWrite , [uei.DefaultValue("1")]  int antiAliasing , [uei.DefaultValue("false")]  bool enableRandomWrite , [uei.DefaultValue("RenderTextureMemoryless.None")]  RenderTextureMemoryless memorylessMode , [uei.DefaultValue("false")]  bool useDynamicScale ) ;

    [uei.ExcludeFromDocs]
    public void GetTemporaryRT (int nameID, int width, int height, int depthBuffer , FilterMode filter , RenderTextureFormat format , RenderTextureReadWrite readWrite , int antiAliasing , bool enableRandomWrite , RenderTextureMemoryless memorylessMode ) {
        bool useDynamicScale = false;
        GetTemporaryRT ( nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, memorylessMode, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRT (int nameID, int width, int height, int depthBuffer , FilterMode filter , RenderTextureFormat format , RenderTextureReadWrite readWrite , int antiAliasing , bool enableRandomWrite ) {
        bool useDynamicScale = false;
        RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
        GetTemporaryRT ( nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, memorylessMode, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRT (int nameID, int width, int height, int depthBuffer , FilterMode filter , RenderTextureFormat format , RenderTextureReadWrite readWrite , int antiAliasing ) {
        bool useDynamicScale = false;
        RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
        bool enableRandomWrite = false;
        GetTemporaryRT ( nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, memorylessMode, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRT (int nameID, int width, int height, int depthBuffer , FilterMode filter , RenderTextureFormat format , RenderTextureReadWrite readWrite ) {
        bool useDynamicScale = false;
        RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        GetTemporaryRT ( nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, memorylessMode, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRT (int nameID, int width, int height, int depthBuffer , FilterMode filter , RenderTextureFormat format ) {
        bool useDynamicScale = false;
        RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
        GetTemporaryRT ( nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, memorylessMode, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRT (int nameID, int width, int height, int depthBuffer , FilterMode filter ) {
        bool useDynamicScale = false;
        RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
        RenderTextureFormat format = RenderTextureFormat.Default;
        GetTemporaryRT ( nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, memorylessMode, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRT (int nameID, int width, int height, int depthBuffer ) {
        bool useDynamicScale = false;
        RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
        RenderTextureFormat format = RenderTextureFormat.Default;
        FilterMode filter = FilterMode.Point;
        GetTemporaryRT ( nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, memorylessMode, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRT (int nameID, int width, int height) {
        bool useDynamicScale = false;
        RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
        RenderTextureFormat format = RenderTextureFormat.Default;
        FilterMode filter = FilterMode.Point;
        int depthBuffer = 0;
        GetTemporaryRT ( nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, memorylessMode, useDynamicScale );
    }

    public void GetTemporaryRT (int nameID, RenderTextureDescriptor desc, [uei.DefaultValue("FilterMode.Point")]  FilterMode filter ) {
        INTERNAL_CALL_GetTemporaryRT ( this, nameID, ref desc, filter );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRT (int nameID, RenderTextureDescriptor desc) {
        FilterMode filter = FilterMode.Point;
        INTERNAL_CALL_GetTemporaryRT ( this, nameID, ref desc, filter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetTemporaryRT (CommandBuffer self, int nameID, ref RenderTextureDescriptor desc, FilterMode filter);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void GetTemporaryRTArray (int nameID, int width, int height, int slices, [uei.DefaultValue("0")]  int depthBuffer , [uei.DefaultValue("FilterMode.Point")]  FilterMode filter , [uei.DefaultValue("RenderTextureFormat.Default")]  RenderTextureFormat format , [uei.DefaultValue("RenderTextureReadWrite.Default")]  RenderTextureReadWrite readWrite , [uei.DefaultValue("1")]  int antiAliasing , [uei.DefaultValue("false")]  bool enableRandomWrite , [uei.DefaultValue("false")]  bool useDynamicScale ) ;

    [uei.ExcludeFromDocs]
    public void GetTemporaryRTArray (int nameID, int width, int height, int slices, int depthBuffer , FilterMode filter , RenderTextureFormat format , RenderTextureReadWrite readWrite , int antiAliasing , bool enableRandomWrite ) {
        bool useDynamicScale = false;
        GetTemporaryRTArray ( nameID, width, height, slices, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRTArray (int nameID, int width, int height, int slices, int depthBuffer , FilterMode filter , RenderTextureFormat format , RenderTextureReadWrite readWrite , int antiAliasing ) {
        bool useDynamicScale = false;
        bool enableRandomWrite = false;
        GetTemporaryRTArray ( nameID, width, height, slices, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRTArray (int nameID, int width, int height, int slices, int depthBuffer , FilterMode filter , RenderTextureFormat format , RenderTextureReadWrite readWrite ) {
        bool useDynamicScale = false;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        GetTemporaryRTArray ( nameID, width, height, slices, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRTArray (int nameID, int width, int height, int slices, int depthBuffer , FilterMode filter , RenderTextureFormat format ) {
        bool useDynamicScale = false;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
        GetTemporaryRTArray ( nameID, width, height, slices, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRTArray (int nameID, int width, int height, int slices, int depthBuffer , FilterMode filter ) {
        bool useDynamicScale = false;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
        RenderTextureFormat format = RenderTextureFormat.Default;
        GetTemporaryRTArray ( nameID, width, height, slices, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRTArray (int nameID, int width, int height, int slices, int depthBuffer ) {
        bool useDynamicScale = false;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
        RenderTextureFormat format = RenderTextureFormat.Default;
        FilterMode filter = FilterMode.Point;
        GetTemporaryRTArray ( nameID, width, height, slices, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, useDynamicScale );
    }

    [uei.ExcludeFromDocs]
    public void GetTemporaryRTArray (int nameID, int width, int height, int slices) {
        bool useDynamicScale = false;
        bool enableRandomWrite = false;
        int antiAliasing = 1;
        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
        RenderTextureFormat format = RenderTextureFormat.Default;
        FilterMode filter = FilterMode.Point;
        int depthBuffer = 0;
        GetTemporaryRTArray ( nameID, width, height, slices, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, useDynamicScale );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void ReleaseTemporaryRT (int nameID) ;

    public void ClearRenderTarget (bool clearDepth, bool clearColor, Color backgroundColor, [uei.DefaultValue("1.0f")]  float depth ) {
        INTERNAL_CALL_ClearRenderTarget ( this, clearDepth, clearColor, ref backgroundColor, depth );
    }

    [uei.ExcludeFromDocs]
    public void ClearRenderTarget (bool clearDepth, bool clearColor, Color backgroundColor) {
        float depth = 1.0f;
        INTERNAL_CALL_ClearRenderTarget ( this, clearDepth, clearColor, ref backgroundColor, depth );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ClearRenderTarget (CommandBuffer self, bool clearDepth, bool clearColor, ref Color backgroundColor, float depth);
    public void SetGlobalFloat(string name, float value)
        {
            SetGlobalFloat(Shader.PropertyToID(name), value);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetGlobalFloat (int nameID, float value) ;

    public void SetGlobalVector(string name, Vector4 value)
        {
            SetGlobalVector(Shader.PropertyToID(name), value);
        }
    
    
    public void SetGlobalVector (int nameID, Vector4 value) {
        INTERNAL_CALL_SetGlobalVector ( this, nameID, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetGlobalVector (CommandBuffer self, int nameID, ref Vector4 value);
    public void SetGlobalColor(string name, Color value)
        {
            SetGlobalColor(Shader.PropertyToID(name), value);
        }
    
    
    public void SetGlobalColor (int nameID, Color value) {
        INTERNAL_CALL_SetGlobalColor ( this, nameID, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetGlobalColor (CommandBuffer self, int nameID, ref Color value);
    public void SetGlobalMatrix(string name, Matrix4x4 value)
        {
            SetGlobalMatrix(Shader.PropertyToID(name), value);
        }
    
    
    public void SetGlobalMatrix (int nameID, Matrix4x4 value) {
        INTERNAL_CALL_SetGlobalMatrix ( this, nameID, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetGlobalMatrix (CommandBuffer self, int nameID, ref Matrix4x4 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void EnableShaderKeyword (string keyword) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void DisableShaderKeyword (string keyword) ;

    public void SetViewMatrix (Matrix4x4 view) {
        INTERNAL_CALL_SetViewMatrix ( this, ref view );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetViewMatrix (CommandBuffer self, ref Matrix4x4 view);
    public void SetProjectionMatrix (Matrix4x4 proj) {
        INTERNAL_CALL_SetProjectionMatrix ( this, ref proj );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetProjectionMatrix (CommandBuffer self, ref Matrix4x4 proj);
    public void SetViewProjectionMatrices (Matrix4x4 view, Matrix4x4 proj) {
        INTERNAL_CALL_SetViewProjectionMatrices ( this, ref view, ref proj );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetViewProjectionMatrices (CommandBuffer self, ref Matrix4x4 view, ref Matrix4x4 proj);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetGlobalDepthBias (float bias, float slopeBias) ;

    public void SetGlobalFloatArray(string propertyName, List<float> values)
        {
            SetGlobalFloatArray(Shader.PropertyToID(propertyName), values);
        }
    
    
    public void SetGlobalFloatArray(int nameID, List<float> values)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Count == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            SetGlobalFloatArrayListImpl(nameID, values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetGlobalFloatArrayListImpl (int nameID, object values) ;

    public void SetGlobalFloatArray(string propertyName, float[] values)
        {
            SetGlobalFloatArray(Shader.PropertyToID(propertyName), values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetGlobalFloatArray (int nameID, float[] values) ;

    public void SetGlobalVectorArray(string propertyName, List<Vector4> values)
        {
            SetGlobalVectorArray(Shader.PropertyToID(propertyName), values);
        }
    
    
    public void SetGlobalVectorArray(int nameID, List<Vector4> values)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Count == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            SetGlobalVectorArrayListImpl(nameID, values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetGlobalVectorArrayListImpl (int nameID, object values) ;

    public void SetGlobalVectorArray(string propertyName, Vector4[] values)
        {
            SetGlobalVectorArray(Shader.PropertyToID(propertyName), values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetGlobalVectorArray (int nameID, Vector4[] values) ;

    public void SetGlobalMatrixArray(string propertyName, List<Matrix4x4> values)
        {
            SetGlobalMatrixArray(Shader.PropertyToID(propertyName), values);
        }
    
    
    public void SetGlobalMatrixArray(int nameID, List<Matrix4x4> values)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Count == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            SetGlobalMatrixArrayListImpl(nameID, values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetGlobalMatrixArrayListImpl (int nameID, object values) ;

    public void SetGlobalMatrixArray(string propertyName, Matrix4x4[] values)
        {
            SetGlobalMatrixArray(Shader.PropertyToID(propertyName), values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetGlobalMatrixArray (int nameID, Matrix4x4[] values) ;

    public void SetGlobalTexture(string name, RenderTargetIdentifier value)
        {
            SetGlobalTexture(Shader.PropertyToID(name), value);
        }
    
    
    public void SetGlobalTexture(int nameID, RenderTargetIdentifier value)
        {
            SetGlobalTexture_Impl(nameID, ref value);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetGlobalTexture_Impl (int nameID, ref UnityEngine.Rendering.RenderTargetIdentifier rt) ;

    public void SetGlobalBuffer(string name, ComputeBuffer value)
        {
            SetGlobalBuffer(Shader.PropertyToID(name), value);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetGlobalBuffer (int nameID, ComputeBuffer value) ;

    public void SetShadowSamplingMode(UnityEngine.Rendering.RenderTargetIdentifier shadowmap, ShadowSamplingMode mode)
        {
            SetShadowSamplingMode_Impl(ref shadowmap, mode);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetShadowSamplingMode_Impl (ref UnityEngine.Rendering.RenderTargetIdentifier shadowmap, ShadowSamplingMode mode) ;

    public void IssuePluginEvent(IntPtr callback, int eventID)
        {
            if (callback == IntPtr.Zero)
                throw new ArgumentException("Null callback specified.");

            IssuePluginEventInternal(callback, eventID);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void IssuePluginEventInternal (IntPtr callback, int eventID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void BeginSample (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void EndSample (string name) ;

    public void IssuePluginEventAndData(IntPtr callback, int eventID, IntPtr data)
        {
            if (callback == IntPtr.Zero)
                throw new ArgumentException("Null callback specified.");

            IssuePluginEventAndDataInternal(callback, eventID, data);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void IssuePluginEventAndDataInternal (IntPtr callback, int eventID, IntPtr data) ;

    public void IssuePluginCustomBlit(IntPtr callback, uint command, UnityEngine.Rendering.RenderTargetIdentifier source, UnityEngine.Rendering.RenderTargetIdentifier dest, uint commandParam, uint commandFlags)
        {
            IssuePluginCustomBlitInternal(callback, command, ref source, ref dest, commandParam, commandFlags);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void IssuePluginCustomBlitInternal (IntPtr callback, uint command, ref UnityEngine.Rendering.RenderTargetIdentifier source, ref UnityEngine.Rendering.RenderTargetIdentifier dest, uint commandParam, uint commandFlags) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void IssuePluginCustomTextureUpdateInternal (IntPtr callback, Texture targetTexture, uint userData) ;

    public void IssuePluginCustomTextureUpdate(IntPtr callback, Texture targetTexture, uint userData)
        {
            IssuePluginCustomTextureUpdateInternal(callback, targetTexture, userData);
        }
    
    
}


} 
