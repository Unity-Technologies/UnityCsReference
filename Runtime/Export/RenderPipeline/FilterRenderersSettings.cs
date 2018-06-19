// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FilterRenderersSettings
    {
        RenderQueueRange    m_RenderQueueRange;
        int                 m_LayerMask;
        UInt32              m_RenderingLayerMask;
        private Int32       m_ExcludeMotionVectorObjects;

        public FilterRenderersSettings(bool initializeValues = false) : this()
        {
            if (initializeValues)
            {
                m_RenderQueueRange = RenderQueueRange.all;
                m_LayerMask = ~0;
                m_RenderingLayerMask = UInt32.MaxValue;
                m_ExcludeMotionVectorObjects = 0;
            }
        }

        public RenderQueueRange renderQueueRange
        {
            get { return m_RenderQueueRange; }
            set { m_RenderQueueRange = value; }
        }

        public int layerMask
        {
            get { return m_LayerMask; }
            set { m_LayerMask = value; }
        }

        public UInt32 renderingLayerMask
        {
            get { return m_RenderingLayerMask; }
            set { m_RenderingLayerMask = value; }
        }

        public bool excludeMotionVectorObjects
        {
            get { return m_ExcludeMotionVectorObjects != 0; }
            set { m_ExcludeMotionVectorObjects = value ? 1 : 0; }
        }
    }
}
