// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;

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
            public static readonly GUIContent animationFrameRateLabel = EditorGUIUtility.TrTextContent("Animation Frame Rate", "Frame rate for playing animated tiles in the tilemap");
            public static readonly GUIContent tilemapColorLabel = EditorGUIUtility.TrTextContent("Color", "Color tinting all Sprites from tiles in the tilemap");
            public static readonly GUIContent tileAnchorLabel = EditorGUIUtility.TrTextContent("Tile Anchor", "Anchoring position for Sprites from tiles in the tilemap");
            public static readonly GUIContent orientationLabel = EditorGUIUtility.TrTextContent("Orientation", "Orientation for tiles in the tilemap");
            public static readonly string pointTopHexagonCreateUndo = L10n.Tr("Hexagonal Point Top Tilemap");
            public static readonly string flatTopHexagonCreateUndo = L10n.Tr("Hexagonal Flat Top Tilemap");
            public static readonly string isometricCreateUndo = L10n.Tr("Isometric Tilemap");
            public static readonly string isometricZAsYCreateUndo = L10n.Tr("Isometric Z As Y Tilemap");
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

        // Called from SceneView code using reflection
        private bool HasFrameBounds()
        {
            return true;
        }

        // Called from SceneView code using reflection
        private Bounds OnGetFrameBounds()
        {
            Bounds localBounds = tilemap.localFrameBounds;
            Bounds bounds = new Bounds(tilemap.transform.TransformPoint(localBounds.center), Vector3.zero);
            for (int i = 0; i < 8; ++i)
            {
                Vector3 extent = localBounds.extents;
                extent.x = (i & 1) == 0 ? -extent.x : extent.x;
                extent.y = (i & 2) == 0 ? -extent.y : extent.y;
                extent.z = (i & 4) == 0 ? -extent.z : extent.z;
                var worldPoint = tilemap.transform.TransformPoint(localBounds.center + extent);
                bounds.Encapsulate(worldPoint);
            }
            return bounds;
        }

        [MenuItem("GameObject/2D Object/Tilemap")]
        internal static void CreateRectangularTilemap()
        {
            var root = FindOrCreateRootGrid();
            var uniqueName = GameObjectUtility.GetUniqueNameForSibling(root.transform, "Tilemap");
            var tilemapGO = ObjectFactory.CreateGameObject(uniqueName, typeof(Tilemap), typeof(TilemapRenderer));
            Undo.SetTransformParent(tilemapGO.transform, root.transform, "");
            tilemapGO.transform.position = Vector3.zero;

            Selection.activeGameObject = tilemapGO;
            Undo.SetCurrentGroupName("Create Tilemap");
        }

        [MenuItem("GameObject/2D Object/Hexagonal Point Top Tilemap")]
        internal static void CreateHexagonalPointTopTilemap()
        {
            CreateHexagonalTilemap(GridLayout.CellSwizzle.XYZ, Styles.pointTopHexagonCreateUndo);
        }

        [MenuItem("GameObject/2D Object/Hexagonal Flat Top Tilemap")]
        internal static void CreateHexagonalFlatTopTilemap()
        {
            CreateHexagonalTilemap(GridLayout.CellSwizzle.YXZ, Styles.flatTopHexagonCreateUndo);
        }

        [MenuItem("GameObject/2D Object/Isometric Tilemap")]
        internal static void CreateIsometricTilemap()
        {
            CreateIsometricTilemap(GridLayout.CellLayout.Isometric, Styles.isometricCreateUndo);
        }

        [MenuItem("GameObject/2D Object/Isometric Z As Y Tilemap")]
        internal static void CreateIsometricZAsYTilemap()
        {
            CreateIsometricTilemap(GridLayout.CellLayout.IsometricZAsY, Styles.isometricZAsYCreateUndo);
        }

        private static void CreateIsometricTilemap(GridLayout.CellLayout isometricLayout, string undoMessage)
        {
            var root = FindOrCreateRootGrid();
            var uniqueName = GameObjectUtility.GetUniqueNameForSibling(root.transform, "Tilemap");
            var tilemapGO = ObjectFactory.CreateGameObject(uniqueName, typeof(Tilemap), typeof(TilemapRenderer));
            tilemapGO.transform.SetParent(root.transform);
            tilemapGO.transform.position = Vector3.zero;

            var grid = root.GetComponent<Grid>();
            // Case 1071703: Do not reset cell size if adding a new Tilemap to an existing Grid of the same layout
            if (isometricLayout != grid.cellLayout)
            {
                grid.cellLayout = isometricLayout;
                grid.cellSize = new Vector3(1.0f, 0.5f, 1.0f);
            }

            var tilemapRenderer = tilemapGO.GetComponent<TilemapRenderer>();
            tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopRight;

            Selection.activeGameObject = tilemapGO;
            Undo.RegisterCreatedObjectUndo(tilemapGO, undoMessage);
        }

        private static void CreateHexagonalTilemap(GridLayout.CellSwizzle swizzle, string undoMessage)
        {
            var root = FindOrCreateRootGrid();
            var uniqueName = GameObjectUtility.GetUniqueNameForSibling(root.transform, "Tilemap");
            var tilemapGO = ObjectFactory.CreateGameObject(uniqueName, typeof(Tilemap), typeof(TilemapRenderer));
            tilemapGO.transform.SetParent(root.transform);
            tilemapGO.transform.position = Vector3.zero;
            var grid = root.GetComponent<Grid>();
            grid.cellLayout = Grid.CellLayout.Hexagon;
            grid.cellSwizzle = swizzle;
            var tilemap = tilemapGO.GetComponent<Tilemap>();
            tilemap.tileAnchor = Vector3.zero;
            Selection.activeGameObject = tilemapGO;
            Undo.RegisterCreatedObjectUndo(tilemapGO, undoMessage);
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
                gridGO = ObjectFactory.CreateGameObject("Grid", typeof(Grid));
                gridGO.transform.position = Vector3.zero;

                var grid = gridGO.GetComponent<Grid>();
                grid.cellSize = new Vector3(1.0f, 1.0f, 0.0f);
                Undo.SetCurrentGroupName("Create Grid");
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
            Undo.RegisterCompleteObjectUndo(tilemap, "Compress Tilemap Bounds");
            tilemap.CompressBounds();
        }
    }
}
