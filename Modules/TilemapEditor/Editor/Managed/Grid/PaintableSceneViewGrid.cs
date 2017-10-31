// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class PaintableSceneViewGrid : PaintableGrid
    {
        private Transform gridTransform { get { return grid != null ? grid.transform : null; } }
        private Grid grid { get { return brushTarget != null ? brushTarget.GetComponentInParent<Grid>() : (Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<Grid>() : null); } }
        private GridBrushBase gridBrush { get { return GridPaintingState.gridBrush; } }
        private SceneView activeSceneView = null;

        GameObject brushTarget
        {
            get { return GridPaintingState.scenePaintTarget; }
        }

        public Tilemap tilemap
        {
            get
            {
                if (brushTarget != null)
                {
                    return brushTarget.GetComponent<Tilemap>();
                }
                return null;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SceneView.onSceneGUIDelegate += OnSceneGUI;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            GridSelection.gridSelectionChanged += OnGridSelectionChanged;
        }

        protected override void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            GridSelection.gridSelectionChanged -= OnGridSelectionChanged;
            base.OnDisable();
        }

        private void OnGridSelectionChanged()
        {
            SceneView.RepaintAll();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            UpdateMouseGridPosition();
            base.OnGUI();
            if (PaintableGrid.InGridEditMode())
            {
                CallOnSceneGUI();
                if ((grid != null) && (GridPaintingState.activeGrid == this || GridSelection.active))
                {
                    CallOnPaintSceneGUI();
                }
                if (Event.current.type == EventType.Repaint)
                    EditorGUIUtility.AddCursorRect(new Rect(0, EditorGUI.kWindowToolbarHeight, sceneView.position.width, sceneView.position.height - EditorGUI.kWindowToolbarHeight), MouseCursor.CustomCursor);
            }
            HandleMouseEnterLeave(sceneView);
        }

        private void HandleMouseEnterLeave(SceneView sceneView)
        {
            if (inEditMode)
            {
                if (Event.current.type == EventType.MouseEnterWindow)
                {
                    OnMouseEnter(sceneView);
                }
                else if (Event.current.type == EventType.MouseLeaveWindow)
                {
                    OnMouseLeave(sceneView);
                }
                else if (sceneView.docked &&
                         (Application.platform == RuntimePlatform.OSXEditor
                          || Application.platform == RuntimePlatform.LinuxEditor))
                {
                    var guiPoint = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                    if (sceneView.position.Contains(guiPoint))
                    {
                        if (GridPaintingState.activeGrid != this)
                        {
                            OnMouseEnter(sceneView);
                        }
                    }
                    else if (activeSceneView == sceneView)
                    {
                        if (GridPaintingState.activeGrid == this)
                        {
                            OnMouseLeave(sceneView);
                        }
                    }
                }
            }
        }

        private void OnMouseEnter(SceneView sceneView)
        {
            if (GridPaintingState.activeBrushEditor != null)
                GridPaintingState.activeBrushEditor.OnMouseEnter();
            GridPaintingState.activeGrid = this;
            activeSceneView = sceneView;
        }

        private void OnMouseLeave(SceneView sceneView)
        {
            if (GridPaintingState.activeBrushEditor != null)
                GridPaintingState.activeBrushEditor.OnMouseLeave();
            GridPaintingState.activeGrid = null;
            activeSceneView = null;
        }

        private void UndoRedoPerformed()
        {
            RefreshAllTiles();
        }

        private void RefreshAllTiles()
        {
            if (tilemap != null)
                tilemap.RefreshAllTiles();
        }

        protected override void RegisterUndo()
        {
            if (GridPaintingState.activeBrushEditor != null)
            {
                GridPaintingState.activeBrushEditor.RegisterUndo(brushTarget, EditModeToBrushTool(EditMode.editMode));
            }
        }

        protected override void Paint(Vector3Int position)
        {
            if (grid != null)
                gridBrush.Paint(grid, brushTarget, position);
        }

        protected override void Erase(Vector3Int position)
        {
            if (grid != null)
                gridBrush.Erase(grid, brushTarget, position);
        }

        protected override void BoxFill(BoundsInt position)
        {
            if (grid != null)
                gridBrush.BoxFill(grid, brushTarget, position);
        }

        protected override void BoxErase(BoundsInt position)
        {
            if (grid != null)
                gridBrush.BoxErase(grid, brushTarget, position);
        }

        protected override void FloodFill(Vector3Int position)
        {
            if (grid != null)
                gridBrush.FloodFill(grid, brushTarget, position);
        }

        protected override void PickBrush(BoundsInt position, Vector3Int pickStart)
        {
            if (grid != null)
                gridBrush.Pick(grid, brushTarget, position, pickStart);
        }

        protected override void Select(BoundsInt position)
        {
            if (grid != null)
            {
                GridSelection.Select(brushTarget, position);
                gridBrush.Select(grid, brushTarget, position);
            }
        }

        protected override void Move(BoundsInt from, BoundsInt to)
        {
            if (grid != null)
                gridBrush.Move(grid, brushTarget, from, to);
        }

        protected override void MoveStart(BoundsInt position)
        {
            if (grid != null)
                gridBrush.MoveStart(grid, brushTarget, position);
        }

        protected override void MoveEnd(BoundsInt position)
        {
            if (grid != null)
                gridBrush.MoveEnd(grid, brushTarget, position);
        }

        protected override void ClearGridSelection()
        {
            GridSelection.Clear();
        }

        public override void Repaint()
        {
            SceneView.RepaintAll();
        }

        protected override bool ValidateFloodFillPosition(Vector3Int position)
        {
            return true;
        }

        protected override Vector2Int ScreenToGrid(Vector2 screenPosition)
        {
            if (tilemap != null)
            {
                var transform = tilemap.transform;
                Vector3 forward = tilemap.orientationMatrix.MultiplyVector(transform.forward) * -1f;
                Plane plane = new Plane(forward, transform.position);
                Vector3Int cell = LocalToGrid(tilemap, GridEditorUtility.ScreenToLocal(transform, screenPosition, plane));
                return new Vector2Int(cell.x, cell.y);
            }
            if (grid)
            {
                Vector3Int cell = LocalToGrid(grid, GridEditorUtility.ScreenToLocal(gridTransform, screenPosition, GetGridPlane(grid)));
                return new Vector2Int(cell.x, cell.y);
            }
            return Vector2Int.zero;
        }

        protected override bool PickingIsDefaultTool()
        {
            return false;
        }

        protected override bool CanPickOutsideEditMode()
        {
            return false;
        }

        protected override Grid.CellLayout CellLayout()
        {
            return grid.cellLayout;
        }

        Vector3Int LocalToGrid(GridLayout gridLayout, Vector3 local)
        {
            return gridLayout.LocalToCell(local);
        }

        private Plane GetGridPlane(Grid grid)
        {
            switch (grid.cellSwizzle)
            {
                case Grid.CellSwizzle.XYZ:
                    return new Plane(grid.transform.forward * -1f, grid.transform.position);
                case Grid.CellSwizzle.XZY:
                    return new Plane(grid.transform.up * -1f, grid.transform.position);
                case Grid.CellSwizzle.YXZ:
                    return new Plane(grid.transform.forward, grid.transform.position);
                case Grid.CellSwizzle.YZX:
                    return new Plane(grid.transform.up, grid.transform.position);
                case Grid.CellSwizzle.ZXY:
                    return new Plane(grid.transform.right, grid.transform.position);
                case Grid.CellSwizzle.ZYX:
                    return new Plane(grid.transform.right * -1f, grid.transform.position);
            }
            return new Plane(grid.transform.forward * -1f, grid.transform.position);
        }

        void CallOnPaintSceneGUI()
        {
            bool hasSelection = GridSelection.active && GridSelection.target == brushTarget;
            if (!hasSelection && GridPaintingState.activeGrid != this)
                return;

            RectInt rect = new RectInt(mouseGridPosition, new Vector2Int(1, 1));

            if (m_MarqueeStart.HasValue)
                rect = GridEditorUtility.GetMarqueeRect(mouseGridPosition, m_MarqueeStart.Value);
            else if (hasSelection)
                rect = new RectInt(GridSelection.position.xMin, GridSelection.position.yMin, GridSelection.position.size.x, GridSelection.position.size.y);

            var layoutGrid = tilemap != null ? tilemap as GridLayout : grid as GridLayout;

            if (GridPaintingState.activeBrushEditor != null)
            {
                GridPaintingState.activeBrushEditor.OnPaintSceneGUI(layoutGrid, brushTarget,
                    new BoundsInt(new Vector3Int(rect.x, rect.y, 0), new Vector3Int(rect.width, rect.height, 1)),
                    EditModeToBrushTool(EditMode.editMode), m_MarqueeStart.HasValue || executing);
            }
            else // Fallback when user hasn't defined custom editor
            {
                GridBrushEditorBase.OnPaintSceneGUIInternal(layoutGrid, brushTarget, new BoundsInt(new Vector3Int(rect.x, rect.y, 0), new Vector3Int(rect.width, rect.height, 1)), EditModeToBrushTool(EditMode.editMode), m_MarqueeStart.HasValue || executing);
            }
        }

        void CallOnSceneGUI()
        {
            if (GridPaintingState.activeBrushEditor != null)
            {
                MethodInfo methodInfo = GridPaintingState.activeBrushEditor.GetType().GetMethod("OnSceneGUI");
                if (methodInfo != null)
                    methodInfo.Invoke(GridPaintingState.activeBrushEditor, null);
            }
        }
    }
}
