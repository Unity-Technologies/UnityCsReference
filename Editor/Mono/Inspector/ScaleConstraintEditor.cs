// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Animations;

namespace UnityEditor
{
    [CustomEditor(typeof(ScaleConstraint))]
    [CanEditMultipleObjects]
    internal class ScaleConstraintEditor : ConstraintEditorBase
    {
        private SerializedProperty m_ScaleAtRest;
        private SerializedProperty m_ScaleOffset;
        private SerializedProperty m_Weight;
        private SerializedProperty m_IsContraintActive;
        private SerializedProperty m_IsLocked;
        private SerializedProperty m_Sources;

        internal override SerializedProperty atRest { get { return m_ScaleAtRest; } }
        internal override SerializedProperty offset { get { return m_ScaleOffset; } }
        internal override SerializedProperty weight { get { return m_Weight; } }
        internal override SerializedProperty isContraintActive { get { return m_IsContraintActive; } }
        internal override SerializedProperty isLocked { get { return m_IsLocked; } }
        internal override SerializedProperty sources { get { return m_Sources; } }

        private class Styles : IConstraintStyle
        {
            GUIContent m_Activate = EditorGUIUtility.TextContent("Activate|Activate the constraint at the current offset from the sources.");
            GUIContent m_Zero = EditorGUIUtility.TextContent("Zero|Activate the constraint at zero offset from the sources.");

            GUIContent m_ScaleAtRest = EditorGUIUtility.TextContent("Scale At Rest");
            GUIContent m_ScaleOffset = EditorGUIUtility.TextContent("Scale Offset");

            GUIContent m_Sources = EditorGUIUtility.TextContent("Sources");

            GUIContent m_Weight = EditorGUIUtility.TextContent("Weight");

            GUIContent m_ScalingAxes = EditorGUIUtility.TextContent("Freeze Scaling Axes");

            GUIContent m_IsActive = EditorGUIUtility.TextContent("Is Active");
            GUIContent m_IsLocked = EditorGUIUtility.TextContent("Lock|When set, evaluate with the current offset. When not set, update the offset based on the current transform.");

            GUIContent[] m_Axes =
            {
                EditorGUIUtility.TextContent("X"),
                EditorGUIUtility.TextContent("Y"),
                EditorGUIUtility.TextContent("Z")
            };

            GUIContent m_ConstraintSettings = EditorGUIUtility.TextContent("Constraint Settings");

            public GUIContent Activate { get { return m_Activate; } }
            public GUIContent Zero { get { return m_Zero; } }
            public GUIContent AtRest { get { return m_ScaleAtRest; } }
            public GUIContent Offset { get { return m_ScaleOffset; } }
            public GUIContent Sources { get { return m_Sources; } }
            public GUIContent Weight { get { return m_Weight; } }
            public GUIContent FreezeAxes { get { return m_ScalingAxes; } }
            public GUIContent IsActive { get { return m_IsActive; } }
            public GUIContent IsLocked { get { return m_IsLocked; } }
            public GUIContent[] Axes { get { return m_Axes; } }
            public GUIContent ConstraintSettings { get { return m_ConstraintSettings; } }
        }

        private static Styles s_Style;

        public void OnEnable()
        {
            if (s_Style == null)
                s_Style = new Styles();

            m_ScaleAtRest = serializedObject.FindProperty("m_ScaleAtRest");
            m_ScaleOffset = serializedObject.FindProperty("m_ScaleOffset");
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_IsContraintActive = serializedObject.FindProperty("m_IsContraintActive");
            m_IsLocked = serializedObject.FindProperty("m_IsLocked");
            m_Sources = serializedObject.FindProperty("m_Sources");

            OnEnable(s_Style);
        }

        internal override void OnValueAtRestChanged()
        {
            foreach (var t in targets)
                (t as ScaleConstraint).transform.localScale = atRest.vector3Value;
        }

        internal override void ShowFreezeAxesControl()
        {
            Rect drawRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, s_Style.FreezeAxes), EditorStyles.toggle);
            EditorGUI.MultiPropertyField(drawRect, s_Style.Axes, serializedObject.FindProperty("m_AffectScalingX"), s_Style.FreezeAxes);
        }

        public override void OnInspectorGUI()
        {
            if (s_Style == null)
                s_Style = new Styles();

            serializedObject.Update();

            ShowConstraintEditor<ScaleConstraint>(s_Style);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
