// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Export/RenderingCommandBuffer.bindings.h")]
    [NativeType("Runtime/Graphics/CommandBuffer/RenderingCommandBuffer.h")]
    [UsedByNativeCode]
    public partial class CommandBuffer : IDisposable
    {
        public void ConvertTexture(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            ConvertTexture_Internal(src, 0, dst, 0);
        }

        public void ConvertTexture(RenderTargetIdentifier src, int srcElement, RenderTargetIdentifier dst, int dstElement)
        {
            ConvertTexture_Internal(src, srcElement, dst, dstElement);
        }

        public void RequestAsyncReadback(ComputeBuffer src, Action<AsyncGPUReadbackRequest> callback)
        {
            Internal_RequestAsyncReadback_1(src, callback);
        }

        public void RequestAsyncReadback(ComputeBuffer src, int size, int offset, Action<AsyncGPUReadbackRequest> callback)
        {
            Internal_RequestAsyncReadback_2(src, size, offset, callback);
        }

        public void RequestAsyncReadback(Texture src, Action<AsyncGPUReadbackRequest> callback)
        {
            Internal_RequestAsyncReadback_3(src, callback);
        }

        public void RequestAsyncReadback(Texture src, int mipIndex, Action<AsyncGPUReadbackRequest> callback)
        {
            Internal_RequestAsyncReadback_4(src, mipIndex, callback);
        }

        public void RequestAsyncReadback(Texture src, int mipIndex, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback)
        {
            Internal_RequestAsyncReadback_5(src, mipIndex, dstFormat, callback);
        }

        public void RequestAsyncReadback(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, Action<AsyncGPUReadbackRequest> callback)
        {
            Internal_RequestAsyncReadback_6(src, mipIndex, x, width, y, height, z, depth, callback);
        }

        public void RequestAsyncReadback(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback)
        {
            Internal_RequestAsyncReadback_7(src, mipIndex, x, width, y, height, z, depth, dstFormat, callback);
        }

        [NativeMethod("AddRequestAsyncReadback")]
        extern private void Internal_RequestAsyncReadback_1([NotNull] ComputeBuffer src, [NotNull] Action<AsyncGPUReadbackRequest> callback);
        [NativeMethod("AddRequestAsyncReadback")]
        extern private void Internal_RequestAsyncReadback_2([NotNull] ComputeBuffer src, int size, int offset, [NotNull] Action<AsyncGPUReadbackRequest> callback);
        [NativeMethod("AddRequestAsyncReadback")]
        extern private void Internal_RequestAsyncReadback_3([NotNull] Texture src, [NotNull] Action<AsyncGPUReadbackRequest> callback);
        [NativeMethod("AddRequestAsyncReadback")]
        extern private void Internal_RequestAsyncReadback_4([NotNull] Texture src, int mipIndex, [NotNull] Action<AsyncGPUReadbackRequest> callback);
        [NativeMethod("AddRequestAsyncReadback")]
        extern private void Internal_RequestAsyncReadback_5([NotNull] Texture src, int mipIndex, TextureFormat dstFormat, [NotNull] Action<AsyncGPUReadbackRequest> callback);
        [NativeMethod("AddRequestAsyncReadback")]
        extern private void Internal_RequestAsyncReadback_6([NotNull] Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, [NotNull] Action<AsyncGPUReadbackRequest> callback);
        [NativeMethod("AddRequestAsyncReadback")]
        extern private void Internal_RequestAsyncReadback_7([NotNull] Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, TextureFormat dstFormat, [NotNull] Action<AsyncGPUReadbackRequest> callback);


        [NativeMethod("AddSetInvertCulling")]
        public extern void SetInvertCulling(bool invertCulling);

        extern void ConvertTexture_Internal(RenderTargetIdentifier src, int srcElement, RenderTargetIdentifier dst, int dstElement);

        [FreeFunction("RenderingCommandBuffer_Bindings::InitBuffer")]
        extern private static IntPtr InitBuffer();

        [FreeFunction("RenderingCommandBuffer_Bindings::CreateGPUFence_Internal", HasExplicitThis = true)]
        extern private IntPtr CreateGPUFence_Internal(SynchronisationStage stage);

        [FreeFunction("RenderingCommandBuffer_Bindings::WaitOnGPUFence_Internal", HasExplicitThis = true)]
        extern private void WaitOnGPUFence_Internal(IntPtr fencePtr, SynchronisationStage stage);

        [FreeFunction("RenderingCommandBuffer_Bindings::ReleaseBuffer", HasExplicitThis = true, IsThreadSafe = true)]
        extern private void ReleaseBuffer();

        [FreeFunction("RenderingCommandBuffer_Bindings::SetComputeFloatParam", HasExplicitThis = true)]
        extern public void SetComputeFloatParam([NotNull] ComputeShader computeShader, int nameID, float val);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetComputeIntParam", HasExplicitThis = true)]
        extern public void SetComputeIntParam([NotNull] ComputeShader computeShader, int nameID, int val);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetComputeVectorParam", HasExplicitThis = true)]
        extern public void SetComputeVectorParam([NotNull] ComputeShader computeShader, int nameID, Vector4 val);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetComputeVectorArrayParam", HasExplicitThis = true)]
        extern public void SetComputeVectorArrayParam([NotNull] ComputeShader computeShader, int nameID, Vector4[] values);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetComputeMatrixParam", HasExplicitThis = true)]
        extern public void SetComputeMatrixParam([NotNull] ComputeShader computeShader, int nameID, Matrix4x4 val);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetComputeMatrixArrayParam", HasExplicitThis = true)]
        extern public void SetComputeMatrixArrayParam([NotNull] ComputeShader computeShader, int nameID, Matrix4x4[] values);

        [FreeFunction("RenderingCommandBuffer_Bindings::Internal_SetComputeFloats", HasExplicitThis = true)]
        extern private void Internal_SetComputeFloats([NotNull] ComputeShader computeShader, int nameID, float[] values);

        [FreeFunction("RenderingCommandBuffer_Bindings::Internal_SetComputeInts", HasExplicitThis = true)]
        extern private void Internal_SetComputeInts([NotNull] ComputeShader computeShader, int nameID, int[] values);

        [FreeFunction("RenderingCommandBuffer_Bindings::Internal_SetComputeTextureParam", HasExplicitThis = true)]
        extern private void Internal_SetComputeTextureParam([NotNull] ComputeShader computeShader, int kernelIndex, int nameID, ref UnityEngine.Rendering.RenderTargetIdentifier rt, int mipLevel);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetComputeBufferParam", HasExplicitThis = true)]
        extern public void SetComputeBufferParam([NotNull] ComputeShader computeShader, int kernelIndex, int nameID, ComputeBuffer buffer);

        [FreeFunction("RenderingCommandBuffer_Bindings::Internal_DispatchCompute", HasExplicitThis = true, ThrowsException = true)]
        extern private void Internal_DispatchCompute([NotNull] ComputeShader computeShader, int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ);

        [FreeFunction("RenderingCommandBuffer_Bindings::Internal_DispatchComputeIndirect", HasExplicitThis = true, ThrowsException = true)]
        extern private void Internal_DispatchComputeIndirect([NotNull] ComputeShader computeShader, int kernelIndex, ComputeBuffer indirectBuffer, uint argsOffset);

        [NativeMethod("AddGenerateMips")]
        extern private void Internal_GenerateMips(RenderTexture rt);

        [NativeMethod("AddResolveAntiAliasedSurface")]
        extern private void Internal_ResolveAntiAliasedSurface(RenderTexture rt, RenderTexture target);

        [NativeMethod("AddCopyCounterValue")]
        extern public void CopyCounterValue(ComputeBuffer src, ComputeBuffer dst, uint dstOffsetBytes);

        extern public string name { get; set; }
        extern public int sizeInBytes {[NativeMethod("GetBufferSize")] get; }

        [NativeMethod("ClearCommands")]
        extern public void Clear();

        [FreeFunction("RenderingCommandBuffer_Bindings::Internal_DrawMesh", HasExplicitThis = true)]
        extern private void Internal_DrawMesh([NotNull] Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass, MaterialPropertyBlock properties);

        [NativeMethod("AddDrawRenderer")]
        extern private void Internal_DrawRenderer([NotNull] Renderer renderer, Material material, int submeshIndex, int shaderPass);

        private void Internal_DrawRenderer(Renderer renderer, Material material, int submeshIndex)
        {
            Internal_DrawRenderer(renderer, material, submeshIndex, -1);
        }

        private void Internal_DrawRenderer(Renderer renderer, Material material)
        {
            Internal_DrawRenderer(renderer, material, 0);
        }

        [NativeMethod("AddDrawProcedural")]
        extern private void Internal_DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount, MaterialPropertyBlock properties);

        [FreeFunction("RenderingCommandBuffer_Bindings::Internal_DrawProceduralIndirect", HasExplicitThis = true)]
        extern private void Internal_DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties);

        [FreeFunction("RenderingCommandBuffer_Bindings::Internal_DrawMeshInstanced", HasExplicitThis = true)]
        extern private void Internal_DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties);

        [FreeFunction("RenderingCommandBuffer_Bindings::Internal_DrawMeshInstancedIndirect", HasExplicitThis = true)]
        extern private void Internal_DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetRandomWriteTarget_Texture", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetRandomWriteTarget_Texture(int index, ref UnityEngine.Rendering.RenderTargetIdentifier rt);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetRandomWriteTarget_Buffer", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetRandomWriteTarget_Buffer(int index, ComputeBuffer uav, bool preserveCounterValue);

        [NativeMethod("AddClearRandomWriteTargets")]
        extern public void ClearRandomWriteTargets();

        [FreeFunction("RenderingCommandBuffer_Bindings::SetViewport", HasExplicitThis = true)]
        extern public void SetViewport(Rect pixelRect);

        [FreeFunction("RenderingCommandBuffer_Bindings::EnableScissorRect", HasExplicitThis = true)]
        extern public void EnableScissorRect(Rect scissor);

        [NativeMethod("AddDisableScissorRect")]
        extern public void DisableScissorRect();

        [FreeFunction("RenderingCommandBuffer_Bindings::CopyTexture_Internal", HasExplicitThis = true)]
        extern private void CopyTexture_Internal(ref UnityEngine.Rendering.RenderTargetIdentifier src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight,
            ref UnityEngine.Rendering.RenderTargetIdentifier dst, int dstElement, int dstMip, int dstX, int dstY, int mode);

        [FreeFunction("RenderingCommandBuffer_Bindings::Blit_Texture", HasExplicitThis = true)]
        extern private void Blit_Texture(Texture source, ref UnityEngine.Rendering.RenderTargetIdentifier dest, Material mat, int pass, Vector2 scale, Vector2 offset);

        [FreeFunction("RenderingCommandBuffer_Bindings::Blit_Identifier", HasExplicitThis = true)]
        extern private void Blit_Identifier(ref UnityEngine.Rendering.RenderTargetIdentifier source, ref UnityEngine.Rendering.RenderTargetIdentifier dest, Material mat, int pass, Vector2 scale, Vector2 offset);

        [FreeFunction("RenderingCommandBuffer_Bindings::GetTemporaryRT", HasExplicitThis = true)]
        extern public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, bool enableRandomWrite, RenderTextureMemoryless memorylessMode, bool useDynamicScale);

        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, bool enableRandomWrite, RenderTextureMemoryless memorylessMode)
        {
            GetTemporaryRT(nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, memorylessMode, false);
        }

        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, bool enableRandomWrite)
        {
            GetTemporaryRT(nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, RenderTextureMemoryless.None);
        }

        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing)
        {
            GetTemporaryRT(nameID, width, height, depthBuffer, filter, format, readWrite, antiAliasing, false);
        }

        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            GetTemporaryRT(nameID, width, height, depthBuffer, filter, format, readWrite, 1);
        }

        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format)
        {
            GetTemporaryRT(nameID, width, height, depthBuffer, filter, format, RenderTextureReadWrite.Default);
        }

        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter)
        {
            GetTemporaryRT(nameID, width, height, depthBuffer, filter, RenderTextureFormat.Default);
        }

        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer)
        {
            GetTemporaryRT(nameID, width, height, depthBuffer, FilterMode.Point);
        }

        public void GetTemporaryRT(int nameID, int width, int height)
        {
            GetTemporaryRT(nameID, width, height, 0);
        }

        [FreeFunction("RenderingCommandBuffer_Bindings::GetTemporaryRTWithDescriptor", HasExplicitThis = true)]
        extern private void GetTemporaryRTWithDescriptor(int nameID, RenderTextureDescriptor desc, FilterMode filter);

        public void GetTemporaryRT(int nameID, RenderTextureDescriptor desc, FilterMode filter)
        {
            GetTemporaryRTWithDescriptor(nameID, desc, filter);
        }

        public void GetTemporaryRT(int nameID, RenderTextureDescriptor desc)
        {
            GetTemporaryRT(nameID, desc, FilterMode.Point);
        }

        [FreeFunction("RenderingCommandBuffer_Bindings::GetTemporaryRTArray", HasExplicitThis = true)]
        extern public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, bool enableRandomWrite, bool useDynamicScale);

        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, bool enableRandomWrite)
        {
            GetTemporaryRTArray(nameID, width, height, slices, depthBuffer, filter, format, readWrite, antiAliasing, enableRandomWrite, false);
        }

        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing)
        {
            GetTemporaryRTArray(nameID, width, height, slices, depthBuffer, filter, format, readWrite, antiAliasing, false);
        }

        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            GetTemporaryRTArray(nameID, width, height, slices, depthBuffer, filter, format, readWrite, 1);
        }

        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, RenderTextureFormat format)
        {
            GetTemporaryRTArray(nameID, width, height, slices, depthBuffer, filter, format, RenderTextureReadWrite.Default);
        }

        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter)
        {
            GetTemporaryRTArray(nameID, width, height, slices, depthBuffer, filter, RenderTextureFormat.Default);
        }

        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer)
        {
            GetTemporaryRTArray(nameID, width, height, slices, depthBuffer, FilterMode.Point);
        }

        public void GetTemporaryRTArray(int nameID, int width, int height, int slices)
        {
            GetTemporaryRTArray(nameID, width, height, slices, 0);
        }

        [FreeFunction("RenderingCommandBuffer_Bindings::ReleaseTemporaryRT", HasExplicitThis = true)]
        extern public void ReleaseTemporaryRT(int nameID);

        [FreeFunction("RenderingCommandBuffer_Bindings::ClearRenderTarget", HasExplicitThis = true)]
        extern public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor, float depth);

        public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor)
        {
            ClearRenderTarget(clearDepth, clearColor, backgroundColor, 1.0f);
        }

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalFloat", HasExplicitThis = true)]
        extern public void SetGlobalFloat(int nameID, float value);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalInt", HasExplicitThis = true)]
        extern public void SetGlobalInt(int nameID, int value);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalVector", HasExplicitThis = true)]
        extern public void SetGlobalVector(int nameID, Vector4 value);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalColor", HasExplicitThis = true)]
        extern public void SetGlobalColor(int nameID, Color value);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalMatrix", HasExplicitThis = true)]
        extern public void SetGlobalMatrix(int nameID, Matrix4x4 value);

        [FreeFunction("RenderingCommandBuffer_Bindings::EnableShaderKeyword", HasExplicitThis = true)]
        extern public void EnableShaderKeyword(string keyword);

        [FreeFunction("RenderingCommandBuffer_Bindings::DisableShaderKeyword", HasExplicitThis = true)]
        extern public void DisableShaderKeyword(string keyword);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetViewMatrix", HasExplicitThis = true)]
        extern public void SetViewMatrix(Matrix4x4 view);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetProjectionMatrix", HasExplicitThis = true)]
        extern public void SetProjectionMatrix(Matrix4x4 proj);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetViewProjectionMatrices", HasExplicitThis = true)]
        extern public void SetViewProjectionMatrices(Matrix4x4 view, Matrix4x4 proj);

        [NativeMethod("AddSetGlobalDepthBias")]
        extern public void SetGlobalDepthBias(float bias, float slopeBias);


        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalFloatArrayListImpl", HasExplicitThis = true)]
        extern private void SetGlobalFloatArrayListImpl(int nameID, object values);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalVectorArrayListImpl", HasExplicitThis = true)]
        extern private void SetGlobalVectorArrayListImpl(int nameID, object values);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalMatrixArrayListImpl", HasExplicitThis = true)]
        extern private void SetGlobalMatrixArrayListImpl(int nameID, object values);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalFloatArray", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetGlobalFloatArray(int nameID, float[] values);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalVectorArray", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetGlobalVectorArray(int nameID, Vector4[] values);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalMatrixArray", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetGlobalMatrixArray(int nameID, Matrix4x4[] values);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalTexture_Impl", HasExplicitThis = true)]
        extern private void SetGlobalTexture_Impl(int nameID, ref UnityEngine.Rendering.RenderTargetIdentifier rt);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetGlobalBuffer", HasExplicitThis = true)]
        extern public void SetGlobalBuffer(int nameID, ComputeBuffer value);

        [FreeFunction("RenderingCommandBuffer_Bindings::SetShadowSamplingMode_Impl", HasExplicitThis = true)]
        extern private void SetShadowSamplingMode_Impl(ref UnityEngine.Rendering.RenderTargetIdentifier shadowmap, ShadowSamplingMode mode);

        [FreeFunction("RenderingCommandBuffer_Bindings::IssuePluginEventInternal", HasExplicitThis = true)]
        extern private void IssuePluginEventInternal(IntPtr callback, int eventID);

        [FreeFunction("RenderingCommandBuffer_Bindings::BeginSample", HasExplicitThis = true)]
        extern public void BeginSample(string name);

        [FreeFunction("RenderingCommandBuffer_Bindings::EndSample", HasExplicitThis = true)]
        extern public void EndSample(string name);

        [FreeFunction("RenderingCommandBuffer_Bindings::IssuePluginEventAndDataInternal", HasExplicitThis = true)]
        extern private void IssuePluginEventAndDataInternal(IntPtr callback, int eventID, IntPtr data);

        [FreeFunction("RenderingCommandBuffer_Bindings::IssuePluginCustomBlitInternal", HasExplicitThis = true)]
        extern private void IssuePluginCustomBlitInternal(IntPtr callback, uint command, ref UnityEngine.Rendering.RenderTargetIdentifier source, ref UnityEngine.Rendering.RenderTargetIdentifier dest, uint commandParam, uint commandFlags);

        [FreeFunction("RenderingCommandBuffer_Bindings::IssuePluginCustomTextureUpdateInternal", HasExplicitThis = true)]
        extern private void IssuePluginCustomTextureUpdateInternal(IntPtr callback, Texture targetTexture, uint userData, bool useNewUnityRenderingExtTextureUpdateParamsV2);

        public void SetRenderTarget(RenderTargetIdentifier rt)
        {
            SetRenderTargetSingle_Internal(rt, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        public void SetRenderTarget(RenderTargetIdentifier rt, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction)
        {
            if (loadAction == RenderBufferLoadAction.Clear)
                throw new ArgumentException("RenderBufferLoadAction.Clear is not supported");
            SetRenderTargetSingle_Internal(rt, loadAction, storeAction, loadAction, storeAction);
        }

        public void SetRenderTarget(RenderTargetIdentifier rt,
            RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction)
        {
            if (colorLoadAction == RenderBufferLoadAction.Clear || depthLoadAction == RenderBufferLoadAction.Clear)
                throw new ArgumentException("RenderBufferLoadAction.Clear is not supported");
            SetRenderTargetSingle_Internal(rt, colorLoadAction, colorStoreAction, depthLoadAction, depthStoreAction);
        }

        public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel)
        {
            if (mipLevel < 0)
                throw new ArgumentException(String.Format("Invalid value for mipLevel ({0})", mipLevel));
            SetRenderTargetSingle_Internal(new RenderTargetIdentifier(rt, mipLevel),
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace)
        {
            if (mipLevel < 0)
                throw new ArgumentException(String.Format("Invalid value for mipLevel ({0})", mipLevel));
            SetRenderTargetSingle_Internal(new RenderTargetIdentifier(rt, mipLevel, cubemapFace),
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        {
            if (depthSlice < -1)
                throw new ArgumentException(String.Format("Invalid value for depthSlice ({0})", depthSlice));
            if (mipLevel < 0)
                throw new ArgumentException(String.Format("Invalid value for mipLevel ({0})", mipLevel));
            SetRenderTargetSingle_Internal(new RenderTargetIdentifier(rt, mipLevel, cubemapFace, depthSlice),
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth)
        {
            SetRenderTargetColorDepth_Internal(color, depth, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth, int mipLevel)
        {
            if (mipLevel < 0)
                throw new ArgumentException(String.Format("Invalid value for mipLevel ({0})", mipLevel));
            SetRenderTargetColorDepth_Internal(new RenderTargetIdentifier(color, mipLevel),
                depth, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace)
        {
            if (mipLevel < 0)
                throw new ArgumentException(String.Format("Invalid value for mipLevel ({0})", mipLevel));
            SetRenderTargetColorDepth_Internal(new RenderTargetIdentifier(color, mipLevel, cubemapFace),
                depth, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        {
            if (depthSlice < -1)
                throw new ArgumentException(String.Format("Invalid value for depthSlice ({0})", depthSlice));
            if (mipLevel < 0)
                throw new ArgumentException(String.Format("Invalid value for mipLevel ({0})", mipLevel));
            SetRenderTargetColorDepth_Internal(new RenderTargetIdentifier(color, mipLevel, cubemapFace, depthSlice),
                depth, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        public void SetRenderTarget(RenderTargetIdentifier color, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depth, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction)
        {
            if (colorLoadAction == RenderBufferLoadAction.Clear || depthLoadAction == RenderBufferLoadAction.Clear)
                throw new ArgumentException("RenderBufferLoadAction.Clear is not supported");
            SetRenderTargetColorDepth_Internal(color, depth, colorLoadAction, colorStoreAction, depthLoadAction, depthStoreAction);
        }

        public void SetRenderTarget(RenderTargetIdentifier[] colors, Rendering.RenderTargetIdentifier depth)
        {
            if (colors.Length < 1)
                throw new ArgumentException(string.Format("colors.Length must be at least 1, but was", colors.Length));
            if (colors.Length > SystemInfo.supportedRenderTargetCount)
                throw new ArgumentException(string.Format("colors.Length is {0} and exceeds the maximum number of supported render targets ({1})", colors.Length, SystemInfo.supportedRenderTargetCount));

            SetRenderTargetMulti_Internal(colors, depth, null, null, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        public void SetRenderTarget(RenderTargetBinding binding)
        {
            if (binding.colorRenderTargets.Length < 1)
                throw new ArgumentException(string.Format("The number of color render targets must be at least 1, but was {0}", binding.colorRenderTargets.Length));
            if (binding.colorRenderTargets.Length > SystemInfo.supportedRenderTargetCount)
                throw new ArgumentException(string.Format("The number of color render targets ({0}) and exceeds the maximum supported number of render targets ({1})", binding.colorRenderTargets.Length, SystemInfo.supportedRenderTargetCount));
            if (binding.colorLoadActions.Length != binding.colorRenderTargets.Length)
                throw new ArgumentException(string.Format("The number of color load actions provided ({0}) does not match the number of color render targets ({1})", binding.colorLoadActions.Length, binding.colorRenderTargets.Length));
            if (binding.colorStoreActions.Length != binding.colorRenderTargets.Length)
                throw new ArgumentException(string.Format("The number of color store actions provided ({0}) does not match the number of color render targets ({1})", binding.colorLoadActions.Length, binding.colorRenderTargets.Length));
            if (binding.depthLoadAction == RenderBufferLoadAction.Clear || Array.IndexOf(binding.colorLoadActions, RenderBufferLoadAction.Clear) > -1)
                throw new ArgumentException("RenderBufferLoadAction.Clear is not supported");

            if (binding.colorRenderTargets.Length == 1) // non-MRT case respects mip/face/slice of color's RenderTargetIdentifier
                SetRenderTargetColorDepth_Internal(binding.colorRenderTargets[0], binding.depthRenderTarget, binding.colorLoadActions[0], binding.colorStoreActions[0], binding.depthLoadAction, binding.depthStoreAction);
            else
                SetRenderTargetMulti_Internal(binding.colorRenderTargets, binding.depthRenderTarget, binding.colorLoadActions, binding.colorStoreActions, binding.depthLoadAction, binding.depthStoreAction);
        }

        extern private void SetRenderTargetSingle_Internal(RenderTargetIdentifier rt,
            RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction);

        extern private void SetRenderTargetColorDepth_Internal(RenderTargetIdentifier color, RenderTargetIdentifier depth,
            RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction);

        extern private void SetRenderTargetMulti_Internal(RenderTargetIdentifier[] colors, Rendering.RenderTargetIdentifier depth,
            RenderBufferLoadAction[] colorLoadActions, RenderBufferStoreAction[] colorStoreActions,
            RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction);
    }
}
