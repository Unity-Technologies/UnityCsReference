// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class Collider3DEditorBase : ColliderEditorBase
    {
        protected SerializedProperty m_Material;
        protected SerializedProperty m_IsTrigger;

        public override void OnEnable()
        {
            base.OnEnable();
            m_Material = serializedObject.FindProperty("m_Material");
            m_IsTrigger = serializedObject.FindProperty("m_IsTrigger");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_IsTrigger);
            EditorGUILayout.PropertyField(m_Material);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
