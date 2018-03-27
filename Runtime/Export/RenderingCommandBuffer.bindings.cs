// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeType("Runtime/Graphics/CommandBuffer/RenderingCommandBuffer.h")]
    public partial class CommandBuffer
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
    }
}
