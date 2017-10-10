// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Interface;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;
using UnityTexture2D = UnityEngine.Texture2D;

namespace UnityEditor
{
    internal interface IShapeEditorFactory
    {
        ShapeEditor CreateShapeEditor();
    }

    internal class ShapeEditorFactory : IShapeEditorFactory
    {
        public ShapeEditor CreateShapeEditor()
        {
            return new ShapeEditor(new GUIUtilitySystem(), new EventSystem());
        }
    }

    internal class ShapeEditor
    {
        public delegate float DistanceToControl(Vector3 pos, Quaternion rotation, float handleSize);

        internal enum SelectionType { Normal, Additive, Subtractive }
        internal enum Tool { Edit, Create, Break }
        internal enum TangentMode { Linear, Continuous, Broken }

        // --- To use this class in your editor, you need to implement these: (see ShapeEditorTests for example)
        // --- Data
        public Func<int, Vector3> GetPointPosition = i => Vector3.zero;
        public Action<int, Vector3> SetPointPosition = (i, p) => {};
        public Func<int, Vector3> GetPointLTangent = i => Vector3.zero;
        public Action<int, Vector3> SetPointLTangent = (i, p) => {};
        public Func<int, Vector3> GetPointRTangent = i => Vector3.zero;
        public Action<int, Vector3> SetPointRTangent = (i, p) => {};
        public Func<int, TangentMode> GetTangentMode = i => TangentMode.Linear;
        public Action<int, TangentMode> SetTangentMode = (i, m) => {};
        public Action<int, Vector3> InsertPointAt = (i, p) => {};
        public Action<int> RemovePointAt = i => {};
        public Func<int> GetPointsCount = () => 0;
        // --- Transforms
        public Func<Vector2, Vector3> ScreenToLocal = i => i;
        public Func<Vector3, Vector2> LocalToScreen = i => i;
        public Func<Matrix4x4> LocalToWorldMatrix = () => Matrix4x4.identity;
        // --- Distance functions
        public Func<DistanceToControl> DistanceToRectangle = () => HandleUtility.DistanceToRectangle;
        public Func<DistanceToControl> DistanceToDiamond = () => HandleUtility.DistanceToDiamond;
        public Func<DistanceToControl> DistanceToCircle = () => DistanceToCircleInternal;
        // --- Other
        public Action Repaint = () => {};
        public Action RecordUndo = () => {};
        public Func<Vector3, Vector3> Snap = i => i;
        public Action<Bounds> Frame = b => {};
        public Action<int> OnPointClick = i => {};
        public Func<bool> OpenEnded = () => false;
        public Func<float> GetHandleSize = () => 5f;
        // --- END

        public ITexture2D lineTexture { get; set; }
        public int activePoint { get; set; }
        public HashSet<int> selectedPoints { get { return m_Selection.indices; } }
        public bool inEditMode { get; set; }
        public int activeEdge { get { return m_ActiveEdge; } set { m_ActiveEdge = value; } }
        // Shape editor needs to reset its state on next OnSceneGUI. Reset can't be called immediately elsewhere, because we need to also reset local sceneview state like GUIUtility.keyboardControl
        public bool delayedReset { set { m_DelayedReset = value; } }

        private ShapeEditorSelection m_Selection;
        private Vector2 m_MousePositionLastMouseDown;
        private int m_ActivePointOnLastMouseDown = -1;
        private int m_NewPointIndex = -1;
        private Vector3 m_EdgeDragStartMousePosition;
        private Vector3 m_EdgeDragStartP0;
        private Vector3 m_EdgeDragStartP1;
        private bool m_NewPointDragFinished;
        private int m_ActiveEdge = -1;
        private bool m_DelayedReset = false;
        private HashSet<ShapeEditor> m_ShapeEditorListeners = new HashSet<ShapeEditor>();
        private ShapeEditorRectSelectionTool m_RectSelectionTool;

        private int m_MouseClosestEdge = -1;
        private float m_MouseClosestEdgeDist = float.MaxValue;
        private int m_ShapeEditorRegisteredTo = 0;
        private int m_ShapeEditorUpdateDone = 0;

        private static Color handleOutlineColor { get; set; }
        private static Color handleFillColor { get; set; }
        private Quaternion handleMatrixrotation { get { return Quaternion.LookRotation(handles.matrix.GetColumn(2), handles.matrix.GetColumn(1)); } }

        private IGUIUtility guiUtility { get; set; }
        private IEventSystem eventSystem { get; set; }
        private IEvent currentEvent { get; set; }
        private IGL glSystem { get; set; }
        private IHandles handles { get; set; }

        private Dictionary<DrawBatchDataKey, List<Vector3>> m_DrawBatch;
        private Vector3[][] m_EdgePoints;

        enum ColorEnum
        {
            EUnselected,
            EUnselectedHovered,
            ESelected,
            ESelectedHovered
        }

        private static readonly Color[] k_OutlineColor = new[] {
            Color.gray,
            Color.white,
            new Color(34f / 255f, 171f / 255f, 1f), // #22abff
            Color.white
        };

