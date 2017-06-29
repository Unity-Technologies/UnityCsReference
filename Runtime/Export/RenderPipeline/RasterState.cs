// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // Must match GfxRasterState on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct RasterState
    {
        // Passing a single parameter here to force non-default constructor
        public static readonly RasterState Default = new RasterState(CullMode.Back);

        public RasterState(
            CullMode cullingMode = CullMode.Back,
            int offsetUnits = 0,
            float offsetFactor = 0f,
            bool depthClip = true)
        {
            m_CullingMode = cullingMode;
            m_OffsetUnits = offsetUnits;
            m_OffsetFactor = offsetFactor;
            m_DepthClip = Convert.ToByte(depthClip);
        }

        public CullMode cullingMode
        {
            get { return m_CullingMode; }
            set { m_CullingMode = value; }
        }

        public bool depthClip
        {
            get { return Convert.ToBoolean(m_DepthClip); }
            set { m_DepthClip = Convert.ToByte(value); }
        }

        public int offsetUnits
        {
            get { return m_OffsetUnits; }
            set { m_OffsetUnits = value; }
        }

        public float offsetFactor
        {
            get { return m_OffsetFactor; }
            set { m_OffsetFactor = value; }
        }

        CullMode m_CullingMode;
        int m_OffsetUnits;
        float m_OffsetFactor;
        byte m_DepthClip;
    }
}
