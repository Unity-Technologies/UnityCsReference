// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    [NativeType("Runtime/Graphics/ScriptableRenderLoop/ScriptableRenderContext.h")]
    [NativeHeader("Runtime/Graphics/ScriptableRenderLoop/ScriptableDrawRenderersUtility.h")]
    [NativeHeader("Runtime/Export/RenderPipeline/ScriptableRenderContext.bindings.h")]
    [NativeHeader("Runtime/Export/RenderPipeline/ScriptableRenderPipeline.bindings.h")]
    [NativeHeader("Modules/UI/Canvas.h")]
    [NativeHeader("Modules/UI/CanvasManager.h")]
    public partial struct ScriptableRenderContext
    {
        [FreeFunction("ScriptableRenderContext::BeginRenderPass")]
        static extern unsafe void BeginRenderPass_Internal(IntPtr self, int width, int height, int volumeDepth, int samples, IntPtr colors, int colorCount, int depthAttachmentIndex);

        [FreeFunction("ScriptableRenderContext::BeginSubPass")]
        static extern unsafe void BeginSubPass_Internal(IntPtr self, IntPtr colors, int colorCount, IntPtr inputs, int inputCount, bool isDepthReadOnly, bool isStencilReadOnly);

        [FreeFunction("ScriptableRenderContext::EndSubPass")]
        static extern void EndSubPass_Internal(IntPtr self);

        [FreeFunction("ScriptableRenderContext::EndRenderPass")]
        static extern void EndRenderPass_Internal(IntPtr self);

        [FreeFunction("ScriptableRenderPipeline_Bindings::Internal_Cull")]
        static extern void Internal_Cull(ref ScriptableCullingParameters parameters, ScriptableRenderContext renderLoop, IntPtr results);

        [FreeFunction("InitializeSortSettings")]
        internal static extern void InitializeSortSettings(Camera camera, out SortingSettings sortingSettings);

        extern private void Submit_Internal();
        extern private bool SubmitForRenderPassValidation_Internal();

        extern private void GetCameras_Internal(Type listType, object resultList);

        extern private unsafe void DrawRenderers_Internal(IntPtr cullResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings, ShaderTagId tagName, bool isPassTagName, IntPtr tagValues, IntPtr stateBlocks, int stateCount);

        extern private void DrawShadows_Internal(IntPtr shadowDrawingSettings);

        [FreeFunction("UI::GetCanvasManager().EmitWorldGeometryForSceneView")]
        extern static public void EmitWorldGeometryForSceneView(Camera cullingCamera);

        [FreeFunction("UI::GetCanvasManager().EmitGeometryForCamera")]
        extern static public void EmitGeometryForCamera(Camera camera);

        [NativeThrows]
        extern private void ExecuteCommandBuffer_Internal(CommandBuffer commandBuffer);

        [NativeThrows]
        extern private void ExecuteCommandBufferAsync_Internal(CommandBuffer commandBuffer, ComputeQueueType queueType);

        extern private void SetupCameraProperties_Internal([NotNull("NullExceptionObject")] Camera camera, bool stereoSetup, int eye);

        extern private void StereoEndRender_Internal([NotNull("NullExceptionObject")] Camera camera, int eye, bool isFinalPass);

        extern private void StartMultiEye_Internal([NotNull("NullExceptionObject")] Camera camera, int eye);

        extern private void StopMultiEye_Internal([NotNull("NullExceptionObject")] Camera camera);

        extern private void DrawSkybox_Internal([NotNull("NullExceptionObject")] Camera camera);

        extern private void InvokeOnRenderObjectCallback_Internal();

        extern private void DrawGizmos_Internal([NotNull("NullExceptionObject")] Camera camera, GizmoSubset gizmoSubset);

        extern private void DrawWireOverlay_Impl([NotNull("NullExceptionObject")] Camera camera);

        extern private void DrawUIOverlay_Internal([NotNull("NullExceptionObject")] Camera camera);

        internal IntPtr Internal_GetPtr()
        {
            return m_Ptr;
        }

        extern private unsafe RendererList CreateRendererList_Internal(IntPtr cullResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings, ShaderTagId tagName, bool isPassTagName, IntPtr tagValues, IntPtr stateBlocks, int stateCount);
        extern private unsafe RendererList CreateShadowRendererList_Internal(IntPtr shadowDrawinSettings);

        internal enum SkyboxXRMode //keep in sync with SkyboxRendererListXRMode (ScriptableDrawRenderers.h)
        {
            Off,
            Enabled,
            LegacySinglePass
        }
        extern private unsafe RendererList CreateSkyboxRendererList_Internal([NotNull("NullExceptionObject")] Camera camera, int mode, Matrix4x4 proj, Matrix4x4 view, Matrix4x4 projR, Matrix4x4 viewR);

        extern private unsafe void PrepareRendererListsAsync_Internal(object rendererLists);
        extern private RendererListStatus QueryRendererListStatus_Internal(RendererList handle);
    }
}