        static readonly Color[] k_FillColor = new[]
        {
            Color.white,
            new Color(131f / 255f, 220f / 255f, 1f), // #83dcff
            new Color(34f / 255f, 171f / 255f, 1f), // #22abff
            new Color(34f / 255f, 171f / 255f, 1f) // #22abff
        };
        private static readonly Color k_TangentColor = new Color(34f / 255f, 171f / 255f, 1f); // #22abff
        private static readonly Color k_TangentColorAlternative = new Color(131f / 255f, 220f / 255f, 1f); // #83dcff
        private const float k_EdgeHoverDistance = 9f;
        private const float k_EdgeWidth = 2f;
        private const float k_ActiveEdgeWidth = 6f;
        private const float k_MinExistingPointDistanceForInsert = 20f;
        private readonly int k_CreatorID;
        private readonly int k_EdgeID;
        private readonly int k_RightTangentID;
        private readonly int k_LeftTangentID;
        private const int k_BezierPatch = 40;

        class DrawBatchDataKey
        {
            public Color color { get; private set; }
            public int glMode { get; private set; }
            private int m_Hash;

            public DrawBatchDataKey(Color c , int mode)
            {
                color = c;
                glMode = mode;
                m_Hash = glMode ^ (color.GetHashCode() << 2);
            }

            public override int GetHashCode()
            {
                return m_Hash;
            }

            public override bool Equals(object obj)
            {
                return m_Hash == obj.GetHashCode();
            }
        }

        public ShapeEditor(IGUIUtility gu, IEventSystem es)
        {
            m_Selection = new ShapeEditorSelection(this);
            guiUtility = gu;
            eventSystem = es;
            k_CreatorID = guiUtility.GetPermanentControlID();
            k_EdgeID = guiUtility.GetPermanentControlID();
            k_RightTangentID = guiUtility.GetPermanentControlID();
            k_LeftTangentID = guiUtility.GetPermanentControlID();
            glSystem = GLSystem.GetSystem();
            handles = HandlesSystem.GetSystem();
        }

        public void SetRectSelectionTool(ShapeEditorRectSelectionTool sers)
        {
            if (m_RectSelectionTool != null)
            {
                m_RectSelectionTool.RectSelect -= SelectPointsInRect;
                m_RectSelectionTool.ClearSelection -= ClearSelectedPoints;
            }
            m_RectSelectionTool = sers;
            m_RectSelectionTool.RectSelect += SelectPointsInRect;
            m_RectSelectionTool.ClearSelection += ClearSelectedPoints;
        }

        public void OnDisable()
        {
            m_RectSelectionTool.RectSelect -= SelectPointsInRect;
            m_RectSelectionTool.ClearSelection -= ClearSelectedPoints;
            m_RectSelectionTool = null;
        }

        private void PrepareDrawBatch()
        {
            if (currentEvent.type == EventType.Repaint)
            {
                m_DrawBatch = new Dictionary<DrawBatchDataKey, List<Vector3>>();
            }
        }

        private void DrawBatch()
        {
            if (currentEvent.type == EventType.Repaint)
            {
                HandleUtility.ApplyWireMaterial();
                glSystem.PushMatrix();
                glSystem.MultMatrix(handles.matrix);
                foreach (var drawBatch in m_DrawBatch)
                {
                    glSystem.Begin(drawBatch.Key.glMode);
                    glSystem.Color(drawBatch.Key.color);
                    foreach (var t in drawBatch.Value)
                    {
                        glSystem.Vertex(t);
                    }
                    glSystem.End();
                }
                glSystem.PopMatrix();
            }
        }

        List<Vector3> GetDrawBatchList(DrawBatchDataKey key)
        {
            List<Vector3> data = null;
            if (!m_DrawBatch.ContainsKey(key))
            {
                m_DrawBatch.Add(key, new List<Vector3>());
            }
            data = m_DrawBatch[key];
            return data;
        }

        public void OnGUI()
        {
            DelayedResetIfNecessary();
            currentEvent = eventSystem.current;

            if (currentEvent.type == EventType.MouseDown)
                StoreMouseDownState();

            var oldColor = handles.color;
            var oldMatrix = handles.matrix;
            handles.matrix = LocalToWorldMatrix();

            PrepareDrawBatch();

            Edges();

            if (inEditMode)
            {
                Framing();
                Tangents();
                Points();
            }

            DrawBatch();

            handles.color = oldColor;
            handles.matrix = oldMatrix;

            OnShapeEditorUpdateDone();
            foreach (var se in m_ShapeEditorListeners)
                se.OnShapeEditorUpdateDone();
        }

        private void Framing()
        {
            if (currentEvent.commandName == "FrameSelected" && m_Selection.Count > 0)
            {
                switch (currentEvent.type)
                {
                    case EventType.ExecuteCommand:
                        Bounds bounds = new Bounds(GetPointPosition(selectedPoints.First()), Vector3.zero);

                        foreach (var index in selectedPoints)
                            bounds.Encapsulate(GetPointPosition(index));

                        Frame(bounds);
                        goto case EventType.ValidateCommand;
                    case EventType.ValidateCommand:
                        currentEvent.Use();
                        break;
                }
            }
        }

