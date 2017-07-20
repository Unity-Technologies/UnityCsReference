// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class ExternalForcesModuleUI : ModuleUI
    {
        SerializedProperty m_Multiplier;

        class Texts
        {
            public GUIContent multiplier = EditorGUIUtility.TextContent("Multiplier|Used to scale the force applied to this particle system.");
        }
        static Texts s_Texts;

        public ExternalForcesModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "ExternalForcesModule", displayName)
        {
            m_ToolTip = "Controls the wind zones that each particle is affected by.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Multiplier != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Multiplier = GetProperty("multiplier");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUIFloat(s_Texts.multiplier, m_Multiplier);
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            text += "\nExternal Forces module is enabled.";
        }
    } // namespace UnityEditor
}
