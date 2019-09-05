// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    class LifetimeByEmitterSpeedModuleUI : ModuleUI
    {
        // Keep in sync with LifetimeByEmitterSpeedModule.h
        SerializedMinMaxCurve m_Curve;
        SerializedProperty m_Range;

        class Texts
        {
            public GUIContent speed = EditorGUIUtility.TrTextContent("Multiplier", "Controls the initial lifetime of particles based on the speed of the emitter.");
            public GUIContent speedRange = EditorGUIUtility.TrTextContent("Speed Range", "Maps the speed to a value along the curve, when using one of the curve modes.");
        }
        static Texts s_Texts;

        public LifetimeByEmitterSpeedModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "LifetimeByEmitterSpeedModule", displayName)
        {
            m_ToolTip = "Controls the initial lifetime of each particle based on the speed of the emitter when the particle was spawned.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Curve != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Curve = new SerializedMinMaxCurve(this, GUIContent.none, "m_Curve");
            m_Range = GetProperty("m_Range");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUIMinMaxCurve(s_Texts.speed, m_Curve);
            GUIMinMaxRange(s_Texts.speedRange, m_Range);
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            Init();

            string failureReason = string.Empty;
            if (!m_Curve.SupportsProcedural(ref failureReason))
                text += "\nLifetime By Emitter Speed module curve: " + failureReason;
        }
    }
} // namespace UnityEditor