        void PrepareEdgePointList()
        {
            if (m_EdgePoints == null)
            {
                var total = this.GetPointsCount();
                int loopCount = OpenEnded() ? total - 1 : total;
                m_EdgePoints = new Vector3[loopCount][];
                int index = mod(total - 1, loopCount);
                for (int loop = mod(index + 1, total); loop < total; index = loop, ++loop)
                {
                    var position0 = this.GetPointPosition(index);
                    var position1 = this.GetPointPosition(loop);
                    if (GetTangentMode(index)  == TangentMode.Linear && GetTangentMode(loop) == TangentMode.Linear)
                    {
                        m_EdgePoints[index] = new[] { position0, position1 };
                    }
                    else
                    {
                        var tangent0 = GetPointRTangent(index) + position0;
                        var tangent1 = GetPointLTangent(loop) + position1;
                        m_EdgePoints[index] = handles.MakeBezierPoints(position0, position1, tangent0, tangent1, k_BezierPatch);
                    }
                }
            }
        }

        float DistancePointEdge(Vector3 point, Vector3[] edge)
        {
            float result = float.MaxValue;
            int index = edge.Length - 1;
            for (int nextIndex = 0; nextIndex < edge.Length; index = nextIndex, nextIndex++)
            {
                float dist = HandleUtility.DistancePointLine(point, edge[index], edge[nextIndex]);
                if (dist < result)
                    result = dist;
            }
            return result;
        }

        private float GetMouseClosestEdgeDistance()
        {
            var mousePosition = ScreenToLocal(eventSystem.current.mousePosition);
            var total = this.GetPointsCount();
            if (m_MouseClosestEdge == -1 && total > 0)
            {
                PrepareEdgePointList();
                m_MouseClosestEdgeDist = float.MaxValue;

                int loopCount = OpenEnded() ? total - 1 : total;
                for (int loop = 0; loop < loopCount; loop++)
                {
                    var dist = DistancePointEdge(mousePosition, m_EdgePoints[loop]);
                    if (dist < m_MouseClosestEdgeDist)
                    {
                        m_MouseClosestEdge = loop;
                        m_MouseClosestEdgeDist = dist;
                    }
                }
            }

            if (guiUtility.hotControl == k_CreatorID || guiUtility.hotControl == k_EdgeID)
                return float.MinValue;
            return m_MouseClosestEdgeDist;
        }

        public void Edges()
        {
            var otherClosestEdgeDistance = float.MaxValue;
            if (m_ShapeEditorListeners.Count > 0)
                otherClosestEdgeDistance = (from se in m_ShapeEditorListeners select se.GetMouseClosestEdgeDistance()).Max();
            float edgeDistance = GetMouseClosestEdgeDistance();
            bool closestEdgeHighlight = EdgeDragModifiersActive() && edgeDistance < k_EdgeHoverDistance && edgeDistance < otherClosestEdgeDistance;
            if (currentEvent.type == EventType.Repaint)
            {
                Color handlesOldColor = handles.color;
                PrepareEdgePointList();

                var total = this.GetPointsCount();
                int loopCount = OpenEnded() ? total - 1 : total;

                for (int loop = 0; loop < loopCount; loop++)
                {
                    Color edgeColor = loop == m_ActiveEdge ? Color.yellow : Color.white;
                    float edgeWidth = loop == m_ActiveEdge || (m_MouseClosestEdge == loop && closestEdgeHighlight) ? k_ActiveEdgeWidth : k_EdgeWidth;

                    handles.color = edgeColor;
                    handles.DrawAAPolyLine(lineTexture, edgeWidth, m_EdgePoints[loop]);
                }
                handles.color = handlesOldColor;
            }

            if (inEditMode)
            {
                if (otherClosestEdgeDistance > edgeDistance)
                {
                    var farEnoughFromExistingPoints = MouseDistanceToPoint(FindClosestPointToMouse()) > k_MinExistingPointDistanceForInsert;
                    var farEnoughtFromActiveTangents = MouseDistanceToClosestTangent() > k_MinExistingPointDistanceForInsert;
                    var farEnoughFromExisting = farEnoughFromExistingPoints && farEnoughtFromActiveTangents;
                    var hoveringEdge = m_MouseClosestEdgeDist < k_EdgeHoverDistance;
                    var handleEvent = hoveringEdge && farEnoughFromExisting && !m_RectSelectionTool.isSelecting;

                    if (GUIUtility.hotControl == k_EdgeID || EdgeDragModifiersActive() && handleEvent)
                        HandleEdgeDragging(m_MouseClosestEdge);
                    else if (GUIUtility.hotControl == k_CreatorID || (currentEvent.modifiers == EventModifiers.None && handleEvent))
                        HandlePointInsertToEdge(m_MouseClosestEdge, m_MouseClosestEdgeDist);
                }
            }

            if (guiUtility.hotControl != k_CreatorID && m_NewPointIndex != -1)
            {
                m_NewPointDragFinished = true;
                guiUtility.keyboardControl = 0;
                m_NewPointIndex = -1;
            }
            if (guiUtility.hotControl != k_EdgeID && m_ActiveEdge != -1)
            {
                m_ActiveEdge = -1;
            }
        }

