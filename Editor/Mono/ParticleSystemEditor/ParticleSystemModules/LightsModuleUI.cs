// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class LightsModuleUI : ModuleUI
    {
        class Texts
        {
            public GUIContent ratio = new GUIContent("Ratio", "Amount of particles that have a light source attached to them.");
            public GUIContent randomDistribution = new GUIContent("Random Distribution", "Emit lights randomly, or at regular intervals.");
            public GUIContent light = new GUIContent("Light", "Light prefab to be used for spawning particle lights.");
            public GUIContent color = new GUIContent("Use Particle Color", "Check the option to multiply the particle color by the light color. Otherwise, only the color of the light is used.");
            public GUIContent range = new GUIContent("Size Affects Range", "Multiply the range of the light with the size of the particle.");
            public GUIContent intensity = new GUIContent("Alpha Affects Intensity", "Multiply the intensity of the light with the alpha of the particle.");
            public GUIContent rangeCurve = new GUIContent("Range Multiplier", "Apply a custom multiplier to the range of the lights.");
            public GUIContent intensityCurve = new GUIContent("Intensity Multiplier", "Apply a custom multiplier to the intensity of the lights.");
            public GUIContent maxLights = new GUIContent("Maximum Lights", "Limit the amount of lights the system can create. This module makes it very easy to create lots of lights, which can hurt performance.");
        }
        static Texts s_Texts;

        SerializedProperty m_Ratio;
        SerializedProperty m_RandomDistribution;
        SerializedProperty m_Light;
        SerializedProperty m_UseParticleColor;
        SerializedProperty m_SizeAffectsRange;
        SerializedProperty m_AlphaAffectsIntensity;
        SerializedMinMaxCurve m_Range;
        SerializedMinMaxCurve m_Intensity;
        SerializedProperty m_MaxLights;

        public LightsModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "LightsModule", displayName)
        {
            m_ToolTip = "Controls light sources attached to particles.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Ratio != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Ratio = GetProperty("ratio");
            m_RandomDistribution = GetProperty("randomDistribution");
            m_Light = GetProperty("light");
            m_UseParticleColor = GetProperty("color");
            m_SizeAffectsRange = GetProperty("range");
            m_AlphaAffectsIntensity = GetProperty("intensity");
            m_MaxLights = GetProperty("maxLights");

            m_Range = new SerializedMinMaxCurve(this, s_Texts.rangeCurve, "rangeCurve");
            m_Intensity = new SerializedMinMaxCurve(this, s_Texts.intensityCurve, "intensityCurve");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUIObject(s_Texts.light, m_Light);
            GUIFloat(s_Texts.ratio, m_Ratio);
            GUIToggle(s_Texts.randomDistribution, m_RandomDistribution);
            GUIToggle(s_Texts.color, m_UseParticleColor);
            GUIToggle(s_Texts.range, m_SizeAffectsRange);
            GUIToggle(s_Texts.intensity, m_AlphaAffectsIntensity);
            GUIMinMaxCurve(s_Texts.rangeCurve, m_Range);
            GUIMinMaxCurve(s_Texts.intensityCurve, m_Intensity);
            GUIInt(s_Texts.maxLights, m_MaxLights);

            if (m_Light.objectReferenceValue)
            {
                Light light = (Light)m_Light.objectReferenceValue;
                if (light.type != LightType.Point && light.type != LightType.Spot)
                {
                    GUIContent warning = EditorGUIUtility.TextContent("Only point and spot lights are supported on particles.");
                    EditorGUILayout.HelpBox(warning.text, MessageType.Warning, true);
                }
            }
        }
    }
} // namespace UnityEditor
