// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FilterRenderersSettings
    {
        RenderQueueRange m_RenderQueueRange;
        int m_LayerMask;

        public FilterRenderersSettings(bool initializeValues = false) : this()
        {
            if (initializeValues)
            {
                m_RenderQueueRange = RenderQueueRange.all;
                m_LayerMask = ~0;
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
    }
}
