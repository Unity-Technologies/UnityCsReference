// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    // Must match RenderStateBlock on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderStateBlock : IEquatable<RenderStateBlock>
    {
        public RenderStateBlock(RenderStateMask mask)
        {
            m_BlendState = BlendState.defaultValue;
            m_RasterState = RasterState.defaultValue;
            m_DepthState = DepthState.defaultValue;
            m_StencilState = StencilState.defaultValue;
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

        public bool Equals(RenderStateBlock other)
        {
            return m_BlendState.Equals(other.m_BlendState) && m_RasterState.Equals(other.m_RasterState) && m_DepthState.Equals(other.m_DepthState) && m_StencilState.Equals(other.m_StencilState) && m_StencilReference == other.m_StencilReference && m_Mask == other.m_Mask;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RenderStateBlock && Equals((RenderStateBlock)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_BlendState.GetHashCode();
                hashCode = (hashCode * 397) ^ m_RasterState.GetHashCode();
                hashCode = (hashCode * 397) ^ m_DepthState.GetHashCode();
                hashCode = (hashCode * 397) ^ m_StencilState.GetHashCode();
                hashCode = (hashCode * 397) ^ m_StencilReference;
                hashCode = (hashCode * 397) ^ (int)m_Mask;
                return hashCode;
            }
        }

        public static bool operator==(RenderStateBlock left, RenderStateBlock right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(RenderStateBlock left, RenderStateBlock right)
        {
            return !left.Equals(right);
        }
    }
}
