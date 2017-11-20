// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;
using TangentMode = UnityEditor.AnimationUtility.TangentMode;
using RectangleToolFlags = UnityEditor.CurveEditorSettings.RectangleToolFlags;

namespace UnityEditor
{
    // External selection interface
    internal interface ISelectionBinding
    {
        GameObject rootGameObject { get; }
        AnimationClip animationClip { get; }

        bool clipIsEditable { get; }
        bool animationIsEditable { get; }

        float timeOffset { get; }

        int id { get; }
    }

    // External state interface
    internal interface ICurveEditorState
    {
        TimeArea.TimeFormat timeFormat { get; }
    }

    internal class CurveWrapper
    {
        public delegate Vector2 GetAxisScalarsCallback();
        public delegate void SetAxisScalarsCallback(Vector2 newAxisScalars);
        public delegate void PreProcessKeyMovement(ref Keyframe key);

        public CurveWrapper()
        {
            id = 0;
            groupId = -1;
            regionId = -1;
            hidden = false;
            readOnly = false;
            listIndex = -1;
            getAxisUiScalarsCallback = null;
            setAxisUiScalarsCallback = null;
        }

        internal enum SelectionMode
        {
            None = 0,
            Selected = 1,
            SemiSelected = 2
        }

        // Curve management
        private CurveRenderer m_Renderer;
        private ISelectionBinding m_SelectionBinding;

        public CurveRenderer renderer { get { return m_Renderer; } set { m_Renderer = value; } }
        public AnimationCurve curve { get { return renderer.GetCurve(); } }

        public GameObject rootGameObjet { get { return m_SelectionBinding != null ? m_SelectionBinding.rootGameObject : null; } }
        public AnimationClip animationClip { get { return m_SelectionBinding != null ? m_SelectionBinding.animationClip : null; } }
        public float timeOffset { get { return m_SelectionBinding != null ? m_SelectionBinding.timeOffset : 0.0f; } }
        public bool clipIsEditable { get { return m_SelectionBinding != null ? m_SelectionBinding.clipIsEditable : true; } }
        public bool animationIsEditable { get { return m_SelectionBinding != null ? m_SelectionBinding.animationIsEditable : true; } }
        public int selectionID { get { return m_SelectionBinding != null ? m_SelectionBinding.id : 0; } }

        public ISelectionBinding selectionBindingInterface { get { return m_SelectionBinding; } set { m_SelectionBinding = value; } }

        public Bounds bounds { get { return renderer.GetBounds(); } }

        // Input - should not be changed by curve editor
        public int id;
        public EditorCurveBinding binding;
        public int groupId;
        public int regionId;                                    // Regions are defined by two curves added after each other with the same regionId.
        public Color color;
        public Color wrapColorMultiplier = Color.white;
        public bool readOnly;
        public bool hidden;
        public GetAxisScalarsCallback getAxisUiScalarsCallback; // Delegate used to fetch values that are multiplied on x and y axis ui values
        public SetAxisScalarsCallback setAxisUiScalarsCallback; // Delegate used to set values back that has been changed by this curve editor

        public PreProcessKeyMovement preProcessKeyMovementDelegate; // Delegate used limit key manipulation to fit curve constraints

        // Should be updated by curve editor as appropriate
        public SelectionMode selected;
        public int listIndex;                                       // Index into m_AnimationCurves list

        private bool m_Changed;
        public bool changed
        {
            get
            {
                return m_Changed;
            }

            set
            {
                m_Changed = value;

                if (value && renderer != null)
                    renderer.FlushCache();
            }
        }

        public int AddKey(Keyframe key)
        {
            PreProcessKey(ref key);
            return curve.AddKey(key);
        }

        public void PreProcessKey(ref Keyframe key)
        {
            if (preProcessKeyMovementDelegate != null)
                preProcessKeyMovementDelegate(ref key);
        }

        public int MoveKey(int index, ref Keyframe key)
        {
            PreProcessKey(ref key);
            return curve.MoveKey(index, key);
        }

        // An additional vertical min / max range clamp when editing multiple curves with different ranges
        public float vRangeMin = -Mathf.Infinity;
        public float vRangeMax = Mathf.Infinity;
    }

    //  Control point collection renderer
    class CurveControlPointRenderer
    {
        // Control point mesh renderers.
        private ControlPointRenderer m_UnselectedPointRenderer;
        private ControlPointRenderer m_SelectedPointRenderer;
        private ControlPointRenderer m_SelectedPointOverlayRenderer;
        private ControlPointRenderer m_SemiSelectedPointOverlayRenderer;

        public CurveControlPointRenderer()
        {
            m_UnselectedPointRenderer = new ControlPointRenderer(CurveEditor.Styles.pointIcon);
            m_SelectedPointRenderer = new ControlPointRenderer(CurveEditor.Styles.pointIconSelected);
            m_SelectedPointOverlayRenderer = new ControlPointRenderer(CurveEditor.Styles.pointIconSelectedOverlay);
            m_SemiSelectedPointOverlayRenderer = new ControlPointRenderer(CurveEditor.Styles.pointIconSemiSelectedOverlay);
        }

        public void FlushCache()
        {
            m_UnselectedPointRenderer.FlushCache();
            m_SelectedPointRenderer.FlushCache();
            m_SelectedPointOverlayRenderer.FlushCache();
            m_SemiSelectedPointOverlayRenderer.FlushCache();
        }

        public void Clear()
        {
            m_UnselectedPointRenderer.Clear();
            m_SelectedPointRenderer.Clear();
            m_SelectedPointOverlayRenderer.Clear();
            m_SemiSelectedPointOverlayRenderer.Clear();
        }

        public void Render()
        {
            m_UnselectedPointRenderer.Render();
            m_SelectedPointRenderer.Render();
            m_SelectedPointOverlayRenderer.Render();
            m_SemiSelectedPointOverlayRenderer.Render();
        }

        public void AddPoint(Rect rect, Color color)
        {
            m_UnselectedPointRenderer.AddPoint(rect, color);
        }

        public void AddSelectedPoint(Rect rect, Color color)
        {
            m_SelectedPointRenderer.AddPoint(rect, color);
            m_SelectedPointOverlayRenderer.AddPoint(rect, Color.white);
        }

        public void AddSemiSelectedPoint(Rect rect, Color color)
        {
            m_SelectedPointRenderer.AddPoint(rect, color);
            m_SemiSelectedPointOverlayRenderer.AddPoint(rect, Color.white);
        }
    }

    [System.Serializable]
    internal class CurveEditor : TimeArea, CurveUpdater
    {
        CurveWrapper[] m_AnimationCurves;

        static int s_SelectKeyHash = "SelectKeys".GetHashCode();

        public delegate void CallbackFunction();

        public CallbackFunction curvesUpdated;

        public CurveWrapper[] animationCurves
        {
            get
            {
                if (m_AnimationCurves == null)
                    m_AnimationCurves = new CurveWrapper[0];

                return m_AnimationCurves;
            }
            set
            {
                m_AnimationCurves = value;

                curveIDToIndexMap.Clear();
                m_EnableCurveGroups = false;
                for (int i = 0; i < m_AnimationCurves.Length; ++i)
                {
                    m_AnimationCurves[i].listIndex = i;
                    curveIDToIndexMap.Add(m_AnimationCurves[i].id, i);
                    m_EnableCurveGroups = m_EnableCurveGroups || (m_AnimationCurves[i].groupId != -1);
                }
                SyncDrawOrder();
                SyncSelection();
                ValidateCurveList();
            }
        }

        public bool GetTopMostCurveID(out int curveID)
        {
            if (m_DrawOrder.Count > 0)
            {
                curveID = m_DrawOrder[m_DrawOrder.Count - 1];
                return true;
            }

            curveID = -1;
            return false;
        }

