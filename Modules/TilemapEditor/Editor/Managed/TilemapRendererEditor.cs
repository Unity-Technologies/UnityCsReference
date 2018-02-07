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
        private SerializedProperty m_ChunkCullingBounds;
        private SerializedProperty m_DetectChunkCullingBounds;

        private TilemapRenderer tilemapRenderer
        {
            get { return target as TilemapRenderer; }
        }

        private static class Styles
        {
            public static readonly GUIContent materialLabel = EditorGUIUtility.TrTextContent("Material", "Material to be used by TilemapRenderer");
            public static readonly GUIContent maskInteractionLabel = EditorGUIUtility.TrTextContent("Mask Interaction", "TilemapRenderer's interaction with a Sprite Mask");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Material = serializedObject.FindProperty("m_Materials.Array"); // Only allow to edit one material
            m_SortOrder = serializedObject.FindProperty("m_SortOrder");
            m_MaskInteraction = serializedObject.FindProperty("m_MaskInteraction");
            m_ChunkCullingBounds = serializedObject.FindProperty("m_ChunkCullingBounds");
            m_DetectChunkCullingBounds = serializedObject.FindProperty("m_DetectChunkCullingBounds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Material.GetArrayElementAtIndex(0), Styles.materialLabel, true);
            EditorGUILayout.PropertyField(m_SortOrder);
            EditorGUILayout.PropertyField(m_DetectChunkCullingBounds);
            GUI.enabled = (!m_DetectChunkCullingBounds.hasMultipleDifferentValues && TilemapRenderer.DetectChunkCullingBounds.Manual == tilemapRenderer.detectChunkCullingBounds);
            EditorGUILayout.PropertyField(m_ChunkCullingBounds);
            GUI.enabled = true;

            RenderSortingLayerFields();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_MaskInteraction, Styles.maskInteractionLabel);
            RenderRenderingLayer();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
