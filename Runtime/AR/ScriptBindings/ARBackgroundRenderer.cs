// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Rendering;

namespace UnityEngine.XR
{
    // Must match ARRenderMode in Runtime/AR/ARTypes.h
    public enum ARRenderMode
    {
        StandardBackground,
        MaterialAsBackground
    };

    public partial class ARBackgroundRenderer
    {
        protected Camera m_Camera = null;
        protected Material m_BackgroundMaterial = null;
        protected Texture m_BackgroundTexture = null;
        private ARRenderMode m_RenderMode = ARRenderMode.StandardBackground;
        private CommandBuffer m_CommandBuffer = null;
        private CameraClearFlags m_CameraClearFlags = CameraClearFlags.Skybox;

        // Actions to be subscribed to by AR platform code
        public event Action backgroundRendererChanged = null;

        // Apply a new Material and reset the command buffers if needed
        public Material backgroundMaterial
        {
            get
            {
                return m_BackgroundMaterial;
            }
            set
            {
                if (m_BackgroundMaterial == value)
                    return;

                RemoveCommandBuffersIfNeeded();
                m_BackgroundMaterial = value;

                if (backgroundRendererChanged != null)
                    backgroundRendererChanged();

                ReapplyCommandBuffersIfNeeded();
            }
        }

        // Apply a new Texture and reset the command buffers if needed
        public Texture backgroundTexture
        {
            get
            {
                return m_BackgroundTexture;
            }
            set
            {
                if (m_BackgroundTexture = value)
                    return;

                RemoveCommandBuffersIfNeeded();
                m_BackgroundTexture = value;

                if (backgroundRendererChanged != null)
                    backgroundRendererChanged();

                ReapplyCommandBuffersIfNeeded();
            }
        }

        // Apply a new Camera and reset the command buffers if needed
        public Camera camera
        {
            get
            {
                // Return main camera when no Camera has been set
                return (m_Camera != null) ? m_Camera : Camera.main;
            }
            set
            {
                if (m_Camera == value)
                    return;

                RemoveCommandBuffersIfNeeded();
                m_Camera = value;

                if (backgroundRendererChanged != null)
                    backgroundRendererChanged();

                ReapplyCommandBuffersIfNeeded();
            }
        }

        // Apply a new render mode and reset the command buffers if needed
        public ARRenderMode mode
        {
            get
            {
                return m_RenderMode;
            }
            set
            {
                if (value == m_RenderMode)
                    return;

                m_RenderMode = value;

                switch (m_RenderMode)
                {
                    case ARRenderMode.StandardBackground:
                        DisableARBackgroundRendering();
                        break;
                    case ARRenderMode.MaterialAsBackground:
                        EnableARBackgroundRendering();
                        break;
                    default:
                        throw new Exception("Unhandled render mode.");
                }

                if (backgroundRendererChanged != null)
                    backgroundRendererChanged();
            }
        }

        protected bool EnableARBackgroundRendering()
        {
            if (m_BackgroundMaterial == null)
                return false;

            Camera camera;

            if (m_Camera != null)
                camera = m_Camera;
            else
                camera = Camera.main;

            if (camera == null)
                return false;

            // Clear flags
            m_CameraClearFlags = camera.clearFlags;
            camera.clearFlags = CameraClearFlags.Depth;

            // Command buffer setup
            m_CommandBuffer = new CommandBuffer();

            var backgroundTexture = m_BackgroundTexture;
            if (backgroundTexture == null)
            {
                const string kMainTexName = "_MainTex";

                // GetTexture will return null if the texture isn't found, but it also
                // writes an error to the console. We check for existence to silence
                // this error.
                if (m_BackgroundMaterial.HasProperty(kMainTexName))
                    backgroundTexture = m_BackgroundMaterial.GetTexture(kMainTexName);
            }

            m_CommandBuffer.Blit(backgroundTexture, BuiltinRenderTextureType.CameraTarget, m_BackgroundMaterial);
            camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, m_CommandBuffer);
            camera.AddCommandBuffer(CameraEvent.BeforeGBuffer, m_CommandBuffer);

            return true;
        }

        protected void DisableARBackgroundRendering()
        {
            if (null == m_CommandBuffer)
                return;

            Camera camera;

            if (m_Camera != null)
                camera = m_Camera;
            else
                camera = Camera.main;

            if (camera != null)
                camera.clearFlags = m_CameraClearFlags;

            // Command buffer
            camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, m_CommandBuffer);
            camera.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, m_CommandBuffer);
        }

        private bool ReapplyCommandBuffersIfNeeded()
        {
            if (m_RenderMode != ARRenderMode.MaterialAsBackground)
                return false;

            EnableARBackgroundRendering();

            return true;
        }

        private bool RemoveCommandBuffersIfNeeded()
        {
            if (m_RenderMode != ARRenderMode.MaterialAsBackground)
                return false;

            DisableARBackgroundRendering();

            return true;
        }
    }
}
