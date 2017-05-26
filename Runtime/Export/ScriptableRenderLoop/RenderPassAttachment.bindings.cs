// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering
{
    [NativeType("Runtime/Graphics/ScriptableRenderLoop/ScriptableRenderContext.h")]
    public class RenderPassAttachment : UnityEngine.Object
    {
        // The Load action to use when this attachment is accessed for the first time in this renderpass
        extern public RenderBufferLoadAction loadAction { get; private set; }
        // Store action to use when the the last subpass that accesses this attachment is done.
        // Note that this isn't set by the constructor, but rather by calling SetStoreTarget/SetResolveTarget
        extern public RenderBufferStoreAction storeAction { get; private set; }
        // The format of this attachment
        extern public RenderTextureFormat format { get; private set; }

        // The render texture where the load/store operations take place
        extern private RenderTargetIdentifier loadStoreTarget { get; set; }
        // The render texture to resolve the attachment into at the end of the renderpass
        extern private RenderTargetIdentifier resolveTarget { get; set; }

        // If loadAction is set to clear and this is a color surface, clear it to this color
        extern public Color clearColor { get; private set; }
        // If loadAction is set to clear and this is a depth surface, clear to this value
        extern public float clearDepth { get; private set; }
        // If loadAction is set to clear and this is a depth+stencil surface, clear to this value
        extern public uint clearStencil { get; private set; }

        // Bind a backing surface for this attachment. If none is set, the attachment is transient / memoryless (where supported)
        // or a temporary surface that's released at the end of the renderpass.
        // If loadExistingContents is true, the current contents of the surface is loaded as the initial pixel values for the attachment,
        // otherwise the initial values are undefined (with the expectation that the renderpass will render to every pixel on the screen)
        // If storeResults is true, the attachment contents at the end of the renderpass are stored to the surface,
        // otherwise the contents of the surface are undefined after the end of the renderpass.
        public void BindSurface(RenderTargetIdentifier tgt, bool loadExistingContents, bool storeResults)
        {
            loadStoreTarget = tgt;
            if (loadExistingContents && loadAction != RenderBufferLoadAction.Clear)
                loadAction = RenderBufferLoadAction.Load;
            if (storeResults)
            {
                if (storeAction == RenderBufferStoreAction.StoreAndResolve || storeAction == RenderBufferStoreAction.Resolve)
                    storeAction = RenderBufferStoreAction.StoreAndResolve;
                else
                    storeAction = RenderBufferStoreAction.Store;
            }
        }

        // If the renderpass has MSAA enabled, AA-resolve this attachment into the given render target.
        public void BindResolveSurface(RenderTargetIdentifier tgt)
        {
            resolveTarget = tgt;
            if (storeAction == RenderBufferStoreAction.StoreAndResolve || storeAction == RenderBufferStoreAction.Store)
                storeAction = RenderBufferStoreAction.StoreAndResolve;
            else
                storeAction = RenderBufferStoreAction.Resolve;
        }

        // At the beginning of the renderpass, clear this attachment with the given clear color (or depth/stencil)
        public void Clear(Color clearCol, float clearDep = 1.0f, uint clearStenc = 0)
        {
            clearColor = clearCol;
            clearDepth = clearDep;
            clearStencil = clearStenc;
            loadAction = RenderBufferLoadAction.Clear;
        }

        public RenderPassAttachment(RenderTextureFormat fmt)
        {
            Internal_CreateAttachment(this);

            loadAction = RenderBufferLoadAction.DontCare;
            storeAction = RenderBufferStoreAction.DontCare;
            format = fmt;
            loadStoreTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.None);
            resolveTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.None);
            clearColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            clearDepth = 1.0f;
        }

        [NativeMethod(Name = "RenderPassAttachment::Internal_CreateAttachment", IsFreeFunction = true)]
        extern public static void Internal_CreateAttachment([Writable] RenderPassAttachment self);
    }
}
