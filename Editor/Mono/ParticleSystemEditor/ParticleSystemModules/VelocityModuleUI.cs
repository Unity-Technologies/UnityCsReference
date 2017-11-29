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
        SerializedMinMaxCurve m_OrbitalX;
        SerializedMinMaxCurve m_OrbitalY;
        SerializedMinMaxCurve m_OrbitalZ;
        SerializedMinMaxCurve m_OrbitalOffsetX;
        SerializedMinMaxCurve m_OrbitalOffsetY;
        SerializedMinMaxCurve m_OrbitalOffsetZ;
        SerializedMinMaxCurve m_Radial;
        SerializedProperty m_InWorldSpace;
        SerializedMinMaxCurve m_SpeedModifier;

        class Texts
        {
            public GUIContent linearX = EditorGUIUtility.TextContent("Linear  X|Apply linear velocity to particles.");
            public GUIContent orbitalX = EditorGUIUtility.TextContent("Orbital X|Apply orbital velocity to particles, which will rotate them around the center of the system.");
            public GUIContent orbitalOffsetX = EditorGUIUtility.TextContent("Offset  X|Apply an offset to the center of rotation.");
            public GUIContent y = EditorGUIUtility.TextContent("Y");
            public GUIContent z = EditorGUIUtility.TextContent("Z");
            public GUIContent space = EditorGUIUtility.TrTextContent("Space", "Specifies if the velocity values are in local space (rotated with the transform) or world space.");
            public GUIContent speedMultiplier = EditorGUIUtility.TrTextContent("Speed Modifier", "Multiply the particle speed by this value");
            public GUIContent radial = EditorGUIUtility.TrTextContent("Radial", "Apply radial velocity to particles, which will project them out from the center of the system.");
            public GUIContent linearSpace = EditorGUIUtility.TrTextContent("Space", "Specifies if the velocity values are in local space (rotated with the transform) or world space.");
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

            m_X = new SerializedMinMaxCurve(this, s_Texts.linearX, "x", kUseSignedRange);
            m_Y = new SerializedMinMaxCurve(this, s_Texts.y, "y", kUseSignedRange);
            m_Z = new SerializedMinMaxCurve(this, s_Texts.z, "z", kUseSignedRange);
            m_OrbitalX = new SerializedMinMaxCurve(this, s_Texts.orbitalX, "orbitalX", kUseSignedRange);
            m_OrbitalY = new SerializedMinMaxCurve(this, s_Texts.y, "orbitalY", kUseSignedRange);
            m_OrbitalZ = new SerializedMinMaxCurve(this, s_Texts.z, "orbitalZ", kUseSignedRange);
            m_OrbitalOffsetX = new SerializedMinMaxCurve(this, s_Texts.orbitalOffsetX, "orbitalOffsetX", kUseSignedRange);
            m_OrbitalOffsetY = new SerializedMinMaxCurve(this, s_Texts.y, "orbitalOffsetY", kUseSignedRange);
            m_OrbitalOffsetZ = new SerializedMinMaxCurve(this, s_Texts.z, "orbitalOffsetZ", kUseSignedRange);
            m_Radial = new SerializedMinMaxCurve(this, s_Texts.radial, "radial", kUseSignedRange);
            m_InWorldSpace = GetProperty("inWorldSpace");
            m_SpeedModifier = new SerializedMinMaxCurve(this, s_Texts.speedMultiplier, "speedModifier", kUseSignedRange);
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUITripleMinMaxCurve(GUIContent.none, s_Texts.linearX, m_X, s_Texts.y, m_Y, s_Texts.z, m_Z, null);
            EditorGUI.indentLevel++;
            GUIBoolAsPopup(s_Texts.linearSpace, m_InWorldSpace, s_Texts.spaces);
            EditorGUI.indentLevel--;

            GUITripleMinMaxCurve(GUIContent.none, s_Texts.orbitalX, m_OrbitalX, s_Texts.y, m_OrbitalY, s_Texts.z, m_OrbitalZ, null);
            GUITripleMinMaxCurve(GUIContent.none, s_Texts.orbitalOffsetX, m_OrbitalOffsetX, s_Texts.y, m_OrbitalOffsetY, s_Texts.z, m_OrbitalOffsetZ, null);
            EditorGUI.indentLevel++;
            GUIMinMaxCurve(s_Texts.radial, m_Radial);
            EditorGUI.indentLevel--;

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

            failureReason = string.Empty;
            if (m_OrbitalX.maxConstant != 0.0f || m_OrbitalY.maxConstant != 0.0f || m_OrbitalZ.maxConstant != 0.0f)
                text += "\nVelocity over Lifetime module orbital velocity is being used";

            failureReason = string.Empty;
            if (m_Radial.maxConstant != 0.0f)
                text += "\nVelocity over Lifetime module radial velocity is being used";
        }
    }
} // namespace UnityEditor
