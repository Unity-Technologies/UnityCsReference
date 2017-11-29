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

        private class Styles : ConstraintStyleBase
        {
            GUIContent m_ScaleAtRest = EditorGUIUtility.TrTextContent("Scale At Rest");
            GUIContent m_ScaleOffset = EditorGUIUtility.TrTextContent("Scale Offset");

            GUIContent m_ScalingAxes = EditorGUIUtility.TrTextContent("Freeze Scaling Axes");

            public override GUIContent AtRest { get { return m_ScaleAtRest; } }
            public override GUIContent Offset { get { return m_ScaleOffset; } }
            public GUIContent FreezeAxes { get { return m_ScalingAxes; } }
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
