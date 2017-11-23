// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal abstract class PaintableGrid : ScriptableObject
    {
        public enum MarqueeType { None = 0, Pick, Box, Select }
        private int m_PermanentControlID;

        public abstract void Repaint();
        protected abstract void RegisterUndo();
        protected abstract void Paint(Vector3Int position);
        protected abstract void Erase(Vector3Int position);
        protected abstract void BoxFill(BoundsInt position);
        protected abstract void BoxErase(BoundsInt position);
        protected abstract void FloodFill(Vector3Int position);
        protected abstract void PickBrush(BoundsInt position, Vector3Int pickStart);
        protected abstract void Select(BoundsInt position);
        protected abstract void Move(BoundsInt from, BoundsInt to);
        protected abstract void MoveStart(BoundsInt position);
        protected abstract void MoveEnd(BoundsInt position);
        protected abstract bool ValidateFloodFillPosition(Vector3Int position);
        protected abstract Vector2Int ScreenToGrid(Vector2 screenPosition);
        protected abstract bool PickingIsDefaultTool();
        protected abstract bool CanPickOutsideEditMode();
        protected abstract Grid.CellLayout CellLayout();
        protected abstract void ClearGridSelection();

        protected virtual void OnBrushPickStarted() {}
        protected virtual void OnBrushPickDragged(BoundsInt position) {}

        internal static PaintableGrid s_LastActivePaintableGrid;

        private Vector2Int m_PreviousMouseGridPosition;
        private Vector2Int m_MouseGridPosition;
        private bool m_MouseGridPositionChanged;
        private bool m_PositionChangeRepaintDone;
        protected Vector2Int? m_PreviousMove = null;
        protected Vector2Int? m_MarqueeStart = null;
        private MarqueeType m_MarqueeType = MarqueeType.None;
        private bool m_IsExecuting;
        private EditMode.SceneViewEditMode m_ModeBeforePicking;

        public Vector2Int mouseGridPosition { get { return m_MouseGridPosition; } }
        public bool isPicking { get { return m_MarqueeType == MarqueeType.Pick; } }
        public bool isBoxing { get { return m_MarqueeType == MarqueeType.Box; } }
        public Grid.CellLayout cellLayout { get { return CellLayout(); } }

        protected bool executing { get { return m_IsExecuting; } set { m_IsExecuting = value && isHotControl; } }

        protected bool isHotControl { get { return GUIUtility.hotControl == m_PermanentControlID; } }
        protected bool mouseGridPositionChanged { get { return m_MouseGridPositionChanged; } }
        protected bool inEditMode { get { return PaintableGrid.InGridEditMode(); } }

        protected virtual void OnEnable()
        {
            m_PermanentControlID = GUIUtility.GetPermanentControlID();
        }

        protected virtual void OnDisable()
        {
        }

        public virtual void OnGUI()
        {
            if (CanPickOutsideEditMode() || inEditMode)
                HandleBrushPicking();

            if (inEditMode)
            {
                HandleBrushPaintAndErase();
                HandleSelectTool();
                HandleMoveTool();
                HandleEditModeChange();
                HandleFloodFill();
                HandleBoxTool();
            }
            else if (isHotControl && !IsPickingEvent(Event.current))
            {
                // Release hot control if it still has it while not in picking or grid edit mode
                GUIUtility.hotControl = 0;
            }

            if (mouseGridPositionChanged && !m_PositionChangeRepaintDone)
            {
                Repaint();
                m_PositionChangeRepaintDone = true;
            }
        }

        protected void UpdateMouseGridPosition()
        {
            if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove || Event.current.type == EventType.DragUpdated)
            {
                m_MouseGridPositionChanged = false;

                Vector2Int newGridPosition = ScreenToGrid(Event.current.mousePosition);
                if (newGridPosition != m_MouseGridPosition)
                {
                    m_PreviousMouseGridPosition = m_MouseGridPosition;
                    m_MouseGridPosition = newGridPosition;
                    m_MouseGridPositionChanged = true;
                    m_PositionChangeRepaintDone = false;
                }
            }
        }

        private void HandleEditModeChange()
        {
            // Handles changes in EditMode while tool is expected to be in the same mode
            if (isPicking && EditMode.editMode != EditMode.SceneViewEditMode.GridPicking)
            {
                m_MarqueeStart = null;
                m_MarqueeType = MarqueeType.None;
                if (isHotControl)
                {
                    GUI.changed = true;
                    GUIUtility.hotControl = 0;
                }
            }
            if (isBoxing && EditMode.editMode != EditMode.SceneViewEditMode.GridBox)
            {
                m_MarqueeStart = null;
                m_MarqueeType = MarqueeType.None;
                if (isHotControl)
                {
                    GUI.changed = true;
                    GUIUtility.hotControl = 0;
                }
            }
            if (EditMode.editMode != EditMode.SceneViewEditMode.GridSelect &&
                EditMode.editMode != EditMode.SceneViewEditMode.GridMove)
            {
                ClearGridSelection();
            }
        }

        private void HandleBrushPicking()
        {
            Event evt = Event.current;

            if (evt.type == EventType.MouseDown && IsPickingEvent(evt) && !isHotControl)
            {
                m_ModeBeforePicking = EditMode.SceneViewEditMode.GridPainting;
                if (inEditMode && EditMode.editMode != EditMode.SceneViewEditMode.GridPicking)
                {
                    m_ModeBeforePicking = EditMode.editMode;
                    EditMode.ChangeEditMode(EditMode.SceneViewEditMode.GridPicking, GridPaintingState.instance);
                }

                m_MarqueeStart = mouseGridPosition;
                m_MarqueeType = MarqueeType.Pick;
                s_LastActivePaintableGrid = this;
                Event.current.Use();
                GUI.changed = true;
                GUIUtility.hotControl = m_PermanentControlID;
                OnBrushPickStarted();
            }
            if (evt.type == EventType.MouseDrag && isHotControl && m_MarqueeStart.HasValue && m_MarqueeType == MarqueeType.Pick && IsPickingEvent(evt))
            {
                RectInt rect = GridEditorUtility.GetMarqueeRect(m_MarqueeStart.Value, mouseGridPosition);
                OnBrushPickDragged(new BoundsInt(new Vector3Int(rect.xMin, rect.yMin, 0), new Vector3Int(rect.size.x, rect.size.y, 1)));
                Event.current.Use();
                GUI.changed = true;
            }
            if (evt.type == EventType.MouseUp && m_MarqueeStart.HasValue && m_MarqueeType == MarqueeType.Pick && IsPickingEvent(evt))
            {
                RectInt rect = GridEditorUtility.GetMarqueeRect(m_MarqueeStart.Value, mouseGridPosition);
                if (isHotControl)
                {
                    Vector2Int pivot = GetMarqueePivot(m_MarqueeStart.Value, mouseGridPosition);
                    PickBrush(new BoundsInt(new Vector3Int(rect.xMin, rect.yMin, 0), new Vector3Int(rect.size.x, rect.size.y, 1)), new Vector3Int(pivot.x, pivot.y, 0));

                    if (inEditMode && EditMode.editMode != m_ModeBeforePicking)
                    {
                        EditMode.ChangeEditMode(m_ModeBeforePicking, GridPaintingState.instance);
                    }

                    GridPaletteBrushes.ActiveGridBrushAssetChanged();
                    s_LastActivePaintableGrid = this;
                    InspectorWindow.RepaintAllInspectors();
                    Event.current.Use();
                    GUI.changed = true;
                    GUIUtility.hotControl = 0;
                }
                m_MarqueeType = MarqueeType.None;
                m_MarqueeStart = null;
            }
        }

        private bool IsPickingEvent(Event evt)
        {
            return ((evt.control && EditMode.editMode != EditMode.SceneViewEditMode.GridMove) ||
                    EditMode.editMode == EditMode.SceneViewEditMode.GridPicking ||
                    EditMode.editMode != EditMode.SceneViewEditMode.GridSelect && PickingIsDefaultTool()) &&
                evt.button == 0 && !evt.alt;
        }

        private void HandleSelectTool()
        {
            Event evt = Event.current;

            if (evt.type == EventType.MouseDown && evt.button == 0 && !evt.alt
                && (EditMode.editMode == EditMode.SceneViewEditMode.GridSelect || (EditMode.editMode == EditMode.SceneViewEditMode.GridMove && evt.control)))
            {
                if (EditMode.editMode == EditMode.SceneViewEditMode.GridMove && evt.control)
                    EditMode.ChangeEditMode(EditMode.SceneViewEditMode.GridSelect, GridPaintingState.instance);

                m_PreviousMove = null;
                m_MarqueeStart = mouseGridPosition;
                m_MarqueeType = MarqueeType.Select;

                s_LastActivePaintableGrid = this;
                GUIUtility.hotControl = m_PermanentControlID;
                Event.current.Use();
            }
            if (evt.type == EventType.MouseUp && evt.button == 0 && !evt.alt && m_MarqueeStart.HasValue && GUIUtility.hotControl == m_PermanentControlID && EditMode.editMode == EditMode.SceneViewEditMode.GridSelect)
            {
                if (m_MarqueeStart.HasValue && m_MarqueeType == MarqueeType.Select)
                {
                    RectInt rect = GridEditorUtility.GetMarqueeRect(m_MarqueeStart.Value, mouseGridPosition);
                    Select(new BoundsInt(new Vector3Int(rect.xMin, rect.yMin, 0), new Vector3Int(rect.size.x, rect.size.y, 1)));
                    m_MarqueeStart = null;
                    m_MarqueeType = MarqueeType.None;
                    InspectorWindow.RepaintAllInspectors();
                }
                if (evt.control)
                    EditMode.ChangeEditMode(EditMode.SceneViewEditMode.GridMove, GridPaintingState.instance);

                GUIUtility.hotControl = 0;
                Event.current.Use();
            }
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape && !m_MarqueeStart.HasValue && !m_PreviousMove.HasValue)
            {
                ClearGridSelection();
                Event.current.Use();
            }
        }

        private void HandleMoveTool()
        {
            Event evt = Event.current;

            if (evt.type == EventType.MouseDown && evt.button == 0 && EditMode.editMode == EditMode.SceneViewEditMode.GridMove)
            {
                RegisterUndo();
                Vector3Int mouse3D = new Vector3Int(mouseGridPosition.x, mouseGridPosition.y, GridSelection.position.zMin);
                if (GridSelection.active && GridSelection.position.Contains(mouse3D))
                {
                    GUIUtility.hotControl = m_PermanentControlID;
                    executing = true;
                    m_MarqueeStart = null;
                    m_MarqueeType = MarqueeType.None;
                    m_PreviousMove = mouseGridPosition;
                    MoveStart(GridSelection.position);
                    s_LastActivePaintableGrid = this;
                }
                Event.current.Use();
            }
            if (evt.type == EventType.MouseDrag && evt.button == 0 && EditMode.editMode == EditMode.SceneViewEditMode.GridMove && GUIUtility.hotControl == m_PermanentControlID)
            {
                if (m_MouseGridPositionChanged && m_PreviousMove.HasValue)
                {
                    executing = true;
                    BoundsInt previousRect = GridSelection.position;
                    BoundsInt previousBounds = new BoundsInt(new Vector3Int(previousRect.xMin, previousRect.yMin, 0), new Vector3Int(previousRect.size.x, previousRect.size.y, 1));

                    Vector2Int direction = mouseGridPosition - m_PreviousMove.Value;
                    BoundsInt pos = GridSelection.position;
                    pos.position = new Vector3Int(pos.x + direction.x, pos.y + direction.y, pos.z);
                    GridSelection.position = pos;
                    Move(previousBounds, pos);
                    m_PreviousMove = mouseGridPosition;
                    Event.current.Use();
                }
            }
            if (evt.type == EventType.MouseUp && evt.button == 0 && m_PreviousMove.HasValue && EditMode.editMode == EditMode.SceneViewEditMode.GridMove && GUIUtility.hotControl == m_PermanentControlID)
            {
                if (m_PreviousMove.HasValue)
                {
                    m_PreviousMove = null;
                    MoveEnd(GridSelection.position);
                }
                executing = false;
                GUIUtility.hotControl = 0;
                Event.current.Use();
            }
        }

        private void HandleBrushPaintAndErase()
        {
            Event evt = Event.current;
            if (!IsPaintingEvent(evt) && !IsErasingEvent(evt))
                return;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    RegisterUndo();
                    if (IsErasingEvent(evt))
                    {
                        if (EditMode.editMode != EditMode.SceneViewEditMode.GridEraser)
                            EditMode.ChangeEditMode(EditMode.SceneViewEditMode.GridEraser, GridPaintingState.instance);
                        Erase(new Vector3Int(mouseGridPosition.x, mouseGridPosition.y, 0));
                    }
                    else
                    {
                        if (EditMode.editMode != EditMode.SceneViewEditMode.GridPainting)
                            EditMode.ChangeEditMode(EditMode.SceneViewEditMode.GridPainting, GridPaintingState.instance);
                        Paint(new Vector3Int(mouseGridPosition.x, mouseGridPosition.y, 0));
                    }

                    Event.current.Use();
                    GUIUtility.hotControl = m_PermanentControlID;
                    GUI.changed = true;
                    executing = true;
                    break;
                case EventType.MouseDrag:
                    if (isHotControl && mouseGridPositionChanged)
                    {
                        List<Vector2Int> points = GridEditorUtility.GetPointsOnLine(m_PreviousMouseGridPosition, mouseGridPosition).ToList();
                        if (points[0] == mouseGridPosition)
                            points.Reverse();

                        for (int i = 1; i < points.Count; i++)
                        {
                            if (IsErasingEvent(evt))
                                Erase(new Vector3Int(points[i].x, points[i].y, 0));
                            else
                                Paint(new Vector3Int(points[i].x, points[i].y, 0));
                        }
                        Event.current.Use();
                        GUI.changed = true;
                    }
                    executing = true;
                    break;
                case EventType.MouseUp:
                    executing = false;
                    if (isHotControl)
                    {
                        if (Event.current.shift && EditMode.editMode != EditMode.SceneViewEditMode.GridPainting)
                            EditMode.ChangeEditMode(EditMode.SceneViewEditMode.GridPainting, GridPaintingState.instance);

                        Event.current.Use();
                        GUI.changed = true;
                        GUIUtility.hotControl = 0;
                    }
                    break;
            }
        }

        private bool IsPaintingEvent(Event evt)
        {
            return (evt.button == 0 && !evt.control && !evt.alt && EditMode.editMode == EditMode.SceneViewEditMode.GridPainting);
        }

        private bool IsErasingEvent(Event evt)
        {
            return (evt.button == 0 && (!evt.control && !evt.alt
                                        && (evt.shift && EditMode.editMode != EditMode.SceneViewEditMode.GridBox
                                            && EditMode.editMode != EditMode.SceneViewEditMode.GridFloodFill
                                            && EditMode.editMode != EditMode.SceneViewEditMode.GridSelect
                                            && EditMode.editMode != EditMode.SceneViewEditMode.GridMove)
                                        || EditMode.editMode == EditMode.SceneViewEditMode.GridEraser));
        }

        private void HandleFloodFill()
        {
            if (EditMode.editMode == EditMode.SceneViewEditMode.GridFloodFill && GridPaintingState.gridBrush != null && ValidateFloodFillPosition(new Vector3Int(mouseGridPosition.x, mouseGridPosition.y, 0)))
            {
                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && evt.button == 0)
                {
                    GUIUtility.hotControl = m_PermanentControlID;
                    GUI.changed = true;
                    executing = true;
                    Event.current.Use();
                }
                if (evt.type == EventType.MouseUp && evt.button == 0 && isHotControl)
                {
                    executing = false;
                    RegisterUndo();
                    FloodFill(new Vector3Int(mouseGridPosition.x, mouseGridPosition.y, 0));
                    GUI.changed = true;
                    Event.current.Use();
                    GUIUtility.hotControl = 0;
                }
            }
        }

        private void HandleBoxTool()
        {
            Event evt = Event.current;

            if (evt.type == EventType.MouseDown && evt.button == 0 && EditMode.editMode == EditMode.SceneViewEditMode.GridBox)
            {
                m_MarqueeStart = mouseGridPosition;
                m_MarqueeType = MarqueeType.Box;
                Event.current.Use();
                GUI.changed = true;
                executing = true;
                GUIUtility.hotControl = m_PermanentControlID;
            }
            if (evt.type == EventType.MouseDrag && evt.button == 0 && EditMode.editMode == EditMode.SceneViewEditMode.GridBox)
            {
                if (isHotControl && m_MarqueeStart.HasValue)
                {
                    Event.current.Use();
                    executing = true;
                    GUI.changed = true;
                }
            }
            if (evt.type == EventType.MouseUp && evt.button == 0 && EditMode.editMode == EditMode.SceneViewEditMode.GridBox)
            {
                if (isHotControl && m_MarqueeStart.HasValue)
                {
                    RegisterUndo();
                    RectInt rect = GridEditorUtility.GetMarqueeRect(m_MarqueeStart.Value, mouseGridPosition);
                    if (evt.shift)
                        BoxErase(new BoundsInt(rect.x, rect.y, 0, rect.size.x, rect.size.y, 1));
                    else
                        BoxFill(new BoundsInt(rect.x, rect.y, 0, rect.size.x, rect.size.y, 1));
                    Event.current.Use();
                    executing = false;
                    GUI.changed = true;
                    GUIUtility.hotControl = 0;
                }
                m_MarqueeStart = null;
                m_MarqueeType = MarqueeType.None;
            }
        }

        private Vector2Int GetMarqueePivot(Vector2Int start, Vector2Int end)
        {
            Vector2Int pivot = new Vector2Int(
                    Math.Max(end.x - start.x, 0),
                    Math.Max(end.y - start.y, 0)
                    );
            return pivot;
        }

        public static bool InGridEditMode()
        {
            return
                EditMode.editMode == EditMode.SceneViewEditMode.GridBox ||
                EditMode.editMode == EditMode.SceneViewEditMode.GridEraser ||
                EditMode.editMode == EditMode.SceneViewEditMode.GridFloodFill ||
                EditMode.editMode == EditMode.SceneViewEditMode.GridPainting ||
                EditMode.editMode == EditMode.SceneViewEditMode.GridPicking ||
                EditMode.editMode == EditMode.SceneViewEditMode.GridSelect ||
                EditMode.editMode == EditMode.SceneViewEditMode.GridMove;
        }

        // TODO: Someday EditMode or its future incarnation will be public and we can get rid of this
        public static GridBrushBase.Tool EditModeToBrushTool(EditMode.SceneViewEditMode editMode)
        {
            switch (editMode)
            {
                case EditMode.SceneViewEditMode.GridBox:
                    return GridBrushBase.Tool.Box;
                case EditMode.SceneViewEditMode.GridEraser:
                    return GridBrushBase.Tool.Erase;
                case EditMode.SceneViewEditMode.GridFloodFill:
                    return GridBrushBase.Tool.FloodFill;
                case EditMode.SceneViewEditMode.GridPainting:
                    return GridBrushBase.Tool.Paint;
                case EditMode.SceneViewEditMode.GridPicking:
                    return GridBrushBase.Tool.Pick;
                case EditMode.SceneViewEditMode.GridSelect:
                    return GridBrushBase.Tool.Select;
                case EditMode.SceneViewEditMode.GridMove:
                    return GridBrushBase.Tool.Move;
            }
            return GridBrushBase.Tool.Paint;
        }

        public static EditMode.SceneViewEditMode BrushToolToEditMode(GridBrushBase.Tool tool)
        {
            switch (tool)
            {
                case GridBrushBase.Tool.Box:
                    return EditMode.SceneViewEditMode.GridBox;
                case GridBrushBase.Tool.Erase:
                    return EditMode.SceneViewEditMode.GridEraser;
                case GridBrushBase.Tool.FloodFill:
                    return EditMode.SceneViewEditMode.GridFloodFill;
                case GridBrushBase.Tool.Paint:
                    return EditMode.SceneViewEditMode.GridPainting;
                case GridBrushBase.Tool.Pick:
                    return EditMode.SceneViewEditMode.GridPicking;
                case GridBrushBase.Tool.Select:
                    return EditMode.SceneViewEditMode.GridSelect;
                case GridBrushBase.Tool.Move:
                    return EditMode.SceneViewEditMode.GridMove;
            }
            return EditMode.SceneViewEditMode.GridPainting;
        }
    }
}
