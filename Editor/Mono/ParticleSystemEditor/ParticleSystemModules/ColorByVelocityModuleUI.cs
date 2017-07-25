// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class ColorByVelocityModuleUI : ModuleUI
    {
        class Texts
        {
            public GUIContent color = EditorGUIUtility.TextContent("Color|Controls the color of each particle based on its speed.");
            public GUIContent velocityRange = EditorGUIUtility.TextContent("Speed Range|Remaps speed in the defined range to a color.");
        }
        static Texts s_Texts;
        SerializedMinMaxGradient m_Gradient;
        SerializedProperty m_Range;

        public ColorByVelocityModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "ColorBySpeedModule", displayName)
        {
            m_ToolTip = "Controls the color of each particle based on its speed.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Gradient != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Gradient = new SerializedMinMaxGradient(this);
            m_Gradient.m_AllowColor = false;
            m_Gradient.m_AllowRandomBetweenTwoColors = false;

            m_Range = GetProperty("range");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUIMinMaxGradient(s_Texts.color, m_Gradient, false);
            GUIMinMaxRange(s_Texts.velocityRange, m_Range);
        }
    }
} // namespace UnityEditor