        private List<int> m_DrawOrder = new List<int>(); // contains curveIds (last element topmost)
        void SyncDrawOrder()
        {
            // Init
            if (m_DrawOrder.Count == 0)
            {
                m_DrawOrder = m_AnimationCurves.Select(cw => cw.id).ToList();
                return;
            }

            List<int> newDrawOrder = new List<int>(m_AnimationCurves.Length);
            // First add known curveIds (same order as before)
            for (int i = 0; i < m_DrawOrder.Count; ++i)
            {
                int curveID = m_DrawOrder[i];
                for (int j = 0; j < m_AnimationCurves.Length; ++j)
                {
                    if (m_AnimationCurves[j].id == curveID)
                    {
                        newDrawOrder.Add(curveID);
                        break;
                    }
                }
            }
            m_DrawOrder = newDrawOrder;

            // Found them all
            if (m_DrawOrder.Count == m_AnimationCurves.Length)
                return;

            // Add nonexisting curveIds (new curves are top most)
            for (int i = 0; i < m_AnimationCurves.Length; ++i)
            {
                int curveID = m_AnimationCurves[i].id;
                bool found = false;
                for (int j = 0; j < m_DrawOrder.Count; ++j)
                {
                    if (m_DrawOrder[j] == curveID)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    m_DrawOrder.Add(curveID);
            }

            // Fallback if invalid setup with multiple curves with same curveID (see case 482048)
            if (m_DrawOrder.Count != m_AnimationCurves.Length)
            {
                // Ordering can fail if we have a hierarchy like:
                //
                // Piston
                //      Cylinder
                //          InnerCyl
                //      Cylinder
                //          InnerCyl
                // Since we cannot generate unique curve ids for identical paths like Cylinder and InnerCyl.
                m_DrawOrder = m_AnimationCurves.Select(cw => cw.id).ToList();
            }
        }

        public ICurveEditorState state;

        public TimeArea.TimeFormat timeFormat
        {
            get
            {
                if (state != null)
                    return state.timeFormat;

                return TimeArea.TimeFormat.None;
            }
        }

        private Matrix4x4 TimeOffsetMatrix(CurveWrapper cw)
        {
            return Matrix4x4.TRS(new Vector3(cw.timeOffset * m_Scale.x, 0.0f, 0.0f), Quaternion.identity, Vector3.one);
        }

        private Matrix4x4 DrawingToOffsetViewMatrix(CurveWrapper cw)
        {
            return TimeOffsetMatrix(cw) * drawingToViewMatrix;
        }

        private Vector2 DrawingToOffsetViewTransformPoint(CurveWrapper cw, Vector2 lhs)
        {
            return new Vector2(lhs.x * m_Scale.x + m_Translation.x + cw.timeOffset * m_Scale.x, lhs.y * m_Scale.y + m_Translation.y);
        }

        private Vector3 DrawingToOffsetViewTransformPoint(CurveWrapper cw, Vector3 lhs)
        {
            return new Vector3(lhs.x * m_Scale.x + m_Translation.x + cw.timeOffset * m_Scale.x, lhs.y * m_Scale.y + m_Translation.y, 0);
        }

        private Vector2 OffsetViewToDrawingTransformPoint(CurveWrapper cw, Vector2 lhs)
        {
            return new Vector2((lhs.x - m_Translation.x - cw.timeOffset * m_Scale.x) / m_Scale.x , (lhs.y - m_Translation.y) / m_Scale.y);
        }

        private Vector3 OffsetViewToDrawingTransformPoint(CurveWrapper cw, Vector3 lhs)
        {
            return new Vector3((lhs.x - m_Translation.x - cw.timeOffset * m_Scale.x) / m_Scale.x , (lhs.y - m_Translation.y) / m_Scale.y, 0);
        }

        private Vector2 OffsetMousePositionInDrawing(CurveWrapper cw)
        {
            return OffsetViewToDrawingTransformPoint(cw, Event.current.mousePosition);
        }

        internal Bounds m_DefaultBounds = new Bounds(new Vector3(0.5f, 0.5f, 0), new Vector3(1, 1, 0));

        private CurveEditorSettings m_Settings = new CurveEditorSettings();
        public CurveEditorSettings settings { get { return m_Settings; } set { if (value != null) { m_Settings = value; ApplySettings(); } } }

        protected void ApplySettings()
        {
            hRangeLocked = settings.hRangeLocked;
            vRangeLocked = settings.vRangeLocked;
            hRangeMin = settings.hRangeMin;
            hRangeMax = settings.hRangeMax;
            vRangeMin = settings.vRangeMin;
            vRangeMax = settings.vRangeMax;
            scaleWithWindow = settings.scaleWithWindow;
            hSlider = settings.hSlider;
            vSlider = settings.vSlider;

            RecalculateBounds();
        }

        // Other style settings
        private Color m_TangentColor = new Color(1, 1, 1, 0.5f);
        public Color tangentColor { get { return m_TangentColor; } set { m_TangentColor = value; } }

        /// 1/time to snap all keyframes to while dragging. Set to 0 for no snap (default)
        public float invSnap = 0;

        private CurveMenuManager m_MenuManager;

        static int s_TangentControlIDHash = "s_TangentControlIDHash".GetHashCode();

        [SerializeField] CurveEditorSelection m_Selection;

        internal CurveEditorSelection selection
        {
            get
            {
                if (m_Selection == null)
                {
                    m_Selection = ScriptableObject.CreateInstance<CurveEditorSelection>();
                    m_Selection.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_Selection;
            }
        }

        internal List<CurveSelection> selectedCurves
        {
            get
            {
                return selection.selectedCurves;
            }
            set
            {
                selection.selectedCurves = value;
                InvalidateSelectionBounds();
            }
        }

        List<CurveSelection> preCurveDragSelection = null;

        public bool hasSelection { get { return selectedCurves.Count != 0; } }

        bool m_InRangeSelection = false;

        internal void BeginRangeSelection()
        {
            m_InRangeSelection = true;
        }

        internal void EndRangeSelection()
        {
            m_InRangeSelection = false;
            selectedCurves.Sort();
        }

        internal void AddSelection(CurveSelection curveSelection)
        {
            selectedCurves.Add(curveSelection);

            if (!m_InRangeSelection)
                selectedCurves.Sort();

            InvalidateSelectionBounds();
        }

        internal void RemoveSelection(CurveSelection curveSelection)
        {
            selectedCurves.Remove(curveSelection);
            InvalidateSelectionBounds();
        }

        internal void ClearSelection()
        {
            selectedCurves.Clear();
            InvalidateSelectionBounds();
        }

        internal CurveWrapper GetCurveWrapperFromID(int curveID)
        {
            if (m_AnimationCurves == null)
                return null;

            int index;
            if (curveIDToIndexMap.TryGetValue(curveID, out index))
                return m_AnimationCurves[index];

            return null;
        }

        internal CurveWrapper GetCurveWrapperFromSelection(CurveSelection curveSelection)
        {
            return GetCurveWrapperFromID(curveSelection.curveID);
        }

        internal AnimationCurve GetCurveFromSelection(CurveSelection curveSelection)
        {
            CurveWrapper curveWrapper = GetCurveWrapperFromSelection(curveSelection);
            return (curveWrapper != null) ? curveWrapper.curve : null;
        }

        internal Keyframe GetKeyframeFromSelection(CurveSelection curveSelection)
        {
            AnimationCurve curve = GetCurveFromSelection(curveSelection);
            if (curve != null)
            {
                if (curveSelection.key >= 0 && curveSelection.key < curve.length)
                {
                    return curve[curveSelection.key];
                }
            }

            return new Keyframe();
        }

        // Array of tangent points that have been revealed
        CurveSelection m_SelectedTangentPoint;

        // Selection tracking:
        // What the selection was at the start of a drag
        List<CurveSelection> s_SelectionBackup;
        // Time range selection, is it active and what was the mousedown time (start) and the current end time.
        float s_TimeRangeSelectionStart, s_TimeRangeSelectionEnd;
        bool s_TimeRangeSelectionActive = false;

        private bool m_BoundsAreDirty = true;
        private bool m_SelectionBoundsAreDirty = true;

        private bool m_EnableCurveGroups = false;

        Bounds m_SelectionBounds = new Bounds(Vector3.zero, Vector3.zero);
        public Bounds selectionBounds
        {
            get
            {
                RecalculateSelectionBounds();
                return m_SelectionBounds;
            }
        }

        Bounds m_CurveBounds = new Bounds(Vector3.zero, Vector3.zero);
        public Bounds curveBounds
        {
            get
            {
                RecalculateBounds();
                return m_CurveBounds;
            }
        }

        Bounds m_DrawingBounds = new Bounds(Vector3.zero, Vector3.zero);
        public override Bounds drawingBounds
        {
            get
            {
                RecalculateBounds();
                return m_DrawingBounds;
            }
        }

        // Helpers for temporarily saving a bunch of keys.
        class SavedCurve
        {
            public class SavedKeyFrame
            {
                public Keyframe key;
                public CurveWrapper.SelectionMode selected;

                public SavedKeyFrame(Keyframe key, CurveWrapper.SelectionMode selected)
                {
                    this.key = key;
                    this.selected = selected;
                }

                public SavedKeyFrame Clone()
                {
                    SavedKeyFrame duplicate = new SavedKeyFrame(key, selected);
                    return duplicate;
                }
            }

            public class SavedKeyFrameComparer : IComparer<float>
            {
                public static SavedKeyFrameComparer Instance = new SavedKeyFrameComparer();

                public int Compare(float time1, float time2)
                {
                    float cmp = time1 - time2;
                    return cmp < -kCurveTimeEpsilon ? -1 : (cmp >= kCurveTimeEpsilon ? 1 : 0);
                }
            }


            public int curveId;
            public List<SavedKeyFrame> keys;

            public delegate SavedKeyFrame KeyFrameOperation(SavedKeyFrame keyframe, SavedCurve curve);
        }
        List<SavedCurve> m_CurveBackups;

        CurveWrapper m_DraggingKey = null;
        Vector2 m_DraggedCoord;
        Vector2 m_MoveCoord;

        // Used to avoid drawing points too close to each other.
        private Vector2 m_PreviousDrawPointCenter;

        private enum AxisLock { None, X, Y }
        private AxisLock m_AxisLock;

        CurveControlPointRenderer m_PointRenderer;
        CurveEditorRectangleTool m_RectangleTool;

        // The square of the maximum pick distance in pixels.
        // The mouse will select a key if it's within this distance from the key point.
        const float kMaxPickDistSqr = 10 * 10;
        const float kExactPickDistSqr = 4 * 4;

        const float kCurveTimeEpsilon = 0.00001f;

        public CurveEditor(Rect rect, CurveWrapper[] curves, bool minimalGUI) : base(minimalGUI)
        {
            this.rect = rect;
            animationCurves = curves;

            float[] modulos = new float[] {
                0.0000001f, 0.0000005f, 0.000001f, 0.000005f, 0.00001f, 0.00005f, 0.0001f, 0.0005f,
                0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.5f, 1, 5, 10, 50, 100, 500,
                1000, 5000, 10000, 50000, 100000, 500000, 1000000, 5000000, 10000000
            };
            hTicks = new TickHandler();
            hTicks.SetTickModulos(modulos);
            vTicks = new TickHandler();
            vTicks.SetTickModulos(modulos);
            margin = 40.0f;

            OnEnable();
        }

        public void OnEnable()
        {
            // Only add callback once.
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            if (m_PointRenderer != null)
                m_PointRenderer.FlushCache();
        }

        public void OnDestroy()
        {
            if (m_Selection != null)
                ScriptableObject.DestroyImmediate(m_Selection);
        }

        void UndoRedoPerformed()
        {
            if (settings.undoRedoSelection)
                InvalidateSelectionBounds();
            else
                SelectNone();
        }

        private void ValidateCurveList()
        {
            // Validate that regions are valid (they should consist of two curves after each other with same regionId)
            for (int i = 0; i < m_AnimationCurves.Length; ++i)
            {
                CurveWrapper cw = m_AnimationCurves[i];
                int regId1 = cw.regionId;
                if (regId1 >= 0)
                {
                    if (i == m_AnimationCurves.Length - 1)
                    {
                        Debug.LogError("Region has only one curve last! Regions should be added as two curves after each other with same regionId");
                        return;
                    }

                    CurveWrapper cw2 = m_AnimationCurves[++i];
                    int regId2 = cw2.regionId;
                    if (regId1 != regId2)
                    {
                        Debug.LogError("Regions should be added as two curves after each other with same regionId: " + regId1 + " != " + regId2);
                        return;
                    }
                }
            }

            if (m_DrawOrder.Count != m_AnimationCurves.Length)
            {
                Debug.LogError("DrawOrder and AnimationCurves mismatch: DrawOrder " + m_DrawOrder.Count + ", AnimationCurves: " + m_AnimationCurves.Length);
                return;
            }

            // Validate draw order regions
            int numCurves = m_DrawOrder.Count;
            for (int i = 0; i < numCurves; ++i)
            {
                int curveID = m_DrawOrder[i];
                // If curve is part of a region then find other curve
                int regionId = GetCurveWrapperFromID(curveID).regionId;
                if (regionId >= 0)
                {
                    if (i == numCurves - 1)
                    {
                        Debug.LogError("Region has only one curve last! Regions should be added as two curves after each other with same regionId");
                        return;
                    }

                    // Ensure next curve has a matching regionId
                    int curveId2 = m_DrawOrder[++i];
                    int regionId2 = GetCurveWrapperFromID(curveId2).regionId;
                    if (regionId != regionId2)
                    {
                        Debug.LogError("DrawOrder: Regions not added correctly after each other. RegionIds: " + regionId + " , " + regionId2);
                        return;
                    }
                }
            }

            // Debug.Log all curves and their state (outcomment if needed)
            /*
            string info = "Count: " + m_AnimationCurves.Length + " (Click me for more info)\n";
            foreach (CurveWrapper cw in m_AnimationCurves)
                info += ("Curve: id " + cw.id + ", regionId " + cw.regionId + ", hidden " + cw.hidden + "\n");
            Debug.Log(info);
            */
        }

        Dictionary<int, int> m_CurveIDToIndexMap;
        private Dictionary<int, int> curveIDToIndexMap
        {
            get
            {
                if (m_CurveIDToIndexMap == null)
                    m_CurveIDToIndexMap = new Dictionary<int, int>();

                return m_CurveIDToIndexMap;
            }
        }

        private void SyncSelection()
        {
            Init();

            List<CurveSelection> newSelection = new List<CurveSelection>(selectedCurves.Count);

            foreach (CurveSelection cs in selectedCurves)
            {
                CurveWrapper cw = GetCurveWrapperFromSelection(cs);
                if (cw != null && (!cw.hidden || cw.groupId != -1))
                {
                    cw.selected = CurveWrapper.SelectionMode.Selected;
                    newSelection.Add(cs);
                }
            }
            if (newSelection.Count != selectedCurves.Count)
            {
                selectedCurves = newSelection;
            }

            InvalidateBounds();
        }

        public void InvalidateBounds()
        {
            m_BoundsAreDirty = true;
        }

        private void RecalculateBounds()
        {
            if (InLiveEdit())
                return;

            if (!m_BoundsAreDirty)
                return;

            const float kMinRange = 0.1F;

            m_DrawingBounds = m_DefaultBounds;
            m_CurveBounds = m_DefaultBounds;

            if (animationCurves != null)
            {
                bool assigned = false;
                for (int i = 0; i < animationCurves.Length; ++i)
                {
                    CurveWrapper wrapper = animationCurves[i];

                    if (wrapper.hidden)
                        continue;

                    if (wrapper.curve.length == 0)
                        continue;

                    if (!assigned)
                    {
                        m_CurveBounds = wrapper.bounds;
                        assigned = true;
                    }
                    else
                    {
                        m_CurveBounds.Encapsulate(wrapper.bounds);
                    }
                }
            }

            //  Calculate bounds based on curve bounds if bound is not set by hRangeMin/hRangeMax vRangeMin/vRangeMax.
            float minx = hRangeMin != Mathf.NegativeInfinity ? hRangeMin : m_CurveBounds.min.x;
            float miny = vRangeMin != Mathf.NegativeInfinity ? vRangeMin : m_CurveBounds.min.y;
            float maxx = hRangeMax != Mathf.Infinity ? hRangeMax : m_CurveBounds.max.x;
            float maxy = vRangeMax != Mathf.Infinity ? vRangeMax : m_CurveBounds.max.y;

            m_DrawingBounds.SetMinMax(new Vector3(minx, miny, m_CurveBounds.min.z), new Vector3(maxx, maxy, m_CurveBounds.max.z));

            // Enforce minimum size of bounds
            m_DrawingBounds.size = new Vector3(Mathf.Max(m_DrawingBounds.size.x, kMinRange), Mathf.Max(m_DrawingBounds.size.y, kMinRange), 0);
            m_CurveBounds.size = new Vector3(Mathf.Max(m_CurveBounds.size.x, kMinRange), Mathf.Max(m_CurveBounds.size.y, kMinRange), 0);

            m_BoundsAreDirty = false;
        }

        public void InvalidateSelectionBounds()
        {
            m_SelectionBoundsAreDirty = true;
        }

        private void RecalculateSelectionBounds()
        {
            if (!m_SelectionBoundsAreDirty)
                return;

            if (hasSelection)
            {
                List<CurveSelection> selected = selectedCurves;

                CurveWrapper curveWrapper = GetCurveWrapperFromSelection(selected[0]);
                float timeOffset = (curveWrapper != null) ? curveWrapper.timeOffset : 0f;

                Keyframe keyframe = GetKeyframeFromSelection(selected[0]);
                m_SelectionBounds = new Bounds(new Vector2(keyframe.time + timeOffset, keyframe.value), Vector2.zero);

                for (int i = 1; i < selected.Count; ++i)
                {
                    keyframe = GetKeyframeFromSelection(selected[i]);
                    m_SelectionBounds.Encapsulate(new Vector2(keyframe.time + timeOffset, keyframe.value));
                }
            }
            else
            {
                m_SelectionBounds = new Bounds(Vector3.zero, Vector3.zero);
            }

            m_SelectionBoundsAreDirty = false;
        }

        // Frame all curves to be visible.
        public void FrameClip(bool horizontally, bool vertically)
        {
            Bounds frameBounds = curveBounds;
            if (frameBounds.size == Vector3.zero)
                return;

            if (horizontally)
                SetShownHRangeInsideMargins(frameBounds.min.x, frameBounds.max.x);
            if (vertically)
                SetShownVRangeInsideMargins(frameBounds.min.y, frameBounds.max.y);
        }

        // Frame selected keys to be visible.
        public void FrameSelected(bool horizontally, bool vertically)
        {
            if (!hasSelection)
            {
                FrameClip(horizontally, vertically);
                return;
            }

            Bounds frameBounds = new Bounds();

            // Add neighboring keys in bounds if only a single key is selected.
            if (selectedCurves.Count == 1)
            {
                CurveSelection cs = selectedCurves[0];
                CurveWrapper cw = GetCurveWrapperFromSelection(cs);

                // Encapsulate key in bounds
                frameBounds = new Bounds(new Vector2(cw.curve[cs.key].time, cw.curve[cs.key].value), Vector2.zero);

                // Include neighboring keys in bounds
                if (cs.key - 1 >= 0)
                    frameBounds.Encapsulate(new Vector2(cw.curve[cs.key - 1].time, cw.curve[cs.key - 1].value));
                if (cs.key + 1 < cw.curve.length)
                    frameBounds.Encapsulate(new Vector2(cw.curve[cs.key + 1].time, cw.curve[cs.key + 1].value));
            }
            else
            {
                frameBounds = selectionBounds;
            }

            // Enforce minimum size of bounds
            frameBounds.size = new Vector3(Mathf.Max(frameBounds.size.x, 0.1F), Mathf.Max(frameBounds.size.y, 0.1F), 0);

            if (horizontally)
                SetShownHRangeInsideMargins(frameBounds.min.x, frameBounds.max.x);
            if (vertically)
                SetShownVRangeInsideMargins(frameBounds.min.y, frameBounds.max.y);
        }

        public void UpdateCurves(List<int> curveIds, string undoText)
        {
            foreach (int id in curveIds)
            {
                CurveWrapper cw = GetCurveWrapperFromID(id);
                cw.changed = true;
            }
            if (curvesUpdated != null)
                curvesUpdated();
        }

        public void UpdateCurves(List<ChangedCurve> changedCurves, string undoText)
        {
            UpdateCurves(new List<int>(changedCurves.Select(curve => curve.curveId)), undoText);
        }

        public void StartLiveEdit()
        {
            MakeCurveBackups();
        }

        public void EndLiveEdit()
        {
            m_CurveBackups = null;
        }

        public bool InLiveEdit()
        {
            return m_CurveBackups != null;
        }

        void Init()
        {
        }

        public void OnGUI()
        {
            BeginViewGUI();
            GridGUI();
            DrawWrapperPopups();
            CurveGUI();
            EndViewGUI();
        }

        public void CurveGUI()
        {
            if (m_PointRenderer == null)
                m_PointRenderer = new CurveControlPointRenderer();

            if (m_RectangleTool == null)
            {
                m_RectangleTool = new CurveEditorRectangleTool();
                m_RectangleTool.Initialize(this);
            }

            GUI.BeginGroup(drawRect);

            Init();
            GUIUtility.GetControlID(s_SelectKeyHash, FocusType.Passive);
            GUI.contentColor = GUI.backgroundColor = Color.white;

            Color oldColor = GUI.color;

            Event evt = Event.current;

            //Because this uses a keyboard input field, it must be allowed to handle events first
            if (evt.type != EventType.Repaint)
            {
                EditSelectedPoints();
            }

            switch (evt.type)
            {
                case EventType.ValidateCommand:
                case EventType.ExecuteCommand:
                    bool execute = evt.type == EventType.ExecuteCommand;
                    switch (evt.commandName)
                    {
                        case "Delete":
                            if (hasSelection)
                            {
                                if (execute)
                                {
                                    DeleteSelectedKeys();
                                }
                                evt.Use();
                            }
                            break;
                        case "FrameSelected":
                            if (execute)
                                FrameSelected(true, true);
                            evt.Use();
                            break;
                        case "SelectAll":
                            if (execute)
                                SelectAll();
                            evt.Use();
                            break;
                    }
                    break;
                case EventType.KeyDown:
                    if ((evt.keyCode == KeyCode.Backspace || evt.keyCode == KeyCode.Delete) && hasSelection)
                    {
                        DeleteSelectedKeys();
                        evt.Use();
                    }

                    // Frame All.
                    // Manually handle hotkey unless we decide to add it to default Unity hotkeys like
                    // we did for FrameSelected.
                    if (evt.keyCode == KeyCode.A)
                    {
                        FrameClip(true, true);
                        evt.Use();
                    }
                    break;

                case EventType.ContextClick:
                    CurveSelection mouseKey = FindNearest();
                    if (mouseKey != null)
                    {
                        List<KeyIdentifier> keyList = new List<KeyIdentifier>();

                        // Find out if key under mouse is part of selected keys
                        bool inSelected = false;
                        foreach (CurveSelection sel in selectedCurves)
                        {
                            keyList.Add(new KeyIdentifier(GetCurveFromSelection(sel), sel.curveID, sel.key));
                            if (sel.curveID == mouseKey.curveID && sel.key == mouseKey.key)
                                inSelected = true;
                        }
                        if (!inSelected)
                        {
                            keyList.Clear();
                            keyList.Add(new KeyIdentifier(GetCurveFromSelection(mouseKey), mouseKey.curveID, mouseKey.key));
                            ClearSelection();
                            AddSelection(mouseKey);
                        }

                        bool isEditable = !selectedCurves.Exists(sel => !GetCurveWrapperFromSelection(sel).animationIsEditable);

                        m_MenuManager = new CurveMenuManager(this);
                        GenericMenu menu = new GenericMenu();

                        string deleteKeyLabel = keyList.Count > 1 ? "Delete Keys" : "Delete Key";
                        string editKeyLabel = keyList.Count > 1 ? "Edit Keys..." : "Edit Key...";

                        if (isEditable)
                        {
                            menu.AddItem(new GUIContent(deleteKeyLabel), false, DeleteKeys, keyList);
                            menu.AddItem(new GUIContent(editKeyLabel), false, StartEditingSelectedPointsContext, OffsetMousePositionInDrawing(GetCurveWrapperFromSelection(mouseKey)));
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent(deleteKeyLabel));
                            menu.AddDisabledItem(new GUIContent(editKeyLabel));
                        }

                        if (isEditable)
                        {
                            menu.AddSeparator("");
                            m_MenuManager.AddTangentMenuItems(menu, keyList);
                        }

                        menu.ShowAsContext();
                        Event.current.Use();
                    }
                    break;
            }

            GUI.color = oldColor;

            m_RectangleTool.HandleOverlayEvents();

            DragTangents();

            m_RectangleTool.HandleEvents();

            EditAxisLabels();
            SelectPoints();

            EditorGUI.BeginChangeCheck();
            Vector2 move = MovePoints();
            if (EditorGUI.EndChangeCheck() && m_DraggingKey != null)
            {
                m_MoveCoord = move;
            }

            if (evt.type == EventType.Repaint)
            {
                DrawCurves();

                m_RectangleTool.OnGUI();

                DrawCurvesTangents();
                DrawCurvesOverlay();

                m_RectangleTool.OverlayOnGUI();

                EditSelectedPoints();
            }

            GUI.color = oldColor;

            GUI.EndGroup();
        }

        // Recalculate curve.selected from selected curves
        void RecalcCurveSelection()
        {
            // Reset selection state of all curves
            foreach (CurveWrapper cw in m_AnimationCurves)
                cw.selected = CurveWrapper.SelectionMode.None;

            // Now sync with selected curves
            foreach (CurveSelection cs in selectedCurves)
            {
                CurveWrapper cw = GetCurveWrapperFromSelection(cs);
                if (cw != null)
                    cw.selected = cs.semiSelected ? CurveWrapper.SelectionMode.SemiSelected : CurveWrapper.SelectionMode.Selected;
            }
        }

        void RecalcSecondarySelection()
        {
            // No need to recalculate secondary selection if there are no curves with a valid groupId.
            if (!m_EnableCurveGroups)
                return;

            // The new list of secondary selections
            List<CurveSelection> newSelection = new List<CurveSelection>(selectedCurves.Count);

            // Go through selection, find curveselections that need syncing, add those for the sync points.
            foreach (CurveSelection cs in selectedCurves)
            {
                CurveWrapper cw = GetCurveWrapperFromSelection(cs);
                if (cw == null)
                    continue;

                int groupId = cw.groupId;
                if (groupId != -1 && !cs.semiSelected)
                {
                    newSelection.Add(cs);
                    foreach (CurveWrapper cw2 in m_AnimationCurves)
                    {
                        if (cw2.groupId == groupId && cw2 != cw)
                        {
                            CurveSelection newCS = new CurveSelection(cw2.id, cs.key);
                            newCS.semiSelected = true;
                            newSelection.Add(newCS);
                        }
                    }
                }
                else
                {
                    newSelection.Add(cs);
                }
            }
            newSelection.Sort();

            // the selection can contain duplicate keys. We go through the selection and remove any duplicates we find.
            // Since the selection is already sorted, the duplicates are next to each other.
            for (int i = 0; i < newSelection.Count - 1;)
            {
                CurveSelection cs1 = newSelection[i];
                CurveSelection cs2 = newSelection[i + 1];
                if (cs1.curveID == cs2.curveID && cs1.key == cs2.key)
                {
                    // If we have a collision, one can be fully selected, while the other can be semiselected. Make sure we always get the most selected one.
                    if (!cs1.semiSelected || !cs2.semiSelected)
                        cs1.semiSelected = false;
                    newSelection.RemoveAt(i + 1);
                }
                else
                {
                    i++;
                }
            }

            // Assign back
            selectedCurves = newSelection;
        }

        void DragTangents()
        {
            Event evt = Event.current;
            int tangentId = GUIUtility.GetControlID(s_TangentControlIDHash, FocusType.Passive);
            switch (evt.GetTypeForControl(tangentId))
            {
                case EventType.MouseDown:
                    if (evt.button == 0 && !evt.alt)
                    {
                        m_SelectedTangentPoint = null;
                        float nearestDist = kMaxPickDistSqr;
                        Vector2 mousePos = Event.current.mousePosition;
                        foreach (CurveSelection cs in selectedCurves)
                        {
                            CurveWrapper curveWrapper = GetCurveWrapperFromSelection(cs);
                            if (curveWrapper == null)
                                continue;

                            if (IsLeftTangentEditable(cs))
                            {
                                CurveSelection tangent = new CurveSelection(cs.curveID, cs.key, CurveSelection.SelectionType.InTangent);
                                float d = (DrawingToOffsetViewTransformPoint(curveWrapper, GetPosition(tangent)) - mousePos).sqrMagnitude;
                                if (d <= nearestDist)
                                {
                                    m_SelectedTangentPoint = tangent;
                                    nearestDist = d;
                                }
                            }

                            if (IsRightTangentEditable(cs))
                            {
                                CurveSelection tangent = new CurveSelection(cs.curveID, cs.key, CurveSelection.SelectionType.OutTangent);
                                float d = (DrawingToOffsetViewTransformPoint(curveWrapper, GetPosition(tangent)) - mousePos).sqrMagnitude;
                                if (d <= nearestDist)
                                {
                                    m_SelectedTangentPoint = tangent;
                                    nearestDist = d;
                                }
                            }
                        }

                        if (m_SelectedTangentPoint != null)
                        {
                            SaveKeySelection("Edit Curve");

                            GUIUtility.hotControl = tangentId;
                            evt.Use();
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == tangentId)
                    {
                        CurveSelection dragged = m_SelectedTangentPoint;
                        CurveWrapper curveWrapper = GetCurveWrapperFromSelection(dragged);

                        if ((curveWrapper != null) && curveWrapper.animationIsEditable)
                        {
                            Vector2 newPosition = OffsetMousePositionInDrawing(curveWrapper);
                            Keyframe key = GetKeyframeFromSelection(dragged);

                            if (dragged.type == CurveSelection.SelectionType.InTangent)
                            {
                                Vector2 tangentDirection = newPosition - new Vector2(key.time, key.value);
                                if (tangentDirection.x < -0.0001F)
                                    key.inTangent = tangentDirection.y / tangentDirection.x;
                                else
                                    key.inTangent = Mathf.Infinity;
                                AnimationUtility.SetKeyLeftTangentMode(ref key, TangentMode.Free);

                                if (!AnimationUtility.GetKeyBroken(key))
                                {
                                    key.outTangent = key.inTangent;
                                    AnimationUtility.SetKeyRightTangentMode(ref key, TangentMode.Free);
                                }
                            }
                            else if (dragged.type == CurveSelection.SelectionType.OutTangent)
                            {
                                Vector2 tangentDirection = newPosition - new Vector2(key.time, key.value);
                                if (tangentDirection.x > 0.0001F)
                                    key.outTangent = tangentDirection.y / tangentDirection.x;
                                else
                                    key.outTangent = Mathf.Infinity;
                                AnimationUtility.SetKeyRightTangentMode(ref key, TangentMode.Free);

                                if (!AnimationUtility.GetKeyBroken(key))
                                {
                                    key.inTangent = key.outTangent;
                                    AnimationUtility.SetKeyLeftTangentMode(ref key, TangentMode.Free);
                                }
                            }

                            dragged.key = curveWrapper.MoveKey(dragged.key, ref key);
                            AnimationUtility.UpdateTangentsFromModeSurrounding(curveWrapper.curve, dragged.key);

                            curveWrapper.changed = true;
                            GUI.changed = true;
                        }
                        Event.current.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == tangentId)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    if (GUIUtility.hotControl == tangentId)
                    {
                        Rect mouseRect = new Rect(evt.mousePosition.x - 10, evt.mousePosition.y - 10, 20, 20);
                        EditorGUIUtility.AddCursorRect(mouseRect, MouseCursor.MoveArrow);
                    }
                    break;
            }
        }

        struct KeyFrameCopy
        {
            public float time, value, inTangent, outTangent;
            public int idx, selectionIdx;
            public KeyFrameCopy(int idx, int selectionIdx, Keyframe source)
            {
                this.idx = idx;
                this.selectionIdx = selectionIdx;
                time = source.time;
                value = source.value;
                inTangent = source.inTangent;
                outTangent = source.outTangent;
            }
        }

        internal void DeleteSelectedKeys()
        {
            string undoLabel;
            if (selectedCurves.Count > 1)
                undoLabel = "Delete Keys";
            else
                undoLabel = "Delete Key";

            SaveKeySelection(undoLabel);

            // Go over selection backwards and delete (avoids wrecking indices)
            for (int i = selectedCurves.Count - 1; i >= 0; i--)
            {
                CurveSelection k = selectedCurves[i];
                CurveWrapper cw = GetCurveWrapperFromSelection(k);
                if (cw == null)
                    continue;

                if (!cw.animationIsEditable)
                    continue;

                if (!settings.allowDeleteLastKeyInCurve)
                    if (cw.curve.keys.Length == 1)
                        continue;

                cw.curve.RemoveKey(k.key);
                AnimationUtility.UpdateTangentsFromMode(cw.curve);
                cw.changed = true;
                GUI.changed = true;
            }
            SelectNone();
        }

        private void DeleteKeys(object obj)
        {
            List<KeyIdentifier> keyList = (List<KeyIdentifier>)obj;

            string undoLabel;
            if (keyList.Count > 1)
                undoLabel = "Delete Keys";
            else
                undoLabel = "Delete Key";

            SaveKeySelection(undoLabel);

            // Go over selection backwards and delete (avoids wrecking indices)
            List<int> curveIds = new List<int>();
            for (int i = keyList.Count - 1; i >= 0; i--)
            {
                if (!settings.allowDeleteLastKeyInCurve)
                    if (keyList[i].curve.keys.Length == 1)
                        continue;

                if (!GetCurveWrapperFromID(keyList[i].curveId).animationIsEditable)
                    continue;

                keyList[i].curve.RemoveKey(keyList[i].key);
                AnimationUtility.UpdateTangentsFromMode(keyList[i].curve);
                curveIds.Add(keyList[i].curveId);
            }

            UpdateCurves(curveIds, undoLabel);
            SelectNone();
        }

        float ClampVerticalValue(float value, int curveID)
        {
            // Clamp by global value
            value = Mathf.Clamp(value, vRangeMin, vRangeMax);

            // Clamp with per curve settings.
            CurveWrapper cw = GetCurveWrapperFromID(curveID);
            if (cw != null)
                value = Mathf.Clamp(value, cw.vRangeMin, cw.vRangeMax);

            return value;
        }

        internal void TranslateSelectedKeys(Vector2 movement)
        {
            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            UpdateCurvesFromPoints(
                delegate(SavedCurve.SavedKeyFrame keyframe, SavedCurve curve)
                {
                    if (keyframe.selected != CurveWrapper.SelectionMode.None)
                    {
                        // Duplicate it - so we don't modify the backup copy
                        var duplicateKeyframe = keyframe.Clone();

                        // Slide in time.  Clamp key.
                        duplicateKeyframe.key.time = Mathf.Clamp(duplicateKeyframe.key.time + movement.x, hRangeMin, hRangeMax);

                        // if it's fully selected, also move on Y
                        if (duplicateKeyframe.selected == CurveWrapper.SelectionMode.Selected)
                            duplicateKeyframe.key.value = ClampVerticalValue(duplicateKeyframe.key.value + movement.y, curve.curveId);

                        return duplicateKeyframe;
                    }

                    return keyframe;
                }
                );

            if (!inLiveEdit)
                EndLiveEdit();
        }

        internal void SetSelectedKeyPositions(float newTime, float newValue, bool updateTime, bool updateValue)
        {
            if (!updateTime && !updateValue)
                return;

            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            UpdateCurvesFromPoints(
                delegate(SavedCurve.SavedKeyFrame keyframe, SavedCurve curve)
                {
                    if (keyframe.selected != CurveWrapper.SelectionMode.None)
                    {
                        // Duplicate it - so we don't modify the backup copy
                        var duplicateKeyframe = keyframe.Clone();

                        if (updateTime)
                        {
                            duplicateKeyframe.key.time = Mathf.Clamp(newTime, hRangeMin, hRangeMax);
                        }
                        if (updateValue)
                        {
                            duplicateKeyframe.key.value = ClampVerticalValue(newValue, curve.curveId);
                        }

                        return duplicateKeyframe;
                    }

                    return keyframe;
                }
                );

            if (!inLiveEdit)
                EndLiveEdit();
        }

        internal void TransformSelectedKeys(Matrix4x4 matrix, bool flipX, bool flipY)
        {
            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            UpdateCurvesFromPoints(

                delegate(SavedCurve.SavedKeyFrame keyframe, SavedCurve curve)
                {
                    if (keyframe.selected != CurveWrapper.SelectionMode.None)
                    {
                        // Duplicate it - so we don't modify the backup copy
                        var duplicateKeyframe = keyframe.Clone();

                        Vector3 v = new Vector3(duplicateKeyframe.key.time, duplicateKeyframe.key.value, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        v.x = SnapTime(v.x);

                        duplicateKeyframe.key.time = Mathf.Clamp(v.x, hRangeMin, hRangeMax);

                        if (flipX)
                        {
                            duplicateKeyframe.key.inTangent = (keyframe.key.outTangent != Mathf.Infinity) ? -keyframe.key.outTangent : Mathf.Infinity;
                            duplicateKeyframe.key.outTangent = (keyframe.key.inTangent != Mathf.Infinity) ? -keyframe.key.inTangent : Mathf.Infinity;
                        }

                        // if it's fully selected, also move on Y
                        if (duplicateKeyframe.selected == CurveWrapper.SelectionMode.Selected)
                        {
                            duplicateKeyframe.key.value = ClampVerticalValue(v.y, curve.curveId);

                            if (flipY)
                            {
                                duplicateKeyframe.key.inTangent = (duplicateKeyframe.key.inTangent != Mathf.Infinity) ? -duplicateKeyframe.key.inTangent : Mathf.Infinity;
                                duplicateKeyframe.key.outTangent = (duplicateKeyframe.key.outTangent != Mathf.Infinity) ? -duplicateKeyframe.key.outTangent : Mathf.Infinity;
                            }
                        }

                        return duplicateKeyframe;
                    }
                    return keyframe;
                }
                );

            if (!inLiveEdit)
                EndLiveEdit();
        }

        internal void TransformRippleKeys(Matrix4x4 matrix, float t1, float t2, bool flipX)
        {
            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            UpdateCurvesFromPoints(

                delegate(SavedCurve.SavedKeyFrame keyframe, SavedCurve curve)
                {
                    float newTime = keyframe.key.time;

                    if (keyframe.selected != CurveWrapper.SelectionMode.None)
                    {
                        Vector3 v = new Vector3(keyframe.key.time, 0f, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        newTime = v.x;

                        // Duplicate it - so we don't modify the backup copy
                        var duplicateKeyframe = keyframe.Clone();

                        duplicateKeyframe.key.time = SnapTime(Mathf.Clamp(newTime, hRangeMin, hRangeMax));

                        if (flipX)
                        {
                            duplicateKeyframe.key.inTangent = (keyframe.key.outTangent != Mathf.Infinity) ? -keyframe.key.outTangent : Mathf.Infinity;
                            duplicateKeyframe.key.outTangent = (keyframe.key.inTangent != Mathf.Infinity) ? -keyframe.key.inTangent : Mathf.Infinity;
                        }

                        return duplicateKeyframe;
                    }
                    else
                    {
                        if (keyframe.key.time > t2)
                        {
                            Vector3 v = new Vector3(flipX ? t1 : t2, 0f, 0f);
                            v = matrix.MultiplyPoint3x4(v);

                            float dt = v.x - t2;
                            if (dt > 0f)
                                newTime = keyframe.key.time + dt;
                        }
                        else if (keyframe.key.time < t1)
                        {
                            Vector3 v = new Vector3(flipX ? t2 : t1, 0f, 0f);
                            v = matrix.MultiplyPoint3x4(v);

                            float dt = v.x - t1;
                            if (dt < 0f)
                                newTime = keyframe.key.time + dt;
                        }

                        if (!Mathf.Approximately(newTime, keyframe.key.time))
                        {
                            // Duplicate it - so we don't modify the backup copy
                            var duplicateKeyframe = keyframe.Clone();
                            duplicateKeyframe.key.time = SnapTime(Mathf.Clamp(newTime, hRangeMin, hRangeMax));
                            return duplicateKeyframe;
                        }
                    }

                    return keyframe;
                }
                );

            if (!inLiveEdit)
                EndLiveEdit();
        }

        void UpdateCurvesFromPoints(SavedCurve.KeyFrameOperation action)
        {
            if (m_CurveBackups == null)
                return;

            // Starting up:
            var dragSelection = new List<CurveSelection>();
            // Go over all saved curves - each of these has at least one selected point.
            foreach (SavedCurve sc in m_CurveBackups)
            {
                CurveWrapper cw = GetCurveWrapperFromID(sc.curveId);
                if (!cw.animationIsEditable)
                    continue;

                // Go through each curve and build a new working set of points.
                var working = new SortedList<float, SavedCurve.SavedKeyFrame>(SavedCurve.SavedKeyFrameComparer.Instance);

                //  Add all unselected key frames to the working collection first.
                foreach (SavedCurve.SavedKeyFrame keyframe in sc.keys)
                {
                    if (keyframe.selected == CurveWrapper.SelectionMode.None)
                    {
                        var newKeyframe = action(keyframe, sc);
                        cw.PreProcessKey(ref newKeyframe.key);

                        // We might have moved keys around, let's add new key or replace existing key.
                        working[newKeyframe.key.time] = newKeyframe;
                    }
                }

                //  Add all modified key frames to the working collection and remove duplicates.
                foreach (SavedCurve.SavedKeyFrame keyframe in sc.keys)
                {
                    if (keyframe.selected != CurveWrapper.SelectionMode.None)
                    {
                        var newKeyframe = action(keyframe, sc);
                        cw.PreProcessKey(ref newKeyframe.key);
                        // We might have moved keys around, let's add new key or replace existing key.
                        working[newKeyframe.key.time] = newKeyframe;
                    }
                }

                // Working now contains a set of points with everything set up correctly.
                // Each point has it's selection set, but m_DisplayCurves has a more traditional key array.
                // Go through the working points and sort those for display.
                int idx = 0;
                Keyframe[] keysToAssign = new Keyframe[working.Count];
                foreach (KeyValuePair<float, SavedCurve.SavedKeyFrame> kvp in working)
                {
                    SavedCurve.SavedKeyFrame sk = kvp.Value;
                    keysToAssign[idx] = sk.key;
                    if (sk.selected != CurveWrapper.SelectionMode.None)
                    {
                        CurveSelection cs = new CurveSelection(sc.curveId, idx);
                        if (sk.selected == CurveWrapper.SelectionMode.SemiSelected)
                            cs.semiSelected = true;
                        dragSelection.Add(cs);
                    }

                    ++idx;
                }

                // We now have the list of keys to assign - let's get them back into the animation clip
                cw.curve.keys = keysToAssign;
                AnimationUtility.UpdateTangentsFromMode(cw.curve);
                cw.changed = true;
            }

            selectedCurves = dragSelection;
        }

        float SnapTime(float t)
        {
            if (EditorGUI.actionKey)
            {
                int snapLevel = hTicks.GetLevelWithMinSeparation(5);
                float snap = hTicks.GetPeriodOfLevel(snapLevel);
                t = Mathf.Round(t / snap) * snap;
            }
            else
            {
                if (invSnap != 0.0f)
                    t = Mathf.Round(t * invSnap) / invSnap;
            }
            return t;
        }

        float SnapValue(float v)
        {
            if (EditorGUI.actionKey)
            {
                int snapLevel = vTicks.GetLevelWithMinSeparation(5);
                float snap = vTicks.GetPeriodOfLevel(snapLevel);
                v = Mathf.Round(v / snap) * snap;
            }
            return v;
        }

        /*string DebugSelection ()
        {
            string s = "";
            foreach (int i in m_PointSelection)
                s += i + ", ";
            s += "\n";
            foreach (CurveSelection k in selectedCurves)
                s += "[" + k.curveID + ", " + k.key+ "], ";
            return s;
        }*/

        new internal static class Styles
        {
            public static Texture2D pointIcon = EditorGUIUtility.LoadIcon("curvekeyframe");
            public static Texture2D pointIconSelected = EditorGUIUtility.LoadIcon("curvekeyframeselected");
            public static Texture2D pointIconSelectedOverlay = EditorGUIUtility.LoadIcon("curvekeyframeselectedoverlay");
            public static Texture2D pointIconSemiSelectedOverlay = EditorGUIUtility.LoadIcon("curvekeyframesemiselectedoverlay");
            public static GUIContent wrapModeMenuIcon = EditorGUIUtility.IconContent("AnimationWrapModeMenu");

            public static GUIStyle none = new GUIStyle();
            public static GUIStyle labelTickMarksY = "CurveEditorLabelTickMarks";
            public static GUIStyle labelTickMarksX;
            public static GUIStyle selectionRect = "SelectionRect";

            public static GUIStyle dragLabel = "ProfilerBadge";
            public static GUIStyle axisLabelNumberField = new GUIStyle(EditorStyles.miniTextField);
            public static GUIStyle rightAlignedLabel = new GUIStyle(EditorStyles.label);

            static Styles()
            {
                axisLabelNumberField.alignment = TextAnchor.UpperRight;
                labelTickMarksY.contentOffset = Vector2.zero; // TODO: Fix this in style when Editor has been merged to Trunk (31/8/2011)
                labelTickMarksX = new GUIStyle(labelTickMarksY);
                labelTickMarksX.clipping = TextClipping.Overflow;
                rightAlignedLabel.alignment = TextAnchor.UpperRight;
            }
        }

        Vector2 GetGUIPoint(CurveWrapper cw, Vector3 point)
        {
            return HandleUtility.WorldToGUIPoint(DrawingToOffsetViewTransformPoint(cw, point));
        }

        Rect GetWorldRect(CurveWrapper cw, Rect rect)
        {
            Vector2 min = GetWorldPoint(cw, rect.min);
            Vector2 max = GetWorldPoint(cw, rect.max);

            // Invert y world coordinates.
            return Rect.MinMaxRect(min.x, max.y, max.x, min.y);
        }

        Vector2 GetWorldPoint(CurveWrapper cw, Vector2 point)
        {
            return OffsetViewToDrawingTransformPoint(cw, point);
        }

        Rect GetCurveRect(CurveWrapper cw)
        {
            Bounds bounds = cw.bounds;
            return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
        }

        Vector2 s_StartMouseDragPosition, s_EndMouseDragPosition, s_StartKeyDragPosition;
        PickMode s_PickMode;

        int OnlyOneEditableCurve()
        {
            int index = -1;
            int curves = 0;
            for (int i = 0; i < m_AnimationCurves.Length; i++)
            {
                CurveWrapper wrapper = m_AnimationCurves[i];
                if (wrapper.hidden || wrapper.readOnly)
                    continue;
                curves++;
                index = i;
            }
            if (curves == 1)
                return index;
            else
                return -1;
        }

        // Returns an index into m_AnimationCurves
        int GetCurveAtPosition(Vector2 viewPos, out Vector2 closestPointOnCurve)
        {
            // Find the closest curve at the time corresponding to the position
            int maxPixelDist = (int)Mathf.Sqrt(kMaxPickDistSqr);
            float smallestDist = kMaxPickDistSqr;
            int closest = -1;
            closestPointOnCurve = Vector3.zero;

            // Use drawOrder to ensure we pick the topmost curve
            for (int i = m_DrawOrder.Count - 1; i >= 0; --i)
            {
                CurveWrapper wrapper = GetCurveWrapperFromID(m_DrawOrder[i]);

                if (wrapper.hidden || wrapper.readOnly)
                    continue;

                Vector2 localPos = OffsetViewToDrawingTransformPoint(wrapper, viewPos);

                // Sample the curves at pixel intervals in the area around the desired time,
                // corresponding to the max cursor distance allowed.
                Vector2 valL;
                valL.x = localPos.x - maxPixelDist / scale.x;
                valL.y = wrapper.renderer.EvaluateCurveSlow(valL.x);
                valL = DrawingToOffsetViewTransformPoint(wrapper, valL);
                for (int x = -maxPixelDist; x < maxPixelDist; x++)
                {
                    Vector2 valR;
                    valR.x = localPos.x + (x + 1) / scale.x;
                    valR.y = wrapper.renderer.EvaluateCurveSlow(valR.x);
                    valR = DrawingToOffsetViewTransformPoint(wrapper, valR);

                    float dist = HandleUtility.DistancePointLine(viewPos, valL, valR);
                    dist = dist * dist;
                    if (dist < smallestDist)
                    {
                        smallestDist = dist;
                        closest = wrapper.listIndex;
                        closestPointOnCurve = HandleUtility.ProjectPointLine(viewPos, valL, valR);
                    }

                    valL = valR;
                }
            }

            if (closest >= 0)
                closestPointOnCurve = OffsetViewToDrawingTransformPoint(m_AnimationCurves[closest], closestPointOnCurve);
            return closest;
        }

        void CreateKeyFromClick(object obj)
        {
            string undoLabel = "Add Key";
            SaveKeySelection(undoLabel);

            List<int> ids = CreateKeyFromClick((Vector2)obj);
            if (ids.Count > 0)
                UpdateCurves(ids, undoLabel);
        }

        List<int> CreateKeyFromClick(Vector2 viewPos)
        {
            List<int> curveIds = new List<int>();

            // Check if there is only one curve to edit
            int singleCurveIndex = OnlyOneEditableCurve();
            if (singleCurveIndex >= 0)
            {
                // If there is only one curve, allow creating keys on it by double/right-clicking anywhere
                // if the click is to the left or right of the existing keys, or if there are no existing keys.
                CurveWrapper cw = m_AnimationCurves[singleCurveIndex];
                Vector2 localPos = OffsetViewToDrawingTransformPoint(cw, viewPos);
                float time = localPos.x - cw.timeOffset;
                if (cw.curve.keys.Length == 0 || time < cw.curve.keys[0].time || time > cw.curve.keys[cw.curve.keys.Length - 1].time)
                {
                    if (CreateKeyFromClick(singleCurveIndex, localPos))
                        curveIds.Add(cw.id);
                    return curveIds;
                }
            }

            // If we didn't create a key above, only allow creating keys
            // when double/right-clicking on an existing curve
            Vector2 closestPointOnCurve;
            int curveIndex = GetCurveAtPosition(viewPos, out closestPointOnCurve);
            if (CreateKeyFromClick(curveIndex, closestPointOnCurve.x))
            {
                if (curveIndex >= 0)
                    curveIds.Add(m_AnimationCurves[curveIndex].id);
            }
            return curveIds;
        }

        bool CreateKeyFromClick(int curveIndex, float time)
        {
            time = Mathf.Clamp(time, settings.hRangeMin, settings.hRangeMax);

            // Add a key on a curve at a specified time
            if (curveIndex >= 0)
            {
                CurveSelection selectedPoint = null;
                CurveWrapper cw = m_AnimationCurves[curveIndex];

                if (cw.animationIsEditable)
                {
                    if (cw.groupId == -1)
                    {
                        selectedPoint = AddKeyAtTime(cw, time);
                    }
                    else
                    {
                        foreach (CurveWrapper cw2 in m_AnimationCurves)
                        {
                            if (cw2.groupId == cw.groupId)
                            {
                                CurveSelection cs = AddKeyAtTime(cw2, time);
                                if (cw2.id == cw.id)
                                    selectedPoint = cs;
                            }
                        }
                    }
                    if (selectedPoint != null)
                    {
                        ClearSelection();
                        AddSelection(selectedPoint);
                        RecalcSecondarySelection();
                    }
                    else
                    {
                        SelectNone();
                    }

                    return true;
                }
            }

            return false;
        }

        bool CreateKeyFromClick(int curveIndex, Vector2 localPos)
        {
            localPos.x = Mathf.Clamp(localPos.x, settings.hRangeMin, settings.hRangeMax);

            // Add a key on a curve at a specified time
            if (curveIndex >= 0)
            {
                CurveSelection selectedPoint = null;
                CurveWrapper cw = m_AnimationCurves[curveIndex];

                if (cw.animationIsEditable)
                {
                    if (cw.groupId == -1)
                    {
                        selectedPoint = AddKeyAtPosition(cw, localPos);
                    }
                    else
                    {
                        foreach (CurveWrapper cw2 in m_AnimationCurves)
                        {
                            if (cw2.groupId == cw.groupId)
                            {
                                if (cw2.id == cw.id)
                                    selectedPoint = AddKeyAtPosition(cw2, localPos);
                                else
                                    AddKeyAtTime(cw2, localPos.x);
                            }
                        }
                    }
                    if (selectedPoint != null)
                    {
                        ClearSelection();
                        AddSelection(selectedPoint);
                        RecalcSecondarySelection();
                    }
                    else
                    {
                        SelectNone();
                    }

                    return true;
                }
            }

            return false;
        }

        public void AddKey(CurveWrapper cw, Keyframe key)
        {
            CurveSelection selectedPoint = AddKeyframeAndSelect(key, cw);

            if (selectedPoint != null)
            {
                ClearSelection();
                AddSelection(selectedPoint);
                RecalcSecondarySelection();
            }
            else
            {
                SelectNone();
            }
        }

        // Add a key to cw at time.
        // Returns the inserted key as a curveSelection
        CurveSelection AddKeyAtTime(CurveWrapper cw, float time)
        {
            // Find out if there's already a key there
            time = SnapTime(time);
            float halfFrame;
            if (invSnap != 0.0f)
                halfFrame = 0.5f / invSnap;
            else
                halfFrame = 0.0001f;
            if (CurveUtility.HaveKeysInRange(cw.curve, time - halfFrame, time + halfFrame))
                return null;

            // Add the key
            float slope = cw.renderer.EvaluateCurveDeltaSlow(time);
            float value = ClampVerticalValue(SnapValue(cw.renderer.EvaluateCurveSlow(time)), cw.id);
            Keyframe key = new Keyframe(time, value, slope, slope);
            return AddKeyframeAndSelect(key, cw);
        }

        // Add a key to cw at time.
        // Returns the inserted key as a curveSelection
        CurveSelection AddKeyAtPosition(CurveWrapper cw, Vector2 position)
        {
            // Find out if there's already a key there
            position.x = SnapTime(position.x);
            float halfFrame;
            if (invSnap != 0.0f)
                halfFrame = 0.5f / invSnap;
            else
                halfFrame = 0.0001f;
            if (CurveUtility.HaveKeysInRange(cw.curve, position.x - halfFrame, position.x + halfFrame))
                return null;

            // Add the key
            float slope = 0;
            Keyframe key = new Keyframe(position.x, SnapValue(position.y), slope, slope);
            return AddKeyframeAndSelect(key, cw);
        }

        CurveSelection AddKeyframeAndSelect(Keyframe key, CurveWrapper cw)
        {
            if (!cw.animationIsEditable)
                return null;

            int keyIndex = cw.AddKey(key);
            CurveUtility.SetKeyModeFromContext(cw.curve, keyIndex);
            AnimationUtility.UpdateTangentsFromModeSurrounding(cw.curve, keyIndex);

            // Select the key
            CurveSelection selectedPoint = new CurveSelection(cw.id, keyIndex);
            cw.selected = CurveWrapper.SelectionMode.Selected;
            cw.changed = true;

            return selectedPoint;
        }

        // Find keyframe nearest to the mouse. We use the draw order to ensure to return the
        // key that is topmost rendered if several keys are overlapping. The user can
        // click on another curve to bring it to front and hereby be able to better select its keys.
        // Returns null if nothing is within Sqrt(kMaxPickDistSqr) pixels.
        CurveSelection FindNearest()
        {
            Vector2 mousePos = Event.current.mousePosition;

            bool foundCurve = false;
            int bestCurveID = -1;
            int bestKey = -1;
            float nearestDist = kMaxPickDistSqr;

            // Last element in draw order list is topmost so reverse traverse list
            for (int index = m_DrawOrder.Count - 1; index >= 0; --index)
            {
                CurveWrapper cw = GetCurveWrapperFromID(m_DrawOrder[index]);
                if (cw.readOnly || cw.hidden)
                    continue;

                for (int i = 0; i < cw.curve.keys.Length; ++i)
                {
                    Keyframe k = cw.curve.keys[i];
                    float d = (GetGUIPoint(cw, new Vector2(k.time, k.value)) - mousePos).sqrMagnitude;
                    // If we have an exact hit we just return that key
                    if (d <= kExactPickDistSqr)
                        return new CurveSelection(cw.id, i);

                    // Otherwise find closest
                    if (d < nearestDist)
                    {
                        foundCurve = true;
                        bestCurveID = cw.id;
                        bestKey = i;
                        nearestDist = d;
                    }
                }
                // If top curve has key within range make it harder for keys below to get selected
                if (index == m_DrawOrder.Count - 1 && bestCurveID >= 0)
                    nearestDist = kExactPickDistSqr;
            }

            if (foundCurve)
                return new CurveSelection(bestCurveID, bestKey);

            return null;
        }

        public void SelectNone()
        {
            ClearSelection();
            foreach (CurveWrapper cw in m_AnimationCurves)
                cw.selected = CurveWrapper.SelectionMode.None;
        }

        public void SelectAll()
        {
            int totalLength = 0;
            foreach (CurveWrapper cw in m_AnimationCurves)
            {
                if (cw.hidden)
                    continue;
                totalLength += cw.curve.length;
            }
            var newSelection = new List<CurveSelection>(totalLength);

            foreach (CurveWrapper cw in m_AnimationCurves)
            {
                cw.selected = CurveWrapper.SelectionMode.Selected;
                for (int j = 0; j < cw.curve.length; j++)
                    newSelection.Add(new CurveSelection(cw.id, j));
            }
            selectedCurves = newSelection;
        }

        public bool IsDraggingKey()
        {
            return m_DraggingKey != null;
        }

        public bool IsDraggingCurveOrRegion()
        {
            return m_DraggingCurveOrRegion != null;
        }

        public bool IsDraggingCurve(CurveWrapper cw)
        {
            return (m_DraggingCurveOrRegion != null && m_DraggingCurveOrRegion.Length == 1 && m_DraggingCurveOrRegion[0] == cw);
        }

        public bool IsDraggingRegion(CurveWrapper cw1, CurveWrapper cw2)
        {
            return (m_DraggingCurveOrRegion != null && m_DraggingCurveOrRegion.Length == 2 && (m_DraggingCurveOrRegion[0] == cw1 || m_DraggingCurveOrRegion[0] == cw2));
        }

        bool HandleCurveAndRegionMoveToFrontOnMouseDown(ref Vector2 timeValue, ref CurveWrapper[] curves)
        {
            // Did we click on a curve
            Vector2 closestPointOnCurve;
            int clickedCurveIndex = GetCurveAtPosition(Event.current.mousePosition, out closestPointOnCurve);
            if (clickedCurveIndex >= 0)
            {
                MoveCurveToFront(m_AnimationCurves[clickedCurveIndex].id);
                timeValue = OffsetMousePositionInDrawing(m_AnimationCurves[clickedCurveIndex]);
                curves = new[] { m_AnimationCurves[clickedCurveIndex] };
                return true;
            }

            // Did we click in a region
            for (int i = m_DrawOrder.Count - 1; i >= 0; --i)
            {
                CurveWrapper cw = GetCurveWrapperFromID(m_DrawOrder[i]);

                if (cw == null)
                    continue;
                if (cw.hidden)
                    continue;
                if (cw.curve.length == 0)
                    continue;

                CurveWrapper cw2 = null;
                if (i > 0)
                    cw2 = GetCurveWrapperFromID(m_DrawOrder[i - 1]);

                if (IsRegion(cw, cw2))
                {
                    Vector2 localPos1 = OffsetMousePositionInDrawing(cw);
                    Vector2 localPos2 = OffsetMousePositionInDrawing(cw2);

                    float v1 = cw.renderer.EvaluateCurveSlow(localPos1.x);
                    float v2 = cw2.renderer.EvaluateCurveSlow(localPos2.x);
                    if (v1 > v2)
                    {
                        float tmp = v1;
                        v1 = v2; v2 = tmp;
                    }
                    if (localPos1.y >= v1 && localPos1.y <= v2)
                    {
                        timeValue = localPos1;
                        curves = new[] {cw, cw2};
                        MoveCurveToFront(cw.id);
                        return true;
                    }
                    i--; // we handled two curves
                }
            }
            return false; // No curves or regions hit
        }

        void SelectPoints()
        {
            int id = GUIUtility.GetControlID(897560, FocusType.Passive);
            Event evt = Event.current;
            bool addToSelection = evt.shift;
            bool toggleSelection = EditorGUI.actionKey;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.ContextClick:
                    Rect drawRectAtOrigin = drawRect;
                    drawRectAtOrigin.x = drawRectAtOrigin.y = 0f;
                    if (drawRectAtOrigin.Contains(Event.current.mousePosition))
                    {
                        Vector2 closestPositionOnCurve;
                        int curveIndex = GetCurveAtPosition(Event.current.mousePosition, out closestPositionOnCurve);
                        if (curveIndex >= 0)
                        {
                            GenericMenu menu = new GenericMenu();
                            if (m_AnimationCurves[curveIndex].animationIsEditable)
                                menu.AddItem(new GUIContent("Add Key"), false, CreateKeyFromClick, Event.current.mousePosition);
                            else
                                menu.AddDisabledItem(new GUIContent("Add Key"));

                            menu.ShowAsContext();
                            Event.current.Use();
                        }
                    }
                    break;

                case EventType.MouseDown:
                    if (evt.clickCount == 2 && evt.button == 0)
                    {
                        CurveSelection selectedPoint = FindNearest();

                        if (selectedPoint != null)
                        {
                            if (!addToSelection)
                            {
                                ClearSelection();
                            }

                            AnimationCurve curve = GetCurveFromSelection(selectedPoint);
                            if (curve != null)
                            {
                                BeginRangeSelection();
                                for (int keyIndex = 0; keyIndex < curve.keys.Length; ++keyIndex)
                                {
                                    if (!selectedCurves.Any(x => x.curveID == selectedPoint.curveID && x.key == keyIndex))
                                    {
                                        var keySelection = new CurveSelection(selectedPoint.curveID, keyIndex);
                                        AddSelection(keySelection);
                                    }
                                }
                                EndRangeSelection();
                            }
                        }
                        else
                        {
                            SaveKeySelection("Add Key");

                            List<int> curveIds = CreateKeyFromClick(Event.current.mousePosition);
                            if (curveIds.Count > 0)
                            {
                                foreach (int curveId in curveIds)
                                {
                                    CurveWrapper cw = GetCurveWrapperFromID(curveId);
                                    cw.changed = true;
                                }
                                GUI.changed = true;
                            }
                        }
                        evt.Use();
                    }
                    else if (evt.button == 0)
                    {
                        CurveSelection selectedPoint = FindNearest();
                        if (selectedPoint == null || selectedPoint.semiSelected)
                        {
                            // If we did not hit a key then check if a curve or region was clicked
                            Vector2 timeValue = Vector2.zero;
                            CurveWrapper[] curves = null;
                            var curveOrRegionClicked = HandleCurveAndRegionMoveToFrontOnMouseDown(ref timeValue, ref curves);

                            if (!addToSelection && !toggleSelection && !curveOrRegionClicked)
                            {
                                SelectNone();
                            }

                            GUIUtility.hotControl = id;
                            s_EndMouseDragPosition = s_StartMouseDragPosition = evt.mousePosition;
                            s_PickMode = PickMode.Click;

                            // case 845553 event will be processed afterwards when dragging curve.
                            if (!curveOrRegionClicked)
                                evt.Use();
                        }
                        else
                        {
                            MoveCurveToFront(selectedPoint.curveID);

                            Keyframe selectedKeyframe = GetKeyframeFromSelection(selectedPoint);
                            s_StartKeyDragPosition = new Vector2(selectedKeyframe.time, selectedKeyframe.value);

                            if (addToSelection)
                            {
                                // Isolate range key indices in current selection.
                                bool addRangeToSelection = false;
                                int keyMin = selectedPoint.key;
                                int keyMax = selectedPoint.key;
                                for (int i = 0; i < selectedCurves.Count; ++i)
                                {
                                    CurveSelection cs = selectedCurves[i];
                                    if (cs.curveID == selectedPoint.curveID)
                                    {
                                        addRangeToSelection = true;
                                        keyMin = Mathf.Min(keyMin, cs.key);
                                        keyMax = Mathf.Max(keyMax, cs.key);
                                    }
                                }

                                if (!addRangeToSelection)
                                {
                                    if (!selectedCurves.Contains(selectedPoint))
                                    {
                                        AddSelection(selectedPoint);
                                    }
                                }
                                else
                                {
                                    // Try and add all keys on the same curve in the range [keyMin, keyMax]
                                    BeginRangeSelection();
                                    for (var keyIndex = keyMin; keyIndex <= keyMax; ++keyIndex)
                                    {
                                        if (!selectedCurves.Any(x => x.curveID == selectedPoint.curveID && x.key == keyIndex))
                                        {
                                            var rangeSelection = new CurveSelection(selectedPoint.curveID, keyIndex);
                                            AddSelection(rangeSelection);
                                        }
                                    }
                                    EndRangeSelection();
                                }
                                Event.current.Use();
                            }
                            else if (toggleSelection)
                            {
                                if (!selectedCurves.Contains(selectedPoint))
                                {
                                    AddSelection(selectedPoint);
                                }
                                else
                                {
                                    RemoveSelection(selectedPoint);
                                }
                                Event.current.Use();
                            }
                            else if (!selectedCurves.Contains(selectedPoint))
                            {
                                ClearSelection();
                                AddSelection(selectedPoint);
                            }

                            RecalcSecondarySelection();
                            RecalcCurveSelection();
                        }
                        HandleUtility.Repaint();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_EndMouseDragPosition = evt.mousePosition;
                        if (s_PickMode == PickMode.Click)
                        {
                            s_PickMode = PickMode.Marquee;
                            if (addToSelection || toggleSelection)
                                s_SelectionBackup = new List<CurveSelection>(selectedCurves);
                            else
                                s_SelectionBackup = new List<CurveSelection>();
                        }
                        else
                        {
                            Rect r = EditorGUIExt.FromToRect(s_StartMouseDragPosition, evt.mousePosition);

                            List<CurveSelection> newSelection = new List<CurveSelection>(s_SelectionBackup);
                            for (int i = 0; i < m_AnimationCurves.Length; ++i)
                            {
                                CurveWrapper cw = m_AnimationCurves[i];
                                if (cw.readOnly || cw.hidden)
                                    continue;

                                Rect worldRect = GetWorldRect(cw, r);
                                if (!GetCurveRect(cw).Overlaps(worldRect))
                                    continue;

                                int keyIndex = 0;
                                foreach (Keyframe k in cw.curve.keys)
                                {
                                    if (worldRect.Contains(new Vector2(k.time, k.value)))
                                        newSelection.Add(new CurveSelection(cw.id, keyIndex));
                                    ++keyIndex;
                                }
                            }
                            selectedCurves = newSelection;

                            // We need to sort selection since we're mixing existing selection with new selection.
                            if (s_SelectionBackup.Count > 0)
                                selectedCurves.Sort();

                            RecalcSecondarySelection();
                            RecalcCurveSelection();
                        }
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        if (s_PickMode != PickMode.Click)
                        {
                            // Move selected curves to front.
                            var processedCurves = new HashSet<int>();
                            for (int i = 0; i < selectedCurves.Count; ++i)
                            {
                                CurveWrapper cw = GetCurveWrapperFromSelection(selectedCurves[i]);
                                if (!processedCurves.Contains(cw.id))
                                {
                                    MoveCurveToFront(cw.id);
                                    processedCurves.Add(cw.id);
                                }
                            }
                        }

                        GUIUtility.hotControl = 0;
                        s_PickMode = PickMode.None;

                        Event.current.Use();
                    }
                    break;
            }

            if (s_PickMode == PickMode.Marquee)
            {
                GUI.Label(EditorGUIExt.FromToRect(s_StartMouseDragPosition, s_EndMouseDragPosition), GUIContent.none, Styles.selectionRect);
            }
        }

        string m_AxisLabelFormat = "n1";

        private void EditAxisLabels()
        {
            int id = GUIUtility.GetControlID(18975602, FocusType.Keyboard);

            List<CurveWrapper> curvesInSameSpace = new List<CurveWrapper>();
            Vector2 axisUiScalars = GetAxisUiScalars(curvesInSameSpace);
            bool isEditable = axisUiScalars.y >= 0 && curvesInSameSpace.Count > 0 && curvesInSameSpace[0].setAxisUiScalarsCallback != null;
            if (!isEditable)
                return;


            Rect editRect = new Rect(0, topmargin - 8, leftmargin - 4, 16);
            Rect dragRect = editRect;
            dragRect.y -= editRect.height;

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    if (GUIUtility.hotControl == 0)
                        EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SlideArrow);
                    break;

                case EventType.MouseDown:
                    if (evt.button == 0)
                    {
                        if (dragRect.Contains(Event.current.mousePosition))
                        {
                            if (GUIUtility.hotControl == 0)
                            {
                                GUIUtility.keyboardControl = 0;
                                GUIUtility.hotControl = id;
                                GUI.changed = true;
                                evt.Use();
                            }
                        }
                        if (!editRect.Contains(Event.current.mousePosition))
                            GUIUtility.keyboardControl = 0; // If not hitting our FloatField ensure it loses focus
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        float dragSensitity = Mathf.Clamp01(Mathf.Max(axisUiScalars.y, Mathf.Pow(Mathf.Abs(axisUiScalars.y), 0.5f)) * .01f);
                        axisUiScalars.y += HandleUtility.niceMouseDelta * dragSensitity;
                        if (axisUiScalars.y < 0.001f)
                            axisUiScalars.y = 0.001f; // Since the scalar is a magnitude we do not want to drag to 0 and below.. find nicer solution
                        SetAxisUiScalars(axisUiScalars, curvesInSameSpace);
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        // Reset dragging
                        GUIUtility.hotControl = 0;
                    }
                    break;
            }

            // Show input text field
            string orgFormat = EditorGUI.kFloatFieldFormatString;
            EditorGUI.kFloatFieldFormatString = m_AxisLabelFormat;
            float newValue = EditorGUI.FloatField(editRect, axisUiScalars.y, Styles.axisLabelNumberField);
            if (axisUiScalars.y != newValue)
                SetAxisUiScalars(new Vector2(axisUiScalars.x, newValue), curvesInSameSpace);
            EditorGUI.kFloatFieldFormatString = orgFormat;
        }

        public void BeginTimeRangeSelection(float time, bool addToSelection)
        {
            if (s_TimeRangeSelectionActive)
            {
                Debug.LogError("BeginTimeRangeSelection can only be called once");
                return;
            }

            s_TimeRangeSelectionActive = true;
            s_TimeRangeSelectionStart = s_TimeRangeSelectionEnd = time;
            if (addToSelection)
                s_SelectionBackup = new List<CurveSelection>(selectedCurves);
            else
                s_SelectionBackup = new List<CurveSelection>();
        }

        public void TimeRangeSelectTo(float time)
        {
            if (!s_TimeRangeSelectionActive)
            {
                Debug.LogError("TimeRangeSelectTo can only be called after BeginTimeRangeSelection");
                return;
            }

            s_TimeRangeSelectionEnd = time;

            var newSelection = new List<CurveSelection>(s_SelectionBackup);
            float minTime = Mathf.Min(s_TimeRangeSelectionStart, s_TimeRangeSelectionEnd);
            float maxTime = Mathf.Max(s_TimeRangeSelectionStart, s_TimeRangeSelectionEnd);
            foreach (CurveWrapper cw in m_AnimationCurves)
            {
                if (cw.readOnly || cw.hidden)
                    continue;
                int i = 0;
                foreach (Keyframe k in cw.curve.keys)
                {
                    if (k.time >= minTime && k.time < maxTime)
                    {
                        newSelection.Add(new CurveSelection(cw.id, i));
                    }
                    i++;
                }
            }
            selectedCurves = newSelection;
            RecalcSecondarySelection();
            RecalcCurveSelection();
        }

        public void EndTimeRangeSelection()
        {
            if (!s_TimeRangeSelectionActive)
            {
                Debug.LogError("EndTimeRangeSelection can only be called after BeginTimeRangeSelection");
                return;
            }

            s_TimeRangeSelectionStart = s_TimeRangeSelectionEnd = 0;
            s_TimeRangeSelectionActive = false;
        }

        public void CancelTimeRangeSelection()
        {
            if (!s_TimeRangeSelectionActive)
            {
                Debug.LogError("CancelTimeRangeSelection can only be called after BeginTimeRangeSelection");
                return;
            }

            selectedCurves = s_SelectionBackup;
            s_TimeRangeSelectionActive = false;
        }

        bool m_EditingPoints;
        bool m_TimeWasEdited;
        bool m_ValueWasEdited;
        float m_NewTime;
        float m_NewValue;

        const string kPointValueFieldName = "pointValueField";
        const string kPointTimeFieldName = "pointTimeField";
        string m_FocusedPointField = null;
        Vector2 m_PointEditingFieldPosition;

        Vector2 GetPointEditionFieldPosition()
        {
            var minTime = selectedCurves.Min(x => GetKeyframeFromSelection(x).time);
            var maxTime = selectedCurves.Max(x => GetKeyframeFromSelection(x).time);
            var minValue = selectedCurves.Min(x => GetKeyframeFromSelection(x).value);
            var maxValue = selectedCurves.Max(x => GetKeyframeFromSelection(x).value);
            return new Vector2(minTime + maxTime, minValue + maxValue) * 0.5f;
        }

        void StartEditingSelectedPointsContext(object fieldPosition)
        {
            StartEditingSelectedPoints((Vector2)fieldPosition);
        }

        void StartEditingSelectedPoints()
        {
            Vector2 centre = GetPointEditionFieldPosition();
            StartEditingSelectedPoints(centre);
        }

        void StartEditingSelectedPoints(Vector2 fieldPosition)
        {
            m_PointEditingFieldPosition = fieldPosition;
            m_FocusedPointField = kPointValueFieldName;
            m_TimeWasEdited = false;
            m_ValueWasEdited = false;

            // Initialize new values to current selection.
            m_NewTime = 0.0f;
            m_NewValue = 0.0f;
            Keyframe keyframe = GetKeyframeFromSelection(selectedCurves[0]);
            if (selectedCurves.All(x => GetKeyframeFromSelection(x).time == keyframe.time))
                m_NewTime = keyframe.time;
            if (selectedCurves.All(x => GetKeyframeFromSelection(x).value == keyframe.value))
                m_NewValue = keyframe.value;

            m_EditingPoints = true;
        }

        void FinishEditingPoints()
        {
            m_EditingPoints = false;
        }

        void EditSelectedPoints()
        {
            var evt = Event.current;

            if (m_EditingPoints && !hasSelection)
            {
                m_EditingPoints = false;
            }

            bool gotEscape = false;
            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return)
                {
                    if (hasSelection && !m_EditingPoints)
                    {
                        StartEditingSelectedPoints();
                        evt.Use();
                    }
                    else if (m_EditingPoints)
                    {
                        SetSelectedKeyPositions(m_NewTime, m_NewValue, m_TimeWasEdited, m_ValueWasEdited);
                        FinishEditingPoints();
                        GUI.changed = true;
                        evt.Use();
                    }
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    gotEscape = true;
                }
            }

