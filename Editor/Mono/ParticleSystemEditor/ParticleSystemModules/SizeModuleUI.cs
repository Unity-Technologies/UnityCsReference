// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class SizeModuleUI : ModuleUI
    {
        SerializedMinMaxCurve m_X;
        SerializedMinMaxCurve m_Y;
        SerializedMinMaxCurve m_Z;
        SerializedProperty m_SeparateAxes;

        class Texts
        {
            public GUIContent size = EditorGUIUtility.TextContent("Size|Controls the size of each particle during its lifetime.");
            public GUIContent separateAxes = EditorGUIUtility.TextContent("Separate Axes|If enabled, you can control the angular velocity limit separately for each axis.");
            public GUIContent x = EditorGUIUtility.TextContent("X");
            public GUIContent y = EditorGUIUtility.TextContent("Y");
            public GUIContent z = EditorGUIUtility.TextContent("Z");
        }
        static Texts s_Texts;

        public SizeModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "SizeModule", displayName)
        {
            m_ToolTip = "Controls the size of each particle during its lifetime.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_X != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_SeparateAxes = GetProperty("separateAxes");
            m_X = new SerializedMinMaxCurve(this, s_Texts.x, "curve");
            m_Y = new SerializedMinMaxCurve(this, s_Texts.y, "y", false, false, m_SeparateAxes.boolValue);
            m_Z = new SerializedMinMaxCurve(this, s_Texts.z, "z", false, false, m_SeparateAxes.boolValue);
            m_X.m_AllowConstant = false;
            m_Y.m_AllowConstant = false;
            m_Z.m_AllowConstant = false;
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            EditorGUI.BeginChangeCheck();
            bool separateAxes = GUIToggle(s_Texts.separateAxes, m_SeparateAxes);
            if (EditorGUI.EndChangeCheck())
            {
                // Remove old curves from curve editor
                if (!separateAxes)
                {
                    m_Y.RemoveCurveFromEditor();
                    m_Z.RemoveCurveFromEditor();
                }
            }

            // Keep states in sync
            if (!m_X.stateHasMultipleDifferentValues)
            {
                m_Z.SetMinMaxState(m_X.state, separateAxes);
                m_Y.SetMinMaxState(m_X.state, separateAxes);
            }

            if (separateAxes)
            {
                m_X.m_DisplayName = s_Texts.x;
                GUITripleMinMaxCurve(GUIContent.none, s_Texts.x, m_X, s_Texts.y, m_Y, s_Texts.z, m_Z, null);
            }
            else
            {
                m_X.m_DisplayName = s_Texts.size;
                GUIMinMaxCurve(s_Texts.size, m_X);
            }
        }
    }
} // namespace UnityEditor
