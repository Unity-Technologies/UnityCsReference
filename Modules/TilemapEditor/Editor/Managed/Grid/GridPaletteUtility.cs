// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal static class GridPaletteUtility
    {
        public static RectInt GetBounds(GameObject palette)
        {
            if (palette == null)
                return new RectInt();

            Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);

            foreach (var tilemap in palette.GetComponentsInChildren<Tilemap>())
            {
                Vector3Int p1 = tilemap.editorPreviewOrigin;
                Vector3Int p2 = p1 + tilemap.editorPreviewSize;
                Vector2Int tilemapMin = new Vector2Int(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y));
                Vector2Int tilemapMax = new Vector2Int(Mathf.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y));
                min = new Vector2Int(Mathf.Min(min.x, tilemapMin.x), Mathf.Min(min.y, tilemapMin.y));
                max = new Vector2Int(Mathf.Max(max.x, tilemapMax.x), Mathf.Max(max.y, tilemapMax.y));
            }

            return GridEditorUtility.GetMarqueeRect(min, max);
        }

        public static GameObject CreateNewPaletteNamed(string name, Grid.CellLayout layout, GridPalette.CellSizing cellSizing, Vector3 cellSize)
        {
            string defaultPath = ProjectBrowser.s_LastInteractedProjectBrowser ? ProjectBrowser.s_LastInteractedProjectBrowser.GetActiveFolderPath() : "Assets";
            string folderPath = EditorUtility.SaveFolderPanel("Create palette into folder ", defaultPath, "");
            folderPath = FileUtil.GetProjectRelativePath(folderPath);

            if (string.IsNullOrEmpty(folderPath))
                return null;

            return CreateNewPalette(folderPath, name, layout, cellSizing, cellSize);
        }

        public static GameObject CreateNewPalette(string folderPath, string name, Grid.CellLayout layout, GridPalette.CellSizing cellSizing, Vector3 cellSize)
        {
            GameObject temporaryGO = new GameObject(name);
            Grid grid = temporaryGO.AddComponent<Grid>();

            // We set size to kEpsilon to mark this as new uninitialized palette
            // Nice default size can be decided when first asset is dragged in
            grid.cellSize = cellSize;
            grid.cellLayout = layout;

            CreateNewLayer(temporaryGO, "Layer1", layout);

            string path = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + name + ".prefab");

            Object prefab = PrefabUtility.CreateEmptyPrefab(path);
            GridPalette palette = GridPalette.CreateInstance<GridPalette>();
            palette.name = "Palette Settings";
            palette.cellSizing = cellSizing;
            AssetDatabase.AddObjectToAsset(palette, prefab);
            PrefabUtility.ReplacePrefab(temporaryGO, prefab, ReplacePrefabOptions.Default);
            AssetDatabase.Refresh();

            GameObject.DestroyImmediate(temporaryGO);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static GameObject CreateNewLayer(GameObject paletteGO, string name, Grid.CellLayout layout)
        {
            GameObject newLayerGO = new GameObject(name);
            newLayerGO.AddComponent<Tilemap>();
            newLayerGO.AddComponent<TilemapRenderer>();
            newLayerGO.transform.parent = paletteGO.transform;
            newLayerGO.layer = paletteGO.layer;

            // Set defaults for certain layouts
            switch (layout)
            {
                case Grid.CellLayout.Rectangle:
                {
                    paletteGO.GetComponent<Grid>().cellSize = new Vector3(1f, 1f, 0f);
                    break;
                }
            }

            return newLayerGO;
        }

        public static Vector3 CalculateAutoCellSize(Grid grid, Vector3 defaultValue)
        {
            Tilemap[] tilemaps = grid.GetComponentsInChildren<Tilemap>();
            foreach (var tilemap in tilemaps)
            {
                foreach (var position in tilemap.cellBounds.allPositionsWithin)
                {
                    Sprite sprite = tilemap.GetSprite(position);
                    if (sprite != null)
                    {
                        return new Vector3(sprite.rect.width, sprite.rect.height, 0f) / sprite.pixelsPerUnit;
                    }
                }
            }
            return defaultValue;
        }
    }
}