            if (!m_EditingPoints)
            {
                return;
            }

            var fieldPosition = DrawingToViewTransformPoint(m_PointEditingFieldPosition);

            const float kFieldHeight = 18f;
            const float kFieldWidth = 80f;

            // Keep fields in the drawing margins
            var drawAreaInMargins = Rect.MinMaxRect(leftmargin, topmargin, rect.width - rightmargin, rect.height - bottommargin);
            fieldPosition.x = Mathf.Clamp(fieldPosition.x, drawAreaInMargins.xMin, drawAreaInMargins.xMax - kFieldWidth);
            fieldPosition.y = Mathf.Clamp(fieldPosition.y, drawAreaInMargins.yMin, drawAreaInMargins.yMax - kFieldHeight * 2);

            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(kPointTimeFieldName);
            m_NewTime = PointFieldForSelection(
                    new Rect(fieldPosition.x, fieldPosition.y, kFieldWidth, kFieldHeight),
                    1,
                    m_NewTime,
                    x => GetKeyframeFromSelection(x).time,
                    (r, id, time) => TimeField(r, id, time, invSnap, timeFormat),
                    "time");
            if (EditorGUI.EndChangeCheck())
            {
                m_TimeWasEdited = true;
            }

            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(kPointValueFieldName);
            m_NewValue = PointFieldForSelection(
                    new Rect(fieldPosition.x, fieldPosition.y + kFieldHeight, kFieldWidth, kFieldHeight),
                    2,
                    m_NewValue,
                    x => GetKeyframeFromSelection(x).value,
                    (r, id, value) => ValueField(r, id, value),
                    "value");
            if (EditorGUI.EndChangeCheck())
            {
                m_ValueWasEdited = true;
            }

