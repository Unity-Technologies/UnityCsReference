// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // TODO: Obsolete/remove this when TMP updates to use new API.
    public static class RenderPipeline
    {
        static RenderPipeline()
        {
            RenderPipelineManager.beginFrameRendering += (context, cameras) =>
            {
                if (beginFrameRendering != null)
                    beginFrameRendering(cameras);
            };

            RenderPipelineManager.beginCameraRendering += (context, camera) =>
            {
                if (beginCameraRendering != null)
                    beginCameraRendering(camera);
            };
        }

        public static event Action<Camera[]> beginFrameRendering;
        public static event Action<Camera> beginCameraRendering;
    }
}

namespace UnityEngine.Rendering
{
    public abstract class RenderPipeline
    {
        // Obsolete: Remove this when TMP, HDRP and LWRP will have updated their
        protected static void BeginFrameRendering(Camera[] cameras)
        {
            RenderPipelineManager.BeginFrameRendering(default(ScriptableRenderContext), cameras);
        }

        protected static void BeginCameraRendering(Camera camera)
        {
            RenderPipelineManager.BeginCameraRendering(default(ScriptableRenderContext), camera);
        }

        // End Obsolete code

        protected abstract void Render(ScriptableRenderContext context, Camera[] cameras);

        protected static void BeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            RenderPipelineManager.BeginFrameRendering(context, cameras);
        }

        protected static void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            RenderPipelineManager.BeginCameraRendering(context, camera);
        }

        protected static void EndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            RenderPipelineManager.EndFrameRendering(context, cameras);
        }

        protected static void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            RenderPipelineManager.EndCameraRendering(context, camera);
        }

        internal void InternalRender(ScriptableRenderContext context, Camera[] cameras)
        {
            if (disposed)
                throw new ObjectDisposedException(string.Format("{0} has been disposed. Do not call Render on disposed a RenderPipeline.", this));
            Render(context, cameras);
        }

        public bool disposed { get; private set; }

        internal void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            disposed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
