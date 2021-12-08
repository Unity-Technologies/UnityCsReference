// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct ShadowSplitData : IEquatable<ShadowSplitData>
    {
        const int k_MaximumCullingPlaneCount = 10;
        public static readonly int maximumCullingPlaneCount = 10;

        int m_CullingPlaneCount;
        // can't make fixed types private, because then the compiler generates different code which BindingsGenerator does not handle yet.
        internal fixed byte m_CullingPlanes[k_MaximumCullingPlaneCount * Plane.size];
        Vector4 m_CullingSphere;
        float m_ShadowCascadeBlendCullingFactor;
        private float m_CullingNearPlane;
        Matrix4x4 m_CullingMatrix;

        public int cullingPlaneCount
        {
            get { return m_CullingPlaneCount; }
            set
            {
                if (value < 0 || value > k_MaximumCullingPlaneCount)
                    throw new ArgumentException($"Value should range from {0} to ShadowSplitData.maximumCullingPlaneCount ({k_MaximumCullingPlaneCount}), but was {value}.");
                m_CullingPlaneCount = value;
            }
        }

        public Vector4 cullingSphere
        {
            get { return m_CullingSphere; }
            set { m_CullingSphere = value; }
        }

        public Matrix4x4 cullingMatrix
        {
            get { return m_CullingMatrix; }
            set { m_CullingMatrix = value; }
        }


        public float shadowCascadeBlendCullingFactor
        {
            get { return m_ShadowCascadeBlendCullingFactor; }
            set
            {
                if (value < 0f || value > 1f)
                    throw new ArgumentException($"Value should range from {0} to {1}, but was {value}.");
                m_ShadowCascadeBlendCullingFactor = value;
            }
        }

        public Plane GetCullingPlane(int index)
        {
            if (index < 0 || index >= cullingPlaneCount)
                throw new ArgumentException("index", $"Index should be at least {0} and less than cullingPlaneCount ({cullingPlaneCount}), but was {index}.");
            fixed(byte* ptr = m_CullingPlanes)
            {
                var planes = (Plane*)ptr;
                return planes[index];
            }
        }

        public void SetCullingPlane(int index, Plane plane)
        {
            if (index < 0 || index >= cullingPlaneCount)
                throw new ArgumentException("index", $"Index should be at least {0} and less than cullingPlaneCount ({cullingPlaneCount}), but was {index}.");
            fixed(byte* ptr = m_CullingPlanes)
            {
                var planes = (Plane*)ptr;
                planes[index] = plane;
            }
        }

        public bool Equals(ShadowSplitData other)
        {
            if (m_CullingPlaneCount != other.m_CullingPlaneCount)
            {
                return false;
            }

            for (var i = 0; i < cullingPlaneCount; i++)
            {
                if (!GetCullingPlane(i).Equals(other.GetCullingPlane(i)))
                {
                    return false;
                }
            }

            return m_CullingSphere.Equals(other.m_CullingSphere);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ShadowSplitData && Equals((ShadowSplitData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_CullingPlaneCount * 397) ^ m_CullingSphere.GetHashCode();
            }
        }

        public static bool operator==(ShadowSplitData left, ShadowSplitData right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(ShadowSplitData left, ShadowSplitData right)
        {
            return !left.Equals(right);
        }
    }
}
