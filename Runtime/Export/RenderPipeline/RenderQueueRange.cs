// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderQueueRange : IEquatable<RenderQueueRange>
    {
        int m_LowerBound;

        int m_UpperBound;

        const int k_MinimumBound = 0;
        public static readonly int minimumBound = k_MinimumBound;

        const int k_MaximumBound = 5000;
        public static readonly int maximumBound = k_MaximumBound;

        public RenderQueueRange(int lowerBound, int upperBound)
        {
            if (lowerBound < k_MinimumBound || lowerBound > k_MaximumBound)
                throw new ArgumentOutOfRangeException(nameof(lowerBound), lowerBound, $"The lower bound must be at least {k_MinimumBound} and at most {k_MaximumBound}.");
            if (upperBound < k_MinimumBound || upperBound > k_MaximumBound)
                throw new ArgumentOutOfRangeException(nameof(upperBound), upperBound, $"The upper bound must be at least {k_MinimumBound} and at most {k_MaximumBound}.");
            m_LowerBound = lowerBound;
            m_UpperBound = upperBound;
        }

        public static RenderQueueRange all => new RenderQueueRange { m_LowerBound = k_MinimumBound, m_UpperBound = k_MaximumBound };

        public static RenderQueueRange opaque => new RenderQueueRange { m_LowerBound = k_MinimumBound, m_UpperBound = (int)UnityEngine.Rendering.RenderQueue.GeometryLast };

        public static RenderQueueRange transparent => new RenderQueueRange { m_LowerBound = (int)UnityEngine.Rendering.RenderQueue.GeometryLast + 1, m_UpperBound = k_MaximumBound };

        public int lowerBound
        {
            get { return m_LowerBound; }
            set
            {
                if (value < k_MinimumBound || value > k_MaximumBound)
                    throw new ArgumentOutOfRangeException($"The lower bound must be at least {k_MinimumBound} and at most {k_MaximumBound}.");
                m_LowerBound = value;
            }
        }

        public int upperBound
        {
            get { return m_UpperBound; }
            set
            {
                if (value < k_MinimumBound || value > k_MaximumBound)
                    throw new ArgumentOutOfRangeException($"The upper bound must be at least {k_MinimumBound} and at most {k_MaximumBound}.");
                m_UpperBound = value;
            }
        }

        public bool Equals(RenderQueueRange other)
        {
            return m_LowerBound == other.m_LowerBound && m_UpperBound == other.m_UpperBound;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RenderQueueRange && Equals((RenderQueueRange)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_LowerBound * 397) ^ m_UpperBound;
            }
        }

        public static bool operator==(RenderQueueRange left, RenderQueueRange right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(RenderQueueRange left, RenderQueueRange right)
        {
            return !left.Equals(right);
        }
    }
}