        public void Tangents()
        {
            if (activePoint < 0 || m_Selection.Count > 1 || GetTangentMode(activePoint) == TangentMode.Linear)
                return;

            var evt = eventSystem.current;
            var p = this.GetPointPosition(activePoint);
            var lt = this.GetPointLTangent(activePoint);
            var rt = this.GetPointRTangent(activePoint);

            var isHot = (guiUtility.hotControl == k_RightTangentID || guiUtility.hotControl == k_LeftTangentID);
            var allZero = lt.sqrMagnitude == 0 && rt.sqrMagnitude == 0;

            if (isHot || !allZero)
            {
                var m = this.GetTangentMode(activePoint);
                var mouseDown = evt.GetTypeForControl(k_RightTangentID) == EventType.MouseDown || evt.GetTypeForControl(k_LeftTangentID) == EventType.MouseDown;
                var mouseUp = evt.GetTypeForControl(k_RightTangentID) == EventType.MouseUp || evt.GetTypeForControl(k_LeftTangentID) == EventType.MouseUp;

                var nlt = DoTangent(p, p + lt, k_LeftTangentID, activePoint, k_TangentColor);
                var nrt = DoTangent(p, p + rt, k_RightTangentID, activePoint, GetTangentMode(activePoint) == TangentMode.Broken ? k_TangentColorAlternative : k_TangentColor);

                var changed = (nlt != lt || nrt != rt);
                allZero = nlt.sqrMagnitude == 0 && nrt.sqrMagnitude == 0;

                // if indeed we are dragging one of the handles and we just shift+mousedown, toggle the point. this happens even without change!
                if (isHot && mouseDown)
                {
                    var nm = ((int)m + 1) % 3;
                    m = (TangentMode)nm;
                    //m = m == PointMode.Continuous ? PointMode.Broken : PointMode.Continuous;
                    this.SetTangentMode(activePoint, m);
                }

                // make it broken when both tangents are zero
                if (mouseUp && allZero)
                {
                    this.SetTangentMode(activePoint, TangentMode.Linear);
                    changed = true;
                }

                // only do something when it's changed
                if (changed)
                {
                    RecordUndo();
                    this.SetPointLTangent(activePoint, nlt);
                    this.SetPointRTangent(activePoint, nrt);
                    RefreshTangents(activePoint, guiUtility.hotControl == k_RightTangentID);
                    Repaint();
                }
            }
        }

        public void Points()
        {
            var wantsDelete =
                (UnityEngine.Event.current.type == EventType.ExecuteCommand || UnityEngine.Event.current.type == EventType.ValidateCommand)
                && (UnityEngine.Event.current.commandName == "SoftDelete" || UnityEngine.Event.current.commandName == "Delete");
            for (int i = 0; i < this.GetPointsCount(); i++)
            {
                if (i == m_NewPointIndex)
                    continue;

                var p0 = this.GetPointPosition(i);
                var id = guiUtility.GetControlID(5353, FocusType.Keyboard);
                var mouseDown = currentEvent.GetTypeForControl(id) == EventType.MouseDown;
                var mouseUp = currentEvent.GetTypeForControl(id) == EventType.MouseUp;

                EditorGUI.BeginChangeCheck();

                if (currentEvent.type == EventType.Repaint)
                {
                    ColorEnum c = GetColorForPoint(i, id);
                    handleOutlineColor = k_OutlineColor[(int)c];
                    handleFillColor = k_FillColor[(int)c];
                }

                var np = p0;
                var hotControlBefore = guiUtility.hotControl;

                if (!currentEvent.alt || guiUtility.hotControl == id)
                    np = DoSlider(id, p0, Vector3.up, Vector3.right, GetHandleSizeForPoint(i), GetCapForPoint(i));
                else if (currentEvent.type == EventType.Repaint)
                    GetCapForPoint(i)(id, p0, Quaternion.LookRotation(Vector3.forward, Vector3.up), GetHandleSizeForPoint(i), currentEvent.type);

                var hotcontrolAfter = guiUtility.hotControl;

                if (mouseUp && hotControlBefore == id && hotcontrolAfter == 0 && (currentEvent.mousePosition == m_MousePositionLastMouseDown) && !currentEvent.shift)
                    HandlePointClick(i);

                if (EditorGUI.EndChangeCheck())
                {
                    RecordUndo();
                    np = Snap(np);
                    MoveSelections(np - p0);
                }

                if (guiUtility.hotControl == id && mouseDown && !m_Selection.Contains(i))
                {
                    SelectPoint(i, currentEvent.shift ? SelectionType.Additive : SelectionType.Normal);
                    Repaint();
                }

                if (m_NewPointDragFinished && activePoint == i && id != -1)
                {
                    guiUtility.keyboardControl = id;
                    m_NewPointDragFinished = false;
                }
            }

            if (wantsDelete)
            {
                if (currentEvent.type == EventType.ValidateCommand)
                {
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.ExecuteCommand)
                {
                    RecordUndo();
                    DeleteSelections();
                    currentEvent.Use();
                }
            }
        }

