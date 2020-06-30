// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    public abstract class RenderPipeline
    {
        protected abstract void Render(ScriptableRenderContext context, Camera[] cameras);
        protected virtual void ProcessRenderRequests(ScriptableRenderContext context, Camera camera, List<Camera.RenderRequest> renderRequests) {}

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

        internal void InternalRenderWithRequests(ScriptableRenderContext context, Camera[] cameras, List<Camera.RenderRequest> renderRequests)
        {
            if (disposed)
                throw new ObjectDisposedException(string.Format("{0} has been disposed. Do not call Render on disposed a RenderPipeline.", this));

            ProcessRenderRequests(context, (cameras == null || cameras.Length == 0) ? null : cameras[0], renderRequests);
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
