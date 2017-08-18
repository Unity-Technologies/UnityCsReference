// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class SizeByVelocityModuleUI : ModuleUI
    {
        class Texts
        {
            public GUIContent velocityRange = EditorGUIUtility.TextContent("Speed Range|Remaps speed in the defined range to a size.");
            public GUIContent size = EditorGUIUtility.TextContent("Size|Controls the size of each particle based on its speed.");
            public GUIContent separateAxes = new GUIContent("Separate Axes", "If enabled, you can control the angular velocity limit separately for each axis.");
            public GUIContent x = new GUIContent("X");
            public GUIContent y = new GUIContent("Y");
            public GUIContent z = new GUIContent("Z");
        }
        static Texts s_Texts;

        SerializedMinMaxCurve m_X;
        SerializedMinMaxCurve m_Y;
        SerializedMinMaxCurve m_Z;
        SerializedProperty m_SeparateAxes;
        SerializedProperty m_Range;

        public SizeByVelocityModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "SizeBySpeedModule", displayName)
        {
            m_ToolTip = "Controls the size of each particle based on its speed.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_X != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_SeparateAxes = GetProperty("separateAxes");
            m_Range = GetProperty("range");

            m_X = new SerializedMinMaxCurve(this, s_Texts.x, "curve"); // use "curve" instead of "x" for backwards compatibility reasons: the old system used to only support one axis, and this was its name
            m_X.m_AllowConstant = false;
            m_Y = new SerializedMinMaxCurve(this, s_Texts.y, "y", false, false, m_SeparateAxes.boolValue);
            m_Y.m_AllowConstant = false;
            m_Z = new SerializedMinMaxCurve(this, s_Texts.z, "z", false, false, m_SeparateAxes.boolValue);
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

            MinMaxCurveState state = m_Z.state;

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

            using (new EditorGUI.DisabledScope((state == MinMaxCurveState.k_Scalar) || (state == MinMaxCurveState.k_TwoScalars)))
            {
                GUIMinMaxRange(s_Texts.velocityRange, m_Range);
            }
        }
    }
} // namespace UnityEditor
