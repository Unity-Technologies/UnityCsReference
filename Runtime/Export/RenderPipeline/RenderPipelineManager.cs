// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    public static class RenderPipelineManager
    {
        private static IRenderPipelineAsset s_CurrentPipelineAsset;
        static Camera[] s_Cameras = new Camera[0];
        static int s_CameraCapacity = 0;

        public static IRenderPipeline currentPipeline { get; private set; }

        [RequiredByNativeCode]
        internal static void CleanupRenderPipeline()
        {
            if (s_CurrentPipelineAsset != null)
            {
                s_CurrentPipelineAsset.DestroyCreatedInstances();
                s_CurrentPipelineAsset = null;
                currentPipeline = null;
                SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
            }
        }

        private static void GetCameras(ScriptableRenderContext context)
        {
            int numCams = context.GetNumberOfCameras();
            if (numCams != s_CameraCapacity)
            {
                Array.Resize(ref s_Cameras, numCams);
                s_CameraCapacity = numCams;
            }

            for (int i = 0; i < numCams; ++i)
            {
                s_Cameras[i] = context.GetCamera(i);
            }
        }

        [RequiredByNativeCode]
        static void DoRenderLoop_Internal(IRenderPipelineAsset pipe, IntPtr loopPtr)
        {
            PrepareRenderPipeline(pipe);
            if (currentPipeline == null)
                return;

            ScriptableRenderContext loop = new ScriptableRenderContext(loopPtr);
            Array.Clear(s_Cameras, 0, s_Cameras.Length);
            GetCameras(loop);
            currentPipeline.Render(loop, s_Cameras);

            Array.Clear(s_Cameras, 0, s_Cameras.Length);
        }

        private static void PrepareRenderPipeline(IRenderPipelineAsset pipe)
        {
            // UnityObject overloads operator == and treats destroyed objects and null as equals
            // However here is needed to differentiate them in other to bookkeep RenderPipeline lifecycle
            if ((object)s_CurrentPipelineAsset != (object)pipe)
            {
                if (s_CurrentPipelineAsset != null)
                {
                    // Required because when switching to a RenderPipeline asset for the first time
                    // it will call OnValidate on the new asset before cleaning up the old one. Thus we
                    // reset the rebuild in order to cleanup properly.
                    CleanupRenderPipeline();
                }

                s_CurrentPipelineAsset = pipe;
            }

            if (s_CurrentPipelineAsset != null
                && (currentPipeline == null || currentPipeline.disposed))
                currentPipeline = s_CurrentPipelineAsset.CreatePipeline();
        }
    }
}
