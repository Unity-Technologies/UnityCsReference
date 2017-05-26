// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering
{
    [NativeType("Runtime/Graphics/ScriptableRenderLoop/ScriptableRenderContext.h")]
    public partial struct ScriptableRenderContext
    {
        [NativeMethod("BeginRenderPass")]
        extern public static void BeginRenderPassInternal(IntPtr _self, int w, int h, int samples, RenderPassAttachment[] colors, RenderPassAttachment depth);

        [NativeMethod("BeginSubPass")]
        extern public static void BeginSubPassInternal(IntPtr _self, RenderPassAttachment[] colors, RenderPassAttachment[] inputs, bool readOnlyDepth);

        [NativeMethod("EndRenderPass")]
        extern public static void EndRenderPassInternal(IntPtr _self);
    }
}
