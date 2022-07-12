// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    public static class RenderPipelineManager
    {
        internal static RenderPipelineAsset s_CurrentPipelineAsset;
        static List<Camera> s_Cameras = new List<Camera>();

        private static string s_CurrentPipelineType;
        const string k_BuiltinPipelineName = "Built-in Pipeline";

        private static RenderPipeline s_CurrentPipeline = null;
        public static RenderPipeline currentPipeline
        {
            get => s_CurrentPipeline;
            private set
            {
                s_CurrentPipelineType = (value != null) ? value.GetType().ToString() : k_BuiltinPipelineName;
                s_CurrentPipeline = value;
            }
        }

        public static event Action<ScriptableRenderContext, List<Camera>> beginContextRendering;
        public static event Action<ScriptableRenderContext, List<Camera>> endContextRendering;
        public static event Action<ScriptableRenderContext, Camera[]> beginFrameRendering;
        public static event Action<ScriptableRenderContext, Camera> beginCameraRendering;
        public static event Action<ScriptableRenderContext, Camera[]> endFrameRendering;
        public static event Action<ScriptableRenderContext, Camera> endCameraRendering;

        public static event Action activeRenderPipelineTypeChanged;
        public static event Action<RenderPipelineAsset, RenderPipelineAsset> activeRenderPipelineAssetChanged;

        internal static void BeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            beginFrameRendering?.Invoke(context, cameras.ToArray());
            beginContextRendering?.Invoke(context, cameras);
        }

        internal static void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            beginCameraRendering?.Invoke(context, camera);
        }

        internal static void EndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            endFrameRendering?.Invoke(context, cameras.ToArray());
            endContextRendering?.Invoke(context, cameras);
        }

        internal static void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            endCameraRendering?.Invoke(context, camera);
        }

        [RequiredByNativeCode]
        internal static void OnActiveRenderPipelineTypeChanged()
        {
            activeRenderPipelineTypeChanged?.Invoke();
        }

        [RequiredByNativeCode]
        internal static void OnActiveRenderPipelineAssetChanged(ScriptableObject from, ScriptableObject to)
        {
            activeRenderPipelineAssetChanged?.Invoke(from as RenderPipelineAsset, to as RenderPipelineAsset);
        }

        [RequiredByNativeCode]
        internal static void HandleRenderPipelineChange(RenderPipelineAsset pipelineAsset)
        {
            bool hasRPAssetChanged = !ReferenceEquals(s_CurrentPipelineAsset, pipelineAsset);
            if (hasRPAssetChanged)
            {
                // Required because when switching to a RenderPipeline asset for the first time
                // it will call OnValidate on the new asset before cleaning up the old one. Thus we
                // reset the rebuild in order to cleanup properly.
                CleanupRenderPipeline();
                s_CurrentPipelineAsset = pipelineAsset;
            }
        }

        [RequiredByNativeCode]
        internal static void CleanupRenderPipeline()
        {
            if (currentPipeline != null && !currentPipeline.disposed)
            {
                currentPipeline.Dispose();
                s_CurrentPipelineAsset = null;
                currentPipeline = null;
                SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
            }
        }

        [RequiredByNativeCode]
        static string GetCurrentPipelineAssetType()
        {
            return s_CurrentPipelineType;
        }

        [RequiredByNativeCode]
        static void DoRenderLoop_Internal(RenderPipelineAsset pipe, IntPtr loopPtr, Object renderRequest, AtomicSafetyHandle safety)
        {
            PrepareRenderPipeline(pipe);

            if (currentPipeline == null)
                return;

            var loop =
                new ScriptableRenderContext(loopPtr, safety);
            s_Cameras.Clear();

            loop.GetCameras(s_Cameras);
            if (renderRequest == null)
                currentPipeline.InternalRender(loop, s_Cameras);
            else
                currentPipeline.InternalProcessRenderRequests(loop, s_Cameras[0], renderRequest);

            s_Cameras.Clear();
        }

        internal static void PrepareRenderPipeline(RenderPipelineAsset pipelineAsset)
        {
            HandleRenderPipelineChange(pipelineAsset);

            if (s_CurrentPipelineAsset != null
                && (currentPipeline == null || currentPipeline.disposed))
            {
                currentPipeline = s_CurrentPipelineAsset.InternalCreatePipeline();
            }
        }
    }
}
