// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(TilemapRenderer))]
    [CanEditMultipleObjects]
    internal class TilemapRendererEditor : RendererEditorBase
    {
        private SerializedProperty m_Material;
        private SerializedProperty m_SortOrder;
        private SerializedProperty m_MaskInteraction;

        private static class Styles
        {
            public static readonly GUIContent materialLabel = EditorGUIUtility.TextContent("Material");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Material = serializedObject.FindProperty("m_Materials.Array"); // Only allow to edit one material
            m_SortOrder = serializedObject.FindProperty("m_SortOrder");
            m_MaskInteraction = serializedObject.FindProperty("m_MaskInteraction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Material.GetArrayElementAtIndex(0), Styles.materialLabel, true);
            EditorGUILayout.PropertyField(m_SortOrder);
            RenderSortingLayerFields();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_MaskInteraction);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