        public void HandlePointInsertToEdge(int closestEdge, float closestEdgeDist)
        {
            var pointCreatorIsHot = GUIUtility.hotControl == k_CreatorID;

            Vector3 position = pointCreatorIsHot ? GetPointPosition(m_NewPointIndex) : FindClosestPointOnEdge(closestEdge, ScreenToLocal(currentEvent.mousePosition), 100);

            EditorGUI.BeginChangeCheck();

            handleFillColor = k_FillColor[(int)ColorEnum.ESelectedHovered];
            handleOutlineColor = k_OutlineColor[(int)ColorEnum.ESelectedHovered];

            if (!pointCreatorIsHot)
            {
                handleFillColor = handleFillColor.AlphaMultiplied(0.5f);
                handleOutlineColor = handleOutlineColor.AlphaMultiplied(0.5f);
            }

            int hotControlBefore = GUIUtility.hotControl;
            var newPosition = DoSlider(k_CreatorID, position, Vector3.up, Vector3.right, GetHandleSizeForPoint(closestEdge), RectCap);
            if (hotControlBefore != k_CreatorID && GUIUtility.hotControl == k_CreatorID)
            {
                RecordUndo();
                m_NewPointIndex = NextIndex(closestEdge, GetPointsCount());
                InsertPointAt(m_NewPointIndex, newPosition);
                SelectPoint(m_NewPointIndex, SelectionType.Normal);
            }
            else if (EditorGUI.EndChangeCheck())
            {
                RecordUndo();
                newPosition = Snap(newPosition);
                MoveSelections(newPosition - position);
            }
        }

        private void HandleEdgeDragging(int closestEdge)
        {
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    m_ActiveEdge = closestEdge;
                    m_EdgeDragStartP0 = GetPointPosition(m_ActiveEdge);
                    m_EdgeDragStartP1 = GetPointPosition(NextIndex(m_ActiveEdge, GetPointsCount()));
                    if (currentEvent.shift)
                    {
                        RecordUndo();
                        InsertPointAt(m_ActiveEdge + 1, m_EdgeDragStartP0);
                        InsertPointAt(m_ActiveEdge + 2, m_EdgeDragStartP1);
                        m_ActiveEdge++;
                    }
                    m_EdgeDragStartMousePosition = ScreenToLocal(currentEvent.mousePosition);
                    GUIUtility.hotControl = k_EdgeID;
                    currentEvent.Use();
                    break;
                case EventType.MouseDrag:
                    // This can happen when MouseDown event happen when dragging a point instead of line
                    if (GUIUtility.hotControl == k_EdgeID)
                    {
                        RecordUndo();
                        Vector3 mousePos = ScreenToLocal(currentEvent.mousePosition);
                        Vector3 delta = mousePos - m_EdgeDragStartMousePosition;

                        Vector3 position = GetPointPosition(m_ActiveEdge);
                        Vector3 newPosition = m_EdgeDragStartP0 + delta;

                        newPosition = Snap(newPosition);

                        Vector3 snappedDelta = newPosition - position;
                        var i0 = m_ActiveEdge;
                        var i1 = NextIndex(m_ActiveEdge, GetPointsCount());
                        SetPointPosition(m_ActiveEdge, GetPointPosition(i0) + snappedDelta);
                        SetPointPosition(i1, GetPointPosition(i1) + snappedDelta);
                        currentEvent.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == k_EdgeID)
                    {
                        m_ActiveEdge = -1;
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    break;
            }
        }

        Vector3 DoTangent(Vector3 p0, Vector3 t0, int cid, int pointIndex, Color color)
        {
            var handleSize = GetHandleSizeForPoint(pointIndex);
            var tangentSize = GetTangentSizeForPoint(pointIndex);

            handles.color = color;

            float distance = HandleUtility.DistanceToCircle(t0, tangentSize);

            if (lineTexture != null)
                handles.DrawAAPolyLine(lineTexture, new Vector3[] {p0, t0});
            else
                handles.DrawLine(p0, t0);

            handleOutlineColor = distance > 0f ? color : k_OutlineColor[(int)ColorEnum.ESelectedHovered];
            handleFillColor = color;
            var delta = DoSlider(cid, t0, Vector3.up, Vector3.right, tangentSize, GetCapForTangent(pointIndex)) - p0;

            return delta.magnitude < handleSize ? Vector3.zero : delta;
        }

