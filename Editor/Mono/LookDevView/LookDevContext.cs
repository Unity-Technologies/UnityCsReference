// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    [Serializable]
    internal class LookDevContext
    {
        [Serializable]
        public class LookDevPropertyValue
        {
            public float floatValue = 0.0f;
            public int intValue = 0;
        }

        [SerializeField]
        private LookDevPropertyValue[] m_Properties = new LookDevPropertyValue[(int)LookDevProperty.Count];

        public float exposureValue
        {
            get { return m_Properties[(int)LookDevProperty.ExposureValue].floatValue; }
        }

        public float envRotation
        {
            get { return m_Properties[(int)LookDevProperty.EnvRotation].floatValue; }
            set { m_Properties[(int)LookDevProperty.EnvRotation].floatValue = value; }
        }

        public int currentHDRIIndex
        {
            get { return m_Properties[(int)LookDevProperty.HDRI].intValue; }
            set { m_Properties[(int)LookDevProperty.HDRI].intValue = value; }
        }

        public int shadingMode
        {
            get { return m_Properties[(int)LookDevProperty.ShadingMode].intValue; }
        }

        public int lodIndex
        {
            get { return m_Properties[(int)LookDevProperty.LoDIndex].intValue; }
        }

        public LookDevContext()
        {
            for (int i = 0; i < (int)LookDevProperty.Count; ++i)
            {
                m_Properties[i] = new LookDevPropertyValue();
            }

            m_Properties[(int)LookDevProperty.ExposureValue].floatValue = 0.0f;
            m_Properties[(int)LookDevProperty.HDRI].intValue = 0;
            m_Properties[(int)LookDevProperty.ShadingMode].intValue = (int)DrawCameraMode.Normal;
            m_Properties[(int)LookDevProperty.LoDIndex].intValue = -1;
            m_Properties[(int)LookDevProperty.EnvRotation].floatValue = 0.0f;
        }

        public LookDevPropertyValue GetProperty(LookDevProperty property)
        {
            return m_Properties[(int)property];
        }

        public void UpdateProperty(LookDevProperty property, float value)
        {
            m_Properties[(int)property].floatValue = value;
        }

        public void UpdateProperty(LookDevProperty property, int value)
        {
            m_Properties[(int)property].intValue = value;
        }
    }
}
