// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class ClampVelocityModuleUI : ModuleUI
    {
        SerializedMinMaxCurve m_X;
        SerializedMinMaxCurve m_Y;
        SerializedMinMaxCurve m_Z;
        SerializedMinMaxCurve m_Magnitude;
        SerializedProperty m_SeparateAxes;
        SerializedProperty m_InWorldSpace;
        SerializedProperty m_Dampen;
        SerializedMinMaxCurve m_Drag;
        SerializedProperty m_MultiplyDragByParticleSize;
        SerializedProperty m_MultiplyDragByParticleVelocity;

        class Texts
        {
            public GUIContent x = EditorGUIUtility.TextContent("X");
            public GUIContent y = EditorGUIUtility.TextContent("Y");
            public GUIContent z = EditorGUIUtility.TextContent("Z");
            public GUIContent dampen = EditorGUIUtility.TextContent("Dampen|Controls how much the velocity that exceeds the velocity limit should be dampened. A value of 0.5 will dampen the exceeding velocity by 50%.");
            public GUIContent magnitude = EditorGUIUtility.TextContent("Speed|The speed limit of particles over the particle lifetime.");
            public GUIContent separateAxes = EditorGUIUtility.TextContent("Separate Axes|If enabled, you can control the velocity limit separately for each axis.");
            public GUIContent space = EditorGUIUtility.TextContent("Space|Specifies if the velocity values are in local space (rotated with the transform) or world space.");
            public string[] spaces = { "Local", "World" };
            public GUIContent drag = EditorGUIUtility.TextContent("Drag|Control the amount of drag applied to each particle during its lifetime.");
            public GUIContent multiplyDragByParticleSize = EditorGUIUtility.TextContent("Multiply by Size|Adjust the drag based on the size of the particles.");
            public GUIContent multiplyDragByParticleVelocity = EditorGUIUtility.TextContent("Multiply by Velocity|Adjust the drag based on the velocity of the particles.");
        }
        static Texts s_Texts;


        public ClampVelocityModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "ClampVelocityModule", displayName)
        {
            m_ToolTip = "Controls the velocity limit and damping of each particle during its lifetime.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_X != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_X = new SerializedMinMaxCurve(this, s_Texts.x, "x");
            m_Y = new SerializedMinMaxCurve(this, s_Texts.y, "y");
            m_Z = new SerializedMinMaxCurve(this, s_Texts.z, "z");
            m_Magnitude = new SerializedMinMaxCurve(this, s_Texts.magnitude, "magnitude");
            m_SeparateAxes = GetProperty("separateAxis");
            m_InWorldSpace = GetProperty("inWorldSpace");
            m_Dampen = GetProperty("dampen");
            m_Drag = new SerializedMinMaxCurve(this, s_Texts.drag, "drag");
            m_MultiplyDragByParticleSize = GetProperty("multiplyDragByParticleSize");
            m_MultiplyDragByParticleVelocity = GetProperty("multiplyDragByParticleVelocity");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            EditorGUI.BeginChangeCheck();
            bool separateAxes = GUIToggle(s_Texts.separateAxes, m_SeparateAxes);
            if (EditorGUI.EndChangeCheck())
            {
                // Remove old curves from curve editor
                if (separateAxes)
                {
                    m_Magnitude.RemoveCurveFromEditor();
                }
                else
                {
                    m_X.RemoveCurveFromEditor();
                    m_Y.RemoveCurveFromEditor();
                    m_Z.RemoveCurveFromEditor();
                }
            }

            // Keep states in sync
            if (!m_X.stateHasMultipleDifferentValues)
            {
                m_Y.SetMinMaxState(m_X.state, separateAxes);
                m_Z.SetMinMaxState(m_X.state, separateAxes);
            }

            if (separateAxes)
            {
                GUITripleMinMaxCurve(GUIContent.none, s_Texts.x, m_X, s_Texts.y, m_Y, s_Texts.z, m_Z, null);
                EditorGUI.indentLevel++;
                GUIBoolAsPopup(s_Texts.space, m_InWorldSpace, s_Texts.spaces);
                EditorGUI.indentLevel--;
            }
            else
            {
                GUIMinMaxCurve(s_Texts.magnitude, m_Magnitude);
            }

            EditorGUI.indentLevel++;
            GUIFloat(s_Texts.dampen, m_Dampen);
            EditorGUI.indentLevel--;

            GUIMinMaxCurve(s_Texts.drag, m_Drag);
            EditorGUI.indentLevel++;
            GUIToggle(s_Texts.multiplyDragByParticleSize, m_MultiplyDragByParticleSize);
            GUIToggle(s_Texts.multiplyDragByParticleVelocity, m_MultiplyDragByParticleVelocity);
            EditorGUI.indentLevel--;
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            text += "\nLimit Velocity over Lifetime module is enabled.";
        }
    }
} // namespace UnityEditor
