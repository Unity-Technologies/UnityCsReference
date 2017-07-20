// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class ForceModuleUI : ModuleUI
    {
        SerializedMinMaxCurve m_X;
        SerializedMinMaxCurve m_Y;
        SerializedMinMaxCurve m_Z;
        SerializedProperty m_RandomizePerFrame;
        SerializedProperty m_InWorldSpace;


        class Texts
        {
            public GUIContent x = EditorGUIUtility.TextContent("X");
            public GUIContent y = EditorGUIUtility.TextContent("Y");
            public GUIContent z = EditorGUIUtility.TextContent("Z");
            public GUIContent randomizePerFrame = EditorGUIUtility.TextContent("Randomize|Randomize force every frame. Only available when using random between two constants or random between two curves.");
            public GUIContent space = EditorGUIUtility.TextContent("Space|Specifies if the force values are in local space (rotated with the transform) or world space.");
            public string[] spaces = {"Local", "World"};
        }
        static Texts s_Texts;

        public ForceModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "ForceModule", displayName)
        {
            m_ToolTip = "Controls the force of each particle during its lifetime.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_X != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_X = new SerializedMinMaxCurve(this, s_Texts.x, "x", kUseSignedRange);
            m_Y = new SerializedMinMaxCurve(this, s_Texts.y, "y", kUseSignedRange);
            m_Z = new SerializedMinMaxCurve(this, s_Texts.z, "z", kUseSignedRange);
            m_RandomizePerFrame = GetProperty("randomizePerFrame");
            m_InWorldSpace = GetProperty("inWorldSpace");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            MinMaxCurveState state = m_X.state;
            GUITripleMinMaxCurve(GUIContent.none, s_Texts.x, m_X, s_Texts.y, m_Y, s_Texts.z, m_Z, m_RandomizePerFrame);

            GUIBoolAsPopup(s_Texts.space, m_InWorldSpace, s_Texts.spaces);

            using (new EditorGUI.DisabledScope((state != MinMaxCurveState.k_TwoScalars) && (state != MinMaxCurveState.k_TwoCurves)))
            {
                GUIToggle(s_Texts.randomizePerFrame, m_RandomizePerFrame);
            }
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            Init();

            string failureReason = string.Empty;
            if (!m_X.SupportsProcedural(ref failureReason))
                text += "\nForce over Lifetime module curve X: " + failureReason;

            failureReason = string.Empty;
            if (!m_Y.SupportsProcedural(ref failureReason))
                text += "\nForce over Lifetime module curve Y: " + failureReason;

            failureReason = string.Empty;
            if (!m_Z.SupportsProcedural(ref failureReason))
                text += "\nForce over Lifetime module curve Z: " + failureReason;

            if (m_RandomizePerFrame.boolValue)
                text += "\nRandomize is enabled in the Force over Lifetime module.";
        }
    } // namespace UnityEditor
}
