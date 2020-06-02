// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System;
using Unity.Collections;

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
        static extern unsafe void BeginRenderPass_Internal(IntPtr self, int width, int height, int samples, IntPtr colors, int colorCount, int depthAttachmentIndex);

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

        extern private int GetNumberOfCameras_Internal();

        extern private Camera GetCamera_Internal(int index);

        extern private unsafe void DrawRenderers_Internal(IntPtr cullResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings, IntPtr renderTypes, IntPtr stateBlocks, int stateCount);

        extern private void DrawShadows_Internal(IntPtr shadowDrawingSettings);

        [FreeFunction("UI::GetCanvasManager().EmitWorldGeometryForSceneView")]
        extern static public void EmitWorldGeometryForSceneView(Camera cullingCamera);

        [NativeThrows]
        extern private void ExecuteCommandBuffer_Internal(CommandBuffer commandBuffer);

        [NativeThrows]
        extern private void ExecuteCommandBufferAsync_Internal(CommandBuffer commandBuffer, ComputeQueueType queueType);

        extern private void SetupCameraProperties_Internal(Camera camera, bool stereoSetup, int eye);

        extern private void StereoEndRender_Internal(Camera camera, int eye, bool isFinalPass);

        extern private void StartMultiEye_Internal(Camera camera, int eye);

        extern private void StopMultiEye_Internal(Camera camera);

        extern private void DrawSkybox_Internal(Camera camera);

        extern private void InvokeOnRenderObjectCallback_Internal();

        extern private void DrawGizmos_Internal(Camera camera, GizmoSubset gizmoSubset);

        extern private void DrawWireOverlay_Impl(Camera camera);

        extern private void DrawUIOverlay_Internal(Camera camera);

        internal IntPtr Internal_GetPtr()
        {
            return m_Ptr;
        }
    }
}
