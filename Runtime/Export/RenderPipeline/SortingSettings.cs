// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    public enum DistanceMetric
    {
        Perspective,
        Orthographic,
        CustomAxis
    }

    // match DrawSortSettings on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct SortingSettings : IEquatable<SortingSettings>
    {
        Matrix4x4 m_WorldToCameraMatrix;
        Vector3 m_CameraPosition;
        Vector3 m_CustomAxis;
        SortingCriteria m_Criteria;
        DistanceMetric m_DistanceMetric;

        public SortingSettings(Camera camera)
        {
            ScriptableRenderContext.InitializeSortSettings(camera, out this);
            m_Criteria = criteria;
        }

        public Matrix4x4 worldToCameraMatrix
        {
            get { return m_WorldToCameraMatrix; }
            set { m_WorldToCameraMatrix = value; }
        }

        public Vector3 cameraPosition
        {
            get { return m_CameraPosition; }
            set { m_CameraPosition = value; }
        }

        public Vector3 customAxis
        {
            get { return m_CustomAxis; }
            set { m_CustomAxis = value; }
        }

        public SortingCriteria criteria
        {
            get { return m_Criteria; }
            set { m_Criteria = value; }
        }

        public DistanceMetric distanceMetric
        {
            get { return m_DistanceMetric; }
            set { m_DistanceMetric = value; }
        }

        public bool Equals(SortingSettings other)
        {
            return m_WorldToCameraMatrix.Equals(other.m_WorldToCameraMatrix) && m_CameraPosition.Equals(other.m_CameraPosition) && m_CustomAxis.Equals(other.m_CustomAxis) && m_Criteria == other.m_Criteria && m_DistanceMetric == other.m_DistanceMetric;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SortingSettings && Equals((SortingSettings)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_WorldToCameraMatrix.GetHashCode();
                hashCode = (hashCode * 397) ^ m_CameraPosition.GetHashCode();
                hashCode = (hashCode * 397) ^ m_CustomAxis.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_Criteria;
                hashCode = (hashCode * 397) ^ (int)m_DistanceMetric;
                return hashCode;
            }
        }

        public static bool operator==(SortingSettings left, SortingSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(SortingSettings left, SortingSettings right)
        {
            return !left.Equals(right);
        }
    }
}
