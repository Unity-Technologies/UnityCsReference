// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Internal;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FilteringSettings : IEquatable<FilteringSettings>
    {
        RenderQueueRange m_RenderQueueRange;
        int m_LayerMask;
        uint m_RenderingLayerMask;
        uint m_BatchLayerMask;
        int m_ExcludeMotionVectorObjects;
        int m_ForceAllMotionVectorObjects;
        SortingLayerRange m_SortingLayerRange;

        public static FilteringSettings defaultValue => new FilteringSettings(RenderQueueRange.all);

        public FilteringSettings([DefaultValue("RenderQueueRange.all")] RenderQueueRange? renderQueueRange = null, int layerMask = ~0, uint renderingLayerMask = uint.MaxValue, int excludeMotionVectorObjects = 0) : this()
        {
            m_RenderQueueRange = renderQueueRange ?? RenderQueueRange.all;
            m_LayerMask = layerMask;
            m_RenderingLayerMask = renderingLayerMask;
            m_BatchLayerMask = uint.MaxValue;
            m_ExcludeMotionVectorObjects = excludeMotionVectorObjects;
            m_ForceAllMotionVectorObjects = 0;
            m_SortingLayerRange = SortingLayerRange.all;
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

        public uint renderingLayerMask
        {
            get { return m_RenderingLayerMask; }
            set { m_RenderingLayerMask = value; }
        }

        public uint batchLayerMask
        {
            get { return m_BatchLayerMask; }
            set { m_BatchLayerMask = value; }
        }

        public bool excludeMotionVectorObjects
        {
            get { return m_ExcludeMotionVectorObjects != 0; }
            set { m_ExcludeMotionVectorObjects = value ? 1 : 0; }
        }
        public bool forceAllMotionVectorObjects
        {
            get { return m_ForceAllMotionVectorObjects != 0; }
            set { m_ForceAllMotionVectorObjects = value ? 1 : 0; }
        }

        public SortingLayerRange sortingLayerRange
        {
            get { return m_SortingLayerRange; }
            set { m_SortingLayerRange = value; }
        }

        public bool Equals(FilteringSettings other)
        {
            return m_RenderQueueRange.Equals(other.m_RenderQueueRange)
                && m_LayerMask == other.m_LayerMask
                && m_RenderingLayerMask == other.m_RenderingLayerMask
                && m_BatchLayerMask == other.m_BatchLayerMask
                && m_ExcludeMotionVectorObjects == other.m_ExcludeMotionVectorObjects
                && m_ForceAllMotionVectorObjects == other.m_ForceAllMotionVectorObjects;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is FilteringSettings && Equals((FilteringSettings)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_RenderQueueRange.GetHashCode();
                hashCode = (hashCode * 397) ^ m_LayerMask;
                hashCode = (hashCode * 397) ^ (int)m_RenderingLayerMask;
                hashCode = (hashCode * 397) ^ (int)m_BatchLayerMask;
                hashCode = (hashCode * 397) ^ m_ExcludeMotionVectorObjects;
                hashCode = (hashCode * 397) ^ m_ForceAllMotionVectorObjects;
                return hashCode;
            }
        }

        public static bool operator==(FilteringSettings left, FilteringSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(FilteringSettings left, FilteringSettings right)
        {
            return !left.Equals(right);
        }
    }
}
