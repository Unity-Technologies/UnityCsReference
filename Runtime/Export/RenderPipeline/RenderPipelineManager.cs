// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    public static class RenderPipelineManager
    {
        static RenderPipelineAsset s_CurrentPipelineAsset;

        public static RenderPipeline currentPipeline { get; private set; }

        public static event Action<ScriptableRenderContext, Camera[]> beginFrameRendering;
        public static event Action<ScriptableRenderContext, Camera> beginCameraRendering;
        public static event Action<ScriptableRenderContext, Camera[]> endFrameRendering;
        public static event Action<ScriptableRenderContext, Camera> endCameraRendering;

        internal static void BeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            beginFrameRendering?.Invoke(context, cameras);
        }

        internal static void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            beginCameraRendering?.Invoke(context, camera);
        }

        internal static void EndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            endFrameRendering?.Invoke(context, cameras);
        }

        internal static void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            endCameraRendering?.Invoke(context, camera);
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
        static void DoRenderLoop_Internal(RenderPipelineAsset pipe, Camera[] cameras, IntPtr loopPtr, AtomicSafetyHandle safety)
        {
            PrepareRenderPipeline(pipe);
            if (currentPipeline == null)
                return;

            var loop =
                new ScriptableRenderContext(loopPtr, safety);
            currentPipeline.InternalRender(loop, cameras);
        }

        static void PrepareRenderPipeline(RenderPipelineAsset pipelineAsset)
        {
            if (!ReferenceEquals(s_CurrentPipelineAsset, pipelineAsset))
            {
                // Required because when switching to a RenderPipeline asset for the first time
                // it will call OnValidate on the new asset before cleaning up the old one. Thus we
                // reset the rebuild in order to cleanup properly.
                CleanupRenderPipeline();
                s_CurrentPipelineAsset = pipelineAsset;
            }

            if (s_CurrentPipelineAsset != null
                && (currentPipeline == null || currentPipeline.disposed))
            {
                currentPipeline = s_CurrentPipelineAsset.InternalCreatePipeline();
            }
        }
    }
}
