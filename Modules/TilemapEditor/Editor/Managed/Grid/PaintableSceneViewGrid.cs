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
        private int sceneViewTransformHash;

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

        private Rect GetSceneViewPositionRect(SceneView sceneView)
        {
            return new Rect(0, 0, sceneView.position.width, sceneView.position.height);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            HandleMouseEnterLeave(sceneView);

            // Case 1077400: SceneView camera transform changes may update the mouse grid position even though the mouse position has not changed
            var currentSceneViewTransformHash = sceneView.camera.transform.localToWorldMatrix.GetHashCode();
            UpdateMouseGridPosition(currentSceneViewTransformHash == sceneViewTransformHash);
            sceneViewTransformHash = currentSceneViewTransformHash;

            var dot = 1.0f;
            var gridView = GetGridView();
            if (gridView != null)
            {
                dot = Math.Abs(Vector3.Dot(sceneView.camera.transform.forward, Grid.Swizzle(gridView.cellSwizzle, gridView.transform.forward)));
            }

            // Case 1021655: Validate that grid is not totally parallel to view (+-5 degrees), otherwise tiles could be accidentally painted on large positions
            if (dot > 0.1f)
            {
                base.OnGUI();
                if (InGridEditMode())
                {
                    CallOnSceneGUI();
                    if ((grid != null) && (GridPaintingState.activeGrid == this || GridSelection.active))
                    {
                        CallOnPaintSceneGUI();
                    }
                    if (Event.current.type == EventType.Repaint)
                        EditorGUIUtility.AddCursorRect(GetSceneViewPositionRect(sceneView), MouseCursor.CustomCursor);
                }
            }
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
                // Case 1043365: When docked, the docking area is considered part of the window and MouseEnter/LeaveWindow events are not considered when entering the docking area
                else if (sceneView.docked)
                {
                    var guiPoint = Event.current.mousePosition;
                    var sceneViewPosition = GetSceneViewPositionRect(sceneView);
                    if (sceneViewPosition.Contains(guiPoint))
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
            ResetPreviousMousePositionToCurrentPosition();
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

        protected override void OnEditStart()
        {
            if (GridPaintingState.activeBrushEditor != null && grid != null)
                GridPaintingState.activeBrushEditor.OnEditStart(grid, brushTarget);
        }

        protected override void OnEditEnd()
        {
            if (GridPaintingState.activeBrushEditor != null && grid != null)
                GridPaintingState.activeBrushEditor.OnEditEnd(grid, brushTarget);
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

        private GridLayout GetGridView()
        {
            if (tilemap != null)
                return tilemap;
            if (grid != null)
                return grid;
            return null;
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
            BoundsInt brushBounds = new BoundsInt(new Vector3Int(rect.x, rect.y, zPosition), new Vector3Int(rect.width, rect.height, 1));
            if (GridPaintingState.activeBrushEditor != null)
            {
                GridPaintingState.activeBrushEditor.OnPaintSceneGUI(layoutGrid, brushTarget, brushBounds
                    , EditModeToBrushTool(EditMode.editMode), m_MarqueeStart.HasValue || executing);
            }
            else // Fallback when user hasn't defined custom editor
            {
                GridBrushEditorBase.OnPaintSceneGUIInternal(layoutGrid, brushTarget, brushBounds
                    , EditModeToBrushTool(EditMode.editMode), m_MarqueeStart.HasValue || executing);
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
