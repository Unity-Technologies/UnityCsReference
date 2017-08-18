// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class RotationModuleUI : ModuleUI
    {
        SerializedMinMaxCurve m_X;
        SerializedMinMaxCurve m_Y;
        SerializedMinMaxCurve m_Z;
        SerializedProperty m_SeparateAxes;

        class Texts
        {
            public GUIContent rotation = EditorGUIUtility.TextContent("Angular Velocity|Controls the angular velocity of each particle during its lifetime.");
            public GUIContent separateAxes = EditorGUIUtility.TextContent("Separate Axes|If enabled, you can control the angular velocity limit separately for each axis.");
            public GUIContent x = EditorGUIUtility.TextContent("X");
            public GUIContent y = EditorGUIUtility.TextContent("Y");
            public GUIContent z = EditorGUIUtility.TextContent("Z");
        }
        static Texts s_Texts;


        public RotationModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "RotationModule", displayName)
        {
            m_ToolTip = "Controls the angular velocity of each particle during its lifetime.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Z != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_SeparateAxes = GetProperty("separateAxes");
            m_X = new SerializedMinMaxCurve(this, s_Texts.x, "x", kUseSignedRange, false, m_SeparateAxes.boolValue);
            m_Y = new SerializedMinMaxCurve(this, s_Texts.y, "y", kUseSignedRange, false, m_SeparateAxes.boolValue);
            m_Z = new SerializedMinMaxCurve(this, s_Texts.z, "curve", kUseSignedRange);
            m_X.m_RemapValue = Mathf.Rad2Deg;
            m_Y.m_RemapValue = Mathf.Rad2Deg;
            m_Z.m_RemapValue = Mathf.Rad2Deg;
        }

        public override void OnInspectorGUI(InitialModuleUI initial)
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
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            Init();

            string failureReason = string.Empty;
            if (!m_X.SupportsProcedural(ref failureReason))
                text += "\nRotation over Lifetime module curve X: " + failureReason;

            failureReason = string.Empty;
            if (!m_Y.SupportsProcedural(ref failureReason))
                text += "\nRotation over Lifetime module curve Y: " + failureReason;

            failureReason = string.Empty;
            if (!m_Z.SupportsProcedural(ref failureReason))
                text += "\nRotation over Lifetime module curve Z: " + failureReason;
        }
    }
} // namespace UnityEditor
