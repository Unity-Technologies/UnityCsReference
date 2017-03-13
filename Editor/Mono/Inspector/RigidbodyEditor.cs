// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Rigidbody))]
    [CanEditMultipleObjects]
    internal class RigidbodyEditor : Editor
    {
        SerializedProperty m_Constraints;
        static GUIContent m_FreezePositionLabel = new GUIContent("Freeze Position");
        static GUIContent m_FreezeRotationLabel = new GUIContent("Freeze Rotation");

        public void OnEnable()
        {
            m_Constraints = serializedObject.FindProperty("m_Constraints");
        }

        void ConstraintToggle(Rect r, string label, RigidbodyConstraints value, int bit)
        {
            bool toggle = ((int)value & (1 << bit)) != 0;
            EditorGUI.showMixedValue = (m_Constraints.hasMultipleDifferentValuesBitwise & (1 << bit)) != 0;
            EditorGUI.BeginChangeCheck();
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            toggle = EditorGUI.ToggleLeft(r, label, toggle);
            EditorGUI.indentLevel = oldIndent;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, "Edit Constraints");
                m_Constraints.SetBitAtIndexForAllTargetsImmediate(bit, toggle);
            }
            EditorGUI.showMixedValue = false;
        }

        void ToggleBlock(RigidbodyConstraints constraints, GUIContent label, int x, int y, int z)
        {
            const int toggleOffset = 30;
            GUILayout.BeginHorizontal();
            Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUILayout.kLabelFloatMaxW, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.numberField);
            int id = GUIUtility.GetControlID(7231, FocusType.Keyboard, r);
            r = EditorGUI.PrefixLabel(r, id, label);
            r.width = toggleOffset;
            ConstraintToggle(r, "X", constraints, x);
            r.x += toggleOffset;
            ConstraintToggle(r, "Y", constraints, y);
            r.x += toggleOffset;
            ConstraintToggle(r, "Z", constraints, z);
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.BeginHorizontal();
            m_Constraints.isExpanded = EditorGUILayout.Foldout(m_Constraints.isExpanded, "Constraints", true);
            GUILayout.EndHorizontal();

            serializedObject.Update();
            RigidbodyConstraints constraints = (RigidbodyConstraints)m_Constraints.intValue;
            if (m_Constraints.isExpanded)
            {
                EditorGUI.indentLevel++;
                ToggleBlock(constraints, m_FreezePositionLabel, 1, 2, 3);
                ToggleBlock(constraints, m_FreezeRotationLabel, 4, 5, 6);
                EditorGUI.indentLevel--;
            }
        }
    }
}
