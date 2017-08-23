// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using Enum = System.Enum;

namespace UnityEditor
{
    [CustomEditor(typeof(Tilemap))]
    [CanEditMultipleObjects]
    internal class TilemapEditor : Editor
    {
        private SerializedProperty m_AnimationFrameRate;
        private SerializedProperty m_TilemapColor;
        private SerializedProperty m_TileAnchor;
        private SerializedProperty m_Orientation;
        private SerializedProperty m_OrientationMatrix;

        private Tilemap tilemap { get { return (target as Tilemap); } }

        private static class Styles
        {
            public static readonly GUIContent animationFrameRateLabel = EditorGUIUtility.TextContent("Animation Frame Rate|Frame rate for playing animated tiles in the tilemap");
            public static readonly GUIContent tilemapColorLabel = EditorGUIUtility.TextContent("Color|Color tinting all Sprites from tiles in the tilemap");
            public static readonly GUIContent tileAnchorLabel = EditorGUIUtility.TextContent("Tile Anchor|Anchoring position for Sprites from tiles in the tilemap");
            public static readonly GUIContent orientationLabel = EditorGUIUtility.TextContent("Orientation|Orientation for tiles in the tilemap");
        }

        private void OnEnable()
        {
            m_AnimationFrameRate = serializedObject.FindProperty("m_AnimationFrameRate");
            m_TilemapColor = serializedObject.FindProperty("m_Color");
            m_TileAnchor = serializedObject.FindProperty("m_TileAnchor");
            m_Orientation = serializedObject.FindProperty("m_TileOrientation");
            m_OrientationMatrix = serializedObject.FindProperty("m_TileOrientationMatrix");
        }

        private void OnDisable()
        {
            if (tilemap != null)
                tilemap.ClearAllEditorPreviewTiles();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_AnimationFrameRate, Styles.animationFrameRateLabel);
            EditorGUILayout.PropertyField(m_TilemapColor, Styles.tilemapColorLabel);
            EditorGUILayout.PropertyField(m_TileAnchor, Styles.tileAnchorLabel);
            EditorGUILayout.PropertyField(m_Orientation, Styles.orientationLabel);
            GUI.enabled = (!m_Orientation.hasMultipleDifferentValues && Tilemap.Orientation.Custom == tilemap.orientation);
            if (targets.Length > 1)
                EditorGUILayout.PropertyField(m_OrientationMatrix, true);
            else
            {
                EditorGUI.BeginChangeCheck();
                var orientationMatrix = TileEditor.TransformMatrixOnGUI(tilemap.orientationMatrix);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tilemap, "Tilemap property change");
                    tilemap.orientationMatrix = orientationMatrix;
                }
            }
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("GameObject/2D Object/Tilemap")]
        internal static void CreateRectangularTilemap()
        {
            var root = FindOrCreateRootGrid();
            var uniqueName = GameObjectUtility.GetUniqueNameForSibling(root.transform, "Tilemap");
            var tilemapGO = new GameObject(uniqueName, typeof(Tilemap), typeof(TilemapRenderer));
            tilemapGO.transform.SetParent(root.transform);
            tilemapGO.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(tilemapGO, "Create Tilemap");
        }

        private static GameObject FindOrCreateRootGrid()
        {
            GameObject gridGO = null;

            // Check if active object has a Grid and can be a parent for the Tile Map
            if (Selection.activeObject is GameObject)
            {
                var go = (GameObject)Selection.activeObject;
                var parentGrid = go.GetComponentInParent<Grid>();
                if (parentGrid != null)
                {
                    gridGO = parentGrid.gameObject;
                }
            }

            // If neither the active object nor its parent has a grid, create a grid for the tilemap
            if (gridGO == null)
            {
                gridGO = new GameObject("Grid", typeof(Grid));
                gridGO.transform.position = Vector3.zero;

                var grid = gridGO.GetComponent<Grid>();
                grid.cellSize = new Vector3(1.0f, 1.0f, 0.0f);
                Undo.RegisterCreatedObjectUndo(gridGO, "Create Grid");
            }

            return gridGO;
        }

        [MenuItem("CONTEXT/Tilemap/Refresh All Tiles")]
        static internal void RefreshAllTiles(MenuCommand item)
        {
            Tilemap tilemap = (Tilemap)item.context;
            tilemap.RefreshAllTiles();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/Tilemap/Compress Tilemap Bounds")]
        static internal void CompressBounds(MenuCommand item)
        {
            Tilemap tilemap = (Tilemap)item.context;
            tilemap.CompressBounds();
        }
    }
}
