// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    // Must match GfxRasterState on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct RasterState : IEquatable<RasterState>
    {
        // Passing a single parameter here to force non-default constructor
        public static readonly RasterState defaultValue = new RasterState(CullMode.Back);

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
            m_Conservative = Convert.ToByte(false);
            m_Padding1 = 0;
            m_Padding2 = 0;
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

        public bool conservative
        {
            get { return Convert.ToBoolean(m_Conservative); }
            set { m_Conservative = Convert.ToByte(value); }
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
        byte m_Conservative;
        byte m_Padding1;
        byte m_Padding2;

        public bool Equals(RasterState other)
        {
            return m_CullingMode == other.m_CullingMode && m_OffsetUnits == other.m_OffsetUnits && m_OffsetFactor.Equals(other.m_OffsetFactor) && m_DepthClip == other.m_DepthClip && m_Conservative == other.m_Conservative;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RasterState && Equals((RasterState)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)m_CullingMode;
                hashCode = (hashCode * 397) ^ m_OffsetUnits;
                hashCode = (hashCode * 397) ^ m_OffsetFactor.GetHashCode();
                hashCode = (hashCode * 397) ^ m_DepthClip.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Conservative.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator==(RasterState left, RasterState right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(RasterState left, RasterState right)
        {
            return !left.Equals(right);
        }
    }
}
