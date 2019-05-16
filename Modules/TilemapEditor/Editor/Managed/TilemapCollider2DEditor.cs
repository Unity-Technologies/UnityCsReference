// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityEditor
{
    [CustomEditor(typeof(TilemapCollider2D))]
    [CanEditMultipleObjects]
    internal class TilemapCollider2DEditor : Collider2DEditorBase
    {
        private SerializedProperty m_MaximumTileChangeCount;
        private SerializedProperty m_ExtrusionFactor;

        private new static class Styles
        {
            public static readonly GUIContent maximumTileChangeCountLabel = EditorGUIUtility.TrTextContent("Max Tile Change Count"
                , "Maximum number of Tile Changes accumulated before doing a full collider rebuild instead of an incremental rebuild. "
                + "Change this if incremental rebuilds are slow for the number of Tile Changes accumulated.");
            public static readonly GUIContent extrusionFactorLabel = EditorGUIUtility.TrTextContent("Extrusion Factor"
                , "Extrudes collider shape of each Tile by this amount for helping with compositing fine gaps between "
                + "neighboring Tile's collider shape when using a CompositeCollider2D. This is in Unity world-space units.");
        }

        public override void OnEnable()
        {
            base.OnEnable();
            m_MaximumTileChangeCount = serializedObject.FindProperty("m_MaximumTileChangeCount");
            m_ExtrusionFactor = serializedObject.FindProperty("m_ExtrusionFactor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_MaximumTileChangeCount, Styles.maximumTileChangeCountLabel);
            using (new EditorGUI.DisabledScope(!(target as TilemapCollider2D).usedByComposite))
                EditorGUILayout.PropertyField(m_ExtrusionFactor, Styles.extrusionFactorLabel);
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();

            FinalizeInspectorGUI();
        }
    }
}