        public void HandlePointClick(int pointIndex)
        {
            if (m_Selection.Count > 1)
            {
                m_Selection.SelectPoint(pointIndex, SelectionType.Normal);
            }
            else if (!currentEvent.control && !currentEvent.shift && m_ActivePointOnLastMouseDown == activePoint)
            {
                OnPointClick(pointIndex);
            }
        }

        public void CycleTangentMode()
        {
            TangentMode oldMode = GetTangentMode(activePoint);
            TangentMode newMode = GetNextTangentMode(oldMode);
            SetTangentMode(activePoint, newMode);
            RefreshTangentsAfterModeChange(activePoint, oldMode, newMode);
        }

        public static TangentMode GetNextTangentMode(TangentMode current)
        {
            return (TangentMode)((((int)current) + 1) % Enum.GetValues(typeof(TangentMode)).Length);
        }

        public void RefreshTangentsAfterModeChange(int pointIndex, TangentMode oldMode, TangentMode newMode)
        {
            if (oldMode != TangentMode.Linear && newMode == TangentMode.Linear)
            {
                SetPointLTangent(pointIndex, Vector3.zero);
                SetPointRTangent(pointIndex, Vector3.zero);
            }
            if (newMode == TangentMode.Continuous)
            {
                if (oldMode == TangentMode.Broken)
                {
                    SetPointRTangent(pointIndex, GetPointLTangent(pointIndex) * -1f);
                }
                if (oldMode == TangentMode.Linear)
                {
                    FromAllZeroToTangents(pointIndex);
                }
            }
        }

        private ColorEnum GetColorForPoint(int pointIndex, int handleID)
        {
            bool hovered = MouseDistanceToPoint(pointIndex) <= 0f;
            bool selected = m_Selection.Contains(pointIndex);

            if (hovered && selected || GUIUtility.hotControl == handleID)
                return ColorEnum.ESelectedHovered;
            if (hovered)
                return ColorEnum.EUnselectedHovered;
            if (selected)
                return ColorEnum.ESelected;

            return ColorEnum.EUnselected;
        }

        private void FromAllZeroToTangents(int pointIndex)
        {
            Vector3 p0 = GetPointPosition(pointIndex);

            int prevPointIndex = pointIndex > 0 ? pointIndex - 1 : GetPointsCount() - 1;

            Vector3 lt = (GetPointPosition(prevPointIndex) - p0) * .33f;
            Vector3 rt = -lt;

            const float maxScreenLen = 100f;

            float ltScreenLen = (LocalToScreen(p0) - LocalToScreen(p0 + lt)).magnitude;
            float rtScreenLen = (LocalToScreen(p0) - LocalToScreen(p0 + rt)).magnitude;

            lt *= Mathf.Min(maxScreenLen / ltScreenLen, 1f);
            rt *= Mathf.Min(maxScreenLen / rtScreenLen, 1f);

            this.SetPointLTangent(pointIndex, lt);
            this.SetPointRTangent(pointIndex, rt);
        }

        private Handles.CapFunction GetCapForTangent(int index)
        {
            if (GetTangentMode(index) == TangentMode.Continuous)
                return CircleCap;

            return DiamondCap;
        }

        private DistanceToControl GetDistanceFuncForTangent(int index)
        {
            if (GetTangentMode(index) == TangentMode.Continuous)
                return DistanceToCircle();

            return HandleUtility.DistanceToDiamond;
        }

        private Handles.CapFunction GetCapForPoint(int index)
        {
            switch (GetTangentMode(index))
            {
                case TangentMode.Broken:
                    return DiamondCap;
                case TangentMode.Continuous:
                    return CircleCap;
                case TangentMode.Linear:
                    return RectCap;
            }
            return DiamondCap;
        }

        private static float DistanceToCircleInternal(Vector3 position, Quaternion rotation, float size)
        {
            return HandleUtility.DistanceToCircle(position, size);
        }

        private float GetHandleSizeForPoint(int index)
        {
            return Camera.current != null ? HandleUtility.GetHandleSize(GetPointPosition(index)) * 0.075f : GetHandleSize();
        }

        private float GetTangentSizeForPoint(int index)
        {
            return GetHandleSizeForPoint(index) * 0.8f;
        }

        private void RefreshTangents(int index, bool rightIsActive)
        {
            var m = GetTangentMode(index);
            var lt = GetPointLTangent(index);
            var rt = GetPointRTangent(index);

            // mirror the change on the other tangent for continuous
            if (m == TangentMode.Continuous)
            {
                if (rightIsActive)
                {
                    lt = -rt;
                    var mag = lt.magnitude;
                    lt = lt.normalized * mag;
                }
                else
                {
                    rt = -lt;
                    var mag = rt.magnitude;
                    rt = rt.normalized * mag;
                }
            }

            this.SetPointLTangent(activePoint, lt);
            this.SetPointRTangent(activePoint, rt);
        }

        private void StoreMouseDownState()
        {
            m_MousePositionLastMouseDown = currentEvent.mousePosition;
            m_ActivePointOnLastMouseDown = activePoint;
        }