            if (gotEscape)
            {
                FinishEditingPoints();
            }

            // Delay focusing these controls until they've been named
            if (m_FocusedPointField != null)
            {
                EditorGUI.FocusTextInControl(m_FocusedPointField);
                if (evt.type == EventType.Repaint)
                {
                    m_FocusedPointField = null;
                }
            }

            if (evt.type == EventType.KeyDown)
            {
                const char tabCharacter = '\t';
                const char endOfMediumCharacter = (char)25; // ASCII 25: "End Of Medium" on pressing shift tab

                // Override Unity's Tab and Shift+Tab handling.
                if (evt.character == tabCharacter || evt.character == endOfMediumCharacter)
                {
                    if (m_TimeWasEdited || m_ValueWasEdited)
                    {
                        SetSelectedKeyPositions(m_NewTime, m_NewValue, m_TimeWasEdited, m_ValueWasEdited);
                        m_PointEditingFieldPosition = GetPointEditionFieldPosition();
                    }

                    m_FocusedPointField = GUI.GetNameOfFocusedControl() == kPointValueFieldName ? kPointTimeFieldName : kPointValueFieldName;
                    evt.Use();
                }
            }

            // Stop editing if there's an unused click
            if (evt.type == EventType.MouseDown)
            {
                FinishEditingPoints();
            }
        }

