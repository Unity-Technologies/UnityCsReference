// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    [CustomEditor(typeof(Rigidbody))]
    [CanEditMultipleObjects]
    internal class RigidbodyEditor : Editor
    {
        SerializedProperty m_Constraints;
        static GUIContent m_FreezePositionLabel = EditorGUIUtility.TrTextContent("Freeze Position");
        static GUIContent m_FreezeRotationLabel = EditorGUIUtility.TrTextContent("Freeze Rotation");

        readonly AnimBool m_ShowInfo = new AnimBool();
        private bool m_RequiresConstantRepaint;
        private SavedBool m_ShowInfoFoldout;

        public void OnEnable()
        {
            m_Constraints = serializedObject.FindProperty("m_Constraints");
            m_ShowInfo.valueChanged.AddListener(Repaint);

            m_RequiresConstantRepaint = false;
            m_ShowInfoFoldout = new SavedBool($"{target.GetType()}.ShowFoldout", false);
            m_ShowInfo.value = m_ShowInfoFoldout.value;
        }

        public void OnDisable()
        {
            m_ShowInfo.valueChanged.RemoveListener(Repaint);
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

            Rect position = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(position, null, m_Constraints);
            m_Constraints.isExpanded = EditorGUI.Foldout(position, m_Constraints.isExpanded, m_Constraints.displayName, true);
            EditorGUI.EndProperty();

            serializedObject.Update();
            RigidbodyConstraints constraints = (RigidbodyConstraints)m_Constraints.intValue;
            if (m_Constraints.isExpanded)
            {
                EditorGUI.indentLevel++;
                ToggleBlock(constraints, m_FreezePositionLabel, 1, 2, 3);
                ToggleBlock(constraints, m_FreezeRotationLabel, 4, 5, 6);
                EditorGUI.indentLevel--;
            }

            ShowBodyInfoProperties();
        }

        private void ShowBodyInfoProperties()
        {
            m_RequiresConstantRepaint = false;

            Rect position = EditorGUILayout.GetControlRect();
            m_ShowInfoFoldout.value = m_ShowInfo.target = EditorGUI.Foldout(position, m_ShowInfo.target, "Info", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowInfo.faded))
            {
                if (targets.Length == 1)
                {
                    var body = targets[0] as Rigidbody;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.FloatField("Speed", body.velocity.magnitude);
                    EditorGUILayout.Vector3Field("Velocity", body.velocity);
                    EditorGUILayout.Vector3Field("Angular Velocity", body.angularVelocity);
                    EditorGUILayout.Vector3Field("Inertia Tensor", body.inertiaTensor);
                    EditorGUILayout.Vector3Field("Inertia Tensor Rotation", body.inertiaTensorRotation.eulerAngles);
                    EditorGUILayout.Vector3Field("Local Center of Mass", body.centerOfMass);
                    EditorGUILayout.Vector3Field("World Center of Mass", body.worldCenterOfMass);

                    EditorGUILayout.LabelField("Sleep State", body.IsSleeping() ? "Asleep" : "Awake");
                    EditorGUI.EndDisabledGroup();

                    // We need to repaint as some of the above properties can change without causing a repaint.
                    if (EditorApplication.isPlaying)
                        m_RequiresConstantRepaint = true;
                }
                else
                {
                    EditorGUILayout.HelpBox("Cannot show Info properties when multiple bodies are selected.", MessageType.Info);
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        public override bool RequiresConstantRepaint()
        {
            return m_RequiresConstantRepaint;
        }
    }
}
