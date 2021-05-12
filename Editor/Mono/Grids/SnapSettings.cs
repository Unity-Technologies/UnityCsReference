// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [Serializable]
    class SnapSettings
    {
        const int k_DefaultSnapMultiplier = 2048;
        const float k_DefaultSnapValue = .25f;
        const float k_MinSnapValue = 0f;

        public static readonly Vector3 defaultMove = new Vector3(k_DefaultSnapValue, k_DefaultSnapValue, k_DefaultSnapValue);
        public const float defaultRotation = 15f;
        public const float defaultScale = 1f;

        [SerializeField]
        Vector3 m_SnapValue = defaultMove;

        [SerializeField]
        Vector3Int m_SnapMultiplier = new Vector3Int(k_DefaultSnapMultiplier, k_DefaultSnapMultiplier, k_DefaultSnapMultiplier);

        [SerializeField]
        float m_Rotation = defaultRotation;

        [SerializeField]
        float m_Scale = defaultScale;

        internal Vector3 snapValue
        {
            get { return SnapValueInUnityUnits(); }
            set
            {
                m_SnapValue.x = Mathf.Max(value.x, k_MinSnapValue);
                m_SnapValue.y = Mathf.Max(value.y, k_MinSnapValue);
                m_SnapValue.z = Mathf.Max(value.z, k_MinSnapValue);
                snapMultiplier = new Vector3Int(k_DefaultSnapMultiplier, k_DefaultSnapMultiplier, k_DefaultSnapMultiplier);
            }
        }

        internal Vector3Int snapMultiplier
        {
            get { return m_SnapMultiplier; }
            set { m_SnapMultiplier = value; }
        }

        internal void ResetMultiplier()
        {
            m_SnapMultiplier = new Vector3Int(k_DefaultSnapMultiplier, k_DefaultSnapMultiplier, k_DefaultSnapMultiplier);
        }

        public float rotation
        {
            get { return m_Rotation; }
            set { m_Rotation = value; }
        }

        public float scale
        {
            get { return m_Scale; }
            set { m_Scale = value; }
        }

        Vector3 SnapMultiplierFrac()
        {
            var val = 1.0f / (float)k_DefaultSnapMultiplier;
            return new Vector3(m_SnapMultiplier.x * val, m_SnapMultiplier.y * val, m_SnapMultiplier.z * val);
        }

        Vector3 SnapValueInUnityUnits()
        {
            Vector3 frac = SnapMultiplierFrac();
            return new Vector3(m_SnapValue.x * frac.x, m_SnapValue.y * frac.y, m_SnapValue.z * frac.z);
        }

        internal void IncrementSnapMultiplier()
        {
            if (m_SnapMultiplier.x < int.MaxValue / 2)
                m_SnapMultiplier.x *= 2;
            if (m_SnapMultiplier.y < int.MaxValue / 2)
                m_SnapMultiplier.y *= 2;
            if (m_SnapMultiplier.z < int.MaxValue / 2)
                m_SnapMultiplier.z *= 2;
        }

        internal void DecrementSnapMultiplier()
        {
            if (m_SnapMultiplier.x > 1)
                m_SnapMultiplier.x /= 2;
            if (m_SnapMultiplier.y > 1)
                m_SnapMultiplier.y /= 2;
            if (m_SnapMultiplier.z > 1)
                m_SnapMultiplier.z /= 2;
        }
    }
}
