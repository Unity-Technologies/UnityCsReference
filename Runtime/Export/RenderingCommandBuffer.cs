// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

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

        public GPUFence CreateGPUFence(SynchronisationStage stage)
        {
            GPUFence newFence = new GPUFence();
            newFence.m_Ptr = CreateGPUFence_Internal(stage);
            newFence.InitPostAllocation();
            newFence.Validate();
            return newFence;
        }

        public GPUFence CreateGPUFence()
        {
            return CreateGPUFence(SynchronisationStage.PixelProcessing);
        }

        public void WaitOnGPUFence(GPUFence fence, SynchronisationStage stage)
        {
            fence.Validate();

            //Don't wait on a fence that's already known to have passed
            if (fence.IsFencePending())
                WaitOnGPUFence_Internal(fence.m_Ptr, stage);
        }

        public void WaitOnGPUFence(GPUFence fence)
        {
            WaitOnGPUFence(fence, SynchronisationStage.VertexProcessing);
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
            Internal_SetComputeTextureParam(computeShader, kernelIndex, Shader.PropertyToID(name), ref rt, 0);
        }

        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, int nameID, RenderTargetIdentifier rt)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, nameID, ref rt, 0);
        }

        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, string name, RenderTargetIdentifier rt, int mipLevel)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, Shader.PropertyToID(name), ref rt, mipLevel);
        }

        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, int nameID, RenderTargetIdentifier rt, int mipLevel)
        {
            Internal_SetComputeTextureParam(computeShader, kernelIndex, nameID, ref rt, mipLevel);
        }

        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, string name, ComputeBuffer buffer)
        {
            SetComputeBufferParam(computeShader, kernelIndex, Shader.PropertyToID(name), buffer);
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

        public void GenerateMips(RenderTexture rt)
        {
            if (rt == null)
                throw new ArgumentNullException("rt");

            Internal_GenerateMips(rt);
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

        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");
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

        public void SetRandomWriteTarget(int index, RenderTargetIdentifier rt)
        {
            SetRandomWriteTarget_Texture(index, ref rt);
        }

        public void SetRandomWriteTarget(int index, ComputeBuffer buffer, bool preserveCounterValue)
        {
            SetRandomWriteTarget_Buffer(index, buffer, preserveCounterValue);
        }

        public void SetRandomWriteTarget(int index, ComputeBuffer buffer)
        {
            SetRandomWriteTarget(index, buffer, false);
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
            SetGlobalTexture(Shader.PropertyToID(name), value);
        }

        public void SetGlobalTexture(int nameID, RenderTargetIdentifier value)
        {
            SetGlobalTexture_Impl(nameID, ref value);
        }

        public void SetGlobalBuffer(string name, ComputeBuffer value)
        {
            SetGlobalBuffer(Shader.PropertyToID(name), value);
        }

        public void SetShadowSamplingMode(UnityEngine.Rendering.RenderTargetIdentifier shadowmap, ShadowSamplingMode mode)
        {
            SetShadowSamplingMode_Impl(ref shadowmap, mode);
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

            IssuePluginEventAndDataInternal(callback, eventID, data);
        }

        public void IssuePluginCustomBlit(IntPtr callback, uint command, UnityEngine.Rendering.RenderTargetIdentifier source, UnityEngine.Rendering.RenderTargetIdentifier dest, uint commandParam, uint commandFlags)
        {
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
            IssuePluginCustomTextureUpdateInternal(callback, targetTexture, userData, true);
        }
    }
}
