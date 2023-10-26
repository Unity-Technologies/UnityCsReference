// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.Rendering
{
    [UsedByNativeCode]
    public struct VisibleLight : IEquatable<VisibleLight>
    {
        LightType            m_LightType;
        Color                m_FinalColor;
        Rect                 m_ScreenRect;
        Matrix4x4            m_LocalToWorldMatrix;
        float                m_Range;
        float                m_SpotAngle;
#pragma warning disable 649
        int                 m_InstanceId;
#pragma warning restore 649
        VisibleLightFlags    m_Flags;

        public Light light => (Light)Object.FindObjectFromInstanceID(m_InstanceId);

        public LightType lightType
        {
            get { return m_LightType; }
            set { m_LightType = value; }
        }

        public Color finalColor
        {
            get { return m_FinalColor; }
            set { m_FinalColor = value; }
        }

        public Rect screenRect
        {
            get { return m_ScreenRect; }
            set { m_ScreenRect = value; }
        }

        public Matrix4x4 localToWorldMatrix
        {
            get { return m_LocalToWorldMatrix; }
            set { m_LocalToWorldMatrix = value; }
        }

        public float range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        public float spotAngle
        {
            get { return m_SpotAngle; }
            set { m_SpotAngle = value; }
        }

        public bool intersectsNearPlane
        {
            get { return (m_Flags & VisibleLightFlags.IntersectsNearPlane) > 0; }
            set
            {
                if (value)
                    m_Flags = m_Flags | VisibleLightFlags.IntersectsNearPlane;
                else
                    m_Flags = m_Flags & ~VisibleLightFlags.IntersectsNearPlane;
            }
        }

        public bool intersectsFarPlane
        {
            get { return (m_Flags & VisibleLightFlags.IntersectsFarPlane) > 0; }
            set
            {
                if (value)
                    m_Flags = m_Flags | VisibleLightFlags.IntersectsFarPlane;
                else
                    m_Flags = m_Flags & ~VisibleLightFlags.IntersectsFarPlane;
            }
        }

        public bool forcedVisible
        {
            get { return (m_Flags & VisibleLightFlags.ForcedVisible) > 0; }
        }

        public bool Equals(VisibleLight other)
        {
            return m_LightType == other.m_LightType && m_FinalColor.Equals(other.m_FinalColor) && m_ScreenRect.Equals(other.m_ScreenRect) && m_LocalToWorldMatrix.Equals(other.m_LocalToWorldMatrix) && m_Range.Equals(other.m_Range) && m_SpotAngle.Equals(other.m_SpotAngle) && m_InstanceId == other.m_InstanceId && m_Flags == other.m_Flags;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VisibleLight && Equals((VisibleLight)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)m_LightType;
                hashCode = (hashCode * 397) ^ m_FinalColor.GetHashCode();
                hashCode = (hashCode * 397) ^ m_ScreenRect.GetHashCode();
                hashCode = (hashCode * 397) ^ m_LocalToWorldMatrix.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Range.GetHashCode();
                hashCode = (hashCode * 397) ^ m_SpotAngle.GetHashCode();
                hashCode = (hashCode * 397) ^ m_InstanceId;
                hashCode = (hashCode * 397) ^ (int)m_Flags;
                return hashCode;
            }
        }

        public static bool operator==(VisibleLight left, VisibleLight right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(VisibleLight left, VisibleLight right)
        {
            return !left.Equals(right);
        }
    }
}
