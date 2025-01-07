// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public abstract class RenderPipeline
    {
        [Obsolete("Render with an array parameter is deprecated. Use Render with a list parameter instead. If you're extending the RenderPipeline class, override the Render method with a List parameter to perform rendering in order to avoid unnecessary allocations and copies. #from 6000.1", false)]
        protected virtual void Render(ScriptableRenderContext context, Camera[] cameras) { }
        protected virtual void ProcessRenderRequests<RequestData>(ScriptableRenderContext context, Camera camera, RequestData renderRequest) { }
        protected internal virtual bool IsRenderRequestSupported<RequestData>(Camera camera, RequestData data) { return false; }

        [Obsolete("BeginFrameRendering is deprecated. Use BeginContextRendering instead. #from 6000.1", false)]
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

        [Obsolete("EndFrameRendering is deprecated. Use EndContextRendering instead. #from 6000.1", false)]
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
            //To do: Remove when the array version will be removed
#pragma warning disable CS0618 // Type or member is obsolete
            Render(context, cameras.ToArray());
#pragma warning restore CS0618 // Type or member is obsolete
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
            var pipelineExistsAndSupportsRequest = false;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                if (RenderPipelineManager.currentPipeline == null)
                {
                    bool renderPipelineConstructed = RenderPipelineManager.TryPrepareRenderPipeline(GraphicsSettings.currentRenderPipeline);
                    Debug.Assert(renderPipelineConstructed);
                }

                if (RenderPipelineManager.currentPipeline != null)
                    pipelineExistsAndSupportsRequest = RenderPipelineManager.currentPipeline.IsRenderRequestSupported(camera, data);
            }
            if (data is ObjectIdRequest)
            {
                // There is a built-in fallback option available for ObjectIdRequest in editor;
                // see Camera.SubmitRenderRequest
                pipelineExistsAndSupportsRequest = true;
            }
            return pipelineExistsAndSupportsRequest;
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
