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
        const float k_DefaultSnapValue = 1f;
        const float k_DefaultRotation = 15f;
        const float k_DefaultScale = 1f;

        // If handle movement is aligned with grid coordinates, snap to grid instead of incremental from handle origin
        [SerializeField]
        bool m_PreferGrid;

        [SerializeField]
        Vector3 m_SnapValue = new Vector3(k_DefaultSnapValue, k_DefaultSnapValue, k_DefaultSnapValue);

        [SerializeField]
        Vector3Int m_SnapMultiplier = new Vector3Int(k_DefaultSnapMultiplier, k_DefaultSnapMultiplier, k_DefaultSnapMultiplier);

        [SerializeField]
        float m_Rotation = k_DefaultRotation;

        [SerializeField]
        float m_Scale = k_DefaultScale;

        internal Vector3 snapValue
        {
            get { return SnapValueInUnityUnits(); }
            set { m_SnapValue = value; snapMultiplier = new Vector3Int(k_DefaultSnapMultiplier, k_DefaultSnapMultiplier, k_DefaultSnapMultiplier); }
        }

        // When moving a handle along a cardinal direction, handles will snap to the nearest grid point instead of
        // increments from the handle origin.
        internal bool preferGrid
        {
            get { return m_PreferGrid; }
            set { m_PreferGrid = value; }
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

        Vector3Int SnapMultiplierFrac()
        {
            var val = 1.0f / (float)k_DefaultSnapMultiplier;
            return new Vector3Int((int)(m_SnapMultiplier.x * val), (int)(m_SnapMultiplier.y * val), (int)(m_SnapMultiplier.z * val));
        }

        Vector3 SnapValueInUnityUnits()
        {
            var frac = SnapMultiplierFrac();
            return new Vector3(m_SnapValue.x * frac.x, m_SnapValue.y * frac.y, m_SnapValue.z * frac.z);
        }
    }
}