        // Float editing field for members of selected points
        float PointFieldForSelection(
            Rect rect,
            int customID,
            float value, System.Func<CurveSelection, float> memberGetter,
            System.Func<Rect, int, float, float> fieldCreator,
            string label)
        {
            float firstSelectedValue = memberGetter(selectedCurves[0]);
            bool sameValues = selectedCurves.All(x => memberGetter(x) == firstSelectedValue);
            if (!sameValues)
                EditorGUI.showMixedValue = true;

            var labelRect = rect;
            labelRect.x -= labelRect.width;

            // Use custom IDs to separate event handling from drawing
            int id = GUIUtility.GetControlID(customID, FocusType.Keyboard, rect);
            var oldColor = GUI.color;
            GUI.color = Color.white;
            GUI.Label(labelRect, label, Styles.rightAlignedLabel);

            value = fieldCreator(rect, id, value);

            GUI.color = oldColor;
            EditorGUI.showMixedValue = false;

            return value;
        }

        // m_DraggingCurveOrRegion is null if nothing is being dragged, has one entry if single curve being dragged or has two entries if region is being dragged
        CurveWrapper[] m_DraggingCurveOrRegion = null;

        void SetupKeyOrCurveDragging(Vector2 timeValue, CurveWrapper cw, int id, Vector2 mousePos)
        {
            m_DraggedCoord = timeValue;
            m_DraggingKey = cw;
            GUIUtility.hotControl = id;

            s_StartMouseDragPosition = mousePos;
        }

