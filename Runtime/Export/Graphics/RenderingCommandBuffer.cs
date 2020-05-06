// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    public partial class CommandBuffer
    {
 #pragma warning disable 414
        internal IntPtr m_Ptr;
 #pragma warning restore 414

        // --------------------------------------------------------------------
        // IDisposable implementation, with Release() for explicit cleanup.

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
            // we don't have any managed references, so 'disposing' part of
            // standard IDisposable pattern does not apply

            // Release native resources
            ReleaseBuffer();
            m_Ptr = IntPtr.Zero;
        }

        // --------------------------------------------------------------------
        // Actual API


        public CommandBuffer()
        {
            //m_Ptr = IntPtr.Zero;
            m_Ptr = InitBuffer();
        }

        public void Release()
        {
            Dispose();
        }

        public GraphicsFence CreateAsyncGraphicsFence()
        {
            return CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.PixelProcessing);
        }

        public GraphicsFence CreateAsyncGraphicsFence(SynchronisationStage stage)
        {
            return CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, GraphicsFence.TranslateSynchronizationStageToFlags(stage));
        }

        public GraphicsFence CreateGraphicsFence(GraphicsFenceType fenceType, SynchronisationStageFlags stage)
        {
            GraphicsFence newFence = new GraphicsFence();
            newFence.m_Ptr = CreateGPUFence_Internal(fenceType, stage);
            newFence.InitPostAllocation();
            newFence.Validate();
            return newFence;
        }

        public void WaitOnAsyncGraphicsFence(GraphicsFence fence)
        {
            WaitOnAsyncGraphicsFence(fence, SynchronisationStage.VertexProcessing);
        }

        public void WaitOnAsyncGraphicsFence(GraphicsFence fence, SynchronisationStage stage)
        {
            WaitOnAsyncGraphicsFence(fence, GraphicsFence.TranslateSynchronizationStageToFlags(stage));
        }

        public void WaitOnAsyncGraphicsFence(GraphicsFence fence, SynchronisationStageFlags stage)
        {
            if (fence.m_FenceType != GraphicsFenceType.AsyncQueueSynchronisation)
                throw new ArgumentException("Attempting to call WaitOnAsyncGPUFence on a fence that is not of GraphicsFenceType.AsyncQueueSynchronization");

            fence.Validate();

            //Don't wait on a fence that's already known to have passed
            if (fence.IsFencePending())
                WaitOnGPUFence_Internal(fence.m_Ptr, stage);
        }

        // Set a float parameter.
        public void SetComputeFloatParam(ComputeShader computeShader, string name, float val)
        {
            SetComputeFloatParam(computeShader, Shader.PropertyToID(name), val);
        }

        // Set an integer parameter.
        public void SetComputeIntParam(ComputeShader computeShader, string name, int val)
        {
            SetComputeIntParam(computeShader, Shader.PropertyToID(name), val);
        }

        // Set a vector parameter.
        public void SetComputeVectorParam(ComputeShader computeShader, string name, Vector4 val)
        {
            SetComputeVectorParam(computeShader, Shader.PropertyToID(name), val);
        }

        // Set a vector array parameter.
        public void SetComputeVectorArrayParam(ComputeShader computeShader, string name, Vector4[] values)
        {
            SetComputeVectorArrayParam(computeShader, Shader.PropertyToID(name), values);
        }

        // Set a matrix parameter.
        public void SetComputeMatrixParam(ComputeShader computeShader, string name, Matrix4x4 val)
        {
            SetComputeMatrixParam(computeShader, Shader.PropertyToID(name), val);
        }

        // Set a matrix array parameter.
        public void SetComputeMatrixArrayParam(ComputeShader computeShader, string name, Matrix4x4[] values)
        {
            SetComputeMatrixArrayParam(computeShader, Shader.PropertyToID(name), values);
        }

        // Set multiple consecutive float parameters at once.
        public void SetComputeFloatParams(ComputeShader computeShader, string name, params float[] values)
        {
            Internal_SetComputeFloats(computeShader, Shader.PropertyToID(name), values);
        }

        public void SetComputeFloatParams(ComputeShader computeShader, int nameID, params float[] values)
        {
            Internal_SetComputeFloats(computeShader, nameID, values);
        }

        // Set multiple consecutive integer parameters at once.
        public void SetComputeIntParams(ComputeShader computeShader, string name, params int[] values)
        {
            Internal_SetComputeInts(computeShader, Shader.PropertyToID(name), values);
        }

        public void SetComputeIntParams(ComputeShader computeShader, int nameID, params int[] values)
        {
            Internal_SetComputeInts(computeShader, nameID, values);
        }

        // Set a texture parameter.
        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, string name, RenderTargetIdentifier rt)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, Shader.PropertyToID(name), ref rt, 0, RenderTextureSubElement.Default);
        }

        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, int nameID, RenderTargetIdentifier rt)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, nameID, ref rt, 0, RenderTextureSubElement.Default);
        }

        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, string name, RenderTargetIdentifier rt, int mipLevel)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, Shader.PropertyToID(name), ref rt, mipLevel, RenderTextureSubElement.Default);
        }

        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, int nameID, RenderTargetIdentifier rt, int mipLevel)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, nameID, ref rt, mipLevel, RenderTextureSubElement.Default);
        }

        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, string name, RenderTargetIdentifier rt, int mipLevel, RenderTextureSubElement element)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, Shader.PropertyToID(name), ref rt, mipLevel, element);
        }

        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, int nameID, RenderTargetIdentifier rt, int mipLevel, RenderTextureSubElement element)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, nameID, ref rt, mipLevel, element);
        }

        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, int nameID, ComputeBuffer buffer)
        {
            Internal_SetComputeBufferParam(computeShader, kernelIndex, nameID, buffer);
        }

        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, string name, ComputeBuffer buffer)
        {
            Internal_SetComputeBufferParam(computeShader, kernelIndex, Shader.PropertyToID(name), buffer);
        }

        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, int nameID, GraphicsBuffer buffer)
        {
            Internal_SetComputeGraphicsBufferParam(computeShader, kernelIndex, nameID, buffer);
        }

        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, string name, GraphicsBuffer buffer)
        {
            Internal_SetComputeGraphicsBufferParam(computeShader, kernelIndex, Shader.PropertyToID(name), buffer);
        }

        public void SetComputeConstantBufferParam(ComputeShader computeShader, int nameID, ComputeBuffer buffer, int offset, int size)
        {
            Internal_SetComputeConstantComputeBufferParam(computeShader, nameID, buffer, offset, size);
        }

        public void SetComputeConstantBufferParam(ComputeShader computeShader, string name, ComputeBuffer buffer, int offset, int size)
        {
            Internal_SetComputeConstantComputeBufferParam(computeShader, Shader.PropertyToID(name), buffer, offset, size);
        }

        public void SetComputeConstantBufferParam(ComputeShader computeShader, int nameID, GraphicsBuffer buffer, int offset, int size)
        {
            Internal_SetComputeConstantGraphicsBufferParam(computeShader, nameID, buffer, offset, size);
        }

        public void SetComputeConstantBufferParam(ComputeShader computeShader, string name, GraphicsBuffer buffer, int offset, int size)
        {
            Internal_SetComputeConstantGraphicsBufferParam(computeShader, Shader.PropertyToID(name), buffer, offset, size);
        }

        // Execute a compute shader.
        public void DispatchCompute(ComputeShader computeShader, int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ)
        {
            Internal_DispatchCompute(computeShader, kernelIndex, threadGroupsX, threadGroupsY, threadGroupsZ);
        }

        public void DispatchCompute(ComputeShader computeShader, int kernelIndex, ComputeBuffer indirectBuffer, uint argsOffset)
        {
            Internal_DispatchComputeIndirect(computeShader, kernelIndex, indirectBuffer, argsOffset);
        }

        public void DispatchCompute(ComputeShader computeShader, int kernelIndex, GraphicsBuffer indirectBuffer, uint argsOffset)
        {
            Internal_DispatchComputeIndirectGraphicsBuffer(computeShader, kernelIndex, indirectBuffer, argsOffset);
        }

        public void BuildRayTracingAccelerationStructure(RayTracingAccelerationStructure accelerationStructure)
        {
            Vector3 zero = new Vector3(0, 0, 0);
            Internal_BuildRayTracingAccelerationStructure(accelerationStructure, zero);
        }

        public void BuildRayTracingAccelerationStructure(RayTracingAccelerationStructure accelerationStructure, Vector3 relativeOrigin)
        {
            Internal_BuildRayTracingAccelerationStructure(accelerationStructure, relativeOrigin);
        }

        public void SetRayTracingAccelerationStructure(RayTracingShader rayTracingShader, string name, RayTracingAccelerationStructure rayTracingAccelerationStructure)
        {
            Internal_SetRayTracingAccelerationStructure(rayTracingShader, Shader.PropertyToID(name), rayTracingAccelerationStructure);
        }

        public void SetRayTracingAccelerationStructure(RayTracingShader rayTracingShader, int nameID, RayTracingAccelerationStructure rayTracingAccelerationStructure)
        {
            Internal_SetRayTracingAccelerationStructure(rayTracingShader, nameID, rayTracingAccelerationStructure);
        }

        public void SetRayTracingBufferParam(RayTracingShader rayTracingShader, string name, ComputeBuffer buffer)
        {
            Internal_SetRayTracingBufferParam(rayTracingShader, Shader.PropertyToID(name), buffer);
        }

        public void SetRayTracingBufferParam(RayTracingShader rayTracingShader, int nameID, ComputeBuffer buffer)
        {
            Internal_SetRayTracingBufferParam(rayTracingShader, nameID, buffer);
        }

        public void SetRayTracingConstantBufferParam(RayTracingShader rayTracingShader, int nameID, ComputeBuffer buffer, int offset, int size)
        {
            Internal_SetRayTracingConstantComputeBufferParam(rayTracingShader, nameID, buffer, offset, size);
        }

        public void SetRayTracingConstantBufferParam(RayTracingShader rayTracingShader, string name, ComputeBuffer buffer, int offset, int size)
        {
            Internal_SetRayTracingConstantComputeBufferParam(rayTracingShader, Shader.PropertyToID(name), buffer, offset, size);
        }

        public void SetRayTracingConstantBufferParam(RayTracingShader rayTracingShader, int nameID, GraphicsBuffer buffer, int offset, int size)
        {
            Internal_SetRayTracingConstantGraphicsBufferParam(rayTracingShader, nameID, buffer, offset, size);
        }

        public void SetRayTracingConstantBufferParam(RayTracingShader rayTracingShader, string name, GraphicsBuffer buffer, int offset, int size)
        {
            Internal_SetRayTracingConstantGraphicsBufferParam(rayTracingShader, Shader.PropertyToID(name), buffer, offset, size);
        }

        public void SetRayTracingTextureParam(RayTracingShader rayTracingShader, string name, RenderTargetIdentifier rt)
        {
            Internal_SetRayTracingTextureParam(rayTracingShader, Shader.PropertyToID(name), ref rt);
        }

        public void SetRayTracingTextureParam(RayTracingShader rayTracingShader, int nameID, RenderTargetIdentifier rt)
        {
            Internal_SetRayTracingTextureParam(rayTracingShader, nameID, ref rt);
        }

        // Set a float parameter.
        public void SetRayTracingFloatParam(RayTracingShader rayTracingShader, string name, float val)
        {
            Internal_SetRayTracingFloatParam(rayTracingShader, Shader.PropertyToID(name), val);
        }

        // Set a float parameter.
        public void SetRayTracingFloatParam(RayTracingShader rayTracingShader, int nameID, float val)
        {
            Internal_SetRayTracingFloatParam(rayTracingShader, nameID, val);
        }

        // Set multiple consecutive float parameters at once.
        public void SetRayTracingFloatParams(RayTracingShader rayTracingShader, string name, params float[] values)
        {
            Internal_SetRayTracingFloats(rayTracingShader, Shader.PropertyToID(name), values);
        }

        // Set multiple consecutive float parameters at once.
        public void SetRayTracingFloatParams(RayTracingShader rayTracingShader, int nameID, params float[] values)
        {
            Internal_SetRayTracingFloats(rayTracingShader, nameID, values);
        }

        // Set a int parameter.
        public void SetRayTracingIntParam(RayTracingShader rayTracingShader, string name, int val)
        {
            Internal_SetRayTracingIntParam(rayTracingShader, Shader.PropertyToID(name), val);
        }

        // Set a int parameter.
        public void SetRayTracingIntParam(RayTracingShader rayTracingShader, int nameID, int val)
        {
            Internal_SetRayTracingIntParam(rayTracingShader, nameID, val);
        }

        // Set multiple consecutive int parameters at once.
        public void SetRayTracingIntParams(RayTracingShader rayTracingShader, string name, params int[] values)
        {
            Internal_SetRayTracingInts(rayTracingShader, Shader.PropertyToID(name), values);
        }

        // Set multiple consecutive int parameters at once.
        public void SetRayTracingIntParams(RayTracingShader rayTracingShader, int nameID, params int[] values)
        {
            Internal_SetRayTracingInts(rayTracingShader, nameID, values);
        }

        // Set a vector parameter.
        public void SetRayTracingVectorParam(RayTracingShader rayTracingShader, string name, Vector4 val)
        {
            Internal_SetRayTracingVectorParam(rayTracingShader, Shader.PropertyToID(name), val);
        }

        // Set a vector parameter.
        public void SetRayTracingVectorParam(RayTracingShader rayTracingShader, int nameID, Vector4 val)
        {
            Internal_SetRayTracingVectorParam(rayTracingShader, nameID, val);
        }

        // Set a vector array parameter.
        public void SetRayTracingVectorArrayParam(RayTracingShader rayTracingShader, string name, params Vector4[] values)
        {
            Internal_SetRayTracingVectorArrayParam(rayTracingShader, Shader.PropertyToID(name), values);
        }

        // Set a vector array parameter.
        public void SetRayTracingVectorArrayParam(RayTracingShader rayTracingShader, int nameID, params Vector4[] values)
        {
            Internal_SetRayTracingVectorArrayParam(rayTracingShader, nameID, values);
        }

        // Set a matrix parameter.
        public void SetRayTracingMatrixParam(RayTracingShader rayTracingShader, string name, Matrix4x4 val)
        {
            Internal_SetRayTracingMatrixParam(rayTracingShader, Shader.PropertyToID(name), val);
        }

        // Set a matrix parameter.
        public void SetRayTracingMatrixParam(RayTracingShader rayTracingShader, int nameID, Matrix4x4 val)
        {
            Internal_SetRayTracingMatrixParam(rayTracingShader, nameID, val);
        }

        // Set a matrix array parameter.
        public void SetRayTracingMatrixArrayParam(RayTracingShader rayTracingShader, string name, params Matrix4x4[] values)
        {
            Internal_SetRayTracingMatrixArrayParam(rayTracingShader, Shader.PropertyToID(name), values);
        }

        // Set a matrix array parameter.
        public void SetRayTracingMatrixArrayParam(RayTracingShader rayTracingShader, int nameID, params Matrix4x4[] values)
        {
            Internal_SetRayTracingMatrixArrayParam(rayTracingShader, nameID, values);
        }

        // Execute a Ray Tracing Shader.
        public void DispatchRays(RayTracingShader rayTracingShader, string rayGenName, UInt32 width, UInt32 height, UInt32 depth, Camera camera = null)
        {
            Internal_DispatchRays(rayTracingShader, rayGenName, width, height, depth, camera);
        }

        public void GenerateMips(RenderTargetIdentifier rt)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Internal_GenerateMips(rt);
        }

        public void GenerateMips(RenderTexture rt)
        {
            if (rt == null)
                throw new ArgumentNullException("rt");
            GenerateMips(new RenderTargetIdentifier(rt));
        }

        public void ResolveAntiAliasedSurface(RenderTexture rt, RenderTexture target = null)
        {
            if (rt == null)
                throw new ArgumentNullException("rt");

            Internal_ResolveAntiAliasedSurface(rt, target);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass, MaterialPropertyBlock properties)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");

            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
            {
                submeshIndex = Mathf.Clamp(submeshIndex, 0, mesh.subMeshCount - 1);
                Debug.LogWarning(String.Format("submeshIndex out of range. Clampped to {0}.", submeshIndex));
            }
            if (material == null)
                throw new ArgumentNullException("material");
            Internal_DrawMesh(mesh, matrix, material, submeshIndex, shaderPass, properties);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass)
        {
            DrawMesh(mesh, matrix, material, submeshIndex, shaderPass, null);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex)
        {
            DrawMesh(mesh, matrix, material, submeshIndex, -1);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material)
        {
            DrawMesh(mesh, matrix, material, 0);
        }

        public void DrawRenderer(Renderer renderer, Material material, int submeshIndex, int shaderPass)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");

            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            if (submeshIndex < 0)
            {
                submeshIndex = Mathf.Max(submeshIndex, 0);
                Debug.LogWarning(String.Format("submeshIndex out of range. Clampped to {0}.", submeshIndex));
            }
            if (material == null)
                throw new ArgumentNullException("material");
            Internal_DrawRenderer(renderer, material, submeshIndex, shaderPass);
        }

        public void DrawRenderer(Renderer renderer, Material material, int submeshIndex)
        {
            DrawRenderer(renderer, material, submeshIndex, -1);
        }

        public void DrawRenderer(Renderer renderer, Material material)
        {
            DrawRenderer(renderer, material, 0);
        }

        public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount, MaterialPropertyBlock properties)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Internal_DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
        }

        public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount)
        {
            DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, null);
        }

        public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount)
        {
            DrawProcedural(matrix, material, shaderPass, topology, vertexCount, 1);
        }

        public void DrawProcedural(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int indexCount, int instanceCount, MaterialPropertyBlock properties)
        {
            if (indexBuffer == null)
                throw new ArgumentNullException("indexBuffer");
            if (material == null)
                throw new ArgumentNullException("material");
            Internal_DrawProceduralIndexed(indexBuffer, matrix, material, shaderPass, topology, indexCount, instanceCount, properties);
        }

        public void DrawProcedural(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int indexCount, int instanceCount)
        {
            DrawProcedural(indexBuffer, matrix, material, shaderPass, topology, indexCount, instanceCount, null);
        }

        public void DrawProcedural(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int indexCount)
        {
            DrawProcedural(indexBuffer, matrix, material, shaderPass, topology, indexCount, 1);
        }

        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");

            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Internal_DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
        }

        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset)
        {
            DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, null);
        }

        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs)
        {
            DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, 0);
        }

        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
        {
            if (indexBuffer == null)
                throw new ArgumentNullException("indexBuffer");
            if (material == null)
                throw new ArgumentNullException("material");
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");
            Internal_DrawProceduralIndexedIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
        }

        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset)
        {
            DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, null);
        }

        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs)
        {
            DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, 0);
        }

        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");

            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Internal_DrawProceduralIndirectGraphicsBuffer(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
        }

        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset)
        {
            DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, null);
        }

        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs)
        {
            DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, 0);
        }

        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
        {
            if (indexBuffer == null)
                throw new ArgumentNullException("indexBuffer");
            if (material == null)
                throw new ArgumentNullException("material");
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");
            Internal_DrawProceduralIndexedIndirectGraphicsBuffer(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
        }

        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset)
        {
            DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, null);
        }

        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs)
        {
            DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, 0);
        }

        public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties)
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

            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            if (count > 0)
                Internal_DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, count, properties);
        }

        public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, count, null);
        }

        public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, matrices.Length);
        }

        public void DrawMeshInstancedProcedural(Mesh mesh, int submeshIndex, Material material, int shaderPass, int count, MaterialPropertyBlock properties = null)
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("DrawMeshInstancedProcedural is not supported.");
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            if (material == null)
                throw new ArgumentNullException("material");
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");

            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            if (count > 0)
                Internal_DrawMeshInstancedProcedural(mesh, submeshIndex, material, shaderPass, count, properties);
        }

        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
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

        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, int argsOffset)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, null);
        }

        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, 0, null);
        }

        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
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
            Internal_DrawMeshInstancedIndirectGraphicsBuffer(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, properties);
        }

        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, GraphicsBuffer bufferWithArgs, int argsOffset)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, null);
        }

        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, GraphicsBuffer bufferWithArgs)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, 0, null);
        }

        public void DrawOcclusionMesh(RectInt normalizedCamViewport)
        {
            Internal_DrawOcclusionMesh(normalizedCamViewport);
        }

        public void SetRandomWriteTarget(int index, RenderTargetIdentifier rt)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            SetRandomWriteTarget_Texture(index, ref rt);
        }

        public void SetRandomWriteTarget(int index, ComputeBuffer buffer, bool preserveCounterValue)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            SetRandomWriteTarget_Buffer(index, buffer, preserveCounterValue);
        }

        public void SetRandomWriteTarget(int index, ComputeBuffer buffer)
        {
            SetRandomWriteTarget(index, buffer, false);
        }

        public void SetRandomWriteTarget(int index, GraphicsBuffer buffer, bool preserveCounterValue)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            SetRandomWriteTarget_GraphicsBuffer(index, buffer, preserveCounterValue);
        }

        public void SetRandomWriteTarget(int index, GraphicsBuffer buffer)
        {
            SetRandomWriteTarget(index, buffer, false);
        }

        public void CopyCounterValue(ComputeBuffer src, ComputeBuffer dst, uint dstOffsetBytes)
        {
            CopyCounterValueCC(src, dst, dstOffsetBytes);
        }

        public void CopyCounterValue(GraphicsBuffer src, ComputeBuffer dst, uint dstOffsetBytes)
        {
            CopyCounterValueGC(src, dst, dstOffsetBytes);
        }

        public void CopyCounterValue(ComputeBuffer src, GraphicsBuffer dst, uint dstOffsetBytes)
        {
            CopyCounterValueCG(src, dst, dstOffsetBytes);
        }

        public void CopyCounterValue(GraphicsBuffer src, GraphicsBuffer dst, uint dstOffsetBytes)
        {
            CopyCounterValueGG(src, dst, dstOffsetBytes);
        }

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

        public void Blit(Texture source, RenderTargetIdentifier dest)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Texture(source, ref dest, null, -1, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), Texture2DArray.allSlices, 0);
        }

        public void Blit(Texture source, RenderTargetIdentifier dest, Vector2 scale, Vector2 offset)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Texture(source, ref dest, null, -1, scale, offset, Texture2DArray.allSlices, 0);
        }

        public void Blit(Texture source, RenderTargetIdentifier dest, Material mat)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Texture(source, ref dest, mat, -1, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), Texture2DArray.allSlices, 0);
        }

        public void Blit(Texture source, RenderTargetIdentifier dest, Material mat, int pass)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Texture(source, ref dest, mat, pass, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), Texture2DArray.allSlices, 0);
        }

        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Identifier(ref source, ref dest, null, -1, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), Texture2DArray.allSlices, 0);
        }

        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Vector2 scale, Vector2 offset)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Identifier(ref source, ref dest, null, -1, scale, offset, Texture2DArray.allSlices, 0);
        }

        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Material mat)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Identifier(ref source, ref dest, mat, -1, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), Texture2DArray.allSlices, 0);
        }

        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Material mat, int pass)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Identifier(ref source, ref dest, mat, pass, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), Texture2DArray.allSlices, 0);
        }

        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, int sourceDepthSlice, int destDepthSlice)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Identifier(ref source, ref dest, null, -1, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), sourceDepthSlice, destDepthSlice);
        }

        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Vector2 scale, Vector2 offset, int sourceDepthSlice, int destDepthSlice)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Identifier(ref source, ref dest, null, -1, scale, offset, sourceDepthSlice, destDepthSlice);
        }

        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Material mat, int pass, int destDepthSlice)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            Blit_Identifier(ref source, ref dest, mat, pass, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), Texture2DArray.allSlices, destDepthSlice);
        }

        public void SetGlobalFloat(string name, float value)
        {
            SetGlobalFloat(Shader.PropertyToID(name), value);
        }

        public void SetGlobalInt(string name, int value)
        {
            SetGlobalInt(Shader.PropertyToID(name), value);
        }

        public void SetGlobalVector(string name, Vector4 value)
        {
            SetGlobalVector(Shader.PropertyToID(name), value);
        }

        public void SetGlobalColor(string name, Color value)
        {
            SetGlobalColor(Shader.PropertyToID(name), value);
        }

        public void SetGlobalMatrix(string name, Matrix4x4 value)
        {
            SetGlobalMatrix(Shader.PropertyToID(name), value);
        }

        // List<T> version
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

        // T[] version
        public void SetGlobalFloatArray(string propertyName, float[] values)
        {
            SetGlobalFloatArray(Shader.PropertyToID(propertyName), values);
        }

        // List<T> version
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

        // T[] version
        public void SetGlobalVectorArray(string propertyName, Vector4[] values)
        {
            SetGlobalVectorArray(Shader.PropertyToID(propertyName), values);
        }

        // List<T> version
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

        // T[] version
        public void SetGlobalMatrixArray(string propertyName, Matrix4x4[] values)
        {
            SetGlobalMatrixArray(Shader.PropertyToID(propertyName), values);
        }

        public void SetGlobalTexture(string name, RenderTargetIdentifier value)
        {
            SetGlobalTexture(Shader.PropertyToID(name), value, RenderTextureSubElement.Default);
        }

        public void SetGlobalTexture(int nameID, RenderTargetIdentifier value)
        {
            SetGlobalTexture_Impl(nameID, ref value, RenderTextureSubElement.Default);
        }

        public void SetGlobalTexture(string name, RenderTargetIdentifier value, RenderTextureSubElement element)
        {
            SetGlobalTexture(Shader.PropertyToID(name), value, element);
        }

        public void SetGlobalTexture(int nameID, RenderTargetIdentifier value, RenderTextureSubElement element)
        {
            SetGlobalTexture_Impl(nameID, ref value, element);
        }

        public void SetGlobalBuffer(string name, ComputeBuffer value)
        {
            SetGlobalBufferInternal(Shader.PropertyToID(name), value);
        }

        public void SetGlobalBuffer(int nameID, ComputeBuffer value)
        {
            SetGlobalBufferInternal(nameID, value);
        }

        public void SetGlobalBuffer(string name, GraphicsBuffer value)
        {
            SetGlobalGraphicsBufferInternal(Shader.PropertyToID(name), value);
        }

        public void SetGlobalBuffer(int nameID, GraphicsBuffer value)
        {
            SetGlobalGraphicsBufferInternal(nameID, value);
        }

        public void SetGlobalConstantBuffer(ComputeBuffer buffer, int nameID, int offset, int size)
        {
            SetGlobalConstantBufferInternal(buffer, nameID, offset, size);
        }

        public void SetGlobalConstantBuffer(ComputeBuffer buffer, string name, int offset, int size)
        {
            SetGlobalConstantBufferInternal(buffer, Shader.PropertyToID(name), offset, size);
        }

        public void SetGlobalConstantBuffer(GraphicsBuffer buffer, int nameID, int offset, int size)
        {
            SetGlobalConstantGraphicsBufferInternal(buffer, nameID, offset, size);
        }

        public void SetGlobalConstantBuffer(GraphicsBuffer buffer, string name, int offset, int size)
        {
            SetGlobalConstantGraphicsBufferInternal(buffer, Shader.PropertyToID(name), offset, size);
        }

        public void SetShadowSamplingMode(UnityEngine.Rendering.RenderTargetIdentifier shadowmap, ShadowSamplingMode mode)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            SetShadowSamplingMode_Impl(ref shadowmap, mode);
        }

        public void SetSinglePassStereo(SinglePassStereoMode mode)
        {
            Internal_SetSinglePassStereo(mode);
        }

        public void IssuePluginEvent(IntPtr callback, int eventID)
        {
            if (callback == IntPtr.Zero)
                throw new ArgumentException("Null callback specified.");

            IssuePluginEventInternal(callback, eventID);
        }

        public void IssuePluginEventAndData(IntPtr callback, int eventID, IntPtr data)
        {
            if (callback == IntPtr.Zero)
                throw new ArgumentException("Null callback specified.");

            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            IssuePluginEventAndDataInternal(callback, eventID, data);
        }

        public void IssuePluginCustomBlit(IntPtr callback, uint command, UnityEngine.Rendering.RenderTargetIdentifier source, UnityEngine.Rendering.RenderTargetIdentifier dest, uint commandParam, uint commandFlags)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            IssuePluginCustomBlitInternal(callback, command, ref source, ref dest, commandParam, commandFlags);
        }

        [Obsolete("Use IssuePluginCustomTextureUpdateV2 to register TextureUpdate callbacks instead. Callbacks will be passed event IDs kUnityRenderingExtEventUpdateTextureBeginV2 or kUnityRenderingExtEventUpdateTextureEndV2, and data parameter of type UnityRenderingExtTextureUpdateParamsV2.", false)]
        public void IssuePluginCustomTextureUpdate(IntPtr callback, Texture targetTexture, uint userData)
        {
            IssuePluginCustomTextureUpdateInternal(callback, targetTexture, userData, false);
        }

        [Obsolete("Use IssuePluginCustomTextureUpdateV2 to register TextureUpdate callbacks instead. Callbacks will be passed event IDs kUnityRenderingExtEventUpdateTextureBeginV2 or kUnityRenderingExtEventUpdateTextureEndV2, and data parameter of type UnityRenderingExtTextureUpdateParamsV2.", false)]
        public void IssuePluginCustomTextureUpdateV1(IntPtr callback, Texture targetTexture, uint userData)
        {
            IssuePluginCustomTextureUpdateInternal(callback, targetTexture, userData, false);
        }

        public void IssuePluginCustomTextureUpdateV2(IntPtr callback, Texture targetTexture, uint userData)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);

            IssuePluginCustomTextureUpdateInternal(callback, targetTexture, userData, true);
        }

        public void ProcessVTFeedback(RenderTargetIdentifier rt, IntPtr resolver, int slice, int x, int width, int y, int height, int mip)
        {
            ValidateAgainstExecutionFlags(CommandBufferExecutionFlags.None, CommandBufferExecutionFlags.AsyncCompute);
            Internal_ProcessVTFeedback(rt, resolver, slice, x, width, y, height, mip);
        }
    }
}
