// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Net.Mime;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(MeshCollider))]
    [CanEditMultipleObjects]
    internal class MeshColliderEditor : Collider3DEditorBase
    {
        private SerializedProperty m_Mesh;
        private SerializedProperty m_Convex;
        private SerializedProperty m_InflateMesh;
        private SerializedProperty m_SkinWidth;

        static class Texts
        {
            public static GUIContent isTriggerText = new GUIContent("Is Trigger", "Is this collider a trigger? Triggers are only supported on convex colliders.");
            public static GUIContent convextText = new GUIContent("Convex", "Is this collider convex?");
            public static GUIContent inflateMeshText = new GUIContent("Inflate Mesh", "Should collision generation inflate the mesh.");
            public static GUIContent skinWidthText = new GUIContent("Skin Width", "How far out to inflate the mesh when building collision mesh.");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Mesh = serializedObject.FindProperty("m_Mesh");
            m_Convex = serializedObject.FindProperty("m_Convex");
            m_InflateMesh = serializedObject.FindProperty("m_InflateMesh");
            m_SkinWidth = serializedObject.FindProperty("m_SkinWidth");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Convex, Texts.convextText);

            if (EditorGUI.EndChangeCheck() && m_Convex.boolValue == false)
                m_IsTrigger.boolValue = false;

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!m_Convex.boolValue))
            {
                EditorGUILayout.PropertyField(m_InflateMesh, Texts.inflateMeshText);
            }

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!m_InflateMesh.boolValue))
            {
                EditorGUILayout.PropertyField(m_SkinWidth, Texts.skinWidthText);
            }
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!m_Convex.boolValue))
            {
                EditorGUILayout.PropertyField(m_IsTrigger, Texts.isTriggerText);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(m_Material);

            EditorGUILayout.PropertyField(m_Mesh);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