        public Vector2 MovePoints()
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);

            if (!hasSelection && !settings.allowDraggingCurvesAndRegions)
                return Vector2.zero;

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (evt.button == 0)
                    {
                        // Key dragging
                        foreach (CurveSelection cs in selectedCurves)
                        {
                            CurveWrapper curveWrapper = GetCurveWrapperFromSelection(cs);
                            if ((curveWrapper == null) || curveWrapper.hidden)
                                continue;

                            if ((DrawingToOffsetViewTransformPoint(curveWrapper, GetPosition(cs)) - evt.mousePosition).sqrMagnitude <= kMaxPickDistSqr)
                            {
                                Keyframe keyframe = GetKeyframeFromSelection(cs);
                                SetupKeyOrCurveDragging(new Vector2(keyframe.time, keyframe.value), curveWrapper, id, evt.mousePosition);
                                m_RectangleTool.OnStartMove(s_StartMouseDragPosition, m_RectangleTool.rippleTimeClutch);
                                evt.Use();
                                break;
                            }
                        }

                        // Curve dragging. Moving keys has highest priority, therefore we check curve/region dragging AFTER key dragging above
                        if (settings.allowDraggingCurvesAndRegions && m_DraggingKey == null)
                        {
                            // We use the logic as for moving keys when we drag entire curves or regions: We just
                            // select all keyFrames in a curve or region before dragging and ensure to hide tangents when drawing.
                            Vector2 timeValue = Vector2.zero;
                            CurveWrapper[] curves = null;
                            if (HandleCurveAndRegionMoveToFrontOnMouseDown(ref timeValue, ref curves))
                            {
                                var newSelection = new List<CurveSelection>();
                                // Add all keys of curves to selection to reuse code of key dragging
                                foreach (CurveWrapper cw in curves)
                                {
                                    for (int i = 0; i < cw.curve.keys.Length; ++i)
                                        newSelection.Add(new CurveSelection(cw.id, i));
                                    MoveCurveToFront(cw.id);
                                }
                                preCurveDragSelection = selectedCurves;
                                selectedCurves = newSelection;

                                // Call after selection above
                                SetupKeyOrCurveDragging(timeValue, curves[0], id, evt.mousePosition);
                                m_DraggingCurveOrRegion = curves;
                                m_RectangleTool.OnStartMove(s_StartMouseDragPosition, false);
                                evt.Use();
                            }
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        Vector2 delta = evt.mousePosition - s_StartMouseDragPosition;
                        Vector2 motion = Vector2.zero;

                        // Only drag along x OR y when shift is held down
                        if (evt.shift && m_AxisLock == AxisLock.None)
                            m_AxisLock = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? AxisLock.X : AxisLock.Y;

                        if (m_DraggingCurveOrRegion != null)
                        {
                            // Curve/Region dragging only in y axis direction (for now)
                            delta.x = 0;
                            motion = ViewToDrawingTransformVector(delta);
                            motion.y = SnapValue(motion.y + s_StartKeyDragPosition.y) - s_StartKeyDragPosition.y;
                        }
                        else
                        {
                            switch (m_AxisLock)
                            {
                                case AxisLock.None:
                                    motion = ViewToDrawingTransformVector(delta);
                                    motion.x = SnapTime(motion.x + s_StartKeyDragPosition.x) - s_StartKeyDragPosition.x;
                                    motion.y = SnapValue(motion.y + s_StartKeyDragPosition.y) - s_StartKeyDragPosition.y;
                                    break;
                                case AxisLock.X:
                                    delta.y = 0;
                                    motion = ViewToDrawingTransformVector(delta);
                                    motion.x = SnapTime(motion.x + s_StartKeyDragPosition.x) - s_StartKeyDragPosition.x;
                                    break;
                                case AxisLock.Y:
                                    delta.x = 0;
                                    motion = ViewToDrawingTransformVector(delta);
                                    motion.y = SnapValue(motion.y + s_StartKeyDragPosition.y) - s_StartKeyDragPosition.y;
                                    break;
                            }
                        }

                        m_RectangleTool.OnMove(s_StartMouseDragPosition + motion);

                        GUI.changed = true;
                        evt.Use();
                        return motion;
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id && evt.keyCode == KeyCode.Escape)
                    {
                        TranslateSelectedKeys(Vector2.zero);
                        ResetDragging();
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        m_RectangleTool.OnEndMove();
                        ResetDragging();

                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    Rect mouseRect = new Rect(evt.mousePosition.x - 10, evt.mousePosition.y - 10, 20, 20);
                    if (m_DraggingCurveOrRegion != null)
                        EditorGUIUtility.AddCursorRect(mouseRect, MouseCursor.ResizeVertical);
                    else if (m_DraggingKey != null)
                        EditorGUIUtility.AddCursorRect(mouseRect, MouseCursor.MoveArrow);
                    break;
            }

            return Vector2.zero;
        }

        void ResetDragging()
        {
            // If we are dragging entire curve we have selected all keys we therefore ensure to deselect them again...
            if (m_DraggingCurveOrRegion != null)
            {
                selectedCurves = preCurveDragSelection;
                preCurveDragSelection = null;
            }

            // Cleanup
            GUIUtility.hotControl = 0;
            m_DraggingKey = null;
            m_DraggingCurveOrRegion = null;
            m_MoveCoord = Vector2.zero;

            m_AxisLock = AxisLock.None;
        }

        void MakeCurveBackups()
        {
            SaveKeySelection("Edit Curve");

            m_CurveBackups = new List<SavedCurve>();
            int lastCurveID = -1;
            SavedCurve sc = null;
            for (int i = 0; i < selectedCurves.Count; i++)
            {
                CurveSelection cs = selectedCurves[i];
                // if it's a different curve than last point, we need to back up this curve.
                if (cs.curveID != lastCurveID)
                {
                    AnimationCurve curve = GetCurveFromSelection(cs);
                    if (curve != null)
                    {
                        // Make a new saved curve with copy of all keyframes. No need to mark them as selected
                        sc = new SavedCurve();
                        lastCurveID = sc.curveId = cs.curveID;
                        Keyframe[] keys = curve.keys;
                        sc.keys = new List<SavedCurve.SavedKeyFrame>(keys.Length);
                        foreach (Keyframe k in keys)
                            sc.keys.Add(new SavedCurve.SavedKeyFrame(k, CurveWrapper.SelectionMode.None));
                        m_CurveBackups.Add(sc);
                    }
                }

                // Mark them as selected
                sc.keys[cs.key].selected = cs.semiSelected ? CurveWrapper.SelectionMode.SemiSelected : CurveWrapper.SelectionMode.Selected;
            }
        }

        public void SaveKeySelection(string undoLabel)
        {
            if (settings.undoRedoSelection)
                Undo.RegisterCompleteObjectUndo(selection, undoLabel);
        }

        // Get the position of a CurveSelection. This will correctly offset tangent handles
        Vector2 GetPosition(CurveSelection selection)
        {
            //AnimationCurve curve = selection.curve;
            Keyframe key = GetKeyframeFromSelection(selection);
            Vector2 position = new Vector2(key.time, key.value);

            float tangentLength = 50F;

            if (selection.type == CurveSelection.SelectionType.InTangent)
            {
                Vector2 dir = new Vector2(1.0F, key.inTangent);
                if (key.inTangent == Mathf.Infinity) dir = new Vector2(0, -1);
                dir = NormalizeInViewSpace(dir);
                return position - dir * tangentLength;
            }
            else if (selection.type == CurveSelection.SelectionType.OutTangent)
            {
                Vector2 dir = new Vector2(1.0F, key.outTangent);
                if (key.outTangent == Mathf.Infinity) dir = new Vector2(0, -1);
                dir = NormalizeInViewSpace(dir);
                return position + dir * tangentLength;
            }
            else
                return position;
        }

        void MoveCurveToFront(int curveID)
        {
            int numCurves = m_DrawOrder.Count;
            for (int i = 0; i < numCurves; ++i)
            {
                // Find curveID in draw order list
                if (m_DrawOrder[i] == curveID)
                {
                    // If curve is part of a region then find other curve
                    int regionId = GetCurveWrapperFromID(curveID).regionId;
                    if (regionId >= 0)
                    {
                        // The other region curve can be on either side of current
                        int indexOffset = 0;
                        int curveID2 = -1;

                        if (i - 1 >= 0)
                        {
                            int id = m_DrawOrder[i - 1];
                            if (regionId == GetCurveWrapperFromID(id).regionId)
                            {
                                curveID2 = id;
                                indexOffset = -1;
                            }
                        }
                        if (i + 1 < numCurves && curveID2 < 0)
                        {
                            int id = m_DrawOrder[i + 1];
                            if (regionId == GetCurveWrapperFromID(id).regionId)
                            {
                                curveID2 = id;
                                indexOffset = 0;
                            }
                        }

                        if (curveID2 >= 0)
                        {
                            m_DrawOrder.RemoveRange(i + indexOffset, 2);
                            m_DrawOrder.Add(curveID2);
                            m_DrawOrder.Add(curveID);       // ensure curveID is topMost (last)
                            ValidateCurveList();
                            return;
                        }

                        Debug.LogError("Unhandled region");
                    }
                    else // Single curve
                    {
                        if (i == numCurves - 1)
                            return; // curve already last (topmost)

                        m_DrawOrder.RemoveAt(i);
                        m_DrawOrder.Add(curveID);
                        ValidateCurveList();
                        return;
                    }
                }
            }
        }

        bool IsCurveSelected(CurveWrapper cw)
        {
            if (cw != null)
                return cw.selected != CurveWrapper.SelectionMode.None;
            return false;
        }

        bool IsRegionCurveSelected(CurveWrapper cw1, CurveWrapper cw2)
        {
            return IsCurveSelected(cw1) ||
                IsCurveSelected(cw2);
        }

        bool IsRegion(CurveWrapper cw1, CurveWrapper cw2)
        {
            if (cw1 != null && cw2 != null)
                if (cw1.regionId >= 0)
                    return cw1.regionId == cw2.regionId;
            return false;
        }

        bool IsLeftTangentEditable(CurveSelection selection)
        {
            Keyframe keyframe = GetKeyframeFromSelection(selection);
            TangentMode mode = AnimationUtility.GetKeyLeftTangentMode(keyframe);

            // Tangent is already set to Free.
            if (mode == TangentMode.Free)
                return true;

            // If tangent is modified, it will be converted to Free.
            if (mode == TangentMode.ClampedAuto || mode == TangentMode.Auto)
                return true;

            return false;
        }

        bool IsRightTangentEditable(CurveSelection selection)
        {
            Keyframe keyframe = GetKeyframeFromSelection(selection);
            TangentMode mode = AnimationUtility.GetKeyRightTangentMode(keyframe);

            // Tangent is already set to Free.
            if (mode == TangentMode.Free)
                return true;

            // If tangent is modified, it will be converted to Free.
            if (mode == TangentMode.ClampedAuto || mode == TangentMode.Auto)
                return true;

            return false;
        }

        void DrawCurvesAndRegion(CurveWrapper cw1, CurveWrapper cw2, List<CurveSelection> selection, bool hasFocus)
        {
            DrawRegion(cw1, cw2, hasFocus);
            DrawCurveAndPoints(cw1, IsCurveSelected(cw1) ? selection : null, hasFocus);
            DrawCurveAndPoints(cw2, IsCurveSelected(cw2) ? selection : null, hasFocus);
        }

        void DrawCurveAndPoints(CurveWrapper cw, List<CurveSelection> selection, bool hasFocus)
        {
            DrawCurve(cw, hasFocus);
            DrawPointsOnCurve(cw, selection, hasFocus);
        }

        bool ShouldCurveHaveFocus(int indexIntoDrawOrder, CurveWrapper cw1, CurveWrapper cw2)
        {
            bool focus = false;
            if (indexIntoDrawOrder == m_DrawOrder.Count - 1)
                focus = true;
            else if (hasSelection)
                focus = IsCurveSelected(cw1) || IsCurveSelected(cw2);
            return focus;
        }

        void DrawCurves()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            m_PointRenderer.Clear();

            // Draw all curves
            for (int i = 0; i < m_DrawOrder.Count; ++i)
            {
                CurveWrapper cw = GetCurveWrapperFromID(m_DrawOrder[i]);

                if (cw == null)
                    continue;
                if (cw.hidden)
                    continue;
                if (cw.curve.length == 0)
                    continue;


                CurveWrapper cw2 = null;
                if (i < m_DrawOrder.Count - 1)
                    cw2 = GetCurveWrapperFromID(m_DrawOrder[i + 1]);

                if (IsRegion(cw, cw2))
                {
                    i++; // we handle two curves

                    bool focus = ShouldCurveHaveFocus(i, cw, cw2);
                    DrawCurvesAndRegion(cw, cw2, IsRegionCurveSelected(cw, cw2) ? selectedCurves : null, focus);
                }
                else
                {
                    bool focus = ShouldCurveHaveFocus(i, cw, null);
                    DrawCurveAndPoints(cw, IsCurveSelected(cw) ? selectedCurves : null, focus);
                }
            }

            m_PointRenderer.Render();
        }

        void DrawCurvesTangents()
        {
            if (m_DraggingCurveOrRegion != null)
                return;

            // Draw left and right tangents lines
            HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.LINES);
            GL.Color(m_TangentColor * new Color(1, 1, 1, 0.75f));
            for (int i = 0; i < selectedCurves.Count; ++i)
            {
                CurveSelection sel = selectedCurves[i];

                if (sel.semiSelected)
                    continue;
                Vector2 keyPoint = GetPosition(sel);

                CurveWrapper curveWrapper = GetCurveWrapperFromSelection(sel);
                if (curveWrapper == null)
                    continue;

                AnimationCurve curve = curveWrapper.curve;
                if (curve == null)
                    continue;

                if (curve.length == 0)
                    continue;

                if (IsLeftTangentEditable(sel) && GetKeyframeFromSelection(sel).time != curve.keys[0].time)
                {
                    Vector2 leftTangent = GetPosition(new CurveSelection(sel.curveID, sel.key, CurveSelection.SelectionType.InTangent));
                    DrawCurveLine(curveWrapper, leftTangent, keyPoint);
                }

                if (IsRightTangentEditable(sel) && GetKeyframeFromSelection(sel).time != curve.keys[curve.keys.Length - 1].time)
                {
                    Vector2 rightTangent = GetPosition(new CurveSelection(sel.curveID, sel.key, CurveSelection.SelectionType.OutTangent));
                    DrawCurveLine(curveWrapper, keyPoint, rightTangent);
                }
            }
            GL.End();

            m_PointRenderer.Clear();

            // Draw left and right tangents handles
            GUI.color = m_TangentColor;
            for (int i = 0; i < selectedCurves.Count; ++i)
            {
                CurveSelection sel = selectedCurves[i];

                if (sel.semiSelected)
                    continue;

                CurveWrapper curveWrapper = GetCurveWrapperFromSelection(sel);
                if (curveWrapper == null)
                    continue;

                AnimationCurve curve = curveWrapper.curve;
                if (curve == null)
                    continue;

                if (curve.length == 0)
                    continue;

                if (IsLeftTangentEditable(sel) && GetKeyframeFromSelection(sel).time != curve.keys[0].time)
                {
                    Vector2 leftTangent = DrawingToOffsetViewTransformPoint(curveWrapper, GetPosition(new CurveSelection(sel.curveID, sel.key, CurveSelection.SelectionType.InTangent)));
                    DrawPoint(leftTangent, CurveWrapper.SelectionMode.None);
                }

                if (IsRightTangentEditable(sel) && GetKeyframeFromSelection(sel).time != curve.keys[curve.keys.Length - 1].time)
                {
                    Vector2 rightTangent = DrawingToOffsetViewTransformPoint(curveWrapper, GetPosition(new CurveSelection(sel.curveID, sel.key, CurveSelection.SelectionType.OutTangent)));
                    DrawPoint(rightTangent, CurveWrapper.SelectionMode.None);
                }
            }

            m_PointRenderer.Render();
        }

        void DrawCurvesOverlay()
        {
            if (m_DraggingCurveOrRegion != null)
                return;

            // Draw label with values while dragging
            if (m_DraggingKey != null && settings.rectangleToolFlags == RectangleToolFlags.NoRectangleTool)
            {
                GUI.color = Color.white;

                // Clamp only using the currently dragged curve (we could have more selected but we only show the coord info for this one).
                float smallestVRangeMin = vRangeMin;
                float smallestVRangeMax = vRangeMax;
                smallestVRangeMin = Mathf.Max(smallestVRangeMin, m_DraggingKey.vRangeMin);
                smallestVRangeMax = Mathf.Min(smallestVRangeMax, m_DraggingKey.vRangeMax);

                Vector2 newPoint = m_DraggedCoord + m_MoveCoord;
                newPoint.x = Mathf.Clamp(newPoint.x, hRangeMin, hRangeMax);
                newPoint.y = Mathf.Clamp(newPoint.y, smallestVRangeMin, smallestVRangeMax);
                Vector2 p = DrawingToOffsetViewTransformPoint(m_DraggingKey, newPoint);

                Vector2 axisUiScalars = m_DraggingKey.getAxisUiScalarsCallback != null ? m_DraggingKey.getAxisUiScalarsCallback() : Vector2.one;
                if (axisUiScalars.x >= 0f)
                    newPoint.x *= axisUiScalars.x;
                if (axisUiScalars.y >= 0f)
                    newPoint.y *= axisUiScalars.y;
                GUIContent content = new GUIContent(string.Format("{0}, {1}", FormatTime(newPoint.x, invSnap, timeFormat), FormatValue(newPoint.y)));
                Vector2 size = Styles.dragLabel.CalcSize(content);
                EditorGUI.DoDropShadowLabel(
                    new Rect(p.x, p.y - size.y, size.x, size.y), content, Styles.dragLabel, 0.3f
                    );
            }
        }

        List<Vector3> CreateRegion(CurveWrapper minCurve, CurveWrapper maxCurve, float deltaTime)
        {
            // Create list of triangle points
            List<Vector3> region = new List<Vector3>();

            List<float> sampleTimes = new List<float>();
            float sampleTime = deltaTime;
            for (; sampleTime <= 1.0f; sampleTime += deltaTime)
                sampleTimes.Add(sampleTime);

            if (sampleTime != 1.0f)
                sampleTimes.Add(1.0f);

            // To handle constant curves (high gradient) we add key time samples on both side of the keys as well
            // the key time itself.

            Keyframe[] maxKeys = maxCurve.curve.keys;
            for (int i = 0; i < maxKeys.Length; ++i)
            {
                Keyframe key = maxKeys[i];
                if (key.time > 0f && key.time < 1.0f)
                {
                    sampleTimes.Add(key.time - 0.0001f);
                    sampleTimes.Add(key.time);
                    sampleTimes.Add(key.time + 0.0001f);
                }
            }
            Keyframe[] minKeys = minCurve.curve.keys;
            for (int i = 0; i < minKeys.Length; ++i)
            {
                Keyframe key = minKeys[i];
                if (key.time > 0f && key.time < 1.0f)
                {
                    sampleTimes.Add(key.time - 0.0001f);
                    sampleTimes.Add(key.time);
                    sampleTimes.Add(key.time + 0.0001f);
                }
            }

            sampleTimes.Sort();

            Vector3 prevA = new Vector3(0.0f, maxCurve.renderer.EvaluateCurveSlow(0.0f), 0.0f);
            Vector3 prevB = new Vector3(0.0f, minCurve.renderer.EvaluateCurveSlow(0.0f), 0.0f);

            Vector3 screenPrevA = DrawingToOffsetViewMatrix(maxCurve).MultiplyPoint(prevA);
            Vector3 screenPrevB = DrawingToOffsetViewMatrix(minCurve).MultiplyPoint(prevB);

            for (int i = 0; i < sampleTimes.Count; ++i)
            {
                float time = sampleTimes[i];
                Vector3 valueA = new Vector3(time, maxCurve.renderer.EvaluateCurveSlow(time), 0.0f);
                Vector3 valueB = new Vector3(time, minCurve.renderer.EvaluateCurveSlow(time), 0.0f);

                Vector3 screenValueA = DrawingToOffsetViewMatrix(maxCurve).MultiplyPoint(valueA);
                Vector3 screenValueB = DrawingToOffsetViewMatrix(minCurve).MultiplyPoint(valueB);

                // Add triangles
                if (prevA.y >= prevB.y && valueA.y >= valueB.y)
                {
                    // max is top
                    region.Add(screenPrevA);
                    region.Add(screenValueB);
                    region.Add(screenPrevB);

                    region.Add(screenPrevA);
                    region.Add(screenValueA);
                    region.Add(screenValueB);
                }
                else if (prevA.y <= prevB.y && valueA.y <= valueB.y)
                {
                    // min is top
                    region.Add(screenPrevB);
                    region.Add(screenValueA);
                    region.Add(screenPrevA);

                    region.Add(screenPrevB);
                    region.Add(screenValueB);
                    region.Add(screenValueA);
                }
                else
                {
                    // Find intersection
                    Vector2 intersection = Vector2.zero;
                    if (Mathf.LineIntersection(screenPrevA, screenValueA, screenPrevB, screenValueB, ref intersection))
                    {
                        region.Add(screenPrevA);
                        region.Add(intersection);
                        region.Add(screenPrevB);

                        region.Add(screenValueA);
                        region.Add(intersection);
                        region.Add(screenValueB);
                    }
                    else
                    {
                        Debug.Log("Error: No intersection found! There should be one...");
                    }
                }

                prevA = valueA;
                prevB = valueB;

                screenPrevA = screenValueA;
                screenPrevB = screenValueB;
            }

            return region;
        }

        public void DrawRegion(CurveWrapper curve1, CurveWrapper curve2, bool hasFocus)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            float deltaTime = 1.0f / (rect.width / 10.0f);
            List<Vector3> points = CreateRegion(curve1, curve2, deltaTime);
            Color color = curve1.color;

            if (IsDraggingRegion(curve1, curve2))
            {
                color = Color.Lerp(color, Color.black, 0.1f);
                color.a = 0.4f;
            }
            else if (settings.useFocusColors && !hasFocus)
            {
                color *= 0.4f;
                color.a = 0.1f;
            }
            else
            {
                color *= 1.0f;
                color.a = 0.4f;
            }

            Shader.SetGlobalColor("_HandleColor", color);
            HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.TRIANGLES);
            int numTriangles = points.Count / 3;
            for (int i = 0; i < numTriangles; i++)
            {
                GL.Color(color);
                GL.Vertex(points[i * 3]);
                GL.Vertex(points[i * 3 + 1]);
                GL.Vertex(points[i * 3 + 2]);
            }
            GL.End();
        }

        void DrawCurve(CurveWrapper cw, bool hasFocus)
        {
            CurveRenderer renderer = cw.renderer;
            Color color = cw.color;

            if (IsDraggingCurve(cw) || cw.selected == CurveWrapper.SelectionMode.Selected)
            {
                color = Color.Lerp(color, Color.white, 0.3f);
            }
            else if (settings.useFocusColors && !hasFocus)
            {
                color *= 0.5f;
                color.a = 0.8f;
            }

            Rect framed = shownArea;
            renderer.DrawCurve(framed.xMin - cw.timeOffset, framed.xMax, color, DrawingToOffsetViewMatrix(cw), settings.wrapColor * cw.wrapColorMultiplier);
        }

        void DrawPointsOnCurve(CurveWrapper cw, List<CurveSelection> selected, bool hasFocus)
        {
            m_PreviousDrawPointCenter = new Vector2(float.MinValue, float.MinValue);

            if (selected == null)
            {
                Color color = cw.color;
                if (settings.useFocusColors && !hasFocus)
                    color *= 0.5f;
                GUI.color = color;

                Keyframe[] keys = cw.curve.keys;
                for (int i = 0; i < keys.Length; ++i)
                {
                    Keyframe k = keys[i];
                    DrawPoint(DrawingToOffsetViewTransformPoint(cw, new Vector2(k.time, k.value)), CurveWrapper.SelectionMode.None);
                }
            }
            else
            {
                Color keyColor = Color.Lerp(cw.color, Color.white, .2f);
                GUI.color = keyColor;

                int selectionIdx = 0;
                // Find the point in selected curves that matches the curve we're about to draw.
                while (selectionIdx < selected.Count && selected[selectionIdx].curveID != cw.id)
                    selectionIdx++;
                // we're now at the right point in the selection.
                int pointIdx = 0;
                Keyframe[] keys = cw.curve.keys;
                for (int i = 0; i < keys.Length; ++i)
                {
                    Keyframe k = keys[i];
                    if (selectionIdx < selected.Count && selected[selectionIdx].key == pointIdx && selected[selectionIdx].curveID == cw.id)
                    {
                        if (selected[selectionIdx].semiSelected)
                            DrawPoint(DrawingToOffsetViewTransformPoint(cw, new Vector2(k.time, k.value)), CurveWrapper.SelectionMode.SemiSelected);
                        else
                            DrawPoint(DrawingToOffsetViewTransformPoint(cw, new Vector2(k.time, k.value)), CurveWrapper.SelectionMode.Selected, settings.rectangleToolFlags == RectangleToolFlags.NoRectangleTool ? MouseCursor.MoveArrow : MouseCursor.Arrow);
                        selectionIdx++;
                    }
                    else
                        DrawPoint(DrawingToOffsetViewTransformPoint(cw, new Vector2(k.time, k.value)), CurveWrapper.SelectionMode.None);
                    pointIdx++;
                }
                GUI.color = Color.white;
            }
        }

        void DrawPoint(Vector2 viewPos, CurveWrapper.SelectionMode selected)
        {
            DrawPoint(viewPos, selected, MouseCursor.Arrow);
        }

        void DrawPoint(Vector2 viewPos, CurveWrapper.SelectionMode selected, MouseCursor mouseCursor)
        {
            // Important to take floor of positions of GUI stuff to get pixel correct alignment of
            // stuff drawn with both GUI and Handles/GL. Otherwise things are off by one pixel half the time.
            var rect = new Rect(Mathf.Floor(viewPos.x) - 4, Mathf.Floor(viewPos.y) - 4, Styles.pointIcon.width, Styles.pointIcon.height);

            Vector2 center = rect.center;
            if ((center - m_PreviousDrawPointCenter).magnitude > 8)
            {
                if (selected == CurveWrapper.SelectionMode.None)
                {
                    m_PointRenderer.AddPoint(rect, GUI.color);
                }
                else
                {
                    if (selected == CurveWrapper.SelectionMode.Selected)
                        m_PointRenderer.AddSelectedPoint(rect, GUI.color);
                    else
                        m_PointRenderer.AddSemiSelectedPoint(rect, GUI.color);
                }

                // Changing the cursor for every point in the selection can be awfully costly.
                if (mouseCursor != MouseCursor.Arrow)
                {
                    if (GUIUtility.hotControl == 0)
                        EditorGUIUtility.AddCursorRect(rect, mouseCursor);
                }

                m_PreviousDrawPointCenter = center;
            }
        }

        // FIXME remove when grid drawing function has been properly rewritten
        void DrawLine(Vector2 lhs, Vector2 rhs)
        {
            GL.Vertex(DrawingToViewTransformPoint(new Vector3(lhs.x, lhs.y, 0)));
            GL.Vertex(DrawingToViewTransformPoint(new Vector3(rhs.x, rhs.y, 0)));
        }

        void DrawCurveLine(CurveWrapper cw, Vector2 lhs, Vector2 rhs)
        {
            GL.Vertex(DrawingToOffsetViewTransformPoint(cw, new Vector3(lhs.x, lhs.y, 0)));
            GL.Vertex(DrawingToOffsetViewTransformPoint(cw, new Vector3(rhs.x, rhs.y, 0)));
        }

        void DrawWrapperPopups()
        {
            if (!settings.showWrapperPopups)
                return;

            int curveId;
            GetTopMostCurveID(out curveId);

            if (curveId == -1)
                return;

            CurveWrapper wrapper = GetCurveWrapperFromID(curveId);
            AnimationCurve curve = wrapper.curve;

            if (curve != null && curve.length >= 2 && curve.preWrapMode != WrapMode.Default)
            {
                GUI.BeginGroup(drawRect);
                Color oldText = GUI.contentColor;

                var preKey = curve.keys[0];
                var preWrap = curve.preWrapMode;
                preWrap = WrapModeIconPopup(preKey, preWrap, -1.5f);
                if (preWrap != curve.preWrapMode)
                {
                    curve.preWrapMode = preWrap;
                    wrapper.changed = true;
                }

                var postKey = curve.keys[curve.length - 1];
                var postWrap = curve.postWrapMode;
                postWrap = WrapModeIconPopup(postKey, postWrap, 0.5f);
                if (postWrap != curve.postWrapMode)
                {
                    curve.postWrapMode = postWrap;
                    wrapper.changed = true;
                }

                if (wrapper.changed)
                {
                    wrapper.renderer.SetWrap(curve.preWrapMode, curve.postWrapMode);
                    if (curvesUpdated != null)
                        curvesUpdated();
                }

                GUI.contentColor = oldText;
                GUI.EndGroup();
            }
        }

        WrapMode WrapModeIconPopup(Keyframe key, WrapMode oldWrap, float hOffset)
        {
            float buttonSize = Styles.wrapModeMenuIcon.image.width;
            var keyPosition = new Vector3(key.time, key.value);
            keyPosition = DrawingToViewTransformPoint(keyPosition);
            var r = new Rect(keyPosition.x + buttonSize * hOffset, keyPosition.y, buttonSize, buttonSize);

            var selectedValue = (WrapModeFixedCurve)oldWrap;

            // EnumPopupInternal begins

            // sa and values are sorted in the same order
            Enum[] enumValues = Enum.GetValues(typeof(WrapModeFixedCurve)).Cast<Enum>().ToArray();
            var stringNames = Enum.GetNames(typeof(WrapModeFixedCurve)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray();

            int i = Array.IndexOf(enumValues, selectedValue);

            // PopupInternal begins

            int controlID = GUIUtility.GetControlID("WrapModeIconPopup".GetHashCode(), FocusType.Keyboard, r);

            // DoPopup begins

            var selectedPopupIndex = EditorGUI.PopupCallbackInfo.GetSelectedValueForControl(controlID, i);

            var popupStrings = EditorGUIUtility.TempContent(stringNames);

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                    GUIStyle.none.Draw(r, Styles.wrapModeMenuIcon, controlID, false);
                    break;
                case EventType.MouseDown:
                    if (evt.button == 0 && r.Contains(evt.mousePosition))
                    {
                        if (Application.platform == RuntimePlatform.OSXEditor)
                        {
                            r.y = r.y - selectedPopupIndex * 16 - 19;
                        }

                        EditorGUI.PopupCallbackInfo.instance = new EditorGUI.PopupCallbackInfo(controlID);
                        EditorUtility.DisplayCustomMenu(r, popupStrings, selectedPopupIndex, EditorGUI.PopupCallbackInfo.instance.SetEnumValueDelegate, null);
                        GUIUtility.keyboardControl = controlID;
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (evt.MainActionKeyForControl(controlID))
                    {
                        if (Application.platform == RuntimePlatform.OSXEditor)
                        {
                            r.y = r.y - selectedPopupIndex * 16 - 19;
                        }

                        EditorGUI.PopupCallbackInfo.instance = new EditorGUI.PopupCallbackInfo(controlID);
                        EditorUtility.DisplayCustomMenu(r, popupStrings, selectedPopupIndex, EditorGUI.PopupCallbackInfo.instance.SetEnumValueDelegate, null);
                        evt.Use();
                    }
                    break;
            }

            return (WrapMode)enumValues[selectedPopupIndex];
        }

        // The return value for each axis can be -1, if so then we do not have any proper value
        // to use.
        private Vector2 GetAxisUiScalars(List<CurveWrapper> curvesWithSameParameterSpace)
        {
            // If none or just one selected curve then use top most rendered curve value
            if (selectedCurves.Count <= 1)
            {
                if (m_DrawOrder.Count > 0)
                {
                    CurveWrapper cw = GetCurveWrapperFromID(m_DrawOrder[m_DrawOrder.Count - 1]);
                    if (cw != null && cw.getAxisUiScalarsCallback != null)
                    {
                        // Save list
                        if (curvesWithSameParameterSpace != null)
                            curvesWithSameParameterSpace.Add(cw);
                        return cw.getAxisUiScalarsCallback();
                    }
                }
                return Vector2.one;
            }

            // If multiple curves selected we have to check if they are in the same value space
            Vector2 axisUiScalars = new Vector2(-1, -1);
            if (selectedCurves.Count > 1)
            {
                // Find common axis scalars if more than one key selected
                bool xAllSame = true;
                bool yAllSame = true;
                Vector2 scalars = Vector2.one;
                for (int i = 0; i < selectedCurves.Count; ++i)
                {
                    CurveWrapper cw = GetCurveWrapperFromSelection(selectedCurves[i]);
                    if (cw == null)
                        continue;

                    if (cw.getAxisUiScalarsCallback != null)
                    {
                        Vector2 temp = cw.getAxisUiScalarsCallback();
                        if (i == 0)
                        {
                            scalars = temp; // init scalars
                        }
                        else
                        {
                            if (Mathf.Abs(temp.x - scalars.x) > 0.00001f)
                                xAllSame = false;
                            if (Mathf.Abs(temp.y - scalars.y) > 0.00001f)
                                yAllSame = false;
                            scalars = temp;
                        }

                        // Save list
                        if (curvesWithSameParameterSpace != null)
                            curvesWithSameParameterSpace.Add(cw);
                    }
                }
                if (xAllSame)
                    axisUiScalars.x = scalars.x;
                if (yAllSame)
                    axisUiScalars.y = scalars.y;
            }

            return axisUiScalars;
        }

        private void SetAxisUiScalars(Vector2 newScalars, List<CurveWrapper> curvesInSameSpace)
        {
            foreach (CurveWrapper cw in curvesInSameSpace)
            {
                // Only set valid values (-1 indicate invalid value, if so use original value)
                Vector2 scalar = cw.getAxisUiScalarsCallback();
                if (newScalars.x >= 0)
                    scalar.x = newScalars.x;
                if (newScalars.y >= 0)
                    scalar.y = newScalars.y;

                if (cw.setAxisUiScalarsCallback != null)
                    cw.setAxisUiScalarsCallback(scalar);
            }
        }

        internal enum PickMode { None, Click, Marquee };

        public void GridGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            GUI.BeginClip(drawRect);

            Color tempCol = GUI.color;

            // Get axis scalars
            Vector2 axisUiScalars = GetAxisUiScalars(null);

            // Cache framed area rect as fetching the property takes some calculations
            Rect shownRect = shownArea;

            hTicks.SetRanges(shownRect.xMin * axisUiScalars.x, shownRect.xMax * axisUiScalars.x, drawRect.xMin, drawRect.xMax);
            vTicks.SetRanges(shownRect.yMin * axisUiScalars.y, shownRect.yMax * axisUiScalars.y, drawRect.yMin, drawRect.yMax);

            // Draw time markers of various strengths
            HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.LINES);

            float lineStart, lineEnd;

            hTicks.SetTickStrengths(settings.hTickStyle.distMin, settings.hTickStyle.distFull, false);
            if (settings.hTickStyle.stubs)
            {
                lineStart = shownRect.yMin;
                lineEnd = shownRect.yMin - 40 / scale.y;
            }
            else
            {
                lineStart = Mathf.Max(shownRect.yMin, vRangeMin);
                lineEnd = Mathf.Min(shownRect.yMax, vRangeMax);
            }

            for (int l = 0; l < hTicks.tickLevels; l++)
            {
                float strength = hTicks.GetStrengthOfLevel(l);
                if (strength > 0f)
                {
                    GL.Color(settings.hTickStyle.tickColor * new Color(1, 1, 1, strength) * new Color(1, 1, 1, 0.75f));
                    float[] ticks = hTicks.GetTicksAtLevel(l, true);
                    for (int j = 0; j < ticks.Length; j++)
                    {
                        ticks[j] /= axisUiScalars.x;
                        if (ticks[j] > hRangeMin && ticks[j] < hRangeMax)
                            DrawLine(new Vector2(ticks[j], lineStart), new Vector2(ticks[j], lineEnd));
                    }
                }
            }

            // Draw bounds of allowed range
            GL.Color(settings.hTickStyle.tickColor * new Color(1, 1, 1, 1) * new Color(1, 1, 1, 0.75f));
            if (hRangeMin != Mathf.NegativeInfinity)
                DrawLine(new Vector2(hRangeMin, lineStart), new Vector2(hRangeMin, lineEnd));
            if (hRangeMax != Mathf.Infinity)
                DrawLine(new Vector2(hRangeMax, lineStart), new Vector2(hRangeMax, lineEnd));

            vTicks.SetTickStrengths(settings.vTickStyle.distMin, settings.vTickStyle.distFull, false);
            if (settings.vTickStyle.stubs)
            {
                lineStart = shownRect.xMin;
                lineEnd = shownRect.xMin + 40 / scale.x;
            }
            else
            {
                lineStart = Mathf.Max(shownRect.xMin, hRangeMin);
                lineEnd = Mathf.Min(shownRect.xMax, hRangeMax);
            }

            // Draw value markers of various strengths
            for (int l = 0; l < vTicks.tickLevels; l++)
            {
                float strength = vTicks.GetStrengthOfLevel(l);
                if (strength > 0f)
                {
                    GL.Color(settings.vTickStyle.tickColor * new Color(1, 1, 1, strength) * new Color(1, 1, 1, 0.75f));
                    float[] ticks = vTicks.GetTicksAtLevel(l, true);
                    for (int j = 0; j < ticks.Length; j++)
                    {
                        ticks[j] /= axisUiScalars.y;
                        if (ticks[j] > vRangeMin && ticks[j] < vRangeMax)
                            DrawLine(new Vector2(lineStart, ticks[j]), new Vector2(lineEnd, ticks[j]));
                    }
                }
            }
            // Draw bounds of allowed range
            GL.Color(settings.vTickStyle.tickColor * new Color(1, 1, 1, 1) * new Color(1, 1, 1, 0.75f));
            if (vRangeMin != Mathf.NegativeInfinity)
                DrawLine(new Vector2(lineStart, vRangeMin), new Vector2(lineEnd, vRangeMin));
            if (vRangeMax != Mathf.Infinity)
                DrawLine(new Vector2(lineStart, vRangeMax), new Vector2(lineEnd, vRangeMax));

            GL.End();


            if (settings.showAxisLabels)
            {
                // X Axis labels
                if (settings.hTickStyle.distLabel > 0 && axisUiScalars.x > 0f)
                {
                    GUI.color = settings.hTickStyle.labelColor;
                    int labelLevel = hTicks.GetLevelWithMinSeparation(settings.hTickStyle.distLabel);

                    // Calculate how many decimals are needed to show the differences between the labeled ticks
                    int decimals = MathUtils.GetNumberOfDecimalsForMinimumDifference(hTicks.GetPeriodOfLevel(labelLevel));

                    // now draw
                    float[] ticks = hTicks.GetTicksAtLevel(labelLevel, false);
                    float[] ticksPos = (float[])ticks.Clone();
                    float vpos = Mathf.Floor(drawRect.height);
                    for (int i = 0; i < ticks.Length; i++)
                    {
                        ticksPos[i] /= axisUiScalars.x;
                        if (ticksPos[i] < hRangeMin || ticksPos[i] > hRangeMax)
                            continue;
                        Vector2 pos = DrawingToViewTransformPoint(new Vector2(ticksPos[i], 0));
                        // Important to take floor of positions of GUI stuff to get pixel correct alignment of
                        // stuff drawn with both GUI and Handles/GL. Otherwise things are off by one pixel half the time.
                        pos = new Vector2(Mathf.Floor(pos.x), vpos);

                        float uiValue = ticks[i];
                        Rect labelRect;
                        TextAnchor wantedAlignment;
                        if (settings.hTickStyle.centerLabel)
                        {
                            wantedAlignment = TextAnchor.UpperCenter;
                            labelRect = new Rect(pos.x, pos.y - 16 - settings.hTickLabelOffset, 1, 16);
                        }
                        else
                        {
                            wantedAlignment = TextAnchor.UpperLeft;
                            labelRect = new Rect(pos.x, pos.y - 16 - settings.hTickLabelOffset, 50, 16);
                        }

                        if (Styles.labelTickMarksX.alignment != wantedAlignment)
                            Styles.labelTickMarksX.alignment = wantedAlignment;

                        GUI.Label(labelRect, uiValue.ToString("n" + decimals) + settings.hTickStyle.unit, Styles.labelTickMarksX);
                    }
                }

                // Y Axis labels
                if (settings.vTickStyle.distLabel > 0 && axisUiScalars.y > 0f)
                {
                    // Draw value labels
                    GUI.color = settings.vTickStyle.labelColor;
                    int labelLevel = vTicks.GetLevelWithMinSeparation(settings.vTickStyle.distLabel);

                    float[] ticks = vTicks.GetTicksAtLevel(labelLevel, false);
                    float[] ticksPos = (float[])ticks.Clone();

                    // Calculate how many decimals are needed to show the differences between the labeled ticks
                    int decimals =  MathUtils.GetNumberOfDecimalsForMinimumDifference(vTicks.GetPeriodOfLevel(labelLevel));
                    string format = "n" + decimals;
                    m_AxisLabelFormat = format;

                    // Calculate the size of the biggest shown label
                    float labelSize = 35;
                    if (!settings.vTickStyle.stubs && ticks.Length > 1)
                    {
                        float min = ticks[1];
                        float max = ticks[ticks.Length - 1];
                        string minNumber = min.ToString(format) + settings.vTickStyle.unit;
                        string maxNumber = max.ToString(format) + settings.vTickStyle.unit;
                        labelSize = Mathf.Max(
                                Styles.labelTickMarksY.CalcSize(new GUIContent(minNumber)).x,
                                Styles.labelTickMarksY.CalcSize(new GUIContent(maxNumber)).x
                                ) + 6;
                    }

                    // Now draw
                    for (int i = 0; i < ticks.Length; i++)
                    {
                        ticksPos[i] /= axisUiScalars.y;
                        if (ticksPos[i] < vRangeMin || ticksPos[i] > vRangeMax)
                            continue;
                        Vector2 pos = DrawingToViewTransformPoint(new Vector2(0, ticksPos[i]));
                        // Important to take floor of positions of GUI stuff to get pixel correct alignment of
                        // stuff drawn with both GUI and Handles/GL. Otherwise things are off by one pixel half the time.
                        pos = new Vector2(pos.x, Mathf.Floor(pos.y));

                        float uiValue = ticks[i];
                        Rect labelRect;
                        if (settings.vTickStyle.centerLabel)
                            labelRect = new Rect(0, pos.y - 8, leftmargin - 4, 16);  // text expands to the left starting from where grid starts (leftmargin size must ensure text is visible)
                        else
                            labelRect = new Rect(0, pos.y - 13, labelSize, 16);     // text expands to the right starting from left side of window


                        GUI.Label(labelRect, uiValue.ToString(format) + settings.vTickStyle.unit, Styles.labelTickMarksY);
                    }
                }
            }
            // Cleanup
            GUI.color = tempCol;

            GUI.EndClip();
        }
    }
} // namespace
