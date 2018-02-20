// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CanvasRenderer))]
    [CanEditMultipleObjects]
    internal class CanvasRendererEditor : Editor
    {
        private static GUIContent m_cullTransparentMeshContent =
            new GUIContent("Cull Transparent Mesh", "Cull if the vertex color alpha is close to zero for every vertex of the mesh.");

        private SerializedProperty m_CullTransparentMeshProperty;

        void OnEnable()
        {
            m_CullTransparentMeshProperty = serializedObject.FindProperty("m_CullTransparentMesh");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_CullTransparentMeshProperty, m_cullTransparentMeshContent);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
