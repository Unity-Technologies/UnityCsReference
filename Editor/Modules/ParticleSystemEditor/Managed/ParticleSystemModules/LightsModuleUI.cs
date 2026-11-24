// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Unity.Scripting.LifecycleManagement;


namespace UnityEditor
{
    class LightsModuleUI : ModuleUI
    {
        class Texts
        {
            public GUIContent ratio = L10n.TextContent("Ratio", "Amount of particles that have a light source attached to them.", null, null);
            public GUIContent randomDistribution = L10n.TextContent("Random Distribution", "Emit lights randomly, or at regular intervals.", null, null);
            public GUIContent light = L10n.TextContent("Light", "Light prefab to be used for spawning particle lights.", null, null);
            public GUIContent color = L10n.TextContent("Use Particle Color", "Check the option to multiply the particle color by the light color. Otherwise, only the color of the light is used.", null, null);
            public GUIContent range = L10n.TextContent("Size Affects Range", "Multiply the range of the light with the size of the particle.", null, null);
            public GUIContent intensity = L10n.TextContent("Alpha Affects Intensity", "Multiply the intensity of the light with the alpha of the particle.", null, null);
            public GUIContent rangeCurve = L10n.TextContent("Range Multiplier", "Apply a custom multiplier to the range of the lights. If you use a curve to set this value, the Particle System applies the curve over the lifetime of each particle.", null, null);
            public GUIContent intensityCurve = L10n.TextContent("Intensity Multiplier", "Apply a custom multiplier to the intensity of the lights. If you use a curve to set this value, the Particle System applies the curve over the lifetime of each particle.", null, null);
            public GUIContent maxLights = L10n.TextContent("Maximum Lights", "Limit the amount of lights the system can create. This module makes it very easy to create lots of lights, which can hurt performance.", null, null);
        }
        [NoAutoStaticsCleanup] // lazy-initialized UI text cache
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
                if (light.type != LightType.Point && light.type != LightType.Spot && light.type != LightType.Rectangle)
                {
                    GUIContent warning = L10n.TextContent("Only point, spot, and rectangle area lights are supported on particles.", null, null, null);
                    EditorGUILayout.HelpBox(warning.text, MessageType.Warning, true);
                }
            }
        }
    }
} // namespace UnityEditor
