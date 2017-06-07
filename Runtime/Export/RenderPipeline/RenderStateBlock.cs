// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // Must match RenderStateBlock on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderStateBlock
    {
        public RenderStateBlock(RenderStateMask mask)
        {
            m_BlendState = BlendState.Default;
            m_RasterState = RasterState.Default;
            m_DepthState = DepthState.Default;
            m_StencilState = StencilState.Default;
            m_StencilReference = 0;
            m_Mask = mask;
        }

        public BlendState blendState
        {
            get { return m_BlendState; }
            set { m_BlendState = value; }
        }

        public RasterState rasterState
        {
            get { return m_RasterState; }
            set { m_RasterState = value; }
        }

        public DepthState depthState
        {
            get { return m_DepthState; }
            set { m_DepthState = value; }
        }

        public StencilState stencilState
        {
            get { return m_StencilState; }
            set { m_StencilState = value; }
        }

        public int stencilReference
        {
            get { return m_StencilReference; }
            set { m_StencilReference = value; }
        }

        public RenderStateMask mask
        {
            get { return m_Mask; }
            set { m_Mask = value; }
        }

        BlendState m_BlendState;
        RasterState m_RasterState;
        DepthState m_DepthState;
        StencilState m_StencilState;
        int m_StencilReference;
        RenderStateMask m_Mask;
    }
}
