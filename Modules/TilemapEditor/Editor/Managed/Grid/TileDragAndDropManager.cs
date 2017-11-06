// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // This class is in charge of drag'n'drops of Tile assets on scene view
    internal class TileDragAndDropManager : ScriptableSingleton<TileDragAndDropManager>
    {
        private bool m_RegisteredEventHandlers;
        private Dictionary<Vector2Int, Object> m_HoverData;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            instance.RegisterEventHandlers();
        }

        void OnEnable()
        {
            RegisterEventHandlers();
        }

        void RegisterEventHandlers()
        {
            if (m_RegisteredEventHandlers)
                return;

            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
            m_RegisteredEventHandlers = true;
        }

        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            m_RegisteredEventHandlers = false;
        }

        private void OnSceneGUIDelegate(SceneView sceneView)
        {
            Event evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform && evt.type != EventType.DragExited && evt.type != EventType.Repaint)
                return;

            Grid activeGrid = GetActiveGrid();
            if (activeGrid == null || DragAndDrop.objectReferences.Length == 0)
                return;

            Vector3 localMouse = GridEditorUtility.ScreenToLocal(activeGrid.transform, evt.mousePosition);
            Vector3Int mouseGridPosition = activeGrid.LocalToCell(localMouse);

            switch (evt.type)
            {
                //TODO: Cache this
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    List<TileBase> tiles = TileDragAndDrop.GetValidTiles(DragAndDrop.objectReferences);
                    instance.m_HoverData = TileDragAndDrop.CreateHoverData(null, null, tiles);
                    if (instance.m_HoverData.Count > 0)
                    {
                        Event.current.Use();
                        GUI.changed = true;
                    }
                    break;
                case EventType.DragPerform:
                    if (instance.m_HoverData.Count > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Dictionary<Vector2Int, TileBase> tileSheet = TileDragAndDrop.ConvertToTileSheet(instance.m_HoverData);
                        Tilemap tilemap = GetOrCreateActiveTilemap();
                        tilemap.ClearAllEditorPreviewTiles();
                        foreach (KeyValuePair<Vector2Int, TileBase> item in tileSheet)
                        {
                            Vector3Int position = new Vector3Int(mouseGridPosition.x + item.Key.x, mouseGridPosition.y + item.Key.y, 0);
                            tilemap.SetTile(position, item.Value);
                        }
                        instance.m_HoverData = null;
                        GUI.changed = true;
                        Event.current.Use();
                    }
                    break;
                case EventType.Repaint:
                    if (instance.m_HoverData != null)
                    {
                        Tilemap map = Selection.activeGameObject.GetComponentInParent<Tilemap>();

                        if (map != null)
                            map.ClearAllEditorPreviewTiles();

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        foreach (KeyValuePair<Vector2Int, Object> item in instance.m_HoverData)
                        {
                            Vector3Int gridPos = mouseGridPosition + new Vector3Int(item.Key.x, item.Key.y, 0);
                            if (item.Value is TileBase)
                            {
                                TileBase tile = item.Value as TileBase;
                                if (map != null)
                                {
                                    map.SetEditorPreviewTile(gridPos, tile);
                                }
                            }
                        }
                    }
                    break;
            }

            if (instance.m_HoverData != null && (
                    Event.current.type == EventType.DragExited ||
                    Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
            {
                if (instance.m_HoverData.Count > 0)
                {
                    Tilemap map = Selection.activeGameObject.GetComponentInParent<Tilemap>();
                    if (map != null)
                        map.ClearAllEditorPreviewTiles();

                    Event.current.Use();
                }

                instance.m_HoverData = null;
            }
        }

        static Tilemap GetOrCreateActiveTilemap()
        {
            if (Selection.activeGameObject != null)
            {
                Tilemap tilemap = Selection.activeGameObject.GetComponentInParent<Tilemap>();
                if (tilemap == null)
                {
                    Grid grid = Selection.activeGameObject.GetComponentInParent<Grid>();
                    tilemap = CreateNewTilemap(grid);
                }
                return tilemap;
            }
            return null;
        }

        static Tilemap CreateNewTilemap(Grid grid)
        {
            GameObject go = new GameObject("Tilemap");
            go.transform.SetParent(grid.gameObject.transform);
            Tilemap map = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
            return map;
        }

        public static Grid GetActiveGrid()
        {
            if (Selection.activeGameObject != null)
            {
                return Selection.activeGameObject.GetComponentInParent<Grid>();
            }
            return null;
        }
    }
}