        private void DelayedResetIfNecessary()
        {
            if (m_DelayedReset)
            {
                guiUtility.hotControl = 0;
                guiUtility.keyboardControl = 0;
                m_Selection.Clear();
                activePoint = -1;
                m_DelayedReset = false;
            }
        }

        public Vector3 FindClosestPointOnEdge(int edgeIndex, Vector3 position, int iterations)
        {
            float step = 1f / iterations;
            float closestDistance = float.MaxValue;
            float closestDistanceIndex = edgeIndex;

            for (float a = 0f; a <= 1f; a += step)
            {
                Vector3 pos = GetPositionByIndex(edgeIndex + a);
                float distance = (position - pos).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDistanceIndex = edgeIndex + a;
                }
            }

            return GetPositionByIndex(closestDistanceIndex);
        }

        private Vector3 GetPositionByIndex(float index)
        {
            int indexA = Mathf.FloorToInt(index);
            int indexB = NextIndex(indexA, GetPointsCount());
            float subPosition = index - indexA;
            return GetPoint(GetPointPosition(indexA), GetPointPosition(indexB), GetPointRTangent(indexA), GetPointLTangent(indexB), subPosition);
        }

        private static Vector3 GetPoint(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, float t)
        {
            t = Mathf.Clamp01(t);
            float a = 1f - t;
            return a * a * a * startPosition + 3f * a * a * t * (startPosition + startTangent) + 3f * a * t * t * (endPosition + endTangent) + t * t * t * endPosition;
        }

        private int FindClosestPointToMouse()
        {
            Vector3 mousePos = ScreenToLocal(currentEvent.mousePosition);
            return FindClosestPointIndex(mousePos);
        }

        private float MouseDistanceToClosestTangent()
        {
            if (activePoint < 0)
                return float.MaxValue;

            var lt = GetPointLTangent(activePoint);
            var rt = GetPointRTangent(activePoint);

            if (lt.sqrMagnitude == 0f && rt.sqrMagnitude == 0f)
                return float.MaxValue;

            var p = GetPointPosition(activePoint);
            var tangentSize = GetTangentSizeForPoint(activePoint);

            return Mathf.Min(
                HandleUtility.DistanceToRectangle(p + lt, Quaternion.identity, tangentSize),
                HandleUtility.DistanceToRectangle(p + rt, Quaternion.identity, tangentSize)
                );
        }

        private int FindClosestPointIndex(Vector3 position)
        {
            float closestDistance = float.MaxValue;
            int closestIndex = -1;
            for (int i = 0; i < this.GetPointsCount(); i++)
            {
                var p0 = this.GetPointPosition(i);
                var distance = (p0 - position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestIndex = i;
                    closestDistance = distance;
                }
            }
            return closestIndex;
        }

        private DistanceToControl GetDistanceFuncForPoint(int index)
        {
            switch (GetTangentMode(index))
            {
                case TangentMode.Broken:
                    return DistanceToDiamond();
                case TangentMode.Continuous:
                    return DistanceToCircle();
                case TangentMode.Linear:
                    return DistanceToRectangle();
            }
            return DistanceToRectangle();
        }

        private float MouseDistanceToPoint(int index)
        {
            switch (GetTangentMode(index))
            {
                case TangentMode.Broken:
                    return HandleUtility.DistanceToDiamond(GetPointPosition(index), Quaternion.identity, GetHandleSizeForPoint(index));
                case TangentMode.Linear:
                    return HandleUtility.DistanceToRectangle(GetPointPosition(index), Quaternion.identity, GetHandleSizeForPoint(index));
                case TangentMode.Continuous:
                    return HandleUtility.DistanceToCircle(GetPointPosition(index), GetHandleSizeForPoint(index));
            }
            return float.MaxValue;
        }

        private bool EdgeDragModifiersActive()
        {
            return currentEvent.modifiers == EventModifiers.Control;
        }

        private static Vector3 DoSlider(int id, Vector3 position, Vector3 slide1, Vector3 slide2, float s, Handles.CapFunction cap)
        {
            return Slider2D.Do(id, position, Vector3.zero, Vector3.Cross(slide1, slide2), slide1, slide2, s, cap, Vector2.zero, false);
        }

