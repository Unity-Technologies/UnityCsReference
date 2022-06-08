// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CapsuleCollider2D))]
    [CanEditMultipleObjects]
    class CapsuleCollider2DEditor : Collider2DEditorBase
    {
        SerializedProperty m_Size;
        SerializedProperty m_Direction;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Size = serializedObject.FindProperty("m_Size");
            m_Direction = serializedObject.FindProperty("m_Direction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Collider"), this);

            GUILayout.Space(5);
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_Size);
            EditorGUILayout.PropertyField(m_Direction);

            FinalizeInspectorGUI();
        }
    }
}
