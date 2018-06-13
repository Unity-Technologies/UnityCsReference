// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering
{
    [NativeType("Runtime/Graphics/ScriptableRenderLoop/ScriptableRenderContext.h")]
    [NativeHeader("Runtime/Export/ScriptableRenderContext.bindings.h")]
    [NativeHeader("Runtime/UI/Canvas.h")]
    [NativeHeader("Runtime/UI/CanvasManager.h")]
    public partial struct ScriptableRenderContext
    {
        [FreeFunction("ScriptableRenderContext::BeginRenderPass")]
        extern public static void BeginRenderPassInternal(IntPtr _self, int w, int h, int samples, RenderPassAttachment[] colors, RenderPassAttachment depth);

        [FreeFunction("ScriptableRenderContext::BeginSubPass")]
        extern public static void BeginSubPassInternal(IntPtr _self, RenderPassAttachment[] colors, RenderPassAttachment[] inputs, bool readOnlyDepth);

        [FreeFunction("ScriptableRenderContext::EndRenderPass")]
        extern public static void EndRenderPassInternal(IntPtr _self);

        extern private void Submit_Internal();

        extern private void DrawRenderers_Internal(FilterResults renderers, ref DrawRendererSettings drawSettings, FilterRenderersSettings filterSettings);

        extern private void DrawRenderers_StateBlock_Internal(FilterResults renderers, ref DrawRendererSettings drawSettings, FilterRenderersSettings filterSettings, RenderStateBlock stateBlock);

        extern private void DrawRenderers_StateMap_Internal(FilterResults renderers, ref DrawRendererSettings drawSettings, FilterRenderersSettings filterSettings, System.Array stateMap, int stateMapLength);

        extern private void DrawShadows_Internal(ref DrawShadowsSettings settings);

        [FreeFunction("UI::GetCanvasManager().EmitWorldGeometryForSceneView")]
        extern static public void EmitWorldGeometryForSceneView(Camera cullingCamera);

        extern private void ExecuteCommandBuffer_Internal(CommandBuffer commandBuffer);

        extern private void ExecuteCommandBufferAsync_Internal(CommandBuffer commandBuffer, ComputeQueueType queueType);

        extern private void SetupCameraProperties_Internal(Camera camera, bool stereoSetup);

        extern private void StereoEndRender_Internal(Camera camera);

        extern private void StartMultiEye_Internal(Camera camera);

        extern private void StopMultiEye_Internal(Camera camera);

        extern private void DrawSkybox_Internal(Camera camera);

        internal IntPtr Internal_GetPtr()
        {
            return m_Ptr;
        }
    }
}
