// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SortingLayerRange : IEquatable<SortingLayerRange>
    {
        private short m_LowerBound;
        private short m_UpperBound;

        public SortingLayerRange(short lowerBound, short upperBound)
        {
            m_LowerBound = lowerBound;
            m_UpperBound = upperBound;
        }

        public short lowerBound
        {
            get { return m_LowerBound; }
            set { m_LowerBound = value; }
        }
        public short upperBound
        {
            get { return m_UpperBound; }
            set { m_UpperBound = value; }
        }

        public static SortingLayerRange all => new SortingLayerRange { m_LowerBound = short.MinValue, m_UpperBound = short.MaxValue };

        public bool Equals(SortingLayerRange other)
        {
            return m_LowerBound == other.m_LowerBound && m_UpperBound == other.m_UpperBound;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SortingLayerRange))
                return false;

            return Equals((SortingLayerRange)obj);
        }

        public static bool operator!=(SortingLayerRange lhs, SortingLayerRange rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator==(SortingLayerRange lhs, SortingLayerRange rhs)
        {
            return lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            return ((int)m_UpperBound << 16) | ((int)m_LowerBound & 0xFFFF);
        }
    }
}
