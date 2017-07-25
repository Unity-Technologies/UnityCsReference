// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class VelocityModuleUI : ModuleUI
    {
        SerializedMinMaxCurve m_X;
        SerializedMinMaxCurve m_Y;
        SerializedMinMaxCurve m_Z;
        SerializedProperty m_InWorldSpace;
        SerializedMinMaxCurve m_SpeedModifier;

        class Texts
        {
            public GUIContent x = EditorGUIUtility.TextContent("X");
            public GUIContent y = EditorGUIUtility.TextContent("Y");
            public GUIContent z = EditorGUIUtility.TextContent("Z");
            public GUIContent space = EditorGUIUtility.TextContent("Space|Specifies if the velocity values are in local space (rotated with the transform) or world space.");
            public GUIContent speedMultiplier = EditorGUIUtility.TextContent("Speed Modifier|Multiply the particle speed by this value");
            public string[] spaces = { "Local", "World" };
        }
        static Texts s_Texts;

        public VelocityModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "VelocityModule", displayName)
        {
            m_ToolTip = "Controls the velocity of each particle during its lifetime.";
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
            m_InWorldSpace = GetProperty("inWorldSpace");

            m_SpeedModifier = new SerializedMinMaxCurve(this, s_Texts.speedMultiplier, "speedModifier", kUseSignedRange);
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUITripleMinMaxCurve(GUIContent.none, s_Texts.x, m_X, s_Texts.y, m_Y, s_Texts.z, m_Z, null);
            GUIBoolAsPopup(s_Texts.space, m_InWorldSpace, s_Texts.spaces);

            GUIMinMaxCurve(s_Texts.speedMultiplier, m_SpeedModifier);
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            Init();

            string failureReason = string.Empty;
            if (!m_X.SupportsProcedural(ref failureReason))
                text += "\nVelocity over Lifetime module curve X: " + failureReason;

            failureReason = string.Empty;
            if (!m_Y.SupportsProcedural(ref failureReason))
                text += "\nVelocity over Lifetime module curve Y: " + failureReason;

            failureReason = string.Empty;
            if (!m_Z.SupportsProcedural(ref failureReason))
                text += "\nVelocity over Lifetime module curve Z: " + failureReason;

            failureReason = string.Empty;
            if (m_SpeedModifier.state != MinMaxCurveState.k_Scalar || m_SpeedModifier.maxConstant != 1.0f)
                text += "\nVelocity over Lifetime module curve Speed Multiplier is being used";
        }
    }
} // namespace UnityEditor
