// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public partial struct ScriptableRenderContext
    {
        //@TODO: Would be good if there was some safety
        // against keeping hold RenderLoop after destruction
        private IntPtr m_Ptr;

        internal ScriptableRenderContext(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        public void Submit()
        {
            CheckValid();
            Submit_Internal();
        }

        public void DrawRenderers(FilterResults renderers, ref DrawRendererSettings drawSettings, FilterRenderersSettings filterSettings)
        {
            CheckValid();
            DrawRenderers_Internal(renderers, ref drawSettings, filterSettings);
        }

        public void DrawRenderers(FilterResults renderers, ref DrawRendererSettings drawSettings, FilterRenderersSettings filterSettings, RenderStateBlock stateBlock)
        {
            CheckValid();
            DrawRenderers_StateBlock_Internal(renderers, ref drawSettings, filterSettings, stateBlock);
        }

        public void DrawRenderers(FilterResults renderers, ref DrawRendererSettings drawSettings, FilterRenderersSettings filterSettings, List<RenderStateMapping> stateMap)
        {
            CheckValid();
            DrawRenderers_StateMap_Internal(renderers, ref drawSettings, filterSettings, NoAllocHelpers.ExtractArrayFromList(stateMap), stateMap.Count);
        }

        public void DrawShadows(ref DrawShadowsSettings settings)
        {
            CheckValid();
            DrawShadows_Internal(ref settings);
        }

        public void ExecuteCommandBuffer(CommandBuffer commandBuffer)
        {
            if (commandBuffer == null)
                throw new ArgumentNullException("commandBuffer");

            CheckValid();
            ExecuteCommandBuffer_Internal(commandBuffer);
        }

        public void ExecuteCommandBufferAsync(CommandBuffer commandBuffer, ComputeQueueType queueType)
        {
            if (commandBuffer == null)
                throw new ArgumentNullException("commandBuffer");

            CheckValid();
            ExecuteCommandBufferAsync_Internal(commandBuffer, queueType);
        }

        public void SetupCameraProperties(Camera camera)
        {
            CheckValid();
            SetupCameraProperties_Internal(camera,  false);
        }

        public void SetupCameraProperties(Camera camera, bool stereoSetup)
        {
            CheckValid();
            SetupCameraProperties_Internal(camera, stereoSetup);
        }

        public void StereoEndRender(Camera camera)
        {
            CheckValid();
            StereoEndRender_Internal(camera);
        }

        public void StartMultiEye(Camera camera)
        {
            CheckValid();
            StartMultiEye_Internal(camera);
        }

        public void StopMultiEye(Camera camera)
        {
            CheckValid();
            StopMultiEye_Internal(camera);
        }

        public void DrawSkybox(Camera camera)
        {
            CheckValid();
            DrawSkybox_Internal(camera);
        }

        internal void CheckValid()
        {
            if (m_Ptr.ToInt64() == 0)
                throw new ArgumentException("Invalid ScriptableRenderContext.  This can be caused by allocating a context in user code.");
        }
    }
}
