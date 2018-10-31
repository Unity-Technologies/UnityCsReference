// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LODParameters : IEquatable<LODParameters>
    {
        // has to be int for marshaling
        private int m_IsOrthographic;
        private Vector3 m_CameraPosition;
        private float m_FieldOfView;
        private float m_OrthoSize;
        private int m_CameraPixelHeight;

        public bool isOrthographic
        {
            get { return Convert.ToBoolean(m_IsOrthographic); }
            set { m_IsOrthographic = Convert.ToInt32(value); }
        }

        public Vector3 cameraPosition
        {
            get { return m_CameraPosition; }
            set { m_CameraPosition = value; }
        }

        public float fieldOfView
        {
            get { return m_FieldOfView; }
            set { m_FieldOfView = value; }
        }

        public float orthoSize
        {
            get { return m_OrthoSize; }
            set { m_OrthoSize = value; }
        }

        public int cameraPixelHeight
        {
            get { return m_CameraPixelHeight; }
            set { m_CameraPixelHeight = value; }
        }

        public bool Equals(LODParameters other)
        {
            return m_IsOrthographic == other.m_IsOrthographic && m_CameraPosition.Equals(other.m_CameraPosition) && m_FieldOfView.Equals(other.m_FieldOfView) && m_OrthoSize.Equals(other.m_OrthoSize) && m_CameraPixelHeight == other.m_CameraPixelHeight;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LODParameters && Equals((LODParameters)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_IsOrthographic;
                hashCode = (hashCode * 397) ^ m_CameraPosition.GetHashCode();
                hashCode = (hashCode * 397) ^ m_FieldOfView.GetHashCode();
                hashCode = (hashCode * 397) ^ m_OrthoSize.GetHashCode();
                hashCode = (hashCode * 397) ^ m_CameraPixelHeight;
                return hashCode;
            }
        }

        public static bool operator==(LODParameters left, LODParameters right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(LODParameters left, LODParameters right)
        {
            return !left.Equals(right);
        }
    }
}
