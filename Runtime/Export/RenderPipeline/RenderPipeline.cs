// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public abstract class RenderPipeline
    {
        protected abstract void Render(ScriptableRenderContext context, Camera[] cameras);
        protected virtual void ProcessRenderRequests<RequestData>(ScriptableRenderContext context, Camera camera, RequestData renderRequest) { }
        protected virtual bool IsRenderRequestSupported<RequestData>(Camera camera, RequestData data) { return false; }

        protected static void BeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            RenderPipelineManager.BeginContextRendering(context, new List<Camera>(cameras));
        }

        protected static void BeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            RenderPipelineManager.BeginContextRendering(context, cameras);
        }

        protected static void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            RenderPipelineManager.BeginCameraRendering(context, camera);
        }

        protected static void EndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            RenderPipelineManager.EndContextRendering(context, cameras);
        }

        protected static void EndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            RenderPipelineManager.EndContextRendering(context, new List<Camera>(cameras));
        }

        protected static void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            RenderPipelineManager.EndCameraRendering(context, camera);
        }

        protected virtual void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            Render(context, cameras.ToArray());
        }

        internal void InternalRender(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (disposed)
                throw new ObjectDisposedException(string.Format("{0} has been disposed. Do not call Render on disposed a RenderPipeline.", this));

            Render(context, cameras);
        }

        internal void InternalProcessRenderRequests<RequestData>(ScriptableRenderContext context, Camera camera, RequestData renderRequest)
        {
            if (disposed)
                throw new ObjectDisposedException(string.Format("{0} has been disposed. Do not call Render on disposed a RenderPipeline.", this));

            ProcessRenderRequests(context, camera, renderRequest);
        }

        public static bool SupportsRenderRequest<RequestData>(Camera camera, RequestData data)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                if(RenderPipelineManager.currentPipeline == null)
                {
                    RenderPipelineManager.PrepareRenderPipeline(GraphicsSettings.currentRenderPipeline);
                }
                return RenderPipelineManager.currentPipeline.IsRenderRequestSupported(camera, data);
            }

            return false;
        }

        public static void SubmitRenderRequest<RequestData>(Camera camera, RequestData data)
        {
            camera.SubmitRenderRequest(data);
        }

        public class StandardRequest
        {
            public RenderTexture destination = null;
            public int mipLevel = 0;
            public CubemapFace face = CubemapFace.Unknown;
            public int slice = 0;
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

        public virtual RenderPipelineGlobalSettings defaultSettings
        {
            get { return null; }
        }
    }
}
