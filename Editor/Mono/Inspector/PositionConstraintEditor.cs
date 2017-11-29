// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Animations;

namespace UnityEditor
{
    [CustomEditor(typeof(PositionConstraint))]
    [CanEditMultipleObjects]
    internal class PositionConstraintEditor : ConstraintEditorBase
    {
        private SerializedProperty m_TranslationAtRest;
        private SerializedProperty m_TranslationOffset;
        private SerializedProperty m_Weight;
        private SerializedProperty m_IsContraintActive;
        private SerializedProperty m_IsLocked;
        private SerializedProperty m_Sources;

        internal override SerializedProperty atRest { get { return m_TranslationAtRest; } }
        internal override SerializedProperty offset { get { return m_TranslationOffset; } }
        internal override SerializedProperty weight { get { return m_Weight; } }
        internal override SerializedProperty isContraintActive { get { return m_IsContraintActive; } }
        internal override SerializedProperty isLocked { get { return m_IsLocked; } }
        internal override SerializedProperty sources { get { return m_Sources; } }

        private class Styles : ConstraintStyleBase
        {
            GUIContent m_RestTranslation = EditorGUIUtility.TrTextContent("Position At Rest");
            GUIContent m_TranslationOffset = EditorGUIUtility.TrTextContent("Position Offset");

            GUIContent m_TranslationAxes = EditorGUIUtility.TrTextContent("Freeze Position Axes");

            public override GUIContent AtRest { get { return m_RestTranslation; } }
            public override GUIContent Offset { get { return m_TranslationOffset; }  }
            public GUIContent FreezeAxes { get { return m_TranslationAxes; } }
        }

        private static Styles s_Style;

        public void OnEnable()
        {
            if (s_Style == null)
                s_Style = new Styles();

            m_TranslationAtRest = serializedObject.FindProperty("m_TranslationAtRest");
            m_TranslationOffset = serializedObject.FindProperty("m_TranslationOffset");
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_IsContraintActive = serializedObject.FindProperty("m_IsContraintActive");
            m_IsLocked = serializedObject.FindProperty("m_IsLocked");
            m_Sources = serializedObject.FindProperty("m_Sources");

            OnEnable(s_Style);
        }

        internal override void OnValueAtRestChanged()
        {
            foreach (var t in targets)
                (t as PositionConstraint).transform.localPosition = atRest.vector3Value;
        }

        internal override void ShowFreezeAxesControl()
        {
            Rect drawRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, s_Style.FreezeAxes), EditorStyles.toggle);
            EditorGUI.MultiPropertyField(drawRect, s_Style.Axes, serializedObject.FindProperty("m_AffectTranslationX"), s_Style.FreezeAxes);
        }

        public override void OnInspectorGUI()
        {
            if (s_Style == null)
                s_Style = new Styles();

            serializedObject.Update();

            ShowConstraintEditor<PositionConstraint>(s_Style);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
