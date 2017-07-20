// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    class InheritVelocityModuleUI : ModuleUI
    {
        // Keep in sync with InheritVelocityModule.h
        enum Modes { Initial = 0, Current = 1 };

        SerializedProperty m_Mode;
        SerializedMinMaxCurve m_Curve;

        class Texts
        {
            public GUIContent mode = EditorGUIUtility.TextContent("Mode|Specifies whether the emitter velocity is inherited as a one-shot when a particle is born, always using the current emitter velocity, or using the emitter velocity when the particle was born.");
            public GUIContent velocity = EditorGUIUtility.TextContent("Multiplier|Controls the amount of emitter velocity inherited during each particle's lifetime.");

            public GUIContent[] modes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Initial"),
                EditorGUIUtility.TextContent("Current")
            };
        }
        static Texts s_Texts;

        public InheritVelocityModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "InheritVelocityModule", displayName)
        {
            m_ToolTip = "Controls the velocity inherited from the emitter, for each particle.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Curve != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Mode = GetProperty("m_Mode");
            m_Curve = new SerializedMinMaxCurve(this, GUIContent.none, "m_Curve", kUseSignedRange);
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUIPopup(s_Texts.mode, m_Mode, s_Texts.modes);
            GUIMinMaxCurve(s_Texts.velocity, m_Curve);
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            Init();

            string failureReason = string.Empty;
            if (!m_Curve.SupportsProcedural(ref failureReason))
                text += "\nInherit Velocity module curve: " + failureReason;
        }
    }
} // namespace UnityEditor
