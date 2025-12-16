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
        public const bool defaultGridSnapEnabled = true;
        public const float defaultRotation = 15f;
        public const float defaultScale = 1f;
        public const float defaultIncrementalSnapSize = 0.25f;

        [SerializeField]
        bool m_SnapToGrid = defaultGridSnapEnabled;

        [SerializeField]
        float m_Rotation = defaultRotation;

        [SerializeField]
        float m_Scale = defaultScale;

        [SerializeField] 
        Vector3 m_IncrementalSnapSize = new (defaultIncrementalSnapSize, defaultIncrementalSnapSize, defaultIncrementalSnapSize);

        public bool snapToGrid
        {
            get => m_SnapToGrid;
            set => m_SnapToGrid = value;
        }
        
        public Vector3 incrementalSnapSize
        {
            get { return m_IncrementalSnapSize; }
            set { m_IncrementalSnapSize = value; }
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
    }
}
