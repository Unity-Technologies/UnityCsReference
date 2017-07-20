// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(GridSelection))]
    internal class GridSelectionEditor : Editor
    {
        private const float iconSize = 32f;

        static class Styles
        {
            public static readonly GUIStyle header = new GUIStyle("IN GameObjectHeader");
            public static readonly GUIContent gridSelectionLabel = EditorGUIUtility.TextContent("Grid Selection");
        }

        public override void OnInspectorGUI()
        {
            if (GridPaintingState.activeBrushEditor && GridSelection.active)
            {
                GridPaintingState.activeBrushEditor.OnSelectionInspectorGUI();
            }
        }

        protected override void OnHeaderGUI()
        {
            EditorGUILayout.BeginHorizontal(Styles.header);
            Texture2D icon = AssetPreview.GetMiniTypeThumbnail(typeof(Grid));
            GUILayout.Label(icon, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
            EditorGUILayout.BeginVertical();
            GUILayout.Label(Styles.gridSelectionLabel);
            GridSelection.position = EditorGUILayout.BoundsIntField(GUIContent.none, GridSelection.position);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            DrawHeaderHelpAndSettingsGUI(GUILayoutUtility.GetLastRect());
        }

        public bool HasFrameBounds()
        {
            return GridSelection.active;
        }

        public Bounds OnGetFrameBounds()
        {
            Bounds bounds = new Bounds();
            if (GridSelection.active)
            {
                Vector3Int gridMin = GridSelection.position.min;
                Vector3Int gridMax = GridSelection.position.max;

                Vector3 min = GridSelection.grid.CellToWorld(gridMin);
                Vector3 max = GridSelection.grid.CellToWorld(gridMax);

                bounds = new Bounds((max + min) * .5f, max - min);
            }
            return bounds;
        }
    }
}
