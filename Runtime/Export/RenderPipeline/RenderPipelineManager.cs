// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    public static class RenderPipelineManager
    {
        static RenderPipelineAsset s_CurrentPipelineAsset;

        public static RenderPipeline currentPipeline { get; private set; }

        public static event Action<Camera[]> beginFrameRendering;
        public static event Action<Camera> beginCameraRendering;

        internal static void BeginFrameRendering(Camera[] cameras)
        {
            beginFrameRendering?.Invoke(cameras);
        }

        internal static void BeginCameraRendering(Camera camera)
        {
            beginCameraRendering?.Invoke(camera);
        }

        [RequiredByNativeCode]
        internal static void CleanupRenderPipeline()
        {
            if (s_CurrentPipelineAsset != null)
            {
                s_CurrentPipelineAsset.DestroyInstances();
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
                if (s_CurrentPipelineAsset != null)
                {
                    // Required because when switching to a RenderPipeline asset for the first time
                    // it will call OnValidate on the new asset before cleaning up the old one. Thus we
                    // reset the rebuild in order to cleanup properly.
                    CleanupRenderPipeline();
                }

                s_CurrentPipelineAsset = pipelineAsset;
            }

            if (s_CurrentPipelineAsset != null
                && (currentPipeline == null || currentPipeline.disposed))
                currentPipeline = s_CurrentPipelineAsset.InternalCreatePipeline();
        }
    }
}
