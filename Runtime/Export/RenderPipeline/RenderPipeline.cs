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
        public static event Action<Camera[]> beginFrameRendering
        {
            add { RenderPipelineManager.beginFrameRendering += value; }
            remove { RenderPipelineManager.beginFrameRendering -= value; }
        }

        public static event Action<Camera> beginCameraRendering
        {
            add { RenderPipelineManager.beginCameraRendering += value; }
            remove { RenderPipelineManager.beginCameraRendering -= value; }
        }
    }
}

namespace UnityEngine.Rendering
{
    public abstract class RenderPipeline
    {
        protected abstract void Render(ScriptableRenderContext context, Camera[] cameras);

        protected static void BeginFrameRendering(Camera[] cameras)
        {
            RenderPipelineManager.BeginFrameRendering(cameras);
        }

        protected static void BeginCameraRendering(Camera camera)
        {
            RenderPipelineManager.BeginCameraRendering(camera);
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
