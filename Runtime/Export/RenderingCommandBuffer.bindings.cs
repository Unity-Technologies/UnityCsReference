// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine;

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
