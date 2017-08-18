// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(TerrainCollider))]
    [CanEditMultipleObjects]
    internal class TerrainColliderEditor : Collider3DEditorBase
    {
        SerializedProperty m_TerrainData;
        SerializedProperty m_EnableTreeColliders;

        protected GUIContent terrainContent = EditorGUIUtility.TextContent("Terrain Data|The TerrainData asset that stores heightmaps, terrain textures, detail meshes and trees.");
        protected GUIContent treeColliderContent = EditorGUIUtility.TextContent("Enable Tree Colliders|When selected, Tree Colliders will be enabled.");

        public override void OnEnable()
        {
            base.OnEnable();

            m_TerrainData = serializedObject.FindProperty("m_TerrainData");
            m_EnableTreeColliders = serializedObject.FindProperty("m_EnableTreeColliders");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Material, materialContent);
            EditorGUILayout.PropertyField(m_TerrainData, terrainContent);
            EditorGUILayout.PropertyField(m_EnableTreeColliders, treeColliderContent);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
