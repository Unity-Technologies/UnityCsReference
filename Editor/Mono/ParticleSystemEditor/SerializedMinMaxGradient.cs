// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    // Must be in sync with enum in ParticleSystemCurves.h
    internal enum MinMaxGradientState
    {
        k_Color = 0,
        k_Gradient = 1,
        k_RandomBetweenTwoColors = 2,
        k_RandomBetweenTwoGradients = 3,
        k_RandomColor = 4
    };

    internal class SerializedMinMaxGradient
    {
        public SerializedProperty m_MaxGradient;
        public SerializedProperty m_MinGradient;
        public SerializedProperty m_MaxColor;
        public SerializedProperty m_MinColor;
        private SerializedProperty m_MinMaxState;

        public bool m_AllowColor;
        public bool m_AllowGradient;
        public bool m_AllowRandomBetweenTwoColors;
        public bool m_AllowRandomBetweenTwoGradients;
        public bool m_AllowRandomColor;

        public MinMaxGradientState state
        {
            get { return (MinMaxGradientState)m_MinMaxState.intValue; }
            set { SetMinMaxState(value); }
        }

        public bool stateHasMultipleDifferentValues
        {
            get { return m_MinMaxState.hasMultipleDifferentValues; }
        }

        public SerializedMinMaxGradient(SerializedModule m)
        {
            Init(m, "gradient");
        }

        public SerializedMinMaxGradient(SerializedModule m, string name)
        {
            Init(m, name);
        }

        void Init(SerializedModule m, string name)
        {
            m_MaxGradient = m.GetProperty(name, "maxGradient");
            m_MinGradient = m.GetProperty(name, "minGradient");
            m_MaxColor = m.GetProperty(name, "maxColor");
            m_MinColor = m.GetProperty(name, "minColor");
            m_MinMaxState = m.GetProperty(name, "minMaxState");

            m_AllowColor = true;
            m_AllowGradient = true;
            m_AllowRandomBetweenTwoColors = true;
            m_AllowRandomBetweenTwoGradients = true;
            m_AllowRandomColor = false;
        }

        private void SetMinMaxState(MinMaxGradientState newState)
        {
            if (newState == state)
                return;

            m_MinMaxState.intValue = (int)newState;
        }

        public static Color GetGradientAsColor(SerializedProperty gradientProp)
        {
            Gradient gradient = gradientProp.gradientValue;
            return gradient.constantColor;
        }

        public static void SetGradientAsColor(SerializedProperty gradientProp, Color color)
        {
            Gradient gradient = gradientProp.gradientValue;
            gradient.constantColor = color;

            // We have changed a gradient so clear preview cache
            UnityEditorInternal.GradientPreviewCache.ClearCache();
        }
    }
} // namespace UnityEditor
