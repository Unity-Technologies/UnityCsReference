// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Animations;

namespace UnityEditor
{
    [CustomEditor(typeof(RotationConstraint))]
    [CanEditMultipleObjects]
    internal class RotationConstraintEditor : ConstraintEditorBase
    {
        private SerializedProperty m_RotationAtRest;
        private SerializedProperty m_RotationOffset;
        private SerializedProperty m_Weight;
        private SerializedProperty m_IsContraintActive;
        private SerializedProperty m_IsLocked;
        private SerializedProperty m_Sources;

        internal override SerializedProperty atRest { get { return m_RotationAtRest; } }
        internal override SerializedProperty offset { get { return m_RotationOffset; } }
        internal override SerializedProperty weight { get { return m_Weight; } }
        internal override SerializedProperty isContraintActive { get { return m_IsContraintActive; } }
        internal override SerializedProperty isLocked { get { return m_IsLocked; } }
        internal override SerializedProperty sources { get { return m_Sources; } }

        private class Styles : ConstraintStyleBase
        {
            GUIContent m_RotationAtRest = EditorGUIUtility.TrTextContent("Rotation At Rest");
            GUIContent m_RotationOffset = EditorGUIUtility.TrTextContent("Rotation Offset");

            GUIContent m_RotationAxes = EditorGUIUtility.TrTextContent("Freeze Rotation Axes");

            public override GUIContent AtRest { get { return m_RotationAtRest; } }
            public override GUIContent Offset { get { return m_RotationOffset; } }
            public GUIContent FreezeAxes { get { return m_RotationAxes; } }
        }

        private static Styles s_Style = null;

        public void OnEnable()
        {
            if (s_Style == null)
                s_Style = new Styles();

            m_RotationAtRest = serializedObject.FindProperty("m_RotationAtRest");
            m_RotationOffset = serializedObject.FindProperty("m_RotationOffset");
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_IsContraintActive = serializedObject.FindProperty("m_IsContraintActive");
            m_IsLocked = serializedObject.FindProperty("m_IsLocked");
            m_Sources = serializedObject.FindProperty("m_Sources");

            OnEnable(s_Style);
        }

        internal override void OnValueAtRestChanged()
        {
            foreach (var t in targets)
                (t as RotationConstraint).transform.SetLocalEulerAngles(atRest.vector3Value, RotationOrder.OrderZXY);
        }

        internal override void ShowFreezeAxesControl()
        {
            Rect drawRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, s_Style.FreezeAxes), EditorStyles.toggle);
            EditorGUI.MultiPropertyField(drawRect, s_Style.Axes, serializedObject.FindProperty("m_AffectRotationX"), s_Style.FreezeAxes);
        }

        public override void OnInspectorGUI()
        {
            if (s_Style == null)
                s_Style = new Styles();

            serializedObject.Update();

            ShowConstraintEditor<RotationConstraint>(s_Style);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