        public void RectCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
            {
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            }
            else if (eventType == EventType.Repaint)
            {
                Vector3 normal = handles.matrix.GetColumn(2);
                Vector3 projectedUp = (ProjectPointOnPlane(normal, position, position + Vector3.up) - position).normalized;
                Quaternion localRotation = Quaternion.LookRotation(handles.matrix.GetColumn(2), projectedUp);
                Vector3 sideways = localRotation * Vector3.right * size;
                Vector3 up = localRotation * Vector3.up * size;
                List<Vector3> list = GetDrawBatchList(new DrawBatchDataKey(handleFillColor, GL.TRIANGLES));
                list.Add(position + sideways + up);
                list.Add(position + sideways - up);
                list.Add(position - sideways - up);
                list.Add(position - sideways - up);
                list.Add(position - sideways + up);
                list.Add(position + sideways + up);

                list = GetDrawBatchList(new DrawBatchDataKey(handleOutlineColor, GL.LINES));
                list.Add(position + sideways + up);
                list.Add(position + sideways - up);
                list.Add(position + sideways - up);
                list.Add(position - sideways - up);
                list.Add(position - sideways - up);
                list.Add(position - sideways + up);
                list.Add(position - sideways + up);
                list.Add(position + sideways + up);
            }
        }

        public void CircleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
            {
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            }
            else if (eventType == EventType.Repaint)
            {
                Vector3 forward = handleMatrixrotation * rotation * Vector3.forward;
                Vector3 tangent = Vector3.Cross(forward, Vector3.up);
                if (tangent.sqrMagnitude < .001f)
                    tangent = Vector3.Cross(forward, Vector3.right);

                Vector3[] points = new Vector3[60];
                handles.SetDiscSectionPoints(points, position, forward, tangent, 360f, size);

                List<Vector3> list = GetDrawBatchList(new DrawBatchDataKey(handleFillColor, GL.TRIANGLES));
                for (int i = 1; i < points.Length; i++)
                {
                    list.Add(position);
                    list.Add(points[i]);
                    list.Add(points[i - 1]);
                }

                list = GetDrawBatchList(new DrawBatchDataKey(handleOutlineColor, GL.LINES));
                for (int i = 0; i < points.Length - 1; i++)
                {
                    list.Add(points[i]);
                    list.Add(points[i + 1]);
                }
            }
        }

        public void DiamondCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
            {
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            }
            else if (eventType == EventType.Repaint)
            {
                Vector3 normal = handles.matrix.GetColumn(2);
                Vector3 projectedUp = (ProjectPointOnPlane(normal, position, position + Vector3.up) - position).normalized;
                Quaternion localRotation = Quaternion.LookRotation(handles.matrix.GetColumn(2), projectedUp);
                Vector3 sideways = localRotation * Vector3.right * size * 1.25f;
                Vector3 up = localRotation * Vector3.up * size * 1.25f;

                List<Vector3> list = GetDrawBatchList(new DrawBatchDataKey(handleFillColor, GL.TRIANGLES));
                list.Add(position - up);
                list.Add(position + sideways);
                list.Add(position - sideways);
                list.Add(position - sideways);
                list.Add(position + up);
                list.Add(position + sideways);

                list = GetDrawBatchList(new DrawBatchDataKey(handleOutlineColor, GL.LINES));
                list.Add(position + sideways);
                list.Add(position - up);
                list.Add(position - up);
                list.Add(position - sideways);
                list.Add(position - sideways);
                list.Add(position + up);
                list.Add(position + up);
                list.Add(position + sideways);
            }
        }

        private static int NextIndex(int index, int total)
        {
            return mod(index + 1, total);
        }

        private static int PreviousIndex(int index, int total)
        {
            return mod(index - 1, total);
        }

        private static int mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        private static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
        {
            planeNormal.Normalize();
            var distance = -Vector3.Dot(planeNormal.normalized, (point - planePoint));
            return point + planeNormal * distance;
        }

        public void RegisterToShapeEditor(ShapeEditor se)
        {
            ++m_ShapeEditorRegisteredTo;
            se.m_ShapeEditorListeners.Add(this);
        }

        public void UnregisterFromShapeEditor(ShapeEditor se)
        {
            --m_ShapeEditorRegisteredTo;
            se.m_ShapeEditorListeners.Remove(this);
        }

        private void OnShapeEditorUpdateDone()
        {
            // When all the ShapeEditor we are interested in
            // has completed their update, we reset our internal values
            ++m_ShapeEditorUpdateDone;
            if (m_ShapeEditorUpdateDone >= m_ShapeEditorRegisteredTo)
            {
                m_ShapeEditorUpdateDone =  0;
                m_MouseClosestEdge = -1;
                m_MouseClosestEdgeDist = float.MaxValue;
                m_EdgePoints = null;
            }
        }

        private void ClearSelectedPoints()
        {
            selectedPoints.Clear();
            activePoint = -1;
        }

        private void SelectPointsInRect(Rect r, SelectionType st)
        {
            var localRect = EditorGUIExt.FromToRect(ScreenToLocal(r.min), ScreenToLocal(r.max));
            m_Selection.RectSelect(localRect, st);
        }

        private void DeleteSelections()
        {
            foreach (var se in m_ShapeEditorListeners)
                se.m_Selection.DeleteSelection();
            m_Selection.DeleteSelection();
        }

        private void MoveSelections(Vector2 distance)
        {
            foreach (var se in m_ShapeEditorListeners)
                se.m_Selection.MoveSelection(distance);
            m_Selection.MoveSelection(distance);
        }

        private void SelectPoint(int index, SelectionType st)
        {
            if (st == SelectionType.Normal)
            {
                foreach (var se in m_ShapeEditorListeners)
                    se.ClearSelectedPoints();
            }
            m_Selection.SelectPoint(index, st);
        }
    }
}
