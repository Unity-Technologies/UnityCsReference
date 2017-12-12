// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine;
using System;

namespace UnityEngine.Rendering
{
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

        extern private void ConvertTexture_Internal(RenderTargetIdentifier src, int srcElement, RenderTargetIdentifier dst, int dstElement);
    }
}
