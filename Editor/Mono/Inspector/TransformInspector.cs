// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    internal class TransformInspector : Editor
    {
        SerializedProperty m_Position;
        SerializedProperty m_Scale;
        TransformRotationGUI m_RotationGUI;

        class Contents
        {
            public GUIContent positionContent = EditorGUIUtility.TextContent("Position|The local position of this GameObject relative to the parent.");
            public GUIContent scaleContent = EditorGUIUtility.TextContent("Scale|The local scaling of this GameObject relative to the parent.");
            public string floatingPointWarning = LocalizationDatabase.GetLocalizedString("Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.");
        }
        static Contents s_Contents;

        public void OnEnable()
        {
            m_Position = serializedObject.FindProperty("m_LocalPosition");
            m_Scale = serializedObject.FindProperty("m_LocalScale");

            if (m_RotationGUI == null)
                m_RotationGUI = new TransformRotationGUI();
            m_RotationGUI.OnEnable(serializedObject.FindProperty("m_LocalRotation"), EditorGUIUtility.TextContent("Rotation|The local rotation of this GameObject relative to the parent."));
        }

        public override void OnInspectorGUI()
        {
            if (s_Contents == null)
                s_Contents = new Contents();

            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 212;
            }

            serializedObject.Update();

            Inspector3D();
            // Warning if global position is too large for floating point errors.
            // SanitizeBounds function doesn't even support values beyond 100000
            Transform t = target as Transform;
            Vector3 pos = t.position;
            if (Mathf.Abs(pos.x) > 100000 || Mathf.Abs(pos.y) > 100000 || Mathf.Abs(pos.z) > 100000)
                EditorGUILayout.HelpBox(s_Contents.floatingPointWarning, MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }

        private void Inspector3D()
        {
            EditorGUILayout.PropertyField(m_Position, s_Contents.positionContent);
            m_RotationGUI.RotationField();
            EditorGUILayout.PropertyField(m_Scale, s_Contents.scaleContent);
        }
    }
}
