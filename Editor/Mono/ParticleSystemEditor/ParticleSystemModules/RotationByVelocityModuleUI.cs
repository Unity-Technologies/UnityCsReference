// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class RotationByVelocityModuleUI : ModuleUI
    {
        class Texts
        {
            public GUIContent velocityRange = EditorGUIUtility.TextContent("Speed Range|Maps the speed to a value along the curve, when using one of the curve modes.");
            public GUIContent rotation = EditorGUIUtility.TextContent("Angular Velocity|Controls the angular velocity of each particle based on its speed.");
            public GUIContent separateAxes = EditorGUIUtility.TextContent("Separate Axes|If enabled, you can control the angular velocity limit separately for each axis.");
            public GUIContent x = EditorGUIUtility.TextContent("X");
            public GUIContent y = EditorGUIUtility.TextContent("Y");
            public GUIContent z = EditorGUIUtility.TextContent("Z");
        }
        static Texts s_Texts;
        SerializedMinMaxCurve m_X;
        SerializedMinMaxCurve m_Y;
        SerializedMinMaxCurve m_Z;
        SerializedProperty m_SeparateAxes;
        SerializedProperty m_Range;

        public RotationByVelocityModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "RotationBySpeedModule", displayName)
        {
            m_ToolTip = "Controls the angular velocity of each particle based on its speed.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Z != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_SeparateAxes = GetProperty("separateAxes");
            m_Range = GetProperty("range");
            m_X = new SerializedMinMaxCurve(this, s_Texts.x, "x", kUseSignedRange, false, m_SeparateAxes.boolValue);
            m_Y = new SerializedMinMaxCurve(this, s_Texts.y, "y", kUseSignedRange, false, m_SeparateAxes.boolValue);
            m_Z = new SerializedMinMaxCurve(this, s_Texts.z, "curve", kUseSignedRange);
            m_X.m_RemapValue = Mathf.Rad2Deg;
            m_Y.m_RemapValue = Mathf.Rad2Deg;
            m_Z.m_RemapValue = Mathf.Rad2Deg;
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
                    m_X.RemoveCurveFromEditor();
                    m_Y.RemoveCurveFromEditor();
                }
            }

            // Keep states in sync
            if (!m_Z.stateHasMultipleDifferentValues)
            {
                m_X.SetMinMaxState(m_Z.state, separateAxes);
                m_Y.SetMinMaxState(m_Z.state, separateAxes);
            }

            MinMaxCurveState state = m_Z.state;

            if (separateAxes)
            {
                m_Z.m_DisplayName = s_Texts.z;
                GUITripleMinMaxCurve(GUIContent.none, s_Texts.x, m_X, s_Texts.y, m_Y, s_Texts.z, m_Z, null);
            }
            else
            {
                m_Z.m_DisplayName = s_Texts.rotation;
                GUIMinMaxCurve(s_Texts.rotation, m_Z);
            }

            using (new EditorGUI.DisabledScope((state == MinMaxCurveState.k_Scalar) || (state == MinMaxCurveState.k_TwoScalars)))
            {
                GUIMinMaxRange(s_Texts.velocityRange, m_Range);
            }
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            text += "\nRotation by Speed module is enabled.";
        }
    }
} // namespace UnityEditor
